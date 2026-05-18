# JingJuUnity — 2.5D 等距开发路线图

> 参考：[Unity 等距 Tilemap 教程](https://www.bilibili.com/video/BV1JJ41197gw/) · [极乐迪斯科](https://www.bilibili.com/video/BV1hw5J6CEUm/) · [桃源深处有人家](https://www.bilibili.com/video/BV1K34y1t737/)

## 分工说明（0.2 等地图资源）

| 内容 | 负责方 |
|------|--------|
| 等距瓦片 PNG / 正式美术 | 美术 |
| Tile 资源导入、Palette | 美术 + 程序协助 |
| 测试地图铺设、碰撞层、边界 | **程序**（编辑器 Tilemap） |
| 占位测试地图（Phase 0） | **程序**（`JingJu/Setup Isometric Demo Scene` 一键生成） |

---

## Phase 分工一览

| 问题 | 属于哪个 Phase | 说明 |
|------|----------------|------|
| 等距地图、占位 tile、围墙 | **Phase 0** | 程序一键生成，非随机 |
| WASD / 点击移动 | **Phase 1** | 已实现（需 Player 挂 `IsometricPlayerController` 并绑定 Ground/Obstacle） |
| 相机跟随、边界、缩放 | **Phase 2** | 已实现（需重新 Run Setup） |
| UI 随窗口缩放、不随 zoom | **Phase 3** | HUD 框架已搭（占位） |

**测试地图不是随机的**：固定 18×14 格，绿色=可走，**红色菱形=围墙障碍**。早期版本在地图中间藏了几块障碍（无清晰颜色），会像「空气墙」——已去掉，只保留外圈围墙。

---

## Phase 0 — 项目底座 ✅

- [x] **0.1** Isometric `Grid` + `Ground` / `Obstacle` Tilemap 层
- [x] **0.2a** 程序占位 tile + 测试地图（可走区域 + 围墙边界）
- [x] **0.2b** Producer 城镇 sample tiles → `JingJu/Setup Town Map (Producer Tiles)`
- [ ] **0.2c** 完整城镇 tileset + 地图布局稿（待更多资源）
- [x] **0.3** 玩家占位：Sprite、`Rigidbody2D`、Collider、Y 轴排序
- [x] **0.4** 可走/不可走（Obstacle 层 + TilemapCollider）、`MapBounds` 供相机 clamp

**验收**：运行场景，角色在平地内，撞墙停，不能走出围墙。

---

## Phase 1 — 移动 ✅ 实现中

- [x] **1.1** 键盘 WASD 移动（等距平面）
- [x] **1.2** 鼠标点击地面 → 行走到目标格中心
- [x] **1.3** 双模式：按住键盘时取消点击目标，优先键盘
- [x] **1.4** 不可走格 / 物理碰撞

**验收**：WASD 能动；点空地走过去；点墙不动；键盘可打断点击移动。

---

## Phase 2 — 相机（星露谷式）✅ 实现中

- [x] **2.1** 正交相机 `SmoothDamp` 跟随
- [x] **2.2** 地图边缘 clamp（`MapBounds`）
- [x] **2.3** **Ctrl + 滚轮** 缩放；**UI `+` / `-` 按钮**（不用键盘 `=` `-`）
- [x] **2.4** 仅改相机 zoom，不涉及 UI（UI 在 Phase 3）

**验收**：角色移动相机平滑跟随；贴边时相机不越界；`=/-` 缩放场景，UI 尚未接入。

---

## Phase 3 — UI 框架 ✅ 基础版

- [x] **3.1** Canvas 1920×1080，`Scale With Screen Size`（`GameUI`）
- [x] **3.2** Screen Space Overlay — 相机 zoom 不改变 HUD 大小
- [x] **3.3** 相机缩放条：`-`（可点）| 滑动条 | `+`（可点）；另支持 Ctrl+滚轮
- [x] **3.4** 主界面 HUD 占位（64px 网格）：角色信息、任务/换装/歌唱槽、地图、背包、工具栏
- [ ] **3.5** 槽位接真实按钮与功能（任务 / 换装 / 对话 — 后续）
- [ ] **3.6** 替换 `Assets/Information/` 美术稿皮肤

---

## Phase 4 — 整合验收

- [ ] **4.1** Demo 场景全流程
- [ ] **4.2** Inspector 可调参数表（移动速度、相机平滑、zoom 范围）

---

## 快速开始（程序）

1. 打开 Unity，打开 `Assets/Scenes/SampleScene.unity`
2. 菜单 **JingJu → Setup Isometric Demo Scene**（地图 + 玩家 + 主界面 HUD + 相机缩放 UI）
3. 使用 Producer 城镇贴图：**JingJu → Setup Town Map (Producer Tiles)**
4. 仅刷新 HUD：**JingJu → Setup Main HUD UI**
4. Play：**WASD** 移动 · **鼠标左键** 点击行走 · **Ctrl+滚轮** 或右下角 **- [滑动条] +** 缩放相机

> 若相机不跟随：菜单再执行一次 **Setup Isometric Demo Scene**，并确认 Main Camera 上有 `StardewStyleCamera2D`，Target 指向 Player。

> **不要手动删单个 Tile 文件**：若只删 `.png` 留下 `.asset`，围墙会消失。直接再点 **Setup Isometric Demo Scene** 即可（脚本会自动检测并重建损坏的 Tile）。

## 脚本索引

| 脚本 | 职责 |
|------|------|
| `IsometricPlayerController` | 键盘 + 点击移动 |
| `StardewStyleCamera2D` | 跟随、边界、缩放 |
| `YSortByPosition` | 2.5D 按 Y 排序 |
| `Editor/IsometricDemoSceneSetup` | 一键搭建 Phase 0–3 场景 |
| `Editor/MainHUDUISetup` | 主界面 HUD 占位布局（64px 网格） |
| `MainHUDLayout` | HUD 根节点标记 |
