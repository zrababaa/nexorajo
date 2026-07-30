using System.ComponentModel.DataAnnotations;

namespace SMPP.Web.ViewModels.Account;

public class LoginViewModel
{
    [Required(ErrorMessage = "The {0} field is required."), Display(Name = "Username or email")]
    public string Identifier { get; set; } = string.Empty;

    [Required(ErrorMessage = "The {0} field is required."), DataType(DataType.Password), Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
