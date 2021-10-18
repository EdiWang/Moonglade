namespace Moonglade.Data.Porting
{
    public class ExportManager
    {
        public static readonly string DataDir = AppDomain.CurrentDomain.GetData("DataDirectory")?.ToString();

        public static string CreateExportDirectory(string directory, string subDirName)
        {
            if (directory is null) return null;

            var path = Path.Join(directory, "export", subDirName);
            if (Directory.Exists(path))
            {
                Directory.Delete(path);
            }

            Directory.CreateDirectory(path);
            return path;
        }
    }
}
