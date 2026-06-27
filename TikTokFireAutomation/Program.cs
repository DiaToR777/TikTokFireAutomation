using Microsoft.Playwright;
using Serilog;
using System.Text.Json;
using TikTokFireAutomation;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/bot-log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    string configPath = "config.json";
    if (!File.Exists(configPath))
    {
        Log.Error("Config file config.json not found!");
        return;
    }
    
    var jsonText = await File.ReadAllTextAsync(configPath);
    var config = JsonSerializer.Deserialize<AppConfig>(jsonText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    
    if (config == null || config.NameStreaks.Count == 0)
    {
        Log.Warning("List NameStreaks empty or config file invalid.");
        return;
    }

    Log.Information("Config success loaded. Found targets: {Count}", config.NameStreaks.Count);

    using var playwright = await Playwright.CreateAsync();
    
    string profilePath = Path.Combine(Directory.GetCurrentDirectory(), "user_profile");

    var contextOptions = new BrowserTypeLaunchPersistentContextOptions
    {
        Headless = config.Headless,
        SlowMo = 500,
        UserAgent = !string.IsNullOrEmpty(config.CustomUserAgent) 
            ? config.CustomUserAgent 
            : "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:152.0) Gecko/20100101 Firefox/152.0"
    };

    Log.Information("Launching Firefox with persistent profile: {ProfilePath}", profilePath);
    await using var context = await playwright.Firefox.LaunchPersistentContextAsync(profilePath, contextOptions); //TODO Different browsers with correct user-agent
    
    var page = context.Pages.Count > 0 ? context.Pages[0] : await context.NewPageAsync();
    
    string manualCookiesPath = "cookies.json";

if (File.Exists(manualCookiesPath))
    {
        Log.Information("Founded cookies.json. Import cookies into profile...");
        try
        {
            var cookiesJson = await File.ReadAllTextAsync(manualCookiesPath);
            
            using var doc = JsonDocument.Parse(cookiesJson);
            var cookiesList = new List<Cookie>();
            JsonElement cookiesArray;

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                cookiesArray = doc.RootElement;
            else if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("cookies", out var cookiesProp) && cookiesProp.ValueKind == JsonValueKind.Array)
                cookiesArray = cookiesProp;
            else
            {
                    Log.Error("Invalid format cookies.json. File must be an array or an object with a key 'cookies'.");
                cookiesArray = default;
            }

            if (cookiesArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in cookiesArray.EnumerateArray())
                {
                    cookiesList.Add(new Cookie
                    {
                        Name = element.GetProperty("name").GetString()!,
                        Value = element.GetProperty("value").GetString()!,
                        Domain = element.GetProperty("domain").GetString()!,
                        Path = element.GetProperty("path").GetString()!,
                        Secure = element.TryGetProperty("secure", out var s) && s.GetBoolean(),
                        HttpOnly = element.TryGetProperty("httpOnly", out var h) && h.GetBoolean()
                    });
                }
            }

            if (cookiesList.Count > 0)
            {
                await context.AddCookiesAsync(cookiesList);
                Log.Information("success imported {Count} cookies ", cookiesList.Count);
            }
            else
            {
                Log.Warning("cookies.json file was read, but no valid cookies were found..");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error reading or importing manual cookies from cookies.json");
        }
    }    Log.Information("Go to TikTok Messages...");
    await page.GotoAsync("https://www.tiktok.com/messages");

    var genericChatLocator = page.Locator("[data-e2e='dm-new-conversation-item']").First;
    
    try
    {
        Log.Information("Checking active session...");
        await genericChatLocator.WaitForAsync(new() 
            { State = WaitForSelectorState.Visible, Timeout = 7000 });
        Log.Information("Session active! Moving forward.");
    }
    catch (TimeoutException)
    {
        if (config.Headless)
        {
            Log.Fatal("Session not found! Cannot login manually in Headless mode on server. Run locally first.");
            return;
        }

        Log.Warning("Session expired or empty. Redirecting to login page...");
        await page.GotoAsync("https://www.tiktok.com/login");
        Log.Information("Please log in to your account. Waiting up to 3 minutes...");
        
        await genericChatLocator.WaitForAsync(new()
            { State = WaitForSelectorState.Visible, Timeout = 180000 });
        Log.Information("Login successful! Profile updated.");
    }

    foreach (var friend in config.NameStreaks)
    {
        Log.Information("Start target searching : {Friend}", friend);
        
        bool found = false;
        int maxScrollAttempts = 40; 
        
        var targetChatLocator = page.Locator("[data-e2e='dm-new-conversation-item']")
            .Filter(new()
            {
                Has = page.Locator("[data-e2e='dm-new-conversation-nickname']").Filter(new() 
                    { HasText = friend })
            });
        
        for (int i = 0; i < maxScrollAttempts; i++)
        {
            if (await targetChatLocator.CountAsync() > 0 && await targetChatLocator.First.IsVisibleAsync())
            {
                Log.Debug("Target {Friend} founded on screen.", friend);
                found = true;
                break;
            }

            Log.Debug("Scroll chat panel... Attempt {Attempt}", i + 1);
    
            if (await genericChatLocator.CountAsync() > 0)
            {
                await genericChatLocator.HoverAsync();
                await page.Keyboard.PressAsync("PageDown");
            }
            await Task.Delay(3000); 
        }

        if (!found)
        {
            Log.Warning("Could not find the target {Friend} after reaching the maximum number of scrolls. Skipping", friend);
            continue;
        }
        
        try
        {
            await targetChatLocator.First.ClickAsync();
            Log.Information("Chat with {Friend} opened. Waiting for an input field...", friend);

            var inputArea = page.Locator("[data-e2e='dm-new-input-editor'] [contenteditable='true']");
            await inputArea.WaitForAsync(new() { Timeout = 5000 });
            await inputArea.ClickAsync();

            await inputArea.TypeAsync(config.DefaultText, new() { Delay = 100 });
            await Task.Delay(1000);
            await page.Keyboard.PressAsync("Enter");

            Log.Information("The message was successfully sent to {Friend}!", friend);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while sending a message to the target {Friend}", friend);
            await page.ScreenshotAsync(new() { Path = $"logs/error_{friend}.png" });
        }

        await Task.Delay(4000); 
    }

    Log.Information("All contacts in the list have been processed.");
    await Task.Delay(3000);//todo delete
}
catch (Exception ex)
{
    Log.Fatal(ex, "Critical error while the bot is running");
}
finally
{
    Log.CloseAndFlush(); 
}