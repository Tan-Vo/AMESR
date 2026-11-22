using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class CosmicHorizonCapture
{
    private const int Width = 1920, Height = 1080;

    /// <summary>Synchronously imports assets without rebuilding scenes.</summary>
    [MenuItem("Cosmic Horizon/Capture/Import Only")]
    public static void ImportOnly()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.SaveAssets();
        Debug.Log("Cosmic Horizon assets imported.");
    }

    /// <summary>Builds the slice and captures each enabled build scene.</summary>
    [MenuItem("Cosmic Horizon/Capture/Build And Capture All")]
    public static void BuildAndCaptureAll()
    {
        CosmicHorizonProjectBuilder.BuildVerticalSlice();
        CaptureAll();
    }

    /// <summary>Captures existing build scenes without rebuilding them.</summary>
    [MenuItem("Cosmic Horizon/Capture/Capture Only")]
    public static void CaptureOnly() => CaptureAll();

    /// <summary>Renders enabled build scenes with their overlay UI to Captures/scene.png.</summary>
    public static void CaptureAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Finish script import and exit Play Mode before capturing scenes.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
        if (scenes.Length == 0) throw new InvalidOperationException("No enabled build scenes. Run Build Vertical Slice first.");
        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Captures"));
        Directory.CreateDirectory(folder);
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (var entry in scenes)
            {
                var scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
                var camera = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>())
                    .FirstOrDefault(value => value.enabled && value.gameObject.activeInHierarchy);
                if (camera == null) throw new InvalidOperationException("No enabled camera in " + entry.path);
                CaptureScene(scene, camera, Path.Combine(folder, scene.name + ".png"));
            }
        }
        finally { if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        Debug.Log("Cosmic Horizon captured " + scenes.Length + " scenes to " + folder);
    }

    private static void CaptureScene(Scene scene, Camera camera, string path)
    {
        var canvases = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Canvas>(true))
            .Where(canvas => canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
        var modes = canvases.Select(canvas => canvas.renderMode).ToArray();
        var cameras = canvases.Select(canvas => canvas.worldCamera).ToArray();
        var distances = canvases.Select(canvas => canvas.planeDistance).ToArray();
        var previousTarget = camera.targetTexture; var previousActive = RenderTexture.active; float previousAspect = camera.aspect;
        var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
        var pixels = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        try
        {
            target.Create(); camera.targetTexture = target; camera.aspect = (float)Width / Height;
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera;
                canvases[i].planeDistance = camera.nearClipPlane + 1f;
            }
            Canvas.ForceUpdateCanvases();
            var request = new RenderPipeline.StandardRequest { destination = target };
            if (RenderPipeline.SupportsRenderRequest(camera, request))
                RenderPipeline.SubmitRenderRequest(camera, request);
            else camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); pixels.Apply();
            File.WriteAllBytes(path, pixels.EncodeToPNG());
            Debug.Log("Captured " + path);
        }
        finally
        {
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = modes[i]; canvases[i].worldCamera = cameras[i]; canvases[i].planeDistance = distances[i];
            }
            camera.targetTexture = previousTarget; camera.aspect = previousAspect; RenderTexture.active = previousActive;
            target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(pixels);
        }
    }
}
