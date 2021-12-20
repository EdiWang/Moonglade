using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Configuration;

public class GetHtmlPitchQuery : IRequest<HtmlPitch>
{
    public GetHtmlPitchQuery(PitchKey key)
    {
        Key = key;
    }

    public PitchKey Key { get; set; }
}

public class GetHtmlPitchQueryHandler : IRequestHandler<GetHtmlPitchQuery, HtmlPitch>
{
    private readonly IRepository<HtmlPitchEntity> _repository;

    public GetHtmlPitchQueryHandler(IRepository<HtmlPitchEntity> repository)
    {
        _repository = repository;
    }

    public async Task<HtmlPitch> Handle(GetHtmlPitchQuery request, CancellationToken cancellationToken)
    {
        var entry = await _repository.GetAsync((int)request.Key);
        if (null == entry) return null;

        return new()
        {
            Key = request.Key,
            Value = entry.HtmlCode
        };
    }
}