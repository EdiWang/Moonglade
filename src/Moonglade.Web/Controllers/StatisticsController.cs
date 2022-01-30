using Moonglade.Core.StatisticFeature;
using Moonglade.Web.Attributes;

namespace Moonglade.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatisticsController : ControllerBase
{
    private readonly IMediator _mediator;

    private bool DNT => (bool)HttpContext.Items["DNT"];

    public StatisticsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{postId:guid}")]
    [ProducesResponseType(typeof(Tuple<int, int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([NotEmpty] Guid postId)
    {
        var (hits, likes) = await _mediator.Send(new GetStatisticQuery(postId));
        return Ok(new { Hits = hits, Likes = likes });
    }

    [HttpPost]
    [DisallowSpiderUA]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Post(StatisticsRequest request)
    {
        if (DNT) return NoContent();

        await _mediator.Send(new UpdateStatisticCommand(request.PostId, request.IsLike));
        return NoContent();
    }
}

public class StatisticsRequest
{
    [NotEmpty]
    public Guid PostId { get; set; }

    public bool IsLike { get; set; }
}