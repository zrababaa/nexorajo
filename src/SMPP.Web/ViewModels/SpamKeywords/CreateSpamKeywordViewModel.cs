using System.ComponentModel.DataAnnotations;
using SMPP.Domain.Enums;

namespace SMPP.Web.ViewModels.SpamKeywords;

public class CreateSpamKeywordViewModel
{
    [Required, StringLength(255)]
    public string Keyword { get; set; } = string.Empty;

    [Display(Name = "Type")]
    public SpamKeywordType KeywordType { get; set; }
}
