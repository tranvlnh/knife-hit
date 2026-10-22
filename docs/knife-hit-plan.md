# Knife Hit: Architecture and Delivery Plan

Status: Proposed architecture for review. Product scope below reflects the user's answers; gameplay implementation has not started.

## Confirmed Product Scope

- Offline Android APK, portrait orientation, flat 2D art, emphasis on responsive game feel.
- One run consists of stages 1 through 5. A knife hitting an embedded knife ends the run; replay starts at stage 1.
- There is no score or high score. Progress is shown only by knives remaining and the current stage.
- Only one knife may be flying. Further taps are discarded, never queued.
- Difficulty increases through required knife counts and initial embedded knives. Stages 1–2 rotate steadily; stages 3–5 use authored speed changes and smooth reversals in repeating cycles.
- Start screen, gameplay, short stage transitions, victory/failure result, replay, and sound toggle. No stage selection.
- Leaving the app pauses gameplay. Returning shows Continue. Terminating the app discards the run and retains the sound preference.
- One developer, 6–8 hours daily, 3–4 days. Bosses, shop, skins, ads, online services, and restoring terminated runs are outside this demo's scope.
- All game-owned Unity content belongs under `Assets/_Project/`, including scene, scripts, prefabs, stage data, artwork, audio, and settings.
- The owner performs manual testing. Automated tests and test assemblies are outside the current scope.

## Architecture

Use a single gameplay scene. Wire concrete component references in the Inspector. Store five stage assets in a serialized, ordered list on the session.

| Component | Owns |
| --- | --- |
| `GameSession` | Run state, stage index, knives remaining, authoritative result handling, stage transitions and app pause/resume |
| `RotatingTarget` | Logical rotation, embedded knife angles, authored rotation cycle, embedded visual objects |
| `KnifeLauncher` | Gameplay taps, one active flying knife, flight progress and landing notification |
| `Knife` | Knife visual and attachment presentation; no stage decisions |
| `GameHUD` | Start, gameplay, paused and result panels; remaining knives, sound toggle |
| `GameFeedback` | Sound and short visual feedback triggered by accepted gameplay outcomes |
| `StageConfig` | Read-only ScriptableObject data: required knives, initial angles and rotation segments |
| `ThrowRules` | Small helper for angular separation and landing validity |

Keep persistence as a direct PlayerPrefs call for one value: sound enabled. A save service, interface or repository is unnecessary for this scope. Persist sound changes immediately.

Required asset layout:

```text
Assets/_Project/
  Scenes/
  Scripts/Gameplay/
  Scripts/UI/
  Scripts/Feedback/
  Data/Stages/
  Prefabs/
  Art/
  Audio/
  Settings/
```

Move the existing scene and game-owned render/input settings into this layout through Unity when setting up the game, preserving `.meta` GUIDs and verifying references. Unity's `Packages/` and `ProjectSettings/` remain at the repository root, as do repository guidance and planning documents. Do not create a test folder or test assemblies for this demo.

## Patterns and Boundaries

- **Finite state machine:** one enum on `GameSession`: `Menu`, `Playing`, `Transition`, `Paused`, `GameOver`, `Victory`. A state-class hierarchy is unnecessary. Flight occupancy is launcher state, not another session state.
- **Component composition:** separate presentation, throwing, target rotation and session rules, using Inspector references.
- **Local Observer:** session events notify HUD and feedback. Critical result handling uses direct calls; subscriber order must not determine gameplay. Subscribe and unsubscribe with component lifetime.
- **Data-driven stages:** one `StageConfig` per stage, with reusable rotation-segment data. Runtime elapsed time and remaining knives never mutate these assets.
- Start with prefab instantiate/destroy at this small scale. Introduce pooling only if device profiling exposes a problem. Avoid a global event bus, DI container, pervasive singletons and interfaces with a single implementation.

## Landing Model

> Superseded by `docs/knife-collision.md`: the implemented rule is angular, but a knife fails when its tip passes the butt of a blocking embedded knife, embed depth is random, and the check lives in `RotatingTarget` instead of a separate `ThrowRules`.

Recommended model: fixed vertical flight, a fixed contact plane at the bottom of the target, and angular occupancy rules. This is an authored arcade rule rather than a general rigid-body simulation.

Store embedded knife angles relative to the target. On reaching the contact plane, convert the entry direction into the target's local angle and compare the shortest angular distance to every embedded knife. Reject a landing within the configured exclusion angle. Tune that angle together with knife width, target radius and visual attachment so visible near misses feel fair.

The exclusion angle is the required center-to-center separation, not half that separation. Include manual checks across the 0/360-degree boundary. Check that the authored layouts leave enough usable space for all required knives.

Resolve a flight crossing at its contact time, including frames that overshoot the plane. Evaluate target rotation at that same time. Use one explicit gameplay update order owned by the session so independent component Update ordering cannot change outcomes. Keep target shake/squash on a visual child; it must not alter the logical target angle or hit geometry.

## Result and Lifecycle Rules

1. Accept a fresh gameplay press only in `Playing`, when no knife is flying and no UI control consumed the press. Held fingers do not repeatedly throw; mouse input remains available for Editor iteration.
2. Reserve one remaining knife when a throw is accepted. Reaching zero remaining knives alone does not win the stage.
3. Resolve each flying knife once. A rejected landing enters `GameOver` and never advances the stage, including on the last knife.
4. A successful landing attaches the visual, and adds its occupied angle. Then either unlock throwing or enter `Transition`/`Victory` when no knives remain.
5. Disable gameplay input throughout transitions and results. Clear active knife references, old target children and transition callbacks when starting another stage or run.
6. Handle focus loss and app suspension idempotently. Freeze flight, rotation and transition timing together, remember the previous gameplay state, and require Continue after foregrounding. The Continue press must not also throw a knife.
7. Reopening a terminated process starts at the menu. Persist preferences, not the active run.

Feedback should reinforce the accepted result: a short hit sound and target pulse, stronger failure feedback, and a brief clear transition. Tune flight duration and response on a real phone before adding optional effects. HUD layouts respect the device safe area.

## Delivery Schedule

| Day | Work | Exit condition |
| --- | --- | --- |
| 1, 6–8 h | Single-stage loop, angular landing rules, restart, minimal HUD, Android build setup | APK installs on a phone; touch, win and loss all work |
| 2, 6–8 h | Five stage assets, rotation cycles, run progression, sound preference, app pause/resume | Complete a five-stage run; restart and backgrounding behave correctly |
| 3, 6–8 h | Flat art, sound, hit/fail effects, layout, owner-led manual testing, difficulty tuning | Release-candidate APK passes the owner's device checklist |
| 4, if available | Device fixes, performance checks, final balancing and packaging | Final APK and reproducible build notes; no new gameplay systems |

If time slips, reduce cosmetic polish first and preserve the five-stage loop and lifecycle correctness. Publish each day's playable build early enough to find Android-specific problems.

## Verification and Completion

The owner tests manually in the Editor and on an Android device. Do not implement or run automated tests, create test assemblies, or add a testing milestone. Provide a playable build with concise steps covering angular wraparound, exclusion boundaries, knife counter updates, one-flight input gating, last-knife failure, single stage advancement, replay cleanup, and pause during flight or transition.

On a real Android phone, check rapid/multitouch input, UI taps, home/return, lock/unlock, repeated focus notifications, process restart, mute persistence, portrait safe area and offline play. Compare landings under 30 and 60 FPS conditions; no double results or frame-rate-dependent hit decisions are acceptable. Target stable 60 FPS on the device used for delivery and record that device in the build notes.

Completion requires an installable offline APK, all five stages playable in sequence, correct failure/replay behavior, persisted preferences, reliable pause/resume and no blocking errors.

## References

- [Unity ScriptableObject](https://docs.unity.cn/6000.6/Documentation/Manual/class-ScriptableObject.html): authored data assets.
- [Unity Mathf.DeltaAngle](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Mathf.DeltaAngle.html): shortest angular difference.
- [Unity PlayerPrefs](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PlayerPrefs.html): local preferences retained between launches.
