using System.Net;
using Hindstorm.AspNetCore;
using Hindstorm.Tests.EndpointFixtures;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Hindstorm.Tests;

// Integration tests for the ASP.NET Core endpoint. This is a hosting seam, not a pure contract unit:
// it drives MapHindstorm through a real (in-memory) request pipeline to assert the format routing,
// including the negative path where an unsupported format is rejected.
public class EndpointIntegrationTests
{
    private static async Task<WebApplication> StartAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();
        app.MapHindstorm("/domain-model", o =>
        {
            o.Assemblies.Add(typeof(Account).Assembly);
            o.ConfigureScanner = s => s.TypeFilter =
                t => t.Namespace == typeof(Account).Namespace;
        });
        await app.StartAsync();
        return app;
    }

    [Fact]
    public async Task Default_format_is_json_and_returns_the_model()
    {
        await using var app = await StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/domain-model");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("\"nodes\"", body);
        Assert.Contains("Account", body);
    }

    [Fact]
    public async Task Mermaid_format_returns_flowchart()
    {
        await using var app = await StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/domain-model?format=mermaid");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("flowchart LR", body);
    }

    [Fact]
    public async Task Dot_format_returns_digraph()
    {
        await using var app = await StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/domain-model?format=dot");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("digraph DomainModel", body);
    }

    [Fact]
    public async Task Unknown_format_is_rejected_with_400()
    {
        await using var app = await StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/domain-model?format=svg");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
