using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Configuration;
using System.Collections.Immutable;
using System.Text.Json;

namespace Moonglade.Web.Tests.Structure;

[ApiController]
[Route("s0/probe")]
public class StructuralProbeController(SnapshotProbeStore store) : Controller
{
    [HttpPost("feed")]
    public ImmutableFeedProbe Feed(ImmutableFeedProbe model) => model;

    [HttpPost("feed-form")]
    public ImmutableFeedProbe FeedForm([FromForm] ImmutableFeedProbe model) => model;

    [HttpPost("menus")]
    public IActionResult Menus(CustomMenuSettingsJsonModel model)
    {
        ImmutableList<ImmutableMenuProbe?>? menus;
        try
        {
            menus = string.IsNullOrWhiteSpace(model.MenuJson) ? null : model.MenuJson.FromJson<ImmutableList<ImmutableMenuProbe?>>();
        }
        catch (JsonException)
        {
            ModelState.AddModelError(nameof(model.MenuJson), "Invalid menu JSON.");
            return ValidationProblem(ModelState);
        }
        var candidate = new ImmutableMenusProbe { IsEnabled = model.IsEnabled, Menus = menus };
        return TryValidateModel(candidate) ? Ok(candidate) : ValidationProblem(ModelState);
    }

    [HttpGet("render")]
    public IActionResult Render()
    {
        var snapshot = store.Capture();
        ViewData[SnapshotProbeStore.ViewDataKey] = snapshot;
        return View("~/Views/StructuralProbe/Render.cshtml", snapshot);
    }

    [HttpGet("form")]
    public IActionResult Form() => View("~/Views/StructuralProbe/Form.cshtml", new ImmutableFeedProbe());

    [HttpGet("operations")]
    public OperationProbeResult Operations()
    {
        var before = store.Capture();
        store.Publish(before with { Feed = before.Feed with { FeedItemCount = 7 } });
        return new(before.Feed.FeedItemCount, store.Capture().Feed.FeedItemCount);
    }
}

public sealed record OperationProbeResult(int Before, int After);

public class SnapshotPageProbeModel(SnapshotProbeStore store) : PageModel
{
    public SettingsSnapshotProbe Snapshot { get; private set; } = null!;

    public void OnGet()
    {
        Snapshot = store.Capture();
        ViewData[SnapshotProbeStore.ViewDataKey] = Snapshot;
    }
}

public sealed class SnapshotProbeStore
{
    public const string ViewDataKey = "S0.SettingsSnapshot";
    private SettingsSnapshotProbe _current = new(new ImmutableFeedProbe { FeedItemCount = 20 },
        CustomMenuSettings.DefaultValue.ToJson().FromJson<ImmutableMenusProbe>());
    public SettingsSnapshotProbe Capture() => Volatile.Read(ref _current);
    public void Publish(SettingsSnapshotProbe snapshot) => Interlocked.Exchange(ref _current, snapshot);
}

public sealed class SnapshotRenderGate
{
    private int _calls;
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Continue { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public async Task PauseFirstRender()
    {
        if (Interlocked.Increment(ref _calls) != 1) return;
        Started.SetResult();
        await Continue.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }
}

public class SnapshotProbeViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var snapshot = (SettingsSnapshotProbe)ViewData[SnapshotProbeStore.ViewDataKey]!;
        return Content($"component:{snapshot.Feed.FeedItemCount}:{snapshot.Menu.Menus![0]!.Title}");
    }
}
