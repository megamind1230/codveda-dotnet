namespace ncdu_clone_dotnet.Models;

public class ScanProgress
{
    public string CurrentPath { get; init; } = "";
    public long BytesFound { get; init; }
    public int DirectoriesScanned { get; init; }
    public int FilesScanned { get; init; }
    public int Errors { get; init; }
    public double Percent { get; init; }
}
