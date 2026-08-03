using System.ComponentModel.DataAnnotations;

namespace SMPP.Web.ViewModels.Accounts;

public class CreateAccountViewModel
{
    [Required(ErrorMessage = "The {0} field is required.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "The field {0} must be a string with a minimum length of {2} and a maximum length of {1}.")]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "The {0} field is required.")]
    [EmailAddress(ErrorMessage = "The {0} field is not a valid e-mail address.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "The {0} field is required.")]
    [StringLength(150, ErrorMessage = "The field {0} must be a string with a maximum length of {1}.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Mobile number")]
    public string? MobileNo { get; set; }

    [Required(ErrorMessage = "The {0} field is required."), DataType(DataType.Password), Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "The field {0} must be between {1} and {2}.")]
    [Display(Name = "Initial credits")]
    public decimal InitialBalance { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "The field {0} must be between {1} and {2}.")]
    [Display(Name = "Credits per message part")]
    public decimal RatePerMessage { get; set; } = 1m;

    [StringLength(500, ErrorMessage = "The field {0} must be a string with a maximum length of {1}.")]
    [Display(Name = "Sender IDs (comma separated)")]
    public string? SenderIds { get; set; }

    [Display(Name = "Allow the account to type its own Sender ID")]
    public bool AllowFreeSenderId { get; set; }
}

public class EditAccountViewModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "The {0} field is required.")]
    [StringLength(150, ErrorMessage = "The field {0} must be a string with a maximum length of {1}.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Mobile number")]
    public string? MobileNo { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "The field {0} must be between {1} and {2}.")]
    [Display(Name = "Credits per message part")]
    public decimal RatePerMessage { get; set; }

    [StringLength(500, ErrorMessage = "The field {0} must be a string with a maximum length of {1}.")]
    [Display(Name = "Sender IDs (comma separated)")]
    public string? SenderIds { get; set; }

    [Display(Name = "Allow the account to type its own Sender ID")]
    public bool AllowFreeSenderId { get; set; }

    [Display(Name = "Valid from")]
    public DateOnly? DateFrom { get; set; }

    [Display(Name = "Valid to")]
    public DateOnly? DateTo { get; set; }

    public decimal Balance { get; set; }
    public string? ApiToken { get; set; }
    public string? ApiSecret { get; set; }
}

public class AdjustBalanceViewModel
{
    public int AccountId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }
}
