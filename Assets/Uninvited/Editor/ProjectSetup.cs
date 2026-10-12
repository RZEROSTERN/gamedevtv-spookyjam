using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Uninvited.Editor
{
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/Uninvited/Scenes/Uninvited.unity";
        public const string BalancePath = "Assets/Uninvited/Data/GameBalance.asset";
        [MenuItem("Uninvited/Generate game scene")]
        public static void Generate()
        {
            Directory.CreateDirectory("Assets/Uninvited/Scenes");
            Directory.CreateDirectory("Assets/Uninvited/Data");
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(BalancePath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GameConfig>();
                config.SetDefaults();
                AssetDatabase.CreateAsset(config, BalancePath);
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Uninvited Game").AddComponent<GameBootstrap>().Configure(config);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            ConfigureProject();
            AssetDatabase.SaveAssets();
            Debug.Log("UNINVITED: Scene and balance generated. Open the scene and press Play.");
        }
        static void ConfigureProject()
        {
            PlayerSettings.companyName = "Spooky Jam";
            PlayerSettings.productName = "Uninvited";
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.resizableWindow = true;
            // Keep rendering/layout alive when an iframe loses focus or resizes.
            // GameBootstrap explicitly pauses the simulation on focus loss.
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = true;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.template = "PROJECT:Uninvited";
            PlayerSettings.WebGL.initialMemorySize = 64;
            PlayerSettings.WebGL.maximumMemorySize = 256;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input = settings.FindProperty("activeInputHandler");
            if (input != null) input.intValue = 0;
            settings.ApplyModifiedPropertiesWithoutUndo();
            QualitySettings.vSyncCount = 0;
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Uninvited/Verify gameplay rules")]
        public static void Verify() => GameplayVerification.Run();
        [MenuItem("Uninvited/Build Web")]
        public static void BuildWeb()
        {
            if (!File.Exists(ScenePath)) Generate();
            Verify();
            ConfigureProject();
            Directory.CreateDirectory("Builds/Web");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = "Builds/Web", target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Web build failed: " + report.summary.result);
            Debug.Log($"UNINVITED: Web build succeeded ({report.summary.totalSize} bytes).");
        }
        public static void VerifyAndBuild() { Generate(); BuildWeb(); }
        public static void GenerateAndVerify() { Generate(); Verify(); }
    }
}
