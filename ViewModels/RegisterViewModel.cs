using System.ComponentModel.DataAnnotations;

namespace CurrencyMini.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Enter your email.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(254)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Choose a password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";
}
