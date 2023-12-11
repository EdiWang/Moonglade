using Moonglade.Data.Context;
using Moonglade.Data.Enum;

namespace Moonglade.Data.Services;

public class HonoraryPositionService
{
    private Moonglade1 _context;
    public HonoraryPositionService(Moonglade1 context)
    {
        _context = context;
    }

    public void DeleteHonoraryPosition(long id)
    {
        try
        {
            HonoraryPositonEntity ord = _context.HonoraryPositonEntity.Find(id);
            _context.HonoraryPositonEntity.Remove(ord);
            _context.SaveChanges();
        }
        catch
        {
            throw;
        }
    }

    public IEnumerable<HonoraryPositonEntity> GetHonoraryPositions()
    {
        try
        {
            LanguageEnum culture = DataHelper.GetLanguage();
            return _context.HonoraryPositonEntity.Where(c => c.Language == culture).ToList();
        }
        catch
        {
            throw;
        }
    }

    public void InsertPosition(HonoraryPositonEntity position)
    {
        try
        {
            _context.HonoraryPositonEntity.Add(position);
            _context.SaveChanges();
        }
        catch
        {
            throw;
        }
    }

    public CertificateEntity SinglePosition(long id)
    {
        throw new NotImplementedException();
    }

    public void UpdateHonoraryPosition(long id, HonoraryPositonEntity position)
    {
        try
        {
            var local = _context.Set<HonoraryPositonEntity>().Local.FirstOrDefault(entry => entry.Id.Equals(position.Id));
            // check if local is not null
            if (local != null)
            {
                // detach
                _context.Entry(local).State = EntityState.Detached;
            }
            _context.Entry(position).State = EntityState.Modified;
            _context.SaveChanges();
        }
        catch
        {
            throw;
        }
    }
}
