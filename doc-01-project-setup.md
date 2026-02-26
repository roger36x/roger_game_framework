# 文档一：项目搭建指南

> 本文档供 Claude Code 按步骤执行。目标是从零搭建一个基于 FNA 的 C# 游戏项目，在 macOS 上跑通一个蓝色窗口。

---

## 0. 环境前置条件

- macOS（Apple Silicon 或 Intel 均可）
- Homebrew 已安装
- IDE：Cursor（VS Code fork）+ C# Dev Kit 扩展
- Claude Code 插件已安装在 Cursor 中

## 1. 安装 .NET 8 SDK

```bash
brew install dotnet@8
```

验证安装：

```bash
dotnet --version
# 预期输出：8.x.x
```

如果 `dotnet` 命令未找到，可能需要将 Homebrew 的 dotnet 路径加入 PATH：

```bash
# Apple Silicon Mac
export DOTNET_ROOT="/opt/homebrew/opt/dotnet@8/libexec"
export PATH="$DOTNET_ROOT:$PATH"

# Intel Mac
export DOTNET_ROOT="/usr/local/opt/dotnet@8/libexec"
export PATH="$DOTNET_ROOT:$PATH"
```

建议将上述 export 语句添加到 `~/.zshrc` 中持久化。

## 2. Cursor 扩展配置

在 Cursor 扩展市场中安装：

- **C# Dev Kit**（Microsoft 官方，ID: `ms-dotnettools.csdevkit`）

它会自动安装依赖的 C# 语言支持和调试器。

## 3. 创建项目目录

```bash
mkdir -p ~/projects/game
cd ~/projects/game
git init
```

项目根目录结构规划：

```
game/
├── src/
│   └── Program.cs            # 入口点
├── lib/
│   └── FNA/                   # FNA 子模块
├── fnalibs/                   # FNA 原生依赖（SDL2 等）
├── content/                   # 游戏资源（后续使用）
├── Game.csproj                # 项目文件
├── .gitignore
└── README.md
```

## 4. 引入 FNA

```bash
cd ~/projects/game

# 添加 FNA 为 git submodule
git submodule add https://github.com/FNA-XNA/FNA.git lib/FNA

# 初始化 FNA 自身的子模块依赖
cd lib/FNA
git submodule update --init --recursive
cd ../..
```

## 5. 下载 FNA 原生依赖库（fnalibs）

FNA 依赖 SDL2、FAudio、FNA3D 等原生 C 库的预编译二进制。

```bash
cd ~/projects/game

curl -L https://fna.flibitijibibo.com/archive/fnalibs.tar.bz2 -o fnalibs.tar.bz2
mkdir -p fnalibs
tar xjf fnalibs.tar.bz2 -C fnalibs
rm fnalibs.tar.bz2
```

解压后 `fnalibs/` 内应包含对应平台的 `.dylib` 文件（macOS）。

## 6. 创建 .gitignore

```gitignore
bin/
obj/
fnalibs/
*.user
*.suo
.vs/
.vscode/
.idea/
*.DS_Store
```

注意：`fnalibs/` 不入版本控制（二进制文件，每个开发者自行下载）。

## 7. 创建项目文件

`Game.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>Game</RootNamespace>
    <AssemblyName>Game</AssemblyName>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="lib/FNA/FNA.csproj" />
  </ItemGroup>

</Project>
```

关键说明：
- `AllowUnsafeBlocks` 是 FNA 所需的
- 直接引用 FNA 的 `.csproj`，不使用 NuGet 包

## 8. 创建入口代码

`src/Program.cs`：

```csharp
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game;

public class MainGame : Microsoft.Xna.Framework.Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    public MainGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        Window.Title = "Game Prototype";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        base.Draw(gameTime);
    }
}

public static class Program
{
    [STAThread]
    public static void Main()
    {
        using var game = new MainGame();
        game.Run();
    }
}
```

## 9. 配置原生库加载路径

FNA 运行时需要找到 fnalibs 中的 `.dylib` 文件。

方式一：设置环境变量后运行

```bash
cd ~/projects/game
export DYLD_LIBRARY_PATH=$(pwd)/fnalibs/lib64:$DYLD_LIBRARY_PATH
dotnet run
```

方式二（推荐）：在项目文件中配置原生库拷贝

在 `Game.csproj` 的 `<PropertyGroup>` 中添加：

```xml
<DefineConstants>FNA</DefineConstants>
```

并添加以下 ItemGroup 来自动拷贝原生库到输出目录：

```xml
<ItemGroup>
  <Content Include="fnalibs/**/*.dylib" Condition="$([MSBuild]::IsOSPlatform('OSX'))">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <Link>%(Filename)%(Extension)</Link>
  </Content>
</ItemGroup>
```

这样 `dotnet run` 时原生库会自动拷贝到 `bin/` 目录，无需手动设置环境变量。

## 10. 构建并运行

```bash
cd ~/projects/game
dotnet build
dotnet run
```

## 11. 验收标准

- [ ] `dotnet build` 无错误
- [ ] `dotnet run` 弹出一个 1280×720 的窗口
- [ ] 窗口标题为 "Game Prototype"
- [ ] 窗口背景为矢车菊蓝（CornflowerBlue）
- [ ] 窗口可正常关闭
- [ ] 鼠标光标可见

## 12. 常见问题

**Q: `dotnet` 命令找不到**
A: 检查 `~/.zshrc` 中是否正确配置了 DOTNET_ROOT 和 PATH。

**Q: 构建时报 FNA 相关错误**
A: 确认 FNA 子模块完整初始化：`cd lib/FNA && git submodule update --init --recursive`

**Q: 运行时报找不到 SDL2**
A: fnalibs 中的动态库未正确加载。检查 `fnalibs/` 目录下是否有 `.dylib` 文件，以及 `.csproj` 中的 Content Include 路径是否匹配实际文件位置。运行 `ls fnalibs/` 查看实际目录结构后调整。

**Q: macOS 安全限制阻止加载 .dylib**
A: 运行 `xattr -r -d com.apple.quarantine fnalibs/` 移除隔离属性。

---

*完成本文档后，项目应处于"空白蓝色窗口可运行"状态，准备进入 Milestone 1。*
