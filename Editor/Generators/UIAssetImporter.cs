using System.Collections.Generic;
using System.IO;
using UnityEditor;
using KK.UI.UMG.Editor.Manifests;
using KK.UI.UMG.Editor.Pipeline;

namespace KK.UI.UMG.Editor.Generators
{
    public sealed class UIAssetImporter
    {
        public IReadOnlyList<string> Import(KKUIPipelineContext context)
        {
            var imported = new List<string>();
            foreach (var asset in context.Assets.Assets ?? new List<UiAssetSpec>())
            {
                var source = AssetManifestUtility.NormalizeAssetPath(asset.Source);
                if (AssetManifestUtility.IsSharedAsset(context, source))
                {
                    continue;
                }

                var target = AssetManifestUtility.ResolveTargetAssetPath(context, asset);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(source, target, true);
                AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                ConfigureImportedAsset(asset, target);
                imported.Add(target);
            }

            return imported;
        }

        private static void ConfigureImportedAsset(UiAssetSpec asset, string target)
        {
            if (asset.Type != "Sprite")
            {
                return;
            }

            var importer = AssetImporter.GetAtPath(target) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(target) as TextureImporter;
            }

            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }
    }
}
