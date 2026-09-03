using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Jlyt.HwAds.Editor
{
    /// <summary>
    /// Small per-project editor: sets the AdMob / Ad Manager APP_ID meta-data value inside
    /// Assets/Plugins/Android/AndroidManifest.xml. Each game/app owns its own ID — this file is
    /// per-project configuration and never stored in the module.
    /// </summary>
    public class HwAdmobIdWindow : EditorWindow
    {
        const string ManifestPath = "Assets/Plugins/Android/AndroidManifest.xml";
        const string Key = "com.google.android.gms.ads.APPLICATION_ID";

        string _value = "";
        string _message;

        public static void Open()
        {
            var win = GetWindow<HwAdmobIdWindow>(true, "设置 AdMob App ID", true);
            win.minSize = new Vector2(480, 190);
            win._value = ReadCurrentValue();
        }

        public static string ReadCurrentValue()
        {
            if (!File.Exists(ManifestPath))
            {
                return "";
            }

            try
            {
                foreach (var line in File.ReadAllLines(ManifestPath))
                {
                    if (line.Contains(Key))
                    {
                        var m = Regex.Match(line, "android:value=\"([^\"]+)\"");
                        if (m.Success)
                        {
                            return m.Groups[1].Value;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[Jlyt.HwAds] 读取 APP_ID 失败：" + e.Message);
            }

            return "";
        }

        void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("每工程配置：AdMob / Ad Manager App ID", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "对应 AndroidManifest.xml 中的 " + Key + "。ID 从 AdMob 控制台(应用→App ID)或 Ad Manager 获取，格式 ca-app-pub-XXXXXXXXXXXXXXXX~YYYYYYYYYY，每个游戏各不相同。",
                EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("当前值：");
            EditorGUILayout.LabelField(string.IsNullOrEmpty(_value) ? "(未找到)" : _value, EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(6);

            _value = EditorGUILayout.TextField("新 App ID：", _value);
            EditorGUILayout.Space(8);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("写入并保存", GUILayout.Height(28)))
                {
                    _message = Apply();
                    EditorGUILayout.LabelField(_message, new GUIStyle(EditorStyles.label) { wordWrap = true });
                }

                if (GUILayout.Button("关闭", GUILayout.Height(28)))
                {
                    Close();
                }
            }

            if (!string.IsNullOrEmpty(_message))
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField(_message, new GUIStyle(EditorStyles.label) { wordWrap = true });
            }
        }

        string Apply()
        {
            if (string.IsNullOrWhiteSpace(_value) || _value.Contains("..."))
            {
                return "请输入有效的 App ID（不能为空或含占位符 …）。";
            }

            if (!File.Exists(ManifestPath))
            {
                return "找不到 " + ManifestPath + "，请确认已导出 Android 配置模板。";
            }

            try
            {
                var lines = File.ReadAllLines(ManifestPath);
                bool changed = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains(Key))
                    {
                        lines[i] = Regex.Replace(lines[i], "android:value=\"[^\"]*\"", "android:value=\"" + _value + "\"");
                        changed = true;
                        break;
                    }
                }

                if (!changed)
                {
                    return "AndroidManifest.xml 中未找到 " + Key + " 条目，请先手动补上该 meta-data。";
                }

                File.WriteAllLines(ManifestPath, lines);
                AssetDatabase.ImportAsset(ManifestPath, ImportAssetOptions.ForceUpdate);
                return "已写入：" + _value;
            }
            catch (Exception e)
            {
                return "写入失败：" + e.Message;
            }
        }
    }
}
