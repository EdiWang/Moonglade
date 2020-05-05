namespace Moonglade.DataPorting
{
    public class ExportResult
    {
        public ExportFormat ExportFormat { get; set; }

        public string ZipFilePath { get; set; }

        public string JsonContent { get; set; }
    }
}