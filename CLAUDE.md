# CrossGuard (LP_V1)

Unity 第一人称对抗机器人原型。纯 Unity 跑通玩法，架构上预留了向 **VR（Quest 3）** 和 **实体机器人（S100）+ 触觉手环（ESP32）** 演进的接缝。

- **引擎**：Unity 6（6000.0.60f1），URP（PC + Mobile 两套渲染设置）
- **命名空间**：`CrossGuard`
- **代码目录**：`Assets/Scripts/`
- **主场景**：`Assets/Scenes/SampleScene.unity`
- **远程仓库**：https://github.com/xiangpengyu666/CrossGuard_VR （分支 `main`）

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
  - `Player/PlayerController.cs` — 第一人称 WASD + 鼠标视角 + 奔跑(Shift)/跳跃(Space)；输入隔离在 `Read*Input()` 里为 VR 预留。
  - `Player/PlayerHealth.cs` — 血量与伤害入口，按 `heavyThreshold` 分类伤害并触发事件。
- **Enemy**
  - `Enemy/IEnemyPoseSource.cs` — 敌人姿态接口（SEAM #2）。
  - `Enemy/LocalAiPoseSource.cs` — 当前实现：NavMesh 风筝 AI（追击/后退/保持距离）。
  - `Enemy/NetworkPoseSource.cs` — 未来实现：真实机器人 WiFi 姿态流（传输方案待定）。
  - `Enemy/EnemyRobot.cs` — 攻击循环：进入射程 → 预警 → 发射弹丸。
  - `Enemy/Projectile.cs` — 直线飞行弹丸，命中走 `PlayerHealth.TakeDamage`。
- **Integration**
  - `Integration/HapticBandListener.cs` — SEAM #1 示例订阅者，当前仅打印日志。
- **Editor**
  - `Editor/PlayerSetup.cs` — 编辑器菜单 `CrossGuard > Setup First-Person Player`：一键在场景装配玩家(CharacterController + PlayerController + 把 Main Camera 收编为子相机/眼睛)。可重复运行，已有玩家则跳过。

## 约定

- 改动脚本后用 UnityMCP 的 `read_console` 检查编译错误，确认无误再继续。
- 路径默认相对 `Assets/`，用正斜杠 `/`。
- 保持现有代码风格：注释密度、命名、`CrossGuard` 命名空间。

---

## 编辑记录 (Changelog)

> 每次对该项目做出编辑后，在此**按时间倒序**追加一条：日期、改了什么、为什么、涉及文件。
> 约定（用户要求）：所有新编辑都要记录进本文件。

### 2026-07-08
- 把《英雄联盟》希瓦娜(骸骨之爪)模型做成 boss 资产并接上追击/攻击：① 用 Blender 5.1 无界面把 `骸骨之爪_希瓦娜.glb` 转 `Assets/Shyvana_Dragonslayer.fbx`(Generic rig，48 动画)；② FBX 内嵌贴图失败(白模)，从 glb 抽出 1024² 漫反射图存 `Assets/ShyvanaTextures/`，建 URP/Lit 材质 `Shyvana_Body.mat` 并用 ModelImporter 材质重映射持久接上；③ 场景实例 `Shyvana_Boss` 缩放 0.01(约 3.4m 高)、脚落地、面向玩家、加 Animator；④ 把 Idle/Run 类 clip 设循环，建 `Assets/Shyvana_Boss.controller`(状态 Idle/Run/Attack/Death，参数 Speed/Attack/Die)；⑤ 新增 `Enemy/BossChaser.cs`：按 tag 找玩家、直接转向追击(驱动 Speed)、进入 attackRange 停下并按冷却触发 Attack(不依赖 NavMesh 烘焙)。攻击目前仅播动画，未接 PlayerHealth 伤害。注：希瓦娜是 Riot 版权模型，仅作原型占位。（文件：`Assets/Shyvana_Dragonslayer.fbx`、`Assets/ShyvanaTextures/*`、`Assets/Shyvana_Boss.controller`、`Assets/Scripts/Enemy/BossChaser.cs`、`Assets/Scenes/SampleScene.unity`）
- 用 UnityMCP 直接在场景里装配玩家（不再需要手点菜单）：执行菜单 `CrossGuard > Setup First-Person Player` 生成 `Player`(CharacterController + PlayerController + Main Camera 收编为眼高 1.6 子相机)，并保存场景。（文件：`Assets/Scenes/SampleScene.unity`）
- 搭建可自由跑动的第一人称玩家。给 `Player/PlayerController.cs` 增加奔跑(LeftShift，`sprintMultiplier`)和跳跃(`Jump`/Space，`jumpSpeed`)，两处新输入按接缝约定隔离进 `ReadSprintInput()`/`ReadJumpInput()`，跳跃仅在 `isGrounded` 时触发。发现场景里根本没有玩家(只有做物理测试的 TestCube)，因 Unity MCP 授权被撤销无法直接拖搭，故新增编辑器脚本 `Editor/PlayerSetup.cs`：菜单 `CrossGuard > Setup First-Person Player` 一键装配 Player(CharacterController h=1.8/r=0.3 + PlayerController)并把 Main Camera 重挂为眼高 1.6 的子相机，带 Undo、可重复运行。用户需在 Unity 里点该菜单完成场景装配。（文件：`Assets/Scripts/Player/PlayerController.cs`、`Assets/Scripts/Editor/PlayerSetup.cs`、`CLAUDE.md`）

### 2026-07-07
- 初始化独立 git 仓库并推送到 GitHub：为 LP_V1 单独 `git init`（分支 `main`，与外层 Desktop 仓库分离），加 Unity 专用 `.gitignore`（忽略 Library/Temp/Logs/obj/UserSettings、.csproj/.sln/.vscode 等）和 `.gitattributes`（统一换行、YAML 智能合并、二进制标记）。首次提交 93 个文件，作者身份修正为 `Xiangpeng <xiangpengyu020104@gmail.com>`。远程：https://github.com/xiangpengyu666/CrossGuard_VR （文件：`.gitignore`、`.gitattributes`）
- 新增 `Player/TestCubeMover.cs`：给 TestCube（已带 Rigidbody）做基于物理的 WASD 移动。用 `AddForce`(ForceMode.Acceleration) + 限速，带惯性和真实物理碰撞；`Awake` 里设 `linearDamping` 给滑行/停下的手感，并冻结 X/Z 旋转防翻倒。输入 Update 读、FixedUpdate 施力。已挂到场景 TestCube 上，编译无误。（文件：`Assets/Scripts/Player/TestCubeMover.cs`、`SampleScene.unity`）
- 创建本 `CLAUDE.md`：记录项目结构与后续编辑历史。（文件：`CLAUDE.md`）
