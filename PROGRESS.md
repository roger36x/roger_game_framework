# 项目进展记录

---

## Milestone 1：最小原型 — 已完成

**完成日期**：2026-02-26

**目标**：验证 FNA + C# 技术链路

### 完成内容
- 从零搭建 FNA 项目，macOS Apple Silicon 上通过 Metal 后端运行
- .NET 8 SDK + FNA 26.02（SDL3）
- fnalibs 从源码构建（SDL3、FNA3D、FAudio），解决了官方下载链接失效问题
- 20×20 等距菱形瓦片棋盘格地图
- 红色角色精灵，WASD 键盘控制移动
- 2D 正交镜头 lerp 平滑跟随角色
- 窗口标题显示 FPS，稳定 60 FPS

### 关键技术决策
- 引用 `FNA.Core.csproj`（SDK 风格 net8.0），而非旧式 `FNA.csproj`
- `EnableDefaultCompileItems=false` 防止自动包含 FNA 子模块源码
- 等距投影公式：screenX = (tileX - tileY) × 32, screenY = (tileX + tileY) × 16

### 代码结构
```
src/
├── Program.cs      — 入口点
├── MainGame.cs     — Game 子类，主循环
├── Camera.cs       — 镜头系统
├── IsoUtils.cs     — 等距坐标转换（64×32 瓦片）
├── TileMap.cs      — 瓦片地图
└── Player.cs       — 角色控制
```

---

## Milestone 2：深度排序与多层建筑 — 已完成

**完成日期**：2026-02-26

**目标**：解决等距渲染中的遮挡关系

### 完成内容
- 引入 `TileCell` 数据结构，支持地面类型 + 方块高度（0-N 层）
- 等距立方体纹理生成（64×48），三面可见：亮色顶面、中色左面、暗色右面
- 统一 `DrawItem` 深度排序渲染管线：
  - 所有可绘制对象（地面、方块、角色）收集到列表
  - 按 depthKey 排序后统一绘制
- depthKey = (tileX + tileY) + height × 0.01 + drawLayer × 0.001
- 测试布局：L 形墙壁、2 层高柱子、墙壁段
- 角色正确地被方块遮挡 / 遮挡方块

### 新增/修改文件
- `src/BlockRenderer.cs` — 新建，等距方块纹理生成
- `src/IsoUtils.cs` — 新增高度偏移重载 + 深度键计算
- `src/TileMap.cs` — 重构为 TileCell 数据结构
- `src/Player.cs` — 暴露 DrawItem 参与排序
- `src/MainGame.cs` — 统一深度排序渲染循环

---

## Milestone 3：地图分块与流式加载 — 已完成

**完成日期**：2026-02-26

**目标**：从固定小地图扩展到可无限延伸的大地图

### 完成内容
- 16×16 瓦片 Chunk 数据结构，一维数组存储（行优先，缓存友好）
- `ChunkManager` 管理 chunk 生命周期：惰性加载 + 定期回收（300帧/5秒未使用）
- `Dictionary<long, Chunk>` 稀疏存储，支持任意方向无限扩展（含负坐标）
- `IChunkGenerator` 接口 + `ProceduralGenerator` 实现：确定性哈希生成地形
- 视口裁剪：Camera 反投影四角到瓦片坐标 AABB，只遍历可见 chunk
- 窗口标题显示调试信息：FPS + 已加载 chunk 数 + 可见瓦片数
- 程序化地图：棋盘格地面 + ~3% 方块（高度 1-2 层）

### 关键技术决策
- Chunk 大小选择 16×16：1280×720 视口下约 9 个 chunk 可见，裁剪粒度合理
- 字典键用 long 打包两个 int：`((long)cx << 32) | (uint)cy`，避免元组哈希开销
- 负坐标处理：`DivRem` 截断向零，需手动调整余数和商
- `FloorDiv` 辅助函数确保负坐标正确映射到 chunk

### 新增/修改文件
- `src/Chunk.cs` — 新建，16×16 chunk 数据结构
- `src/IChunkGenerator.cs` — 新建，chunk 生成接口
- `src/ProceduralGenerator.cs` — 新建，确定性程序化地形生成
- `src/ChunkManager.cs` — 新建，chunk 加载/存储/回收管理
- `src/Camera.cs` — 新增 `GetVisibleTileBounds()` 视口裁剪方法
- `src/TileMap.cs` — 精简为纹理提供者，移除地图数据
- `src/Player.cs` — 初始位置解耦，改为 (100, 100)
- `src/MainGame.cs` — 集成 chunk 系统 + 视口裁剪渲染循环

### 代码结构
```
src/
├── Program.cs             — 入口点
├── MainGame.cs            — 主循环，chunk 集成 + 视口裁剪渲染
├── Camera.cs              — 镜头系统 + 可见瓦片范围计算
├── IsoUtils.cs            — 等距坐标转换
├── TileMap.cs             — 地面纹理提供者
├── BlockRenderer.cs       — 等距方块纹理生成
├── Player.cs              — 角色控制
├── Chunk.cs               — 16×16 chunk 数据结构
├── IChunkGenerator.cs     — chunk 生成接口
├── ProceduralGenerator.cs — 确定性程序化生成
└── ChunkManager.cs        — chunk 生命周期管理
```

---

## Milestone 4：光照系统 — 已完成

**完成日期**：2026-02-26

**目标**：2D 光照系统（环境光、点光源、墙壁遮光）

### 完成内容
- RenderTarget2D 光照图 + 3-pass 渲染管线：
  - Pass 1: 构建光照图（环境光底色 + 加法混合点光源）
  - Pass 2: 绘制场景到 backbuffer（不变）
  - Pass 3: 乘法混合光照图覆盖场景
- 点光源：平台形 blob 纹理（128×80）+ 二次衰减 + BrightnessScale 控制
- 确定性位置/亮度抖动：哈希打破网格对齐，保留像素艺术质感
- Bresenham 视线检测：墙壁（BlockHeight > 0）遮挡光线投射
- 围墙测试房间 (95-99, 95-99)：室内被墙遮光，室内点光源照亮
- K/L 键日夜切换：白天 (200,200,210) / 夜晚 (30,30,60)
- 窗口标题增加光源数量调试信息

### 关键技术决策
- 光照图先于场景绘制（Pass 1 在 Pass 2 之前），避免 FNA backbuffer DiscardContents 问题
- 光照图采用 LinearClamp 采样器，消除移动时的亚像素闪烁
- 镜头跟随从 Lerp 改为 SmoothDamp（临界阻尼弹簧），消除停止时的尾部抖动
- Blob 纹理采用平台+衰减形状（中心 45% 区域满亮度），消除瓦片中心亮点
- 确定性 TileHash 位置抖动 ±5/±3px，打破网格对齐但不闪烁

### 新增/修改文件
- `src/LightSource.cs` — 新建，点光源数据结构
- `src/LightingSystem.cs` — 新建，光照图渲染 + 阴影投射 + 乘法合成
- `src/ProceduralGenerator.cs` — 新增围墙测试房间
- `src/MainGame.cs` — 集成 3-pass 光照渲染 + 日夜切换
- `src/Camera.cs` — Lerp 替换为 SmoothDamp

### 代码结构
```
src/
├── Program.cs             — 入口点
├── MainGame.cs            — 主循环，3-pass 光照渲染
├── Camera.cs              — SmoothDamp 镜头 + 可见瓦片范围
├── IsoUtils.cs            — 等距坐标转换
├── TileMap.cs             — 地面纹理提供者
├── BlockRenderer.cs       — 等距方块纹理生成
├── Player.cs              — 角色控制
├── Chunk.cs               — 16×16 chunk 数据结构
├── IChunkGenerator.cs     — chunk 生成接口
├── ProceduralGenerator.cs — 确定性生成 + 测试围墙房间
├── ChunkManager.cs        — chunk 生命周期管理
├── LightSource.cs         — 点光源数据结构
└── LightingSystem.cs      — 光照图渲染 + 阴影 + 合成
```

---

## Milestone 5：轻量级实体-组件系统 — 已完成

**完成日期**：2026-02-26

**目标**：建立通用的实体交互框架，使所有游戏对象共享同一套规则

### 完成内容
- 轻量 ECS 架构：Entity = int ID，Component = 纯 struct，System = 普通类
- `EntityManager`：按类型的 `Dictionary<int, T>` 组件存储 + 空间索引 `Dictionary<long, int>`
- `CollisionSystem`：地形碰撞（BlockHeight > 0）+ 实体碰撞（Collision.BlocksMovement）
- `InputSystem`：WASD 移动（含轴分离碰撞实现墙壁滑动）+ E 键交互触发
- `InteractionSystem`：近距离搜索（8 邻格最近实体），分发三种交互：
  - 门：开/关切换，同步碰撞状态 + 纹理切换
  - 拾取：销毁实体
  - 推动：沿玩家→实体方向推动 1 格（检查目标格可通行性）
- `EntityFactory`：运行时纹理生成（玩家/门/家具/物品），统一创建接口
- 测试围墙房间 (95-99, 95-99) 内放置门 (97,95)、家具 (97,97)、物品 (96,96)
- 玩家出生点附近额外放置物品 (101,100) 便于测试
- 配置值统一抽取到 `GameConfig.cs`（所有文件的魔法数字集中管理）

### 关键技术决策
- 不引入外部 ECS 库，显式字典比泛型注册更简单直观
- 空间索引用 long 打包坐标：`((long)tx << 32) | (uint)ty`，O(1) 查询瓦片实体
- 交互采用近距离搜索而非朝向检测：等距移动产生对角方向，朝向难以命中目标瓦片
- 轴分离碰撞：先尝试完整移动 → X 轴移动 → Y 轴移动，实现沿墙壁滑动
- 镜头像素对齐：`MathF.Round` 消除 PointClamp 采样下的亚像素闪烁
- Sprite.OffsetY：方块类实体（门、家具）设置 OffsetY = BlockVisualHeight (16px)，修正渲染对齐
- ProceduralGenerator 清空房间内部 (96-98, 96-98) 的随机方块，防止覆盖测试实体

### 新增文件
- `src/Components.cs` — 7 个组件 struct + InteractionType 枚举
- `src/EntityManager.cs` — 实体管理 + 组件字典 + 空间索引
- `src/CollisionSystem.cs` — 地形 + 实体碰撞检测
- `src/InteractionSystem.cs` — 交互处理（门/拾取/推动）
- `src/InputSystem.cs` — 玩家输入 + 移动 + 交互触发
- `src/EntityFactory.cs` — 实体工厂 + 运行时纹理生成

### 修改文件
- `src/GameConfig.cs` — 新增 InteractKey、实体颜色常量；集中所有配置值
- `src/Player.cs` — 精简为薄包装（EntityId + GetScreenPosition）
- `src/MainGame.cs` — 集成 ECS，实体渲染循环替代硬编码玩家绘制
- `src/Camera.cs` — GetTransformMatrix 像素对齐
- `src/ProceduralGenerator.cs` — 清空测试房间内部方块
- `src/IsoUtils.cs` — 常量改引用 GameConfig
- `src/BlockRenderer.cs` — 常量改引用 GameConfig
- `src/Chunk.cs` — 常量改引用 GameConfig
- `src/ChunkManager.cs` — 常量改引用 GameConfig
- `src/TileMap.cs` — 常量改引用 GameConfig
- `src/LightingSystem.cs` — 常量改引用 GameConfig

### 代码结构
```
src/
├── Program.cs             — 入口点
├── MainGame.cs            — 主循环，ECS 集成 + 3-pass 光照渲染
├── GameConfig.cs          — 集中配置常量
├── Camera.cs              — SmoothDamp 镜头 + 像素对齐
├── IsoUtils.cs            — 等距坐标转换
├── TileMap.cs             — 地面纹理提供者
├── BlockRenderer.cs       — 等距方块纹理生成
├── Components.cs          — ECS 组件定义
├── EntityManager.cs       — 实体管理 + 空间索引
├── CollisionSystem.cs     — 碰撞检测
├── InputSystem.cs         — 玩家输入处理
├── InteractionSystem.cs   — 交互系统
├── EntityFactory.cs       — 实体工厂
├── Player.cs              — 玩家薄包装
├── Chunk.cs               — 16×16 chunk 数据结构
├── IChunkGenerator.cs     — chunk 生成接口
├── ProceduralGenerator.cs — 确定性生成 + 测试房间
├── ChunkManager.cs        — chunk 生命周期管理
├── LightSource.cs         — 点光源数据结构
└── LightingSystem.cs      — 光照图渲染 + 阴影 + 合成
```

---

## 待完成里程碑

| 里程碑 | 目标 | 状态 |
|--------|------|------|
| Milestone 6 | 天气与粒子 | 待开始 |
| Milestone 7 | 音频系统 | 待开始 |
| Milestone 8 | Lua 脚本绑定 | 待开始 |
| Milestone 9 | 网络与多人 | 待开始 |

---

## 环境信息

- **平台**：macOS Apple Silicon (M3 Pro)
- **SDK**：.NET 8.0.124 via Homebrew
- **框架**：FNA 26.02（SDL3 + Metal）
- **IDE**：Cursor + C# Dev Kit
- **构建命令**：`export PATH="/opt/homebrew/opt/dotnet@8/bin:$PATH" && dotnet run`
