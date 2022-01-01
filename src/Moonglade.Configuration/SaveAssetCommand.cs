using Dapper;
using MediatR;
using System.Data;

namespace Moonglade.Configuration;

public class SaveAssetCommand : IRequest
{
    public SaveAssetCommand(Guid assetId, string assetBase64)
    {
        AssetId = assetId;
        AssetBase64 = assetBase64;
    }

    public Guid AssetId { get; set; }
    public string AssetBase64 { get; set; }
}

public class SaveAssetCommandHandler : IRequestHandler<SaveAssetCommand>
{
    private readonly IDbConnection _dbConnection;

    public SaveAssetCommandHandler(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<Unit> Handle(SaveAssetCommand request, CancellationToken cancellationToken)
    {
        if (request.AssetId == Guid.Empty) throw new ArgumentOutOfRangeException(nameof(request.AssetId));
        if (string.IsNullOrWhiteSpace(request.AssetBase64)) throw new ArgumentNullException(nameof(request.AssetBase64));

        var exists = await
            _dbConnection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM BlogAsset ba WHERE ba.Id = @assetId",
                new { request.AssetId });

        if (exists == 0)
        {
            await _dbConnection.ExecuteAsync(
                "INSERT INTO BlogAsset(Id, Base64Data, LastModifiedTimeUtc) VALUES (@assetId, @assetBase64, @utcNow)",
                new
                {
                    request.AssetId,
                    request.AssetBase64,
                    DateTime.UtcNow
                });
        }
        else
        {
            await _dbConnection.ExecuteAsync(
                "UPDATE BlogAsset SET Base64Data = @assetBase64, LastModifiedTimeUtc = @utcNow WHERE Id = @assetId",
                new
                {
                    request.AssetId,
                    request.AssetBase64,
                    DateTime.UtcNow
                });
        }

        return Unit.Value;
    }
}