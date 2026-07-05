using System;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace KK.UI.UMG.Editor.Pipeline
{
    public static class KKUIPackageBuilder
    {
        private const string PackageRoot = "Packages/com.kk.ui-umg";
        private const string OutputRoot = "Dist";

        private static PackRequest _request;
        private static string _outputDirectory;

        [MenuItem("KK_UI_UMG/Build Package", priority = 100)]
        public static void BuildPackage()
        {
            if (_request != null && !_request.IsCompleted)
            {
                Debug.LogWarning("[KK_UI_UMG] Package build is already running.");
                return;
            }

            if (!Directory.Exists(PackageRoot))
            {
                Debug.LogError($"[KK_UI_UMG] Package root does not exist: {PackageRoot}");
                return;
            }

            _outputDirectory = Path.GetFullPath(OutputRoot);
            Directory.CreateDirectory(_outputDirectory);
#pragma warning disable 618
            _request = Client.Pack(PackageRoot, _outputDirectory);
#pragma warning restore 618
            EditorApplication.update -= TickPackRequest;
            EditorApplication.update += TickPackRequest;
            Debug.Log($"[KK_UI_UMG] Building package '{PackageRoot}' to '{_outputDirectory}'.");
        }

        private static void TickPackRequest()
        {
            if (_request == null || !_request.IsCompleted)
            {
                return;
            }

            EditorApplication.update -= TickPackRequest;
            var request = _request;
            _request = null;

            if (request.Status == StatusCode.Failure)
            {
                Debug.LogError($"[KK_UI_UMG] Package build failed: {request.Error?.message}");
                return;
            }

            var tarballPath = request.Result?.tarballPath;
            if (string.IsNullOrWhiteSpace(tarballPath))
            {
                tarballPath = FindNewestTarball(_outputDirectory);
            }

            Debug.Log($"[KK_UI_UMG] Package build completed: {tarballPath}");
            if (!string.IsNullOrWhiteSpace(tarballPath))
            {
                EditorUtility.RevealInFinder(tarballPath);
            }
        }

        private static string FindNewestTarball(string outputDirectory)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory) || !Directory.Exists(outputDirectory))
            {
                return null;
            }

            var files = Directory.GetFiles(outputDirectory, "com.kk.ui-umg-*.tgz", SearchOption.TopDirectoryOnly);
            Array.Sort(files, (left, right) => File.GetLastWriteTimeUtc(right).CompareTo(File.GetLastWriteTimeUtc(left)));
            return files.Length > 0 ? files[0] : null;
        }
    }
}
