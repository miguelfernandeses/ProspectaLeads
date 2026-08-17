using System.ComponentModel.DataAnnotations;

namespace ProspeccaoLeads.Application.DTOs.Auth;

public class LoginDto
{
    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.")]
    public string Password { get; set; } = string.Empty;
}

public class RegisterDto
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [MinLength(2, ErrorMessage = "O nome deve ter no mínimo 2 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "A confirmação de senha é obrigatória.")]
    [Compare(nameof(Password), ErrorMessage = "As senhas não conferem.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ResetPasswordDto
{
    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    public string Email { get; set; } = string.Empty;
}

public class UserSessionDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class ChangePasswordDto
{
    [Required(ErrorMessage = "A nova senha é obrigatória.")]
    [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "A confirmação de senha é obrigatória.")]
    [Compare(nameof(NewPassword), ErrorMessage = "As senhas não conferem.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

public class UpdateProfileDto
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [MinLength(2, ErrorMessage = "O nome deve ter no mínimo 2 caracteres.")]
    public string Name { get; set; } = string.Empty;

    public string? DefaultNiche { get; set; }
    public string? DefaultCity { get; set; }
    public string? DefaultState { get; set; }
}
