# ARCHITECTURE_AND_FUNCTIONS.md

## 项目结构

```text
pixel_edit/
├─ App.xaml / App.xaml.cs                # 应用入口、全局资源、DataTemplate
├─ MainWindow.xaml / .cs                 # 主壳：侧边栏 + 内容区
├─ Models/                               # 数据模型
├─ ViewModels/                           # MVVM 视图模型与命令
├─ Views/                                # 页面视图
├─ Services/                             # 业务服务层
├─ Converters/                           # XAML 绑定转换器
├─ Assets/                               # 图标等静态资源
├─ color-aliases.json                    # 默认颜色别名
└─ pixel_edit.csproj                     # 项目配置与依赖
```

---

## 核心模块说明

## 1) Models

### `Models/PixelProject.cs`
像素工程根对象，包含：
- 画布参数（`Canvas`）
- 调色板（`Palette`）
- 图层（`Layers`）
- 组合模式与组合项（`ComposeMode`, `CompositionItems`）

### `Models/CanvasSpec.cs`
画布规格：宽、高、像素显示尺寸。

### `Models/PaletteEntry.cs`
颜色条目：`Alias`（如 M6）、`Hex`（如 #445566）、`Name`。

### `Models/PixelLayer.cs`
单图层像素数据：
- `Pixels` 使用调色板索引数组（-1 表示透明）
- 图层可见性、偏移、层级

### `Models/CompositionItem.cs`
记录拼接/堆叠中引用的来源项目和位置信息。

### `Models/PixelCell.cs`
编辑器中的单个可视像素单元，含 `X/Y`、`PaletteIndex`、`Brush`。

---

## 2) ViewModels

### `ViewModels/MainWindowViewModel.cs`
主导航与状态中心：
- 维护 `CurrentViewModel`
- 维护侧边栏状态（收缩/展开）
- 提供导航命令：
  - `GoConvertImageCommand`
  - `GoBlankEditorCommand`
  - `GoComposeCommand`
  - `GoHomeCommand`
- 维护当前高亮导航项（`ActiveNavKey`）

### `ViewModels/HomeViewModel.cs`
首页逻辑（目前主要作为欢迎/说明页）。

### `ViewModels/EditorViewModel.cs`
编辑器核心逻辑：
- 画布像素构建与重建 `RebuildCells()`
- 单点绘制 `PaintCell(...)`
- 删除选中像素 `DeleteSelectedPixel()`
- 图片导入并转换 `ImportAndConvertImageAsync()`
- 项目保存/加载 `SaveProjectAsync()` / `LoadProjectAsync()`
- 导出 PNG `ExportPngAsync()`

### `ViewModels/ComposeViewModel.cs`
组合页面逻辑：
- 加载多个本地项目 `AddProjectsAsync()`
- 执行组合 `Compose()`
- 组合结果保存 `SaveComposedProjectAsync()`
- 导出 PNG `ExportPngAsync()`
- 在编辑器继续编辑 `OpenInEditor()`

---

## 3) Services

### `Services/ProjectStorageService.cs`
负责 `.pxproj.json` 的读写序列化。

### `Services/PixelConvertService.cs`
图片 -> 像素项目转换：
- 读取图片像素
- 采样到目标网格
- 映射调色板并生成图层数据

### `Services/ExportService.cs`
将 `PixelProject` 渲染并导出为 PNG。

### `Services/ComposeService.cs`
多个项目组合：
- `Stack` 堆叠
- `Stitch` 拼接
并输出新的 `PixelProject`。

### `Services/ColorAliasService.cs`
颜色别名管理：
- 读取本地颜色别名
- 新颜色自动分配别名
- 持久化到本地配置

### `Services/AppPaths.cs`
集中管理本地文件路径（项目目录、颜色配置路径）。

### `Services/ColorHelper.cs`
Hex 与 `Color` 的转换辅助。

---

## 4) Views

### `Views/HomeView.xaml`
首页欢迎与说明布局。

### `Views/EditorView.xaml`
编辑器页面：
- 左侧调色板
- 中央像素网格
- 底部统一操作栏

### `Views/ComposeView.xaml`
组合页面：
- 左侧组合参数与已加载列表
- 右侧组合结果信息
- 底部统一操作栏

---

## 5) Converters

### `Converters/HexToBrushConverter.cs`
将颜色字符串（`#RRGGBB`）转换为 `Brush` 供 XAML 使用。

### `Converters/BoolToVisibilityConverter.cs`
布尔值转 `Visibility`，用于侧边栏收缩/展开时文本显示控制。

---

## 关键流程（调用链）

## A. 图片转像素画
`MainWindowViewModel.GoConvertImageCommand`
→ `EditorViewModel.ImportAndConvertImageAsync()`
→ `PixelConvertService.ConvertAsync(...)`
→ `EditorViewModel.RebuildCells()`
→ `ProjectStorageService.SaveAsync(...)` / `ExportService.ExportPngAsync(...)`

## B. 白板绘制
`MainWindowViewModel.GoBlankEditorCommand`
→ `EditorViewModel.PaintCell(...)`
→ `EditorViewModel.DeleteSelectedPixel()`
→ `ProjectStorageService.SaveAsync(...)`

## C. 拼接堆叠
`MainWindowViewModel.GoComposeCommand`
→ `ComposeViewModel.AddProjectsAsync()`
→ `ComposeViewModel.Compose()`
→ `ComposeService.Compose(...)`
→ 保存/导出/回编辑器

---

## 维护建议
- 新增功能优先放入 `Services`，ViewModel 负责编排命令。
- 与 UI 相关的全局视觉规则尽量集中在 `App.xaml`。
- 模型扩展时优先保持 `PixelProject` 向后兼容（如增加 `SchemaVersion` 迁移策略）。
