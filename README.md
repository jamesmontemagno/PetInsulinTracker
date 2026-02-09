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

### 🔗 Sharing & Access Control
- Generate 6-character share codes (Full Access or Guest Access) to share a pet's profile
- **Full Access** — family members can view all logs, pet info, and log new entries
- **Guest Access** — pet sitters can view pet info and log entries but not see other people's logs
- Owner and full-access users can create and manage share codes
- Each share code tracks who created it and when
- View and delete active share codes from the Share page
- Deleting a code prevents future redemptions — existing access is not revoked
- Owner can see all people with access and revoke any individual's access
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
- Azure Table Storage as cloud store (partitioned by OwnerId / PetId)
- Last-write-wins conflict resolution
- Sync triggered manually from Settings

### Share System

Share codes let pet owners grant access to family members or pet sitters without an account or sign-in.

#### Access Levels
| Level | Can view pet info | Can log entries | Sees all logs | Create/manage share codes | Manage people |
|-------|:-:|:-:|:-:|:-:|:-:|
| **Owner** | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Full** | ✅ | ✅ | ✅ | ✅ | ❌ |
| **Guest** | ✅ | ✅ | ❌ | ❌ | ❌ |

#### Code Lifecycle
1. **Create** — Owner or full-access user taps "+" on the Share page and picks Full Access or Guest Access. A unique 6-character alphanumeric code is generated server-side (charset excludes ambiguous chars: `O`, `0`, `1`, `I`, `L`).
2. **Share** — The code is displayed and can be copied to the clipboard. Creator name and creation date are recorded.
3. **Redeem** — A recipient enters the code on the Welcome or Import Pet page. The pet and all relevant data are downloaded and saved locally.
4. **Delete** — Owner or full-access users can delete a code to prevent future redemptions. Anyone who already redeemed it keeps their access.
5. **Revoke** — The pet owner can revoke a specific user's access from the "People with Access" list. Revoked users will receive a `403 Forbidden` on their next sync.

#### Azure Table Storage Layout
| Table | PartitionKey | RowKey | Purpose |
|-------|-------------|--------|--------|
| `Pets` | OwnerId | PetId | Pet profiles |
| `ShareCodes` | PetId | Code | Active share codes with creator metadata |
| `ShareRedemptions` | PetId | DeviceUserId | Who redeemed which code; tracks revocation |
| `InsulinLogs` | PetId | LogId | Insulin administration records |
| `FeedingLogs` | PetId | LogId | Feeding records |
| `WeightLogs` | PetId | LogId | Weight records |
| `VetInfos` | PetId | VetInfoId | Veterinarian contact info |
| `Schedules` | PetId | ScheduleId | Insulin/feeding/vet schedules |

#### API Endpoints (Azure Functions)
| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/share/generate` | Owner/Full | Generate a new share code |
| POST | `/share/redeem` | Any | Redeem a code and download pet data |
| GET | `/share/pet/{petId}/codes` | Owner/Full | List all active codes for a pet |
| DELETE | `/share/{code}` | Owner/Full | Delete a share code |
| GET | `/share/pet/{petId}/users` | Owner/Full | List all users with access |
| POST | `/share/revoke` | Owner | Revoke a specific user's access |
| POST | `/share/leave` | Self | Remove your own access to a shared pet |
| POST | `/sync` | Any member | Bi-directional sync for a single pet |
| POST | `/pets` | Owner | Create a new pet |
| POST | `/pets/delete` | Owner | Delete a pet and all associated data |

## 🔐 CI/CD Secrets

GitHub Actions workflows rely on these repository secrets:
- Azure Functions deploy ([.github/workflows/main_petinsulintracker.yml](.github/workflows/main_petinsulintracker.yml)): `AZUREAPPSERVICE_CLIENTID_95BD0AE70DA5486DB584273BD3C00BD4`, `AZUREAPPSERVICE_TENANTID_2F54073DBFA7478F9B2E7345654A59CE`, `AZUREAPPSERVICE_SUBSCRIPTIONID_8D56788940EF406B846605A315DCE`.
- Android release build & signing ([.github/workflows/maui-android.yml](.github/workflows/maui-android.yml)): optional `API_BASE_URL` override; release signing requires `ANDROID_KEYSTORE` (base64), `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS`.
- iOS TestFlight build & signing ([.github/workflows/maui-ios.yml](.github/workflows/maui-ios.yml)): optional `API_BASE_URL`; signing requires `APPSTORE_CERTIFICATE_P12` (base64), `APPSTORE_CERTIFICATE_P12_PASSWORD`, `APPSTORE_CODESIGN_KEY` (App Store signing identity), and App Store Connect API keys `APPSTORE_ISSUER_ID`, `APPSTORE_KEY_ID`, `APPSTORE_PRIVATE_KEY`.
- GitHub Pages deploy ([.github/workflows/pages.yml](.github/workflows/pages.yml)): no secrets required.

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
