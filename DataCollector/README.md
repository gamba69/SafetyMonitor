# SafetyMonitorData

Консольная утилита для сбора данных с устройств ASCOM Alpaca (ObservingConditions и SafetyMonitor) и сохранения в шардированную базу данных FireBird.

## Особенности логирования

Все сообщения выводятся в **единообразном формате БЕЗ ЦВЕТОВ** для простоты обработки:

```
[2026-02-03 10:15:30] [INFO] Message text
[2026-02-03 10:15:31] [WARN] Warning text
[2026-02-03 10:15:32] [ERROR] Error text
[2026-02-03 10:15:33] [DATA]
  Temperature:      15.50°C
  Humidity:         65.0%
  Safety Status:    SAFE
```

## Требования

- .NET 10.0 или выше
- FireBird 5.0 Server (если используется сохранение в БД)
- Библиотека **DataStorage** (должна быть доступна в соседнем каталоге)
- ASCOM Alpaca устройства в сети

## Установка

### Структура каталогов

```
YourWorkspace/
├── DataStorage/
│   └── DataStorage.csproj
└── SafetyMonitorData/
    └── SafetyMonitorData.csproj
```

### Сборка

```bash
dotnet restore
dotnet build
```

## Использование

### Параметры

```bash
# ObservingConditions (обязательно):
--oc-name <name>           # Имя для discovery
--oc-address <ip>          # IP адрес (с --oc-port)
--oc-port <port>           # Порт
--oc-device-number <n>     # Номер устройства (по умолчанию: 0)

# SafetyMonitor (обязательно):
--sm-name <name>
--sm-address <ip>
--sm-port <port>
--sm-device-number <n>

# Retry:
--discovery-retries <n>    # По умолчанию: 3
--data-retries <n>         # По умолчанию: 3
--retry-delay <ms>         # По умолчанию: 1000

# Вывод:
--quiet                    # Отключить вывод данных

# База данных:
--storage-path <path>
--db-user <user>           # По умолчанию: SYSDBA
--db-password <pass>       # По умолчанию: masterkey

# Режим:
--continuous               # Непрерывный режим
--interval <seconds>       # По умолчанию: 3
--error-retry-delay <sec>  # Задержка после фатальной ошибки (по умолчанию: 30)
```

### Поведение в continuous режиме

В режиме `--continuous` программа **НИКОГДА не завершается сама**:
- При любой ошибке (discovery, подключение, сбор данных) программа ждет `--error-retry-delay` секунд и пытается снова
- Автоматическое переподключение к устройствам при потере связи
- Единственный способ остановить - нажать Ctrl+C
- Это позволяет программе работать как сервис/демон без необходимости внешнего мониторинга

### Примеры

```bash
# Однократный сбор
dotnet run -- --oc-name "Weather" --sm-name "Safety"

# Непрерывный сбор с БД
dotnet run -- \
  --oc-address 192.168.1.100 --oc-port 11111 \
  --sm-address 192.168.1.101 --sm-port 11111 \
  --continuous --interval 5 \
  --storage-path "C:\AlpacaData"

# Тихий режим
dotnet run -- \
  --oc-name "Weather" --sm-name "Safety" \
  --quiet --continuous --storage-path "/data"
```

## Зависимости

- **ASCOM.Alpaca.Components 3.0.0**
- **ASCOM.Common.Components 3.0.0**
- **System.CommandLine 2.0.2**
- **Microsoft.Extensions.DependencyInjection 9.0.0** (только DI, без Logging)
- **DataStorage** (ProjectReference)

## Лицензия

См. LICENSE файл.
