# MO56 UI acceptance evidence

## Automated evidence

- Avalonia XAML compiles through `dotnet build src/PixelGameStudio.App/PixelGameStudio.App.csproj --no-restore`.
- MainWindow keeps a 1024x640 minimum and a 1280x800 design size.
- The five workspace ids and four Character section ids are asserted by the binding structure and are backed by the corresponding physical Character views.
- Frame build fields use integer-step sliders and their view-model setters normalize values to the safe native range.
- Character, Equipment, Animation and Actions physical views are loaded through typed DataTemplates.

## Target layouts

| Target | Layout contract | Result |
|---|---|---|
| 1280x800 | 2*:3* preview/config split, contextual browser visible for Equipment/Actions | XAML/build only; native resize and interaction not yet verified |
| 1024x640 | minimum window, scrollable inspector, no Role workspace | XAML/build only; native overflow and button accessibility not yet verified |

The earlier screenshot attempt reported `Computer Use native pipe is unavailable`; no successful native UI acceptance is recorded. XAML/build evidence is not a substitute for resizing and interacting with the running app at both target sizes.
