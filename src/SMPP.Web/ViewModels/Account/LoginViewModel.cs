using System.ComponentModel.DataAnnotations;

namespace SMPP.Web.ViewModels.Account;

public class LoginViewModel
{
    [Required, Display(Name = "Username or email")]
    public string Identifier { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
