namespace SMPP.Domain.Enums;

/// <summary>
/// Legacy <c>under_process.priority</c>, lowest number drains first. Values are part of the
/// external SMPP daemon's contract - do not renumber.
/// </summary>
public enum SendPriority
{
    Interactive = 1,
    Template = 2,
    Api = 3
}
