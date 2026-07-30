using System.ComponentModel.DataAnnotations;

namespace SMPP.Web.ViewModels.Account;

public class ResetPasswordViewModel
{
    [Required(ErrorMessage = "The {0} field is required.")]
    [EmailAddress(ErrorMessage = "The {0} field is not a valid e-mail address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "The {0} field is required.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "The {0} field is required."), DataType(DataType.Password), Display(Name = "New password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "The {0} field is required."), DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "'{0}' and '{1}' do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
