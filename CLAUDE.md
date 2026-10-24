# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Unity **6000.6.5f1** 2D project (URP, Input System) for an offline, portrait Android Knife Hit game with a 30-level campaign (3 worlds, bosses at levels 10/20/30). Match the Editor version in `ProjectSettings/ProjectVersion.txt`.

- `Assets/_Project/` is the required root for all game-owned assets: `Scenes/`, `Scripts/`, `Prefabs/`, `Data/` (`Stages/`, `Levels/`, `Worlds/`, `Bosses/`, `Skins/`, `Campaign.asset`), `Art/`, `Audio/`, `Settings/`. Runtime code goes in `Scripts/`, Editor utilities in `Assets/_Project/Editor/`.
- `Packages/manifest.json` and `Packages/packages-lock.json` must be committed together when dependencies change. Unity requires `Packages/` and `ProjectSettings/` at the repository root.
- Repository documentation stays at the root or in `docs/`.

## Workflow

- Open the project through Unity Hub, load `Assets/_Project/Scenes/MainMenu.unity` (or `GamePlay.unity` to test a level directly: it plays the current level), and enter Play mode. The build scene list is `MainMenu.unity` then `GamePlay.unity`.
- Build with **File > Build Profiles** (Android, portrait). Build an APK early and iterate on a real phone. No build scripts, CI, formatter or linter exist.
- Only one Editor instance may have the project open. Close the interactive Editor before launching another one against this project.
- Unity-specific agent skills live in `.claude/skills/` (e.g. `unity-cli`, `create-2d-physics`, `ui-ugui`); consult the relevant one before Editor or package work.
- Move or rename assets only through Unity and commit their `.meta` files so GUID references survive. Never hand-edit GUIDs. Keep `Library/`, `Temp/`, `Logs/`, IDE projects and build output out of commits.

## Testing

**No automated tests.** Do not create tests, test assemblies or a test folder, and do not run test suites, even though Unity Test Framework is installed. The owner tests manually. After a behavior change, supply a playable build and concise manual reproduction steps. Cover rapid taps, last-knife results, stage/phase transitions, retry, app background/resume, boss Guard and Danger Zone windows, and the persisted save (sound, coins, stars, skins). `docs/manual-test-checklist.md` lists the steps.

## Coding style

- Four-space indentation, braces on their own lines, `PascalCase` for types, methods and properties, `camelCase` for parameters and serialized fields.
- Non-serialized private fields use `_camelCase`. Existing scripts declare no namespace (`// ReSharper disable once CheckNamespace`); match both in new gameplay code.
- Script filenames match their main class. Prefer private `[SerializeField]` fields over public mutable state.

## Commits and pull requests

- Commits use short, imperative, conventional-style subjects with one focused change each, e.g. `feat(assets): ...`, `chore(skills): ...`.
- PRs explain the behavior change, link relevant issues, record the validation performed, and include screenshots or video for visual changes. Call out package, scene and project-setting changes explicitly.

## Domain and design docs

- `CONTEXT.md`: the domain glossary (Campaign, World, Level, Run, Stage, Boss, Phase, Target, Flying Knife, Embedded Knife, Successful Throw, Star, Coin, Knife Skin, Paused Run). The game has no score or high score; Stars rate a win by run time. Use these terms in code and docs and avoid the listed alternatives.
- `docs/knife-hit-plan.md`: target architecture and lifecycle rules (in English).
- `docs/knife-collision.md` (Vietnamese): the collision model actually implemented. **It supersedes the plan's "Landing Model" and `ThrowRules`.**
- `docs/assets-guide.md` (Vietnamese): which sprite and sound is used for which event, and the art still missing.
- `docs/level-table.md`: the generated 30-level parameter table. `docs/manual-test-checklist.md`: Editor and Android test steps.

## Architecture

### Implemented (`Assets/_Project/Scripts/`)

- **`KnifeLauncher`** drives the whole simulation. It owns input (Touchscreen primary touch or mouse left button via the Input System), allows only one flying knife and ignores taps during flight (it never queues them), and is the **only caller of `RotatingTarget.AdvanceRotation`**. While a knife flies, it substeps time (bounded by `RotatingTarget.MaximumStepTime` and throw speed), rotates the target, moves the knife and checks the result geometrically. It reports results through direct calls to `GameSession` (`TryAcceptThrow`, `HandleSuccessfulThrow`, `HandleKnifeHitKnife`). A hit stops the launcher, which freezes the target, until `ResetForStage`.
- **`GameSession`**: full enum `State` (`Playing`, `Transition`, `Paused`, `GameOver`, `Victory`), `CurrentLevel`, `StageNumber`/`StageCount` (stages or boss phases of the level), `KnivesRemaining`, `RunSeconds` and `LastResult`. A Run is one attempt at one `LevelConfig`: it plays the level's stages in order with a timed `Transition` between them. It starts a Run in `Start` with the level chosen in the MainMenu scene (`SceneFlow.TakePendingLevel`, else the current level); `StartRun` retries, `StartNextLevel`, `ExitToMenu(page)` loads the MainMenu scene. It reserves one knife per accepted throw, spawns the next ready knife, enters `Transition` or `Victory` when none remain, and rebuilds the stage on every stage start (clears embedded knives and places `initialKnifeAngles`). `TryAcceptThrow` also rejects taps during a short input lock after a stage starts (so a button tap never throws) and while a boss Guard is active. On `Victory` it calls `PlayerProgress.RecordWin` exactly once. Run time counts only `Playing` seconds, and `LevelConfig.CalculateStars` turns it into 1–3 Stars. `Pause`/`Resume` remember the previous state; `OnApplicationPause`/`OnApplicationFocus` (device only) call `Pause`, which is idempotent, and the player must press Continue. Its `Changed` event is only for HUD and feedback, so subscriber order never decides gameplay. The context menu has "Pause", "Resume" and "Replay", and every state change is logged.
- **Data (`Scripts/Data/`, assets in `Data/`)**, all read-only ScriptableObjects that runtime code never mutates: `StageConfig` (`requiredKnives`, `initialKnifeAngles`, looping `RotationSegment`s; negative speed reverses), `LevelConfig` (stable `levelId`, one `StageConfig` or one `BossConfig`, `WorldConfig`, coin reward, star bonus, 2★/3★ seconds, optional `unlockSkin`, optional hint), `WorldConfig` (background + tints), `BossConfig` (`BossPhase[]`: a `StageConfig` plus Guard / Wide Zone windows), `KnifeSkin` (sprite + tint + price) and `CampaignConfig` (ordered 30 levels, 3 worlds, skins; the first skin is the default). Never branch gameplay on a level number. `Editor/CampaignBuilder.cs` (`Knife Hit > Build Campaign Data`) generated these assets and `docs/level-table.md`; running it again overwrites hand edits.
- **`PlayerProgress`** (`Scripts/Save/`, one scene component, no singleton) is the only save layer: JSON with a `version` field in `PlayerPrefs` key `KnifeHit.Save` (sound, coins, per-level best stars, owned/equipped skins, seen hints). It migrates the old `SoundEnabled` key once, falls back to safe defaults for empty/corrupt/other-version data (copying corrupt JSON to `KnifeHit.Save.Corrupt`), drops unknown level/skin ids while keeping the rest, and applies sound through `AudioListener.volume`. Rewards are computed in `RecordWin`: first win pays `coinReward`, any win pays `starBonusCoins` per star above the previous best, `unlockSkin` is granted on the first win. Level N+1 unlocks when level N has ≥1 star. UI components may `OnEnable` before its `Awake`, so it loads lazily.
- **`BossEncounter`** is presentation only (tints the target visual and exposes the warning text). Boss rules live in `BossPhase` and are evaluated by `RotatingTarget` from its `StageTime` (see `docs/knife-collision.md`).
- **`GameHUD`** (`Scripts/UI/`, on the `HUD` Screen Space Overlay canvas, 1080×1920 reference): level/phase name, knives remaining with the equipped skin icon, boss warning banner, first-time hint, pause panel (Continue), result panel (stars, time, coins, new knife; Next / Retry / Levels) and sound-toggle buttons. `MenuScreens` (Home / Level Map / Knives, shown only in `State.Menu`), `LevelMapScreen`, `KnifeInventoryScreen`, `LevelButtonView` and `SkinCardView` live on the `HUD/Menu` object and `HUD/SafeArea/MenuRoot`; they only read state and call actions. UI only reads session/progress properties on `Changed`. `SafeAreaFitter` keeps the content inside `Screen.safeArea`. Text uses TextMeshPro; its Essentials are in `Assets/TextMesh Pro/` (third-party, outside `_Project/`). The default LiberationSans SDF font has no Vietnamese glyphs, so UI strings stay in English.
- **`GameFeedback`** (`Scripts/Feedback/`): observes `Changed` for sound cues (throw, wood hit, clash, stage clear, fail, victory, button click/toggle), a target pulse and a camera shake on failure. The pulse is applied to a visual child of the target, never to the logical target, so hit geometry is unaffected.
- **Collision model (no Physics2D):** knives fly straight up the target's vertical axis. `RotatingTarget` stores each embedded knife's local angle and center-to-butt distance. A flying knife fails once its tip passes the butt of an embedded knife lying within `blockAngle` of the throw line; otherwise it embeds at a random depth, sampled once per throw from `minEmbedDepth`..`maxEmbedDepth`. The part inside the wood can therefore never cause a failure. Boss windows only scale `blockAngle` (Wide Zone) or gate `TryAcceptThrow` (Guard).
- The knife prefab is only a `SpriteRenderer`, sorted just behind the target so the wood hides the embedded part; `KnifeLauncher` applies the equipped `KnifeSkin` (sprite and tint only) to every knife it spawns. Knife geometry (`knifeTipOffset`, `knifeLength`) is serialized on the launcher, and the wood `radius` on the target.
- **`RotatingTarget`** takes its speed from the current `StageConfig` (via `GetSpeed(stageTime)`), exposes `StageTime`, and for boss phases `IsThrowGated` and `BlockAngleScale`. Do not add a separate `Update` rotation, because that would double-rotate the target and break the substepping.
- Setup assumptions checked in `KnifeLauncher.Awake`: the launcher sits directly below the target center and below its surface; the target and knife use scale 1.

### Design constraints

- Deliberately avoided: a global event bus, DI, pervasive singletons, interfaces with only one implementation, and pooling (unless profiling shows it is needed).
