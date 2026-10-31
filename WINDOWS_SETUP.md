# The Sickness — hallway development on Windows

This is the editable Unity source handoff, prepared 31 October 2026. Start from the supplied project ZIP, or clone the private repository and fetch its Git LFS objects. A Mac `.app` is not a Windows project or executable.

## Exact project version and contents

- **Unity 6000.3.25f1**, revision **e1dba0a9aba4**. Use this exact version for the first import. Both values were read from `ProjectSettings/ProjectVersion.txt` and checked against [Unity's release page](https://unity.com/releases/editor/whats-new/6000.3.25f1).
- Universal Render Pipeline **17.3.0**; Input System **1.20.0**; AI Navigation **2.0.14**; Timeline **1.8.13**. Keep `Packages/manifest.json` and `packages-lock.json` unchanged initially.
- Keep **all of `Assets`, `Packages`, and `ProjectSettings`**, including every `.meta` file. The hallway uses the woodland player's rifle, arms, inventory, audio, controller and medical systems. Sending `MedicalResearch.unity` alone loses these dependencies. Unity uses metadata to preserve asset identity and references. [Unity asset metadata](https://docs.unity.com/en-us/engine/6000.3/manual/assets-and-media/import-assets/asset-metadata)
- Source assets occupy approximately **529 MiB** before compression; allow additional disk space for Unity, imported caches and builds. `Library`, `Library.before-*`, `Temp`, `Logs`, `UserSettings`, generated solution/project files and Mac applications are intentionally excluded.

## Install on the Windows PC

Use a native Windows 11 x64 workspace, such as `C:\Dev\TheSickness`. Keep this first working copy outside a live OneDrive folder and use the native PowerShell agent so Unity and Codex see the same paths.

1. Install **Unity Hub** from [Unity's download page](https://unity.com/download). Sign in and activate the appropriate Unity license normally. Install Editor **6000.3.25f1** using the release page above; do not select “latest” or convert to HDRP.
2. Ensure **Windows desktop build support** is available. The handoff builder targets **Windows x64, Mono, Direct3D 11**. IL2CPP is optional for this first working build. If you later choose IL2CPP, add Windows Build Support (IL2CPP), Visual Studio C++ tools and a Windows SDK. Unity documents the additional IL2CPP tools separately. [Unity Windows requirements](https://docs.unity.com/en-us/engine/6000.3/manual/platform-specific/windows/requirements-and-compatibility)
3. Install **Visual Studio 2022** (an edition your organisation is entitled to use), selecting **Game development with Unity**. In Unity, select it under **Edit → Preferences → External Tools**. C++ workloads are not needed for the initial Mono route. [Microsoft's Unity setup guide](https://learn.microsoft.com/en-us/visualstudio/gamedev/unity/get-started/getting-started-with-visual-studio-tools-for-unity)
4. Install the **Windows Codex/ChatGPT desktop app**, then sign in. The official Windows app supports the native PowerShell agent; WSL is optional. The current official Store command is `winget install --id 9PLM9XGG6VKS -s msstore`. [Official OpenAI Windows setup](https://learn.chatgpt.com/docs/app/windows)
5. Install **Git for Windows**. For repository development, verify `git lfs version`; if absent install [Git LFS](https://docs.github.com/en/repositories/working-with-files/managing-large-files/installing-git-large-file-storage). **GitHub CLI** is convenient for private repository authentication and downloads. Node.js and Python are optional automation tools; Blender is not required to import the supplied FBX/Unity assets.

`Check-WindowsSetup.ps1` audits these tools by default. Add `-InstallTools` to install missing Unity Hub and Git through WinGet; add `-WithOptionalTools` for Node.js, Python and GitHub CLI. It does not change security policies, activate licenses, sign into accounts or silently accept agreements. Inspect the script first; organisational prompts may require someone at the PC. WinGet's exact package selection and installation flags are documented by [Microsoft](https://learn.microsoft.com/en-us/windows/package-manager/winget/install).

After installing tools, restart the terminal so PATH changes take effect. Install the exact Unity Editor through Hub's UI. The older Hub headless CLI is now documented as deprecated from Hub 3.18, so this handoff does not depend on that installer interface. [Hub CLI reference](https://docs.unity.com/en-us/hub/hub-cli-reference)

## First open

1. Extract the **attached release asset ZIP**, preserving its folder structure. Do not open inside the ZIP. If using the repository instead, authenticate as an account with access, clone it, then run `git lfs install --local` and `git lfs pull` inside that repository. Check `git lfs status` before opening Unity. A tiny text file beginning `version https://git-lfs.github.com/spec/v1` is a missing binary, not the actual model/audio. Prefer the prepared release asset over GitHub's automatically generated source archive.
2. In Unity Hub, **Add project from disk**, choosing the folder containing `Assets`, `Packages` and `ProjectSettings`. Open with **6000.3.25f1**.
3. Allow the first Windows import and package restore to finish. This rebuilds the deliberately omitted Mac cache. Resolve Console compile errors before pressing Play; do not replace missing assets with placeholders.
4. The starting scene is **`Assets/Scenes/Woodland.unity`**. `WoodlandMedicalSequence.Enter()` loads **`Assets/Scenes/MedicalResearch.unity` additively** and binds the existing player. Opening the medical scene by itself is useful for editing geometry, but it is not a complete playable entrypoint.
5. The package's helper under `Assets/Editor/WindowsHandoffTools.cs` provides **Tools → The Sickness → Handoff → Play hallway gameplay** and **Play hallway cinematic**. The gameplay preview enters the medical building with its real player and combat systems. The cinematic preview runs the existing authored trailer sequence. Both are temporary Play Mode setups and do not save modified scenes or inventory. Stop Play Mode to return to editing.

## Build a Windows playable

Use **Tools → The Sickness → Handoff → Build Windows x64 (Mono)**. The helper explicitly includes both scenes and writes a fresh timestamped folder under `Builds`.

For Codex/PowerShell automation, close any other Unity instance using the project, then run (adjust the two paths):

```powershell
$unityExe = 'C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe'
$projectPath = 'C:\Dev\TheSickness\The Sickness Woodland'
$outputPath = Join-Path $projectPath ('Builds\Windows-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
$unityArgs = @('-batchmode', '-quit', '-projectPath', $projectPath,
    '-buildTarget', 'Win64', '-executeMethod', 'SicknessWindowsHandoff.BuildWindows',
    '-sicknessBuildPath', $outputPath, '-logFile', (Join-Path $outputPath 'Editor-build.log'))
# Quote path-bearing arguments for Windows PowerShell Start-Process.
$quotedArgs = ($unityArgs | ForEach-Object { '"' + $_.Replace('"', '\"') + '"' }) -join ' '
$process = Start-Process -FilePath $unityExe -ArgumentList $quotedArgs -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "Unity failed: $($process.ExitCode). Read Editor-build.log." }
Get-Content (Join-Path $outputPath 'Build verification.txt')
```

Send/run the **whole build folder**, including `The Sickness Demo.exe`, its `_Data` folder and Unity runtime files. The `.exe` alone is incomplete. This package is editable source; Windows runtime success must be established on the Windows PC.

The earlier `WindowsDemoBuild.Build` calls three checks whose reports use `/Users/georgeambrose/...` paths. Use the new handoff builder above, not that historical entrypoint. The new builder checks required sources/resources and reports Unity build results; it does **not** claim the old behavioural checks or a Windows playthrough passed.

## Windows verification checklist

- Import finishes with no script errors; materials are rendered correctly by URP.
- In Woodland: rifle/arms/watch visible, red dot visible with torch off, torch works, controller movement/aim/pause work.
- Enter the medical building and confirm doors, supplies, lights, armed/unarmed exiles and Thornback load.
- In the cinematic preview: NPC disappearance and scream; masked dialogue; backpack unzip and empty battery search; white torch failure with red flicker; five shots, brass impacts, two empty clicks and panic line; claws hold the doorway; scraping only during movement; roar/shake; black cut; red title and running ink/music.
- In playable combat: verify NPC hit reactions, blood, weapon attachment and ragdoll on the actual Windows GPU. Check lighting, audio balance and controller responsiveness there.
- Build x64, launch the `.exe` outside the Editor, repeat the main hallway checks and retain the log. Windows player logs normally appear under `%USERPROFILE%\AppData\LocalLow\Sadtrip\The Sickness Demo\Player.log` if the project's company/product names remain unchanged.

## Paste this into Codex on the Windows PC

> Set up this extracted The Sickness Unity project for Windows development. Read WINDOWS_SETUP.md and PORTABILITY_AUDIT.md first. Preserve all existing gameplay, assets, .meta GUIDs, audio, animation and scene references. Inspect installed software; install only missing required tools from official sources, and leave normal sign-in/license/UAC interactions to me when needed. Use native Windows PowerShell and Unity 6000.3.25f1 revision e1dba0a9aba4. Do not upgrade Unity/packages or change URP. Install Unity Hub and Git, verify Git LFS if this is a clone, and set up Visual Studio with the Unity workload. Restore packages, resolve genuine Windows import issues, and use the included SicknessWindowsHandoff preview menus to inspect the hallway gameplay and cinematic. Build Windows x64/Mono/D3D11 with SicknessWindowsHandoff.BuildWindows, then launch and visually/audio test the resulting game with Xbox controller support. Report actual tests, errors and paths; do not call the transfer complete merely because the Editor compiled. Keep changes reversible and do not run the old Mac-path build/check scripts. Capture a short Windows verification video after the full hallway sequence works.

## Preserved creative direction

Keep the **original Thornback** used by the current scene, not the rejected replacement rig. Preserve its long-arm doorway grip, slow pull, synchronized plant/drag/room damage, restrained lighting, inhuman mimic, masked player dialogue and final roar. The new bodycam film is a separate presentation variant; do not replace the ordinary playable camera with it. The source ZIP is the handoff baseline, not a claim that every visual issue in the wider demo has been resolved.
