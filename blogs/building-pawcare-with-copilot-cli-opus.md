# Building Pawse: How I Built a Full .NET MAUI App with GitHub Copilot CLI and Claude Opus 4.6

Hey friends! I'm super excited to share the story behind Pawse — a .NET MAUI app for managing diabetic pet care that I built almost entirely through conversational AI using the **GitHub Copilot CLI** powered by **Claude Opus 4.6**. This was one of the most fun and eye-opening development experiences I've had in a long time, and I want to walk you through the whole thing.

## The Problem

If you've ever had a diabetic pet (or know someone who has), you know the drill. Insulin shots twice a day at specific times. Careful food tracking. Weight monitoring. Vet appointments. Sharing care duties with family members or pet sitters. It's a lot to keep track of, and getting it wrong can be dangerous for your furry friend.

I wanted an app that would handle all of this in one place — insulin logging, feeding schedules, weight tracking, vet emergency info, and the ability to share your pet's care history with another person using a simple code. And I wanted it to look absolutely stunning while doing it.

## The Experiment: GitHub Copilot CLI + Opus 4.6

Here's where things get interesting. I decided to build this entire app using the **GitHub Copilot CLI** — the terminal-based agent that can read your codebase, edit files, run builds, and iterate with you conversationally. Under the hood, I was running **Claude Opus 4.6**, and let me tell you, the results blew me away.

I started from literally `File > New` — an empty .NET MAUI project template — and through a series of conversational prompts, built out the entire application. No copy-pasting from Stack Overflow. No manually scaffolding ViewModels. Just me describing what I wanted and the agent making it happen.

## The Architecture: MVVM Done Right

One of my first instructions was to use the **.NET Community Toolkit for MVVM** — `CommunityToolkit.Mvvm` — for data binding, messaging, commands, the whole nine yards. The agent set everything up with proper `ObservableObject` base classes, `[ObservableProperty]` attributes, `[RelayCommand]` attributes, and `WeakReferenceMessenger` for cross-ViewModel communication.

The resulting architecture is clean and follows patterns that any .NET MAUI developer would recognize:

- **Models**: `Pet`, `InsulinLog`, `FeedingLog`, `WeightLog`, `VetInfo`, `Schedule`
- **ViewModels**: One per page, all using source generators from the MVVM Toolkit
- **Services**: `DatabaseService` (SQLite), `SyncService` (Azure Functions + Table Storage), `NotificationService`
- **Views**: 11 XAML pages with proper data binding throughout
- **Themes**: A full theme engine with 5 color palettes and runtime switching

The agent even researched diabetic pet care to understand insulin types (Vetsulin, ProZinc, NPH, Glargine), concentration units (U-40 vs U-100), typical feeding schedules, and weight monitoring best practices. It baked all of that domain knowledge right into the UI — picker options, pro tips, schedule templates.

## The Visual Refresh: Making It Beautiful

After the core functionality was working, I asked for a complete visual overhaul. And I mean complete. I wanted:

- **5 custom color themes** (Berry Bliss, Warm & Earthy, Ocean Breeze, Forest Walk, Midnight Indigo)
- **SkiaSharp controls** for custom gradient cards and dose indicators  
- **Fluent font icons** throughout (goodbye emoji!)
- **Light and dark mode support**
- **Smooth animations**
- **Pet photo support** with circular avatars

The agent delivered all of this iteratively. We went through several rounds of fixing icon contrast, getting the theme picker to actually work (MAUI's `CollectionView` with `RelativeSource` bindings is... fun), and converting from `StaticResource` to `DynamicResource` so theme changes would propagate at runtime.

That last one was a particularly interesting debugging session. I kept saying "the theme isn't changing" and the agent kept trying different approaches — `x:Reference` bindings, code-behind event handlers, replacing `CollectionView` with `FlexLayout`, then finally a standard `Picker`. Eventually we traced the real root cause: `{AppThemeBinding Light={StaticResource X}, Dark={StaticResource Y}}` resolves `StaticResource` once at load time and caches it. We had to convert the entire app to use `{DynamicResource}` with resolved theme keys that `ThemeService` updates based on the current light/dark system appearance. The agent did that migration across all 11 pages and the Styles.xaml in one shot — 15 files, 163 insertions, 123 deletions. Clean.

## The Onboarding: Theme Carousel

One of my favorite features is the welcome screen. Instead of just asking for your name and moving on, we added a **CarouselView** with mini phone previews of all 5 themes. As you swipe through them, the theme applies in real-time — the entire page background, icons, and buttons change color live. It's a delightful first experience and lets users personalize the app from the very first interaction.

## The Backend: Azure Functions + Table Storage

For cloud sync and the pet sharing feature, we set up an **Azure Functions** API with **Azure Table Storage**. The sharing system generates a 6-character code that another user can enter to pull down a pet's complete care history. Simple, no account required, and the data structure maps perfectly to Table Storage's partition/row key model.

## The Marketing Site

I also had the agent build a complete **marketing landing page** — pure HTML and CSS, hosted on **GitHub Pages** with an automated deployment workflow. It has a hero section, feature grid, screenshot gallery placeholders, and my favorite touch: an interactive theme switcher that lets visitors click through the same 5 themes from the app, with the selection saved to `localStorage`. Berry Bliss is the default, obviously. 💜

## What I Learned About Building with Copilot CLI

After spending hours going back and forth with the CLI agent, here are my big takeaways:

### 1. Describe the "What", Not the "How"

The best results came when I described what I wanted at a high level — "add pet photo support with circular avatars and a fallback to the first letter of the pet's name" — rather than dictating specific implementation details. The agent would figure out the converters, XAML structure, and ViewModel properties on its own.

### 2. Iterate, Don't Start Over

When something didn't work (and things definitely didn't always work on the first try), I'd describe the problem and let the agent debug it. The theme switching saga was 3-4 rounds of iteration, but each round got closer. The agent would read its own previous code, understand what it tried, and pivot to a different approach.

### 3. It Handles the Boring Stuff Incredibly Well

Renaming properties across 15 files? Converting emoji to font icons in 11 XAML pages? Updating color hex values in SVGs, CSS, csproj files, and markdown docs? This is where the agent absolutely shines. It's tireless and thorough in a way that I definitely wouldn't be at 11pm.

### 4. Domain Research Is a Superpower

When I said "research diabetic pet insulin for cats and dogs," the agent came back with medically accurate information about insulin types, dosing schedules, U-40 vs U-100 concentrations, and feeding recommendations. That domain knowledge made the app genuinely useful instead of just a generic tracker.

### 5. The Terminal Is Underrated

Working in the CLI meant no context switching. The agent could read files, edit them, run `dotnet build`, check for errors, fix them, and commit — all in the same flow. There's something really satisfying about watching it chain commands together and iterate until the build succeeds with 0 warnings and 0 errors.

## The Numbers

Just to give you a sense of the scope:

- **11 XAML pages** with full data binding
- **8 ViewModels** with MVVM Toolkit source generators
- **6 data models** with SQLite persistence
- **5 custom themes** with runtime switching
- **1 Azure Functions API** with Table Storage
- **1 marketing website** with GitHub Pages deployment
- **15+ commits** all done through the CLI agent

All from an empty template. In one session.

## What's Next

I'm planning to flesh out the notification system, add more SkiaSharp visualizations for weight trends, and polish the sync experience. And of course, I'll keep using the Copilot CLI to do it.

If you haven't tried the GitHub Copilot CLI yet, I really encourage you to give it a shot. Especially with Opus 4.6 powering it — the code quality, the debugging capability, and the sheer breadth of knowledge (from MAUI XAML quirks to Azure Table Storage partition strategies to SVG icon design) is remarkable.

The code is all up on GitHub if you want to check it out. And if you have a diabetic pet, maybe Pawse will actually be useful for you too. 🐾

Cheers,
James
