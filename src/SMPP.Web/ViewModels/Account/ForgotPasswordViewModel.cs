using System.ComponentModel.DataAnnotations;

namespace SMPP.Web.ViewModels.Account;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "The {0} field is required.")]
    [EmailAddress(ErrorMessage = "The {0} field is not a valid e-mail address.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
}
