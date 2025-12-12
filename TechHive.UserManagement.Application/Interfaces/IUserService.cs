using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechHive.UserManagement.Domain;
using TechHive.UserManagement.Application.POCO;

namespace TechHive.UserManagement.Application.Interfaces
{

    public interface IUserService
    {
        Task<int> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
        Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default);
        Task<PagedResult<User>> GetPagedAsync(string? department, bool? isActive, int page, int pageSize, CancellationToken ct = default);
        Task<bool> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    }

}
