# Детелина / Eltrade BackOffice — Крос-таблични (Pivot / Matrix) справки

Модерно, бързо и самостоятелно Windows десктоп приложение (C# .NET Framework 4.8 с WPF / Material Design) за генериране на обобщени крос-таблични (Pivot Matrix) справки от база данни на Firebird 3.0 (.FDB / .GDB), използвана от търговски софтуер Eltrade BackOffice / Детелина.
Проектът е таргетиран към **.NET Framework 4.8** (`<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>`), което позволява директно стартиране на клиентски машини с Windows 10/11 без нужда от инсталиране на допълнителни .NET Core/.NET 8 runtimes.

---

## 🚀 Основни възможности

1. **Динамична крос-таблица (Pivot / Matrix Grid):**
   - **Редове:** Артикули (Код `SPLU_PLUNUMB`, Наименование `PLU_NAME`).
   - **Колони:** Динамично генерирани за всеки отделен ден от избрания период (напр. `01.09 (Пн)`, `02.09 (Вт)`).
   - **Стойности в клетките:** Сумирано продадено количество (`SPLU_SOLDQUANT`) за съответния артикул на съответната дата. Нулевите стойности се визуализират прегледно с тире (`-`).
   - **Крайна колона:** `ОБЩО` (Общо продадено количество за артикула за целия период).
   - **Сортиране:** Еднократно кликване на която и да е колона (код, артикул, произволна дата, общо) сортира незабавно числата във възходящ/низходящ ред.

2. **Информационни карти (KPIs):**
   - 📦 **Общо продадени бройки** за филтрирания период.
   - 🏷️ **Брой уникални артикули** с регистрирани продажби.
   - 📅 **Активни дни** с продажби.
   - ⭐ **Най-продаван артикул** с точното му количество.
   - 📈 **Най-силен (пиков) ден** на продажбите.

3. **Гъвкава филтрация:**
   - **Училище / Артикулна група (`N_PLUGROUPS`):** Избор от падащо меню с търсене и визуализация на подгрупи.
   - **Включи подгрупи:** Чекбокс за автоматично рекурсивно включване на всички вложени подгрупи (класове/секции).
   - **POS Терминал (`SELL_TERMINAL`):** Филтриране по конкретен терминал (напр. Слънчев бряг, Цар Калоян, Мол Галерия, ул. Ивайло) или "Всички терминали".
   - **Бързи периоди:** Бутони за един клик:
     - `Днес`
     - `Последен 1 ден`
     - `10 дни`
     - `30 дни`
     - `50 дни`
     - `Този месец`
   - **Ръчен избор на период:** `DatePicker` за `От дата` и `До дата`.
   - **Скриване на празни дни:** Опция за показване на колони само за дните, в които действително има продажби.
   - **Бързо филтриране в таблицата (Live Search):** Моментално търсене по име или код на артикул директно в интерфейса без повторна заявка към базата.

4. **Експорт и интеграция:**
   - 📊 **Експорт в Microsoft Excel (.xlsx):** Чрез ClosedXML — генерира стилизиран Excel файл с оцветени заглавия, замразени заглавни колони (Freeze Panes), авто-ширина на колоните, сумиращ ред и числово форматиране.
   - 📄 **Експорт в CSV:** Запис със знак точка и запетая (`;`) и UTF-8 BOM, съвместим на 100% с българските регионални настройки на Excel.
   - 📋 **Копиране в клипборда:** Копира таблицата като TSV за директно поставяне (`Ctrl+V`) в Excel.

5. **Настройки на Firebird връзката и външен файл:**
   - Прозорец за настройки с **тест на връзката на живо** (показва Firebird версията).
   - Всички настройки се записват във външен файл `appsettings.json` в папката на програмата:
     - `Host` (напр. `localhost` или IP)
     - `Port` (3050)
     - `Database` (път до `.GDB` или `.FDB`)
     - `User` (`SYSDBA` или `ATMKIOSK`)
     - `Password` (`masterkey` или служебна парола)
     - `Charset` (`WIN1251`)
     - Имена на терминалите за лесно разпознаване.

---

## 🗄️ Свързване с Firebird 3.0

Приложението използва `FirebirdSql.Data.FirebirdClient` v10.0.0.

При стартиране се регистрира енкодинг провайдър за пълна съвместимост:
```csharp
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
```
Това гарантира коректна поддръжка на кирилица (`WIN1251`) за всички наименования на артикули и училища от Eltrade BackOffice.

### Конфигурация в `appsettings.json`:
```json
{
  "Firebird": {
    "Host": "localhost",
    "Port": 3050,
    "Database": "C:\\Users\\a.andonov\\АТМ\\ELTRADEBACKOFFICE.GDB",
    "User": "SYSDBA",
    "Password": "masterkey",
    "Charset": "WIN1251",
    "ConnectionTimeout": 15
  },
  "ReportSettings": {
    "DefaultPeriod": "ThisMonth",
    "HideEmptyDays": false,
    "IncludeSubgroups": true,
    "TerminalNames": {
      "1": "Слънчев бряг (Пос 1)",
      "2": "Цар Калоян (Пос 2)",
      "3": "Галерия (Пос 3)",
      "4": "Ивайло (Пос 4)"
    }
  }
}
```

---

## 🛠️ Как се стартира и компилира

### Вариант 1: Чрез Visual Studio 2022
1. Отворете `DetelinaPivotReports.sln`.
2. Изберете конфигурация `Release` или `Debug` и натиснете `F5` (Start).

### Вариант 2: Чрез конзолата (dotnet CLI)
```bash
cd DetelinaPivotReports
dotnet run --project DetelinaPivotReports\DetelinaPivotReports.csproj
```

### Вариант 3: Еднократно компилиране за .NET Framework 4.8
Стартирайте файла `publish_standalone.bat` или `СЪЗДАЙ_EXE.bat` с двоен клик.
Той ще създаде папка `publish/`, съдържаща:
- `DetelinaPivotReports.exe` — изпълним файл, готов за директно стартиране на всеки компютър с Windows 10/11 с вградения .NET Framework 4.8 без допълнителни инсталации!
- `appsettings.json` — за настройка на връзката към базата.

---

## 📁 Структура на решението

```text
DetelinaPivotReports/
├── DetelinaPivotReports.sln                # Visual Studio Solution
├── publish_standalone.bat                  # Скрипт за компилиране (.NET Framework 4.8)
├── README.md                               # Тази документация
└── DetelinaPivotReports/
    ├── DetelinaPivotReports.csproj         # Проект (.NET Framework 4.8 WPF)
    ├── App.config                          # Конфигурация на .NET Framework runtime
    ├── appsettings.json                    # Външни настройки на връзката
    ├── App.xaml / App.xaml.cs              # Приложение, теми, грешки, WIN1251 регистрация
    ├── MainWindow.xaml / .cs               # Главен екран с динамичен Pivot DataGrid
    ├── Models/
    │   ├── DatabaseSettings.cs             # Модел за Firebird настройки
    │   ├── PlugroupItem.cs                 # Модел за група/училище
    │   ├── TerminalItem.cs                 # Модел за терминал
    │   ├── ReportFilter.cs                 # Модел за филтрите на справката
    │   ├── ArticleSaleRecord.cs            # Модел на запис от SQL
    │   └── PivotReportResult.cs            # Модел на матричния резултат + DataTable
    ├── Services/
    │   ├── IConfigService / ConfigService  # Четене и запис на appsettings.json
    │   ├── IFirebirdService / FirebirdService # Firebird SQL заявки и тестове
    │   ├── IPivotReportService / PivotReportService # Матрично агрегиране и йерархия
    │   └── IExportService / ExportService  # ClosedXML Excel (.xlsx), CSV, Clipboard
    ├── ViewModels/
    │   ├── ViewModelBase.cs                # INotifyPropertyChanged база
    │   ├── RelayCommand.cs                 # ICommand реализация
    │   ├── MainViewModel.cs                # Основна бизнес логика и управление на изгледа
    │   └── SettingsViewModel.cs            # Логика за прозореца с настройки и тест
    ├── Views/
    │   └── SettingsWindow.xaml / .cs       # Прозорец за настройки на базата данни
    └── Converters/
        └── ValueConverters.cs              # Форматиране на числа, нули към тире и видимост
```
