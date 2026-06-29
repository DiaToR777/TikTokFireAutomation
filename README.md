# 🔥 TikTok Fire Automation

Automatically keep your TikTok Streaks alive by sending messages through the official TikTok Web interface.  
Автоматична підтримка "вогників" (Streaks) в TikTok через офіційний веб-інтерфейс.

---

## 🌐 Language / Мова
* 🇬🇧 [English Documentation](#-english-documentation)
* 🇺🇦 [Українська документація](#-українська-документація)

---

# 🇬🇧 English Documentation

TikTok Fire Automation simulates real user actions: it initializes a session, scrolls through your DM list, finds specific friends by their display names, and sends them a predefined message.

## ⚙️ Local Installation & Run

### 📌 Requirements
* **[.NET 10.0 SDK](https://dotnet.microsoft.com/download)**.
* **[Git](https://git-scm.com/downloads)** installed.
* Terminal supporting **PowerShell** (Windows) or **Bash** (Linux/macOS).

### 🚀 Setup Steps
1. **Clone the repository and navigate directly to the project folder:**
   ```bash
   git clone https://github.com/diator777/TikTokFireAutomation.git && cd TikTokFireAutomation/TikTokFireAutomation
   
2. **Build and run the project for the first time** to generate the required folders:
   ```bash
   dotnet run

3. Install Playwright browser binaries (required once):
* **Windows (PowerShell):** `pwsh bin/Debug/net10.0/playwright.ps1 install` OR (win10) `powershell -ExecutionPolicy Bypass -File bin/Debug/net10.0/playwright.ps1 install`
* **Linux / macOS (Bash):** `node bin/Debug/net10.0/playwright.js install`


4. Create a `config.json` file in `bin/Debug/net10.0/`.

## 📝 Configuration (config.json & cookies.json)

### 1. `config.json`

```json
{
  "NameStreaks": [
    "Friend Name 1",
    "Friend Name 2"
  ],
  "DefaultText": "+",
  "CustomUserAgent": null,
  "Headless": true
}

```

* `NameStreaks`: Array of TikTok chat display names (exact match required).
* `DefaultText`: Message content. If empty, defaults to `+`.
* `CustomUserAgent`: Custom User-Agent string. If `null`, default Firefox is used.
* `Headless`: Set to `false` for local run (to pass captcha), and `true` for Docker/VPS.

### 2. Session Import (`cookies.json`)

Export your TikTok cookies using any browser extension (like *Cookie-Editor*) in **JSON** format. Save it as `cookies.json` in the root folder.

> 💡 *Note:* This file is used for the initial import. Afterwards, the bot will maintain the session inside the `user_profile` folder.

## 🐳 Server Deployment (Docker Compose)

### 🚀 Deploy Steps on VPS:

1. Create a working directory on your server:
```bash
mkdir -p /opt/tiktok-bot && cd /opt/tiktok-bot

```


2. Upload your `config.json`, `cookies.json` to this directory.
3. Create a `docker-compose.yaml` file:

```yaml
services:
  tiktok-bot:
    image: ghcr.io/diator777/tiktok-fire-automation:latest
    container_name: tiktok-automation-bot
    init: true
    restart: "no"
    volumes:
      - ./config.json:/app/config.json
      - ./cookies.json:/app/cookies.json
      - ./logs:/app/logs
      - ./user_profile:/app/user_profile

```

4. Run the container:
```bash
docker compose up

```



### ⏰ Automation (Daily Cron Job)

To run the bot automatically every day at 10:00 AM, add a cron job on your server:

```bash
crontab -e
# Add the following line:
0 10 * * * cd /opt/tiktok-bot && /usr/bin/docker compose up >> /opt/tiktok-bot/logs/cron.log 2>&1

```

---

# 🇺🇦 Українська документація

Бот імітує дії реального користувача: ініціалізує сесію, прокручує список діалогів, знаходить друзів за їхніми відображуваними іменами та відправляє їм заданий текст.

## ⚙️ Локальне встановлення та запуск

### 📌 Вимоги

* Встановлений **[.NET 10.0 SDK](https://dotnet.microsoft.com/download)**.
* Термінал з підтримкою **PowerShell** (Windows) або **Bash** (Linux/macOS).

### 🚀 Покроковий запуск 

1. **Клонуйте репозиторій та відразу перейдіть у папку з проєктом:**
```bash
git clone https://github.com/diator777/TikTokFireAutomation.git && cd TikTokFireAutomation/TikTokFireAutomation

```


2. **Зберіть та запустіть проєкт вперше**, щоб згенерувати необхідні папки:
```bash
dotnet run

```


3. **Встановіть бінарники браузерів Playwright (виконується один раз):**
* **Windows (PowerShell):** `pwsh bin/Debug/net10.0/playwright.ps1 install` АБО (Win 10): `powershell -ExecutionPolicy Bypass -File bin/Debug/net10.0/playwright.ps1 install`
* **Linux / macOS (Bash):** `node bin/Debug/net10.0/playwright.js install`


4. **Створіть файл `config.json` у папці `bin/Debug/net10.0/`.**

## 📝 Налаштування конфігурації (config.json та cookies.json)

### 1. Файл `config.json`

```json
{
  "NameStreaks": [
    "Ім'я Друга 1",
    "Ім'я Друга 2"
  ],
  "DefaultText": "+",
  "CustomUserAgent": null,
  "Headless": true
}

```

* `NameStreaks` — масив відображуваних імен у чатах TikTok (пошук за *точним збігом*).
* `DefaultText` — текст повідомлення (якщо порожньо, відправить `+`).
* `CustomUserAgent` — рядок User-Agent. Якщо `null`, використовується дефолтний Firefox.
* `Headless` — `false` для локального запуску (щоб пройти капчу руками), та `true` для Docker/VPS.

### 2. Імпорт сесії (`cookies.json`)

Експортуйте куки вашого акаунту TikTok за допомогою браузерного розширення (наприклад, *Cookie-Editor*) у форматі **JSON** та збережіть як `cookies.json` у корінь проєкту.

> 💡 *Примітка:* Цей файл потрібен лише для першого імпорту. Далі бот підтримуватиме сесію всередині папки `user_profile`.

## 🐳 Розгортання на сервері (Docker Compose)

### 🚀 Порядок деплою на VPS:

1. Створіть робочу директорію на сервері:
```bash
mkdir -p /opt/tiktok-bot && cd /opt/tiktok-bot

```


2. Перенесіть туди ваші файли `config.json`, `cookies.json`.
3. Створіть файл `docker-compose.yml`:

```yaml
services:
  tiktok-bot:
    image: ghcr.io/diator777/tiktok-fire-automation:latest
    container_name: tiktok-automation-bot
    init: true
    restart: "no"
    volumes:
      - ./config.json:/app/config.json
      - ./cookies.json:/app/cookies.json
      - ./logs:/app/logs
      - ./user_profile:/app/user_profile

```

4. Запустіть контейнер:
```bash
docker compose up

```



### ⏰ Автоматизація (Запуск щодня через Cron)

Щоб бот автоматично запускався щодня, наприклад, о 10:00 ранку, налаштуйте `cron` на сервері:

```bash
crontab -e
# Додайте цей рядок:
0 10 * * * cd /opt/tiktok-bot && /usr/bin/docker compose up >> /opt/tiktok-bot/logs/cron.log 2>&1
```


## 🛠️ Logs & Troubleshooting / Логи та Помилки

* **Logs:** All actions are logged to the console and to `./logs/bot-log.txt`. / Всі дії пишуться в консоль та зберігаються у `./logs/bot-log.txt`.
* **Screenshots:** If an error occurs, the bot saves a screenshot at `./logs/error_[Name].png`. / Якщо сталася помилка, бот збереже скріншот у папку `./logs/error_[Ім'я].png`.
