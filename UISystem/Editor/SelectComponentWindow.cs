using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FFramework.Editor
{
    internal class SelectComponentWindow : EditorWindow
    {
        private List<Component> components;
        private string objectName;
        private Component result;  // 改为实例变量
        private Action<Component> onSelected;
        // IMGUI specific fields
        private int selectedIndex = 0;
        private GUIStyle titleStyle, subTitleStyle, itemStyle, selectedItemStyle, windowBackgroundStyle;

        public static void Prompt(List<Component> comps, string objName, Action<Component> onSelected)
        {
            var win = CreateInstance<SelectComponentWindow>();
            win.titleContent = new GUIContent("选择组件类型");
            win.minSize = new Vector2(280, 100);

            win.components = comps;
            win.objectName = objName;
            win.onSelected = onSelected;

            win.Show();
        }

        private void OnEnable()
        {
            // 初始化样式
            InitializeStyles();
        }

        private void InitializeStyles()
        {
            // 窗口背景
            windowBackgroundStyle = new GUIStyle();
            windowBackgroundStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/projectbrowsericonareabg.png") as Texture2D;

            // 标题样式
            titleStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
            };

            // 副标题样式
            subTitleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };

            // 列表项样式
            itemStyle = new GUIStyle(EditorStyles.label)
            {
                padding = new RectOffset(5, 5, 5, 5),
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
            };

            // 选中项样式
            selectedItemStyle = new GUIStyle(itemStyle);
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0.25f, 0.45f, 0.65f)); // 蓝色高亮
            tex.Apply();
            selectedItemStyle.normal.background = tex;
        }

        public void OnGUI()
        {
            // 防止布局栈错乱
            if (components == null || components.Count == 0)
                return;

            // 绘制深色背景
            GUI.Box(new Rect(0, 0, position.width, position.height), GUIContent.none, windowBackgroundStyle);

            EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(5, 5, 0, 0) });

            // --- 标题区域 ---
            EditorGUILayout.LabelField($"对象: {objectName}", titleStyle);
            EditorGUILayout.LabelField("请选择要绑定的组件类型:", subTitleStyle);

            // --- 下拉选框区域 ---
            string[] options = components.Select(c => c != null ? c.GetType().Name : "空").ToArray();

            // 确保 selectedIndex 在有效范围内
            selectedIndex = Mathf.Clamp(selectedIndex, 0, options.Length - 1);
            selectedIndex = EditorGUILayout.Popup(selectedIndex, options, GUILayout.Height(30));

            // --- 按钮区域 ---
            EditorGUILayout.BeginHorizontal();
            float btnWidth = (position.width - 20) / 2f; // 15+15内边距+10间隔
            if (GUILayout.Button("取消", GUILayout.Width(btnWidth), GUILayout.Height(24)))
            {
                onSelected?.Invoke(null);
                Close();
            }
            GUILayout.Space(1);
            if (GUILayout.Button("确定", GUILayout.Width(btnWidth), GUILayout.Height(24)))
            {
                ConfirmSelection();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            HandleKeyboardEvents();
        }

        private void HandleKeyboardEvents()
        {
            Event e = Event.current;
            if (e.type != EventType.KeyDown) return;

            if (e.keyCode == KeyCode.UpArrow)
            {
                selectedIndex = Mathf.Max(0, selectedIndex - 1);
                e.Use();
                Repaint();
            }
            else if (e.keyCode == KeyCode.DownArrow)
            {
                selectedIndex = Mathf.Min(components.Count - 1, selectedIndex + 1);
                e.Use();
                Repaint();
            }
            else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                ConfirmSelection();
                e.Use();
            }
            else if (e.keyCode == KeyCode.Escape)
            {
                Close();
                e.Use();
            }
        }

        private void ConfirmSelection()
        {
            if (components != null && selectedIndex >= 0 && selectedIndex < components.Count)
            {
                var selectedComponent = components[selectedIndex];
                onSelected?.Invoke(selectedComponent);
                Close();
            }
            else
            {
                onSelected?.Invoke(null);
                Close();
            }
        }
    }
}