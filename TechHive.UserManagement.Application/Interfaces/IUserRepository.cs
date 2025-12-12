using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechHive.UserManagement.Domain;

namespace TechHive.UserManagement.Application.Interfaces
{

    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default);
        Task<int> AddAsync(User user, CancellationToken ct = default);
        Task<bool> UpdateAsync(User user, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    }

}
