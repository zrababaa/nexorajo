using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using SMPP.Domain.Enums;

namespace SMPP.Web.ViewModels.Payments;

public class SubmitPaymentViewModel
{
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Display(Name = "Payment method")]
    public PaymentMethod Method { get; set; }

    [Display(Name = "Transaction reference")]
    [StringLength(150, ErrorMessage = "The field {0} must be a string with a maximum length of {1}.")]
    public string? TransactionRef { get; set; }

    [Display(Name = "Proof of payment (optional)")]
    public IFormFile? ProofFile { get; set; }

    [Display(Name = "Note (optional)")]
    public string? Note { get; set; }
}

public class RejectPaymentViewModel
{
    public int PaymentId { get; set; }

    [Display(Name = "Reason (optional)")]
    [StringLength(500, ErrorMessage = "The field {0} must be a string with a maximum length of {1}.")]
    public string? ReviewNote { get; set; }
}
