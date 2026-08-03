namespace SMPP.Domain.Enums;

/// <summary>
/// Legacy <c>under_process.push_type</c>. Values are part of the external SMPP daemon's
/// contract - do not renumber.
/// </summary>
public enum PushType
{
    CampaignNumbers = 1,
    DirectNumbers = 2
}
