namespace R3EServerRaceResult.Models
{
    public class MultipleUploadResult
    {
        public List<string> ProcessedFiles { get; set; } = [];
        public List<string> SkippedFiles { get; set; } = [];
        public List<FileUploadError> FailedFiles { get; set; } = [];
        public int TotalReceived { get; set; }
        public int TotalProcessed { get; set; }
        public int TotalSkipped { get; set; }
        public int TotalFailed { get; set; }
    }

    public class FileUploadError
    {
        public string FileName { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }
}
