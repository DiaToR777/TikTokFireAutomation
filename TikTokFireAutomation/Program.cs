using Microsoft.Playwright;

using var playwright = await Playwright.CreateAsync();

var launchOptions = new BrowserTypeLaunchOptions
{
    Headless = true,
    SlowMo = 1000,
};

await using var browser = await playwright.Firefox.LaunchAsync(launchOptions);

string statePath = "state.json"; 

var contextOptions = new BrowserNewContextOptions
{
    StorageStatePath = statePath, 
    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:152.0) Gecko/20100101 Firefox/152.0",
};

await using var context = await browser.NewContextAsync(contextOptions);
var page = await context.NewPageAsync();

Console.WriteLine("Загружаю TikTok Messages напрямую через куки...");
await page.GotoAsync("https://www.tiktok.com/messages");

await Task.Delay(10000); //TODO random delay 

Console.WriteLine("Ожидаю загрузки чатов...");
await page.WaitForSelectorAsync("[data-e2e='dm-new-conversation-nickname']");

string friendNickname = "ещкере"; 
Console.WriteLine($"Ищу чат с {friendNickname}...");

var chatListItem = page.Locator("[data-e2e='dm-new-conversation-nickname']")
    .Filter(new() { HasText = friendNickname });

await chatListItem.ClickAsync();
Console.WriteLine("Чат открыт. Жду поле ввода...");

var inputArea = page.Locator("[data-e2e='dm-new-input-editor'] [contenteditable='true']");
await inputArea.WaitForAsync();

await inputArea.ClickAsync();
Console.WriteLine("Вбиваю текст сообщения...");
await inputArea.TypeAsync("🔥 Привет! Тестирую бота.", new() { Delay = 100 }); //TODO: random words/text

await Task.Delay(500); //TODO random delay
await page.Keyboard.PressAsync("Enter");

Console.WriteLine("Огонёк отправлен успешно!");