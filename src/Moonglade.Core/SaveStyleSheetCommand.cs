﻿using Microsoft.Extensions.Logging;
using Moonglade.Data;
using System.Security.Cryptography;

namespace Moonglade.Core;

public record SaveStyleSheetCommand(Guid Id, string Slug, string CssContent) : IRequest<Guid>;

public class SaveStyleSheetCommandHandler(
    MoongladeRepository<StyleSheetEntity> repo,
    ILogger<SaveStyleSheetCommandHandler> logger
) : IRequestHandler<SaveStyleSheetCommand, Guid>
{
    public async Task<Guid> Handle(SaveStyleSheetCommand request, CancellationToken cancellationToken)
    {
        var slug = request.Slug.ToLower().Trim();
        var css = request.CssContent.Trim();
        var hash = CalculateHash($"{slug}_{css}");

        var entity = await repo.GetByIdAsync(request.Id, cancellationToken) ?? new StyleSheetEntity { Id = request.Id };

        entity.FriendlyName = $"page_{slug}";
        entity.CssContent = css;
        entity.Hash = hash;
        entity.LastModifiedTimeUtc = DateTime.UtcNow;

        if (entity.Id == request.Id)
        {
            await repo.UpdateAsync(entity, cancellationToken);
            logger.LogInformation("Style sheet updated: {slug}", slug);
        }
        else
        {
            await repo.AddAsync(entity, cancellationToken);
            logger.LogInformation("New style sheet added: {slug}", slug);
        }

        return entity.Id;
    }

    private string CalculateHash(string content)
    {
        using var sha256 = SHA256.Create();
        byte[] inputBytes = Encoding.ASCII.GetBytes(content);
        byte[] outputBytes = sha256.ComputeHash(inputBytes);
        return Convert.ToBase64String(outputBytes);
    }
}