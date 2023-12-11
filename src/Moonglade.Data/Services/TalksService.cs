using Moonglade.Data.Context;
using Moonglade.Data.Enum;

namespace Moonglade.Data.Services;

public class TalksService
{
    private Moonglade1 _context;
    public TalksService(Moonglade1 context)
    {
        _context = context;
    }

    public void DeleteTalk(long id)
    {
        try
        {
            TalkEntity ord = _context.TalkEntity.Find(id);
            _context.TalkEntity.Remove(ord);
            _context.SaveChanges();
        }
        catch
        {
            throw;
        }
    }

    public IEnumerable<TalkEntity> GetTalkEntity()
    {
        try
        {
            LanguageEnum culture = DataHelper.GetLanguage();
            return _context.TalkEntity.Where(t => t.Language == culture).OrderByDescending(p => p.Date).ToList();
        }
        catch
        {
            throw;
        }
    }

    public void InsertTalk(TalkEntity talk)
    {
        try
        {
            _context.TalkEntity.Add(talk);
            _context.SaveChanges();
        }
        catch
        {
            throw;
        }
    }

    public TalkEntity SingleTalk(long id)
    {
        throw new NotImplementedException();
    }

    public void UpdateTalk(long id, TalkEntity talk)
    {
        try
        {
            var local = _context.Set<TalkEntity>().Local.FirstOrDefault(entry => entry.Id.Equals(talk.Id));
            // check if local is not null
            if (local != null)
            {
                // detach
                _context.Entry(local).State = EntityState.Detached;
            }
            _context.Entry(talk).State = EntityState.Modified;
            _context.SaveChanges();
        }
        catch
        {
            throw;
        }
    }
}
