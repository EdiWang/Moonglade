namespace Moonglade.Data.Porting;

public class ExportResult
{
    public ExportFormat ExportFormat { get; set; }

    public string FilePath { get; set; }

    public byte[] Content { get; set; }

    public string ContentType
    {
        get
        {
            return ExportFormat switch
            {
                ExportFormat.SingleCSVFile => "text/csv",
                ExportFormat.SingleJsonFile => "application/octet-stream",
                ExportFormat.ZippedJsonFiles => "application/zip",
                _ => string.Empty
            };
        }
    }
}