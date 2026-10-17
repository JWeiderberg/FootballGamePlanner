namespace FootballGamePlanner.Web.Tests;

public sealed class WebAssemblyTests
{
    [Fact]
    public void Web_assembly_is_loadable()
    {
        var name = typeof(Program).Assembly.GetName().Name;

        Assert.Equal("FootballGamePlanner.Web", name);
    }
}
