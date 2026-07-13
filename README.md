# KK_UI_UMG

KK_UI_UMG 是一个面向 Unity UGUI 的 MVVM-C UI 生成框架。它把 UI 的源头放在可评审的 JSON Manifest 中，再生成 C# 代码和 UGUI Prefab，让 UI 可以稳定校验、生成、预览、运行和重建。

```text
Assets/UI/Source/<PackageId>/          # default Source package path
  -> Validate
  -> Generate C# + UGUI Prefab
  -> Verify
  -> Preview
  -> UIManager.OpenAsync(...)
```

Generated-owned 输出是可删除、可覆盖、可重建的产物；需要长期维护的是 Source JSON、生成包根目录下的手写 View / Controller partial 和业务 Service Adapter。Prefab 中手调的受支持布局只有通过 KKPipeline 的 `Export` 模式显式采纳回 Source 后才会持久化。

## 适用目标

KK_UI_UMG 适合这些 Unity 项目：

- 运行时 UI 使用 UGUI，但希望 UI 结构、绑定、事件和资产依赖有稳定的文本源头。
- 希望 UI 改动可以 code review，而不是只看 Prefab 序列化 diff。
- 希望让 AI / Codex 从自然语言创建或修改 UI，也允许设计者在 Prefab 中微调布局后显式采纳回 Source JSON。
- 希望 UI 输入链路保持 MVVM-C 边界：View.Generated 转发事件，View partial 负责视觉动画，Controller 写 Store，Binder 刷新 UGUI，UIManager 管生命周期。
- 希望把 UI 先做成纯显示结构，后续再通过 Service Adapter 接入已有业务代码。

它不是这些东西：

- 不是完整 UI 生态或主题系统。
- 不是 Tween 引擎、动画 JSON DSL、虚拟列表框架或 UI Toolkit runtime 方案；框架只提供 View 生命周期动画的等待协议。
- 不是任意 Unity Component 的可视化编辑器。
- 不是让 View / Binder 直接访问业务系统的快捷通道。

核心设计取舍是：**JSON Manifest 是源头，UGUI Prefab 是生成物，Controller 是业务入口，UIManager 是打开和关闭的唯一运行时入口。**

## 安装方法

### 环境要求

- Unity `6000.3` 或同一大版本线。
- Addressables。
- UGUI。
- TextMeshPro。

Package 依赖写在 `package.json` 中，Unity Package Manager 会处理 Addressables、UGUI 以及 Editor pipeline 内部使用的 Unity Newtonsoft Json 依赖。用户业务 Runtime 代码不需要直接使用 Newtonsoft Json；TextMeshPro 字体资产需要项目侧准备。

授权以 `LICENSE.md` 和 `package.json` 为准。当前 package 标记为 `UNLICENSED`，如果要作为公开开源项目接受外部使用，需要先更新授权策略。

### 方式一：从 Release tarball 安装

普通用户推荐使用 GitHub Release 中的 tarball。tarball 是正式交付包，不包含 package 开发用 `Tests/`。

```text
com.kk.ui-umg-1.0.6.tgz
```

在 Unity Package Manager 中选择：

```text
Package Manager
  -> Add package from tarball...
  -> com.kk.ui-umg-1.0.6.tgz
```

或写入 `Packages/manifest.json`：

```json
{
  "dependencies": {
    "com.kk.ui-umg": "file:/absolute/path/com.kk.ui-umg-1.0.6.tgz"
  }
}
```

### 方式二：从 GitHub 安装

```json
{
  "dependencies": {
    "com.kk.ui-umg": "https://github.com/KyleKK04/KK_UI_UMG.git#v1.0.6"
  }
}
```

Git URL 更适合希望跟随源码、调试 package 或查看测试代码的开发者。源码仓库会包含开发用 `Tests/`，tarball 默认不会包含。

### 方式三：本地嵌入开发

如果直接把 package 放在 Unity 项目的 `Packages/com.kk.ui-umg/` 下：

```json
{
  "dependencies": {
    "com.kk.ui-umg": "file:com.kk.ui-umg"
  }
}
```

### 首次使用

1. 打开设置面板：

   ```text
   KK_UI_UMG/Setting
   ```

2. 点击 `Install Codex Skill`，安装 AI authoring skill。

   Skill 名称：

   ```text
   kk-ui-umg
   ```

3. 在场景中创建一个 GameObject，通常命名为 `UIManager`，并挂载：

   ```text
   KK.UI.UMG.UIManager
   ```

   也可以在 `KK_UI_UMG/Setting` 中点击 `Create UIManager In Scene`。

4. 打开主面板：

   ```text
   KK_UI_UMG/KKPipeline
   ```

5. 创建或选择 Source package。默认推荐路径是：

   ```text
   Assets/UI/Source/<PackageId>/package.json
   ```

   项目也可以使用自定义 Source 根，例如：

   ```text
   Assets/_Project/UISource/<PackageId>/package.json
   Assets/Game/UI/Source/<PackageId>/package.json
   ```

   Source package 必须在 `Assets/` 或 `Packages/` 下，最后一级文件夹名必须等于 `packageId`，并且不能放在 `Generated` 文件夹下。

6. 在 `Generated Parent Folder` 选择生成父目录。默认是：

   ```text
   Assets/UI/Generated
   ```

   每个 UI 会生成到：

   ```text
   <Generated Parent>/<PackageId>/
   ```

7. KKPipeline 顶部提供两个模式按钮：

   ```text
   Import  # 保留原有完整界面；按 Validate、Generate、Verify 流程生成并检查 UGUI
   Export  # 只显示操作提示和 Export 按钮
   ```

   使用 `Export` 时，先修改并保存生成 Prefab，再在 Project 窗口选中对应 Prefab，最后点击 `Export`。它会同步现有 Source 节点的 `anchorMin`、`anchorMax`、`anchoredPosition`、`sizeDelta` 和已经声明的 layoutComponents；不会导入节点增删、重命名、重挂父节点、pivot、旋转、缩放、文本、图片或任意组件。反向写回后切回 `Import`，再运行 `Validate`、`Generate`、`Verify` 和 `Refresh Preview` 检查往返结果。

8. 运行时打开 UI：

   ```csharp
   await UIManager.Instance.OpenAsync("<PackageId>");
   ```

   高频 UI，例如背包、暂停菜单、地图、设置或 HUD，可以提前预加载并隐藏/显示复用，避免每次关闭后重新加载和实例化：

   ```csharp
   await UIManager.Instance.PreloadAsync("<PackageId>");
   await UIManager.Instance.OpenAsync("<PackageId>");
   await UIManager.Instance.HideAsync("<PackageId>");
   await UIManager.Instance.ShowAsync("<PackageId>");
   await UIManager.Instance.ReleaseAsync("<PackageId>");
   ```

### 示例：Inventory Panel Sample

Package 内置一个可直接打开的样例：

```text
Packages/com.kk.ui-umg/Sample/InventoryPanelSample/
```

它展示完整链路：Source JSON、静态 `locKey`、动态 Store 字段、Generated Prefab、Addressables、`UIManager.OpenAsync`、手写 Controller partial、业务 `IInventoryService` 注册和运行时数据更新。

样例中的 Source / Generated / Scene / Scripts 都在 package 内，方便随 package 一起交付。真实项目中仍以项目自己的 Source package 为源头，通过 `KK_UI_UMG/KKPipeline` 重新生成项目自己的 UI。

新 UI 推荐把 sample 当作 AI authoring 模板：让 Codex 参考 `Sample/InventoryPanelSample/Source/KkSampleInventoryPanel` 的 Source JSON 结构，生成你项目中的 Source package，例如 `Assets/UI/Source/<PackageId>/` 或 `Assets/_Project/UISource/<PackageId>/`。不要把 sample 的 Generated 输出当作手写模板。

## 项目亮点

### Manifest 是唯一源头

每个 UI Source package 使用固定文件结构。默认路径是：

```text
Assets/UI/Source/<PackageId>/
├─ package.json
├─ layout.json
├─ bindings.json
├─ codegen.json
├─ strings.json
├─ assets.json
├─ README.md
├─ validation.md
└─ Assets/
```

也可以放在项目自定义 Source 根下，例如 `Assets/_Project/UISource/<PackageId>/`。合法规则是：Source package root 在 `Assets/` 或 `Packages/` 下，最后一级文件夹名等于 `packageId`，且不在 `Generated` 文件夹下。

生成包的 generated-owned 子目录中的 C# 和 Prefab 可以删除后重建。如果生成结果不对，应该修改 Source JSON 或 generator。允许在生成 Prefab 中微调受支持布局，但保存后必须在 Project 窗口选中该 Prefab，并通过 KKPipeline 的 `Export` 写回 `layout.json`；其他 Generated 修改不会持久化。生成包根目录的手写 partial 不属于可重建输出。

### MVVM-C 边界清楚

标准输入链路：

```text
UGUI event
  -> View.Generated handler
  -> Controller handler
  -> Store.Update
  -> Flush
  -> Binder writes UGUI
```

职责边界：

- View.Generated 持有 UGUI 引用并转发事件；手写 View partial 只负责视觉表现。
- Controller 是 UI 业务入口，只由它写 Store。
- ViewModelStore 是显示状态来源。
- Binder 只把 Store 写回 UGUI。
- UIManager 负责预加载、打开、隐藏、显示、关闭、释放和生命周期。

高频 UI 可以通过 `HideAsync` 保留 View / Controller / Store，再通过 `ShowAsync` 恢复显示；`CloseAsync` / `ReleaseAsync` 才是真正释放 Controller、销毁 View、释放 Addressables 的路径。Store 写入仍然使用泛型 `Store.Update<T>`，没有强类型 Update 快路径。

### View partial 生命周期动画

JSON 继续只描述结构、绑定和资产。需要自定义动画时，在生成包根目录添加手写 View partial：

```text
<Generated Parent>/<PackageId>/<ViewClassName>.cs
```

不要把它放进 generator-owned 的 `Scripts/`。可按需 override 四个阶段，未 override 时立即完成：

```csharp
using System.Threading;
using System.Threading.Tasks;

public partial class InventoryPanelView
{
    protected override Task OnPlayOpenTransitionAsync(CancellationToken cancellationToken)
    {
        return PlayEnterAsync(cancellationToken);
    }

    protected override Task OnPlayCloseTransitionAsync(CancellationToken cancellationToken)
    {
        return PlayExitAsync(cancellationToken);
    }
}
```

同样可以 override `OnPlayShowTransitionAsync` 和 `OnPlayHideTransitionAsync`。View partial 只操作 alpha、position、scale、color、Animator 等视觉属性；不写 Store、不访问 Service，也不调用 UIManager 或 Controller 生命周期。所有可等待动画必须响应传入的 `CancellationToken`。

`UIManager` 会在 transition 完成后再推进稳定生命周期；同一个 View 的相反请求会串行，不同 View 可以并行。transition 期间目标不可交互并阻止点击穿透，只有已经完成 transition 且实际位于栈顶的 View 会收到 `OnActivated()`。

### 静态文本和动态文本分流

静态标题、按钮文案、Label、placeholder 和固定提示写在 `strings.json`，由 `layout.json` 的 `locKey` 引用，生成 Prefab 时写入 TMP 文本，不进入 Store。

只有运行时会变化、来自业务 Service、数量/进度/状态、玩家/物品/任务信息或列表 item 文本，才进入 `bindings.json`、`Store.Update(...)` 和 Binder 刷新链路。

### Source / Generated / Handwritten 分离

```text
<Source Package Root>/                 # 人和 AI 维护的源头，默认 Assets/UI/Source/<PackageId>/
<Generated Parent>/<PackageId>/Scripts # 生成 C#
<Generated Parent>/<PackageId>/Prefabs # 生成 Prefab
<Generated Parent>/<PackageId>/Reports # 生成报告
<Generated Parent>/<PackageId>/Assets  # 复制后的运行时资产
<Generated Parent>/<PackageId>/<PackageId>Controller.cs  # 手写业务 partial
<Generated Parent>/<PackageId>/<ViewClassName>.cs         # 可选手写视觉动画 partial
```

生成器拥有 `Scripts/`、`Prefabs/`、`Reports/`、`Assets/` 子目录；手写 View / Controller partial 放在对应 UI 文件夹根目录，不放进 `Scripts/`。

`assets.json` 里的 `contentHash` 是可选内容锁，不是必填项。只写 `source` 时会跳过 hash 校验；填写 `contentHash` 时才会严格校验 `sha256:` 格式和实际文件内容。TMP 字体、动态 atlas、材质等 Unity 可能自动重写的资源建议不填 `contentHash`，图标和稳定贴图等需要防漂移的资源再填写。

### AI Authoring Skill 随包交付

Package 内包含 Codex Skill：

```text
CodexSkills/kk-ui-umg/
```

安装后，Codex 可以按框架规则创建或修改 Source JSON，包括 layout、bindings、codegen、strings、assets、README 和 validation ledger。

安装命令：

如果当前目录是 GitHub package 仓库根目录：

```bash
python3 CodexSkills/kk-ui-umg/scripts/install_skill.py
python3 ~/.codex/skills/kk-ui-umg/scripts/quick_validate.py
```

如果当前目录是安装了该 package 的 Unity 项目根目录：

```bash
python3 Packages/com.kk.ui-umg/CodexSkills/kk-ui-umg/scripts/install_skill.py
python3 ~/.codex/skills/kk-ui-umg/scripts/quick_validate.py
```

### 支持常用 UGUI 控件和布局组件

当前 Source JSON 支持常用 UGUI 控件和结构化布局能力，包括 Text、Image、Button、RawImage、Toggle、Slider、InputField、Dropdown、Scrollbar、ScrollView、VerticalList，以及 layoutElement、horizontalLayout、verticalLayout、gridLayout、contentSizeFitter、aspectRatioFitter。

### 验证闭环

Editor pipeline 提供：

```text
Validate -> Generate -> Verify -> Refresh Preview

Export (select saved Prefab) -> Import / Validate -> Generate -> Verify
```

每个 Source package 可以带 `validation.md`，由 pipeline 写入 Validate / Generate / Verify / Preview 状态，方便记录交付状态。Runtime 的规范状态只有 `Pending / Verified`；真实完成 PlayMode 验收后点击 KKPipeline 的 `Runtime Verify` 并确认即可记录为 `Verified`。再次运行 `Generate` 会自动恢复为 `Pending`，等待重新验收；旧版 `Runtime: Pass` 会在下一次管线写入时迁移为 `Verified`。

### 业务接入通过 Service Adapter

已有业务代码不直接暴露给 View、Binder 或 Source JSON。需要业务数据时，由业务目录提供 UI-facing service adapter，并注册到 `UIManager`；Controller partial 订阅服务、映射数据到 Store，再由 Binder 刷新 UGUI。

### Package-only 交付边界

GitHub / UPM 交付对象是 package 本体：

```text
README.md
CHANGELOG.md
LICENSE.md
package.json
Runtime/
Editor/
CodexSkills/
```

项目根 `Assets/` 示例、开发期测试和临时生成内容不属于发布 package。

## 授权

Copyright (c) KyleKK. All rights reserved.

This package is private/internal unless a separate license is provided.
