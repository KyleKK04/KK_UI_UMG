using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using KK.UI.UMG;
using KK.UI.UMG.Components;

namespace KK.UI.UMG.Editor.Pipeline
{
    public enum PrefabPreviewStatus
    {
        NoPackage,
        NoPreview,
        PreviewReady,
        PreviewStale,
        PreviewFailed
    }

    public sealed class PrefabPreviewResult
    {
        public PrefabPreviewStatus Status { get; set; }
        public Texture2D Texture { get; set; }
        public string Error { get; set; }
        public string PrefabPath { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int DisabledRuntimeScriptCount { get; set; }

        public bool Success => Status == PrefabPreviewStatus.PreviewReady && Texture != null;
    }

    public sealed class PrefabPreviewRenderer
    {
        private const int DefaultWidth = 1920;
        private const int DefaultHeight = 1080;
        private const int PreviewLayer = 31;
        private static readonly Color PreviewBackground = new Color(0.08f, 0.1f, 0.14f, 1f);
        private static readonly Vector3 PreviewOrigin = new Vector3(100000f, 100000f, 0f);

        public PrefabPreviewResult Render(string packageManifestPath)
        {
            if (string.IsNullOrWhiteSpace(packageManifestPath))
            {
                return Failed(PrefabPreviewStatus.NoPackage, null, "No package manifest selected.");
            }

            try
            {
                return Render(KKUIPipelineContext.Load(packageManifestPath));
            }
            catch (Exception ex)
            {
                return Failed(PrefabPreviewStatus.PreviewFailed, null, $"Preview failed: {ex.Message}");
            }
        }

        public PrefabPreviewResult Render(KKUIPipelineContext context)
        {
            if (context == null || context.Package == null)
            {
                return Failed(PrefabPreviewStatus.NoPackage, null, "No package context.");
            }

            var prefabPath = AssetManifestUtility.ToAssetPath(Path.Combine(context.GeneratedRoot, "Prefabs", $"{context.Package.PackageId}View.prefab"));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                return Failed(PrefabPreviewStatus.PreviewFailed, prefabPath, $"Generated prefab does not exist: {prefabPath}");
            }

            var width = context.Package.DesignResolution?.Width > 0 ? context.Package.DesignResolution.Width : DefaultWidth;
            var height = context.Package.DesignResolution?.Height > 0 ? context.Package.DesignResolution.Height : DefaultHeight;
            return RenderPrefab(prefab, prefabPath, width, height);
        }

        private static PrefabPreviewResult RenderPrefab(GameObject prefab, string prefabPath, int width, int height)
        {
            var screenSpaceResult = TryRenderPrefab(prefab, prefabPath, width, height, RenderMode.ScreenSpaceCamera);
            if (screenSpaceResult.Success)
            {
                return screenSpaceResult;
            }

            var worldSpaceResult = TryRenderPrefab(prefab, prefabPath, width, height, RenderMode.WorldSpace);
            if (worldSpaceResult.Success)
            {
                return worldSpaceResult;
            }

            return Failed(
                PrefabPreviewStatus.PreviewFailed,
                prefabPath,
                $"{screenSpaceResult.Error}\nFallback: {worldSpaceResult.Error}");
        }

        private static PrefabPreviewResult TryRenderPrefab(GameObject prefab, string prefabPath, int width, int height, RenderMode renderMode)
        {
            var previousScene = SceneManager.GetActiveScene();
            var previousActive = RenderTexture.active;
            var renderScene = default(Scene);
            var closeRenderScene = false;
            RenderTexture renderTexture = null;
            Camera camera = null;
            Canvas canvas = null;
            GameObject instance = null;

            try
            {
                renderScene = CreateRenderScene(previousScene);
                closeRenderScene = renderScene.IsValid() && renderScene != previousScene;
                SceneManager.SetActiveScene(renderScene);

                renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
                renderTexture.name = $"{prefab.name} Preview RT";
                renderTexture.Create();

                camera = CreatePreviewCamera(renderScene, width, height);
                camera.targetTexture = renderTexture;

                canvas = CreatePreviewCanvas(renderScene, camera, width, height, renderMode);
                instance = InstantiatePrefab(prefab, renderScene);
                if (instance == null)
                {
                    return Failed(PrefabPreviewStatus.PreviewFailed, prefabPath, $"Could not instantiate generated prefab: {prefabPath}");
                }

                var disabledScripts = DisableRuntimeScripts(instance);
                HidePreviewObject(camera.gameObject);
                HidePreviewObject(canvas.gameObject);
                HidePreviewObject(instance);
                instance.transform.SetParent(canvas.transform, false);
                FitRootToPreviewCanvas(instance);
                SetLayerRecursive(canvas.gameObject, PreviewLayer);
                SetLayerRecursive(instance, PreviewLayer);
                instance.SetActive(true);

                RebuildLayout(canvas, instance);
                MarkGraphicsDirty(instance);
                var visibleGraphicCount = CountVisibleGraphics(instance);
                if (visibleGraphicCount == 0)
                {
                    return Failed(
                        PrefabPreviewStatus.PreviewFailed,
                        prefabPath,
                        $"Preview rendered only the camera background in {renderMode}. Visible graphics: 0. Generated prefab has no visible UI graphics.");
                }

                camera.Render();
                Canvas.ForceUpdateCanvases();
                camera.Render();

                RenderTexture.active = renderTexture;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    name = $"{prefab.name} Preview",
                    hideFlags = HideFlags.HideAndDontSave
                };
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();

                if (!HasVisiblePixels(texture, camera.backgroundColor))
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                    return Failed(
                        PrefabPreviewStatus.PreviewFailed,
                        prefabPath,
                        $"Preview rendered only the camera background in {renderMode}. Visible graphics: {visibleGraphicCount}. Generated prefab UI is outside the preview camera or has no visible graphics.");
                }

                return new PrefabPreviewResult
                {
                    Status = PrefabPreviewStatus.PreviewReady,
                    Texture = texture,
                    PrefabPath = prefabPath,
                    Width = width,
                    Height = height,
                    DisabledRuntimeScriptCount = disabledScripts
                };
            }
            catch (Exception ex)
            {
                return Failed(PrefabPreviewStatus.PreviewFailed, prefabPath, $"Preview render failed: {ex.Message}");
            }
            finally
            {
                RenderTexture.active = previousActive;
                if (camera != null)
                {
                    camera.targetTexture = null;
                }

                if (camera != null)
                {
                    UnityEngine.Object.DestroyImmediate(camera.gameObject);
                }

                if (instance != null)
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }

                if (canvas != null)
                {
                    UnityEngine.Object.DestroyImmediate(canvas.gameObject);
                }

                if (previousScene.IsValid() && previousScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousScene);
                }

                if (renderTexture != null)
                {
                    RenderTexture.ReleaseTemporary(renderTexture);
                }

                if (closeRenderScene && renderScene.IsValid() && renderScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(renderScene, true);
                }
            }
        }

        private static Scene CreateRenderScene(Scene fallbackScene)
        {
            try
            {
                return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            }
            catch (InvalidOperationException ex) when (ex.Message.IndexOf("untitled scene unsaved", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return fallbackScene;
            }
        }

        private static void HidePreviewObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            gameObject.hideFlags = HideFlags.HideAndDontSave;
            foreach (Transform child in gameObject.transform)
            {
                HidePreviewObject(child.gameObject);
            }
        }

        private static Camera CreatePreviewCamera(Scene previewScene, int width, int height)
        {
            var gameObject = new GameObject("PreviewCamera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(gameObject, previewScene);
            var camera = gameObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = PreviewBackground;
            camera.orthographic = true;
            camera.orthographicSize = height * 0.5f;
            camera.aspect = width / (float)height;
            camera.rect = new Rect(0f, 0f, 1f, 1f);
            camera.pixelRect = new Rect(0f, 0f, width, height);
            camera.cullingMask = 1 << PreviewLayer;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 5000f;
            camera.transform.position = PreviewOrigin + new Vector3(0f, 0f, -1000f);
            camera.transform.rotation = Quaternion.identity;
            gameObject.layer = PreviewLayer;
            return camera;
        }

        private static Canvas CreatePreviewCanvas(Scene previewScene, Camera camera, int width, int height, RenderMode renderMode)
        {
            var gameObject = new GameObject("PreviewCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(gameObject, previewScene);

            var rect = gameObject.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.position = PreviewOrigin;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;

            var canvas = gameObject.GetComponent<Canvas>();
            canvas.renderMode = renderMode;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10f;
            canvas.pixelPerfect = false;

            var scaler = gameObject.GetComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(width, height);
            if (renderMode == RenderMode.WorldSpace)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            }
            else
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            return canvas;
        }

        private static GameObject InstantiatePrefab(GameObject prefab, Scene scene)
        {
            var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance != null)
            {
                return instance;
            }

            instance = UnityEngine.Object.Instantiate(prefab);
            SceneManager.MoveGameObjectToScene(instance, scene);
            return instance;
        }

        private static void FitRootToPreviewCanvas(GameObject instance)
        {
            if (!instance.TryGetComponent<RectTransform>(out var rect))
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        private static void SetLayerRecursive(GameObject gameObject, int layer)
        {
            if (gameObject == null)
            {
                return;
            }

            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }

        private static int DisableRuntimeScripts(GameObject instance)
        {
            var disabled = 0;
            foreach (var behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null || !ShouldDisableRuntimeScript(behaviour))
                {
                    continue;
                }

                behaviour.enabled = false;
                disabled++;
            }

            return disabled;
        }

        private static bool ShouldDisableRuntimeScript(MonoBehaviour behaviour)
        {
            if (behaviour is UIViewBase)
            {
                return true;
            }

            var type = behaviour.GetType();
            if (type.Name.EndsWith("ControllerProvider", StringComparison.Ordinal))
            {
                return true;
            }

            return !IsPreviewSafeBehaviour(behaviour);
        }

        private static bool IsPreviewSafeBehaviour(MonoBehaviour behaviour)
        {
            return behaviour is CanvasScaler ||
                behaviour is GraphicRaycaster ||
                behaviour is Graphic ||
                behaviour is Selectable ||
                behaviour is ScrollRect ||
                behaviour is Mask ||
                behaviour is RectMask2D ||
                behaviour is LayoutGroup ||
                behaviour is LayoutElement ||
                behaviour is ContentSizeFitter ||
                behaviour is AspectRatioFitter ||
                behaviour is Shadow ||
                behaviour is Outline ||
                behaviour is TMP_Text ||
                behaviour is TMP_InputField ||
                behaviour is TMP_Dropdown ||
                behaviour is UIListView;
        }

        private static void RebuildLayout(Canvas canvas, GameObject instance)
        {
            Canvas.ForceUpdateCanvases();

            if (instance.TryGetComponent<RectTransform>(out var rootRect))
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
            }

            if (canvas.TryGetComponent<RectTransform>(out var canvasRect))
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(canvasRect);
            }

            Canvas.ForceUpdateCanvases();
        }

        private static void MarkGraphicsDirty(GameObject instance)
        {
            foreach (var graphic in instance.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic == null)
                {
                    continue;
                }

                graphic.SetVerticesDirty();
                graphic.SetMaterialDirty();
            }

            foreach (var text in instance.GetComponentsInChildren<TMP_Text>(true))
            {
                text.ForceMeshUpdate(true, true);
            }
        }

        private static bool HasVisiblePixels(Texture2D texture, Color background)
        {
            var background32 = (Color32)background;
            foreach (var pixel in texture.GetPixels32())
            {
                if (Math.Abs(pixel.r - background32.r) > 3 ||
                    Math.Abs(pixel.g - background32.g) > 3 ||
                    Math.Abs(pixel.b - background32.b) > 3)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountVisibleGraphics(GameObject instance)
        {
            var count = 0;
            foreach (var graphic in instance.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic != null && graphic.isActiveAndEnabled && graphic.color.a > 0.001f)
                {
                    count++;
                }
            }

            return count;
        }

        private static PrefabPreviewResult Failed(PrefabPreviewStatus status, string prefabPath, string error)
        {
            return new PrefabPreviewResult
            {
                Status = status,
                Error = error,
                PrefabPath = prefabPath
            };
        }

    }
}
