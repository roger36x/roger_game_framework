# 快速开始指南

> 在新机器上克隆项目并跑通的完整步骤。适用于 macOS（Apple Silicon / Intel）。

---

## 1. 前置依赖

```bash
# .NET 8 SDK
brew install dotnet@8

# 验证（如果命令找不到，见下方 PATH 配置）
/opt/homebrew/opt/dotnet@8/bin/dotnet --version
```

**持久化 PATH**（添加到 `~/.zshrc`）：

```bash
# Apple Silicon
export PATH="/opt/homebrew/opt/dotnet@8/bin:$PATH"

# Intel Mac
export PATH="/usr/local/opt/dotnet@8/bin:$PATH"
```

## 2. 克隆项目

```bash
git clone --recurse-submodules <repo-url> roger_game_framework
cd roger_game_framework
```

如果忘了 `--recurse-submodules`：

```bash
git submodule update --init --recursive
```

## 3. 构建 fnalibs（原生依赖）

项目使用 FNA 26.02（SDL3），官方 fnalibs 下载已失效，需要从源码构建三个库。

### 3a. SDL3

```bash
git clone https://github.com/libsdl-org/SDL.git /tmp/SDL3 --branch release-3.2.x --depth 1
cmake -S /tmp/SDL3 -B /tmp/SDL3/build -DCMAKE_BUILD_TYPE=Release
cmake --build /tmp/SDL3/build -j$(sysctl -n hw.ncpu)

cp /tmp/SDL3/build/libSDL3.0.dylib fnalibs/
ln -sf libSDL3.0.dylib fnalibs/libSDL3.dylib
```

### 3b. FNA3D

```bash
git clone https://github.com/FNA-XNA/FNA3D.git /tmp/FNA3D --recurse-submodules --depth 1
cmake -S /tmp/FNA3D -B /tmp/FNA3D/build -DCMAKE_BUILD_TYPE=Release \
      -DSDL3_DIR=/tmp/SDL3/build
cmake --build /tmp/FNA3D/build -j$(sysctl -n hw.ncpu)

cp /tmp/FNA3D/build/libFNA3D.0.dylib fnalibs/
ln -sf libFNA3D.0.dylib fnalibs/libFNA3D.dylib
```

### 3c. FAudio

```bash
git clone https://github.com/FNA-XNA/FAudio.git /tmp/FAudio --depth 1
cmake -S /tmp/FAudio -B /tmp/FAudio/build -DCMAKE_BUILD_TYPE=Release \
      -DSDL3_DIR=/tmp/SDL3/build
cmake --build /tmp/FAudio/build -j$(sysctl -n hw.ncpu)

cp /tmp/FAudio/build/libFAudio.0.dylib fnalibs/
ln -sf libFAudio.0.dylib fnalibs/libFAudio.dylib
```

### 3d. 移除 macOS 隔离属性

```bash
xattr -r -d com.apple.quarantine fnalibs/
```

### 3e. 验证

```bash
ls fnalibs/
# 应有 6 个文件：
# libSDL3.0.dylib  libSDL3.dylib
# libFNA3D.0.dylib libFNA3D.dylib
# libFAudio.0.dylib libFAudio.dylib
```

> **提示**：如果你有之前编译好的 fnalibs，可以直接把 6 个 dylib 拷贝到 `fnalibs/` 目录，跳过构建步骤。项目仓库中有一份 `fnalibs.tar.bz2` 备份（仅限同架构机器使用）。

## 4. 构建并运行

```bash
dotnet build
dotnet run
```

预期：弹出 1280×720 窗口，等距地图 + 角色 + 光照。

- **WASD** — 移动
- **E** — 交互（开门 / 拾取 / 推箱子）
- **K / L** — 白天 / 夜晚切换

## 5. Claude Code 开发环境

### CLAUDE.md

项目的 `.claude/` 目录已被 `.gitignore` 排除。在新机器上 Claude Code 会自动创建工作目录。

项目记忆文件（`MEMORY.md`）不跨机器同步，但以下文件包含完整的项目上下文：

| 文件 | 用途 |
|------|------|
| `PROGRESS.md` | 详细的里程碑完成记录（M1-M5 已完成） |
| `doc-01-project-setup.md` | 初始搭建过程记录 |
| `doc-03-milestone-overview.md` | 里程碑路线图（M1-M9） |

### 建议在新机器上告诉 Claude Code

> 请阅读 PROGRESS.md 和 doc-03-milestone-overview.md 了解项目背景。当前 M1-M5 已完成，下一个是 M6（天气与粒子）。

## 6. 项目结构

```
roger_game_framework/
├── Game.csproj            # 项目文件（net8.0 + FNA）
├── PROGRESS.md            # 里程碑进展记录
├── SETUP.md               # 本文件
├── lib/FNA/               # FNA 子模块（SDL3 版本）
├── fnalibs/               # 原生库（不入 git，需手动构建）
├── src/                   # 游戏源码（21 个 .cs 文件）
│   ├── MainGame.cs        # 主循环
│   ├── GameConfig.cs      # 集中配置常量
│   ├── Components.cs      # ECS 组件定义
│   ├── EntityManager.cs   # 实体管理 + 空间索引
│   ├── CollisionSystem.cs # 碰撞检测
│   ├── InputSystem.cs     # 玩家输入
│   ├── InteractionSystem.cs # 交互系统
│   ├── EntityFactory.cs   # 实体工厂
│   ├── LightingSystem.cs  # 光照系统
│   ├── ChunkManager.cs    # Chunk 流式加载
│   └── ...                # 其他模块
├── doc-01-project-setup.md
└── doc-03-milestone-overview.md
```

## 7. 常见问题

**Q: `dotnet build` 报 FNA 错误**
A: 确认子模块完整：`git submodule update --init --recursive`

**Q: 运行时报找不到 SDL3 / FNA3D**
A: 检查 `fnalibs/` 下有 6 个 `.dylib` 文件。运行 `xattr -r -d com.apple.quarantine fnalibs/`。

**Q: cmake 找不到 SDL3**
A: 确保 SDL3 先编译完成，且 `-DSDL3_DIR=` 指向正确的 build 目录。

**Q: FNA 子模块显示 `(untracked content)` 警告**
A: 正常现象，`.gitmodules` 已配置 `ignore = dirty`。

---

*最后更新：2026-02-26 | M1-M5 已完成*
