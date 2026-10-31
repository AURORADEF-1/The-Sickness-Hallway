# The Sickness — hallway development handoff

Editable Unity project for Windows development, prepared 31 October 2026.

**Start with [WINDOWS_SETUP.md](WINDOWS_SETUP.md).** It includes the exact software versions, a prompt to paste into Codex on Windows, first-open instructions, hallway preview menus and a Windows build command. [PORTABILITY_AUDIT.md](PORTABILITY_AUDIT.md) records platform findings and remaining checks.

## Download or clone

Private repository: https://github.com/AURORADEF-1/The-Sickness-Hallway

First transfer: use the prepared **The-Sickness-Hallway-Windows-Source-2026-10-31.zip** under the [Windows handoff release](https://github.com/AURORADEF-1/The-Sickness-Hallway/releases/tag/windows-handoff-2026-10-31). It contains the actual asset bytes. The automatically generated GitHub source ZIP may contain Git LFS pointers instead of binary assets.

Sign in to GitHub as **AURORADEF-1** to access this private repository. A different development account needs an explicit invitation; the email recipient address alone does not grant GitHub access.

For ongoing development, install Git and Git LFS, authenticate with GitHub, and clone this repository. Run `git lfs install --local` and `git lfs pull` inside the clone before opening Unity. Do not publish or change repository visibility during setup.

## Unity entry points

- Unity **6000.3.25f1** (revision **e1dba0a9aba4**), URP **17.3.0**.
- Main scene: `Assets/Scenes/Woodland.unity`.
- Medical scene: `Assets/Scenes/MedicalResearch.unity`, loaded additively using the main scene player.
- Editor shortcuts: **Tools → The Sickness → Handoff → Play hallway gameplay** or **Play hallway cinematic**.
- Windows builder: **Tools → The Sickness → Handoff → Build Windows x64 (Mono)**.

All Assets, Packages, ProjectSettings and asset `.meta` GUIDs are preserved. This includes the original Thornback, hallway set, weapons, NPCs, animations, processed dialogue, new dragging Foley, roar and title sequence. Caches, Mac builds and personal save data are excluded. Credit/source files remain beside the imported assets.

## Validation and limits

The source baseline produced the verified 65-second Unity hallway cinematic on macOS, with zero runtime errors, eight motion-synchronized scrape events, silent scrape rests and unclipped encoded audio. The included portable editor helper compiled against this exact Unity/project assembly set. The Windows machine must still import, build and run the project; no Windows runtime pass is claimed.

The separate bodycam film is a postproduction treatment of real Unity first-person footage. It preserves the original audio mix and leaves the red title ending clean; it does not alter the playable camera.
