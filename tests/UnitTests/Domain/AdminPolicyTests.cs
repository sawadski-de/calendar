using Domain;
using Xunit;

namespace UnitTests.Domain;

public class AdminPolicyTests
{
    [Fact]
    public void CanDemoteLastAdmin_returns_false_when_only_one_admin_remains()
    {
        Assert.False(AdminPolicy.CanDemoteLastAdmin(1));
    }

    [Fact]
    public void CanDemoteLastAdmin_returns_true_when_more_than_one_admin_remains()
    {
        Assert.True(AdminPolicy.CanDemoteLastAdmin(2));
    }

    [Fact]
    public void CanDemoteLastAdmin_returns_false_when_zero_admins_exist()
    {
        // Defensive: a zero-admin state should never occur, but the rule must not accidentally allow it.
        Assert.False(AdminPolicy.CanDemoteLastAdmin(0));
    }
}
