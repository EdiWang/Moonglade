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

public class AddLinkCommandHandler : AsyncRequestHandler<AddLinkCommand>
{
    private readonly IRepository<FriendLinkEntity> _repo;

    public AddLinkCommandHandler(IRepository<FriendLinkEntity> repo) => _repo = repo;

    protected override async Task Handle(AddLinkCommand request, CancellationToken ct)
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

        await _repo.AddAsync(link, ct);
    }
}