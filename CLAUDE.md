# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Unity **6000.6.5f1** 2D project (URP, Input System) for an offline, portrait Android Knife Hit demo. Match the Editor version in `ProjectSettings/ProjectVersion.txt`.

- `Assets/_Project/` is the required root for all game-owned assets: `Scenes/`, `Scripts/`, `Prefabs/`, `Data/Stages/`, `Art/`, `Audio/`, `Settings/`. Runtime code goes in `Scripts/`, Editor utilities in `Assets/_Project/Editor/`.
- `Packages/manifest.json` and `Packages/packages-lock.json` must be committed together when dependencies change. Unity requires `Packages/` and `ProjectSettings/` at the repository root.
- Repository documentation stays at the root or in `docs/`.

## Workflow

- Open the project through Unity Hub, load `Assets/_Project/Scenes/GamePlay.unity`, and enter Play mode. The build scene list contains only `GamePlay.unity`.
- Build with **File > Build Profiles** (Android, portrait). Build an APK early and iterate on a real phone. No build scripts, CI, formatter or linter exist.
- Only one Editor instance may have the project open. Close the interactive Editor before launching another one against this project.
- Unity-specific agent skills live in `.claude/skills/` (e.g. `unity-cli`, `create-2d-physics`, `ui-ugui`); consult the relevant one before Editor or package work.
- Move or rename assets only through Unity and commit their `.meta` files so GUID references survive. Never hand-edit GUIDs. Keep `Library/`, `Temp/`, `Logs/`, IDE projects and build output out of commits.

## Testing

**No automated tests.** Do not create tests, test assemblies or a test folder, and do not run test suites, even though Unity Test Framework is installed. The owner tests manually. After a behavior change, supply a playable build and concise manual reproduction steps. Cover rapid taps, last-knife results, stage transitions, replay, app background/resume, and the persisted sound setting.

## Coding style

- Four-space indentation, braces on their own lines, `PascalCase` for types, methods and properties, `camelCase` for parameters and serialized fields.
- Non-serialized private fields use `_camelCase`. Existing scripts declare no namespace (`// ReSharper disable once CheckNamespace`); match both in new gameplay code.
- Script filenames match their main class. Prefer private `[SerializeField]` fields over public mutable state.

## Commits and pull requests

- Commits use short, imperative, conventional-style subjects with one focused change each, e.g. `feat(assets): ...`, `chore(skills): ...`.
- PRs explain the behavior change, link relevant issues, record the validation performed, and include screenshots or video for visual changes. Call out package, scene and project-setting changes explicitly.

## Domain and design docs

- `CONTEXT.md`: the domain glossary (Run, Stage, Target, Flying Knife, Embedded Knife, Successful Throw, Paused Run). The game has no score or high score. Use these terms in code and docs and avoid the listed alternatives.
- `docs/knife-hit-plan.md`: target architecture and lifecycle rules (in English).
- `docs/knife-collision.md` (Vietnamese): the collision model actually implemented. **It supersedes the plan's "Landing Model" and `ThrowRules`.**
- `docs/assets-guide.md` (Vietnamese): which sprite and sound is used for which event.

## Architecture

### Implemented now (`Assets/_Project/Scripts/Gameplay/`)

- **`KnifeLauncher`** drives the whole simulation. It owns input (Touchscreen primary touch or mouse left button via the Input System), allows only one flying knife and ignores taps during flight (it never queues them), and is the **only caller of `RotatingTarget.AdvanceRotation`**. While a knife flies, it substeps time (bounded by `RotatingTarget.MaximumStepTime` and throw speed), rotates the target, moves the knife and checks the result geometrically. It reports results through direct calls to `GameSession` (`TryAcceptThrow`, `HandleSuccessfulThrow`, `HandleKnifeHitKnife`). A hit stops the launcher, which freezes the target, until `ResetForStage`.
- **`GameSession`** (single stage for now): enum `State` (`Playing`, `GameOver`, `Victory`) and `KnivesRemaining`. It reserves one knife per accepted throw, spawns the next ready knife or enters `Victory`, and rebuilds the stage on `StartRun` (clears embedded knives and places `initialKnifeAngles`). Its `Changed` event is only for HUD and feedback. The component's context menu also has "Replay", and every state change is logged.
- **`GameHUD`** (`Scripts/UI/`, on the `HUD` Screen Space Overlay canvas, 1080×1920 reference): knives remaining and a result panel (GameOver/Victory) whose Replay button calls `GameSession.StartRun`. It only reads session properties on `Changed`. `SafeAreaFitter` keeps the content inside `Screen.safeArea`. Text uses TextMeshPro; its Essentials are in `Assets/TextMesh Pro/` (third-party, outside `_Project/`). The default LiberationSans SDF font has no Vietnamese glyphs, so UI strings stay in English.
- **Collision model (no Physics2D):** knives fly straight up the target's vertical axis. `RotatingTarget` stores each embedded knife's local angle and center-to-butt distance. A flying knife fails once its tip passes the butt of an embedded knife lying within `blockAngle` of the throw line; otherwise it embeds at a random depth, sampled once per throw from `minEmbedDepth`..`maxEmbedDepth`. The part inside the wood can therefore never cause a failure.
- The knife prefab is only a `SpriteRenderer`, sorted just behind the target so the wood hides the embedded part. Knife geometry (`knifeTipOffset`, `knifeLength`) is serialized on the launcher, and the wood `radius` on the target.
- **`RotatingTarget`** currently has a constant `speed`. Do not add a separate `Update` rotation, because that would double-rotate the target and break the substepping.
- Setup assumptions checked in `KnifeLauncher.Awake`: the launcher sits directly below the target center and below its surface; the target and knife use scale 1.

### Planned (not yet built; see the plan doc)

- `GameSession` grows to the full enum FSM (`Menu`, `Playing`, `Transition`, `Paused`, `GameOver`, `Victory`), plus stage index and app pause/resume. It handles results through direct calls. Observer events go only to the HUD and feedback, so subscriber order never decides gameplay.
- `StageConfig` ScriptableObjects (5, in `Data/Stages/`) hold the required knives, initial embedded knives and rotation segments. Stages 3–5 add speed changes and reversals. Runtime code never mutates them.
- Start, paused and sound-toggle UI on `GameHUD`; `GameFeedback`. The sound setting is stored as a direct `PlayerPrefs` call, with no save service.
- Deliberately avoided: a global event bus, DI, pervasive singletons, interfaces with only one implementation, and pooling (unless profiling shows it is needed).
