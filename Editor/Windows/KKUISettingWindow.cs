using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace KK.UI.UMG.Editor.Windows
{
    public sealed class KKUISettingWindow : EditorWindow
    {
        private const string PackageManifestPath = "Packages/com.kk.ui-umg/package.json";
        private const string InstallScriptRelativePath = "CodexSkills/kk-ui-umg/scripts/install_skill.py";
        private string _statusMessage;
        private MessageType _statusType = MessageType.Info;

        [MenuItem("KK_UI_UMG/Setting", priority = 10)]
        public static void Open()
        {
            var window = GetWindow<KKUISettingWindow>("Setting");
            window.minSize = new Vector2(460f, 190f);
            window.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Setting");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("KK_UI_UMG", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Install the Codex skill before AI authoring.\nSkill: kk-ui-umg\nRuntime: add a scene GameObject with KK.UI.UMG.UIManager.", MessageType.Info);
            EditorGUILayout.Space(6f);

            if (GUILayout.Button("Install Codex Skill"))
            {
                InstallSkill();
            }

            if (GUILayout.Button("Open KKPipeline"))
            {
                KKUIPipelineWindow.Open();
            }

            if (!string.IsNullOrWhiteSpace(_statusMessage))
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.HelpBox(_statusMessage, _statusType);
            }
        }

        private void InstallSkill()
        {
            var scriptPath = ResolveInstallScriptPath();
            if (string.IsNullOrWhiteSpace(scriptPath) || !File.Exists(scriptPath))
            {
                SetStatus($"Install script was not found: {scriptPath}", MessageType.Error);
                return;
            }

            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "python3",
                    Arguments = Quote(scriptPath),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = System.Diagnostics.Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        SetStatus("Could not start python3.", MessageType.Error);
                        return;
                    }

                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        SetStatus(string.IsNullOrWhiteSpace(error) ? $"Skill install failed with exit code {process.ExitCode}." : error.Trim(), MessageType.Error);
                        return;
                    }

                    SetStatus(string.IsNullOrWhiteSpace(output) ? "Skill installed." : output.Trim(), MessageType.Info);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Skill install failed: {ex.Message}", MessageType.Error);
            }
        }

        private static string ResolveInstallScriptPath()
        {
            var packageRoot = ResolvePackageRoot();
            return string.IsNullOrWhiteSpace(packageRoot)
                ? null
                : Path.Combine(packageRoot, InstallScriptRelativePath);
        }

        private static string ResolvePackageRoot()
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(PackageManifestPath);
            if (packageInfo != null && !string.IsNullOrWhiteSpace(packageInfo.resolvedPath))
            {
                return packageInfo.resolvedPath;
            }

            var embeddedPath = Path.GetFullPath("Packages/com.kk.ui-umg");
            return Directory.Exists(embeddedPath) ? embeddedPath : null;
        }

        private void SetStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusType = type;
            if (type == MessageType.Error)
            {
                Debug.LogError($"[KK_UI_UMG] {message}");
            }
            else
            {
                Debug.Log($"[KK_UI_UMG] {message}");
            }
        }

        private static string Quote(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\"";
        }
    }

    [InitializeOnLoad]
    internal static class KKUISettingLauncher
    {
        private const string SessionKey = "KK_UI_UMG.Setting.AutoOpenScheduled";

        static KKUISettingLauncher()
        {
            if (Application.isBatchMode || SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += OpenOncePerProject;
        }

        private static void OpenOncePerProject()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += OpenOncePerProject;
                return;
            }

            var key = GetShownKey();
            if (EditorPrefs.GetBool(key, false))
            {
                return;
            }

            EditorPrefs.SetBool(key, true);
            KKUISettingWindow.Open();
        }

        private static string GetShownKey()
        {
            return "KK_UI_UMG.Setting.Shown." + Hash(Application.dataPath);
        }

        private static string Hash(string value)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                var builder = new StringBuilder(16);
                for (var i = 0; i < 8 && i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}
