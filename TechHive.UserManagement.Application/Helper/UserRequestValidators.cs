using System.Text.RegularExpressions;
using TechHive.UserManagement.Application.POCO;

namespace TechHive.UserManagement.Application.Helper;

public static class UserRequestValidators
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public static void ValidateCreate(CreateUserRequest r)
    {
        if (r is null) throw new ArgumentException("Request body is required.");
        if (string.IsNullOrWhiteSpace(r.FirstName)) throw new ArgumentException("FirstName is required.");
        if (string.IsNullOrWhiteSpace(r.LastName)) throw new ArgumentException("LastName is required.");
        if (string.IsNullOrWhiteSpace(r.Email)) throw new ArgumentException("Email is required.");
        if (!EmailRegex.IsMatch(r.Email)) throw new ArgumentException("Email format is invalid.");
        if (string.IsNullOrWhiteSpace(r.Department)) throw new ArgumentException("Department is required.");
    }

    public static void ValidateUpdate(UpdateUserRequest r)
    {
        if (r is null) throw new ArgumentException("Request body is required.");
        if (string.IsNullOrWhiteSpace(r.FirstName)) throw new ArgumentException("FirstName is required.");
        if (string.IsNullOrWhiteSpace(r.LastName)) throw new ArgumentException("LastName is required.");
        if (string.IsNullOrWhiteSpace(r.Email)) throw new ArgumentException("Email is required.");
        if (!EmailRegex.IsMatch(r.Email)) throw new ArgumentException("Email format is invalid.");
        if (string.IsNullOrWhiteSpace(r.Department)) throw new ArgumentException("Department is required.");
    }
}
