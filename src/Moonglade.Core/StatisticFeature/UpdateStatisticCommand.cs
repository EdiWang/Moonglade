namespace Moonglade.Core.StatisticFeature;

public record UpdateStatisticCommand(Guid PostId, bool IsLike) : IRequest;

public class UpdateStatisticCommandHandler : IRequestHandler<UpdateStatisticCommand>
{
    private readonly IRepository<PostExtensionEntity> _postExtensionRepo;

    public UpdateStatisticCommandHandler(IRepository<PostExtensionEntity> postExtensionRepo)
    {
        _postExtensionRepo = postExtensionRepo;
    }

    public async Task<Unit> Handle(UpdateStatisticCommand request, CancellationToken cancellationToken)
    {
        var pp = await _postExtensionRepo.GetAsync(request.PostId);
        if (pp is null) return Unit.Value;

        if (request.IsLike)
        {
            if (pp.Likes >= int.MaxValue) return Unit.Value;
            pp.Likes += 1;
        }
        else
        {
            if (pp.Hits >= int.MaxValue) return Unit.Value;
            pp.Hits += 1;
        }

        await _postExtensionRepo.UpdateAsync(pp);
        return Unit.Value;
    }
}