using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Utils;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.FriendLink;

public class EditLinkRequest : IValidatableObject
{
    [Required]
    [Display(Name = "Title")]
    [MaxLength(64)]
    public string Title { get; set; }

    [Required]
    [Display(Name = "Link")]
    [DataType(DataType.Url)]
    [MaxLength(256)]
    public string LinkUrl { get; set; }

    [Display(Name = "Rank")]
    public int Rank { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Uri.IsWellFormedUriString(LinkUrl, UriKind.Absolute))
        {
            yield return new($"{nameof(LinkUrl)} is not a valid url.");
        }
    }
}

public record CreateLinkCommand(EditLinkRequest Payload) : ICommand;

public class CreateLinkCommandHandler(
    MoongladeRepository<FriendLinkEntity> repo,
    ILogger<CreateLinkCommandHandler> logger) : ICommandHandler<CreateLinkCommand>
{
    public async Task HandleAsync(CreateLinkCommand request, CancellationToken ct)
    {
        var link = new FriendLinkEntity
        {
            Id = Guid.NewGuid(),
            LinkUrl = SecurityHelper.SterilizeLink(request.Payload.LinkUrl),
            Title = request.Payload.Title,
            Rank = request.Payload.Rank
        };

        await repo.AddAsync(link, ct);

        logger.LogInformation("Created a new friend link: {Title}", link.Title);
    }
}