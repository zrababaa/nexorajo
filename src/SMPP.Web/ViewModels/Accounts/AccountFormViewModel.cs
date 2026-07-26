using System.ComponentModel.DataAnnotations;

namespace SMPP.Web.ViewModels.Accounts;

public class CreateAccountViewModel
{
    [Required, StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(150)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Mobile number")]
    public string? MobileNo { get; set; }

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    [Display(Name = "Initial credits")]
    public decimal InitialBalance { get; set; }

    [Range(0, double.MaxValue)]
    [Display(Name = "Rate per message")]
    public decimal RatePerMessage { get; set; }

    [Display(Name = "Sender ID")]
    public string? SenderId { get; set; }
}

public class EditAccountViewModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(150)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Mobile number")]
    public string? MobileNo { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; }

    [Range(0, double.MaxValue)]
    [Display(Name = "Rate per message")]
    public decimal RatePerMessage { get; set; }

    [Display(Name = "Sender ID")]
    public string? SenderId { get; set; }

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
