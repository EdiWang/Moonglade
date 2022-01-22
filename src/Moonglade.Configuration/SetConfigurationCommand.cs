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

public class SetConfigurationCommand : IRequest<OperationCode>
{
    public SetConfigurationCommand(IBlogSettings blogSettings)
    {
        BlogSettings = blogSettings;
    }

    public IBlogSettings BlogSettings { get; set; }
}

public class SetConfigurationCommandHandler : IRequestHandler<SetConfigurationCommand, OperationCode>
{
    private readonly IRepository<BlogConfigurationEntity> _repository;

    public SetConfigurationCommandHandler(IRepository<BlogConfigurationEntity> repository)
    {
        _repository = repository;
    }

    public async Task<OperationCode> Handle(SetConfigurationCommand request, CancellationToken cancellationToken)
    {
        var json = request.BlogSettings.ToJson();

        var entity = await _repository.GetAsync(p => p.CfgKey == nameof(request.BlogSettings));
        if (entity == null) return OperationCode.ObjectNotFound;

        entity.CfgValue = json;
        entity.LastModifiedTimeUtc = DateTime.UtcNow;

        await _repository.UpdateAsync(entity);
        return OperationCode.Done;
    }
}