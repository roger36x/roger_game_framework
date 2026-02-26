# 文档二：Milestone 1 — 最小原型规格

> 前置条件：文档一已完成，蓝色窗口可正常运行。

---

## 目标

验证 FNA + C# 技术链路在以下场景下的可行性：
- 等距（isometric）瓦片渲染
- 精灵绘制与深度排序
- 键盘输入驱动角色移动
- 镜头跟随

**本里程碑不做任何游戏设计决策。** 所有内容均为技术验证用途，使用最简单的占位图形。

---

## 不做什么（明确排除）

- 不做 ECS 架构（直接写即可，后续里程碑再重构）
- 不做地图文件加载（硬编码一个小地图）
- 不做美术资源（全部用代码生成的纯色几何图形）
- 不做游戏逻辑（无碰撞、无物品、无 AI）
- 不做内容管线（所有资源运行时生成）
- 不做 UI 框架（帧率用最简单的方式显示即可）

---

## 具体要求

### 1. 等距瓦片网格

- 采用标准菱形等距投影（diamond isometric）
- 瓦片逻辑尺寸：64×32 像素（2:1 宽高比）
- 渲染一个固定大小的网格：20×20 瓦片
- 瓦片使用代码生成的纯色纹理，两种颜色交替（棋盘格），便于视觉确认坐标正确性
- 坐标转换函数：
  - `WorldToScreen(int tileX, int tileY) -> Vector2` — 世界网格坐标到屏幕像素坐标
  - `ScreenToWorld(Vector2 screenPos) -> (int tileX, int tileY)` — 屏幕坐标到网格坐标（用于后续鼠标交互，本里程碑可以先写好但不一定使用）

### 2. 角色精灵

- 一个代码生成的简单矩形或圆形精灵，颜色醒目（如红色），大小约 16×32 像素
- 角色有浮点精度的世界坐标（不锁定到网格）
- 键盘 WASD 控制移动，移动方向应沿等距轴（W=左上，S=右下，A=左下，D=右上，或采用更直觉的映射——以实际手感为准，可调整）
- 移动速度：约 100 像素/秒（可配置常量）

### 3. 深度排序

- 角色应正确地显示在瓦片"之上"
- 本里程碑暂不需要处理角色与其他立体物体的遮挡关系（没有立体物体）
- 绘制顺序：先绘制瓦片（从后往前），再绘制角色

### 4. 镜头（Camera）

- 2D 正交镜头，跟随角色
- 角色始终保持在屏幕中央（或接近中央）
- 使用 Matrix 变换实现（`SpriteBatch.Begin` 的 `transformMatrix` 参数）
- 可选：镜头平滑跟随（lerp），但不强制

### 5. 帧率显示

- 在屏幕左上角显示当前 FPS
- 使用 FNA 内置的 SpriteFont 或直接用窗口标题显示（最简方案）
- 目标：在 20×20 地图 + 1 个角色的场景下，稳定 60 FPS

### 6. 代码结构

建议但不强制的文件组织：

```
src/
├── Program.cs              # 入口
├── MainGame.cs             # Game 子类，主循环
├── Camera.cs               # 镜头
├── IsoUtils.cs             # 等距坐标转换工具函数
├── TileMap.cs              # 瓦片地图数据 + 渲染
└── Player.cs               # 角色状态 + 渲染
```

原则：保持简单，一个类一个职责，但不要过度抽象。这是原型，不是最终架构。

---

## 等距投影参考公式

标准菱形等距投影（2:1）：

```
screenX = (tileX - tileY) * (tileWidth / 2)
screenY = (tileX + tileY) * (tileHeight / 2)
```

反向：

```
tileX = (screenX / (tileWidth / 2) + screenY / (tileHeight / 2)) / 2
tileY = (screenY / (tileHeight / 2) - screenX / (tileWidth / 2)) / 2
```

其中 `tileWidth = 64`, `tileHeight = 32`。

---

## 运行时纹理生成

由于不使用内容管线，所有纹理在 `LoadContent()` 中通过代码生成：

```csharp
// 示例：生成一个纯色纹理
Texture2D CreateColorTexture(GraphicsDevice device, int width, int height, Color color)
{
    var texture = new Texture2D(device, width, height);
    var data = new Color[width * height];
    Array.Fill(data, color);
    texture.SetData(data);
    return texture;
}
```

等距瓦片的菱形形状可以通过生成带透明背景的菱形纹理实现，或者在绘制时使用简单的几何计算。

---

## 验收标准

- [ ] 运行后看到 20×20 的等距棋盘格地图
- [ ] 地图中有一个红色角色精灵
- [ ] WASD 可控制角色沿等距方向移动
- [ ] 角色移动时镜头跟随，地图随之滚动
- [ ] 角色绘制在瓦片之上（正确的绘制顺序）
- [ ] 帧率可见且稳定在 60 FPS
- [ ] 角色可以走出初始可见区域，镜头持续跟随

---

## 完成后的状态

Milestone 1 完成意味着：
- FNA 在 macOS 上完全可用
- 等距渲染管线基本通了
- 输入 → 状态更新 → 渲染 的主循环已建立
- 镜头系统已有雏形
- 我们的协作模式（Claude Code 写代码 + 开发者调试图形）被验证

准备进入 Milestone 2：深度排序 + 多层建筑遮挡。
