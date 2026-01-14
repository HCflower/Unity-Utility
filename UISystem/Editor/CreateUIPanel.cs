// =============================================================
// 描述：创建UI面板
// 作者：HCFlower
// 创建时间：2025-12-13 12:00:00
// 版本：1.0.2
// =============================================================
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FFramework.Editor
{
    public class CreateUIPanel : EditorWindow
    {
        // 默认保存位置（相对 Assets）
        private const string DefaultRelativeSavePath = "Assets/Game/Scripts/ViewController/UI";
        // 默认作者名称（可以从 EditorPrefs 中读取）
        private const string AuthorNameKey = "FFramework.UI.AuthorName";

        // UI Elements
        private TextField namespaceField;
        private TextField panelNameField;
        private TextField authorField;
        private TextField savePathField;
        private Button browseButton;
        private Button createButton;
        private Button resetButton;
        private Label previewLabel;
        private HelpBox statusHelp;

        [MenuItem("FFramework/创建UI面板", priority = 0)]
        public static void OpenWindow()
        {
            var window = GetWindow<CreateUIPanel>(false, "创建UI面板");
            window.minSize = new Vector2(480, 300);
            window.Show();
        }

        public void CreateGUI()
        {
            // 创建根容器
            var root = rootVisualElement;
            root.style.paddingTop = 5;
            root.style.paddingBottom = 5;
            root.style.paddingLeft = 5;
            root.style.paddingRight = 5;

            // 添加样式
            var styleSheet = Resources.Load<StyleSheet>("CreateUIPanelStyle");
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }

            // 标题区域
            var titleContainer = new VisualElement();
            titleContainer.AddToClassList("title-container");

            var titleLabel = new Label("创建 UI 面板");
            titleLabel.AddToClassList("title-label");
            titleContainer.Add(titleLabel);

            var helpBox = new HelpBox("请输入命名空间、面板名称、作者名称和保存位置。创建前将检查是否存在同名脚本。", HelpBoxMessageType.Info);
            titleContainer.Add(helpBox);

            root.Add(titleContainer);

            // 输入区域
            var inputContainer = new VisualElement();
            inputContainer.AddToClassList("input-container");

            // 命名空间输入
            namespaceField = new TextField("命名空间:");
            namespaceField.value = "Game.UI";
            namespaceField.RegisterValueChangedCallback(OnInputChanged);
            inputContainer.Add(namespaceField);

            // 面板名称输入
            panelNameField = new TextField("面板名称:");
            panelNameField.value = "ExamplePanel";
            panelNameField.RegisterValueChangedCallback(OnInputChanged);
            inputContainer.Add(panelNameField);

            // 作者名称输入
            authorField = new TextField("作者名称:");
            authorField.value = EditorPrefs.GetString(AuthorNameKey, "HCFlower");
            authorField.RegisterValueChangedCallback(OnAuthorChanged);
            inputContainer.Add(authorField);

            // 保存路径输入
            var pathContainer = new VisualElement();
            pathContainer.style.flexDirection = FlexDirection.Row;

            savePathField = new TextField("保存位置:");
            savePathField.value = DefaultRelativeSavePath;
            savePathField.style.flexGrow = 1;
            savePathField.RegisterValueChangedCallback(OnInputChanged);

            browseButton = new Button(() => SelectSavePath()) { text = "浏览" };
            browseButton.style.width = 80;
            browseButton.style.marginLeft = 5;

            pathContainer.Add(savePathField);
            pathContainer.Add(browseButton);
            inputContainer.Add(pathContainer);

            root.Add(inputContainer);

            // 预览区域
            var previewContainer = new VisualElement();
            previewContainer.AddToClassList("preview-container");

            var previewTitle = new Label("文件预览");
            previewTitle.AddToClassList("section-title");
            previewContainer.Add(previewTitle);

            previewLabel = new Label();
            previewLabel.AddToClassList("preview-label");
            previewContainer.Add(previewLabel);

            statusHelp = new HelpBox("", HelpBoxMessageType.Info);
            statusHelp.style.marginTop = 5;
            previewContainer.Add(statusHelp);

            root.Add(previewContainer);

            // 按钮区域
            var buttonContainer = new VisualElement();
            buttonContainer.AddToClassList("button-container");

            resetButton = new Button(() => ResetToDefault()) { text = "重置为默认" };
            resetButton.AddToClassList("secondary-button");

            createButton = new Button(() => TryCreatePanel()) { text = "创建面板" };
            createButton.AddToClassList("primary-button");

            buttonContainer.Add(resetButton);
            buttonContainer.Add(createButton);

            root.Add(buttonContainer);

            // 初始更新预览
            UpdatePreview();
        }

        private void OnInputChanged(ChangeEvent<string> evt)
        {
            UpdatePreview();
        }

        private void OnAuthorChanged(ChangeEvent<string> evt)
        {
            // 保存作者名称到 EditorPrefs，下次打开时自动填充
            EditorPrefs.SetString(AuthorNameKey, evt.newValue);
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (previewLabel == null || statusHelp == null) return;

            string panelName = panelNameField?.value ?? "";
            string savePath = savePathField?.value ?? "";
            string author = authorField?.value ?? "";

            if (string.IsNullOrWhiteSpace(panelName))
            {
                previewLabel.text = "请输入面板名称";
                statusHelp.text = "面板名称不能为空";
                statusHelp.messageType = HelpBoxMessageType.Error;
                createButton.SetEnabled(false);
                return;
            }

            if (!IsValidCSharpIdentifier(panelName))
            {
                previewLabel.text = "无效的面板名称";
                statusHelp.text = "面板名称必须是有效的 C# 标识符";
                statusHelp.messageType = HelpBoxMessageType.Error;
                createButton.SetEnabled(false);
                return;
            }

            if (string.IsNullOrWhiteSpace(author))
            {
                statusHelp.text = "建议填写作者名称";
                statusHelp.messageType = HelpBoxMessageType.Warning;
            }

            string filename = $"{panelName}.cs";
            string fullPath = PathCombine(savePath, filename);

            previewLabel.text = $"文件名: {filename}\n完整路径: {fullPath}\n作者: {(string.IsNullOrWhiteSpace(author) ? "未填写" : author)}";

            // 检查文件是否存在
            bool fileExists = AssetDatabase.LoadAssetAtPath<TextAsset>(fullPath) != null || File.Exists(fullPath);
            if (fileExists)
            {
                statusHelp.text = "文件已存在，将无法创建";
                statusHelp.messageType = HelpBoxMessageType.Warning;
                createButton.SetEnabled(false);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(author))
                {
                    statusHelp.text = "可以创建（建议填写作者名称）";
                    statusHelp.messageType = HelpBoxMessageType.Warning;
                }
                else
                {
                    statusHelp.text = "可以创建";
                    statusHelp.messageType = HelpBoxMessageType.Info;
                }
                createButton.SetEnabled(true);
            }
        }

        private void SelectSavePath()
        {
            string selected = EditorUtility.OpenFolderPanel("选择保存位置（位于 Assets 下）", Application.dataPath, "");
            if (!string.IsNullOrEmpty(selected))
            {
                string assetsPath = Application.dataPath.Replace('\\', '/');
                selected = selected.Replace('\\', '/');

                if (selected.StartsWith(assetsPath))
                {
                    // 转换为相对路径：Assets/...
                    savePathField.value = "Assets" + selected.Substring(assetsPath.Length);
                    UpdatePreview();
                }
                else
                {
                    EditorUtility.DisplayDialog("路径错误", "请选择位于项目 Assets 目录下的路径。", "确定");
                }
            }
        }

        private void ResetToDefault()
        {
            savePathField.value = DefaultRelativeSavePath;
            // 重置作者名称为默认值或 EditorPrefs 中保存的值
            authorField.value = EditorPrefs.GetString(AuthorNameKey, "HCFlower");
            UpdatePreview();
        }

        private void TryCreatePanel()
        {
            string namespaceName = namespaceField.value;
            string panelName = panelNameField.value;
            string author = authorField.value;
            string savePath = savePathField.value;

            // 基本校验
            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                EditorUtility.DisplayDialog("输入错误", "命名空间不能为空", "确定");
                return;
            }
            if (string.IsNullOrWhiteSpace(panelName))
            {
                EditorUtility.DisplayDialog("输入错误", "面板名称不能为空", "确定");
                return;
            }
            if (!IsValidCSharpIdentifier(panelName))
            {
                EditorUtility.DisplayDialog("输入错误", "面板名称不是有效的 C# 标识符", "确定");
                return;
            }
            if (string.IsNullOrWhiteSpace(savePath))
            {
                savePath = DefaultRelativeSavePath;
            }
            if (!savePath.Replace('\\', '/').StartsWith("Assets"))
            {
                EditorUtility.DisplayDialog("路径错误", "保存位置必须在 Assets 目录下", "确定");
                return;
            }

            // 如果作者名称为空，显示确认对话框
            if (string.IsNullOrWhiteSpace(author))
            {
                if (!EditorUtility.DisplayDialog("确认创建", "作者名称为空，确定要继续创建吗？", "继续创建", "取消"))
                {
                    return;
                }
                author = "Unknown";
            }

            string targetDir = savePath.Replace('\\', '/');
            string targetFile = PathCombine(targetDir, $"{panelName}.cs");

            // 检查是否已存在
            if (AssetDatabase.LoadAssetAtPath<TextAsset>(targetFile) != null || File.Exists(targetFile))
            {
                EditorUtility.DisplayDialog("创建失败", $"文件已存在：\n{targetFile}", "确定");
                return;
            }

            // 确保目录存在
            EnsureDirectory(targetDir);

            // 写入模板
            string content = GeneratePanelTemplate(namespaceName.Trim(), panelName.Trim(), author.Trim());
            File.WriteAllText(targetFile, content);

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("创建成功", $"已创建：\n{targetFile}", "确定");

            // 选中并高亮新建文件
            var createdAsset = AssetDatabase.LoadAssetAtPath<Object>(targetFile);
            if (createdAsset != null)
            {
                Selection.activeObject = createdAsset;
                EditorGUIUtility.PingObject(createdAsset);
            }

            // 关闭窗口
            Close();
        }

        private static string PathCombine(string a, string b)
        {
            a = a.Replace('\\', '/');
            b = b.Replace('\\', '/');
            if (a.EndsWith("/")) return a + b;
            return a + "/" + b;
        }

        private static void EnsureDirectory(string assetRelativeDir)
        {
            assetRelativeDir = assetRelativeDir.Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(assetRelativeDir))
            {
                // 逐级创建文件夹
                string[] parts = assetRelativeDir.Split('/');
                if (parts.Length > 0 && parts[0] == "Assets")
                {
                    string current = "Assets";
                    for (int i = 1; i < parts.Length; i++)
                    {
                        string next = current + "/" + parts[i];
                        if (!AssetDatabase.IsValidFolder(next))
                        {
                            AssetDatabase.CreateFolder(current, parts[i]);
                        }
                        current = next;
                    }
                }
            }
        }

        private static bool IsValidCSharpIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (!(char.IsLetter(name[0]) || name[0] == '_')) return false;
            for (int i = 1; i < name.Length; i++)
            {
                char c = name[i];
                if (!(char.IsLetterOrDigit(c) || c == '_')) return false;
            }
            return true;
        }

        private static string GeneratePanelTemplate(string ns, string className, string author)
        {
            // 生成继承 UIPanel 的模板代码
            return
$@"// =============================================================
// 描述：{className} UI面板
// 作者：{author}
// 创建时间：{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}
// 版本：1.0.0
// =============================================================
using UnityEngine;
using FFramework.Utility;
    
namespace {ns}
{{
    public class {className} : UIPanel
    {{
        #region 字段

        #endregion

        /// <summary>面板初始化 - 只调用一次</summary>
        protected override void Initialize()
        {{
            // 初始化逻辑
            // 例如：绑定按钮事件、查找子节点等
        }}

        protected override void OnPanelEnable()
        {{
            // 打开面板逻辑
        }}
    }}
}}";
        }
    }
}