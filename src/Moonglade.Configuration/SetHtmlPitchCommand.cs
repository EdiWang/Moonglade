using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Configuration;

public class SetHtmlPitchCommand : IRequest<OperationCode>
{
    public SetHtmlPitchCommand(PitchKey key, string value)
    {
        Key = key;
        Value = value;
    }

    public PitchKey Key { get; set; }

    public string Value { get; set; }
}

public class SetHtmlPitchCommandHandler : IRequestHandler<SetHtmlPitchCommand, OperationCode>
{
    private readonly IRepository<HtmlPitchEntity> _repository;

    public SetHtmlPitchCommandHandler(IRepository<HtmlPitchEntity> repository)
    {
        _repository = repository;
    }

    public async Task<OperationCode> Handle(SetHtmlPitchCommand request, CancellationToken cancellationToken)
    {
        var entry = await _repository.GetAsync((int)request.Key);
        if (null == entry) return OperationCode.ObjectNotFound;

        entry.HtmlCode = request.Value;

        await _repository.UpdateAsync(entry);
        return OperationCode.Done;
    }
}