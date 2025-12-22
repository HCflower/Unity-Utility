using System.Collections.Generic;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine;
using TMPro;

namespace FFramework.Tools
{
    /// <summary>
    /// 一键替换场景中所有 Text 和 TextMeshPro 字体的工具窗口
    /// </summary>
    public class OneClickReplacementAllFontInScene : EditorWindow
    {
        // 目标字体（用于Unity原生Text组件）
        private Font targetFont;
        // 目标TMP字体（用于TextMeshPro组件）
        private TMP_FontAsset targetTMPFont;
        // 是否包含未激活的对象
        private bool includeInactiveObjects = true;
        // 滚动视图位置
        private Vector2 scrollPosition;

        // GUI样式
        private GUIStyle headerStyle;
        private GUIStyle sectionStyle;
        private GUIStyle buttonStyle;
        private GUIStyle boxStyle;

        /// <summary>
        /// 打开工具窗口菜单
        /// </summary>
        [MenuItem("FFramework/一键替换场景中的字体资产", priority = 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<OneClickReplacementAllFontInScene>("一键字体替换工具");
            window.minSize = new Vector2(400, 410);
        }

        /// <summary>
        /// 初始化样式
        /// </summary>
        private void InitializeStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.largeLabel)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
                };
            }

            if (sectionStyle == null)
            {
                sectionStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 12,
                    normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.8f, 0.8f, 0.8f) : new Color(0.3f, 0.3f, 0.3f) }
                };
            }

            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    fixedHeight = 28
                };
            }

            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(15, 15, 10, 10)
                };
            }
        }

        /// <summary>
        /// 绘制窗口界面
        /// </summary>
        private void OnGUI()
        {
            InitializeStyles();

            // 背景色
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height),
                EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.76f, 0.76f, 0.76f));

            GUILayout.Space(5);

            // 主标题
            EditorGUILayout.LabelField("场景字体一键替换工具", headerStyle);

            // 分割线
            DrawSeparator();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // 字体设置区域
            DrawFontSettingsSection();

            GUILayout.Space(5);

            // 选项设置区域
            DrawOptionsSection();

            GUILayout.Space(5);

            // 操作按钮区域
            DrawActionButtonsSection();

            GUILayout.Space(5);

            // 工具区域
            DrawToolsSection();

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制分割线
        /// </summary>
        private void DrawSeparator()
        {
            GUILayout.Space(5);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.6f, 0.6f, 0.6f));
            GUILayout.Space(5);
        }

        /// <summary>
        /// 绘制字体设置区域
        /// </summary>
        private void DrawFontSettingsSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);

            EditorGUILayout.LabelField("字体资产设置", sectionStyle);
            GUILayout.Space(2);

            // Text组件字体设置
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Unity Text 字体:", GUILayout.Width(120));
            targetFont = (Font)EditorGUILayout.ObjectField(targetFont, typeof(Font), false);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(2);

            // TextMeshPro组件字体设置
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("TextMeshPro 字体:", GUILayout.Width(120));
            targetTMPFont = (TMP_FontAsset)EditorGUILayout.ObjectField(targetTMPFont, typeof(TMP_FontAsset), false);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制选项设置区域
        /// </summary>
        private void DrawOptionsSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);

            EditorGUILayout.LabelField("选项设置", sectionStyle);
            GUILayout.Space(8);

            includeInactiveObjects = EditorGUILayout.ToggleLeft("包含未激活的对象", includeInactiveObjects);

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制操作按钮区域
        /// </summary>
        private void DrawActionButtonsSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);

            EditorGUILayout.LabelField("执行操作", sectionStyle);
            GUILayout.Space(8);

            // 提示信息
            if (targetFont == null && targetTMPFont == null)
            {
                EditorGUILayout.HelpBox("请至少选择一个目标字体", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("本工具会替换场景中所有匹配组件的字体，请确保已保存场景。", MessageType.Info);
            }

            GUILayout.Space(5);

            // 替换按钮
            GUI.backgroundColor = (targetFont != null || targetTMPFont != null) ? Color.cyan : Color.gray;
            EditorGUI.BeginDisabledGroup(targetFont == null && targetTMPFont == null);
            if (GUILayout.Button("一键替换字体", buttonStyle))
            {
                ReplaceAllFontsInScene();
            }
            EditorGUI.EndDisabledGroup();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制工具区域
        /// </summary>
        private void DrawToolsSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);

            EditorGUILayout.LabelField("分析工具", sectionStyle);
            GUILayout.Space(5);

            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("统计场景字体使用情况", buttonStyle))
            {
                ShowFontUsageStatistics();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 替换场景中所有Text和TextMeshPro组件的字体
        /// </summary>
        private void ReplaceAllFontsInScene()
        {
            int textReplaceCount = 0;
            int tmpReplaceCount = 0;

            // 查找所有GameObject
            GameObject[] allObjects = FindObjectsOfType<GameObject>(includeInactiveObjects);

            foreach (GameObject obj in allObjects)
            {
                // 替换Text组件字体
                if (targetFont != null)
                {
                    Text textComponent = obj.GetComponent<Text>();
                    if (textComponent != null && textComponent.font != targetFont)
                    {
                        textComponent.font = targetFont;
                        textReplaceCount++;
                        EditorUtility.SetDirty(textComponent);
                    }
                }

                // 替换TextMeshPro组件字体（UGUI）
                if (targetTMPFont != null)
                {
                    TextMeshProUGUI tmpComponent = obj.GetComponent<TextMeshProUGUI>();
                    if (tmpComponent != null && tmpComponent.font != targetTMPFont)
                    {
                        tmpComponent.font = targetTMPFont;
                        tmpReplaceCount++;
                        EditorUtility.SetDirty(tmpComponent);
                    }

                    // 新增：替换3D TMP字体
                    TextMeshPro tmp3dComponent = obj.GetComponent<TextMeshPro>();
                    if (tmp3dComponent != null && tmp3dComponent.font != targetTMPFont)
                    {
                        tmp3dComponent.font = targetTMPFont;
                        tmpReplaceCount++;
                        EditorUtility.SetDirty(tmp3dComponent);
                    }
                }
            }

            // 显示结果
            string resultMessage = $"字体替换完成！\n\n";
            if (targetFont != null)
                resultMessage += $"Text组件替换: {textReplaceCount} 个\n";
            if (targetTMPFont != null)
                resultMessage += $"TextMeshPro组件替换: {tmpReplaceCount} 个";

            EditorUtility.DisplayDialog("替换结果", resultMessage, "确定");

            // 标记场景已修改
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }

        /// <summary>
        /// 统计场景中所有Text和TextMeshPro组件的字体使用情况
        /// </summary>
        private void ShowFontUsageStatistics()
        {
            Dictionary<Font, int> fontUsage = new Dictionary<Font, int>();
            Dictionary<TMP_FontAsset, int> tmpFontUsage = new Dictionary<TMP_FontAsset, int>();

            GameObject[] allObjects = FindObjectsOfType<GameObject>(includeInactiveObjects);

            foreach (GameObject obj in allObjects)
            {
                // 统计Text组件字体
                Text textComponent = obj.GetComponent<Text>();
                if (textComponent != null && textComponent.font != null)
                {
                    if (fontUsage.ContainsKey(textComponent.font))
                        fontUsage[textComponent.font]++;
                    else
                        fontUsage[textComponent.font] = 1;
                }

                // 统计TextMeshPro组件字体
                TextMeshProUGUI tmpComponent = obj.GetComponent<TextMeshProUGUI>();
                if (tmpComponent != null && tmpComponent.font != null)
                {
                    if (tmpFontUsage.ContainsKey(tmpComponent.font))
                        tmpFontUsage[tmpComponent.font]++;
                    else
                        tmpFontUsage[tmpComponent.font] = 1;
                }
            }

            // 显示统计结果
            string statsMessage = "场景字体使用统计:\n\n";
            statsMessage += "Unity Text 字体:\n";
            if (fontUsage.Count > 0)
            {
                foreach (var pair in fontUsage)
                {
                    statsMessage += $"  • {pair.Key.name}: {pair.Value} 个\n";
                }
            }
            else
            {
                statsMessage += "  • 无Text字体\n";
            }

            statsMessage += "\nTextMeshPro 字体:\n";
            if (tmpFontUsage.Count > 0)
            {
                foreach (var pair in tmpFontUsage)
                {
                    statsMessage += $"  • {pair.Key.name}: {pair.Value} 个\n";
                }
            }
            else
            {
                statsMessage += "  • 无TextMeshPro字体\n";
            }

            EditorUtility.DisplayDialog("字体使用统计", statsMessage, "确定");
        }
    }
}