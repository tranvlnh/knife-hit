# Repository Guidelines

## Project Structure & Module Organization

This is a Unity **6000.6.5f1** 2D project using URP and the Input System. Match the Editor version recorded in `ProjectSettings/ProjectVersion.txt`.

- `Assets/_Project/`: required root for all game-owned assets, including scenes, scripts, prefabs, stage data, artwork, audio, and render/input settings.
- `Assets/_Project/Scenes/`, `Scripts/`, `Prefabs/`, `Data/Stages/`, `Art/`, `Audio/`, and `Settings/`: organize content by purpose beneath that root.
- `Packages/manifest.json` and `Packages/packages-lock.json`: package dependencies and resolved versions; commit dependency changes together.
- `ProjectSettings/`: shared Editor and player configuration.

No gameplay scripts or test assemblies exist yet. Add runtime code under `Assets/_Project/Scripts/` and Editor utilities under `Assets/_Project/Editor/`. Existing template assets in `Assets/Scenes/` and `Assets/Settings/` should be moved through Unity when setting up the gameplay scene, preserving their `.meta` GUIDs and references. Repository documentation stays at the root or in `docs/`; Unity requires `Packages/` and `ProjectSettings/` at the project root.

## Build, Test, and Development Commands

Open this directory through Unity Hub, load `SampleScene`, and enter Play mode to run locally. Use **File > Build Profiles** to configure the target platform, scene list, and player build. No custom build scripts or CI workflow are currently checked in.

Build an offline portrait Android APK early and iterate on a real phone. Close the interactive Editor before launching a second Editor against this project.

## Coding Style & Naming Conventions

For new C# code, use four-space indentation, braces on separate lines, `PascalCase` for types and methods, and `camelCase` for parameters and private fields. Match script filenames to their main class names. Prefer private serialized fields over public mutable state. No repository formatter or lint configuration is currently present.

Move assets through Unity and commit their `.meta` files to preserve GUID references. Keep generated `Library/`, `Temp/`, `Logs/`, IDE projects, and build output out of commits.

## Testing Guidelines

The owner performs manual testing for this demo. Do not create automated tests, test assemblies, or a test folder, and do not run automated test suites. Supply a playable build and concise manual reproduction steps for changed behavior. Focus on rapid taps, last-knife results, stage transitions, replay, app background/resume, and persisted high score and sound settings. Unity Test Framework is installed but is not part of the current workflow.

## Commit & Pull Request Guidelines

History contains only `init prj`; no formal commit convention is established. Use short, imperative subjects describing one focused change. Pull requests should explain the behavior change, link relevant issues, record validation performed, and include screenshots or video for visual changes. Call out package, scene, and project-setting changes explicitly.
