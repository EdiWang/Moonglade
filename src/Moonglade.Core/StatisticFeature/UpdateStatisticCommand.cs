namespace Moonglade.Core.StatisticFeature;

public record UpdateStatisticCommand(Guid PostId) : IRequest;

public class UpdateStatisticCommandHandler : IRequestHandler<UpdateStatisticCommand>
{
    private readonly IRepository<PostExtensionEntity> _repo;

    public UpdateStatisticCommandHandler(IRepository<PostExtensionEntity> repo) => _repo = repo;

    public async Task Handle(UpdateStatisticCommand request, CancellationToken ct)
    {
        var pp = await _repo.GetAsync(request.PostId, ct);
        if (pp is null) return;

        if (pp.Hits >= int.MaxValue) return;
        pp.Hits += 1;

        await _repo.UpdateAsync(pp, ct);
    }
}