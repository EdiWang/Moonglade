using Moonglade.Data.Context;
using Moonglade.Data.Enum;

namespace Moonglade.Data.Services;

public class PublicationService
{
    private Moonglade1 _context;
    public PublicationService(Moonglade1 context)
    {
        _context = context;
    }

    public void DeletePublication(long id)
    {
        try
        {
            PublicationEntity ord = _context.PublicationEntity.Find(id);
            _context.PublicationEntity.Remove(ord);
            _context.SaveChanges();
        }
        catch
        {
            throw;
        }
    }

    public IEnumerable<PublicationEntity> GetPublicationEntity()
    {
        try
        {
            LanguageEnum culture = DataHelper.GetLanguage();
            return _context.PublicationEntity.Where(c => c.Language == culture).OrderByDescending(d => d.DatePublished).ToList();
        }
        catch
        {
            throw;
        }
    }

    public void InsertPublication(PublicationEntity publication)
    {
        try
        {
            _context.PublicationEntity.Add(publication);
            _context.SaveChanges();
        }
        catch
        {
            throw;
        }
    }

    public MandateEntity SinglePublication(long id)
    {
        throw new NotImplementedException();
    }

    public void UpdatePublication(long id, PublicationEntity publication)
    {
        try
        {
            var local = _context.Set<PublicationEntity>().Local.FirstOrDefault(entry => entry.Id.Equals(publication.Id));
            // check if local is not null
            if (local != null)
            {
                // detach
                _context.Entry(local).State = EntityState.Detached;
            }
            _context.Entry(publication).State = EntityState.Modified;
            _context.SaveChanges();
        }
        catch
        {
            throw;
        }
    }
}
