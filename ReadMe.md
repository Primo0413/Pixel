# ReadMe

## Pixel Edit

Pixel Edit 是一个基于 **.NET 8 + WPF + MVVM** 的像素画工具，支持：
- 图片转像素画
- 白板手工画像素画
- 本地项目保存/加载
- 多项目拼接与堆叠
- 导出 PNG

## 运行环境
- Windows 10/11
- .NET SDK 8.0+

## 快速启动
```bash
dotnet restore
dotnet build
dotnet run
```

## 主界面说明
- 左侧是可收缩侧边栏：
  - **加载图片并转换**：导入图片并生成像素工程
  - **创建白板绘制**：新建空白像素画布
  - **加载并拼接/堆叠**：组合多个本地像素工程
  - **首页**：返回欢迎页
- 右侧为当前功能页面
- 每个页面的操作按钮统一在底部操作栏

## 功能使用

### 1) 图片转像素画
1. 点击侧边栏「加载图片并转换」
2. 在编辑页底部点击「导入并转换图片」
3. 选择图片（png/jpg/jpeg/bmp）
4. 转换完成后可继续编辑
5. 点击「保存工程」保存为 `.pxproj.json`
6. 点击「导出PNG」导出图片

### 2) 白板绘制
1. 点击侧边栏「创建白板绘制」
2. 在画布区域点击像素点进行上色
3. 使用左侧调色板切换颜色
4. 使用「橡皮模式」擦除
5. 使用「删除选中像素」删除最后一次选中的点
6. 保存工程 / 导出 PNG

### 3) 拼接与堆叠
1. 点击侧边栏「加载并拼接/堆叠」
2. 点击底部「加载本地工程」选择多个 `.pxproj.json`
3. 选择组合模式：
   - `Stack`：堆叠
   - `Stitch`：拼接
4. 点击「执行组合」
5. 可「在编辑器打开」继续调整
6. 可保存组合工程或导出 PNG

## 文件与数据
- 工程文件：`*.pxproj.json`
- 导出图片：`*.png`
- 颜色别名配置：
  - 仓库默认：`color-aliases.json`
  - 运行时：`%LocalAppData%/pixel_edit/color-aliases.json`

## 图标
- 当前应用图标：`Assets/pixel-p-icon.png`（像素风大写 P）

## 常见问题

### 启动闪退（XAML 资源错误）
先执行：
```bash
dotnet clean
dotnet build
dotnet run
```
并确认 `App.xaml` 中样式键存在。

### 颜色映射不符合预期
检查本地颜色别名文件：
`%LocalAppData%/pixel_edit/color-aliases.json`

### 找不到保存的项目
默认建议保存目录：
`%LocalAppData%/pixel_edit/projects/`

## 后续建议
- 增加拖拽连续绘制
- 增加网格拼接参数（纵向、行列）
- 增加撤销/重做
- 增加缩放与平移画布
