
namespace TechHive.UserManagement.Domain;

public sealed class User
{
    public int Id { get; init; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }
    public string Department { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; private set; }

    public User(int id, string firstName, string lastName, string email, string department, bool isActive, DateTime createdAtUtc, DateTime updatedAtUtc)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Department = department;
        IsActive = isActive;
        Id = id;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        Validate();
    }

    public void Update(string firstName, string lastName, string email, string department, bool isActive)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Department = department;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
        Validate();
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(FirstName)) throw new ArgumentException("First name is required.", nameof(FirstName));
        if (string.IsNullOrWhiteSpace(LastName)) throw new ArgumentException("Last name is required.", nameof(LastName));
        if (string.IsNullOrWhiteSpace(Email)) throw new ArgumentException("Email is required.", nameof(Email));
        if (!Email.Contains('@') || Email.StartsWith("@") || Email.EndsWith("@"))
            throw new ArgumentException("Email format is invalid.", nameof(Email));
        if (string.IsNullOrWhiteSpace(Department)) throw new ArgumentException("Department is required.", nameof(Department));
    }
}
