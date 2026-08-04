using SMPP.Application.AdminBudget;
using SMPP.Application.Common;

namespace SMPP.Web.ViewModels.AdminBudget;

public class AdminBudgetPageViewModel
{
    public decimal Balance { get; set; }
    public PagedResult<AdminBudgetLogRowDto> Log { get; set; } = new();
    public SetAdminBudgetViewModel Form { get; set; } = new();
}
