using HouseTodoList.Core;
using HouseTodoList.Web;

namespace HouseTodoList.UnitTests;

// Placeholder tests so the harness has unit tests loading each core project. Delete each with its
// placeholder type in the first ticket that adds real code to that project.
public class PlaceholderTests
{
    [Fact]
    public void CorePlaceholderNamesItsProject() => Assert.Equal("HouseTodoList.Core", CorePlaceholder.ProjectName);

    [Fact]
    public void WebPlaceholderNamesItsProject() => Assert.Equal("HouseTodoList.Web", WebPlaceholder.ProjectName);
}
