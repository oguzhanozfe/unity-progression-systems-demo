using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OguzhanOzdemir.ProgressionDemo.Editor
{
    public static class DemoProjectBuilder
    {
        private const string ScenePath = "Assets/Scenes/ProgressionDemo.unity";

        [MenuItem("Demo/Create clean scene")]
        public static void CreateScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.companyName = "Oguzhan Ozdemir";
            PlayerSettings.productName = "Progression Systems Demo";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, "dev.oguzhanozdemir.progressiondemo");
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Demo/Build macOS")]
        public static void BuildMac()
        {
            CreateScene();
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string output = Argument("-buildOutput") ?? Path.Combine(root, "Builds", "macOS", "ProgressionSystemsDemo.app");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.CleanBuildCache
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Build failed with " + report.summary.totalErrors + " errors.");
            Debug.Log("Demo build succeeded: " + output);
        }

        private static string Argument(string key)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == key) return args[i + 1];
            return null;
        }
    }
}

