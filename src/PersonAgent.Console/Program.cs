using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using PersonAgent.Agent;
using PersonAgent.Cli;
using PersonAgent.Configuration;
using PersonAgent.Http;
using PersonAgent.Llm;

LoadEnv(Directory.GetCurrentDirectory());
LoadEnv(AppContext.BaseDirectory);

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = [],
    ContentRootPath = AppContext.BaseDirectory,
});
builder.Logging.AddConsole();
builder.Services.Configure<ConsoleLoggerOptions>(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

var configuration = builder.Configuration;
var apiSettings = configuration.GetSection(PersonApiSettings.SectionName).Get<PersonApiSettings>() ?? new PersonApiSettings();
var ollamaSettings = configuration.GetSection(OllamaSettings.SectionName).Get<OllamaSettings>() ?? new OllamaSettings();
var llmSettings = configuration.GetSection(LlmSettings.SectionName).Get<LlmSettings>() ?? new LlmSettings();
var agentSettings = configuration.GetSection(AgentSettings.SectionName).Get<AgentSettings>() ?? new AgentSettings();

builder.Services.AddHttpClient<PersonApiClient>(client =>
{
    client.BaseAddress = new Uri(apiSettings.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(apiSettings.TimeoutSeconds, 1, 600));
}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    AllowAutoRedirect = false,
});

builder.Services.AddSingleton<IChatClient>(_ => ChatClientFactory.Create(ollamaSettings, llmSettings));
builder.Services.AddSingleton(sp => new PersonAgentRunner(
    sp.GetRequiredService<IChatClient>(),
    sp.GetRequiredService<PersonApiClient>(),
    sp.GetRequiredService<ILogger<PersonAgentRunner>>(),
    LoadPrompt(),
    agentSettings.MaxActions));

using var app = builder.Build();
var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("PersonAgent");

var configurationError = SettingsValidator.Validate(apiSettings, ollamaSettings, llmSettings, agentSettings);
if (configurationError is not null)
{
    Console.Out.WriteLine(ResponseFormatter.FromConfiguration(configurationError).Text);
    return 1;
}

if (!File.Exists(PromptPath()))
{
    Console.Out.WriteLine(ResponseFormatter.FromConfiguration("не найден файл Prompts/system.prompt").Text);
    return 1;
}

logger.LogInformation(
    "Агент запущен. API {Api}, провайдер {Provider}, модель {Model}",
    apiSettings.BaseUrl,
    llmSettings.Provider,
    SettingsValidator.IsOpenAiCompatible(llmSettings.Provider) ? llmSettings.Model : ollamaSettings.Model);

var input = ConsoleInput.Read(args, Console.ReadLine, Console.Out.WriteLine);
var runner = app.Services.GetRequiredService<PersonAgentRunner>();
var result = await runner.RunAsync(input).ConfigureAwait(false);
Console.Out.WriteLine(result.Text);
return result.Success ? 0 : 1;

static string PromptPath() => Path.Combine(AppContext.BaseDirectory, "Prompts", "system.prompt");

static string LoadPrompt() => File.ReadAllText(PromptPath());

static void LoadEnv(string directory)
{
    var envPath = Path.Combine(directory, ".env");
    if (File.Exists(envPath))
    {
        EnvFile.ApplyIfMissing(EnvFile.Parse(File.ReadAllText(envPath)));
    }
}
