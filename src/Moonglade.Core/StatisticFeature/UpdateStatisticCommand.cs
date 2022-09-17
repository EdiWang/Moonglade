namespace Moonglade.Core.StatisticFeature;

public record UpdateStatisticCommand(Guid PostId, bool IsLike) : IRequest;

public class UpdateStatisticCommandHandler : AsyncRequestHandler<UpdateStatisticCommand>
{
    private readonly IRepository<PostExtensionEntity> _repo;

    public UpdateStatisticCommandHandler(IRepository<PostExtensionEntity> repo) => _repo = repo;

    protected override async Task Handle(UpdateStatisticCommand request, CancellationToken ct)
    {
        var pp = await _repo.GetAsync(request.PostId, ct);
        if (pp is null) return;

        if (request.IsLike)
        {
            if (pp.Likes >= int.MaxValue) return;
            pp.Likes += 1;
        }
        else
        {
            if (pp.Hits >= int.MaxValue) return;
            pp.Hits += 1;
        }

        await _repo.UpdateAsync(pp, ct);
    }
}