# IOS.Viewer.Desktop

这是IOS自动出库系统的桌面版本，基于Avalonia框架构建。

## 项目结构

- **IOS.Viewer.Desktop** - 桌面应用启动项目
- **IOS.Viewer** - 共享的UI和业务逻辑项目
- **IOS.Viewer.Android** - Android移动应用项目

## 特性

- 🖥️ 跨平台桌面支持 (Windows, macOS, Linux)
- 🎨 使用SukiUI提供现代化界面
- 🌙 支持明暗主题切换
- 📱 与移动版本共享核心代码

## 运行项目

### 开发环境要求

- .NET 8.0 SDK
- Visual Studio 2022 或 Visual Studio Code
- Windows 10/11, macOS, 或 Linux

### 构建和运行

```bash
# 构建项目
dotnet build IOS.Viewer.Desktop

# 运行项目
dotnet run --project IOS.Viewer.Desktop
```

### 发布应用

```bash
# Windows x64
dotnet publish IOS.Viewer.Desktop -c Release -r win-x64 --self-contained

# macOS x64
dotnet publish IOS.Viewer.Desktop -c Release -r osx-x64 --self-contained

# Linux x64
dotnet publish IOS.Viewer.Desktop -c Release -r linux-x64 --self-contained
```

## 依赖项

- **Avalonia** - 跨平台UI框架
- **SukiUI** - 现代化UI主题库
- **CommunityToolkit.Mvvm** - MVVM框架
- **Material.Icons.Avalonia** - Material Design图标

## 架构说明

桌面版本通过引用 `IOS.Viewer` 项目来共享所有的视图、视图模型和业务逻辑，只需要提供不同的平台入口点：

- `Program.cs` - 桌面应用入口
- `app.manifest` - Windows平台配置（高DPI支持等）
- `IOS.Viewer.Desktop.csproj` - 项目配置文件

这种架构确保了代码复用的最大化，同时保持了平台特定功能的灵活性。 