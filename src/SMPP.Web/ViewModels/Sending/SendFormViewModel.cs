using System.ComponentModel.DataAnnotations;

namespace SMPP.Web.ViewModels.Sending;

/// <summary>
/// The message + Sender ID half of both send screens, shared so _MessageField and _SenderIdField
/// can render either one.
///
/// The Sender ID arrives in one of two fields because the account may be restricted to a list:
/// <see cref="SenderId"/> is the dropdown, <see cref="FreeSenderId"/> the free-text box, and
/// <see cref="UseFreeSenderId"/> says which one the user meant. Two named fields rather than one
/// field the browser rewrites, so the form still posts something sane without JavaScript.
/// Whichever arrives is checked against the account's policy on the server before it is used.
/// </summary>
public abstract class SendFormViewModel
{
    [Required(ErrorMessage = "The {0} field is required.")]
    [Display(Name = "Message")]
    public string Message { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "The field {0} must be a string with a maximum length of {1}.")]
    [Display(Name = "Sender ID")]
    public string? SenderId { get; set; }

    [StringLength(20, ErrorMessage = "The field {0} must be a string with a maximum length of {1}.")]
    [Display(Name = "Own Sender ID")]
    public string? FreeSenderId { get; set; }

    [Display(Name = "Use my own Sender ID")]
    public bool UseFreeSenderId { get; set; }

    public string EffectiveSenderId => (UseFreeSenderId ? FreeSenderId : SenderId)?.Trim() ?? string.Empty;
}
