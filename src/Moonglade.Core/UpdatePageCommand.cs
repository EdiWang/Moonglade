using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Core
{
    public class UpdatePageCommand : IRequest<Guid>
    {
        public UpdatePageCommand(Guid id, PageEditModel model)
        {
            Id = id;
            Model = model;
        }

        public Guid Id { get; set; }
        public PageEditModel Model { get; set; }
    }

    public class UpdatePageCommandHandler : IRequestHandler<UpdatePageCommand, Guid>
    {
        private readonly IRepository<PageEntity> _pageRepo;
        private readonly IBlogAudit _audit;

        public UpdatePageCommandHandler(IRepository<PageEntity> pageRepo, IBlogAudit audit)
        {
            _pageRepo = pageRepo;
            _audit = audit;
        }

        public async Task<Guid> Handle(UpdatePageCommand request, CancellationToken cancellationToken)
        {
            var page = await _pageRepo.GetAsync(request.Id);
            if (page is null)
            {
                throw new InvalidOperationException($"PageEntity with Id '{request.Id}' not found.");
            }

            page.Title = request.Model.Title.Trim();
            page.Slug = request.Model.Slug.ToLower().Trim();
            page.MetaDescription = request.Model.MetaDescription;
            page.HtmlContent = request.Model.RawHtmlContent;
            page.CssContent = request.Model.CssContent;
            page.HideSidebar = request.Model.HideSidebar;
            page.UpdateTimeUtc = DateTime.UtcNow;
            page.IsPublished = request.Model.IsPublished;

            await _pageRepo.UpdateAsync(page);
            await _audit.AddEntry(BlogEventType.Content, BlogEventId.PageUpdated, $"Page '{request.Id}' updated.");

            return page.Id;
        }
    }
}
