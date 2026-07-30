using System.ComponentModel.DataAnnotations;
using SMPP.Domain.Enums;

namespace SMPP.Web.ViewModels.SpamKeywords;

public class CreateSpamKeywordViewModel
{
    [Required(ErrorMessage = "The {0} field is required.")]
    [StringLength(255, ErrorMessage = "The field {0} must be a string with a maximum length of {1}.")]
    [Display(Name = "Keyword")]
    public string Keyword { get; set; } = string.Empty;

    [Display(Name = "Type")]
    public SpamKeywordType KeywordType { get; set; }
}
