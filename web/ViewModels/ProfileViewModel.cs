using System.ComponentModel.DataAnnotations;

namespace TechSpecs.ViewModels;

public class ProfileViewModel
{
    [Required, MaxLength(100), Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsGoogleAccount { get; set; }

    [DataType(DataType.Password), Display(Name = "Current Password")]
    public string? CurrentPassword { get; set; }

    [DataType(DataType.Password), MinLength(8), Display(Name = "New Password")]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password), Compare(nameof(NewPassword)), Display(Name = "Confirm New Password")]
    public string? ConfirmPassword { get; set; }
}
