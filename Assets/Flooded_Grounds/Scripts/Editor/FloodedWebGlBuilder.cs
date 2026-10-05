using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

public static class FloodedWebGlBuilder
{
    public const string BUILD_PATH = "Builds/WebGL";

    [MenuItem("FloodedGrounds/1. Configure WebGL Settings", priority = 50)]
    public static void ConfigureSettings()
    {
        EditorUtility.DisplayProgressBar("WebGL Setup", "Configuring WebGL player settings...", 0.2f);
        try
        {
            // 1. Switch Active Target to WebGL if not already
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            }

            // 2. Set Scenes in Build Settings
            string scenePath = "Assets/Scenes/FloodedGrounds_Complete.unity";
            EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(scenePath, true)
            };

            // 3. WebGL Heap Memory (Crucial for 3D URP scenes)
            PlayerSettings.WebGL.memorySize = 512;
            PlayerSettings.WebGL.initialMemorySize = 512;
            PlayerSettings.WebGL.maximumMemorySize = 2048;

            // 4. Universal Web Server Compatibility (No gzip/brotli header errors on itch.io or local server)
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.dataCaching = true;

            // 5. Canvas Resolution
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.WebGL.template = "APPLICATION:Default";

            // 6. Graphics & Render Pipeline Fallback
            var mobileRP = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            if (mobileRP != null)
            {
                GraphicsSettings.defaultRenderPipeline = mobileRP;
            }

            // 7. Product Details
            PlayerSettings.companyName = "FloodedGrounds";
            PlayerSettings.productName = "Beacon of the Flooded Ruins";

            AssetDatabase.SaveAssets();
            Debug.Log("<color=green>[FloodedWebGlBuilder] WebGL settings successfully configured for 3D URP!</color>\n" +
                      "- Memory: 512MB initial, 2048MB max\n" +
                      "- Compression: Disabled (Instantly compatible with all hosts & local testing)\n" +
                      "- Main Scene: Assets/Scenes/FloodedGrounds_Complete.unity\n" +
                      "- Canvas: 1280x720 (Default Template)");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    [MenuItem("FloodedGrounds/2. Build WebGL Player", priority = 51)]
    public static void BuildWebGlPlayer()
    {
        ConfigureSettings();

        string projectDir = Directory.GetCurrentDirectory();
        string fullBuildPath = Path.Combine(projectDir, BUILD_PATH);

        if (!Directory.Exists(fullBuildPath))
        {
            Directory.CreateDirectory(fullBuildPath);
        }

        Debug.Log($"<color=cyan>[FloodedWebGlBuilder] Starting WebGL Player Build to: {fullBuildPath}</color>");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new string[] { "Assets/Scenes/FloodedGrounds_Complete.unity" },
            locationPathName = BUILD_PATH,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            double sizeMb = summary.totalSize / (1024.0 * 1024.0);
            Debug.Log($"<color=green>★ [FloodedWebGlBuilder] WebGL BUILD SUCCEEDED! ★</color>\n" +
                      $"- Destination: {fullBuildPath}\n" +
                      $"- Output Size: {sizeMb:F2} MB\n" +
                      $"- Duration: {summary.totalTime.TotalMinutes:F1} min ({summary.totalTime.TotalSeconds:F0}s)\n" +
                      $"- Index File: {Path.Combine(fullBuildPath, "index.html")}");

            EditorUtility.RevealInFinder(Path.Combine(fullBuildPath, "index.html"));
        }
        else
        {
            Debug.LogError($"<color=red>[FloodedWebGlBuilder] WebGL Build Failed with {summary.totalErrors} errors.</color>");
            foreach (var step in report.steps)
            {
                foreach (var msg in step.messages)
                {
                    if (msg.type == LogType.Error || msg.type == LogType.Exception)
                    {
                        Debug.LogError($"[Build Error] {msg.content}");
                    }
                }
            }
        }
    }
}
