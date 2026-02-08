# 🐾 Insulog — Pet Insulin & Wellness Tracker

> **[🌐 Visit the Insulog website](https://jamesmontemagno.github.io/PetInsulinTracker/)**

**Insulog** is a cross-platform .NET MAUI app designed to help pet owners manage their diabetic pet's insulin regimen, feeding schedule, weight tracking, and overall wellness. Built with love for the furry family members who depend on us.

---

## ✨ Features

### 💉 Insulin Tracking
- Log every insulin shot with dose (IU), injection site, date/time, and notes
- Quick-log insulin at the prescribed dose with one tap
- Visual dose countdown timer showing time until next injection
- Support for all common insulin types: Vetsulin, ProZinc, NPH, Glargine

### 🍽️ Feeding Log
- Track meals with food name, amount, unit, and food type (wet/dry/treat)
- History view with swipe-to-delete

### ⚖️ Weight Tracking
- Log weight in lbs or kg
- Interactive weight trend chart (SkiaSharp line chart)
- Track weight changes over time

### 📅 Schedules & Reminders
- Create recurring schedules for insulin, feeding, and vet visits
- Local push notifications to never miss a dose or meal
- Configurable reminder lead time

### 🏥 Vet Information & Pro Tips
- Store veterinarian contact details (phone, emergency line, address, email)
- One-tap call buttons for vet and emergency
- Built-in diabetic pet care tips: hypoglycemia signs, injection technique, feeding timing, insulin storage

### 🔗 Sharing
- Generate a 6-character share code to share your pet's profile
- Family members or pet sitters can import the pet and log entries
- Cloud sync via Azure Functions + Table Storage

### 🎨 Themes & Personalization
- 5 beautiful color themes: Warm, Ocean, Forest, Berry, Midnight
- Full light and dark mode support
- Pet photo support with camera and gallery
- Owner name tracking on every log entry
- Welcome page for first-time setup

### 📱 Multi-Pet Support
- Manage multiple diabetic pets from one app
- Each pet has its own dashboard, logs, schedules, and vet info

---

## 🛠️ Tech Stack

| Component | Technology |
|-----------|------------|
| **Framework** | .NET MAUI (.NET 10) |
| **Architecture** | MVVM with CommunityToolkit.Mvvm |
| **Local Storage** | SQLite (sqlite-net-pcl) |
| **Cloud Backend** | Azure Functions (Isolated Worker) |
| **Cloud Storage** | Azure Table Storage |
| **UI Components** | SkiaSharp, CommunityToolkit.Maui |
| **Icons** | MauiIcons.Fluent (Microsoft Fluent icons) |
| **Notifications** | Plugin.LocalNotification |
| **Platforms** | iOS, Android, macOS (Catalyst), Windows |

---

## 📂 Project Structure

```
PetInsulinTracker/
├── PetInsulinTracker/           # .NET MAUI app
│   ├── Models/                  # Data models (Pet, InsulinLog, FeedingLog, etc.)
│   ├── ViewModels/              # MVVM ViewModels with CommunityToolkit
│   ├── Views/                   # XAML pages
│   ├── Services/                # Database, Sync, Notification services
│   ├── Controls/                # Custom SkiaSharp controls
│   ├── Converters/              # Value converters
│   ├── Themes/                  # Theme service with 5 palettes
│   └── Helpers/                 # Constants and utilities
├── PetInsulinTracker.Api/       # Azure Functions API
│   └── Functions/               # HTTP-triggered sync endpoints
├── PetInsulinTracker.Shared/    # Shared models/DTOs
└── marketing/                   # App store assets and descriptions
```

---

## 🏗️ Building

### Prerequisites
- .NET 10 SDK
- Visual Studio 2022 17.x+ or VS Code with C# Dev Kit
- Platform-specific workloads: `maui-android`, `maui-ios`, `maui-maccatalyst`, `maui-windows`

### Build Commands

```bash
# Restore packages
dotnet restore PetInsulinTracker/PetInsulinTracker.csproj

# Build for macOS
dotnet build PetInsulinTracker/PetInsulinTracker.csproj -f net10.0-maccatalyst

# Build for Android
dotnet build PetInsulinTracker/PetInsulinTracker.csproj -f net10.0-android

# Build for iOS
dotnet build PetInsulinTracker/PetInsulinTracker.csproj -f net10.0-ios

# Run on macOS
dotnet run --project PetInsulinTracker/PetInsulinTracker.csproj -f net10.0-maccatalyst

# Build Azure Functions API
dotnet build PetInsulinTracker.Api/PetInsulinTracker.Api.csproj
```

---

## 🧩 Architecture

### MVVM Pattern
- **CommunityToolkit.Mvvm** source generators: `[ObservableProperty]`, `[RelayCommand]`
- All ViewModels are `partial` classes inheriting from `ObservableObject`
- `WeakReferenceMessenger` for cross-ViewModel communication
- Constructor-injected services via .NET MAUI DI

### Offline-First Sync
- SQLite for local persistence with full CRUD
- `IsSynced` flag and `LastModified` timestamp on every model
- Azure Table Storage as cloud store (partitioned by share code)
- Last-write-wins conflict resolution
- Sync triggered manually from Settings

### Share System
- 6-character alphanumeric codes (charset excludes ambiguous chars: O, 0, 1, I, L)
- Pet data uploaded to Azure Table Storage with share code as partition key
- Recipients import by entering the code

---

## 📄 License

This project is provided as-is for educational and personal use.

---

## 🙏 Acknowledgements

- [.NET MAUI](https://dotnet.microsoft.com/apps/maui) — Cross-platform app framework
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) — MVVM toolkit
- [SkiaSharp](https://github.com/mono/SkiaSharp) — 2D graphics library
- [MauiIcons](https://github.com/AathifMahir/MauiIcons) — Fluent icon integration
- [Plugin.LocalNotification](https://github.com/thudugala/Plugin.LocalNotification) — Local notifications
