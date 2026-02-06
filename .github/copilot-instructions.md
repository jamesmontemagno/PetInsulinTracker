# Copilot Instructions — PetInsulinTracker

## Build & Run

This is a .NET MAUI app targeting .NET 10. There is no `.sln` file; build directly from the project:

```bash
# Restore
dotnet restore PetInsulinTracker/PetInsulinTracker.csproj

# Build for macOS (default on Mac)
dotnet build PetInsulinTracker/PetInsulinTracker.csproj -f net10.0-maccatalyst

# Build for other targets
dotnet build PetInsulinTracker/PetInsulinTracker.csproj -f net10.0-android
dotnet build PetInsulinTracker/PetInsulinTracker.csproj -f net10.0-ios
```

Always specify a `-f` target framework — multi-targeting means bare `dotnet build` may fail or build all targets.

## Architecture

- **Single-project MAUI app** using Shell navigation (`AppShell.xaml`).
- Entry point: `MauiProgram.cs` → `App.xaml.cs` → `AppShell` → pages.
- Platform-specific code lives under `Platforms/{Android,iOS,MacCatalyst,Windows}`.
- Styles and colors are defined in `Resources/Styles/Colors.xaml` and `Styles.xaml`, merged via `App.xaml`.
- **MVVM with CommunityToolkit.Mvvm** (`CommunityToolkit.Mvvm` NuGet package). Views live alongside or in a `Views/` folder; ViewModels go in a `ViewModels/` folder; Models go in a `Models/` folder.

## MVVM Conventions (CommunityToolkit.Mvvm)

Use the MVVM Toolkit source generators to minimize boilerplate. All ViewModels must be `partial` classes inheriting from `ObservableObject`.

### Observable Properties

Use `[ObservableProperty]` on private fields instead of writing full property boilerplate:

```csharp
public partial class PetViewModel : ObservableObject
{
    [ObservableProperty]
    private string? petName;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private double insulinDose;
}
```

Use `[NotifyPropertyChangedFor(...)]` when a computed property depends on another, and `[NotifyCanExecuteChangedFor(...)]` to re-evaluate command `CanExecute` when a property changes.

### Commands

Use `[RelayCommand]` on methods instead of manually creating `ICommand` properties. Async methods get an `AsyncRelayCommand` automatically:

```csharp
[RelayCommand]
private async Task SaveAsync()
{
    // save logic
}

[RelayCommand(CanExecute = nameof(CanDelete))]
private void Delete() { /* ... */ }

private bool CanDelete() => SelectedItem is not null;
```

### Messaging

Use `WeakReferenceMessenger` (not the deprecated `MessagingCenter`) for decoupled cross-component communication:

```csharp
// Define a message
public sealed class PetUpdatedMessage(Pet pet) : ValueChangedMessage<Pet>(pet);

// Send
WeakReferenceMessenger.Default.Send(new PetUpdatedMessage(pet));

// Receive — implement IRecipient<T> or register inline
WeakReferenceMessenger.Default.Register<PetListViewModel, PetUpdatedMessage>(
    this, static (r, m) => r.RefreshList());
```

### Dependency Injection & Page Registration

Register all ViewModels, pages, and services in `MauiProgram.cs` using `builder.Services`. Shell navigation resolves pages from the DI container automatically:

```csharp
builder.Services.AddTransient<MainPage>();
builder.Services.AddTransient<MainPageViewModel>();
```

Pages receive their ViewModel via constructor injection and set `BindingContext`:

```csharp
public MainPage(MainPageViewModel viewModel)
{
    InitializeComponent();
    BindingContext = viewModel;
}
```

### Data Binding in XAML

Bind to generated property names (PascalCase), not to private field names:

```xml
<Entry Text="{Binding PetName}" />
<Button Text="Save" Command="{Binding SaveCommand}" />
```

## General Conventions

- **Nullable reference types** are enabled.
- **Implicit usings** are enabled — no need for common `using` statements.
- UI layouts use `SemanticProperties` for accessibility — continue this practice on new controls.
- Tab indentation is used in both C# and XAML files.
