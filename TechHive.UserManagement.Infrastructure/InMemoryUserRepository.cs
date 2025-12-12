using System.Collections.Concurrent;
using TechHive.UserManagement.Application.Interfaces;
using TechHive.UserManagement.Domain;

namespace TechHive.UserManagement.Infrastructure;

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<int, User> _store = new();
    private int _nextId = 1;

    public Task<User?> GetByIdAsync(int id, CancellationToken ct = default)
        => Task.FromResult(_store.TryGetValue(id, out var user) ? user : null);

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<User>>(_store.Values.ToArray());

    public Task<int> AddAsync(User user, CancellationToken ct = default)
    {
        var id = Interlocked.Increment(ref _nextId) - 1;
        var created = new User(id, user.FirstName, user.LastName, user.Email, user.Department, user.IsActive, user.CreatedAtUtc, user.UpdatedAtUtc);
        _store[id] = created;
        return Task.FromResult(id);
    }

    public Task<bool> UpdateAsync(User user, CancellationToken ct = default)
    {
        if (!_store.ContainsKey(user.Id)) return Task.FromResult(false);
        _store[user.Id] = user;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        => Task.FromResult(_store.TryRemove(id, out _));
}
