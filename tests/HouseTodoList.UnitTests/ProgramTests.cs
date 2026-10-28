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
}
