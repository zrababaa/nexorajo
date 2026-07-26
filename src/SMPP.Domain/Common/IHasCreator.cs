namespace SMPP.Domain.Common;

/// <summary>
/// Marks an entity as owned by whichever user created it, forming the
/// Superadmin -&gt; WhiteLabelAdmin -&gt; EndUser ownership chain used for scoping queries.
/// </summary>
public interface IHasCreator
{
    int CreatedByUserId { get; set; }
}
