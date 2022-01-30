using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Utils;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.FriendLink;

public class AddLinkCommand : IRequest
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
}

public class AddLinkCommandHandler : IRequestHandler<AddLinkCommand>
{
    private readonly IRepository<FriendLinkEntity> _friendlinkRepo;

    public AddLinkCommandHandler(IRepository<FriendLinkEntity> friendlinkRepo)
    {
        _friendlinkRepo = friendlinkRepo;
    }

    public async Task<Unit> Handle(AddLinkCommand request, CancellationToken cancellationToken)
    {
        if (!Uri.IsWellFormedUriString(request.LinkUrl, UriKind.Absolute))
        {
            throw new InvalidOperationException($"{nameof(request.LinkUrl)} is not a valid url.");
        }

        var link = new FriendLinkEntity
        {
            Id = Guid.NewGuid(),
            LinkUrl = Helper.SterilizeLink(request.LinkUrl),
            Title = request.Title
        };

        await _friendlinkRepo.AddAsync(link);

        return Unit.Value;
    }
}