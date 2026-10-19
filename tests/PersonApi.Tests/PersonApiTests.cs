using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PersonApi.Tests;

public sealed class PersonApiTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public PersonApiTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_Get_Update_List_And_NotFound()
    {
        var created = await _client.PostAsJsonAsync("/persons", new
        {
            lastName = "Иванов",
            firstName = "Иван",
            middleName = "Иванович",
            birthDate = "1990-05-01",
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var person = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = person.GetProperty("id").GetInt32();
        Assert.True(id > 0);
        Assert.Equal($"/persons/{id}", created.Headers.Location?.OriginalString);

        var fetched = await _client.GetFromJsonAsync<JsonElement>($"/persons/{id}");
        Assert.Equal("Иванов", fetched.GetProperty("lastName").GetString());
        Assert.Equal("Иван", fetched.GetProperty("firstName").GetString());

        var updated = await _client.PutAsJsonAsync($"/persons/{id}", new { lastName = "Петров" });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var updatedBody = await updated.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Петров", updatedBody.GetProperty("lastName").GetString());
        Assert.Equal("Иван", updatedBody.GetProperty("firstName").GetString());

        var list = await _client.GetFromJsonAsync<JsonElement>("/persons");
        Assert.True(list.GetProperty("count").GetInt32() >= 1);
        Assert.Contains(list.GetProperty("items").EnumerateArray(), item => item.GetProperty("id").GetInt32() == id);

        var missing = await _client.GetAsync("/persons/999999");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        var missingBody = await missing.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(404, missingBody.GetProperty("status").GetInt32());
        Assert.Equal("Физлицо не найдено", missingBody.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Create_WithoutRequiredFields_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/persons", new { firstName = " " });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(400, body.GetProperty("status").GetInt32());
        Assert.True(body.GetProperty("errors").TryGetProperty("lastName", out _));
        Assert.True(body.GetProperty("errors").TryGetProperty("firstName", out _));
    }

    [Fact]
    public async Task Create_WithFutureBirthDate_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/persons", new
        {
            lastName = "Иванов",
            firstName = "Иван",
            birthDate = "2999-01-01",
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_MissingPerson_Returns404()
    {
        var response = await _client.PutAsJsonAsync("/persons/424242", new { firstName = "Анна" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithoutFields_Returns400()
    {
        var created = await _client.PostAsJsonAsync("/persons", new { lastName = "Сидоров", firstName = "Сидор" });
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var response = await _client.PutAsJsonAsync($"/persons/{id}", new { });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_NonPositiveId_Returns400()
    {
        var response = await _client.GetAsync("/persons/0");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ParallelCreates_HaveDistinctIds()
    {
        string[] names =
        [
            "Альфа", "Бета", "Гамма", "Дельта", "Эпсилон", "Зета", "Эта", "Тета", "Йота", "Каппа",
            "Лямбда", "Мю", "Ню", "Кси", "Омикрон", "Пи", "Ро", "Сигма", "Тау", "Ипсилон",
        ];
        var tasks = names.Select(name => _client.PostAsJsonAsync("/persons", new
        {
            lastName = "Параллельный",
            firstName = name,
        }));
        var responses = await Task.WhenAll(tasks);
        var ids = new List<int>();
        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            ids.Add(body.GetProperty("id").GetInt32());
        }

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public async Task Swagger_DescribesFourOperations()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"/persons\"", body, StringComparison.Ordinal);
        Assert.Contains("\"/persons/{id}\"", body, StringComparison.Ordinal);
        Assert.Contains("CreatePerson", body, StringComparison.Ordinal);
        Assert.Contains("GetPerson", body, StringComparison.Ordinal);
        Assert.Contains("UpdatePerson", body, StringComparison.Ordinal);
        Assert.Contains("ListPersons", body, StringComparison.Ordinal);
    }
}
