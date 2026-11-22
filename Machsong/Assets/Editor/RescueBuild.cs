using System;
using System.IO;
using MachSong;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RescueBuild
{
    public const string ScenePath = "Assets/Scenes/Rescue.unity";
    private const string MaterialPath = "Assets/Resources/MachSong/SceneSprite.mat";

    [MenuItem("Mach Song/Open Rescue Scene")]
    public static void Open()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Prepare();
        EditorSceneManager.OpenScene(ScenePath);
    }

    public static void Prepare()
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) throw new InvalidOperationException("Unity's built-in sprite shader is unavailable.");
            AssetDatabase.CreateAsset(new Material(shader),MaterialPath);
        }
        if (!File.Exists(ScenePath))
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("Mach Song Rescue",typeof(RescueGame));
            if (!EditorSceneManager.SaveScene(scene,ScenePath))
                throw new InvalidOperationException("Could not save the Mach Song rescue scene.");
        }
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath,true) };
        PlayerSettings.companyName = "Cosmic Horizon";
        PlayerSettings.productName = "Mach Song";
        PlayerSettings.defaultScreenWidth = 1440;
        PlayerSettings.defaultScreenHeight = 810;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Mach Song/Build Windows")]
    public static void Windows()
    {
        Prepare();
        Directory.CreateDirectory("Builds/MachSong");
        var report = BuildPipeline.BuildPlayer(new[]{ScenePath},"Builds/MachSong/MachSong.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Windows build failed: "+report.summary.result);
        Debug.Log($"MACH SONG BUILD {report.summary.totalSize} bytes");
    }
}
