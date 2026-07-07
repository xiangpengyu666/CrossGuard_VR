# CrossGuard (LP_V1)

Unity 第一人称对抗机器人原型。纯 Unity 跑通玩法，架构上预留了向 **VR（Quest 3）** 和 **实体机器人（S100）+ 触觉手环（ESP32）** 演进的接缝。

- **引擎**：Unity，URP（PC + Mobile 两套渲染设置）
- **命名空间**：`CrossGuard`
- **代码目录**：`Assets/Scripts/`
- **主场景**：`Assets/Scenes/SampleScene.unity`

## 架构：两个接缝（SEAM）

平滑演进的核心 —— 原型阶段用软件实现，最终阶段替换成硬件，**游戏逻辑零改动**。

| SEAM | 含义 | 现在 | 将来 |
|------|------|------|------|
| **#1** | 玩家被击中事件 | Console 打印日志 | 转发到 ESP32 触觉手环震动 |
| **#2** | 敌人姿态来源 | Unity NavMesh AI | 真实 S100 机器人 WiFi 姿态流 |

另外 `PlayerController` 把输入隔离在 `ReadMoveInput()`/`ReadLookInput()` 两个方法里，转 VR 时只替换这两处 + 换 XR Origin。

## 脚本地图

- **Core**
  - `Core/GameEvents.cs` — 静态事件中枢（SEAM #1）。`HitType`(Light/Heavy/Warning)、`HitInfo` 结构体、`OnPlayerHit`/`OnPlayerHealthChanged` 事件。
- **Player**
  - `Player/PlayerController.cs` — 第一人称 WASD + 鼠标视角；输入隔离为 VR 预留。
  - `Player/PlayerHealth.cs` — 血量与伤害入口，按 `heavyThreshold` 分类伤害并触发事件。
- **Enemy**
  - `Enemy/IEnemyPoseSource.cs` — 敌人姿态接口（SEAM #2）。
  - `Enemy/LocalAiPoseSource.cs` — 当前实现：NavMesh 风筝 AI（追击/后退/保持距离）。
  - `Enemy/NetworkPoseSource.cs` — 未来实现：真实机器人 WiFi 姿态流（传输方案待定）。
  - `Enemy/EnemyRobot.cs` — 攻击循环：进入射程 → 预警 → 发射弹丸。
  - `Enemy/Projectile.cs` — 直线飞行弹丸，命中走 `PlayerHealth.TakeDamage`。
- **Integration**
  - `Integration/HapticBandListener.cs` — SEAM #1 示例订阅者，当前仅打印日志。

## 约定

- 改动脚本后用 UnityMCP 的 `read_console` 检查编译错误，确认无误再继续。
- 路径默认相对 `Assets/`，用正斜杠 `/`。
- 保持现有代码风格：注释密度、命名、`CrossGuard` 命名空间。

---

## 编辑记录 (Changelog)

> 每次对该项目做出编辑后，在此**按时间倒序**追加一条：日期、改了什么、为什么、涉及文件。

### 2026-07-07
- 新增 `Player/TestCubeMover.cs`：给 TestCube（已带 Rigidbody）做基于物理的 WASD 移动。用 `AddForce`(ForceMode.Acceleration) + 限速，带惯性和真实物理碰撞；`Awake` 里设 `linearDamping` 给滑行/停下的手感，并冻结 X/Z 旋转防翻倒。输入 Update 读、FixedUpdate 施力。已挂到场景 TestCube 上，编译无误。（文件：`Assets/Scripts/Player/TestCubeMover.cs`、`SampleScene.unity`）
- 创建本 `CLAUDE.md`：记录项目结构与后续编辑历史。（文件：`CLAUDE.md`）
