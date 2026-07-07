using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using KK.UI.UMG.Editor.Manifests;
using KK.UI.UMG.Editor.Pipeline;

namespace KK.UI.UMG.Editor.Validators
{
    public sealed class AssetValidator : IManifestValidator
    {
        private static readonly HashSet<string> SupportedTypes = new HashSet<string> { "Sprite", "Texture", "TMP_FontAsset" };

        public void Validate(KKUIPipelineContext context)
        {
            var ids = new HashSet<string>();
            var byId = new Dictionary<string, UiAssetSpec>();

            foreach (var asset in context.Assets.Assets ?? new List<UiAssetSpec>())
            {
                ValidateAsset(context, asset, ids);
                if (!string.IsNullOrWhiteSpace(asset.Id))
                {
                    byId[asset.Id] = asset;
                }
            }

            ValidatorUtility.Walk(context.Layout.Root, node =>
            {
                if (node.Type == "Text" && node.Text != null && !string.IsNullOrWhiteSpace(node.Text.FontAsset))
                {
                    RequireAssetId(context, byId, node.Text.FontAsset, "TMP_FontAsset", $"Text node '{node.Id}' fontAsset");
                }

                if ((node.Type == "Image" || node.Type == "Panel" || node.Type == "Button") && node.Image != null && !string.IsNullOrWhiteSpace(node.Image.Sprite))
                {
                    RequireAssetId(context, byId, node.Image.Sprite, "Sprite", $"Image node '{node.Id}' sprite");
                }

                if (node.Type == "RawImage" && node.RawImage != null && !string.IsNullOrWhiteSpace(node.RawImage.Texture))
                {
                    RequireAssetId(context, byId, node.RawImage.Texture, "Texture", $"RawImage node '{node.Id}' texture");
                }
            });
        }

        private static void ValidateAsset(KKUIPipelineContext context, UiAssetSpec asset, HashSet<string> ids)
        {
            if (string.IsNullOrWhiteSpace(asset.Id))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "AST001", "Asset id is required.");
            }
            else if (!ids.Add(asset.Id))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "AST002", $"Duplicate asset id '{asset.Id}'.");
            }

            if (!SupportedTypes.Contains(asset.Type))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "AST003", $"Unsupported asset type '{asset.Type}' for '{asset.Id}'.");
            }

            var source = AssetManifestUtility.NormalizeAssetPath(asset.Source);
            if (string.IsNullOrWhiteSpace(source) || !IsUnityAssetPath(source) || !File.Exists(source))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "AST004", $"Asset '{asset.Id}' source '{asset.Source}' must be an existing Assets/ or Packages/ file.");
                return;
            }

            var isSourceAsset = AssetManifestUtility.IsUnderAssetPath(source, AssetManifestUtility.SourceAssetsRoot(context));
            var isSharedAsset = AssetManifestUtility.IsSharedAsset(context, source);
            if (!isSourceAsset && !isSharedAsset)
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "AST005", $"Asset '{asset.Id}' source must be under Source/Assets or package sharedAssetRoots.");
            }

            if (isSourceAsset)
            {
                var target = AssetManifestUtility.ResolveTargetAssetPath(context, asset);
                var generatedAssetsRoot = AssetManifestUtility.GeneratedAssetsRoot(context);
                if (string.IsNullOrWhiteSpace(target) || !AssetManifestUtility.IsUnderAssetPath(target, generatedAssetsRoot))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "AST006", $"Asset '{asset.Id}' target must resolve under {generatedAssetsRoot}.");
                }
            }

            if (isSharedAsset && !string.IsNullOrWhiteSpace(asset.Target))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "AST007", $"Shared asset '{asset.Id}' must not define target.");
            }

            ValidateHash(context, asset, source);
        }

        private static bool IsUnityAssetPath(string path)
        {
            return path.StartsWith("Assets/") || path.StartsWith("Packages/");
        }

        private static void RequireAssetId(KKUIPipelineContext context, Dictionary<string, UiAssetSpec> byId, string id, string expectedType, string label)
        {
            if (!byId.TryGetValue(id, out var asset))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "AST010", $"{label} references undeclared asset id '{id}'.");
                return;
            }

            if (asset.Type != expectedType)
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "AST011", $"{label} references asset '{id}' with type '{asset.Type}', expected '{expectedType}'.");
            }
        }

        private static void ValidateHash(KKUIPipelineContext context, UiAssetSpec asset, string source)
        {
            var actual = ComputeSha256(source);
            if (string.IsNullOrWhiteSpace(asset.ContentHash))
            {
                context.Add(KKUIPipelineIssueSeverity.Warning, "AST008", $"Asset '{asset.Id}' contentHash is missing. Actual hash is sha256:{actual}.");
                return;
            }

            var expected = asset.ContentHash.StartsWith("sha256:") ? asset.ContentHash.Substring("sha256:".Length) : null;
            if (string.IsNullOrWhiteSpace(expected))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "AST009", $"Asset '{asset.Id}' contentHash must start with sha256:.");
                return;
            }

            if (expected.ToLowerInvariant() != actual)
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "AST012", $"Asset '{asset.Id}' contentHash mismatch. Expected {asset.ContentHash}, actual sha256:{actual}.");
            }
        }

        private static string ComputeSha256(string assetPath)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(assetPath))
            {
                return string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("x2")));
            }
        }
    }
}
