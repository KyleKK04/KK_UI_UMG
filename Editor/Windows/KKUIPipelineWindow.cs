using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using KK.UI.UMG.Editor.Pipeline;

namespace KK.UI.UMG.Editor.Windows
{
    public sealed class KKUIPipelineWindow : EditorWindow
    {
        private string _packageManifestPath = string.Empty;
        private TextAsset _packageManifestAsset;
        private Vector2 _scroll;
        private KKUIPipelineResult _lastResult;
        private string _selectionError;
        private Texture2D _previewTexture;
        private PrefabPreviewStatus _previewStatus = PrefabPreviewStatus.NoPreview;
        private string _previewError;
        private string _previewPrefabPath;
        private string _previewPackagePath;

        [MenuItem("KK_UI_UMG/KKPipeline", priority = 0)]
        public static void Open()
        {
            GetWindow<KKUIPipelineWindow>("KKPipeline");
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("KKPipeline");
            SelectInitialManifestPath();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Package Manifest", EditorStyles.boldLabel);
            DrawManifestSelector();

            if (!string.IsNullOrWhiteSpace(_selectionError))
            {
                EditorGUILayout.HelpBox(_selectionError, MessageType.Warning);
            }

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!IsManifestPathRunnable()))
            {
                if (GUILayout.Button("Validate"))
                {
                    if (!ValidateBeforeRun())
                    {
                        return;
                    }
                    _lastResult = new KKUIPipeline().ValidateOnly(_packageManifestPath);
                    MarkPreviewStale();
                }

                if (GUILayout.Button("Generate"))
                {
                    if (!ValidateBeforeRun())
                    {
                        return;
                    }
                    _lastResult = new KKUIPipeline().Run(_packageManifestPath);
                    MarkPreviewStale();
                }

                if (GUILayout.Button("Verify"))
                {
                    if (!ValidateBeforeRun())
                    {
                        return;
                    }
                    _lastResult = new KKUIPipeline().VerifyOnly(_packageManifestPath);
                    if (_lastResult.Success)
                    {
                        RenderPreview();
                    }
                    else
                    {
                        MarkPreviewStale();
                    }
                }

                if (GUILayout.Button("Refresh Preview"))
                {
                    if (!ValidateBeforeRun())
                    {
                        return;
                    }
                    RenderPreview();
                }
            }

            if (GUILayout.Button("Open Report"))
            {
                OpenReport();
            }
            EditorGUILayout.EndHorizontal();

            DrawPreviewPanel();

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(180f));
            if (_lastResult != null)
            {
                EditorGUILayout.LabelField($"{_lastResult.Operation} Status: {_lastResult.Status}  Success: {_lastResult.Success}", EditorStyles.boldLabel);
                if (!string.IsNullOrWhiteSpace(_lastResult.Error))
                {
                    EditorGUILayout.HelpBox(_lastResult.Error, MessageType.Error);
                }

                foreach (var issue in _lastResult.Issues ?? Enumerable.Empty<KKUIPipelineIssue>())
                {
                    var type = issue.Severity == KKUIPipelineIssueSeverity.Error ? MessageType.Error :
                        issue.Severity == KKUIPipelineIssueSeverity.Warning ? MessageType.Warning : MessageType.Info;
                    EditorGUILayout.HelpBox($"{issue.Code}: {issue.Message}", type);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void OnDisable()
        {
            DestroyPreviewTexture();
        }

        private void DrawManifestSelector()
        {
            EditorGUI.BeginChangeCheck();
            var selectedAsset = (TextAsset)EditorGUILayout.ObjectField("Asset", _packageManifestAsset, typeof(TextAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                SetManifestAsset(selectedAsset);
            }

            EditorGUI.BeginChangeCheck();
            var typedPath = EditorGUILayout.TextField("Path", _packageManifestPath);
            if (EditorGUI.EndChangeCheck())
            {
                SetManifestPath(typedPath);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Browse"))
            {
                BrowseManifest();
            }

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_packageManifestPath)))
            {
                if (GUILayout.Button("Ping"))
                {
                    PingManifest();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void SetManifestAsset(TextAsset asset)
        {
            _packageManifestAsset = asset;
            if (asset == null)
            {
                _selectionError = null;
                return;
            }

            SetManifestPath(AssetDatabase.GetAssetPath(asset));
        }

        private void SetManifestPath(string path)
        {
            _packageManifestPath = NormalizeAssetPath(path);
            _packageManifestAsset = string.IsNullOrWhiteSpace(_packageManifestPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<TextAsset>(_packageManifestPath);
            _selectionError = ValidateManifestPath(_packageManifestPath);
            MarkPreviewStale();
        }

        private void BrowseManifest()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            var sourceRoot = Path.Combine(Application.dataPath, "UI", "Source");
            var startDirectory = Directory.Exists(sourceRoot) ? sourceRoot : Application.dataPath;
            var selected = EditorUtility.OpenFilePanel("Select UI package.json", startDirectory, "json");
            if (string.IsNullOrWhiteSpace(selected))
            {
                return;
            }

            if (projectRoot == null || !selected.StartsWith(projectRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                _selectionError = "Package manifest must be inside this Unity project.";
                return;
            }

            SetManifestPath(selected.Substring(projectRoot.Length + 1));
        }

        private void PingManifest()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(_packageManifestPath);
            if (asset == null)
            {
                _selectionError = $"Package manifest does not exist: {_packageManifestPath}";
                return;
            }

            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
        }

        private void OpenReport()
        {
            var reportPath = ResolveReportPath();
            if (string.IsNullOrWhiteSpace(reportPath))
            {
                return;
            }

            if (!File.Exists(reportPath))
            {
                _selectionError = $"Report does not exist yet: {reportPath}";
                return;
            }

            EditorUtility.RevealInFinder(reportPath);
        }

        private string ResolveReportPath()
        {
            var pathError = ValidateManifestPath(_packageManifestPath);
            if (!string.IsNullOrWhiteSpace(pathError))
            {
                _selectionError = pathError;
                return null;
            }

            try
            {
                var context = KKUIPipelineContext.Load(_packageManifestPath);
                return Path.Combine(context.GeneratedRoot, "Reports", "generate-report.json");
            }
            catch (Exception ex)
            {
                _selectionError = $"Cannot resolve report path: {ex.Message}";
                return null;
            }
        }

        private bool ValidateBeforeRun()
        {
            _selectionError = ValidateManifestPath(_packageManifestPath);
            return string.IsNullOrWhiteSpace(_selectionError);
        }

        private bool IsManifestPathRunnable()
        {
            return string.IsNullOrWhiteSpace(ValidateManifestPath(_packageManifestPath));
        }

        private static string ValidateManifestPath(string path)
        {
            var normalized = NormalizeAssetPath(path);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return "Select a package.json manifest under Assets/UI/Source.";
            }

            if (!normalized.StartsWith("Assets/", StringComparison.Ordinal))
            {
                return "Package manifest must be an Assets-relative path.";
            }

            if (!string.Equals(Path.GetFileName(normalized), "package.json", StringComparison.Ordinal))
            {
                return "Package manifest must be named package.json.";
            }

            if (!File.Exists(normalized))
            {
                return $"Package manifest does not exist: {normalized}";
            }

            return null;
        }

        private static string NormalizeAssetPath(string path)
        {
            return (path ?? string.Empty).Replace('\\', '/').Trim();
        }

        private void SelectInitialManifestPath()
        {
            if (string.IsNullOrWhiteSpace(_packageManifestPath))
            {
                SetManifestPath(FindFirstPackageManifestPath());
                return;
            }

            if (string.IsNullOrWhiteSpace(ValidateManifestPath(_packageManifestPath)))
            {
                SetManifestPath(_packageManifestPath);
                return;
            }

            SetManifestPath(FindFirstPackageManifestPath());
        }

        private static string FindFirstPackageManifestPath()
        {
            const string sourceRoot = "Assets/UI/Source";
            if (!Directory.Exists(sourceRoot))
            {
                return string.Empty;
            }

            return Directory
                .GetFiles(sourceRoot, "package.json", SearchOption.AllDirectories)
                .Select(NormalizeAssetPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .FirstOrDefault() ?? string.Empty;
        }

        private void RenderPreview()
        {
            var result = new PrefabPreviewRenderer().Render(_packageManifestPath);
            if (result.Success)
            {
                ReplacePreviewTexture(result.Texture);
                _previewStatus = PrefabPreviewStatus.PreviewReady;
                _previewError = null;
                _previewPrefabPath = result.PrefabPath;
                _previewPackagePath = _packageManifestPath;
                WritePreviewLedger(true, "-");
                Repaint();
                return;
            }

            _previewStatus = result.Status == PrefabPreviewStatus.NoPackage
                ? PrefabPreviewStatus.NoPackage
                : PrefabPreviewStatus.PreviewFailed;
            _previewError = result.Error;
            _previewPrefabPath = result.PrefabPath;
            WritePreviewLedger(false, result.Error);
            Repaint();
        }

        private void WritePreviewLedger(bool success, string note)
        {
            try
            {
                var context = KKUIPipelineContext.Load(_packageManifestPath);
                new ValidationLedgerWriter().WritePreviewResult(context, success, note);
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                _selectionError = $"Cannot update validation.md preview ledger: {ex.Message}";
            }
        }

        private void MarkPreviewStale()
        {
            if (_previewTexture == null)
            {
                _previewStatus = string.IsNullOrWhiteSpace(_packageManifestPath)
                    ? PrefabPreviewStatus.NoPackage
                    : PrefabPreviewStatus.NoPreview;
                _previewError = null;
                return;
            }

            if (!string.Equals(_previewPackagePath, _packageManifestPath, StringComparison.Ordinal))
            {
                _previewPrefabPath = null;
            }

            _previewStatus = PrefabPreviewStatus.PreviewStale;
            _previewError = "Preview may be stale. Click Refresh Preview or run Verify again.";
        }

        private void DrawPreviewPanel()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Status", GetPreviewStatusLabel());

            if (!string.IsNullOrWhiteSpace(_previewPrefabPath))
            {
                EditorGUILayout.LabelField("Prefab", _previewPrefabPath);
            }

            if (!string.IsNullOrWhiteSpace(_previewError))
            {
                var messageType = _previewStatus == PrefabPreviewStatus.PreviewFailed ? MessageType.Error : MessageType.Warning;
                EditorGUILayout.HelpBox(_previewError, messageType);
            }

            if (_previewTexture == null)
            {
                if (_previewStatus == PrefabPreviewStatus.PreviewFailed)
                {
                    return;
                }

                EditorGUILayout.HelpBox("No Preview", MessageType.Info);
                return;
            }

            var aspect = _previewTexture.width > 0 && _previewTexture.height > 0
                ? (float)_previewTexture.width / _previewTexture.height
                : 16f / 9f;
            var maxHeight = 360f;
            var rect = GUILayoutUtility.GetRect(0f, maxHeight, GUILayout.ExpandWidth(true), GUILayout.Height(maxHeight));
            var imageRect = FitAspect(rect, aspect);
            EditorGUI.DrawPreviewTexture(imageRect, _previewTexture, null, ScaleMode.ScaleToFit);
        }

        private string GetPreviewStatusLabel()
        {
            if (!IsManifestPathRunnable())
            {
                return "No Package";
            }

            return _previewStatus switch
            {
                PrefabPreviewStatus.NoPackage => "No Package",
                PrefabPreviewStatus.PreviewReady => "Preview Ready",
                PrefabPreviewStatus.PreviewStale => "Preview Stale",
                PrefabPreviewStatus.PreviewFailed => "Preview Failed",
                _ => "No Preview"
            };
        }

        private void ReplacePreviewTexture(Texture2D texture)
        {
            DestroyPreviewTexture();
            _previewTexture = texture;
        }

        private void DestroyPreviewTexture()
        {
            if (_previewTexture == null)
            {
                return;
            }

            UnityEngine.Object.DestroyImmediate(_previewTexture);
            _previewTexture = null;
        }

        private static Rect FitAspect(Rect rect, float aspect)
        {
            if (aspect <= 0f || rect.width <= 0f || rect.height <= 0f)
            {
                return rect;
            }

            var width = rect.width;
            var height = width / aspect;
            if (height > rect.height)
            {
                height = rect.height;
                width = height * aspect;
            }

            return new Rect(
                rect.x + (rect.width - width) * 0.5f,
                rect.y + (rect.height - height) * 0.5f,
                width,
                height);
        }
    }
}
