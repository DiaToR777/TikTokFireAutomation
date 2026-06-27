using Microsoft.Playwright;
using Serilog;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TikTokFireAutomation;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/bot-log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    // 1. Парсим конфиг
    string configPath = "config.json";
    if (!File.Exists(configPath))
    {
        Log.Error("Файл конфигурации config.json не найден!");
        return;
    }
    
    var jsonText = await File.ReadAllTextAsync(configPath);
    var config = JsonSerializer.Deserialize<AppConfig>(jsonText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    
    if (config == null || config.NameStreaks.Count == 0)
    {
        Log.Warning("Список NameStreaks пуст или конфиг не валиден.");
        return;
    }

    Log.Information("Конфиг успешно загружен. Найдено целей: {Count}", config.NameStreaks.Count);

    // 2. Инициализация Playwright
    using var playwright = await Playwright.CreateAsync();
    var launchOptions = new BrowserTypeLaunchOptions { Headless = false, SlowMo = 500 };
    await using var browser = await playwright.Firefox.LaunchAsync(launchOptions);

    string statePath = "state.json";
    var contextOptions = new BrowserNewContextOptions
    {
        StorageStatePath = statePath,
        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:152.0) Gecko/20100101 Firefox/152.0"
    };

    await using var context = await browser.NewContextAsync(contextOptions);
    var page = await context.NewPageAsync();

    Log.Information("Переход в TikTok Messages...");
    await page.GotoAsync("https://www.tiktok.com/messages");

    // 3. Ожидаем загрузки самого списка чатов
    // Берем первый попавшийся элемент чата, чтобы убедиться, что страница отрендерилась
    var genericChatLocator = page.Locator("[data-e2e='dm-new-conversation-item']").First;
    Log.Information("Ожидаем появление элементов интерфейса сообщений...");
    await genericChatLocator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });

    // 4. Главный цикл по всем кентам из конфига
    foreach (var friend in config.NameStreaks)
    {
        Log.Information("Начинаю поиск контакта: {Friend}", friend);
        
        bool found = false;
        int maxScrollAttempts = 40; 
        
        // Селектор конкретного чата на основе переданного имени
        var targetChatLocator = page.Locator("[data-e2e='dm-new-conversation-item']")
            .Filter(new()
            {
                Has = page.Locator("[data-e2e='dm-new-conversation-nickname']").Filter(new() { HasText = friend })
            });
        
        for (int i = 0; i < maxScrollAttempts; i++)
        {
            if (await targetChatLocator.First.CountAsync() > 0 && await targetChatLocator.First.IsVisibleAsync())
            {
                Log.Debug("Контакт {Friend} найден на экране.", friend);
                found = true;
                break;
            }

            Log.Debug("Скроллим панель чатов... Попытка {Attempt}", i + 1);
    
            // Надежный скролл: наводим мышь на первый элемент в списке и жмем PageDown.
            // Браузер автоматически прокрутит именно тот контейнер, над которым находится курсор.
            if (await genericChatLocator.CountAsync() > 0)
            {
                await genericChatLocator.HoverAsync();
                await page.Keyboard.PressAsync("PageDown");
            }
    
            await Task.Delay(800); // Пауза для перестроения translateY блоков в DOM-дереве
        }

        if (!found)
        {
            Log.Warning("Не удалось найти контакт {Friend} после максимального количества скроллов. Пропускаю.", friend);
            continue;
        }

        // 5. Кликаем и отправляем сообщение
        try
        {
            await targetChatLocator.First.ClickAsync();
            Log.Information("Чат с {Friend} открыт. Ожидаю поле ввода...", friend);

            var inputArea = page.Locator("[data-e2e='dm-new-input-editor'] [contenteditable='true']");
            await inputArea.WaitForAsync(new() { Timeout = 5000 });
            await inputArea.ClickAsync();

            // Вбиваем текст из конфига
            await inputArea.TypeAsync(config.DefaultText, new() { Delay = 100 });
            await Task.Delay(300);
            await page.Keyboard.PressAsync("Enter");

            Log.Information("Сообщение успешно отправлено для {Friend}!", friend);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при отправке сообщения для {Friend}", friend);
            await page.ScreenshotAsync(new() { Path = $"logs/error_{friend}.png" });
        }

        await Task.Delay(2500); // Безопасный интервал между контактами
    }

    Log.Information("Все контакты из списка обработаны.");
    await Task.Delay(3000);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Критическая ошибка во время работы бота");
}
finally
{
    Log.CloseAndFlush(); 
}