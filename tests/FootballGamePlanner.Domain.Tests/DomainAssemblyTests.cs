using FootballGamePlanner.Domain;

namespace FootballGamePlanner.Domain.Tests;

public sealed class DomainAssemblyTests
{
    [Fact]
    public void Domain_assembly_is_loadable()
    {
        var name = typeof(DomainAssemblyMarker).Assembly.GetName().Name;

        Assert.Equal("FootballGamePlanner.Domain", name);
    }
}
