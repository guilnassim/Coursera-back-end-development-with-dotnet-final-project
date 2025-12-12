using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechHive.UserManagement.Application.POCO
{
    public record UpdateUserRequest(string FirstName, string LastName, string Email, string Department, bool IsActive);
}
