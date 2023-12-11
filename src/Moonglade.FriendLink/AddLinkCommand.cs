using System.ComponentModel.DataAnnotations;

using MediatR;
using Moonglade.Data.Generated.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Utils;

namespace Moonglade.FriendLink;

public class AddLinkCommand : IRequest, IValidatableObject
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

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		if (!Uri.IsWellFormedUriString(LinkUrl, UriKind.Absolute))
		{
			yield return new($"{nameof(LinkUrl)} is not a valid url.");
		}
	}
}

public class AddLinkCommandHandler(IRepository<FriendLinkEntity> repo) : IRequestHandler<AddLinkCommand>
{
    public async Task Handle(AddLinkCommand request, CancellationToken ct)
    {
        var link = new FriendLinkEntity
        {
            Id = Guid.NewGuid(),
            LinkUrl = Helper.SterilizeLink(request.LinkUrl),
            Title = request.Title
        };

        await repo.AddAsync(link, ct);
    }
}
