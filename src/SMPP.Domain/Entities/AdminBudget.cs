namespace SMPP.Domain.Entities;

/// <summary>
/// The shared pool of credits a Superadmin can hand out to accounts. Singleton row (Id = 1) -
/// see IAdminBudgetService for the only code allowed to mutate it.
/// </summary>
public class AdminBudget
{
    public int Id { get; set; }
    public decimal Balance { get; set; }
}
