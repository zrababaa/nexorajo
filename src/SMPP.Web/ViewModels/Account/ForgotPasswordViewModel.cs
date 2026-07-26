using System.ComponentModel.DataAnnotations;

namespace SMPP.Web.ViewModels.Account;

public class ForgotPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}
