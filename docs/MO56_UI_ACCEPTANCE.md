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
| 1280x800 | 2*:3* preview/config split, contextual browser visible for Equipment/Actions | Passes XAML/build contract |
| 1024x640 | minimum window, scrollable inspector, no Role workspace | Passes XAML/build contract |

Native Computer Use screenshots could not be captured in this environment because the helper reports `Computer Use native pipe is unavailable`. The code/build/test evidence is therefore the reproducible acceptance artifact.
