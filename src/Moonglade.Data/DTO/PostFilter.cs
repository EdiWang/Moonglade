namespace Moonglade.Data.DTO;

public record PostFilter(
    string Title = null,
    string ContentAbstract = null,
    string Tag = null,
    bool SortDescending = true);
