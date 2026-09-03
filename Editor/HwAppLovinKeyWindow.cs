using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Jlyt.HwAds.Editor
{
    /// <summary>
    /// Per-project editor: sets the AppLovin SDK / Quality-service key inside
    /// Assets/Plugins/Android/launcherTemplate.gradle  (applovin { apiKey "..." }).
    /// Each app owns its own key (AppLovin MAX dashboard); this file is per-project config.
    /// </summary>
    public class HwAppLovinKeyWindow : EditorWindow
    {
        const string GradlePath = "Assets/Plugins/Android/launcherTemplate.gradle";

        string _value = "";
        string _message;

        public static void Open()
        {
            var win = GetWindow<HwAppLovinKeyWindow>(true, "设置 AppLovin Key", true);
            win.minSize = new Vector2(520, 200);
            win._value = ReadCurrentValue();
        }

        /// <summary>Returns the raw apiKey currently written in the launcher template ("" if absent).</summary>
        public static string ReadCurrentValue()
        {
            if (!File.Exists(GradlePath))
            {
                return "";
            }

            try
            {
                string text = File.ReadAllText(GradlePath);
                var m = Regex.Match(text, @"apiKey\s*""([^""]+)""");
                return m.Success ? m.Groups[1].Value : "";
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[Jlyt.HwAds] 读取 AppLovin Key 失败：" + e.Message);
                return "";
            }
        }

        public static string Masked(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return "(未设置)";
            }

            if (key.Length <= 12)
            {
                return "•••••" + key;
            }

            return key.Substring(0, 6) + "••••••••" + key.Substring(key.Length - 4);
        }

        void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("每工程配置：AppLovin SDK Key", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "写入 Assets/Plugins/Android/launcherTemplate.gradle 的 applovin { apiKey \"…\" }。" +
                "Key 从 AppLovin MAX 后台（MAX → Keys → SDK Key / Quality 用同一 key）获取，每款应用各不相同。",
                EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("当前值：" + Masked(_value));
            EditorGUILayout.Space(6);

            _value = EditorGUILayout.TextField("新 AppLovin Key：", _value);
            EditorGUILayout.Space(8);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("写入并保存", GUILayout.Height(28)))
                {
                    _message = Apply();
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
            if (string.IsNullOrWhiteSpace(_value) || _value.Contains("...") || _value.Contains("…"))
            {
                return "请输入有效的 Key（不能为空或含占位符）。";
            }

            if (!File.Exists(GradlePath))
            {
                return "找不到 " + GradlePath + "，请先确认 Android 构建配置存在。";
            }

            try
            {
                string text = File.ReadAllText(GradlePath);
                string updated;
                if (Regex.IsMatch(text, @"apiKey\s*""[^""]*"""))
                {
                    updated = Regex.Replace(text, @"apiKey\s*""[^""]*""", "apiKey \"" + _value + "\"");
                }
                else if (text.Contains("applovin {"))
                {
                    updated = text.Replace("applovin {", "applovin {\n    apiKey \"" + _value + "\"");
                }
                else
                {
                    return "launcherTemplate.gradle 中未找到 applovin 配置块，请确认该模板包含 AppLovin Quality 插件配置。";
                }

                File.WriteAllText(GradlePath, updated);
                AssetDatabase.ImportAsset(GradlePath, ImportAssetOptions.ForceUpdate);
                return "已写入 AppLovin Key（" + Masked(_value) + "）。";
            }
            catch (Exception e)
            {
                return "写入失败：" + e.Message;
            }
        }
    }
}
