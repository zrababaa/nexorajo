using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SMPP.Web.ViewModels.Campaigns;

public class CampaignFormViewModel
{
    public int? Id { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 4)]
    [Display(Name = "Campaign code")]
    public string ExternalCampaignCode { get; set; } = string.Empty;

    [Display(Name = "Numbers (comma or newline separated)")]
    public string? PastedNumbers { get; set; }

    [Display(Name = "Or upload a CSV file")]
    public IFormFile? CsvFile { get; set; }

    [Range(0, 3600)]
    [Display(Name = "Min delay (seconds)")]
    public int? SendSpeedMinSeconds { get; set; }

    [Range(0, 3600)]
    [Display(Name = "Max delay (seconds)")]
    public int? SendSpeedMaxSeconds { get; set; }
}
