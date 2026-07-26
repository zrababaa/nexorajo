using System.ComponentModel.DataAnnotations;

namespace SMPP.Web.ViewModels.Sending;

public class QuickSendViewModel
{
    [Required]
    [Display(Name = "Numbers (comma or newline separated)")]
    public string RawNumbers { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Message")]
    public string Message { get; set; } = string.Empty;

    [Required, StringLength(20)]
    [Display(Name = "Sender ID")]
    public string SenderId { get; set; } = string.Empty;
}
