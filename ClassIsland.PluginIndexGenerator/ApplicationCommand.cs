namespace ClassIsland.PluginIndexGenerator;

public class ApplicationCommand
{
    public string InputDir { get; set; } = "";

    public string Output { get; set; } = "";
    
    public string? GitHubToken { get; set; }
    
    public string? BaseFile { get; set; }
}