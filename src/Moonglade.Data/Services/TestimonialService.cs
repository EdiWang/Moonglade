using Moonglade.Data.Context;
using Moonglade.Data.Enum;

namespace Moonglade.Data.Services;

public class TestimonialService
{
    private Moonglade1 _context;
    public TestimonialService(Moonglade1 context)
    {
        _context = context;
    }

    public void DeleteTestimonial(long id)
    {
        try
        {
            TestimonialEntity ord = _context.TestimonialEntity.Find(id);
            _context.TestimonialEntity.Remove(ord);
            _context.SaveChanges();
        }
        catch
        {
            throw;
        }
    }

    public IEnumerable<TestimonialEntity> GetTestimonialEntity()
    {
        try
        {
            LanguageEnum culture = DataHelper.GetLanguage();
            return _context.TestimonialEntity.Where(c => c.Language == culture).OrderByDescending(d => d.Date).ToList();
        }
        catch
        {
            throw;
        }
    }

    public void InsertTestimonial(TestimonialEntity testimonial)
    {
        try
        {
            _context.TestimonialEntity.Add(testimonial);
            _context.SaveChanges();
        }
        catch
        {
            throw;
        }
    }

    public TestimonialEntity SingleTestimonial(long id)
    {
        throw new NotImplementedException();
    }

    public void UpdateTestimonial(long id, TestimonialEntity testimonial)
    {
        try
        {
            var local = _context.Set<TestimonialEntity>().Local.FirstOrDefault(entry => entry.Id.Equals(testimonial.Id));
            // check if local is not null
            if (local != null)
            {
                // detach
                _context.Entry(local).State = EntityState.Detached;
            }
            _context.Entry(testimonial).State = EntityState.Modified;
            _context.SaveChanges();
        }
        catch
        {
            throw;
        }
    }
}
