using Application.Accounts;
using Domain;
using Xunit;

namespace UnitTests.Application;

public class RoleChangeServiceTests
{
    [Fact]
    public async Task ChangeRoleAsync_rejects_demoting_the_last_admin()
    {
        var admin = new Person(Guid.NewGuid(), "admin@example.com", PersonRole.Admin);
        var repository = new FakePersonRepository(admin);
        var sut = new RoleChangeService(repository);

        var result = await sut.ChangeRoleAsync(admin.Id, PersonRole.Member);

        Assert.False(result.Succeeded);
        Assert.Equal("last-admin-cannot-be-demoted", result.ErrorCode);
        Assert.Equal(PersonRole.Admin, (await repository.GetByIdAsync(admin.Id))!.Role);
    }

    [Fact]
    public async Task ChangeRoleAsync_allows_demoting_an_admin_when_another_admin_remains()
    {
        var admin1 = new Person(Guid.NewGuid(), "admin1@example.com", PersonRole.Admin);
        var admin2 = new Person(Guid.NewGuid(), "admin2@example.com", PersonRole.Admin);
        var repository = new FakePersonRepository(admin1, admin2);
        var sut = new RoleChangeService(repository);

        var result = await sut.ChangeRoleAsync(admin1.Id, PersonRole.Member);

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorCode);
        Assert.Equal(PersonRole.Member, (await repository.GetByIdAsync(admin1.Id))!.Role);
    }

    [Fact]
    public async Task ChangeRoleAsync_promoting_a_member_to_admin_never_hits_the_last_admin_check()
    {
        var admin = new Person(Guid.NewGuid(), "admin@example.com", PersonRole.Admin);
        var member = new Person(Guid.NewGuid(), "member@example.com", PersonRole.Member);
        var repository = new FakePersonRepository(admin, member);
        var sut = new RoleChangeService(repository);

        var result = await sut.ChangeRoleAsync(member.Id, PersonRole.Admin);

        Assert.True(result.Succeeded);
        Assert.Equal(PersonRole.Admin, (await repository.GetByIdAsync(member.Id))!.Role);
    }

    [Fact]
    public async Task ChangeRoleAsync_returns_not_found_for_unknown_person()
    {
        var repository = new FakePersonRepository();
        var sut = new RoleChangeService(repository);

        var result = await sut.ChangeRoleAsync(Guid.NewGuid(), PersonRole.Admin);

        Assert.False(result.Succeeded);
        Assert.Equal("person-not-found", result.ErrorCode);
    }
}
