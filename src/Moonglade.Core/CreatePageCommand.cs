using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Core
{
    public class CreatePageCommand : IRequest<Guid>
    {
        public CreatePageCommand(PageEditModel model)
        {
            Model = model;
        }

        public PageEditModel Model { get; set; }
    }

    public class CreatePageCommandHandler : IRequestHandler<CreatePageCommand, Guid>
    {
        private readonly IRepository<PageEntity> _pageRepo;
        private readonly IBlogAudit _audit;

        public CreatePageCommandHandler(IRepository<PageEntity> pageRepo, IBlogAudit audit)
        {
            _pageRepo = pageRepo;
            _audit = audit;
        }

        public async Task<Guid> Handle(CreatePageCommand request, CancellationToken cancellationToken)
        {
            var uid = Guid.NewGuid();
            var page = new PageEntity
            {
                Id = uid,
                Title = request.Model.Title.Trim(),
                Slug = request.Model.Slug.ToLower().Trim(),
                MetaDescription = request.Model.MetaDescription,
                CreateTimeUtc = DateTime.UtcNow,
                HtmlContent = request.Model.RawHtmlContent,
                CssContent = request.Model.CssContent,
                HideSidebar = request.Model.HideSidebar,
                IsPublished = request.Model.IsPublished
            };

            await _pageRepo.AddAsync(page);
            await _audit.AddEntry(BlogEventType.Content, BlogEventId.PageCreated, $"Page '{page.Id}' created.");

            return uid;
        }
    }
}
