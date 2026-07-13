using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.UI;
using KK.UI.UMG.Editor.Generators;
using KK.UI.UMG.Editor.Manifests;
using KK.UI.UMG.Internal;

namespace KK.UI.UMG.Editor.Pipeline
{
    public sealed class GeneratedAssetVerifier
    {
        public IReadOnlyList<string> Verify(KKUIPipelineContext context)
        {
            var verified = new List<string>();
            var prefabPath = ToAssetPath(Path.Combine(context.GeneratedRoot, "Prefabs", $"{context.Package.PackageId}View.prefab"));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                var scriptsRoot = Path.Combine(context.GeneratedRoot, "Scripts");
                var viewScript = Path.Combine(scriptsRoot, $"{context.Codegen.View.ClassName}.Generated.cs");
                var message = File.Exists(viewScript)
                    ? $"Generated prefab does not exist: {prefabPath}. Generated scripts exist, so Unity likely needed a compile pass before prefab generation. Wait for compilation to finish, then run Generate again before Verify."
                    : $"Generated prefab does not exist: {prefabPath}. Run Generate before Verify.";
                context.Add(KKUIPipelineIssueSeverity.Error, "VER001", message);
                return verified;
            }

            verified.Add(prefabPath);
            VerifyPrefabRoot(context, prefab, prefabPath);
            VerifyViewFields(context, prefab);
            VerifyControllerRegistration(context);
            verified.AddRange(VerifyGeneratedScripts(context));
            VerifyGeneratedScriptOwnership(context);
            VerifyRuntimeSourceBoundaries(context);
            VerifyTextFonts(context, prefab);
            VerifyTextAlignments(context, prefab);
            VerifyPersistentEvents(prefab, context);
            VerifyPrefabDependencies(context, prefabPath);
            VerifyAddressable(context, prefabPath);
            VerifySourceNotAddressable(context);
            return verified;
        }

        private static void VerifyPrefabRoot(KKUIPipelineContext context, GameObject prefab, string prefabPath)
        {
            if (prefab.GetComponent<GeneratedAssetMarker>() == null)
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "VER002", $"Prefab '{prefabPath}' is missing GeneratedAssetMarker.");
            }

            if (prefab.activeSelf)
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "VER003", $"Prefab '{prefabPath}' root must be inactive.");
            }

            var viewType = FindType(context.Codegen.Namespace, context.Codegen.View.ClassName);
            if (viewType == null || prefab.GetComponent(viewType) == null)
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "VER004", $"Prefab root is missing view component '{context.Codegen.View.ClassName}'.");
            }

            foreach (var component in prefab.GetComponents<Component>())
            {
                if (component != null && component.GetType().Name.EndsWith("ControllerProvider", StringComparison.Ordinal))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "VER005", $"Prefab root must not contain legacy controller provider component '{component.GetType().Name}'.");
                }
            }

            if (prefab.GetComponent<CanvasGroup>() == null)
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "VER017", $"Prefab '{prefabPath}' root must have CanvasGroup.");
            }

            if (prefab.GetComponent<GraphicRaycaster>() == null)
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "VER018", $"Prefab '{prefabPath}' root must have GraphicRaycaster.");
            }
        }

        private static void VerifyControllerRegistration(KKUIPipelineContext context)
        {
            var registrationType = FindType(context.Codegen.Namespace, $"{context.Codegen.Controller.ClassName}Registration");
            if (registrationType == null)
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "VER013", $"Generated controller registration type '{context.Codegen.Controller.ClassName}Registration' is missing or not compiled.");
            }
        }

        private static IReadOnlyList<string> VerifyGeneratedScripts(KKUIPipelineContext context)
        {
            var verified = new List<string>();
            var scripts = ExpectedGeneratedScripts(context);
            foreach (var script in scripts)
            {
                if (!File.Exists(script.Value))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "VER019", $"Generated script '{AssetManifestUtility.ToAssetPath(script.Value)}' is missing.");
                    continue;
                }

                verified.Add(AssetManifestUtility.ToAssetPath(script.Value));
                var firstLine = File.ReadLines(script.Value).FirstOrDefault() ?? string.Empty;
                if (!string.Equals(firstLine, CSharpCodeGenerator.Header, StringComparison.Ordinal))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "VER020", $"Generated script '{AssetManifestUtility.ToAssetPath(script.Value)}' is missing the current generated header.");
                }
            }

            if (TryRead(scripts["registration"], out var registrationSource))
            {
                VerifyNotContains(context, registrationSource, "UIMessageBus.Subscribe", "VER021", "ControllerRegistration.Generated.cs must not subscribe to UIMessageBus directly. UIManager owns inbound bus subscription.");
            }

            if (TryRead(scripts["view"], out var viewSource))
            {
                if (!Contains(viewSource, $"public partial class {context.Codegen.View.ClassName}"))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "VER037", $"View.Generated.cs must declare '{context.Codegen.View.ClassName}' as a partial class.");
                }

                VerifyNotContains(context, viewSource, "Store.", "VER022", "View.Generated.cs must not access ViewModelStore directly.");
                VerifyNotContains(context, viewSource, "ViewModelStore", "VER022", "View.Generated.cs must not access ViewModelStore directly.");
                VerifyNotContains(context, viewSource, "UILocalizationService", "VER023", "View.Generated.cs must not access localization directly.");
                VerifyNotContains(context, viewSource, "UIManager", "VER024", "View.Generated.cs must not access UIManager directly.");
                VerifyNotContains(context, viewSource, "RequireService<", "VER032", "View.Generated.cs must not resolve business services.");
                VerifyNotContains(context, viewSource, "TryGetService<", "VER032", "View.Generated.cs must not resolve business services.");
                foreach (var service in context.Codegen.RequiredServices ?? new List<UiRequiredServiceSpec>())
                {
                    if (!string.IsNullOrWhiteSpace(service.Type))
                    {
                        VerifyNotContains(context, viewSource, service.Type, "VER032", "View.Generated.cs must not reference business service types.");
                    }
                }
            }

            if (TryRead(scripts["controller"], out var controllerSource))
            {
                VerifyNotContains(context, controllerSource, "UILocalizationService.Instance.Resolve", "VER034", "Controller.Generated.cs must not resolve static localization at runtime. Static locKey text is written during prefab generation.");
            }

            return verified;
        }

        private static void VerifyGeneratedScriptOwnership(KKUIPipelineContext context)
        {
            var scriptsRoot = Path.Combine(context.GeneratedRoot, "Scripts");
            if (!Directory.Exists(scriptsRoot))
            {
                return;
            }

            foreach (var path in Directory.GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories))
            {
                var firstLine = File.ReadLines(path).FirstOrDefault() ?? string.Empty;
                if (!string.Equals(firstLine, CSharpCodeGenerator.Header, StringComparison.Ordinal))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "VER036", $"Handwritten script '{AssetManifestUtility.ToAssetPath(path)}' must not be placed in the generated-owned Scripts folder.");
                }
            }
        }

        private static void VerifyRuntimeSourceBoundaries(KKUIPipelineContext context)
        {
            var runtimeRoot = Path.Combine("Packages", "com.kk.ui-umg", "Runtime");
            var binderPath = Path.Combine(runtimeRoot, "Binding", "UguiBinder.cs");
            if (TryRead(binderPath, out var binderSource))
            {
                VerifyNotContains(context, binderSource, "UILocalizationService", "VER025", "UguiBinder must not access localization directly. Static locKey text is resolved during prefab generation; dynamic text comes from Store.");
                VerifyNotContains(context, binderSource, "RequireService<", "VER033", "UguiBinder must not resolve business services.");
                VerifyNotContains(context, binderSource, "TryGetService<", "VER033", "UguiBinder must not resolve business services.");
            }

            if (!Directory.Exists(runtimeRoot))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(runtimeRoot, "*.cs", SearchOption.AllDirectories))
            {
                var source = File.ReadAllText(file);
                if (Contains(source, "Assets/UI/Source") || Contains(source, "strings.json"))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "VER026", $"Runtime source '{AssetManifestUtility.ToAssetPath(file)}' must not read Source json/assets directly.");
                }
            }
        }

        private static void VerifyViewFields(KKUIPipelineContext context, GameObject prefab)
        {
            var viewType = FindType(context.Codegen.Namespace, context.Codegen.View.ClassName);
            if (viewType == null)
            {
                return;
            }

            var view = prefab.GetComponent(viewType);
            if (view == null)
            {
                return;
            }

            Walk(context.Layout.Root, node =>
            {
                if (!IsGeneratedViewReference(context.Layout.Root, node))
                {
                    return;
                }

                var fieldName = ToFieldName(node.Id);
                var field = viewType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field == null)
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "VER006", $"View '{viewType.Name}' is missing serialized field '{fieldName}'.");
                    return;
                }

                if (field.GetValue(view) == null)
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "VER007", $"View field '{fieldName}' is not assigned in prefab.");
                }
            });
        }

        private static void VerifyTextFonts(KKUIPipelineContext context, GameObject prefab)
        {
            foreach (var text in prefab.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (text.font == null)
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "VER008", $"Text '{text.name}' has no TMP font asset.");
                }
            }
        }

        private static void VerifyTextAlignments(KKUIPipelineContext context, GameObject prefab)
        {
            VerifyTextAlignments(context, prefab, context.Layout.Root);
        }

        private static void VerifyTextAlignments(KKUIPipelineContext context, GameObject prefab, UiLayoutNode node)
        {
            if (node == null)
            {
                return;
            }

            if (node.Type == "Text" && node.Text != null)
            {
                var text = prefab.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(component => component.name == node.Id);
                var expected = UguiPrefabGenerator.ParseTextAlignment(node.Text.Alignment);
                if (text != null && text.alignment != expected)
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "VER035", $"Text '{node.Id}' alignment mismatch. Expected '{expected}', got '{text.alignment}'.");
                }
            }

            foreach (var child in node.Children ?? new List<UiLayoutNode>())
            {
                VerifyTextAlignments(context, prefab, child);
            }

            VerifyTextAlignments(context, prefab, node.VerticalList?.ItemTemplate);
        }

        private static void VerifyPersistentEvents(GameObject prefab, KKUIPipelineContext context)
        {
            foreach (var button in prefab.GetComponentsInChildren<Button>(true))
            {
                if (button.onClick.GetPersistentEventCount() > 0)
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "VER027", $"Button '{button.name}' must not have persistent Inspector onClick events.");
                }
            }

            foreach (var toggle in prefab.GetComponentsInChildren<Toggle>(true))
            {
                if (toggle.onValueChanged.GetPersistentEventCount() > 0)
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "VER028", $"Toggle '{toggle.name}' must not have persistent Inspector onValueChanged events.");
                }
            }

            foreach (var slider in prefab.GetComponentsInChildren<Slider>(true))
            {
                if (slider.onValueChanged.GetPersistentEventCount() > 0)
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "VER029", $"Slider '{slider.name}' must not have persistent Inspector onValueChanged events.");
                }
            }

            foreach (var input in prefab.GetComponentsInChildren<TMP_InputField>(true))
            {
                if (input.onValueChanged.GetPersistentEventCount() > 0 || input.onEndEdit.GetPersistentEventCount() > 0)
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "VER030", $"InputField '{input.name}' must not have persistent Inspector events.");
                }
            }

            foreach (var dropdown in prefab.GetComponentsInChildren<TMP_Dropdown>(true))
            {
                if (dropdown.onValueChanged.GetPersistentEventCount() > 0)
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "VER031", $"Dropdown '{dropdown.name}' must not have persistent Inspector onValueChanged events.");
                }
            }
        }

        private static void VerifyPrefabDependencies(KKUIPipelineContext context, string prefabPath)
        {
            foreach (var dependency in AssetDatabase.GetDependencies(prefabPath, true))
            {
                if (string.IsNullOrWhiteSpace(dependency) || dependency.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (dependency == prefabPath || dependency.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (AssetManifestUtility.IsUnderAssetPath(dependency, AssetManifestUtility.ToAssetPath(context.GeneratedRoot)) ||
                    AssetManifestUtility.IsSharedAsset(context, dependency))
                {
                    continue;
                }

                context.Add(KKUIPipelineIssueSeverity.Error, "VER015", $"Prefab dependency '{dependency}' must be under Generated/ or package sharedAssetRoots.");
            }
        }

        private static void VerifyAddressable(KKUIPipelineContext context, string prefabPath)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "VER009", "Addressables settings do not exist.");
                return;
            }

            var guid = AssetDatabase.AssetPathToGUID(prefabPath);
            var entry = settings.FindAssetEntry(guid, true);
            if (entry == null)
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "VER010", $"Prefab '{prefabPath}' is not registered as Addressable.");
                return;
            }

            if (entry.address != context.Codegen.AddressablesKey)
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "VER011", $"Addressable key mismatch. Expected '{context.Codegen.AddressablesKey}', got '{entry.address}'.");
            }

            if (!string.IsNullOrWhiteSpace(context.Package.AddressablesGroup) && entry.parentGroup != null && entry.parentGroup.Name != context.Package.AddressablesGroup)
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "VER014", $"Addressable group mismatch. Expected '{context.Package.AddressablesGroup}', got '{entry.parentGroup.Name}'.");
            }
        }

        private static void VerifySourceNotAddressable(KKUIPipelineContext context)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                return;
            }

            var sourceRoot = ToAssetPath(context.SourceRoot);
            foreach (var assetPath in Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories))
            {
                if (assetPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var normalizedPath = assetPath.Replace('\\', '/');
                var guid = AssetDatabase.AssetPathToGUID(normalizedPath);
                if (string.IsNullOrWhiteSpace(guid))
                {
                    continue;
                }

                var entry = settings.FindAssetEntry(guid, true);
                if (entry != null)
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "VER012", $"Source asset '{normalizedPath}' must not be registered as Addressable. Only Generated runtime assets are allowed in player content.");
                }
            }
        }

        private static Type FindType(string ns, string className)
        {
            var fullName = $"{ns}.{className}";
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static Dictionary<string, string> ExpectedGeneratedScripts(KKUIPipelineContext context)
        {
            var scriptsRoot = Path.Combine(context.GeneratedRoot, "Scripts");
            return new Dictionary<string, string>
            {
                ["view"] = Path.Combine(scriptsRoot, $"{context.Codegen.View.ClassName}.Generated.cs"),
                ["controller"] = Path.Combine(scriptsRoot, $"{context.Codegen.Controller.ClassName}.Generated.cs"),
                ["viewModel"] = Path.Combine(scriptsRoot, $"{context.Codegen.ViewModel.ClassName}.Generated.cs"),
                ["registration"] = Path.Combine(scriptsRoot, $"{context.Codegen.Controller.ClassName}Registration.Generated.cs"),
                ["bus"] = Path.Combine(scriptsRoot, $"{context.Package.PackageId}Bus.Generated.cs"),
                ["strings"] = Path.Combine(scriptsRoot, $"{context.Package.PackageId}Strings.Generated.cs")
            };
        }

        private static bool TryRead(string path, out string source)
        {
            if (!File.Exists(path))
            {
                source = null;
                return false;
            }

            source = File.ReadAllText(path);
            return true;
        }

        private static void VerifyNotContains(KKUIPipelineContext context, string source, string forbidden, string code, string message)
        {
            if (Contains(source, forbidden))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, code, message);
            }
        }

        private static bool Contains(string source, string value)
        {
            return source?.IndexOf(value, StringComparison.Ordinal) >= 0;
        }

        private static void Walk(UiLayoutNode node, Action<UiLayoutNode> visitor)
        {
            if (node == null)
            {
                return;
            }

            visitor(node);
            foreach (var child in node.Children ?? new List<UiLayoutNode>())
            {
                Walk(child, visitor);
            }
        }

        private static string ToAssetPath(string fullPath)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(fullPath).Replace(projectRoot + Path.DirectorySeparatorChar, string.Empty).Replace('\\', '/');
        }

        private static string ToFieldName(string id)
        {
            var pascal = string.Concat(id.Split('_', '-', ' ').Where(part => part.Length > 0).Select(part => char.ToUpperInvariant(part[0]) + part.Substring(1)));
            return "_" + char.ToLowerInvariant(pascal[0]) + pascal.Substring(1);
        }

        private static bool IsBindableControl(string type)
        {
            return type == "Text" ||
                type == "Image" ||
                type == "Button" ||
                type == "RawImage" ||
                type == "Toggle" ||
                type == "Slider" ||
                type == "InputField" ||
                type == "Dropdown" ||
                type == "Scrollbar" ||
                type == "ScrollView" ||
                type == "VerticalList";
        }

        private static bool IsGeneratedViewReference(UiLayoutNode root, UiLayoutNode node)
        {
            return IsBindableControl(node.Type) || (!ReferenceEquals(root, node) && node.Type == "Panel");
        }
    }
}
