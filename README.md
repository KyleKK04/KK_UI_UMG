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

Generated 输出是可删除、可覆盖、可重建的产物；需要长期维护的是 Source JSON、手写 Controller partial 和业务 Service Adapter。

## 适用目标

KK_UI_UMG 适合这些 Unity 项目：

- 运行时 UI 使用 UGUI，但希望 UI 结构、绑定、事件和资产依赖有稳定的文本源头。
- 希望 UI 改动可以 code review，而不是只看 Prefab 序列化 diff。
- 希望让 AI / Codex 从自然语言创建或修改 UI，同时不直接手改 Generated 文件。
- 希望 UI 输入链路保持 MVVM-C 边界：View 只转发事件，Controller 写 Store，Binder 刷新 UGUI，UIManager 管生命周期。
- 希望把 UI 先做成纯显示结构，后续再通过 Service Adapter 接入已有业务代码。

它不是这些东西：

- 不是完整 UI 生态或主题系统。
- 不是动画框架、虚拟列表框架或 UI Toolkit runtime 方案。
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
com.kk.ui-umg-1.0.3.tgz
```

在 Unity Package Manager 中选择：

```text
Package Manager
  -> Add package from tarball...
  -> com.kk.ui-umg-1.0.3.tgz
```

或写入 `Packages/manifest.json`：

```json
{
  "dependencies": {
    "com.kk.ui-umg": "file:/absolute/path/com.kk.ui-umg-1.0.3.tgz"
  }
}
```

### 方式二：从 GitHub 安装

```json
{
  "dependencies": {
    "com.kk.ui-umg": "https://github.com/KyleKK04/KK_UI_UMG.git#v1.0.3"
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

7. 依次运行：

   ```text
   Validate
   Generate
   Verify
   Refresh Preview
   ```

8. 运行时打开 UI：

   ```csharp
   await UIManager.Instance.OpenAsync("<PackageId>");
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

`Generated/` 下的 C# 和 Prefab 可以删除后重建。如果生成结果不对，应该修改 Source JSON 或 generator，而不是手改生成物。

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

- View 只持有 UGUI 引用并转发事件。
- Controller 是 UI 业务入口，只由它写 Store。
- ViewModelStore 是显示状态来源。
- Binder 只把 Store 写回 UGUI。
- UIManager 负责加载、打开、关闭、释放和生命周期。

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
```

生成器拥有 `Scripts/`、`Prefabs/`、`Reports/`、`Assets/` 子目录；手写业务 partial 放在对应 UI 文件夹根目录，不放进 `Scripts/`。

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
```

每个 Source package 可以带 `validation.md`，由 pipeline 写入 Validate / Generate / Verify / Preview 状态，方便记录交付状态。

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
