using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.FeatureManagement;
using Moonglade.Configuration.Settings;
using Moonglade.Data;
using Moonglade.Data.Entities;
using X.PagedList;

namespace Moonglade.Web.Pages.Admin
{
    public class AuditLogsModel : PageModel
    {
        private readonly IFeatureManager _featureManager;
        private readonly IBlogAudit _blogAudit;

        public StaticPagedList<AuditLogEntity> Entries { get; set; }

        public AuditLogsModel(IFeatureManager featureManager, IBlogAudit blogAudit)
        {
            _featureManager = featureManager;
            _blogAudit = blogAudit;
        }

        public async Task<IActionResult> OnGetAsync(int pageIndex = 1)
        {
            var flag = await _featureManager.IsEnabledAsync(nameof(FeatureFlags.EnableAudit));
            if (!flag) return Forbid();

            if (pageIndex <= 0) return BadRequest();

            var skip = (pageIndex - 1) * 20;

            var (entries, count) = await _blogAudit.GetAuditEntries(skip, 20);
            Entries = new(entries, pageIndex, 20, count);
            return Page();
        }
    }
}
