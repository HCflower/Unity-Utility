# FFramework UISystem 使用文档（重写版）

## 1. 概述

`UISystem` 是一个集成了 **UI 根节点管理** 与 **面板栈管理** 的统一 UI 框架，包含：

- 面板打开 / 关闭 / 栈管理
- 多 UI 层级与锁定机制
- 统一组件查找、事件绑定与自动清理
- Canvas / CanvasScaler / EventSystem 自动构建

核心类：

- `UISystem`：全局 UI 管理器（继承 `SingletonMono<UISystem>`）
- `UIPanel`：所有 UI 面板基类
- `UIEventExtensions`：UI 获取 / 绑定 / 设置 的扩展方法


---

## 2. 快速开始

### 2.1 场景初始化 `UISystem`

```csharp
// 在场景中新建空对象，然后添加 UISystem 组件
// 在 Inspector 右键菜单选择："初始化UI根节点"
```

该操作会自动完成：

- 为 `UISystem` GameObject 添加：
  - `Canvas`（`ScreenSpaceOverlay`）
  - `CanvasScaler`（参考分辨率 `1920x1080`）
  - `GraphicRaycaster`
- 创建并挂载 6 个层级节点（`RectTransform` 全屏拉伸）：
  - `BackgroundLayer`
  - `PostProcessingLayer`
  - `ContentLayer`
  - `PopupLayer`
  - `GuideLayer`
  - `DebugLayer`
- 在场景中创建唯一的 `EventSystem + StandaloneInputModule`

> 建议将带有 `UISystem` 的对象放在常驻场景中。


---

### 2.2 创建一个面板

```csharp
using FFramework.Utility;
using UnityEngine.UI;
using UnityEngine;

public class MainPanel : UIPanel
{
    private Button playButton;
    private Button settingsButton;

    protected override void Initialize()
    {
        // 初始化只调用一次，适合缓存组件与绑定事件
        playButton = this.GetButton("PlayBtn");
        settingsButton = this.GetButton("SettingsBtn");

        playButton.BindClick(OnPlay, this);
        settingsButton.BindClick(() => UISystem.Instance.OpenPanel<SettingsPanel>(), this);
    }

    private void OnPlay()
    {
        UISystem.Instance.OpenPanel<GamePanel>();
    }

    protected override void OnShow()
    {
        Debug.Log("MainPanel 显示");
    }

    protected override void OnHide()
    {
        Debug.Log("MainPanel 隐藏");
    }
}
```

**预制体要求：**

- 路径：`Assets/Resources/UI/MainPanel.prefab`
- 预制体名必须与类名 `MainPanel` 一致


---

### 2.3 打开 / 关闭面板

```csharp
// 打开默认内容层（ContentLayer），使用缓存
UISystem.Instance.OpenPanel<MainPanel>();

// 在弹窗层打开 MessageDialog
UISystem.Instance.OpenPanel<MessageDialog>(UILayer.PopupLayer);

// 从自定义预制体打开（不走 Resources 名称）
GameObject customPrefab = Resources.Load<GameObject>("UI/CustomPanel");
UISystem.Instance.OpenPanel<CustomPanel>(customPrefab, UILayer.ContentLayer);

// 关闭指定类型面板
UISystem.Instance.ClosePanel<MainPanel>();

// 关闭当前栈顶面板
UISystem.Instance.CloseCurrentPanel();
```


---

## 3. 面板管理 API

### 3.1 打开面板

```csharp
// 从 Resources/UI/{类型名}.prefab 加载
T UISystem.Instance.OpenPanel<T>(
    UILayer layer = UILayer.ContentLayer,
    bool useCache = true
) where T : UIPanel;

// 从指定预制体实例化
T UISystem.Instance.OpenPanel<T>(
    GameObject prefab,
    UILayer layer = UILayer.ContentLayer,
    bool useCache = true
) where T : UIPanel;
```

说明：

- `layer`：面板打开所在的 UI 层级
- `useCache = true`：
  - 首次打开：实例化并加入缓存
  - 再次打开：如果缓存中存在且未激活则复用实例
- 对需要“锁定下层交互”的层（除 `PopupLayer`、`PostProcessingLayer` 外）会自动锁定当前栈顶面板。

### 3.2 关闭 & 查询面板

```csharp
// 关闭当前栈顶面板
UISystem.Instance.CloseCurrentPanel();

// 关闭指定类型面板
UISystem.Instance.ClosePanel<MainPanel>();

// 获取已打开的指定类型面板（activeInHierarchy 时返回）
MainPanel panel = UISystem.Instance.GetPanel<MainPanel>();

// 获取当前栈顶面板（强转为 T）
MainPanel topPanel = UISystem.Instance.GetTopPanel<MainPanel>();

// 当前面板是否为某类型
bool isMain = UISystem.Instance.IsCurrentPanel<MainPanel>();
```

### 3.3 批量管理

```csharp
// 清理所有面板（可选是否销毁 GameObject）
UISystem.Instance.ClearAllPanels(destroyGameObjects: true);

// 清理指定层级的所有面板
UISystem.Instance.ClearPanelsInLayer(UILayer.PopupLayer, destroyGameObjects: true);

// 获取指定层级的激活面板数量
int count = UISystem.Instance.GetActivePanelCountInLayer(UILayer.ContentLayer);

// 属性
int    openCount  = UISystem.Instance.OpenPanelCount;
int    cacheCount = UISystem.Instance.CachedPanelCount;
bool   hasOpen    = UISystem.Instance.HasOpenPanels;
UIPanel current   = UISystem.Instance.CurrentPanel;
string currentName = UISystem.Instance.CurrentPanelName;
string currentType = UISystem.Instance.CurrentPanelTypeName;
```


---

## 4. UI 层级系统

### 4.1 枚举定义

```csharp
public enum UILayer
{
    BackgroundLayer,      // 背景层 - 静态背景
    PostProcessingLayer,  // 后期处理层 - UI 后期特效
    ContentLayer,         // 内容层 - 主要功能（默认）
    PopupLayer,           // 弹窗层 - 消息对话框
    GuideLayer,           // 引导层 - 教程引导
    DebugLayer            // 调试层 - 开发调试
}
```

### 4.2 层级特性

- `PopupLayer` 和 `PostProcessingLayer` **不会锁定** 下层面板交互
- 其他层级打开新面板时，会对当前栈顶面板调用 `OnLock()` 禁用交互
- 面板按层级节点顺序渲染

```csharp
// 在不同层级打开面板
UISystem.Instance.OpenPanel<MainMenuPanel>(UILayer.ContentLayer);
UISystem.Instance.OpenPanel<MessageDialog>(UILayer.PopupLayer);
UISystem.Instance.OpenPanel<TutorialPanel>(UILayer.GuideLayer);
```


---

## 5. UIPanel 基类

### 5.1 生命周期

```csharp
public class ExamplePanel : UIPanel
{
    // Unity 生命周期
    protected override void OnAwake() { }
    protected override void OnStart() { }

    // 面板初始化（只调用一次）
    protected override void Initialize()
    {
        // 缓存组件 + 绑定事件
    }

    // 显示/隐藏
    protected override void OnShow() { }
    protected override void OnHide() { }

    // 锁定/解锁
    protected override void OnLockPanel() { }
    protected override void OnUnlockPanel() { }

    // 启用/禁用
    protected override void OnPanelEnable() { }
    protected override void OnPanelDisable() { }

    // 销毁
    protected override void OnPanelDestroy() { }
}
```

### 5.2 面板控制

```csharp
// 显示 / 隐藏
panel.Show();    // 设置 active=true，调用 OnShow()
panel.Hide();    // 设置 active=false，调用 OnHide()
panel.Close();   // Hide 的别名

// 锁定控制
panel.OnLock();    // 禁用交互（blocksRaycasts = false）
panel.OnUnLock();  // 启用交互（视显示状态恢复 blocksRaycasts）

// 属性设置
panel.SetAlpha(0.5f);
panel.SetInteractable(false);
panel.SetBlocksRaycasts(true);

// 状态查询
bool isInit    = panel.IsInitialized;
bool isShowing = panel.IsShowing;
bool isLocked  = panel.IsLocked;
UILayer layer  = panel.Layer;
```

> `UIPanel` 内部自动确保存在 `CanvasGroup` 组件，无需手动添加。


---

## 6. 事件管理

### 6.1 自动事件清理

`UIEventExtensions` 通过 `autoTrack` 与 `UIPanel.AddEventCleanup` 实现自动清理：

```csharp
// 自动追踪（推荐，默认 autoTrack = true）
this.BindButton("Btn", OnClick);

// 手动关闭自动追踪（需要自己清理）
this.BindButton("Btn", OnClick, autoTrack: false);

// 手动清理追踪事件
this.ClearTrackedEvents();     // 执行并清空追踪的 cleanup

// 清理所有 UI 事件（扩展方法）
this.UnbindAllEvents();        // 别名：ClearAllEvents
this.ClearAllEvents();
```

### 6.2 事件追踪 API

```csharp
// 手动添加清理动作
this.AddEventCleanup(() => SomeAction(), "ComponentName");

// 手动移除
this.RemoveEventCleanup(cleanupAction);

// 查看事件数量
int eventCount = this.EventCount;
```


---

## 7. 组件获取与设置

### 7.1 组件获取

```csharp
// 通用获取
T component     = this.GetComponent<T>("ObjectName");

// 常用 UI 组件
Button btn      = this.GetButton("ButtonName");
Toggle toggle   = this.GetToggle("ToggleName");
Slider slider   = this.GetSlider("SliderName");
InputField input= this.GetInputField("InputName");
Dropdown dropdown = this.GetDropdown("DropdownName");
Image image     = this.GetImage("ImageName");
Text text       = this.GetText("TextName");

// TextMeshPro 组件
TextMeshProUGUI tmpText = this.GetTMPText("TMPTextName");
TMP_InputField tmpInput = this.GetTMPInputField("TMPInputName");
TMP_Dropdown tmpDropdown = this.GetTMPDropdown("TMPDropdownName");

// 获取所有 / 第一个组件
T[] allComponents   = this.GetAllComponents<T>();
T firstComponent    = this.GetFirstComponent<T>();
```

> 名称参数支持路径：`"Parent/Child/GrandChild"`。

### 7.2 事件绑定

```csharp
public Button testButton;
public Toggle testToggle;
public Slider testSlider;

protected override void Initialize()
{
    // 基础 UI 组件（通过名称）
    this.BindButton("StartBtn", OnStart);
    this.BindToggle("SoundToggle", OnSoundToggle);
    this.BindSlider("VolumeSlider", OnVolumeChange);
    this.BindInputField("NameInput", OnNameChanged);
    this.BindDropdown("QualityDropdown", OnQualityChanged);

    // TextMeshPro 组件
    this.BindTMPInputField("TMPInput", OnTMPInputChanged);
    this.BindTMPDropdown("TMPDropdown", OnTMPDropdownChanged);

    // 直接组件绑定
    testButton.BindClick(() => { }, this);
    testToggle.BindValueChanged((value) => { }, this);
    testSlider.BindValueChanged((value) => { }, this);

    Button btn = GetButton("DirectBtn");
    btn.BindClick(OnDirectClick, this);

    // 批量绑定按钮
    this.BindButtons(new Dictionary<string, UnityEngine.Events.UnityAction>
    {
        ["Btn1"] = OnBtn1,
        ["Btn2"] = OnBtn2
    });
}
```

### 7.3 便捷设置

```csharp
// 文本设置
this.SetText("ScoreText", "Score: 1000");
this.SetTMPText("TMPText", "Hello World");

// 组件状态
this.SetButtonInteractable("StartBtn", false);
this.SetToggleValue("SoundToggle", true,  sendCallback: false);
this.SetSliderValue("VolumeSlider", 0.8f, sendCallback: true);

// 图片设置
this.SetImageSprite("Icon", newSprite);
this.SetImageColor("Background", Color.red);

// 通用属性设置
this.SetProperty<Button>("MyBtn", btn => btn.interactable = false);
```


---

## 8. 组件查找系统（UISystem 内部）

`UIEventExtensions` 的查找最终由 `UISystem` 完成：

```csharp
// 根据名称或路径查找子物体 GameObject
GameObject obj = UISystem.Instance.FindChildGameObject(parentObj, "ChildName");
GameObject deepChild = UISystem.Instance.FindChildGameObject(parentObj, "Parent/Child/GrandChild");

// 获取子物体组件
T component = UISystem.Instance.GetChildComponent<T>(parentObj, "ObjectName");
```

一般业务层直接使用 `UIPanel` 上的 `GetComponent<T>` / `GetButton` 等扩展即可。


---

## 9. 高级用法

### 9.1 预制体面板

```csharp
// 从预制体创建面板
GameObject prefab = Resources.Load<GameObject>("UI/CustomPanel");
UISystem.Instance.OpenPanel<CustomPanel>(prefab, UILayer.ContentLayer);
```

### 9.2 组件缓存优化

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

    private void OnActionClick(int index)
    {
        Debug.Log($"按钮 {index} 被点击");
        statusText.text = $"当前点击：{index}";
    }
}
```

### 9.3 调试工具

```csharp
// 右键菜单调试方法
[ContextMenu("打印面板状态")]
public void PrintPanelStatus();

[ContextMenu("强制清理事件")]
public void ForceCleanupEvents();
```

控制台会输出彩色日志：

- 绿色：显示面板
- 黄色：隐藏面板
- 橙色：锁定面板
- 青色：解锁面板


---

## 10. 文件结构建议

```text
Assets/
├── Resources/UI/          # UI 预制体（名称需与类名一致）
│   ├── MainPanel.prefab
│   └── SettingsPanel.prefab
└── Scripts/UI/            # UI 脚本
    ├── MainPanel.cs
    └── SettingsPanel.cs
```


---

## 11. 最佳实践

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
// 1. 禁用自动追踪（除非有特殊需求）
this.BindButton("Btn", OnClick, autoTrack: false);

// 2. 重复查找组件
this.GetButton("Btn").interactable = false; // 每次都查找

// 3. 忘记预制体命名
// 预制体名 ≠ 类名会导致加载失败
```


---

## 12. 常见问题

**Q: 面板打开失败？**\
A: 检查 `Resources/UI/` 路径和预制体名称是否与类名一致，且预制体上挂载了对应 `UIPanel` 子类。

**Q: 事件重复触发？**\
A: 确保没有重复调用 `BindXXX`；推荐使用 `autoTrack = true` 并依赖销毁时自动清理。

**Q: 找不到组件？**\
A: 检查 GameObject 名称与层级结构；必要时使用路径形式 `"Parent/Child"`。

**Q: 面板层级/交互异常？**\
A: 查看 `UILayer` 与锁定规则，确认当前面板打开的层级是否会锁定下层。


---

## 13. 总结

FFramework UISystem 的核心使用方式：


1. 在场景中添加 `UISystem`，初始化 UI 根节点与层级；
2. 每个 UI 面板继承 `UIPanel`，在 `Initialize` 中缓存组件并使用 `this.BindXXX` 绑定事件；
3. 使用 `UISystem.Instance.OpenPanel/ClosePanel` 管理面板栈与层级；
4. 利用自动事件清理与批量清理，避免内存泄漏与重复监听。

在遵守 **预制体命名规则** 与 **Initialize 只做一次性初始化** 的前提下，就可以用非常少的代码实现稳定、高效、可维护的 UI 管理。