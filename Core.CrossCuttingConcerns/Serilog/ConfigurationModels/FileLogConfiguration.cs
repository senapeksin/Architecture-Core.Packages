namespace Core.CrossCuttingConcerns.Serilog.ConfigurationModels
{
    public class FileLogConfiguration
    {
        public string FilePath { get; set; }


        public FileLogConfiguration()
        {
            FilePath = String.Empty;
        }

        public FileLogConfiguration(string filePath)
        {
            FilePath = filePath;
        }
    }
}
