﻿using Microsoft.Extensions.Logging;
using Moonglade.Data;

namespace Moonglade.Core.PageFeature;

public record DeletePageCommand(Guid Id) : IRequest<OperationCode>;

public class DeletePageCommandHandler(
    MoongladeRepository<PageEntity> repo,
    IMediator mediator,
    ILogger<DeletePageCommandHandler> logger) : IRequestHandler<DeletePageCommand, OperationCode>
{
    public async Task<OperationCode> Handle(DeletePageCommand request, CancellationToken ct)
    {
        var page = await repo.GetByIdAsync(request.Id, ct);
        if (page == null) return OperationCode.ObjectNotFound;

        if (!string.IsNullOrWhiteSpace(page.CssId))
        {
            await mediator.Send(new DeleteStyleSheetCommand(new(page.CssId)), ct);
        }

        await repo.DeleteAsync(page, ct);

        logger.LogInformation("Deleted page: {PageId}", request.Id);
        return OperationCode.Done;
    }
}