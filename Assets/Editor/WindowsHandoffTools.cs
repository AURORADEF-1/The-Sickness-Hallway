#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Package-only convenience tools. Runtime scenes and gameplay are unchanged.
[InitializeOnLoad]
public static class SicknessWindowsHandoff
{
    const string PreviewKey = "Sickness.WindowsHandoff.Preview";
    const string PersistKey = "Sickness.WindowsHandoff.OriginalPersist";
    const string Woodland = "Assets/Scenes/Woodland.unity";
    const string Medical = "Assets/Scenes/MedicalResearch.unity";
    static double deadline;
    static int stage;

    static SicknessWindowsHandoff()
    {
        EditorApplication.playModeStateChanged += ModeChanged;
    }

    [MenuItem("Tools/The Sickness/Handoff/Play hallway gameplay")]
    public static void PreviewGameplay() { StartPreview(1); }

    [MenuItem("Tools/The Sickness/Handoff/Play hallway cinematic")]
    public static void PreviewCinematic() { StartPreview(2); }

    static void StartPreview(int mode)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before starting another hallway preview.");
        ValidateRequiredFiles();
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(Woodland);
        var inventory = Object.FindFirstObjectByType<WoodlandInventory>();
        if (!inventory) throw new InvalidOperationException("Woodland inventory is missing; inspect scene imports.");
        SessionState.SetBool(PersistKey, inventory.persist);
        // Do this before Awake loads a save. This temporary editor value is
        // restored on leaving Play Mode; the scene is never saved by this tool.
        inventory.persist = false;
        SessionState.SetInt(PreviewKey, mode);
        EditorApplication.isPlaying = true;
    }

    static void ModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetInt(PreviewKey, 0) != 0)
        {
            stage = 0;
            deadline = EditorApplication.timeSinceStartup + 180;
            EditorApplication.update -= PreviewTick;
            EditorApplication.update += PreviewTick;
        }
        if (state == PlayModeStateChange.ExitingPlayMode)
            EditorApplication.update -= PreviewTick;
        if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetInt(PreviewKey, 0) != 0)
        {
            var inventory = Object.FindFirstObjectByType<WoodlandInventory>();
            if (inventory) inventory.persist = SessionState.GetBool(PersistKey, true);
            SessionState.SetInt(PreviewKey, 0);
        }
    }

    static void PreviewTick()
    {
        try
        {
            if (!Application.isPlaying) return;
            if (EditorApplication.timeSinceStartup > deadline)
                throw new TimeoutException("Hallway preview did not initialise within 180 seconds. Inspect Console errors.");
            var flow = Object.FindFirstObjectByType<WoodlandMedicalSequence>();
            if (!flow || !flow.player) return;
            if (stage == 0)
            {
                flow.player.inventory.persist = false;
                flow.powered = true;
                flow.StartCoroutine(flow.Enter());
                stage = 1;
                return;
            }
            if (!flow.inside || !flow.interior || flow.loading) return;
            if (SessionState.GetInt(PreviewKey, 0) == 2)
            {
                var room = flow.interior;
                foreach (var hostile in room.hostiles)
                    if (hostile) hostile.gameObject.SetActive(false);
                if (room.bleedingExile) room.bleedingExile.gameObject.SetActive(false);
                room.collapseStarted = true;
                flow.player.TeleportTo(new Vector3(200, .08f, 39.2f), 180);
                flow.player.eye.transform.localRotation = Quaternion.identity;
                room.jammedDoor.localRotation = Quaternion.Euler(0, -105, 0);
                room.breached = true;
                room.gameObject.AddComponent<MedicalTrailerSequence>().Configure(room);
            }
            Debug.Log("The Sickness hallway preview ready. Stop Play Mode to return to editing. No scene or inventory save was written.");
            EditorApplication.update -= PreviewTick;
        }
        catch (Exception error)
        {
            EditorApplication.update -= PreviewTick;
            Debug.LogException(error);
            EditorApplication.isPlaying = false;
        }
    }

    [MenuItem("Tools/The Sickness/Handoff/Validate source dependencies")]
    public static void ValidateRequiredFiles()
    {
        foreach (var path in new[] { Woodland, Medical })
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(path))
                throw new FileNotFoundException("Required scene is missing or not imported: " + path);
        foreach (var clip in new[] {
            "monster-lure-inhuman", "monster-postshot-inhuman", "player-masked-oh-fuck",
            "player-masked-empty-panic", "npc-disappear-scream", "backpack-unzip",
            "claw-drag-8560", "claw-drag-8560-b", "doorway-roar-titan", "ending-theme"
        })
            if (!Resources.Load<AudioClip>("Medical/ThornbackFX/" + clip))
                throw new FileNotFoundException("Required audio is missing or not imported: " + clip);
        Debug.Log("Required hallway scenes and current trailer audio are imported. This is a source check, not a runtime playtest.");
    }

    [MenuItem("Tools/The Sickness/Handoff/Build Windows x64 (Mono)")]
    public static void BuildWindows()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before building.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        ValidateRequiredFiles();
        const BuildTarget target = BuildTarget.StandaloneWindows64;
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
            throw new InvalidOperationException("Install Windows build support for Unity 6000.3.25f1 in Unity Hub.");
        var args = Environment.GetCommandLineArgs();
        int argument = Array.IndexOf(args, "-sicknessBuildPath");
        string output = argument >= 0 && argument + 1 < args.Length ? args[argument + 1] :
            Path.Combine(Path.GetDirectoryName(Application.dataPath), "Builds", "Windows-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        output = Path.GetFullPath(output);
        Directory.CreateDirectory(output);
        var named = NamedBuildTarget.Standalone;
        var originalBackend = PlayerSettings.GetScriptingBackend(named);
        bool originalAuto = PlayerSettings.GetUseDefaultGraphicsAPIs(target);
        var originalApis = PlayerSettings.GetGraphicsAPIs(target);
        bool originalStrip = DesktopDemoShaderStrip.Enabled;
        try
        {
            DesktopDemoShaderStrip.Enabled = false;
            PlayerSettings.SetScriptingBackend(named, ScriptingImplementation.Mono2x);
            PlayerSettings.SetUseDefaultGraphicsAPIs(target, false);
            PlayerSettings.SetGraphicsAPIs(target, new[] { GraphicsDeviceType.Direct3D11 });
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { Woodland, Medical }, target = target,
                locationPathName = Path.Combine(output, "The Sickness Demo.exe"),
                options = BuildOptions.CompressWithLz4
            });
            File.WriteAllText(Path.Combine(output, "Build verification.txt"),
                "Unity " + Application.unityVersion + "\nHost: " + Application.platform +
                "\nWindows x64 / Direct3D 11 / Mono\nResult: " + report.summary.result +
                "\nErrors: " + report.summary.totalErrors + "\nWarnings: " + report.summary.totalWarnings +
                "\nBoth Woodland and MedicalResearch scenes included.\n" +
                "Required source scene/audio check passed. Behavioural checks and Windows runtime playtest are not performed by this builder.\n");
            if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors != 0)
                throw new InvalidOperationException("Windows build failed. Inspect the Unity build log.");
            Debug.Log("Windows build completed: " + output);
        }
        finally
        {
            DesktopDemoShaderStrip.Enabled = originalStrip;
            PlayerSettings.SetScriptingBackend(named, originalBackend);
            PlayerSettings.SetUseDefaultGraphicsAPIs(target, originalAuto);
            PlayerSettings.SetGraphicsAPIs(target, originalApis);
        }
    }
}
#endif
