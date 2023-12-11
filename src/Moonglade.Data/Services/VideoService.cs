using Moonglade.Data.Context;

using LanguageEnum = Moonglade.Data.Enum.LanguageEnum;

namespace Moonglade.Data.Services;

public class VideoService
{
    private Moonglade1 _context;
    public VideoService(Moonglade1 context)
    {
        _context = context;
    }

    public void DeleteVideo(long id)
    {
        try
        {
            VideoEntity ord = _context.VideoEntity.Find(id);
            _context.VideoEntity.Remove(ord);
            _context.SaveChanges();
        }
        catch
        {
            throw;
        }
    }

    public IEnumerable<VideoEntity> GetVideoEntity()
    {
        try
        {
            LanguageEnum culture = DataHelper.GetLanguage();
            return _context.VideoEntity.Where(l => l.Language == culture).OrderByDescending(d => d.DatePublished).ToList();
        }
        catch
        {
            throw;
        }
    }

    public void InsertVideo(VideoEntity video)
    {
        try
        {
            _context.VideoEntity.Add(video);
            _context.SaveChanges();
        }
        catch
        {
            throw;
        }
    }

    public VideoEntity Video(long id)
    {
        throw new NotImplementedException();
    }

    public void UpdateVideo(long id, VideoEntity video)
    {
        try
        {
            var local = _context.Set<VideoEntity>().Local.FirstOrDefault(entry => entry.Id.Equals(video.Id));
            // check if local is not null
            if (local != null)
            {
                // detach
                _context.Entry(local).State = EntityState.Detached;
            }
            _context.Entry(video).State = EntityState.Modified;
            _context.SaveChanges();
        }
        catch
        {
            throw;
        }
    }
}
