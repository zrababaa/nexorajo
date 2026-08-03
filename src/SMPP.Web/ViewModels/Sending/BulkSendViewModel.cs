using System.ComponentModel.DataAnnotations;

namespace SMPP.Web.ViewModels.Sending;

public class BulkSendViewModel : SendFormViewModel
{
    [Required(ErrorMessage = "The {0} field is required.")]
    [Display(Name = "Campaign")]
    public int CampaignId { get; set; }
}
