// =============================================================
// 描述：UI检查器面板
// 作者：HCFlower
// 创建时间：2025-11-15 18:49:00
// 版本：1.0.5
// =============================================================
using System.Collections.Generic;
using UnityEngine.EventSystems;
using FFramework.Utility;
using UnityEngine.Events;
using System.Reflection;
using UnityEngine.UI;
using UnityEditor;
using System.Linq;
using UnityEngine;
using System;
using TMPro;

namespace FFramework.Editor
{
    [CustomEditor(typeof(UIPanel), true)]
    public class UIPanelInspector : UnityEditor.Editor
    {
        #region Private Fields
        private UIPanel panel;
        private bool showSummary = false;
        private bool showCleanupActions;
        private Vector2 scrollPos;
        private string searchFilter = "";
        private List<Action> trackedCleanupActions = new List<Action>();

        private bool showStats = true;
        private bool showComponents = true;
        private bool showTracking = false;
        private bool showQuickOps = false;
        #endregion

        #region Unity Methods

        public override void OnInspectorGUI()
        {
            panel = (UIPanel)target;
            if (panel == null)
            {
                EditorGUILayout.HelpBox("面板为空。", MessageType.Error);
                return;
            }

            // 使用自定义边距样式修正左右边距不一致问题
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(-15); // 减少左边距
            EditorGUILayout.BeginVertical();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            DrawSerializedProperties();
            DrawSummarySection();

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
            GUILayout.Space(-4); // 增加右边距
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region 序列化属性区域

        /// <summary>
        /// 绘制序列化属性区域
        /// </summary>
        private void DrawSerializedProperties()
        {
            // 获取整个面板区域的起始位置
            Rect panelStartRect = GUILayoutUtility.GetRect(0, 0);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(2);

            // 标题行
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(2);

            // 标题标签 - 不再使用虚线边框
            EditorGUILayout.LabelField("字段管理 (支持拖拽UI对象)", EditorStyles.boldLabel, GUILayout.Width(220));

            GUILayout.FlexibleSpace();
            // 用图标按钮表示锚点全覆盖
            GUIContent anchorIcon = EditorGUIUtility.IconContent("RectTransformBlueprint");
            anchorIcon.tooltip = "锚点全覆盖";
            GUIStyle iconBtnStyle = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(2, 1, 1, 2),
                alignment = TextAnchor.MiddleCenter
            };
            if (GUILayout.Button(anchorIcon, iconBtnStyle, GUILayout.Width(20), GUILayout.Height(20)))
            {
                // 设置RectTransform锚点为全覆盖
                if (panel != null && panel.transform is RectTransform rect)
                {
                    Undo.RecordObject(rect, "RectTransform锚点全覆盖");
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    EditorUtility.SetDirty(rect);
                    Debug.Log("已将RectTransform锚点设置为全覆盖模式");
                }
                else
                {
                    Debug.LogWarning("当前对象没有RectTransform,无法设置锚点全覆盖");
                }
            }
            if (GUILayout.Button("设置面板名", GUILayout.Width(70), GUILayout.Height(20)))
            {
                if (panel != null)
                {
                    panel.gameObject.name = panel.GetType().Name;
                    EditorUtility.SetDirty(panel.gameObject);
                    Debug.Log($"已将面板名称设置为类名：{panel.gameObject.name}");
                }
            }
            if (GUILayout.Button("定位脚本", GUILayout.Width(60), GUILayout.Height(20)))
            {
                PingScriptFile();
            }
            if (GUILayout.Button("打开脚本", GUILayout.Width(60), GUILayout.Height(20)))
            {
                OpenScript();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 获取整个属性区域的起始位置
            Rect propertiesStartRect = GUILayoutUtility.GetRect(0, 0);

            // 绘制属性
            serializedObject.Update();
            SerializedProperty prop = serializedObject.GetIterator();
            bool enterChildren = true;

            EditorGUI.indentLevel++;
            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (prop.propertyPath == "m_Script") continue;

                // 绘制字段及删除按钮
                DrawPropertyFieldWithDeleteButton(prop);
            }
            EditorGUI.indentLevel--;
            serializedObject.ApplyModifiedProperties();

            // 获取整个属性区域的结束位置
            Rect propertiesEndRect = GUILayoutUtility.GetRect(0, 0);
            float propertiesEndY = propertiesEndRect.y;

            EditorGUILayout.EndVertical();

            // 计算整个面板区域的矩形（从面板开始到结束）
            Rect fullPanelRect = new Rect(
                panelStartRect.x,
                panelStartRect.y,
                propertiesStartRect.width,
                propertiesEndY - panelStartRect.y
            );

            // 在整个区域处理拖拽
            HandleDragAndDrop(fullPanelRect);
        }

        /// <summary>
        /// 绘制带删除按钮的属性字段
        /// </summary>
        private void DrawPropertyFieldWithDeleteButton(SerializedProperty prop)
        {
            // 计算完整高度与“仅头部”(含装饰器+标签行)高度
            float fullHeight = EditorGUI.GetPropertyHeight(prop, true);
            float headerHeight = EditorGUI.GetPropertyHeight(prop, false);

            float buttonWidth = 20f;
            float buttonSpacing = 2f;

            // 预留按钮空间的整体区域
            Rect fullRect = EditorGUILayout.GetControlRect(true, fullHeight);

            // 左侧属性区（减去按钮宽度与间距）
            Rect propertyRect = new Rect(fullRect.x, fullRect.y, fullRect.width - buttonWidth - buttonSpacing, fullHeight);
            EditorGUI.PropertyField(propertyRect, prop, true);

            // 将按钮锚定到“标签行”的垂直位置（头部高度 - 单行高度）
            float labelY = fullRect.y + Mathf.Max(0f, headerHeight - EditorGUIUtility.singleLineHeight);
            Rect buttonRect = new Rect(fullRect.xMax - buttonWidth, labelY, buttonWidth, EditorGUIUtility.singleLineHeight);

            DrawDeleteButton(buttonRect, prop.name);
        }

        // 统一的删除按钮绘制与逻辑
        private void DrawDeleteButton(Rect buttonRect, string fieldName)
        {
            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.red;
            if (GUI.Button(buttonRect, "x"))
            {
                if (EditorUtility.DisplayDialog(
                    "删除字段",
                    $"确定要从代码中删除字段 '{fieldName}' 吗?\n\n此操作将从脚本文件中移除字段定义.",
                    "删除", "取消"))
                {
                    RemoveFieldFromScript(fieldName);
                }
            }
            GUI.backgroundColor = oldColor;
        }

        /// <summary>
        /// 绘制复杂类型（序列化类、数组、List）的字段，删除按钮固定在标题行
        /// </summary>
        private void DrawComplexTypeWithDeleteButton(SerializedProperty prop, float propertyHeight, bool hasDecorator, float buttonWidth, float buttonSpacing)
        {
            // 获取标题行高度
            float headerHeight = EditorGUIUtility.singleLineHeight;
            if (hasDecorator)
            {
                // 如果有装饰器，需要计算装饰器的高度
                var field = GetFieldFromProperty(prop);
                if (field != null)
                {
                    var headerAttrs = field.GetCustomAttributes<HeaderAttribute>();
                    if (headerAttrs.Any())
                    {
                        headerHeight += 18f; // Header装饰器大约增加18像素
                    }
                }
            }

            // 计算总的控件区域
            Rect fullRect = EditorGUILayout.GetControlRect(true, propertyHeight);

            // 绘制属性，为按钮预留空间
            Rect propertyRect = new Rect(fullRect.x, fullRect.y, fullRect.width - buttonWidth - buttonSpacing, fullRect.height);
            EditorGUI.PropertyField(propertyRect, prop, true);

            // 删除按钮固定在标题区域的右侧
            Rect buttonRect = new Rect(
                fullRect.xMax - buttonWidth,
                fullRect.y + (hasDecorator ? 18f : 0f), // 如果有装饰器，按钮下移到装饰器下方
                buttonWidth,
                EditorGUIUtility.singleLineHeight
            );

            // 绘制删除按钮
            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.red;
            if (GUI.Button(buttonRect, "x"))
            {
                string fieldName = prop.name;
                if (EditorUtility.DisplayDialog(
                    "删除字段",
                    $"确定要从代码中删除字段 '{fieldName}' 吗?\n\n此操作将从脚本文件中移除字段定义.",
                    "删除", "取消"))
                {
                    RemoveFieldFromScript(fieldName);
                }
            }
            GUI.backgroundColor = oldColor;
        }

        /// <summary>
        /// 绘制有装饰器的简单类型
        /// </summary>
        private void DrawSimpleTypeWithDecoratorAndDeleteButton(SerializedProperty prop, float propertyHeight, float buttonWidth, float buttonSpacing)
        {
            Rect fullRect = EditorGUILayout.GetControlRect(true, propertyHeight);

            // 绘制属性(包含装饰器)，限制宽度为按钮预留空间
            Rect propertyRect = new Rect(fullRect.x, fullRect.y, fullRect.width - buttonWidth - buttonSpacing, fullRect.height);
            EditorGUI.PropertyField(propertyRect, prop, true);

            // 绘制删除按钮 - 与最后一行对齐
            Rect buttonRect = new Rect(
                fullRect.xMax - buttonWidth,
                fullRect.yMax - EditorGUIUtility.singleLineHeight,
                buttonWidth,
                EditorGUIUtility.singleLineHeight
            );

            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.red;
            if (GUI.Button(buttonRect, "x"))
            {
                string fieldName = prop.name;
                if (EditorUtility.DisplayDialog(
                    "删除字段",
                    $"确定要从代码中删除字段 '{fieldName}' 吗?\n\n此操作将从脚本文件中移除字段定义.",
                    "删除", "取消"))
                {
                    RemoveFieldFromScript(fieldName);
                }
            }
            GUI.backgroundColor = oldColor;
        }

        /// <summary>
        /// 绘制普通简单类型
        /// </summary>
        private void DrawSimpleTypeWithDeleteButton(SerializedProperty prop, float propertyHeight, float buttonWidth, float buttonSpacing)
        {
            Rect fullRect = EditorGUILayout.GetControlRect(true, propertyHeight);

            // 绘制属性
            Rect propertyRect = new Rect(fullRect.x, fullRect.y, fullRect.width - buttonWidth - buttonSpacing, fullRect.height);
            EditorGUI.PropertyField(propertyRect, prop, true);

            // 绘制删除按钮 - 位置和大小与有装饰器的情况完全一致
            Rect buttonRect = new Rect(
                fullRect.xMax - buttonWidth,
                fullRect.y,
                buttonWidth,
                EditorGUIUtility.singleLineHeight
            );

            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.red;
            if (GUI.Button(buttonRect, "x"))
            {
                string fieldName = prop.name;
                if (EditorUtility.DisplayDialog(
                    "删除字段",
                    $"确定要从代码中删除字段 '{fieldName}' 吗?\n\n此操作将从脚本文件中移除字段定义.",
                    "删除", "取消"))
                {
                    RemoveFieldFromScript(fieldName);
                }
            }
            GUI.backgroundColor = oldColor;
        }

        /// <summary>
        /// 判断属性是否为复杂的可序列化类型（包括序列化类、数组、List等）
        /// </summary>
        private bool IsComplexSerializableType(SerializedProperty prop)
        {
            // 检查数组类型
            if (prop.isArray && prop.propertyType != SerializedPropertyType.String)
            {
                return true;
            }

            // 检查属性类型是否为可序列化类
            if (prop.propertyType == SerializedPropertyType.Generic)
            {
                // 获取字段类型
                var field = GetFieldFromProperty(prop);
                if (field != null)
                {
                    var fieldType = field.FieldType;

                    // 检查是否为List<T>
                    if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        return true;
                    }

                    // 检查是否有 [System.Serializable] 特性或Unity的基本类型
                    if (fieldType.IsClass &&
                        !fieldType.IsSubclassOf(typeof(UnityEngine.Object)) &&
                        (fieldType.GetCustomAttribute<System.SerializableAttribute>() != null ||
                         IsUnitySerializableType(fieldType)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 检查是否为Unity可序列化的类型
        /// </summary>
        private bool IsUnitySerializableType(System.Type type)
        {
            // Unity内置的可序列化类型
            return type == typeof(Vector2) ||
                   type == typeof(Vector3) ||
                   type == typeof(Vector4) ||
                   type == typeof(Quaternion) ||
                   type == typeof(Color) ||
                   type == typeof(Rect) ||
                   type == typeof(AnimationCurve) ||
                   type == typeof(Gradient);
        }

        /// <summary>
        /// 从SerializedProperty获取对应的FieldInfo
        /// </summary>
        private System.Reflection.FieldInfo GetFieldFromProperty(SerializedProperty prop)
        {
            var targetType = prop.serializedObject.targetObject.GetType();
            return targetType.GetField(prop.name,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);
        }

        /// <summary>
        /// 从脚本中移除字段定义
        /// </summary>
        private void RemoveFieldFromScript(string fieldName)
        {
            MonoScript script = MonoScript.FromMonoBehaviour(panel);
            if (script == null)
            {
                EditorUtility.DisplayDialog("错误", "无法定位面板脚本文件。", "确定");
                return;
            }

            string scriptPath = AssetDatabase.GetAssetPath(script);
            if (string.IsNullOrEmpty(scriptPath) || !System.IO.File.Exists(scriptPath))
            {
                EditorUtility.DisplayDialog("错误", "脚本文件不存在。", "确定");
                return;
            }

            try
            {
                string code = System.IO.File.ReadAllText(scriptPath);
                string modifiedCode = ScriptModifier.RemoveFieldFromCode(code, fieldName);

                if (modifiedCode == code)
                {
                    EditorUtility.DisplayDialog("警告", $"未找到字段 '{fieldName}' 的定义。", "确定");
                    return;
                }

                System.IO.File.WriteAllText(scriptPath, modifiedCode);
                AssetDatabase.Refresh();

                Debug.Log($"成功删除字段: {fieldName}");
                Repaint();
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("删除字段失败",
                    $"修改脚本文件时出错：\n{ex.Message}", "确定");
            }
        }

        // 修复后的拖拽处理逻辑 - 优化版
        private void HandleDragAndDrop(Rect dropRect)
        {
            Event evt = Event.current;
            if (evt == null) return;

            // 只处理特定的事件类型
            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform)
                return;

            // 检查鼠标是否在指定区域内
            if (!dropRect.Contains(evt.mousePosition))
                return;

            // 检查是否有GameObject被拖拽
            if (DragAndDrop.objectReferences == null || DragAndDrop.objectReferences.Length == 0)
                return;

            bool hasGameObject = DragAndDrop.objectReferences.Any(o => o is GameObject);
            if (!hasGameObject)
                return;

            // 更新拖拽视觉效果
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            // 在属性区域添加视觉反馈
            if (evt.type == EventType.DragUpdated)
            {
                DrawDropZoneHighlight(dropRect);
            }

            // 只在真正执行拖拽时处理
            if (evt.type == EventType.DragPerform)
            {
                // 接受拖拽
                DragAndDrop.AcceptDrag();
                evt.Use(); // 立即标记事件已处理

                // 直接处理，不延迟
                ProcessDraggedObjects(DragAndDrop.objectReferences);

                // 清理拖拽状态
                DragAndDrop.PrepareStartDrag();
            }
        }

        /// <summary>
        /// 绘制拖拽区域高亮效果
        /// </summary>
        private void DrawDropZoneHighlight(Rect dropRect)
        {
            // 保存原始颜色
            var originalColor = Handles.color;

            // 设置高亮颜色
            Handles.color = new Color(0.3f, 0.8f, 1f, 0.3f);

            // 绘制半透明覆盖层
            Handles.DrawSolidRectangleWithOutline(dropRect,
                new Color(0.3f, 0.8f, 1f, 0.1f), // 填充色
                new Color(0.3f, 0.8f, 1f, 0.8f)  // 边框色
            );

            // 恢复原始颜色
            Handles.color = originalColor;

            // 强制重绘以显示高亮效果
            Repaint();
        }

        // 处理拖拽对象的方法
        private void ProcessDraggedObjects(UnityEngine.Object[] draggedObjects)
        {
            if (panel == null) return;

            // 只处理第一个GameObject
            var go = draggedObjects.OfType<GameObject>().FirstOrDefault();
            if (go == null) return;

            // 延迟处理，避免在 OnGUI 中直接调用模态窗口
            EditorApplication.delayCall += () =>
            {
                if (TryCreateSerializedFieldForGameObject(go))
                {
                    EditorUtility.SetDirty(panel);
                    Repaint();
                }
            };

            // 立即退出当前 GUI 绘制，防止布局栈错乱
            GUIUtility.ExitGUI();
        }

        // 创建字段方法，返回是否成功
        private bool TryCreateSerializedFieldForGameObject(GameObject go)
        {
            if (panel == null || go == null)
                return false;

            var components = go.GetComponents<Component>()
                               .Where(c => c is RectTransform || c is Transform ||
                                           c is UnityEngine.UI.Selectable ||
                                           c is UnityEngine.UI.Button ||
                                           c is UnityEngine.UI.Toggle ||
                                           c is UnityEngine.UI.Slider ||
                                           c is UnityEngine.UI.InputField ||
                                           c is UnityEngine.UI.Dropdown ||
                                           c is UnityEngine.UI.ScrollRect ||
                                           c is UnityEngine.EventSystems.EventTrigger ||
                                           c is CanvasGroup ||
                                           c is Text || c is Image || c is RawImage ||
                                           // 支持 TMP
                                           c is TextMeshProUGUI ||
                                           c is TMP_Text ||
                                           c is TMP_InputField ||
                                           c is TMP_Dropdown)
                               .Distinct()
                               .ToList();

            if (components.Count == 0)
            {
                EditorUtility.DisplayDialog("无法创建字段",
                    $"对象 '{go.name}' 不包含可支持的UI/TMP组件。", "确定");
                return false;
            }

            if (components.Count == 1)
            {
                // 只有一个组件，直接创建
                return CreateNewSerializedField(ToSafeIdentifier(go.name), components[0]);
            }
            else
            {
                // 多个组件，弹窗选择，使用回调
                SelectComponentWindow.Prompt(components, go.name, (chosen) =>
                {
                    if (chosen == null)
                    {
                        Debug.Log($"取消为对象 '{go.name}' 创建字段");
                        return;
                    }

                    string fieldName = ToSafeIdentifier(go.name);

                    // 检查字段是否已存在
                    var existingField = panel.GetType().GetField(fieldName,
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                    if (existingField != null && existingField.FieldType.IsAssignableFrom(chosen.GetType()))
                    {
                        Undo.RecordObject(panel, "赋值序列化字段");
                        existingField.SetValue(panel, chosen);
                        EditorUtility.SetDirty(panel);
                        Debug.Log($"已为现有字段赋值: {fieldName} => {chosen.GetType().Name}");
                    }
                    else
                    {
                        CreateNewSerializedField(fieldName, chosen);
                    }

                    Repaint();
                });

                // 回调式，不阻塞，直接返回
                return true;
            }
        }

        // 创建新序列化字段的方法
        private bool CreateNewSerializedField(string fieldName, Component component)
        {
            MonoScript script = MonoScript.FromMonoBehaviour(panel);
            if (script == null)
            {
                EditorUtility.DisplayDialog("错误", "无法定位面板脚本文件。", "确定");
                return false;
            }

            string scriptPath = AssetDatabase.GetAssetPath(script);
            if (string.IsNullOrEmpty(scriptPath) || !System.IO.File.Exists(scriptPath))
            {
                EditorUtility.DisplayDialog("错误", "脚本文件不存在。", "确定");
                return false;
            }

            try
            {
                string code = System.IO.File.ReadAllText(scriptPath);
                string typeName = component.GetType().Name;
                string usingNamespace = component.GetType().Namespace;
                string fieldLine = $"        [SerializeField] private {typeName} {fieldName};";

                // 修改这里：使用 insertAtEnd: true 参数，确保新字段添加在区域末尾
                string newCode = ScriptModifier.InsertFieldIntoRegion(code, fieldLine, regionName: "字段", insertAtEnd: true);
                newCode = ScriptModifier.EnsureUsing(newCode, usingNamespace);
                newCode = ScriptModifier.EnsureUsing(newCode, "UnityEngine.UI");
                newCode = ScriptModifier.EnsureUsing(newCode, "UnityEngine");
                newCode = ScriptModifier.EnsureUsing(newCode, "TMPro");

                System.IO.File.WriteAllText(scriptPath, newCode);
                AssetDatabase.Refresh();

                Debug.Log($"成功创建字段: {fieldName} ({typeName})");
                return true;
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("创建字段失败",
                    $"写入脚本文件时出错：\n{ex.Message}", "确定");
                return false;
            }
        }

        private string ToSafeIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name)) return "_field";
            // 将非法字符替换为下划线，若首字符不是字母或下划线则前缀_
            var sb = new System.Text.StringBuilder(name.Length + 1);
            if (!(char.IsLetter(name[0]) || name[0] == '_')) sb.Append('_');
            foreach (char c in name)
            {
                sb.Append((char.IsLetterOrDigit(c) || c == '_') ? c : '_');
            }
            return sb.ToString();
        }
        #endregion

        #region 事件监听区域

        /// <summary>
        /// 绘制UI事件检查器区域
        /// </summary>
        private void DrawSummarySection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 折叠标题按钮
            DrawCollapsibleTitle();

            if (!showSummary)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            // 统一的工具栏与搜索
            DrawToolbarAndSearch();

            // 合并信息面板
            DrawInfoPanel();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制折叠标题
        /// </summary>
        private void DrawCollapsibleTitle()
        {
            EditorGUILayout.BeginHorizontal();

            GUIStyle transparentButtonStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = null },
                hover = { background = null },
                active = { background = null },
                focused = { background = null },
                border = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };

            if (GUILayout.Button(" UI事件检查器 - 完整概览", transparentButtonStyle,
                GUILayout.Height(24), GUILayout.ExpandWidth(true)))
            {
                showSummary = !showSummary;
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制工具栏和搜索区域
        /// </summary>
        private void DrawToolbarAndSearch()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            // 操作按钮
            if (GUILayout.Button("刷新", GUILayout.Height(18), GUILayout.Width(50)))
            {
                Repaint();
            }
            // 搜索区域
            GUILayout.Label("筛选:", GUILayout.Width(30));
            searchFilter = EditorGUILayout.TextField(searchFilter, GUILayout.Height(18));

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 合并后的信息面板（统计 / 组件列表 / 事件追踪 / 快速操作）
        /// </summary>
        private void DrawInfoPanel()
        {
            // 顶部页签切换（可选：用按钮模拟）
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(showStats, "统计", EditorStyles.miniButtonLeft, GUILayout.Height(20)))
            { showStats = true; showComponents = false; showTracking = false; showQuickOps = false; }
            if (GUILayout.Toggle(showComponents, "组件", EditorStyles.miniButtonMid, GUILayout.Height(20)))
            { showStats = false; showComponents = true; showTracking = false; showQuickOps = false; }
            if (GUILayout.Toggle(showTracking, "追踪", EditorStyles.miniButtonMid, GUILayout.Height(20)))
            { showStats = false; showComponents = false; showTracking = true; showQuickOps = false; }
            if (GUILayout.Toggle(showQuickOps, "操作", EditorStyles.miniButtonRight, GUILayout.Height(20)))
            { showStats = false; showComponents = false; showTracking = false; showQuickOps = true; }
            EditorGUILayout.EndHorizontal();

            // 单容器承载内容，内部使用更轻量的样式，避免重复 helpBox
            EditorGUILayout.BeginVertical();

            if (showStats)
            {
                DrawStatistics();
            }
            else if (showComponents)
            {
                DrawComponentsList();
            }
            else if (showTracking)
            {
                DrawCleanupActionsSection();
            }
            else if (showQuickOps)
            {
                DrawQuickActions();
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制统计信息
        /// </summary>
        private void DrawStatistics()
        {
            var allUIComponents = GetAllUIComponentsWithEvents();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("UI事件组件:", EditorStyles.boldLabel, GUILayout.Width(90));
            EditorGUILayout.LabelField($"{allUIComponents.Count}", EditorStyles.boldLabel, GUILayout.Width(30));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制组件列表 - 优化布局
        /// </summary>
        private void DrawComponentsList()
        {
            var allUIComponents = GetAllUIComponentsWithEvents();

            if (allUIComponents.Count > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("已绑定事件的UI组件", EditorStyles.boldLabel);
                // EditorGUILayout.Space(2);

                foreach (var uiComponent in allUIComponents)
                {
                    if (!IsMatchFilter(uiComponent.Component.gameObject.name))
                        continue;

                    DrawUIComponentItemCompact(uiComponent);
                }
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("无已绑定事件的UI组件", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// 绘制事件追踪统计区域
        /// </summary>
        private void DrawCleanupActionsSection()
        {
            FetchCleanupActions();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 标题行
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("事件追踪", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField($"追踪: {trackedCleanupActions.Count}", GUILayout.Width(60));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            // 清理动作详情
            if (trackedCleanupActions.Count > 0)
            {
                DrawCleanupActionsDetails();
            }
            else
            {
                DrawNoCleanupActionsMessage();
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制快速操作区域
        /// </summary>
        private void DrawQuickActions()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("快速操作", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("重新扫描", GUILayout.Height(20)))
            {
                Repaint();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("打印概览", GUILayout.Height(20)))
            {
                ExportReportToConsole(false);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        #endregion

        #region 辅助方法 - 组件列表绘制优化

        /// <summary>
        /// 绘制紧凑的UI组件项 - 主要优化方法
        /// </summary>
        private void DrawUIComponentItemCompact(UIComponentInfo uiComponent)
        {
            var prevBgColor = GUI.backgroundColor;
            GUI.backgroundColor = uiComponent.TypeColor * 0.15f + Color.white * 0.85f;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 主要信息行 - 紧凑布局
            EditorGUILayout.BeginHorizontal();

            // 组件类型标签 - 缩短
            var prevColor = GUI.color;
            GUI.color = uiComponent.TypeColor;
            string shortType = uiComponent.ComponentType;
            EditorGUILayout.LabelField($"[{shortType}]", GUILayout.Width(90));
            GUI.color = prevColor;

            // 组件名称 - 限制长度
            string displayName = uiComponent.Component.gameObject.name;
            if (displayName.Length > 20)
                displayName = displayName.Substring(0, 17) + "...";

            EditorGUILayout.LabelField(displayName, EditorStyles.boldLabel, GUILayout.Width(140));

            // 监听器数量
            EditorGUILayout.LabelField($"事件:{uiComponent.ListenerCount}", GUILayout.Width(50));

            GUILayout.FlexibleSpace();

            // 操作按钮 - 小尺寸
            if (GUILayout.Button("选中", GUILayout.Width(35), GUILayout.Height(16)))
            {
                Selection.activeObject = uiComponent.Component.gameObject;
                EditorGUIUtility.PingObject(uiComponent.Component.gameObject);
            }

            if (GUILayout.Button("详情", GUILayout.Width(35), GUILayout.Height(16)))
            {
                ShowComponentDetail(uiComponent);
            }

            EditorGUILayout.EndHorizontal();

            // 简化的路径信息 - 只在需要时显示
            if (ShouldShowPath(uiComponent.Component.gameObject))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space(10);
                string shortPath = GetShortPath(uiComponent.Component.gameObject);
                EditorGUILayout.LabelField($"路径: {shortPath}", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            GUI.backgroundColor = prevBgColor;
        }

        /// <summary>
        /// 判断是否需要显示路径
        /// </summary>
        private bool ShouldShowPath(GameObject obj)
        {
            // 只有当对象不是直接子对象时才显示路径
            return obj.transform.parent != panel.transform;
        }

        /// <summary>
        /// 获取简短的路径
        /// </summary>
        private string GetShortPath(GameObject obj)
        {
            string fullPath = GetGameObjectPath(obj);

            // 如果路径太长，只显示最后两级
            string[] pathParts = fullPath.Split('/');
            if (pathParts.Length > 2)
            {
                return ".../" + pathParts[pathParts.Length - 2] + "/" + pathParts[pathParts.Length - 1];
            }

            return fullPath;
        }

        /// <summary>
        /// 绘制清理动作详情
        /// </summary>
        private void DrawCleanupActionsDetails()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"已追踪 {trackedCleanupActions.Count} 个清理动作", EditorStyles.miniLabel);

            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("查看详情", GUILayout.Height(18), GUILayout.Width(70)))
            {
                showCleanupActions = !showCleanupActions;
            }
            EditorGUILayout.EndHorizontal();

            if (showCleanupActions)
            {
                DrawCleanupActionsDetailsList();
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制清理动作详情列表
        /// </summary>
        private void DrawCleanupActionsDetailsList()
        {
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("清理动作详情:", EditorStyles.miniLabel);

            int displayCount = Math.Min(trackedCleanupActions.Count, 8);
            for (int i = 0; i < displayCount; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  [{i}]", EditorStyles.miniLabel, GUILayout.Width(30));
                string methodName = trackedCleanupActions[i]?.Method?.Name ?? "Unknown";
                if (methodName.Length > 25)
                    methodName = methodName.Substring(0, 22) + "...";
                EditorGUILayout.LabelField(methodName, EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            if (trackedCleanupActions.Count > 8)
            {
                EditorGUILayout.LabelField($"  ... 还有 {trackedCleanupActions.Count - 8} 个", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("提示: 这些清理动作会在面板销毁时自动执行", EditorStyles.miniLabel);
        }

        /// <summary>
        /// 绘制无清理动作消息
        /// </summary>
        private void DrawNoCleanupActionsMessage()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("没有追踪的事件清理动作", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField("建议使用带自动追踪的绑定方法", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
        }
        #endregion

        #region 组件详情弹窗

        /// <summary>
        /// 显示组件详情
        /// </summary>
        private void ShowComponentDetail(UIComponentInfo uiComponent)
        {
            switch (uiComponent.ComponentType)
            {
                case "Button":
                    ShowDetailDialog(
                        uiComponent.Component.gameObject.name,
                        "Button",
                        uiComponent.Component as Button,
                        // 基本信息
                        comp => $"Button详细信息: {comp.name}\n",
                        // 事件列表
                        comp => new[] { ("onClick", (UnityEventBase)(comp as Button).onClick) }
                    );
                    break;
                case "Toggle":
                    ShowDetailDialog(
                        uiComponent.Component.gameObject.name,
                        "Toggle",
                        uiComponent.Component as Toggle,
                        comp => $"Toggle详细信息: {comp.name}\n当前状态: {((comp as Toggle).isOn ? "开启" : "关闭")}\n",
                        comp => new[] { ("onValueChanged", (UnityEventBase)(comp as Toggle).onValueChanged) }
                    );
                    break;
                case "Slider":
                    ShowDetailDialog(
                        uiComponent.Component.gameObject.name,
                        "Slider",
                        uiComponent.Component as Slider,
                        comp =>
                        {
                            var s = comp as Slider;
                            return $"Slider详细信息: {s.name}\n当前值: {s.value:F2} (范围: {s.minValue:F2} - {s.maxValue:F2})\n";
                        },
                        comp => new[] { ("onValueChanged", (UnityEventBase)(comp as Slider).onValueChanged) }
                    );
                    break;
                case "InputField":
                    ShowDetailDialog(
                        uiComponent.Component.gameObject.name,
                        "InputField",
                        uiComponent.Component as InputField,
                        comp => $"InputField详细信息: {comp.name}\n当前文本: \"{(comp as InputField).text}\"\n",
                        comp => new[] {
                            ("onValueChanged", (UnityEventBase)(comp as InputField).onValueChanged),
                            ("onEndEdit",     (UnityEventBase)(comp as InputField).onEndEdit)
                        }
                    );
                    break;
                case "Dropdown":
                    ShowDetailDialog(
                        uiComponent.Component.gameObject.name,
                        "Dropdown",
                        uiComponent.Component as Dropdown,
                        comp =>
                        {
                            var d = comp as Dropdown;
                            string basic = $"Dropdown详细信息: {d.name}\n当前值: {d.value}\n";
                            if (d.options != null && d.value >= 0 && d.value < d.options.Count)
                                basic += $"当前选项: \"{d.options[d.value].text}\"\n";
                            basic += $"选项总数: {d.options?.Count ?? 0}\n";
                            return basic;
                        },
                        comp => new[] { ("onValueChanged", (UnityEventBase)(comp as Dropdown).onValueChanged) }
                    );
                    break;
                case "ScrollRect":
                    ShowDetailDialog(
                        uiComponent.Component.gameObject.name,
                        "ScrollRect",
                        uiComponent.Component as ScrollRect,
                        comp =>
                        {
                            var sr = comp as ScrollRect;
                            return $"ScrollRect详细信息: {sr.name}\n当前位置: {sr.normalizedPosition}\n水平滚动: {(sr.horizontal ? "启用" : "禁用")}\n垂直滚动: {(sr.vertical ? "启用" : "禁用")}\n";
                        },
                        comp => new[] { ("onValueChanged", (UnityEventBase)(comp as ScrollRect).onValueChanged) }
                    );
                    break;
                case "EventTrigger":
                    DrawEventTriggerDetail(uiComponent.Component as EventTrigger); // 保留专用实现
                    break;
            }
        }

        // 通用详情弹窗
        private void ShowDetailDialog<TComp>(
            string goName,
            string titlePrefix,
            TComp comp,
            Func<TComp, string> buildBasic,
            Func<TComp, (string label, UnityEventBase evt)[]> buildEvents
        ) where TComp : Component
        {
            if (comp == null)
            {
                EditorUtility.DisplayDialog($"{titlePrefix} 详情", "组件为空。", "确定");
                return;
            }

            string content = buildBasic(comp) + "\n";
            var events = buildEvents(comp);

            foreach (var (label, evt) in events)
            {
                int persistent = evt?.GetPersistentEventCount() ?? 0;
                int runtime = GetRuntimeListenerCount(evt);

                content += $"{label}事件:\n";
                content += $"  持久化监听器: {persistent}\n";
                for (int i = 0; i < persistent; i++)
                {
                    var targetObj = evt.GetPersistentTarget(i);
                    var method = evt.GetPersistentMethodName(i);
                    content += $"    [P{i}] {targetObj?.name}.{method}()\n";
                }
                content += $"  运行时监听器: {runtime}\n\n";
            }

            if (!events.Any(e => (e.evt?.GetPersistentEventCount() ?? 0) > 0 || GetRuntimeListenerCount(e.evt) > 0))
            {
                content += "无事件监听器";
            }

            EditorUtility.DisplayDialog($"{titlePrefix} 详情", content, "确定");
        }

        private void DrawEventTriggerDetail(EventTrigger trigger)
        {
            string content = $"EventTrigger详细信息: {trigger.name}\n\n";

            if (trigger.triggers == null || trigger.triggers.Count == 0)
            {
                content += "无事件条目。";
            }
            else
            {
                content += $"事件条目数量: {trigger.triggers.Count}\n\n";

                foreach (var entry in trigger.triggers)
                {
                    int count = entry.callback.GetPersistentEventCount();
                    content += $"事件类型: {entry.eventID}\n";
                    content += $"监听器数量: {count}\n";

                    for (int i = 0; i < count; i++)
                    {
                        UnityEngine.Object targetObj = entry.callback.GetPersistentTarget(i);
                        string method = entry.callback.GetPersistentMethodName(i);
                        content += $"  [{i}] {targetObj?.name}.{method}()\n";
                    }
                    content += "\n";
                }
            }

            EditorUtility.DisplayDialog("EventTrigger 详情", content, "确定");
        }
        #endregion

        #region 数据结构
        /// <summary>
        /// UI组件信息数据结构
        /// </summary>
        private class UIComponentInfo
        {
            public Component Component;
            public string ComponentType;
            public int ListenerCount;
            public Color TypeColor;
        }
        #endregion

        #region 数据收集方法
        /// <summary>
        /// 获取所有有事件绑定的UI组件
        /// </summary>
        private List<UIComponentInfo> GetAllUIComponentsWithEvents()
        {
            var result = new List<UIComponentInfo>();

            // Buttons
            AddComponentsWithEvents<Button>(result, "Button", Color.cyan, GetListenerCount_Button);
            // Toggles
            AddComponentsWithEvents<Toggle>(result, "Toggle", Color.green, GetListenerCount_Toggle);
            // Sliders
            AddComponentsWithEvents<Slider>(result, "Slider", new Color(0.8f, 0.6f, 0.2f), GetListenerCount_Slider);
            // InputFields
            AddComponentsWithEvents<InputField>(result, "InputField", Color.magenta, GetListenerCount_InputField);
            // Dropdowns
            AddComponentsWithEvents<Dropdown>(result, "Dropdown", Color.yellow, GetListenerCount_Dropdown);
            // ScrollRects
            AddComponentsWithEvents<ScrollRect>(result, "ScrollRect", Color.gray, GetListenerCount_ScrollRect);
            // EventTriggers
            AddEventTriggersWithEvents(result);

            return result;
        }

        /// <summary>
        /// 添加有事件的组件到结果列表
        /// </summary>
        private void AddComponentsWithEvents<T>(List<UIComponentInfo> result, string typeName, Color typeColor,
            Func<T, int> getListenerCount) where T : Component
        {
            var components = FetchComponents<T>();
            foreach (var component in components)
            {
                int count = getListenerCount(component);
                if (count > 0)
                {
                    result.Add(new UIComponentInfo
                    {
                        Component = component,
                        ComponentType = typeName,
                        ListenerCount = count,
                        TypeColor = typeColor
                    });
                }
            }
        }

        /// <summary>
        /// 添加有事件的EventTrigger组件
        /// </summary>
        private void AddEventTriggersWithEvents(List<UIComponentInfo> result)
        {
            var eventTriggers = FetchComponents<EventTrigger>();
            foreach (var trigger in eventTriggers)
            {
                int count = (trigger.triggers != null && trigger.triggers.Count > 0) ? trigger.triggers.Count : 0;
                if (count > 0)
                {
                    result.Add(new UIComponentInfo
                    {
                        Component = trigger,
                        ComponentType = "EventTrigger",
                        ListenerCount = count,
                        TypeColor = Color.red
                    });
                }
            }
        }

        /// <summary>
        /// 获取指定类型的组件列表
        /// </summary>
        private List<T> FetchComponents<T>() where T : Component
        {
            if (panel == null) return new List<T>();
            return panel.GetComponentsInChildren<T>(true).ToList();
        }

        /// <summary>
        /// 获取清理动作列表
        /// </summary>
        private void FetchCleanupActions()
        {
            trackedCleanupActions.Clear();
            if (panel == null) return;

            var field = typeof(UIPanel).GetField("eventCleanupActions", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                var listObj = field.GetValue(panel) as List<Action>;
                if (listObj != null)
                    trackedCleanupActions.AddRange(listObj);
            }
        }
        #endregion

        #region 监听器计数方法
        private int GetListenerCount_Button(Button b)
        {
            if (b == null) return 0;
            return b.onClick.GetPersistentEventCount() + GetRuntimeListenerCount(b.onClick);
        }

        private int GetListenerCount_Toggle(Toggle t)
        {
            if (t == null) return 0;
            return t.onValueChanged.GetPersistentEventCount() + GetRuntimeListenerCount(t.onValueChanged);
        }

        private int GetListenerCount_Slider(Slider s)
        {
            if (s == null) return 0;
            return s.onValueChanged.GetPersistentEventCount() + GetRuntimeListenerCount(s.onValueChanged);
        }

        private int GetListenerCount_InputField(InputField i)
        {
            if (i == null) return 0;
            return i.onValueChanged.GetPersistentEventCount() + GetRuntimeListenerCount(i.onValueChanged) +
                   i.onEndEdit.GetPersistentEventCount() + GetRuntimeListenerCount(i.onEndEdit);
        }

        private int GetListenerCount_Dropdown(Dropdown d)
        {
            if (d == null) return 0;
            return d.onValueChanged.GetPersistentEventCount() + GetRuntimeListenerCount(d.onValueChanged);
        }

        private int GetListenerCount_ScrollRect(ScrollRect sr)
        {
            if (sr == null) return 0;
            return sr.onValueChanged.GetPersistentEventCount() + GetRuntimeListenerCount(sr.onValueChanged);
        }

        /// <summary>
        /// 通过反射获取运行时监听器数量
        /// </summary>
        private int GetRuntimeListenerCount(UnityEventBase unityEvent)
        {
            if (unityEvent == null) return 0;

            try
            {
                var field = typeof(UnityEventBase).GetField("m_Calls", BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    var callsObject = field.GetValue(unityEvent);
                    if (callsObject != null)
                    {
                        var runtimeCallsField = callsObject.GetType().GetField("m_RuntimeCalls", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (runtimeCallsField != null)
                        {
                            var runtimeCalls = runtimeCallsField.GetValue(callsObject) as System.Collections.IList;
                            return runtimeCalls?.Count ?? 0;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"获取运行时监听器数量失败: {e.Message}");
            }

            return 0;
        }
        #endregion

        #region 辅助功能方法
        /// <summary>
        /// 打开脚本文件
        /// </summary>
        private void OpenScript()
        {
            if (panel == null) return;

            MonoScript script = MonoScript.FromMonoBehaviour(panel);
            if (script != null)
            {
                AssetDatabase.OpenAsset(script);
            }
            else
            {
                Debug.LogWarning("无法找到脚本文件");
            }
        }

        /// <summary>
        /// 在项目窗口中定位并高亮当前面板脚本
        /// </summary>
        private void PingScriptFile()
        {
            if (panel == null)
            {
                Debug.LogWarning("面板为空，无法定位脚本");
                return;
            }

            var script = MonoScript.FromMonoBehaviour(panel);
            if (script == null)
            {
                Debug.LogWarning("无法获取面板脚本对象");
                return;
            }

            Selection.activeObject = script;
            EditorGUIUtility.PingObject(script);
        }

        /// <summary>
        /// 检查名称是否匹配搜索过滤器
        /// </summary>
        private bool IsMatchFilter(string name)
        {
            if (string.IsNullOrEmpty(searchFilter)) return true;
            return name.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 获取GameObject的层级路径
        /// </summary>
        private string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            while (parent != null && parent != panel.transform)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        /// <summary>
        /// 导出报告到控制台
        /// </summary>
        private void ExportReportToConsole(bool detailed = true)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== UIPanel事件报告: {panel.name} ===");
            sb.AppendLine($"Buttons: {FetchComponents<Button>().Count}");
            sb.AppendLine($"Toggles: {FetchComponents<Toggle>().Count}");
            sb.AppendLine($"Sliders: {FetchComponents<Slider>().Count}");
            sb.AppendLine($"InputFields: {FetchComponents<InputField>().Count}");
            sb.AppendLine($"Dropdowns: {FetchComponents<Dropdown>().Count}");
            sb.AppendLine($"ScrollRects: {FetchComponents<ScrollRect>().Count}");
            sb.AppendLine($"EventTriggers: {FetchComponents<EventTrigger>().Count}");

            if (detailed)
            {
                sb.AppendLine("--- 详细持久化监听 ---");
                AppendComponentDetails(sb);
            }

            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// 添加组件详细信息到报告
        /// </summary>
        private void AppendComponentDetails(System.Text.StringBuilder sb)
        {
            AppendDetail<Button>(sb, "Button", b => b.onClick);
            AppendDetail<Toggle>(sb, "Toggle", t => t.onValueChanged);
            AppendDetail<Slider>(sb, "Slider", s => s.onValueChanged);
            AppendDetail<InputField>(sb, "InputField(onValueChanged)", i => i.onValueChanged);
            AppendDetail<InputField>(sb, "InputField(onEndEdit)", i => i.onEndEdit);
            AppendDetail<Dropdown>(sb, "Dropdown", d => d.onValueChanged);
            AppendDetail<ScrollRect>(sb, "ScrollRect", sr => sr.onValueChanged);

            var triggers = FetchComponents<EventTrigger>();
            foreach (var tr in triggers)
            {
                sb.AppendLine($"EventTrigger: {tr.name} entries={(tr.triggers?.Count ?? 0)}");
                if (tr.triggers != null)
                {
                    foreach (var entry in tr.triggers)
                    {
                        int c = entry.callback.GetPersistentEventCount();
                        sb.AppendLine($"  - {entry.eventID} listeners={c}");
                        for (int i = 0; i < c; i++)
                        {
                            var targetObj = entry.callback.GetPersistentTarget(i);
                            var method = entry.callback.GetPersistentMethodName(i);
                            sb.AppendLine($"      [{i}] {targetObj?.name}.{method}()");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 添加组件详细信息
        /// </summary>
        private void AppendDetail<T>(System.Text.StringBuilder sb, string label, Func<T, UnityEventBase> getter)
            where T : Component
        {
            var comps = FetchComponents<T>();
            foreach (var c in comps)
            {
                var evt = getter(c);
                int count = evt.GetPersistentEventCount();
                sb.AppendLine($"{label}: {c.name} listeners={count}");
                for (int i = 0; i < count; i++)
                {
                    var targetObj = evt.GetPersistentTarget(i);
                    var method = evt.GetPersistentMethodName(i);
                    sb.AppendLine($"   [{i}] {targetObj?.name}.{method}()");
                }
            }
        }
        #endregion
    }
}