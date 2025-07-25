﻿using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;

namespace Moonglade.Core.PageFeature;

public record CreatePageCommand(EditPageRequest Payload) : ICommand<Guid>;

public class CreatePageCommandHandler(
    MoongladeRepository<PageEntity> repo,
    ICommandMediator commandMediator,
    ILogger<CreatePageCommandHandler> logger) : ICommandHandler<CreatePageCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreatePageCommand request, CancellationToken ct)
    {
        var slug = request.Payload.Slug.ToLower().Trim();

        Guid? cssId = null;
        if (!string.IsNullOrWhiteSpace(request.Payload.CssContent))
        {
            cssId = await commandMediator.SendAsync(new SaveStyleSheetCommand(Guid.NewGuid(), slug, request.Payload.CssContent), ct);
        }

        var uid = Guid.NewGuid();
        var utcNow = DateTime.UtcNow;

        var page = new PageEntity
        {
            Id = uid,
            Title = request.Payload.Title.Trim(),
            Slug = slug,
            MetaDescription = request.Payload.MetaDescription,
            CreateTimeUtc = utcNow,
            UpdateTimeUtc = utcNow,
            HtmlContent = request.Payload.RawHtmlContent,
            HideSidebar = request.Payload.HideSidebar,
            IsPublished = request.Payload.IsPublished,
            CssId = cssId.ToString()
        };

        await repo.AddAsync(page, ct);

        logger.LogInformation("Created page: {PageId}", uid);
        return uid;
    }
}