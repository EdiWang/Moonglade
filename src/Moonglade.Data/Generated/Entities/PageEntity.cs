namespace Moonglade.Data.Generated.Entities;

public class PageEntity
{
	public Guid Id { get; set; }
	public string Title { get; set; }
	public string Slug { get; set; }
	public string MetaDescription { get; set; }
	public string HtmlContent { get; set; }
	public string CssId { get; set; }
	public bool HideSidebar { get; set; }
	public bool IsPublished { get; set; }
	public DateTime CreateTimeUtc { get; set; }
	public DateTime? UpdateTimeUtc { get; set; }
}