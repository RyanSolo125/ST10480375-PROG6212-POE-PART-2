using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public class RegisterDto
{
    /// <example>Amahle Dlamini</example>
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    /// <example>amahle@example.com</example>
    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    /// <example>Password123!</example>
    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Must be "Organiser" or "Participant".</summary>
    /// <example>Participant</example>
    [Required]
    public string Role { get; set; } = string.Empty;

    /// <example>0712345678</example>
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
}

public class LoginDto
{
    /// <example>amahle.dlamini@gmail.com</example>
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <example>Password123!</example>
    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    /// <summary>The session ID. The browser also receives it as the session cookie.</summary>
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class UpdateProfileDto
{
    /// <example>Amahle N. Dlamini</example>
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    /// <example>0710000000</example>
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
}

public class UserDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; }

    public static UserDto FromModel(User u) => new UserDto
    {
        UserId = u.UserId,
        FullName = u.FullName,
        Email = u.Email,
        Role = u.Role,
        PhoneNumber = u.PhoneNumber,
        CreatedAt = u.CreatedAt
    };
}
