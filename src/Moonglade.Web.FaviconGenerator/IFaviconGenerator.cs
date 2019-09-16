namespace Moonglade.Web.FaviconGenerator
{
    public interface IFaviconGenerator
    {
        void GenerateIcons(string sourceImagePath, string directory);
    }
}