namespace Moonglade.Core.StatisticFeature;

public record UpdateStatisticCommand(Guid PostId, bool IsLike) : IRequest;

public class UpdateStatisticCommandHandler : AsyncRequestHandler<UpdateStatisticCommand>
{
    private readonly IRepository<PostExtensionEntity> _postExtensionRepo;

    public UpdateStatisticCommandHandler(IRepository<PostExtensionEntity> postExtensionRepo) => _postExtensionRepo = postExtensionRepo;

    protected override async Task Handle(UpdateStatisticCommand request, CancellationToken cancellationToken)
    {
        var pp = await _postExtensionRepo.GetAsync(request.PostId);
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

        await _postExtensionRepo.UpdateAsync(pp, cancellationToken);
    }
}