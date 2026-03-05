using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace Moonglade.Features.Page;

public record SaveStyleSheetCommand(Guid Id, string Slug, string CssContent) : ICommand<Guid>;

public class SaveStyleSheetCommandHandler(
    IRepositoryBase<StyleSheetEntity> repo,
    ILogger<SaveStyleSheetCommandHandler> logger
) : ICommandHandler<SaveStyleSheetCommand, Guid>
{
    public async Task<Guid> HandleAsync(SaveStyleSheetCommand request, CancellationToken cancellationToken)
    {
        var slug = request.Slug.ToLower().Trim();
        var css = request.CssContent.Trim();
        var hash = CalculateHash($"{slug}_{css}");

        var entity = await repo.GetByIdAsync(request.Id, cancellationToken);

        if (entity != null)
        {
            entity.FriendlyName = $"page_{slug}";
            entity.CssContent = css;
            entity.Hash = hash;
            entity.LastModifiedTimeUtc = DateTime.UtcNow;

            await repo.UpdateAsync(entity, cancellationToken);
            logger.LogInformation("Style sheet updated: {slug}", slug);
        }
        else
        {
            entity = new StyleSheetEntity
            {
                Id = request.Id,
                FriendlyName = $"page_{slug}",
                CssContent = css,
                Hash = hash,
                LastModifiedTimeUtc = DateTime.UtcNow
            };

            await repo.AddAsync(entity, cancellationToken);
            logger.LogInformation("New style sheet added: {slug}", slug);
        }

        return entity.Id;
    }

    private static string CalculateHash(string content)
    {
        byte[] inputBytes = Encoding.ASCII.GetBytes(content);
        byte[] outputBytes = SHA256.HashData(inputBytes);
        return Convert.ToBase64String(outputBytes);
    }
}