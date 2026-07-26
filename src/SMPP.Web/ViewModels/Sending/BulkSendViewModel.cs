using System.ComponentModel.DataAnnotations;

namespace SMPP.Web.ViewModels.Sending;

public class BulkSendViewModel
{
    [Required]
    [Display(Name = "Campaign")]
    public int CampaignId { get; set; }

    [Required]
    [Display(Name = "Message")]
    public string Message { get; set; } = string.Empty;

    [Required, StringLength(20)]
    [Display(Name = "Sender ID")]
    public string SenderId { get; set; } = string.Empty;
}
