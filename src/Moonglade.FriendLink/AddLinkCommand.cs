using MediatR;
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

public record AddLinkCommand(EditLinkRequest Payload) : IRequest;

public class AddLinkCommandHandler(MoongladeRepository<FriendLinkEntity> repo) : IRequestHandler<AddLinkCommand>
{
    public async Task Handle(AddLinkCommand request, CancellationToken ct)
    {
        var link = new FriendLinkEntity
        {
            Id = Guid.NewGuid(),
            LinkUrl = Helper.SterilizeLink(request.Payload.LinkUrl),
            Title = request.Payload.Title,
            Rank = request.Payload.Rank
        };

        await repo.AddAsync(link, ct);
    }
}