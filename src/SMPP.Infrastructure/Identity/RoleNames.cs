namespace SMPP.Infrastructure.Identity;

public static class RoleNames
{
    public const string Superadmin = "Superadmin";
    public const string WhiteLabelAdmin = "WhiteLabelAdmin";
    public const string EndUser = "EndUser";

    public static readonly string[] All = { Superadmin, WhiteLabelAdmin, EndUser };
}
