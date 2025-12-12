
using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.Contracts;

/// <summary>
/// Request body for issuing a JWT.
/// Keep it minimal: the subject (typically an email or unique user id).
/// </summary>
public sealed record TokenRequest(
    [property: Required, EmailAddress, StringLength(320)]
    string Subject
);
