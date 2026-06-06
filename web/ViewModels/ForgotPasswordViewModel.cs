using System.ComponentModel.DataAnnotations;

namespace TechSpecs.ViewModels;

public class ForgotPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, MinLength(8), DataType(DataType.Password), Display(Name = "New Password")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(Password)), Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
