namespace Moonglade.Syndication
{
    public interface IAtomGenerator
    {
        Task<string> WriteAtomAsync();
    }
}