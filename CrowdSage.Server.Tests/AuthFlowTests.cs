using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CrowdSage.Server.Models;
using CrowdSage.Server.Models.Outputs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CrowdSage.Server.Tests;

// Runs the real pipeline (Identity + OpenIddict server and validation) over an in-memory
// database, covering register -> password grant -> bearer-authenticated request.
public class AuthFlowTests(AuthFlowTests.CrowdsageFactory factory) : IClassFixture<AuthFlowTests.CrowdsageFactory>
{
    private const string Password = "Passw0rd!";

    public class CrowdsageFactory : WebApplicationFactory<Program>
    {
        private readonly string databaseName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                // Drop the Npgsql registration so only one provider is configured.
                services.RemoveAll<DbContextOptions<CrowdsageDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<CrowdsageDbContext>>();
                services.AddDbContext<CrowdsageDbContext>(options =>
                {
                    options.UseInMemoryDatabase(databaseName);
                    options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                    options.UseOpenIddict();
                });
            });
        }
    }

    private HttpClient CreateClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static string NewUserName() => "user" + Guid.NewGuid().ToString("N")[..12];

    private static async Task RegisterAsync(HttpClient client, string userName)
    {
        var response = await client.PostAsJsonAsync("/register", new
        {
            userName,
            email = $"{userName}@example.com",
            password = Password
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Mirrors the SPA's login mutation: form-encoded password grant, no client_id, no scope.
    private static Task<HttpResponseMessage> RequestTokenAsync(HttpClient client, string userName, string password) =>
        client.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = userName,
            ["password"] = password,
        }));

    private static async Task<string> LoginAsync(HttpClient client, string userName)
    {
        var response = await RequestTokenAsync(client, userName, Password);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);

        using var json = JsonDocument.Parse(body);
        return json.RootElement.GetProperty("access_token").GetString()!;
    }

    private async Task<string> GetUserIdAsync(string userName)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<CrowdsageUser>>();
        var user = await userManager.FindByNameAsync(userName);
        return user!.Id;
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401WithoutCookieRedirect()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task Me_WithInvalidToken_Returns401()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-token");

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_WithWrongPassword_ReturnsInvalidGrant()
    {
        var client = CreateClient();
        var userName = NewUserName();
        await RegisterAsync(client, userName);

        var response = await RequestTokenAsync(client, userName, "wrong-password");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("invalid_grant", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Me_WithTokenFromPasswordGrant_ReturnsThatUser()
    {
        var client = CreateClient();
        var alice = NewUserName();
        var bob = NewUserName();
        await RegisterAsync(client, alice);
        await RegisterAsync(client, bob);

        foreach (var userName in new[] { alice, bob })
        {
            var token = await LoginAsync(client, userName);
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
            Assert.Equal(await GetUserIdAsync(userName), profile!.Id);
            Assert.Equal(userName, profile.UserName);
        }
    }

    [Fact]
    public async Task AddQuestion_WithToken_AttributesQuestionToCaller()
    {
        var client = CreateClient();
        var userName = NewUserName();
        await RegisterAsync(client, userName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, userName));

        var response = await client.PostAsJsonAsync("/api/questions", new { title = "Bearer auth", content = "Does it work?" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var questions = await client.GetFromJsonAsync<List<UserQuestionSummaryDto>>($"/api/users/{await GetUserIdAsync(userName)}/questions");
        Assert.Equal("Bearer auth", Assert.Single(questions!).Title);
    }
}
