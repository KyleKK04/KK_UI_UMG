using System;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace KK.UI.UMG.Editor.Pipeline
{
    [InitializeOnLoad]
    internal static class PendingPrefabGenerationScheduler
    {
        private const string PendingManifestKey = "KK.UI.UMG.PendingPrefabManifestPath";
        private const string NextRunTimeKey = "KK.UI.UMG.PendingPrefabNextRunTime";
        private static bool _isRunningContinuation;

        static PendingPrefabGenerationScheduler()
        {
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            EditorApplication.update += ContinueIfReady;
        }

        public static bool IsRunningContinuation => _isRunningContinuation;

        public static void Schedule(string packageManifestPath)
        {
            if (string.IsNullOrWhiteSpace(packageManifestPath))
            {
                return;
            }

            SessionState.SetString(PendingManifestKey, Path.GetFullPath(packageManifestPath));
            SessionState.SetFloat(NextRunTimeKey, (float)(EditorApplication.timeSinceStartup + 0.5d));
            EditorApplication.update -= ContinueIfReady;
            EditorApplication.update += ContinueIfReady;
        }

        private static void OnCompilationFinished(object obj)
        {
            ContinueIfReady();
        }

        private static void ContinueIfReady()
        {
            if (_isRunningContinuation || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            if (EditorApplication.timeSinceStartup < SessionState.GetFloat(NextRunTimeKey, 0f))
            {
                return;
            }

            var packageManifestPath = SessionState.GetString(PendingManifestKey, string.Empty);
            if (string.IsNullOrWhiteSpace(packageManifestPath))
            {
                return;
            }

            if (!File.Exists(packageManifestPath))
            {
                SessionState.EraseString(PendingManifestKey);
                SessionState.EraseString(NextRunTimeKey);
                Debug.LogWarning($"[KK_UI_UMG] Pending Generate skipped because manifest does not exist: {packageManifestPath}");
                return;
            }

            _isRunningContinuation = true;
            try
            {
                SessionState.EraseString(PendingManifestKey);
                SessionState.EraseString(NextRunTimeKey);
                var result = new KKUIPipeline().Run(packageManifestPath);
                if (result.Status == "PendingCompile")
                {
                    Schedule(packageManifestPath);
                }
                else if (!result.Success)
                {
                    Debug.LogError($"[KK_UI_UMG] Auto Generate failed: {result.Error ?? result.Status}");
                }
                else
                {
                    Debug.Log($"[KK_UI_UMG] Auto Generate completed for {packageManifestPath}.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _isRunningContinuation = false;
            }
        }
    }
}
