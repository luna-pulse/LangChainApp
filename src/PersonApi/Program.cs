using PersonApi.Endpoints;
using PersonApi.Store;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IPersonStore, InMemoryPersonStore>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Учёт физических лиц",
        Version = "v1",
        Description = "Minimal API: CreatePerson, GetPerson, UpdatePerson, ListPersons.",
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Учёт физических лиц v1");
    options.RoutePrefix = "swagger";
});

app.MapPersonEndpoints();
app.Run();

public partial class Program;
