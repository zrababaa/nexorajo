using System.ComponentModel.DataAnnotations;

namespace SMPP.Web.ViewModels.Sending;

public class QuickSendViewModel : SendFormViewModel
{
    [Required(ErrorMessage = "The {0} field is required.")]
    [Display(Name = "Numbers (comma or newline separated)")]
    public string RawNumbers { get; set; } = string.Empty;

    [StringLength(150, ErrorMessage = "The field {0} must be a string with a maximum length of {1}.")]
    [Display(Name = "List name")]
    public string? SaveListName { get; set; }
}
