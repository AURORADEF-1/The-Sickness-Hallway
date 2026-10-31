# Hallway source portability audit — 31 October 2026

## What to include

Copy the complete `Assets/`, `Packages/` and `ProjectSettings/` directories. Include the handoff documents, `Check-WindowsSetup.ps1`, and the portable `WindowsHandoffTools.cs` helper at `Assets/Editor/WindowsHandoffTools.cs` with its `.meta`. Preserve all license, credit and source text files already under Assets. Root packaging will add repository configuration and the exact release checksum.

Do not prune assets solely from serialized scene dependency graphs: the medical and trailer scripts also load materials, prefabs and audio using `Resources.Load`. Full Assets is currently the compact, reliable source transfer (~529 MiB).

Exclude caches and host state: `Library/`, `Library.before-*`, `Temp/`, `Logs/`, `UserSettings/`, `obj/`, `Builds/`, `.DS_Store`, generated `.csproj`/`.slnx`, Mac `.app` builds. No credentials or personal save files are needed. The project's `.vscode` is optional host/editor configuration and not required for the handoff.

## Entry points and runtime dependencies

| File | Role |
|---|---|
| `Assets/Scenes/Woodland.unity` | Main scene, player and persistent medical sequence owner |
| `Assets/Scenes/MedicalResearch.unity` | Medical interior loaded additively |
| `Assets/Woodland/Scripts/WoodlandMedicalSequence.cs` | Generator, additive loading, player binding, exterior suspension |
| `Assets/Woodland/Scripts/MedicalInterior.cs` | Interior interactions, hostiles, Thornback and ending |
| `Assets/Woodland/Scripts/MedicalTrailerSequence.cs` | Opt-in authored trailer director; not attached by ordinary demo |
| `Assets/Woodland/Scripts/MedicalThornbackCrawl.cs` | Creature movement/doorway animation |
| `Assets/Woodland/Scripts/MedicalThornbackEffects.cs` | Movement-linked scrape, thud, roar, damage/audio presentation |
| `Assets/Resources/Medical/ThornbackFX/` | User Foley, processed dialogue/mimic, roar, ending theme and credits |
| `Assets/Editor/ThornbackTrailerVideo.cs` | Existing deterministic Unity MediaEncoder capture/test harness |

`EditorBuildSettings.asset` already lists Woodland first and MedicalResearch second. Preserve this order. The inventory uses `Application.persistentDataPath`, which adapts to the new OS. A new Windows account starts without the Mac save; importing a personal save is not necessary to make the game work.

## Package/platform findings

Exact Unity: `6000.3.25f1 (e1dba0a9aba4)`.

Manifest: URP17.3.0, InputSystem1.20.0, AI Navigation2.0.14, Timeline1.8.13, uGUI2.0.0, VisualScripting1.9.12, testframework1.6.0, VS integration2.0.26. `com.unity.pipeline` is `0.8.0-exp.1`; it is an existing project dependency, so preserve it for the baseline import. Lockfile sources are Unity registry or built-in, with no external filesystem package paths found.

No native `.dll`, `.dylib`, `.so`, `.bundle` plugin or `.blend` file was found under Assets, and no `DllImport` was found in its C# source. FBX files and generated Unity meshes are already included; Blender, Meshy, Godot and a Mac installation are not needed to open this project. These checks do not prove Windows GPU/runtime compatibility.

## Historical host-specific tools

- `Assets/Editor/DesktopControllerCheck.cs`, `DesktopAimCheck.cs` and `DesktopCombatCheck.cs` write check reports to absolute Mac paths. `WindowsDemoBuild.Build` calls them, so that old builder is unsuitable on a fresh Windows machine even though its build target is Windows.
- `DesktopDemoBuild.cs` targets macOS and has Mac report/output paths.
- `DoorwayGeometryReview.cs` writes to a Mac work directory.
- `DetailPassReview.cs`, `DemoShowcaseRecorder.cs`, `LatestUpdatesRecorder.cs` and `UpdatedWalkthroughRecorder.cs` contain Mac work/Desktop locations. Do not attach these automatically on Windows.
- `ThornbackTrailerVideo` uses Unity's MediaEncoder rather than the local FFmpeg binary, but its output directory is relative to the old repository layout (`../../../../work/...`). Adapt it to a project-relative output before invoking on Windows. Some review harnesses exit Unity upon completion.
- The new `SicknessWindowsHandoff` builder/preview helper avoids those report paths. It does not alter runtime scripts. It supports manual play and Windows builds without assuming the old workspace layout.

## Validation scope

This audit is source inspection on macOS. The new helper passed standalone compilation with Unity 6000.3.25f1's Roslyn compiler against the project's actual Unity and game assemblies (exit code 0). The PowerShell installer was reviewed but not executed on Windows. The helper must still import on Windows, and its preview and resulting executable must be tested there. A Windows build report is separate from runtime verification. Preserve the private repository/release as the baseline and record any changes made during the first Windows import.
