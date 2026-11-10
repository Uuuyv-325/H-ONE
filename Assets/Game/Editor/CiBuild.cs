using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HOne.Game.Editor
{
    /// <summary>GameCI entry point. Uses an explicit scene list without rewriting Editor Build Settings.</summary>
    public static class CiBuild
    {
        private const string SceneListPath = ".github/unity-build-scenes.txt";

        public static void Build()
        {
            if (!Application.isBatchMode)
                throw new InvalidOperationException("CiBuild.Build is for batch-mode CI only.");

            try
            {
                BuildPlayer();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static string[] ReadScenes()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var scenes = new List<string>();
            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (var line in File.ReadAllLines(Path.Combine(projectRoot, SceneListPath)))
            {
                var path = line.Trim();
                if (path.Length == 0 || path.StartsWith("#", StringComparison.Ordinal)) continue;
                if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains("..") ||
                    !path.EndsWith(".unity", StringComparison.Ordinal) ||
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                    throw new BuildFailedException("Invalid or missing CI scene: " + path);
                if (!unique.Add(path)) throw new BuildFailedException("Duplicate CI scene: " + path);
                scenes.Add(path);
            }
            if (scenes.Count == 0) throw new BuildFailedException("The CI scene list is empty.");
            return scenes.ToArray();
        }

        private static void BuildPlayer()
        {
            var targetName = RequiredArgument("-buildTarget");
            if (!Enum.TryParse(targetName, true, out BuildTarget target) ||
                (target != BuildTarget.StandaloneWindows64 && target != BuildTarget.StandaloneLinux64 &&
                 target != BuildTarget.StandaloneOSX && target != BuildTarget.WebGL))
                throw new BuildFailedException("Unsupported CI target: " + targetName);
            if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target), target))
                throw new BuildFailedException("Unity build support is not installed for " + target);

            var outputPath = RequiredArgument("-customBuildPath");
            if (target == BuildTarget.StandaloneOSX && !outputPath.EndsWith(".app", StringComparison.Ordinal))
                outputPath += ".app";
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath)));
            var scenes = ReadScenes();

            var productName = PlayerSettings.productName;
            var backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
            var compression = PlayerSettings.WebGL.compressionFormat;
            var fallback = PlayerSettings.WebGL.decompressionFallback;
            var threads = PlayerSettings.WebGL.threadsSupport;
            try
            {
                PlayerSettings.productName = GameFlow.GameTitle;
                if (target == BuildTarget.WebGL)
                {
                    // Pages cannot set Content-Encoding or cross-origin isolation headers.
                    PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
                    PlayerSettings.WebGL.decompressionFallback = true;
                    PlayerSettings.WebGL.threadsSupport = false;
                }
                else
                {
                    // Linux containers can cross-compile the desktop Mono players.
                    PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
                }

                Debug.Log($"CI building {target}: {string.Join(", ", scenes)} -> {outputPath}");
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = outputPath,
                    target = target,
                    options = BuildOptions.StrictMode
                });
                var summary = report.summary;
                Debug.Log($"CI build {summary.result}: {summary.totalSize} bytes in {summary.totalTime}");
                if (summary.result != BuildResult.Succeeded)
                    throw new BuildFailedException($"{target} build failed with {summary.totalErrors} errors.");
            }
            finally
            {
                PlayerSettings.productName = productName;
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, backend);
                PlayerSettings.WebGL.compressionFormat = compression;
                PlayerSettings.WebGL.decompressionFallback = fallback;
                PlayerSettings.WebGL.threadsSupport = threads;
            }
        }

        private static string RequiredArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var index = 0; index + 1 < args.Length; index++)
                if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(args[index + 1]) && !args[index + 1].StartsWith("-"))
                    return args[index + 1];
            throw new BuildFailedException("Missing command-line argument: " + name);
        }
    }
}
