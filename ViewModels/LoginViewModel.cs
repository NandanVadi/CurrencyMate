using System.ComponentModel.DataAnnotations;

namespace CurrencyMini.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Enter your email.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Display(Name = "Keep me logged in")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
