using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace KK.UI.UMG.Editor.Pipeline
{
    public static class KKUIPipelineCli
    {
        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            var packageIndex = Array.IndexOf(args, "--package");
            if (packageIndex < 0 || packageIndex + 1 >= args.Length)
            {
                Debug.LogError("Missing --package <path> argument.");
                EditorApplication.Exit(1);
                return;
            }

            var generatedParentIndex = Array.IndexOf(args, "--generated-parent");
            var generatedParentPath = generatedParentIndex >= 0 && generatedParentIndex + 1 < args.Length
                ? args[generatedParentIndex + 1]
                : null;
            var result = new KKUIPipeline().Run(args[packageIndex + 1], generatedParentPath);
            if (!result.Success)
            {
                Debug.LogError(result.Error ?? string.Join("\n", result.Issues.Select(issue => $"{issue.Severity} {issue.Code}: {issue.Message}")));
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("KK_UI_UMG completed.");
        }
    }
}
