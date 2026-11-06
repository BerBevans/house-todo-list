using System.Net;
using HouseTodoList.Web;
using Microsoft.Extensions.Hosting;

namespace HouseTodoList.UnitTests;

public class ProgramTests
{
    [Fact]
    public async Task BuildAppBuildsAnAppForTheCurrentEnvironment()
    {
        await using var app = Program.BuildApp(["--environment", "Testing"]);

        Assert.True(app.Environment.IsEnvironment("Testing"));
    }

    [Fact]
    public async Task TheHealthEndpointSaysTheAppIsHealthy()
    {
        await using var app = Program.BuildApp(["--environment", "Testing", "--urls", "http://127.0.0.1:0"]);
        await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };

        using var response = await client.GetAsync(new Uri(Program.HealthPath, UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
