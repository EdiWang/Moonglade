using System.Security.Cryptography;

namespace Moonglade.Core;

public record SaveStyleSheetCommand(Guid Id, string Slug, string CssContent) : IRequest<Guid>;

public class SaveStyleSheetCommandHandler(IRepository<StyleSheetEntity> repo) : IRequestHandler<SaveStyleSheetCommand, Guid>
{
    public async Task<Guid> Handle(SaveStyleSheetCommand request, CancellationToken cancellationToken)
    {
        var slug = request.Slug.ToLower().Trim();
        var css = request.CssContent.Trim();
        var hash = CalculateHash($"{slug}_{css}");

        var entity = await repo.GetAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            entity = new()
            {
                Id = request.Id,
                FriendlyName = $"page_{slug}",
                CssContent = css,
                Hash = hash,
                LastModifiedTimeUtc = DateTime.UtcNow
            };

            await repo.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.FriendlyName = $"page_{slug}";
            entity.CssContent = css;
            entity.Hash = hash;
            entity.LastModifiedTimeUtc = DateTime.UtcNow;

            await repo.UpdateAsync(entity, cancellationToken);
        }

        return entity.Id;
    }

    private string CalculateHash(string content)
    {
        var sha256 = SHA256.Create();

        byte[] inputBytes = Encoding.ASCII.GetBytes(content);
        byte[] outputBytes = sha256.ComputeHash(inputBytes);

        return Convert.ToBase64String(outputBytes);
    }
}