using System;
using System.IO;
using UnityEngine;
using KK.UI.UMG.Editor.Manifests;

namespace KK.UI.UMG.Editor.Pipeline
{
    internal static class AssetManifestUtility
    {
        public static string NormalizeAssetPath(string path)
        {
            return (path ?? string.Empty).Replace('\\', '/').TrimEnd('/');
        }

        public static string ToAssetPath(string fullPath)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(fullPath).Replace(projectRoot + Path.DirectorySeparatorChar, string.Empty).Replace('\\', '/');
        }

        public static string ToFullPath(string assetPath)
        {
            return Path.GetFullPath(NormalizeAssetPath(assetPath));
        }

        public static bool IsUnderAssetPath(string assetPath, string rootAssetPath)
        {
            var path = NormalizeAssetPath(assetPath);
            var root = NormalizeAssetPath(rootAssetPath);
            return string.Equals(path, root, StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase);
        }

        public static string SourceAssetsRoot(KKUIPipelineContext context)
        {
            return ToAssetPath(Path.Combine(context.SourceRoot, "Assets"));
        }

        public static string GeneratedAssetsRoot(KKUIPipelineContext context)
        {
            return ToAssetPath(Path.Combine(context.GeneratedRoot, "Assets"));
        }

        public static bool IsSharedAsset(KKUIPipelineContext context, string assetPath)
        {
            var normalized = NormalizeAssetPath(assetPath);
            foreach (var root in context.Package.SharedAssetRoots ?? new System.Collections.Generic.List<string>())
            {
                if (IsUnderAssetPath(normalized, root))
                {
                    return true;
                }
            }

            return false;
        }

        public static string RuntimeAssetPath(KKUIPipelineContext context, UiAssetSpec asset)
        {
            if (asset == null)
            {
                return null;
            }

            var source = NormalizeAssetPath(asset.Source);
            if (IsSharedAsset(context, source))
            {
                return source;
            }

            return ResolveTargetAssetPath(context, asset);
        }

        public static string ResolveTargetAssetPath(KKUIPipelineContext context, UiAssetSpec asset)
        {
            if (asset == null || string.IsNullOrWhiteSpace(asset.Target))
            {
                return null;
            }

            var target = NormalizeAssetPath(asset.Target);
            if (target.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                return target;
            }

            return ToAssetPath(Path.Combine(context.SourceRoot, target));
        }
    }
}
