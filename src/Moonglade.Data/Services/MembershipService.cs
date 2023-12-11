using Moonglade.Data.Context;
using Moonglade.Data.Enum;

namespace Moonglade.Data.Services;

public class MembershipService
{
    private Moonglade1 _context;
    public MembershipService(Moonglade1 context)
    {
        _context = context;
    }

    public void DeleteMembership(long id)
    {
        try
        {
            MembershipEntity ord = _context.MembershipEntity.Find(id);
            _context.MembershipEntity.Remove(ord);
            _context.SaveChanges();
        }
        catch
        {
            throw;
        }
    }

    public IEnumerable<MembershipEntity> GetMembershipEntity()
    {
        try
        {
            LanguageEnum culture = DataHelper.GetLanguage();
            return _context.MembershipEntity.Where(c => c.Language == culture).ToList();
        }
        catch
        {
            throw;
        }
    }

    public void InsertMembershipEntity(MembershipEntity membership)
    {
        try
        {
            _context.MembershipEntity.Add(membership);
            _context.SaveChanges();
        }
        catch
        {
            throw;
        }
    }

    public MembershipEntity SingleMeMembership(long id)
    {
        throw new NotImplementedException();
    }

    public void UpdateMembership(long id, MembershipEntity membership)
    {
        try
        {
            var local = _context.Set<MembershipEntity>().Local.FirstOrDefault(entry => entry.Id.Equals(membership.Id));
            // check if local is not null
            if (local != null)
            {
                // detach
                _context.Entry(local).State = EntityState.Detached;
            }
            _context.Entry(membership).State = EntityState.Modified;
            _context.SaveChanges();
        }
        catch
        {
            throw;
        }
    }
}
