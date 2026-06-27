namespace TikTokFireAutomation;

public class AppConfig
{
    public List<string> NameStreaks { get; set; } = new();
    public string DefaultText { get; set; } = "+";
        
    public string? CustomUserAgent { get; set; } = null;
    public bool Headless { get; set; } = false;
}