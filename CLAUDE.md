# CrossGuard (LP_V1)

**CrossGuard —— 一个 Meta Quest 3 passthrough 混合现实(MR)剑斗游戏**：玩家戴头显、挥一把（带传感器的）真剑，去打一台会移动的**真实机器人**；机器人在 MR 里被"皮肤化"成虚拟 boss。机器人的攻击**只在虚拟层结算、绝不物理碰到玩家**，被击中时玩家腰上的**触觉手环**震动。

> GIX LAUNCH 学生项目 (2026–2027)。团队：**Xiangpeng**（机器人 / ROS2 / CV / VR）、**Cindy**（连接设备 / 3D·机械 / 电路 / VFX）、**Lewis**（连接设备 / 用户研究 / 硬件）。
> 当前仓库 `LP_V1` 是**纯 Unity 玩法原型**：先把"第一人称剑斗 vs boss"的游戏层跑通，架构上留好接缝，之后逐步换上 VR + 真机器人 + 触觉硬件。

## 北极星 / 产品定义

- **核心体验**：MR 里挥真剑近战一个会动的 boss，命中有真实空间感 + 触觉反馈；boss 的攻击只在虚拟层演出（安全，机器人不碰人）。
- **商业模式**：**一个平台，多个皮肤** —— 同一台机器人靠软件换皮成不同 boss（希瓦娜 / 亚托克斯 / … 就是"多个 title"）。目标客户：VR 街机 / 商场线下娱乐场馆（经济性靠高翻台率 + 频繁换内容）。
- **MVP**：单机器人 boss 战，~40m² 场地，玩家与机器人**都在动**；命中判定 = 剑柄冲击传感器触发 **且** co-localization 判定剑落在 boss 判定范围内；腰带手环 on-hit 震动。
- **硬件**：Meta Quest 3（passthrough MR）、WHEELTEC S100（ROS2 移动机器人，载被动击打靶 Century BOB XL 假人躯干）、剑柄 ESP32+IMU 冲击检测、ESP32 腰带触觉手环。
- **Tier 1 必攻克**：① **co-localization**（玩家 + 剑 + 机器人共享一个空间坐标系，驱动敌人对齐 + 命中判定，主路线 Quest 3 自定位、备选外部房间传感）；② **安全边界**（限速 / 最小间距 / 地理围栏 / 掉追踪看门狗急停）。
- **安全红线**：机器人**永不物理接触玩家**，攻击全在虚拟层；慢→快渐进测试；有人参与的测试须先知情同意 + 安全审查。
- 原始文档在桌面：`CrossGuard_Proposal.docx`、`2026 Student Project Definition Deck_TEMPLATE.pptx`。

## 技术栈 / 仓库

- **引擎**：Unity 6（6000.0.60f1），URP（PC + Mobile 两套渲染设置）
- **命名空间**：`CrossGuard`；**代码目录**：`Assets/Scripts/`；**主场景**：`Assets/Scenes/SampleScene.unity`
- **远程**：https://github.com/xiangpengyu666/CrossGuard_VR （分支 `main`）。大 FBX 走 **Git LFS**（跟踪 `*.fbx`）。
- **本地工具**：Blender 5.1（无界面转 glb→fbx / 抽贴图）、UnityMCP（直接驱动编辑器）。

## 架构：两个接缝（SEAM）

平滑演进的核心 —— 原型阶段用软件实现，最终阶段替换成硬件，**游戏逻辑尽量零改动**。

| SEAM | 含义 | 现在（纯 Unity 原型） | 将来（MVP 硬件） |
|------|------|------|------|
| **#1** | 玩家被击中 / 攻击结算事件 | boss 攻击命中帧（telegraph **闪光瞬间**）；`GameEvents.OnPlayerHit` 现仅打印日志 | 转发到 ESP32 触觉手环震动 |
| **#2** | 敌人姿态来源 | `BossChaser` 直接转向追击 / `LocalAiPoseSource` NavMesh AI | 真实 S100 机器人 WiFi 姿态流（`NetworkPoseSource` 实现 `IEnemyPoseSource`）|

- 玩家移动/视角输入隔离在 `PlayerController.Read*Input()`、攻击输入隔离在 `SwordSwing.ReadAttackInput()`，转 VR 时只换这些 + XR Origin + 真剑传感器。
- **换皮 = 一个平台多 title**：不同 boss 是同一"敌人"的不同美术/动画皮肤，逻辑走统一的 chaser / telegraph。

## 脚本地图

- **Core** — `Core/GameEvents.cs`：静态事件中枢（SEAM #1）。`HitType`(Light/Heavy/Warning)、`HitInfo`、`OnPlayerHit`/`OnPlayerHealthChanged`。
- **Player**
  - `Player/PlayerController.cs` — 第一人称 WASD + 鼠标 + 奔跑(Shift) + 跳跃(Space)；输入隔离 `Read*Input()`。
  - `Player/PlayerHealth.cs` — 玩家血量与伤害入口（100HP），受击触发 `OnPlayerHit`(SEAM #1)。
  - `Player/FirstPersonBody.cs` — 读 CharacterController 水平速度驱动第一人称身体模型的 Idle/Run。
  - `Player/SwordSwing.cs` — 相机剑 viewmodel 左键向下劈（绕 `WeaponPivot`）；下劈帧从相机 `SphereCast` 打 boss（近战命中）；输入隔离 `ReadAttackInput()`。
  - `Player/CameraShake.cs` — 受击镜头震颤（SEAM #1 订阅者，`LateUpdate` 叠加）。⚠️VR 里禁用。
  - `Player/TestCubeMover.cs` — 早期物理移动测试（Rigidbody AddForce），非玩法主线。
- **Enemy**
  - `Enemy/IEnemyPoseSource.cs` — 敌人姿态接口（SEAM #2）。
  - `Enemy/LocalAiPoseSource.cs` — NavMesh 风筝 AI（远程 boss 用）。
  - `Enemy/NetworkPoseSource.cs` — 未来：真机器人 WiFi 姿态流。
  - `Enemy/EnemyRobot.cs` + `Enemy/Projectile.cs` — 早期"远程弹丸"boss 原型（预警→发射）。
  - `Enemy/BossChaser.cs` — **当前近战 boss 大脑**：找玩家、转向追击、进 attackRange 停下随机攻击、攻击时定身、命中闪光帧结算对玩家伤害；两阶段——第一条命见底触发变身(定身+无敌)→回满血 + 强化 + 恶魔招式。不依赖 NavMesh。
  - `Enemy/BossHealth.cs` — boss 血量，**两条命/两阶段**：非最后一条命见底→`OnPhaseAdvance`(变身)，最后一条→`Die`。
  - `Enemy/BossTelegraph.cs` — **攻击范围指示器**：跟随 boss，按招式显示矩形/半月/圆，读 Animator `normalizedTime` 同步蓄力填充 + 命中爆闪；`IsPointInShape` 供命中判定。
- **UI**
  - `UI/HealthBarUI.cs` — 血条（绑 BossHealth 或玩家 `OnPlayerHealthChanged`），初值在 `Start` 读。
  - `UI/DamageFlashUI.cs` — 受击红色闪屏（SEAM #1 订阅者）。**VR 安全、可留用**。
- **Integration** — `Integration/HapticBandListener.cs`：SEAM #1 示例订阅者（当前仅日志，将来接手环）。
- **Editor** — `Editor/PlayerSetup.cs`：菜单 `CrossGuard > Setup First-Person Player` 一键装配玩家。
- **Shader/FX** — `Assets/BossFX/Telegraph.shader`（`CrossGuard/Telegraph`，蓄力填充/亮边/闪光）。

## 场景现状 (SampleScene)

- **`Player`**（CharacterController + PlayerController + PlayerHealth，tag `Player`）。子物体：`Main Camera`（眼高 1.6 + `CameraShake`）；相机上的剑 viewmodel `WeaponPivot`（→ `ViewModel_Sword` + 占位手臂 `ViewModel_Arms`，`SwordSwing` 左键劈）；隐藏的全身 `PlayerBody`（易大师，藏了 Head 骨骼）。
- **`Aatrox_Boss`**（亚托克斯，~3.2m，脚在地板顶 y≈0.5）：Animator + `Aatrox_Boss.controller` + `BossChaser` + `BossHealth`(600HP×2命) + 子物体 `HitCapsule`(反缩放胶囊碰撞体) + 关联 `Aatrox_Telegraph`。
- **`Aatrox_Telegraph`**：`BossTelegraph` + 三指示器（Q1_Rect / Q2_Arc / Q3_Circle）。
- **`GameHUD`**（Screen-Space Overlay Canvas）：`BossBar`(顶,红,`HealthBarUI`) + `PlayerBar`(左下,绿) + `HitFlash`(全屏红,`DamageFlashUI`)。
- **场地**：若干 Cube 拼的墙 + 地板（地板顶面约 y=0.5）。
- 注：`Shyvana_Boss` 曾在场景、**现已移除**（资产仍在，可重新放）。

## 关键节点 (里程碑)

> 高层进度速览；逐条细节见下方"编辑记录"。

- **[基础] 玩家 & 移动** — 第一人称 CharacterController 玩家（WASD + 鼠标 + 跑 + 跳）+ 编辑器一键装配。
- **[管线] 模型工作流** — Blender 无界面 `glb→fbx` + 从 glb 抽贴图 + ModelImporter 材质重映射（解决 URP 白模），已跑通 3 个 LoL 模型。
- **[内容] boss #1 希瓦娜** — 追击 + 攻击动画（`BossChaser` 起点）。已从场景移除。
- **[内容] boss #2 亚托克斯**（当前主力）— 追击、攻击时定身、一次性变身（Taunt_Intro→ULT_Idle，0.5x、下降 0.3m）、普通/恶魔形态各随机 Q1/Q2/Q3（统一 2.5s）。
- **[玩家] 第一人称持剑** — 易大师全身（藏头骨）→ 改为相机挂载的剑 viewmodel + 左键向下劈 + 占位手臂。
- **[反馈] 攻击范围指示器** — `BossTelegraph` + 自定义 shader；矩形/半月/圆，蓄力填充 + 亮边 + 命中爆闪 + Bloom，与攻击动画同步、**命中(闪光)即结算点、之后即撤**。← 这个闪光帧就是将来触发触觉手环的时刻（**SEAM #1**）。
- **[战斗] 核心闭环** — 血量系统(boss 两命×600 / 玩家 100) + 命中判定(玩家剑 SphereCast / boss 命中帧形状判定) + 物理碰撞(反缩放胶囊) + 血条 HUD。boss 打空第一条命→变身回满+强化→打空第二条→死亡。
- **[反馈] 受击反馈** — 摄像机震颤(平面用) + 红色闪屏(VR 留用)，都挂 `OnPlayerHit`(SEAM #1) 可插拔。
- **[工程] 仓库** — 独立 git 仓库 + Unity `.gitignore`/`.gitattributes` + GitHub 远程 + Git LFS 管大 FBX。

**下一步候选**：`OnPlayerHit` 接真实手环桥（SEAM #1 硬件半边）；把 boss 姿态源收敛到 `IEnemyPoseSource`（SEAM #2 换真机器人）；VR/XR Origin 迁移 + 受击反馈切到红闪+手环；co-localization 研究；正式资产替换 Riot 占位模型。

## 约定

- 改动脚本后用 UnityMCP 的 `read_console` 检查编译错误，确认无误再继续。
- 路径默认相对 `Assets/`，用正斜杠 `/`。保持代码风格：注释密度、命名、`CrossGuard` 命名空间。
- **版权**：希瓦娜 / 亚托克斯 / 易 都是 Riot《英雄联盟》版权模型，**仅作原型占位**；正式/商用须换自有或可商用授权（CC0 / 授权）资产。
- **所有新编辑都要按时间倒序记进下方"编辑记录"**（用户要求）。

---

## 编辑记录 (Changelog)

> 每次对该项目做出编辑后，在此**按时间倒序**追加一条：日期、改了什么、为什么、涉及文件。

### 2026-07-08
- 受击视觉反馈（都挂在 `GameEvents.OnPlayerHit`/SEAM #1 上，可插拔）：① 新增 `Player/CameraShake.cs`（挂 Main Camera）——被击中用 Perlin 噪声抖镜头，按 HitType 分强度、`LateUpdate` 叠加不干扰视角、随时间衰减；② 新增 `UI/DamageFlashUI.cs` + HUD 全屏红图 `HitFlash`——屏幕闪红后淡出。**VR 迁移**：摄像机震颤在 VR 会晕、要禁用；红闪 + 触觉手环是 VR 安全反馈、直接留用（同一事件后面换响应器，逻辑零改动）。（文件：`Assets/Scripts/Player/CameraShake.cs`、`Assets/Scripts/UI/DamageFlashUI.cs`、`Assets/Scenes/SampleScene.unity`）
- 修 bug + boss 两条命/两阶段：① HUD 血条进游戏瞬间闪 0——因 `HealthBarUI.OnEnable` 早于 `BossHealth.Awake` 读到未初始化的 0，改为在 `Start` 读初值；② `BossHealth` 加"命/阶段"(maxLives=2)：非最后一条命见底不死而是 `OnPhaseAdvance`+拦伤，最后一条命见底才 `Die`；变身触发从"按时间"改成"按血量"——`BossChaser` 订阅 `OnPhaseAdvance`→播变身(定身+无敌)→完成后 `BeginNextPhase` 回满血 + 全面强化(伤害×1.8/移速×1.3/冷却×0.65)+改用 ULT 招式。注：调试时误在编辑模式跑到 `Die()` 把 `BossChaser.enabled` 关了并存进场景，已重新启用（教训：不在编辑模式对活对象跑"打到死"测试）。（文件：`Assets/Scripts/Enemy/BossHealth.cs`、`Assets/Scripts/Enemy/BossChaser.cs`、`Assets/Scripts/UI/HealthBarUI.cs`、`Assets/Scenes/SampleScene.unity`）
- 核心战斗系统（血量 + 命中判定 + 物理碰撞 + 血条 UI）：① `Enemy/BossHealth.cs`（Aatrox 600HP）+ 已有 `PlayerHealth`(加到 Player,100HP)；② `UI/HealthBarUI.cs` + Screen-Space `GameHUD` 画布（顶部 boss 大血条带名、左下玩家血条，平滑掉血）；③ 命中判定——玩家剑：`SwordSwing` 在下劈帧从相机做前向 `SphereCast` 打 boss(第一人称正解)；boss 攻击：`BossChaser` 在命中闪光帧用 `BossTelegraph.IsPointInShape`(矩形/半月/圆) 判定玩家→`PlayerHealth.TakeDamage`（走 SEAM #1）；④ 物理碰撞——给 Aatrox 加反缩放子物体 `HitCapsule`(CapsuleCollider，抵消 0.0104 缩放→世界半径 0.7m)，既能被剑 SphereCast 命中也挡住玩家穿过。伤害数值 Inspector 可调(剑 60/boss 攻击 15)。（文件：`Assets/Scripts/Enemy/BossHealth.cs`、`Assets/Scripts/UI/HealthBarUI.cs`、`Assets/Scripts/Player/SwordSwing.cs`、`Assets/Scripts/Enemy/BossChaser.cs`、`Assets/Scripts/Enemy/BossTelegraph.cs`、`Assets/Scenes/SampleScene.unity`）
- 整理 `CLAUDE.md`：读了桌面 `CrossGuard_Proposal.docx` 和 `2026 Student Project Definition Deck_TEMPLATE.pptx`，据此补写"北极星/产品定义"（Quest 3 MR + 真机器人换皮 boss + 触觉手环、MVP、硬件、Tier1、安全红线、团队），并重构文档结构：技术栈、SEAM 架构(连到硬件)、脚本地图(补全新脚本)、场景现状、关键节点(里程碑)。（文件：`CLAUDE.md`）
- 上 Git LFS 管理大 FBX：`git lfs track "*.fbx"`（写入 `.gitattributes`），新的大模型（Aatrox 69M、Yi 37M）走 LFS 提交。注意希瓦娜 FBX(28M) 已在更早的历史提交里、仍是普通 blob；若要把它也移出历史需 `git lfs migrate` + 强推（改写已推送历史），暂未做。（文件：`.gitattributes`）
- 亚托克斯 boss 攻击范围指示器(telegraph)：新增 `Enemy/BossTelegraph.cs` + 自定义 shader `Assets/BossFX/Telegraph.shader`(`CrossGuard/Telegraph`)。三个贴地半透明红指示器(`Assets/BossFX/` 网格+材质)：Q1 前方细长矩形、Q2 前方半月扇形、Q3 周身圆形，填充坐标烘进 UV.x。效果=蓄力填充+HDR 亮黄扫描边+命中爆闪(白)，给 Global Volume 加了 Bloom、相机开后处理。指示器读 boss Animator 的 `normalizedTime` 保持与攻击动画同步(`impactFraction=0.45` 对准命中帧，实测 Q1 命中在 nt≈0.45)；闪光=攻击结算瞬间，闪光后立即隐藏不残留。`BossChaser` 每次攻击按招式调 `telegraph.Show(shape,dur,atk)`。（文件：`Assets/Scripts/Enemy/BossTelegraph.cs`、`Assets/Scripts/Enemy/BossChaser.cs`、`Assets/BossFX/*`、`Assets/Scenes/SampleScene.unity`）
- 亚托克斯攻击/变身扩展(改 `Enemy/BossChaser.cs` + `Assets/Aatrox_Boss.controller`)：① 攻击时定身——攻击触发瞬间锁位不动不转，直到 `attackDuration` 结束；② 变身——交战 `transformAfterSeconds`(6s) 后触发一次，播 `Taunt_Intro`→自动进 `ULT_Idle` 循环(用 `anim.Play` 强制进入，trigger 手动步进不稳)，Transform 状态速度 0.5x、定身时长 5.58s、变身完成瞬间下降 0.3m；③ 攻击招式——普通形态随机 Q1/Q2/Q3、变身后随机 ULT_Q1/Q2/Q3，各状态速度按 `clip长/2.5` 统一成 2.5s，打完退出时间过渡回 Idle/UltIdle；④ 变身后用恶魔奔跑 `UltRun`(Ult_Run) 继续追击。（文件：`Assets/Scripts/Enemy/BossChaser.cs`、`Assets/Aatrox_Boss.controller`、`Assets/Scenes/SampleScene.unity`）
- 亚托克斯 boss 资产(同希瓦娜流程)：Blender 把 `海魔至尊_亚托克斯.glb` 转 `Assets/Aatrox_SeaMonster.fbx`(68M，Generic rig，118 动画)；从 glb 查出材质→贴图映射，5 张贴图存 `Assets/AatroxTextures/` 建 5 个 URP/Lit 材质并重映射；场景实例 `Aatrox_Boss` 缩放约 0.01→反复调整到约 3.2m、脚落地(+上移避免陷地)、隐藏特效网格 `Icosphere`、加 Animator + `Aatrox_Boss.controller`、挂 `BossChaser`。Riot 版权模型，仅原型占位。（文件：`Assets/Aatrox_SeaMonster.fbx`、`Assets/AatroxTextures/*`、`Assets/Aatrox_Boss.controller`）
- 易大师做第一人称玩家模型 + 持剑 viewmodel：① Blender 把 `墨之影武者_易.glb` 转 `Assets/Yi_InkShadow.fbx`(36M，52 动画)，7 张贴图存 `Assets/YiTextures/` 重映射；作为 `PlayerBody` 挂到 `Player` 下、缩放约 1.8m、脚对齐胶囊底、缩掉 `Head` 骨骼防第一人称穿头；建 `Yi_Player.controller`(Idle/Run 按速度)，新增 `Player/FirstPersonBody.cs` 读 CharacterController 水平速度驱动。② 现有全身第一人称手感差，改为剑 viewmodel：Blender 按材质分离出 `Assets/Yi_Sword.fbx`(剑)，挂相机做右下持剑视角、缩小到约 65%；新增 `Player/SwordSwing.cs`(左键向下劈，绕 `WeaponPivot` 枢轴)，并用基础几何体做占位深色手套手臂(`ViewModel_Arms`)。全身 `PlayerBody` 已隐藏。（文件：`Assets/Yi_InkShadow.fbx`、`Assets/Yi_Sword.fbx`、`Assets/YiTextures/*`、`Assets/Yi_Player.controller`、`Assets/Scripts/Player/FirstPersonBody.cs`、`Assets/Scripts/Player/SwordSwing.cs`、`Assets/Scenes/SampleScene.unity`）
- 提交并推送希瓦娜 boss 相关改动到 GitHub：commit `8369009`（模型 FBX、贴图/材质、Animator 控制器、`BossChaser.cs`、场景）。注意本次把 27MB 的 FBX 提交进仓库，二进制资源增多后可考虑上 Git LFS。（远程：https://github.com/xiangpengyu666/CrossGuard_VR）
- 把《英雄联盟》希瓦娜(骸骨之爪)模型做成 boss 资产并接上追击/攻击：① 用 Blender 5.1 无界面把 `骸骨之爪_希瓦娜.glb` 转 `Assets/Shyvana_Dragonslayer.fbx`(Generic rig，48 动画)；② FBX 内嵌贴图失败(白模)，从 glb 抽出 1024² 漫反射图存 `Assets/ShyvanaTextures/`，建 URP/Lit 材质 `Shyvana_Body.mat` 并用 ModelImporter 材质重映射持久接上；③ 场景实例 `Shyvana_Boss` 缩放 0.01(约 3.4m 高)、脚落地、面向玩家、加 Animator；④ 把 Idle/Run 类 clip 设循环，建 `Assets/Shyvana_Boss.controller`(状态 Idle/Run/Attack/Death，参数 Speed/Attack/Die)；⑤ 新增 `Enemy/BossChaser.cs`：按 tag 找玩家、直接转向追击(驱动 Speed)、进入 attackRange 停下并按冷却触发 Attack(不依赖 NavMesh 烘焙)。攻击目前仅播动画，未接 PlayerHealth 伤害。注：希瓦娜是 Riot 版权模型，仅作原型占位。（文件：`Assets/Shyvana_Dragonslayer.fbx`、`Assets/ShyvanaTextures/*`、`Assets/Shyvana_Boss.controller`、`Assets/Scripts/Enemy/BossChaser.cs`、`Assets/Scenes/SampleScene.unity`）
- 用 UnityMCP 直接在场景里装配玩家（不再需要手点菜单）：执行菜单 `CrossGuard > Setup First-Person Player` 生成 `Player`(CharacterController + PlayerController + Main Camera 收编为眼高 1.6 子相机)，并保存场景。（文件：`Assets/Scenes/SampleScene.unity`）
- 搭建可自由跑动的第一人称玩家。给 `Player/PlayerController.cs` 增加奔跑(LeftShift，`sprintMultiplier`)和跳跃(`Jump`/Space，`jumpSpeed`)，两处新输入按接缝约定隔离进 `ReadSprintInput()`/`ReadJumpInput()`，跳跃仅在 `isGrounded` 时触发。发现场景里根本没有玩家(只有做物理测试的 TestCube)，因 Unity MCP 授权被撤销无法直接拖搭，故新增编辑器脚本 `Editor/PlayerSetup.cs`：菜单 `CrossGuard > Setup First-Person Player` 一键装配 Player(CharacterController h=1.8/r=0.3 + PlayerController)并把 Main Camera 重挂为眼高 1.6 的子相机，带 Undo、可重复运行。用户需在 Unity 里点该菜单完成场景装配。（文件：`Assets/Scripts/Player/PlayerController.cs`、`Assets/Scripts/Editor/PlayerSetup.cs`、`CLAUDE.md`）

### 2026-07-07
- 初始化独立 git 仓库并推送到 GitHub：为 LP_V1 单独 `git init`（分支 `main`，与外层 Desktop 仓库分离），加 Unity 专用 `.gitignore`（忽略 Library/Temp/Logs/obj/UserSettings、.csproj/.sln/.vscode 等）和 `.gitattributes`（统一换行、YAML 智能合并、二进制标记）。首次提交 93 个文件，作者身份修正为 `Xiangpeng <xiangpengyu020104@gmail.com>`。远程：https://github.com/xiangpengyu666/CrossGuard_VR （文件：`.gitignore`、`.gitattributes`）
- 新增 `Player/TestCubeMover.cs`：给 TestCube（已带 Rigidbody）做基于物理的 WASD 移动。用 `AddForce`(ForceMode.Acceleration) + 限速，带惯性和真实物理碰撞；`Awake` 里设 `linearDamping` 给滑行/停下的手感，并冻结 X/Z 旋转防翻倒。输入 Update 读、FixedUpdate 施力。已挂到场景 TestCube 上，编译无误。（文件：`Assets/Scripts/Player/TestCubeMover.cs`、`SampleScene.unity`）
- 创建本 `CLAUDE.md`：记录项目结构与后续编辑历史。（文件：`CLAUDE.md`）
