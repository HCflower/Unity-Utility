# FFramework UISystem 使用文档

## 概述

一个简洁高效的 Unity UI 管理框架，提供面板管理、事件绑定、组件查找和层级控制功能。

### 特性

- **简单面板管理** - 一行代码打开/关闭面板
- **自动事件清理** - 防止内存泄漏，无需手动管理
- **多层级支持** - 6 个 UI 层级，自动管理栈和锁定
- **便捷组件查找** - 通过名称快速获取 UI 组件
- **智能缓存** - 可选面板缓存，提高性能


---

## 快速开始

### 1. 初始化 UIRoot

```csharp
// 场景中添加UIRoot组件，右键菜单选择"创建UI层级"
```

### 2. 创建面板

```csharp
public class MainPanel : UIPanel
{
    protected override void Initialize()
    {
        this.BindButton("PlayBtn", OnPlay);
        this.BindButton("SettingsBtn", () => UISystem.Instance.OpenPanel<SettingsPanel>());
    }

    private void OnPlay() => UISystem.Instance.OpenPanel<GamePanel>();
}
```

### 3. 使用面板

```csharp
UISystem.Instance.OpenPanel<MainPanel>();
UISystem.Instance.ClosePanel<MainPanel>();
```


---

## 核心 API

### 面板管理

```csharp
// 打开面板
UISystem.Instance.OpenPanel<T>(UILayer layer = UILayer.ContentLayer, bool useCache = true);
UISystem.Instance.OpenPanel<T>(GameObject prefab, UILayer layer, bool useCache = true);

// 关闭面板
UISystem.Instance.ClosePanel<T>();
UISystem.Instance.CloseCurrentPanel();

// 获取面板
T panel = UISystem.Instance.GetPanel<T>();
T topPanel = UISystem.Instance.GetTopPanel<T>();

// 检查状态
bool isOpen = UISystem.Instance.IsCurrentPanel<T>();
bool hasOpenPanels = UISystem.Instance.HasOpenPanels;
string currentName = UISystem.Instance.CurrentPanelName;

// 批量管理
UISystem.Instance.ClearAllPanels(destroyGameObjects: true);
UISystem.Instance.ClearPanelsInLayer(UILayer.PopupLayer);
int count = UISystem.Instance.GetActivePanelCountInLayer(UILayer.ContentLayer);
```

### 事件绑定

```csharp
protected override void Initialize()
{
    // 基础UI组件
    this.BindButton("StartBtn", OnStart);
    this.BindToggle("SoundToggle", OnSoundToggle);
    this.BindSlider("VolumeSlider", OnVolumeChange);
    this.BindInputField("NameInput", OnNameChanged);
    this.BindDropdown("QualityDropdown", OnQualityChanged);

    // TextMeshPro组件
    this.BindTMPInputField("TMPInput", OnTMPInputChanged);
    this.BindTMPDropdown("TMPDropdown", OnTMPDropdownChanged);

    // 直接组件绑定
    Button btn = GetButton("DirectBtn");
    btn.BindClick(OnDirectClick, this);

    // 批量绑定
    this.BindButtons(new Dictionary<string, UnityAction>
    {
        ["Btn1"] = OnBtn1,
        ["Btn2"] = OnBtn2
    });
}
```

### 组件获取

```csharp
// 基础组件
Button btn = this.GetButton("ButtonName");
Toggle toggle = this.GetToggle("ToggleName");
Slider slider = this.GetSlider("SliderName");
InputField input = this.GetInputField("InputName");
Dropdown dropdown = this.GetDropdown("DropdownName");
Image image = this.GetImage("ImageName");
Text text = this.GetText("TextName");

// TextMeshPro组件
TextMeshProUGUI tmpText = this.GetTMPText("TMPTextName");
TMP_InputField tmpInput = this.GetTMPInputField("TMPInputName");
TMP_Dropdown tmpDropdown = this.GetTMPDropdown("TMPDropdownName");

// 通用获取
T component = this.GetComponent<T>("ComponentName");
T[] allComponents = this.GetAllComponents<T>();
T firstComponent = this.GetFirstComponent<T>();
```

### 便捷设置

```csharp
// 文本设置
this.SetText("ScoreText", "Score: 1000");
this.SetTMPText("TMPText", "Hello World");

// 组件状态
this.SetButtonInteractable("StartBtn", false);
this.SetToggleValue("SoundToggle", true, sendCallback: false);
this.SetSliderValue("VolumeSlider", 0.8f, sendCallback: true);

// 图片设置
this.SetImageSprite("Icon", newSprite);
this.SetImageColor("Background", Color.red);

// 通用属性设置
this.SetProperty<Button>("MyBtn", btn => btn.interactable = false);
```


---

## UI 层级系统

```csharp
public enum UILayer
{
    BackgroundLayer,      // 背景层 - 静态背景
    PostProcessingLayer,  // 后期处理层 - UI特效
    ContentLayer,         // 内容层 - 主要功能（默认）
    PopupLayer,          // 弹窗层 - 消息对话框
    GuideLayer,          // 引导层 - 教程引导
    DebugLayer           // 调试层 - 开发调试
}
```

**层级特性：**

- `PopupLayer`和 `PostProcessingLayer`不会锁定下层面板
- 其他层级打开时会自动锁定下层面板交互
- 面板按层级顺序渲染

```csharp
// 在不同层级打开面板
UISystem.Instance.OpenPanel<MainMenuPanel>(UILayer.ContentLayer);
UISystem.Instance.OpenPanel<MessageDialog>(UILayer.PopupLayer);
UISystem.Instance.OpenPanel<TutorialPanel>(UILayer.GuideLayer);
```


---

## 面板生命周期

```csharp
public class ExamplePanel : UIPanel
{
    // Unity生命周期
    protected override void OnAwake() { }
    protected override void OnStart() { }

    // 面板生命周期
    protected override void Initialize()
    {
        // 初始化，只调用一次
        // 在这里绑定事件
    }

    protected override void OnShow() { /* 显示时 */ }
    protected override void OnHide() { /* 隐藏时 */ }
    protected override void OnLockPanel() { /* 锁定时 */ }
    protected override void OnUnlockPanel() { /* 解锁时 */ }
    protected override void OnPanelEnable() { /* 启用时 */ }
    protected override void OnPanelDisable() { /* 禁用时 */ }
    protected override void OnPanelDestroy() { /* 销毁时 */ }
}
```

### 面板控制

```csharp
// 基础控制
panel.Show();
panel.Hide();
panel.Close(); // Hide的别名

// 锁定控制
panel.OnLock();   // 禁用交互
panel.OnUnLock(); // 启用交互

// 属性设置
panel.SetAlpha(0.5f);
panel.SetInteractable(false);
panel.SetBlocksRaycasts(true);

// 状态查询
bool isInit = panel.IsInitialized;
bool isShowing = panel.IsShowing;
bool isLocked = panel.IsLocked;
UILayer layer = panel.Layer;
```


---

## 事件管理

### 自动事件清理

```csharp
// 自动追踪（推荐，默认开启）
this.BindButton("Btn", OnClick); // autoTrack = true

// 手动管理
this.BindButton("Btn", OnClick, autoTrack: false);

// 清理操作
this.ClearTrackedEvents();    // 清理追踪的事件
this.UnbindAllEvents();       // 清理所有UI事件（别名）
this.ClearAllEvents();        // 清理所有UI事件
```

### 事件追踪

```csharp
// 手动添加清理动作
this.AddEventCleanup(() => SomeAction(), "ComponentName");
this.RemoveEventCleanup(cleanupAction);

// 查看事件数量
int eventCount = this.EventCount;
```


---

## 组件查找系统

### UISystem 组件查找

```csharp
// 基础查找
GameObject obj = UISystem.Instance.FindChildGameObject(parentObj, "ChildName");
T component = UISystem.Instance.GetChildComponent<T>(parentObj, "ObjectName");

// 批量查找
T[] allComponents = UISystem.Instance.GetAllChildComponents<T>(parentObj);
T firstComponent = UISystem.Instance.GetFirstChildComponent<T>(parentObj);

// 路径查找（支持"Parent/Child"格式）
GameObject deepChild = UISystem.Instance.FindChildGameObject(parentObj, "Parent/Child/GrandChild");
```


---

## 高级用法

### 预制体面板

```csharp
// 从预制体创建面板
GameObject prefab = Resources.Load<GameObject>("UI/CustomPanel");
UISystem.Instance.OpenPanel<CustomPanel>(prefab, UILayer.ContentLayer);
```

### 组件缓存优化

```csharp
public class OptimizedPanel : UIPanel
{
    private Button[] actionButtons;
    private Text statusText;

    protected override void Initialize()
    {
        // 缓存常用组件
        actionButtons = this.GetAllComponents<Button>();
        statusText = this.GetText("StatusText");

        // 批量绑定
        for (int i = 0; i < actionButtons.Length; i++)
        {
            int index = i; // 闭包捕获
            actionButtons[i].BindClick(() => OnActionClick(index), this);
        }
    }

    private void OnActionClick(int index) => Debug.Log($"按钮 {index} 被点击");
}
```

### 静态访问（兼容）

```csharp
// 静态属性
int openCount = UISystem.S_OpenPanelCount;
bool hasOpen = UISystem.S_HasOpenPanels;
UIPanel current = UISystem.S_CurrentPanel;
string currentName = UISystem.S_CurrentPanelName;
```


---

## 调试工具

### 面板调试

```csharp
// 右键菜单调试方法
[ContextMenu("打印面板状态")]
panel.PrintPanelStatus();

[ContextMenu("强制清理事件")]
panel.ForceCleanupEvents();
```

### 日志输出

系统会自动输出彩色日志：

- 🟢 显示面板
- 🟡 隐藏面板
- 🟠 锁定面板
- 🔵 解锁面板


---

## 文件结构

```
Assets/
├── Resources/UI/          # UI预制体（名称需与类名一致）
│   ├── MainPanel.prefab
│   └── SettingsPanel.prefab
└── Scripts/UI/            # UI脚本
    ├── MainPanel.cs
    └── SettingsPanel.cs
```


---

## 最佳实践

### ✅ 推荐做法

```csharp
// 1. 使用自动事件追踪
this.BindButton("Btn", OnClick);

// 2. 缓存常用组件
private Button startBtn;
protected override void Initialize()
{
    startBtn = this.GetButton("StartBtn");
}

// 3. 合理使用层级
UISystem.Instance.OpenPanel<MessageDialog>(UILayer.PopupLayer);

// 4. 批量操作
this.BindButtons(buttonEvents);
```

### ❌ 避免做法

```csharp
// 1. 禁用自动追踪（除非特殊需求）
this.BindButton("Btn", OnClick, autoTrack: false);

// 2. 重复查找组件
this.GetButton("Btn").interactable = false; // 每次都查找

// 3. 忘记预制体命名
// 预制体名 ≠ 类名会导致加载失败
```


---

## 常见问题

**Q: 面板打开失败？**
A: 检查 `Resources/UI/`路径和预制体名称是否与类名一致

**Q: 事件重复触发？**
A: 确保使用 `autoTrack=true`或手动清理事件

**Q: 找不到组件？**
A: 检查 GameObject 名称拼写和层级结构

**Q: 面板层级问题？**
A: 理解层级锁定机制，合理选择面板层级


---

## 总结

FFramework UISystem 核心思想：


1. **继承 UIPanel** → 实现 Initialize 方法
2. **this.BindXXX** → 绑定 UI 事件
3. **UISystem.Instance** → 管理面板
4. **自动清理** → 无需担心内存泄漏

简单、高效、可靠！🚀