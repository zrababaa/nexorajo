using System.ComponentModel.DataAnnotations;

namespace SMPP.Web.ViewModels.AdminBudget;

public class SetAdminBudgetViewModel
{
    [Range(0, double.MaxValue, ErrorMessage = "The budget cannot be negative.")]
    [Display(Name = "New budget total")]
    public decimal NewBalance { get; set; }

    [StringLength(500, ErrorMessage = "The field {0} must be a string with a maximum length of {1}.")]
    [Display(Name = "Note (optional)")]
    public string? Note { get; set; }
}
