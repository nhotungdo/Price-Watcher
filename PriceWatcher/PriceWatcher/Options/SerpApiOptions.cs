namespace PriceWatcher.Options;

public class SerpApiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://serpapi.com";
    public int TimeoutSeconds { get; set; } = 30;
    public bool Enabled { get; set; } = true;
}
