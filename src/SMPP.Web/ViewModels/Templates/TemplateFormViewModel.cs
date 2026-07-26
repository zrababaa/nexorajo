using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SMPP.Web.ViewModels.Templates;

public class TemplateFormViewModel
{
    public int? Id { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 3)]
    [Display(Name = "Template code")]
    public string TemplateCode { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Message")]
    public string MessageBody { get; set; } = string.Empty;

    [Display(Name = "Recipient CSV (required on create)")]
    public IFormFile? CsvFile { get; set; }

    public string? ExistingCsvFilePath { get; set; }
}
