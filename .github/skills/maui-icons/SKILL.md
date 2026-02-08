---
name: maui-icons
description: Using MauiIcons library in .NET MAUI for Fluent, Material, Cupertino, and FontAwesome icons. Includes installation, XAML usage, workarounds for XamlC errors, and best practices.
---

# MauiIcons - Icon Library for .NET MAUI

MauiIcons is a comprehensive icon library for .NET MAUI that provides access to multiple icon collections including Fluent, Material, Cupertino, and FontAwesome.

## Installation

### 1. Add NuGet Package

Choose the icon collection(s) you need:

```bash
# Fluent Icons (Microsoft's open source Fluent icons)
dotnet add package AathifMahir.Maui.MauiIcons.Fluent

# Material Icons
dotnet add package AathifMahir.Maui.MauiIcons.Material

# Cupertino Icons (iOS-style icons)
dotnet add package AathifMahir.Maui.MauiIcons.Cupertino

# FontAwesome Icons
dotnet add package AathifMahir.Maui.MauiIcons.FontAwesome
```

### 2. Register in MauiProgram.cs

Call the appropriate extension method in `MauiProgram.cs`:

```csharp
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseFluentMauiIcons()      // For Fluent icons
            .UseMaterialMauiIcons()    // For Material icons
            .UseCupertinoMauiIcons();  // For Cupertino icons
            
        return builder.Build();
    }
}
```

## XAML Usage

### Namespace Declaration

Add the MauiIcons namespace to your XAML files:

```xml
xmlns:mi="http://www.aathifmahir.com/dotnet/2022/maui/icons"
```

### Using MauiIcon Control

The `MauiIcon` control is the simplest way to display icons:

```xml
<!-- Basic usage -->
<mi:MauiIcon Icon="{mi:Fluent Accounts}" />
<mi:MauiIcon Icon="{mi:Material ABC}" />
<mi:MauiIcon Icon="{mi:Cupertino Airplane}" />

<!-- With customization -->
<mi:MauiIcon Icon="{mi:Fluent PeopleAdd24}" 
             IconSize="64"
             IconColor="{DynamicResource CurrentPrimary}"
             HorizontalOptions="Center" />
```

### Using with Other Controls (Attached Property)

Apply icons to existing MAUI controls using the attached property:

```xml
<!-- Image -->
<Image mi:MauiIcon.Value="{mi:Fluent Icon=Accessibility48}" />

<!-- Button -->
<Button mi:MauiIcon.Value="{mi:Fluent Icon=Home32, IconSize=Large, IconColor=Pink}" />

<!-- ImageButton -->
<ImageButton mi:MauiIcon.Value="{mi:Material Icon=AccessAlarm}" />

<!-- Label (requires FontOverride=True) -->
<Label mi:MauiIcon.Value="{mi:Fluent Icon=Accounts, FontOverride=True}" />

<!-- Entry (requires FontOverride=True) -->
<Entry mi:MauiIcon.Value="{mi:FontAwesome Icon=AddressBook, FontOverride=True}" 
       Placeholder="Enter text" />
```

### FontOverride Requirement

Controls that use text (Entry, Label, SearchBar, etc.) require `FontOverride=True` to apply icons:

```xml
<Entry mi:MauiIcon.Value="{mi:Fluent Icon=Search24, FontOverride=True}" 
       Placeholder="Search..." />
```

**Warning**: FontOverride replaces the control's font, which may cause unexpected rendering behaviors.

## CRITICAL: XamlC Workaround

### Problem

When building in **Release mode**, you may encounter XAML compilation errors:

```
XamlC error XC0000: Cannot resolve type "http://www.aathifmahir.com/dotnet/2022/maui/icons:MauiIcon"
```

This is a known issue tracked at [dotnet/maui#7503](https://github.com/dotnet/maui/issues/7503).

### Solution

Add a discarded instance of `MauiIcon` in each page's code-behind that uses MauiIcons:

```csharp
using MauiIcons.Core;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        
        // REQUIRED: Workaround for XamlC errors in release builds
        _ = new MauiIcon();
    }
}
```

**This must be added to EVERY page that uses MauiIcon or the namespace.**

## Data Binding

Bind to icon properties in your ViewModel:

```xml
<ContentPage xmlns:mi="http://www.aathifmahir.com/dotnet/2022/maui/icons"
             x:Name="thisRoot">
    <VerticalStackLayout>
        <mi:MauiIcon Icon="{mi:Fluent Icon={Binding MyIcon}}" 
                     IconColor="{Binding MyColor}" />
        
        <Image mi:MauiIcon.Value="{mi:Fluent Icon={Binding MyIcon}, 
                                           IconColor={Binding MyColor}}" />
    </VerticalStackLayout>
</ContentPage>
```

## C# Markup Usage

Use icons in C# with fluent syntax:

```csharp
using MauiIcons.Fluent;
using MauiIcons.Material;

// MauiIcon control
new MauiIcon()
    .Icon(FluentIcons.Accounts)
    .IconColor(Colors.Blue)
    .IconSize(40);

// Extension methods
new Image().Icon(FluentIcons.Home);

new Button().Icon(FluentIcons.Save);

new Label()
    .Icon(MaterialIcons.Home, fontOverride: true)
    .IconSize(40.0)
    .IconColor(Colors.Red);
```

## Advanced Settings

Configure global defaults in `MauiProgram.cs`:

```csharp
builder.UseMauiIconsCore(x => 
{
    x.SetDefaultIconSize(30.0);
    x.SetDefaultFontOverride(true);
    x.SetDefaultFontAutoScaling(true);
});
```

## Icon Properties

| Property | Type | Description |
|----------|------|-------------|
| `Icon` | Enum | The icon enum value |
| `IconSize` | double | Size of the icon |
| `IconColor` | Color | Color of the icon |
| `IconBackgroundColor` | Color | Background color of the icon |
| `IconAutoScaling` | bool | Enable automatic scaling |
| `IconSuffix` | string | Suffix text for the icon |
| `IconSuffixFontSize` | double | Font size for suffix |
| `IconSuffixTextColor` | Color | Text color for suffix |

## Animation Support

Add entrance and click animations:

```xml
<!-- Entrance Animation -->
<mi:MauiIcon Icon="{mi:Fluent Accounts}" 
             EntranceAnimationType="Fade"
             EntranceAnimationDuration="500" />

<!-- OnClick Animation -->
<mi:MauiIcon Icon="{mi:Fluent Save}" 
             OnClickAnimationType="Scale"
             OnClickAnimationDuration="200" />
```

Available animation types: `Fade`, `Rotate`, `Scale`

## Platform and Idiom Control

Show icons conditionally:

```xml
<!-- Only on specific platforms -->
<mi:MauiIcon Icon="{mi:Cupertino Airplane}" 
             OnPlatforms="iOS, MacCatalyst" />

<!-- Only on specific idioms -->
<mi:MauiIcon Icon="{mi:Material ABC}" 
             OnIdioms="Phone, Tablet" />
```

## Version & License

- **Package**: AathifMahir.Maui.MauiIcons.Fluent (and variants)
- **Latest Version**: 5.0.0
- **License**: MIT
- **Repository**: https://github.com/AathifMahir/MauiIcons

## Common Issues

### XamlC Errors in Release Builds
**Solution**: Add `_ = new MauiIcon();` in page constructors (see CRITICAL section above)

### Icons Not Showing
**Checklist**:
1. Package installed via NuGet
2. `.Use{IconFamily}MauiIcons()` called in MauiProgram.cs
3. Correct namespace in XAML: `xmlns:mi="http://www.aathifmahir.com/dotnet/2022/maui/icons"`
4. For text controls: `FontOverride=True` is set

### MauiEnableXamlCBindingWithSourceCompilation Warning
When enabled, you may see warnings about bindings. The XamlC workaround still applies.

## Examples

### Complete Page Setup

**XAML (MainPage.xaml)**:
```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:mi="http://www.aathifmahir.com/dotnet/2022/maui/icons"
             x:Class="MyApp.MainPage">
    
    <VerticalStackLayout Spacing="20" Padding="20">
        <!-- Icon control -->
        <mi:MauiIcon Icon="{mi:Fluent Home24}" 
                     IconSize="48"
                     IconColor="Blue" />
        
        <!-- Button with icon -->
        <Button Text="Save" 
                mi:MauiIcon.Value="{mi:Fluent Save24}" />
    </VerticalStackLayout>
</ContentPage>
```

**Code-behind (MainPage.xaml.cs)**:
```csharp
using MauiIcons.Core;

namespace MyApp;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        
        // REQUIRED: XamlC workaround
        _ = new MauiIcon();
    }
}
```