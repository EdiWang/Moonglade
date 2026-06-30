using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Features.Dashboard;

namespace Moonglade.Web.Pages.Admin;

public class DashboardModel(IQueryMediator queryMediator) : PageModel
{
    public DashboardStats Stats { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Stats = await queryMediator.QueryAsync(new GetDashboardStatsQuery(), cancellationToken);
    }
}
