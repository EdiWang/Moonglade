using Moonglade.Data.Context;
using Moonglade.Data.Enum;

namespace Moonglade.Data.Services;

public class MandateService
{
    private Moonglade1 _context;
    public MandateService(Moonglade1 context)
    {
        _context = context;
    }

    public void DeleteMandateEntity(long id)
    {
        try
        {
            MandateEntity ord = _context.MandateEntity.Find(id);
            _context.MandateEntity.Remove(ord);
            _context.SaveChanges();
        }
        catch
        {
            throw;
        }
    }

    public IEnumerable<MandateEntity> GetMandateEntitys()
    {
        try
        {
            LanguageEnum culture = DataHelper.GetLanguage();
            return _context.MandateEntity.Where(c => c.Language == culture).OrderByDescending(d => d.Years).ToList();
        }
        catch
        {
            throw;
        }
    }

    public void InsertMandateEntity(MandateEntity mandate)
    {
        try
        {
            _context.MandateEntity.Add(mandate);
            _context.SaveChanges();
        }
        catch
        {
            throw;
        }
    }

    public MandateEntity SingleMandateEntity(long id)
    {
        throw new NotImplementedException();
    }

    public void UpdateMandateEntity(long id, MandateEntity mandate)
    {
        try
        {
            var local = _context.Set<MandateEntity>().Local.FirstOrDefault(entry => entry.Id.Equals(mandate.Id));
            // check if local is not null
            if (local != null)
            {
                // detach
                _context.Entry(local).State = EntityState.Detached;
            }
            _context.Entry(mandate).State = EntityState.Modified;
            _context.SaveChanges();
        }
        catch
        {
            throw;
        }
    }
}
