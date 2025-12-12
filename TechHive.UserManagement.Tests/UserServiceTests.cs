using Microsoft.Extensions.Logging;
using TechHive.UserManagement.Application;
using TechHive.UserManagement.Application.Classes;
using TechHive.UserManagement.Application.POCO;
using TechHive.UserManagement.Infrastructure;
using Xunit;

public class UserServiceTests
{
    private static UserService CreateService()
    {
        var repo = new InMemoryUserRepository();
        var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));
        var logger = loggerFactory.CreateLogger<UserService>();
        return new UserService(repo, logger);
    }

    [Fact]
    public async Task CreateUser_AssignsId_AndPersists()
    {
        var svc = CreateService();
        var id = await svc.CreateAsync(new CreateUserRequest("Ada", "Lovelace", "ada@techhive.local", "R&D", true));
        var user = await svc.GetByIdAsync(id);
        Assert.NotNull(user);
        Assert.Equal(id, user!.Id);
        Assert.Equal("Ada", user.FirstName);
    }

    [Fact]
    public async Task CreateUser_InvalidEmail_ThrowsArgumentException()
    {
        var svc = CreateService();
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await svc.CreateAsync(new CreateUserRequest("Alan", "Turing", "invalid-email", "AI", true)));
    }

    [Fact]
    public async Task UpdateUser_ChangesFields()
    {
        var svc = CreateService();
        var id = await svc.CreateAsync(new CreateUserRequest("Grace", "Hopper", "grace@techhive.local", "Engineering", true));
        var ok = await svc.UpdateAsync(id, new UpdateUserRequest("Grace", "Hopper", "grace@techhive.local", "Platform", false));
        Assert.True(ok);
        var user = await svc.GetByIdAsync(id);
        Assert.Equal("Platform", user!.Department);
        Assert.False(user.IsActive);
    }

    [Fact]
    public async Task UpdateUser_InvalidData_ThrowsArgumentException()
    {
        var svc = CreateService();
        var id = await svc.CreateAsync(new CreateUserRequest("Marie", "Curie", "marie@techhive.local", "Science", true));
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await svc.UpdateAsync(id, new UpdateUserRequest("", "Curie", "marie@techhive.local", "Science", true)));
    }

    [Fact]
    public async Task DeleteUser_RemovesEntry()
    {
        var svc = CreateService();
        var id = await svc.CreateAsync(new CreateUserRequest("Alan", "Turing", "alan@techhive.local", "AI", true));
        var ok = await svc.DeleteAsync(id);
        Assert.True(ok);
        var user = await svc.GetByIdAsync(id);
        Assert.Null(user);
    }

    [Fact]
    public async Task GetPaged_FiltersAndPaginates()
    {
        var svc = CreateService();
        await svc.CreateAsync(new CreateUserRequest("A", "One", "a@techhive.local", "Engineering", true));
        await svc.CreateAsync(new CreateUserRequest("B", "Two", "b@techhive.local", "Engineering", false));
        await svc.CreateAsync(new CreateUserRequest("C", "Three", "c@techhive.local", "HR", true));
        await svc.CreateAsync(new CreateUserRequest("D", "Four", "d@techhive.local", "Engineering", true));

        var page1 = await svc.GetPagedAsync("Engineering", isActive: true, page: 1, pageSize: 2);
        Assert.Equal(2, page1.Items.Count);
        Assert.True(page1.Items.All(u => u.Department == "Engineering" && u.IsActive));

        var page2 = await svc.GetPagedAsync("Engineering", isActive: true, page: 2, pageSize: 2);
        Assert.True(page2.Items.Count <= 2);
    }
}
