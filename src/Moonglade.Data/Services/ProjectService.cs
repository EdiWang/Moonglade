using Moonglade.Data.Context;

namespace Moonglade.Data.Services;

public class ProjectService
{
    private Moonglade1 _context;
    public ProjectService(Moonglade1 context)
    {
        _context = context;
    }

    public void DeleteProject(long id)
    {
        try
        {
            ProjectEntity ord = _context.ProjectEntity.Find(id);
            _context.ProjectEntity.Remove(ord);
            _context.SaveChanges();
        }
        catch
        {
            throw;
        }
    }

    public IEnumerable<ProjectEntity> GetProjectEntity()
    {
        try
        {
            return _context.ProjectEntity.ToList();
        }
        catch
        {
            throw;
        }
    }

    public void InsertProject(ProjectEntity project)
    {
        try
        {
            _context.ProjectEntity.Add(project);
            _context.SaveChanges();
        }
        catch
        {
            throw;
        }
    }

    public ProjectEntity SingleProject(long id)
    {
        throw new NotImplementedException();
    }

    public void UpdateProject(long id, ProjectEntity project)
    {
        try
        {
            var local = _context.Set<ProjectEntity>().Local.FirstOrDefault(entry => entry.Id.Equals(project.Id));
            // check if local is not null
            if (local != null)
            {
                // detach
                _context.Entry(local).State = EntityState.Detached;
            }
            _context.Entry(project).State = EntityState.Modified;
            _context.SaveChanges();
        }
        catch
        {
            throw;
        }
    }
}
