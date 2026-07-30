using System.ComponentModel.DataAnnotations;

namespace SMPP.Web.ViewModels.Sending;

public class QuickSendViewModel
{
    [Required(ErrorMessage = "The {0} field is required.")]
    [Display(Name = "Numbers (comma or newline separated)")]
    public string RawNumbers { get; set; } = string.Empty;

    [Required(ErrorMessage = "The {0} field is required.")]
    [Display(Name = "Message")]
    public string Message { get; set; } = string.Empty;

    [Required(ErrorMessage = "The {0} field is required.")]
    [StringLength(20, ErrorMessage = "The field {0} must be a string with a maximum length of {1}.")]
    [Display(Name = "Sender ID")]
    public string SenderId { get; set; } = string.Empty;

    [StringLength(150, ErrorMessage = "The field {0} must be a string with a maximum length of {1}.")]
    [Display(Name = "List name")]
    public string? SaveListName { get; set; }
}
