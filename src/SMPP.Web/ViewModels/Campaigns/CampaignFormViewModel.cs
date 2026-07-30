using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SMPP.Web.ViewModels.Campaigns;

public class CampaignFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "The {0} field is required.")]
    [StringLength(150, ErrorMessage = "The field {0} must be a string with a maximum length of {1}.")]
    [Display(Name = "Campaign name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "The {0} field is required.")]
    [StringLength(50, MinimumLength = 4, ErrorMessage = "The field {0} must be a string with a minimum length of {2} and a maximum length of {1}.")]
    [Display(Name = "Campaign code")]
    public string ExternalCampaignCode { get; set; } = string.Empty;

    [Display(Name = "Numbers (comma or newline separated)")]
    public string? PastedNumbers { get; set; }

    [Display(Name = "Or upload a CSV file")]
    public IFormFile? CsvFile { get; set; }

    [Range(0, 3600, ErrorMessage = "The field {0} must be between {1} and {2}.")]
    [Display(Name = "Min delay (seconds)")]
    public int? SendSpeedMinSeconds { get; set; }

    [Range(0, 3600, ErrorMessage = "The field {0} must be between {1} and {2}.")]
    [Display(Name = "Max delay (seconds)")]
    public int? SendSpeedMaxSeconds { get; set; }
}
