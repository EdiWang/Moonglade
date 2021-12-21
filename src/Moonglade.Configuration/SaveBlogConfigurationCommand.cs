using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Configuration;

public class SaveBlogConfigurationCommand : IRequest<OperationCode>
{
    public SaveBlogConfigurationCommand(object blogSettings)
    {
        BlogSettings = blogSettings;
    }

    // IBlogSettings will blow up System.Text.Json
    public object BlogSettings { get; set; }
}

public class SaveBlogConfigurationCommandHandler : IRequestHandler<SaveBlogConfigurationCommand, OperationCode>
{
    private readonly IRepository<BlogConfigurationEntity> _repository;

    public SaveBlogConfigurationCommandHandler(IRepository<BlogConfigurationEntity> repository)
    {
        _repository = repository;
    }

    public async Task<OperationCode> Handle(SaveBlogConfigurationCommand request, CancellationToken cancellationToken)
    {
        var json = request.BlogSettings.ToJson();
        var key = request.BlogSettings.GetType().Name;

        var entry = await _repository.GetAsync(p => p.CfgKey == key);
        if (entry == null) return OperationCode.ObjectNotFound;

        entry.CfgValue = json;
        entry.LastModifiedTimeUtc = DateTime.UtcNow;

        await _repository.UpdateAsync(entry);
        return OperationCode.Done;
    }
}