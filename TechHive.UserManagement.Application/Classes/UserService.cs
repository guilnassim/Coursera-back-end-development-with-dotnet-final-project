using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechHive.UserManagement.Application.Helper;
using TechHive.UserManagement.Application.Interfaces;
using TechHive.UserManagement.Application.POCO;
using TechHive.UserManagement.Domain;
using Microsoft.Extensions.Logging;


namespace TechHive.UserManagement.Application.Classes;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _repo;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository repo, ILogger<UserService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<int> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        UserRequestValidators.ValidateCreate(request);

        var now = DateTime.UtcNow;
        var user = new User(
            id: 0,
            firstName: request.FirstName.Trim(),
            lastName: request.LastName.Trim(),
            email: request.Email.Trim(),
            department: request.Department.Trim(),
            isActive: request.IsActive,
            createdAtUtc: now,
            updatedAtUtc: now);

        var id = await _repo.AddAsync(user, ct);
        _logger.LogInformation("Created user {UserId} ({Email})", id, user.Email);
        return id;
    }

    public Task<User?> GetByIdAsync(int id, CancellationToken ct = default) => _repo.GetByIdAsync(id, ct);

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default) => _repo.GetAllAsync(ct);

    public async Task<PagedResult<User>> GetPagedAsync(string? department, bool? isActive, int page, int pageSize, CancellationToken ct = default)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize is < 1 or > 500 ? 20 : pageSize;

        var all = await _repo.GetAllAsync(ct);

        var filtered = all.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(department))
            filtered = filtered.Where(u => string.Equals(u.Department, department, StringComparison.OrdinalIgnoreCase));
        if (isActive.HasValue)
            filtered = filtered.Where(u => u.IsActive == isActive.Value);

        var total = filtered.Count();
        var items = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<User>(items, total, page, pageSize);
    }

    public async Task<bool> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct = default)
    {
        UserRequestValidators.ValidateUpdate(request);

        var existing = await _repo.GetByIdAsync(id, ct);
        if (existing is null) return false;

        existing.Update(
            firstName: request.FirstName.Trim(),
            lastName: request.LastName.Trim(),
            email: request.Email.Trim(),
            department: request.Department.Trim(),
            isActive: request.IsActive);

        var updated = await _repo.UpdateAsync(existing, ct);
        if (updated) _logger.LogInformation("Updated user {UserId}", id);
        return updated;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var deleted = await _repo.DeleteAsync(id, ct);
        if (deleted) _logger.LogInformation("Deleted user {UserId}", id);
        return deleted;
    }

}
