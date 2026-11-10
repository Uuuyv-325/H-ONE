# 占位美术资源替换 Checklist

首次盘点：2026-11-08；本次更新：2026-11-11。范围：第一章灰盒、可复用 Prefab、场景直挂视觉和程序生成视觉；含未提交文件。已按更新后的 `../H-ONE-arts/README.md` 试装外部交付，**部分条目为已接入待验收，完整条目正式验收仍为 0 项**。现有 P0 七图保留给尚缺状态、灰盒与工具，不再等同于当前全部画面。

最新素材映射、裁切/导入规格、使用授权、Windows 实测及限制见 §7 与[美术试装记录](09-美术试装记录.md)。[原核查报告](08-H-ONE-arts素材核查.md)仅保留 11-08 的历史文件名/权限快照，不能作为当前使用规范。

[美术交付清单](02-美术交付清单.md)定义“要交什么、规格和批次”；本文跟踪“现在用了什么、在哪里替换、替换到哪一步”。玩法实现及运行历史见[开发进度](04-开发进度.md)，音频仍由[音频交付清单](03-音频交付清单.md)管理。本文不改变原案、制作规则或 P0→P1→P2 的顺序。

## 1. 首次静态盘点结论与范围（2026-11-08 快照）

| 已核查对象 | 当前结果 |
| --- | --- |
| `Assets/Game/Art/P0/` | 7 张 PNG + `Trajectory.mat` + 占位说明及 `.meta`；不是正式 P0 样板包 |
| 图片源 | 本机 `tools/p0/generate_art.py` 使用 Python/Pillow 绘制几何图形；[来源说明](../Assets/Game/Art/P0/README.md)注明原创、未使用外部图片/字体/样本；2026-11-09 起整个 tools 仅本地保留，不随新检出提供 |
| 第一章使用位置 | `ChapterPlayer.prefab`、10 个机关 Prefab、`Chapter01_Core.unity`、`Chapter01_ShadowSlice.unity`；两场景均为技术灰盒，不是正式两关 |
| 无独立文件的视觉 | 轨迹 LineRenderer、缩放/换色/透明度反馈、相机纯色背景、uGUI 纯色面板/按钮、内置字体、教学剪影/圆环组合 |
| 尚未找到的交付 | `Assets` 中无正式角色动画片段/Controller、SpriteAtlas、导入字体、分层美术源文件；三乐器、正式出口、成套 HUD/教学和背景仍缺交付 |
| 旧原型与渲染配置 | `Northline` 等原型、迷雾材质/Shader、URP 设置单独保留，见 §5；不能统称为待换图片 |

核查方式：读取设计文档、PNG/生成脚本、`.meta`、场景/Prefab 的序列化 GUID 引用以及表现/制作代码；查看七图合览。另只读核对两份 Git 状态和磁盘文件：**WSL/Windows 的 `Assets/Game` 各 194 个文件（含 `.meta`），无缺失、差异或额外文件**；图片生成脚本仅在 WSL 找到。旧 `Northline.unity` 和 `MapRevealFog.mat` 两份内容不同，各自保留。这个一致性结论仅适用于本次快照，后续同步前必须重查。

本轮仅整理文档，没有替换、移动或删除资源，没有同步 Windows 文件，也没有连接 Editor 或进行 Play Mode 验收。磁盘一致不证明运行中的 Editor 已加载这些内容。

## 2. 原 P0 图片与材质台账

以下为 11-08 原 P0 的复用关系；11-09 已拆开的用途见 §3/§7，不表示所有用途仍引用 P0。路径前缀为 `Assets/Game/Art/P0/`。七图均为 128 PPU、Multiple 模式、各含一个子 Sprite；源画布与实际切片范围不同。`rect` 采用 Unity 左下角原点的 `x,y,w,h`，不是新资产的制作规格。

| 当前文件 | 源画布 / 当前 rect（px） | 当前实际用途 | 对应替换项 |
| --- | --- | --- | --- |
| [human.png](../Assets/Game/Art/P0/human.png) | 256×256 / `54,28,136,210` | 玩家人形；M2 静态教学剪影 | ART-001、022 |
| [star.png](../Assets/Game/Art/P0/star.png) | 128×128 / `12,19,104,99` | 玩家星弹；两种槽的星影核心 | ART-002、012 |
| [ring.png](../Assets/Game/Art/P0/ring.png) | 128×128 / `4,4,120,120` | 蓄力/重组环；轨迹终点；M2 地面教学圆环 | ART-003、004、021、022 |
| [stone.png](../Assets/Game/Art/P0/stone.png) | 128×128 / `0,0,128,128` | 地墙/平台、背景柱、星影承重条、桥、普通门、星石、危险条 | ART-008–010、013、016–019 |
| [fragment.png](../Assets/Game/Art/P0/fragment.png) | 128×128 / `8,7,111,109` | 世界收集片；Core 已收集节点 | ART-005、006 |
| [empty_node.png](../Assets/Game/Art/P0/empty_node.png) | 128×128 / `8,7,111,109` | Core 未收集节点；两种星槽的空槽 | ART-006、011 |
| [dais.png](../Assets/Game/Art/P0/dais.png) | 128×128 / `1,13,126,56` | 星台；独立压力板；压板型槽的本地压力板 | ART-007、015 |
| [Trajectory.mat](../Assets/Game/Art/P0/Trajectory.mat) | 材质，无自有纹理 | 两灰盒的轨迹线，创建代码使用 `Sprites/Default` | ART-020 |

引用追踪必须沿“图片 `.meta` GUID → 场景/Prefab → Prefab 实例及覆盖”查询；场景可能只保存 Prefab 引用，搜不到图片 GUID 不代表没有使用。还要查脚本的路径加载与运行时创建。

**不能通过覆盖一张共用 PNG 完成整组替换。** 例如 `stone.png` 同时承担背景、实体、桥和危险提示，正式版必须按用途拆开；`empty_node.png` 的收集节点和星槽也不是同一种资源。保留技术占位供灰盒使用是允许的，正式场景移除占位依赖和旧原型继续保留应分别记录。

## 3. 已有占位的替换 Checklist

以下保留 23 个 ART ID；所有父项仍为 `[ ]`，按用途记录部分接入和缺失状态。11-09 新交付共用 `Assets/Game/Art/Delivery01/`（下称 `D/`）；可编辑裁切/布局配方为 `tools/art/prepare_delivery.py`，修订 `delivery01-20261109`，逐图源指纹/rect/PPU/pivot 见 `D/manifest.json`，来源使用规范见 `D/SOURCE_USAGE.md`。原始栅格源位于 `../H-ONE-arts`；分层绘画工程、独立作者/第三方来源授权清单仍未交付。Windows 已导入的具体范围与运行证据见[试装记录](09-美术试装记录.md)。P0→P1→P2 的正式批次不因提前试装 P2 压板/星石而改动。

### 3.1 角色、收集与安全点

- [ ] `ART-001` 人形与动作 — **部分已接入待验收｜P0→P1；推石 P2｜CH-01–06、CH-11**。`sprite_03.PNG` → `D/human_idle_00–03.png`、`human_walk_00–03.png`；`sprite_01.PNG` → `human_launch_00–09.png`。接入 `ChapterPlayer.prefab/Visual/Human|Star`、`PlayerView.cs` 和两灰盒实例；1024×832 待机/行走画布、512 PPU、统一脚底 pivot，832² 变星画布，发射前 0.18 s 与真实飞行重叠。跳落/推石/蓄力专用帧、成功动作和完整 CH 状态仍缺；目前这些人形状态回退待机。11-09 样例运行不代表整组动作或 P0 风格已签收。 **本轮更新：** 2026-11-10 补 `Delivery02/human_jump.png`：`jump.png` 单帧、1024×832/512 PPU/脚底 (512,32)，`ChapterArt.Jump` / `PlayerView` 随真实离地/落地切换，跳落共用，完整专用动作仍缺。

- [ ] `ART-002` 星弹主体 — **部分已接入待验收｜P1｜CH-07**。`sprite_02.PNG` → `D/star_00–03.png`，704²、512 PPU；`ChapterPlayer.prefab/Visual/Star` 由 `PlayerView` 6 fps 循环，显示 scale 0.55，原查询半径不改。月桂/光晕独立拆层尚缺，四帧运动轮廓/高速可读性仍需美术确认；星槽内的星影核心继续用原 P0，未全局覆盖 `star.png`。

- [ ] `ART-003` 蓄力环与满蓄 — **占位｜P1｜UI-02、FX-02**。旧 `ring.png` 制作引用保留，新组件有效时仅在蓄力状态隐藏旧环（飞行/重组环仍在），另有 `GameplayFeedbackView` 程序进度/满蓄层（2026-11-10，§11）；接入玩家 `Visual/ChargeRing`、`PlayerView.cs` 与独立 `ProceduralFeedback`。正式交付仍需底环/进度遮罩/满蓄闪层，明确未满与满蓄；方向、进度和角色剪影不能互相遮挡。

- [ ] `ART-004` 失败消散与星台重组 — **占位｜P1｜CH-09、FX-07**。当前 `PlayerView.cs` 保留圆环，2026-11-10 增加 `ImpactFeedbackView` 程序消散/聚合样板（§11），正式完整炸散/重聚素材仍未交付。补齐独立效果并接到失败/重组状态，约 0.5 秒恢复控制由玩法决定；消散后返回位置和安全星台可读。

- [ ] `ART-005` 世界星座碎片与收集反馈 — **部分已接入待验收｜P0→P1/P2｜ME-01、FX-05**。`sprite_02.PNG` → `D/fragment_00–03.png`，256²、256 PPU；`Fragment.prefab/Visual`、Core `F1/Visual`、两场景实例由 `SpriteSequenceView` 转动，保留原 `StarFragment` 触发器/稳定 ID。尚为共用黄色收集物，缺两关各三片独特星图、空位、HUD/门一致映射和正式收集 VFX；2026-11-10 加程序收片/真实 ID HUD 样板，见 §11，Core 的 ART-006 世界节点未替换。

- [ ] `ART-006` 收集进度节点 — **占位｜P1/P2｜UI-01、ME-15 的节点**。当前 Core `LevelRoot/Mechanisms/CollectedFragmentNode` 在 `empty_node.png` / `fragment.png` 间切换；`SessionView.cs` 只判断是否收过任意一片。替换需实际三片图形进度和门连线支持；此节点不是已实现的正式 HUD 或星座出口。完整 HUD/出口另见 NEW-006/007。

- [ ] `ART-007` 星台 — **部分已接入待验收｜P1｜ME-13**。`spawn-point.PNG` → `D/checkpoint.png`，Alpha 裁边/最大 1024、512 PPU；接入 `Checkpoint.prefab/Visual`、Core `EntryDais|SecondDais` 和 M2 星台，绘制于玩家后方。`SafeSpawn` / `FallbackSpawn` / Trigger 原样保留。只交付未点亮外观，缺激活/当前重组点/独立光环，不能以相同图案宣称激活状态已完成；星台和压力板已分开引用。

### 3.2 地形与背景

- [ ] `ART-008` 实体地墙 — **部分已接入待验收｜P0→P1｜ENV-01**。原拉伸 `stone.png`，本轮已接方形草地格点表现；接入 `Solid.prefab`、Core `Geometry` 直挂视觉和 M2 地形实例。用模块拼接、切片或平铺替换拉长色块，墙角/顶面/收口连续，视觉碰撞边不漂移；薄墙与斜墙仍遵循各自物理轮廓。 **本轮更新：** 2026-11-10 改为**部分已接入待验收**：`Delivery02/terrain_middle|left|right|soil.png`、`terrain_atlas.png|GrassTerrain.mat`；`Solid.prefab/GrassCells`、Core/ShadowSlice/Gallery 普通静态实体；`GrassTerrainView` 在原 BoxCollider2D 区块内按 1-unit 格铺图、按邻接选端头/中段/土层，零散边缘裁 UV，不拉长整张图。`DeliveredArtAuthoring.ApplyToScene`、Core 制作收尾、`LevelPieceFactory`、`M2SceneAuthoring.Resize`、Runtime Catalog / 元数据导入与运行编辑器 resize 同步维护；保留旧 Visual 节点但停用渲染器，物理根/稳定 ID/出生点不变。

- [ ] `ART-009` 普通平台与台阶 — **部分已接入待验收｜P1｜ENV-02**。原同用 `stone.png`，本轮已接方形草地格点表现，例如 Core `JumpLedge` / `FarLanding`，也可由 Solid 元件制作。替换为有厚度和端部的可站立结构，验脚踏面与碰撞线；不与星影承重面、星桥、装饰残垣混淆。 **本轮更新：** 2026-11-10 改为**部分已接入待验收**：复用 ART-008 的方图网格表现，包含 Core `JumpLedge|FarLanding` 及 Gallery 台阶；薄平台裁切图内 UV，保留原碰撞顶面和尺寸。神庙平台独立模块、端部与美术圆角的正式碰撞轮廓验收仍缺。

- [ ] `ART-010` 背景与柱子 — **部分已接入待验收｜P1｜ENV-03–05**。`bg-level1.PNG`、`fg-level1-grass.PNG` → `D/background.png|foreground_grass.png`（1920×1080、128 PPU）；两场景 `LevelRoot/Background/DeliveryBackground|DeliveryForeground` 由 `CameraBackdrop` 覆盖视口跟随，无碰撞，sorting -100/40。Core 六个旧装饰柱仅停用渲染器，不删节点。尚缺分层神庙/柱套件、逐房间构图与正式两关背景验收；前景遮挡边界随关卡继续复核。

### 3.3 星槽、星影与持续机关

以下机关 Prefab 均在 `Assets/Game/Prefabs/Mechanisms/`。核查共享 Prefab 后仍须检查场景实例覆盖；当前 `CoreSceneAuthoring.cs`、`LevelPieceFactory.cs`、`M2SceneAuthoring.cs` 还会加载 P0 路径，正式制作入口和默认引用也要一起维护，保留灰盒入口须注明。

- [ ] `ART-011` 空槽/占用/不可用槽 — **部分已接入待验收｜P0→P1｜ME-02**。2026-11-10 按用户要求，将两型槽根下 `Visual` 从 `P0/empty_node.png` 替换为 `rock-dark.PNG|rock-light.PNG` → `D/rock_dark.png|rock_light.png`，分别表示空 / 实际占槽。沿用 `delivery01-20261109` 原图源指纹、裁切和 995×1024 / 512 PPU / 中心 pivot，接入修订 `socket-art-20261110` 见 `D/manifest.json.integrations.star_socket` / `SOURCE_USAGE.md` / `D/README.md`；分层源和独立授权清单仍缺。接入 `PlatformSocket.prefab|PlateSocket.prefab/Visual`，实例为 ShadowSlice `LevelRoot/Mechanisms/Puzzle_A/PlatformSocket|PlateSocket`、Gallery `MechanismGallery/Mechanisms/02_ShadowBridge/PlatformSocket|BridgeSocket`。`StarSocketArtView.cs` 只读实际占用，召回完整 1 秒后变暗；`ShadowFeedback` 对已接入槽跳过旧整体染色。`DeliveredArtAuthoring.ApplyToPiece|ApplyToScene`、`LevelPieceFactory` / `M2SceneAuthoring` 新建默认和 Runtime Catalog 已覆盖；临时场景实测新建两型槽均为暗图、单个表现组件、高 0.7 unit。原根/吸附区/碰撞/稳定 ID/出生点保留，核心排序在岩石之后、承重条在前。Windows 已导入、保存并回写 WSL；Gallery 实际 Input Actions 发射入槽，召回/暂停/失败/复位/重开及 ShadowSlice 压板召回通过；720p/1080p/16:10、静音/无后处理和离线灰度局部检查见 09 记录。2026-11-10 补可嵌开口/额度锁形的程序样板（§11）；正式独立外框/光层与完整 TUT-03/04 仍缺，父项不勾选。

- [ ] `ART-012` 星影主体 — **占位｜P1｜CH-08**。两种槽 `ShadowPlatform/Visual` 内以 `star.png` 作核心；11-10 起排序在 ART-011 岩石层之后，保留独立召回缩放，不覆盖原画星纹（`DeliveredArtAuthoring`，见 §9；源仍 P0，正式 CH-08 未交）。补半透明、中空/月桂轮廓和独立星点；实际入槽后看得出本体已返回、此处留下不可操控星影；与 ART-022 的演示幽灵区分。

- [ ] `ART-013` 星影承重面 — **占位｜P1｜ME-03**。两种槽的 `ShadowPlatform` 使用拉伸 `stone.png` 作顶条；11-10 起排序在 ART-011 岩石之前，2 / 0.9 unit 宽和高 0.18、位置与原 Collider 对齐，图源/PPU/pivot 未变（`DeliveredArtAuthoring`，§9）。补显现/稳定/撤除的独立顶面与星尘；核对两型实际 Collider 尺寸和 offset，不能按“默认 2 unit”盲配所有实例。脚底准确落在可见承重线上。

- [ ] `ART-014` 召回视觉 — **占位｜P1｜CH-10、FX-08**。当前 `ShadowFeedback.cs` 仅缩放 `ShadowPlatform/Visual` 的第一个核心子物体，承重条保持；11-10 新岩石槽本体不缩放，完整召回结束才切回暗图，暂停/复位时序见 §9 / 09 记录。2026-11-10 补程序召回弧/星点回流（§11），正式回流纹理/拆层素材仍缺。正式交付需完整 1 秒收拢、回流和完成态；完成前承重轮廓保持清楚，暂停/复位/卸载清理表现；撤碰撞由 `ShadowService` 决定，不能靠动画结束事件。

- [ ] `ART-015` 压力星板 — **部分已接入待验收｜P2｜ME-09**。`sprite_02.PNG` → `D/plate_up.png|plate_down.png`，同 544×128 画布、384 PPU；`PressurePlate.prefab/Visual`、`PlateSocket.prefab/PressurePlate/Visual` 及场景实例由 `PressurePlateArtView` 读取实际 `Value` 切图，显示宽度匹配传感器，保留上下表面形变。测试验证占槽压下、召回前 0.8 s 仍压下、1 s 完成恢复；尚缺占用亮纹/星石星影标识/连接点与正式 P2 关卡验收。

- [ ] `ART-016` 持续星桥 — **占位｜P2｜ME-11、FX-11**。当前 `Bridge.prefab` 是金色 `stone.png` 长条，`SignalBarrier.cs` 改透明度。补起/中/终段、渐显遮罩和撤除星尘；不同跨度保持可站立上沿，画面与实体开关时刻一致，撤影后不得留假桥。

- [ ] `ART-017` 普通机关门 — **占位｜P2｜ME-14**。当前 `Gate.prefab` 是石块竖条，已有信号/净空保护代码，未有完整门框与运动素材。补分离门框/门扇、开/关/受阻状态及连接纹；等待净空时可读，不作碾压演出。不是星座出口门。

- [ ] `ART-018` 可推星石 — **部分已接入待验收｜P2｜ME-10、FX-14**。`rock-dark.PNG` → `D/rock_dark.png`；`Stone.prefab/Visual` 及新建元件默认外观，Alpha 裁边/最大 1024、512 PPU，显示宽度 1 unit，动态重物根/Collider 不改。可推 Stone 自身未接点亮切换；`rock_light.png` 自 11-10 起另用于 ART-011 星槽占用，不新增星石点亮玩法。平顶/四侧星纹、轻尘与完整侧推/坑边恢复仍缺，当前图形与矩形碰撞的轮廓差异需正式 P2 复核。

- [ ] `ART-019` 深坑/失败边界 — **占位｜P1｜ENV-10**。当前 M2 `Hazard.prefab` 是红色石条，Core `PitHazard` 仅有 Trigger。用坑口结构与深处暗层/星雾提示危险，按实际进入失败的边界验收；不为触发体随意添加实体碰撞，也不改成尖刺/敌人设定。

### 3.4 轨迹、教学与菜单

- [ ] `ART-020` 轨迹线 — **占位｜P1｜UI-03**。当前 `Presentation/Trajectory` 保留 LineRenderer + `Trajectory.mat` 作为制作引用，2026-11-10 运行时隐藏旧线，`StarPathShapes` Mesh 提供星点/箭头/反弹标（§11），正式笔触素材仍缺；仍由 `TrajectoryPreview.cs` 和共享求解器提供几何；不能把轨迹烘焙成固定图片或产生玩法副作用。

- [ ] `ART-021` 轨迹终点/阻断标 — **占位｜P1｜UI-03**。当前 `Presentation/TrajectoryEnd` 保留 `ring.png` 制作引用，2026-11-10 运行隐藏，`TrajectoryPreview` 程序绘制范围/阻断/入槽/锁定形状（§11）；正式素材验收须核对预测落点、入槽终止与一次反弹，不单靠颜色；与蓄力月桂环分开接入。

- [ ] `ART-022` 世界教学剪影与地标 — **占位｜P1｜CH-12、TUT-01–04、TUT-09**。当前 M2 `LevelRoot/Tutorial` 仅静态半透明 `human.png` + 三个 `ring.png`，没有真实自动示范/重播。替换/扩充壁画、正确角度地贴、无底座演示幽灵和可重绑键帽；实际说明入槽→本体返回→踩影→召回，不将静态摆件算成完整教学。第二关补项见 NEW-007。

- [ ] `ART-023` 暂停菜单皮肤与字体 — **部分已接入待验收｜P1｜UI-06、UI-07、UI-10**。`IMG_6055|6054|6053|main-menu.PNG` → `D/pause_0–3.png`，按实际碎片数 0/1/2/3 显示。ShadowSlice 与 MechanismGallery 的 `Presentation/PuzzlePauseMenu/Panel/ArtworkFrame/Illustration`（Core 未接入） 叠加于 `BlurredGame`，`PauseArtwork` 截取冻结游戏画面并虚化，继续/复位/重开按钮维持原回调和独立文本；Canvas 1920×1080、排序 2000。插画图标仍为图内装饰；11-10 新试场在 `Panel/MenuControls` 左侧 35% 不透明面板覆盖原图标区，放置中文继续/复位/重开/回标题与音乐/音效控件，保留右侧进度柱，字体来源 `Assets/Game/Art/Fonts/manifest.json|LICENSE.txt`；2026-11-11 按用户要求，Chapter01_01/02 与 Gallery 的同名控件容器整体移到右侧 35%，左侧主插画和进度柱露出；现行锚点、制作默认和验证见 §15。独立四态按钮/正式布局签收仍缺，标题另见 NEW-010。

## 4. 尚无资源：新增交付 Checklist

以下 10 个父项均保持 `[ ]`：除 NEW-008 部分前景、NEW-010 标题单图外，成套交付仍缺；不伪造“当前占位文件”。它们与 §3 的共用素材相互关联，不重复计费；完整状态、数量、尺寸和批次以美术交付文档的 ID 为准。将来出现占位或正式文件时，在原 ID 下更新路径/接入点，不另开同义任务。

- [ ] `NEW-001` P0 设计与可导入样板 — **未制作｜P0｜AD-01–06、FX-01**。风格板、实机尺度色稿、角色/机关语言/分层比例及样板包；七张技术图不能抵扣整批验收。先以一屏实机验证风格、PPU、比例和灰度可读性，再生产 P1/P2。
- [ ] `NEW-002` 里拉琴及受击/反弹表现 — **部分已接入待验收｜P0→P1｜ME-04、FX-04 的里拉部分**。交框/有效弦/锚点、闲置/振弦/激活，弦面法线与实际反射一致。当前斜石是普通墙测试件，不能登记成里拉已实现。 **本轮更新：** 2026-11-10 改为**部分已接入待验收**：`IMG_6092.PNG` → `Delivery02/lyre.png`，673×1024/512 PPU/居中；`Lyre.prefab/Visual`、Gallery `Mechanisms/04_Instruments/Lyre`，`StarInstrument` / `InstrumentArtView`，有效弦面独立 Collider。共用单次反弹，点亮真实锁存接收器、开 `LyreGate`；失败/谜题复位保留，整关重开清除；工厂/运行 Catalog/文档导入导出已接入。振弦帧、独立琴弦光层、配对星纹教学和正式音频仍缺。
- [ ] `NEW-003` 手鼓、鼓波与脆陶墙 — **部分已接入待验收｜P2｜ME-05、ME-08、FX-09/10**。鼓面、半径匹配波纹、墙体完整/破碎残缘与装饰碎片；里拉/手鼓/碎墙局部机制已接入试区，完整组合关卡仍待实现。 **本轮更新：** 2026-11-10 改为**部分已接入待验收**：`IMG_6089.PNG|IMG_6090.PNG` → `Delivery02/drum_idle.png|drum_hit_00–09.png`，同 512×1024/512 PPU/居中，20 fps 单次受击；`Drum.prefab/Visual`、Gallery `04_Instruments/Drum|DrumWaveWall`，`StarInstrument` / `BrittleWall` / `InstrumentArtView`，实际鼓波半径 3 unit（调参值）。鼓动画与判定分离；波纹/陶墙技术占位另见 NEW-003-a，陶墙绘画/碎片/完整 FX 与正式鼓音仍缺。
- [ ] `NEW-004` 排箫控制器与升降平台 — **程序占位已接入、正式素材未制作｜P2｜ME-06/07、FX-12**。有效上/下管口、方向纹样、移动台/端点与流光；不能以普通平台/星桥占位算已交付。
- [ ] `NEW-005` 锁存开关与机关连接语言 — **程序占位已接入、正式素材未制作｜P2｜ME-12/16**。S1/S2 独立纹样、未亮/撞亮/锁存、配对接收端与连线；不把当前持续压板当作锁存开关。
- [ ] `NEW-006` 星座出口与两关完整星图 — **程序占位已接入、正式素材未制作｜P1/P2｜ME-15、UI-08、FX-13**。共用门框/门扇、各关三片节点/连线，第二关额外两开关徽记；开门条件和独立通关效果有真实程序接入后再验收。
- [ ] `NEW-007` 完整 HUD、操作图标与第二关教学 — **程序占位已接入、正式素材未制作｜P1/P2｜UI-01/04/05、TUT-05–09**。两关碎片栏、单影四态、动态键位字形、演示/重播/目标图标，以及推石/桥/鼓波/里拉 S1/排箫上下的图组；复用 ART-005/006/022，仍需逐关核对。
- [ ] `NEW-008` 环境装饰与关卡布局补齐 — **前景部分已交付，其余未制作｜P1/P2｜ENV-06–09、ENV-11；ENV-04 第二关布局**。已有前景草图由 ART-010 接入；辅助构件、马赛克/边饰、装饰陶瓶、残石仍缺，复用 ART-010 背景套件；装饰不能形成假平台或假交互提示。 **本轮更新：** 2026-11-10 增加格点草地与圆形笔刷用途，见 ART-008/009 / NEW-008-a；装饰环境交付范围未取消。
- [ ] `NEW-009` 尚缺的共用特效 — **部分程序占位已接入、正式素材未制作｜P1/P2｜FX-03/04/06/14**。发射尾迹、普通撞石粉、入槽展开、走路/落地/推石轻尘；与 §3 已列蓄力/收集/重组/召回及 NEW-002/003 的素材复用，按事件验收，无需为每个事件重复画全套纹理。
- [ ] `NEW-010` 标题、设置、转场与菜单补页 — **部分已接入待验收｜P1/P2｜UI-06–10**。实际源文件 `tittle-screen.PNG`（README 写作 title-screen）→ `D/title.png`，`ChapterArt.asset.Title` 已接入 `MainMenu/MainMenuCanvas/TitleArtwork`；`M2MenuAuthoring` 创作默认布局，`MainMenuController` 进入试场/操作声音页/退出，右侧独立 uGUI 为程序布局。音量设置已接，独立四态按钮/最终字版、转场、成功章纹仍缺，暂停插画改记 ART-023。来源/配方/修订沿用 §3；不能把备妥单图视为菜单套件完成。

## 5. 保留与排除项

| ID / 对象 | 处理规则 |
| --- | --- |
| KEEP-001：`Assets/Scenes/Northline.unity`、`EditorScratchBackup.unity`、`SampleScene.unity`；`Assets/Settings/Scenes/URP2DSceneTemplate.unity` | 原型/备份/模板单独保留。旧方块与格线由 `SolidShape2D` 使用 `Texture2D.whiteTexture` 运行时生成，不是漏掉的 PNG；不为它们下单正式首章美术，不运行破坏性 `PlatformerSceneBuilder.BuildScene()` |
| KEEP-002：`Assets/Platformer/MapRevealFog.mat` / `.shader` 与 `MapReveal2D.cs` | 旧地图揭示实验，遮罩运行时生成；第一章默认不用，保留待单独决定。两工作树材质不同，不随美术盘点互相覆盖 |
| KEEP-003：URP/Renderer2D、Volume、内置 Sprite/UI Shader、物理材质与包内资源 | 渲染/工程依赖，不因“当前是占位”全部重制或删除；正式素材需改变 Lit/Unlit 等分配时单独说明并验收。字体替换只跟踪实际运行菜单的需求 |
| KEEP-004：`RuntimeLevelEditor.cs` 的程序生成 uGUI、`ChapterLevelEditorWindow.uxml/.uss` 和编辑期 Gizmo/辅助线 | 创作工具视觉，当前为功能性控件。新资源需要更新元件预览/引用并保持工具可用；不把整套工具换肤升级为本期游戏美术的必交项。工具/调试标记不得误留在正式游戏画面。2026-11-10 Windows Demo 修复：`RuntimeLevelEditorCanvas` 排序从 40 改为 60，高于 GalleryNavigation 的 50，避免其覆盖保存/加载/试玩工具栏；仍使用原程序控件与内置 LegacyRuntime 字体，无新增图片或交付规格变更。验证记录见本文末尾。 |

占位淘汰以**正式场景和正式制作入口的引用**为准；旧灰盒、备份或测试仍引用 P0 可以保留。删除 P0 前必须证明所有保留场景、Prefab、Catalog、测试和路径加载均不再需要，不能只搜索当前打开的场景。

## 6. 后续更新和完成判定

### 状态与记录字段

`占位` / `未制作` → `制作中` → `已交付` → `已接入待验收` → `已验收`。有返修或回归失败时回退相应状态并撤销勾选；不再需要的条目保留 ID，改为“已取消”，写明用户决策日期和替代项，不能勾为已验收。

每次涉及资源、新占位、程序生成视觉、状态、Prefab/场景引用或制作入口变更，在同一任务更新本清单。保留 `ART/NEW/KEEP` ID，不按图片改名重编号；拆项加后缀并保留父项的范围说明。更新记录至少包含：

| 字段 | 必填内容 |
| --- | --- |
| 范围 | 条目 ID、对应 AD/CH/ENV/ME/TUT/UI/FX ID、批次、已完成状态与剩余状态 |
| 文件 | 当前占位/正式导出路径、可编辑源文件与 revision、manifest/来源/授权位置；尚缺写“未交付”，不用猜测路径 |
| 使用位置 | Prefab/场景及层级、实例覆盖、表现脚本、路径加载/制作工具、Runtime Catalog 等引用 |
| 接入 | PPU、画布/rect、pivot/锚点、9-slice/帧序等变更；保留或重绑的 GUID + 子 Sprite fileID；Windows 同步/导入状态 |
| 验收 | 日期、场景、提交或准确工作树范围、Editor/目标设备、分辨率、Game view/录像证据位置、实际触发的状态、Console 结论、缺口 |

### 替换操作与验收门槛

1. 先核对两工作树状态与目标文件差异；只动本任务资源，保留用户场景。正式图按用途接入，不把七张共用图全局覆盖；不运行 `generate_art.py --overwrite` 重写正式交付，不批量重建作者 Prefab。
2. 原资源返修保留 `.meta` GUID；新拆件保存新 `.meta` 并显式重绑。当前 PNG 都引用子 Sprite，不能只保留顶层 GUID 却任由切片 fileID 丢失。`human.png.meta` 顶层 pivot 不能当作当前裁切子图的有效脚底证明；正式动作逐帧验画布/切片、pivot、脚底和边缘。
3. 优先改独立 Visual/VFX，保留玩法根 scale、Collider、稳定 ID、出生点和显式引用。若新视觉层级需要调整 `PlayerView`、`ShadowFeedback`、`PressurePlate`、`SignalBarrier` 或编辑器的 `Find("Visual")` / `GetComponentInChildren<SpriteRenderer>()`，列为程序接入工作；不得只换 Prefab 后留下代码写错渲染器。
4. 检查共享 Prefab、场景直挂对象和实例覆盖，也检查 `CoreSceneAuthoring` / `LevelPieceFactory` / `M2SceneAuthoring` 的加载路径与新建默认值、Catalog 及运行时实例化。允许保留技术灰盒的 P0 路径，但须明确哪些入口生产正式资源，避免新放元件重新带回占位。
5. 分开记录“源文件已交付、Unity 已接入、Windows 已同步/导入、运行已验收”。单纯 PNG 存在、重命名、生成新图、编译成功或旧测试通过，均不能勾选。组合项所有列明状态/用途均通过，才可 `[x] 已验收`；阶段通过保留 `[ ]` 并写明范围。
6. 在 Windows Editor 中确认帧推进、查看 Game view 和 Console，并实际触发本项相关状态；检查 1080p、720p、16:10、灰度/关闭 bloom/静音可读性，以及平台/弦面/轨迹与碰撞的一致性。召回、暂停、复位等受影响行为要回归，保留证据并恢复测试设置。历史灰盒通过不等于新美术验收。
7. P0 冻结风格与比例后做 P1，再做 P2。仅美术返修更新本文与交付文档相关记录；若改变玩法或视觉语义，同步架构、教学、美术和相关音频要求，仍将原案与制作建议分开。

### 更新记录

| 日期 | 范围 | 本次变化与验证 |
| --- | --- | --- |
| 2026-11-08 | 初始盘点；ART-001–023、NEW-001–010、KEEP-001–004 | 已盘点七图/材质、Prefab、场景、脚本生成视觉和旧原型边界，建立 23 项占位替换 + 10 项缺失交付，正式验收 0 项。静态引用/切片检查及两工作树文件比对；未改资源、未同步、未做 Editor/Play Mode 验收 |
| 2026-11-08 | KEEP-001、KEEP-002；双副本同步 | 保留 Windows `Northline` 已保存的迷雾/脚步新字段及参数，与当前脚本匹配；不改场景 GUID、层级、碰撞或 M2 引用。同步前两端 `Assets/Game` 的 196 个文件（含 `.meta`）一致，七图及现有切片保持原样；材质/相关 `.meta` 差异仅空白，沿用 WSL 文本。未替换正式美术、增加交付或勾选验收项；本次不将原型参数同步视为美术验收 |
| 2026-11-08 | H-ONE-arts 外部候选；ART-001/005/006/007/015、NEW-010 | 先读来源 README，程序检查 4 PNG + 2 GIF 的完整性、透明度、区域和时序，登记候选来源；不向模型提供原图/派生图。未裁切、未修改源目录、未导入/同步/目视或运行验收，勾选与正式交付状态不变 |
| 2026-11-09 | ART-001/002/005/007/010/015/018/023、NEW-008/010；外部来源与制作默认值 | 按更新授权裁切/布局，新增 Delivery01 38 PNG、manifest/使用规范快照，接入六 Prefab、两灰盒与制作工厂。部分已接入待验收，标题/亮石仅备妥；缺失状态和正式 P0/P1/P2 验收不勾选。Windows 运行证据与测试结果见 09 试装记录 |

| 2026-11-09 | 工具跟踪；ART-001–023 的来源追溯 | 按用户要求忽略整个 tools，移除既有脚本的 Git 跟踪并保留本机文件；配方路径标为本地，派生图/manifest/使用规范及验收状态不变。静态检查 Git 索引与忽略规则，未因本次版本控制调整重跑 Editor |

工具跟踪规则（2026-11-09）：按用户要求，根目录 `tools/` 整体加入 `.gitignore`，已有生成脚本通过新提交移除 Git 跟踪、本机文件保留。本文提及的 `tools/art/prepare_delivery.py`、`configure_delivery.cs` 是本机整理配方，不再是仓库交付；新检出仍含派生图、Unity `.meta`、manifest 与使用规范快照。未改变图像、引用、资源验收状态，本次规则调整仅作静态核查。

## 7. 当前外部交付与试装（2026-11-09）

来源目录为工程同级 `../H-ONE-arts`。访问前先读最新 [README](../../H-ONE-arts/README.md)。当前规范允许只读目视查看、适当裁切/布局调整、复制进项目和重命名；仍禁止以原图为参考调用 AI 生图或作训练数据。源目录未写入，未使用生图工具。11-08 的“AI 不直接查看”限制与旧文件名仅作为历史记录，当前以新版规范及 `D/SOURCE_USAGE.md` 快照为准。

| 来源原图 | 当前派生/使用 | 范围与缺口 |
| --- | --- | --- |
| `sprite_03.PNG`、`walking.gif` | 待机 4 / 行走 4；PNG 为导出源，GIF 仅参考 | ART-001，缺其他人形动作 |
| `sprite_01.PNG`、`turn_star.gif` | PNG 10 帧变星，GIF 11 帧仅参考 | ART-001，重叠实际飞行 0.18 s，不等待 1.76 s 演示 |
| `sprite_02.PNG` | 星弹 4 / 碎片 4 / 压板 2 | ART-002/005/015；不再误记为星台或 Core 节点 |
| `spawn-point.PNG` | 未点亮星台单图 | ART-007，点亮状态缺 |
| `rock-dark.PNG`、`rock-light.PNG` | 暗色仍接 Stone；11-10 起两图另接两型星槽的空 / 占槽 | ART-011 / ART-018；仅槽随实际占用切换，Stone 无新增点亮规则 |
| `bg-level1.PNG`、`fg-level1-grass.PNG` | 背景与前景 | ART-010，非完整地形/神庙模块 |
| `IMG_6055.PNG`、`IMG_6054.PNG`、`IMG_6053.PNG`、`main-menu.PNG` | 暂停进度 0/1/2/3 片 | ART-023，叠加虚化游戏画面，不当标题页 |
| `tittle-screen.PNG` | 标题单图，11-10 接 MainMenu | NEW-010，独立按钮四态/最终美术仍缺 |

38 张派生 PNG 的完整路径、矩形、画布、pivot、PPU、源 SHA-256 与修订均在 [manifest](../Assets/Game/Art/Delivery01/manifest.json)。原始栅格交付已可用，独立分层工程/来源授权清单仍缺。制作入口 `CoreSceneAuthoring`、`LevelPieceFactory`、`M2SceneAuthoring` 会在存在 `ChapterArt.asset` 时应用 `DeliveredArtAuthoring`；Runtime Catalog 引用已保存的 Prefab，故新建/运行实例同步采用试装图。未提供的地墙、星影、教学、环等保留 P0，不全局替换七图。

本次两副本同步/导入、运行检查和限制集中见[美术试装记录](09-美术试装记录.md)，完整条目仍未勾选。后续返修应复查源 README、更新指纹，保留派生图 `.meta` 和玩法根/碰撞，不重新生成旧 P0 覆盖交付。

## 8. M2 收尾使用映射（2026-11-10）

本节补充现有 ID，不新增美术批次、不勾选完整父项。新场景 `Assets/Game/Scenes/M2_MechanismGallery.unity`（根 `MechanismGallery`）和 `MainMenu.unity` 由 Windows Editor 创作并逐文件同步；原 Core/ShadowSlice 不重建。

| 范围 / ID | 当前引用与剩余状态 | 来源、修订与证据 |
| --- | --- | --- |
| ART-001/002/003/004/005/007/010/011/012/015/016/017/018/019/020/021 | 新试场 `Actors/ChapterPlayer`、`Collectibles`、`Checkpoints`、`Background`、`Mechanisms` 下的 `01_StarFlight` / `02_ShadowBridge` / `03_StoneGate`、`Geometry`、`Presentation/Trajectory` 复用现有 Prefab/图；保留对应既有缺失状态，不因多一个使用点视为正式完成 | Delivery01 `delivery01-20261109` / manifest / SOURCE_USAGE 沿用 §3；P0 路径/子 Sprite GUID 与 fileID 保留；新场景见 10 收尾记录 |
| ART-013/014；ME-03、CH-10/FX-08，P1 | PlateSocket 顶条宽 0.9、高 0.18，位置 y=0.3，与原 Collider 对齐；LevelPieceFactory 默认和新试场覆写一致。ShadowFeedback 召回只缩核心，承重条不缩；正式月桂/星尘/回流线仍缺 | 当前仍 `P0/stone.png`、`P0/star.png`，无新增正式源；程序修改与 Prefab 定点调整，未改 PPU/pivot/根 scale/碰撞 |
| ART-023 / NEW-010；UI-06–10，P1/P2 | MainMenu 标题/中文按钮/操作声音页；试场暂停中文四按钮和双音量滑条；历史 ShadowSlice 保留原菜单。新菜单默认值由 M2MenuAuthoring 创作，新场景制作由 M2ReleaseAuthoring（new only）执行 | 图片仍 Delivery01 原 manifest/hash，未重新裁图或 AI 生成；字体 `Fonts/MenuSans.ttf` 为 Noto Sans SC 400 子集，`m2-menu-20261110`，manifest/README/OFL-1.1 同目录。最终按钮美术与过场仍缺 |
| ART-022 / NEW-007；TUT-01–09，P1/P2 | 新试场底部仅键位字形提示；未增加自动幽灵/壁画教学，也不把调试传送当教学。原 ShadowSlice 的静态 P0 幽灵保留 | 字形为同一字体；完整教学图源仍缺。陈列可绕过的碎片是有意的机制试验，不作为正式谜题必要性验收 |
| KEEP-001/004 | Northline/Sample/Backup 和 Core/ShadowSlice 保留在工程，退出发布清单；RuntimeLevelEditor 与 GalleryDebugNavigation 仅在 Editor/Development Build 显示。普通构建仍保留试场的技术地形，不能声称无占位素材 | 无删除原型；菜单/CI/Editor 使用同一两场景顺序，构建保护拒绝清单分歧 |

| 日期 | 更新范围 | 记录 |
| --- | --- | --- |
| 2026-11-10 | 上表所有使用点、作者默认值、字体与菜单 | 已接入待验收；Windows 同步、实测状态、分辨率、Console/构建结果以 [10 收尾记录](10-M2收尾与机关试场.md) 为准。完整 ART/NEW 父项维持未勾选；本次为 M2 程序/布局收尾，不授予正式音画签收 |

## 9. 星槽双状态替换（2026-11-10）

| 日期 | 范围 | 变化与验证 |
| --- | --- | --- |
| 2026-11-10 | ART-011；ME-02，P0→P1；ART-012/013/014 分层依赖 | 按当前源 README 复核原图指纹，复用 Delivery01 双图；Windows live Editor 保存两型槽 Prefab/两场景、导入新组件并同步 WSL。实际发射入槽、完整召回与暂停、压板/桥、失败保留、谜题复位/重开及三种分辨率检查通过；两型新建默认也已检查。五个原 Prefab 物理块、场景物理覆盖、稳定 ID/出生点未改，双副本任务文件一致。仍缺完整拆层/锁形/教学，父项不勾选；证据及两次诊断截图失败见 09 记录 |

## 10. 2026-11-10：剩余素材、地形格点与乐器试区

统一来源、PNG 路径/原稿哈希、裁切、画布、PPU、锚点和 atlas UV：`Assets/Game/Art/Delivery02/manifest.json` / `SOURCE_USAGE.md` / `README.md`，revision `delivery02-20261110`。原始栅格来自只读 `../H-ONE-arts`；分层绘画工程及独立授权清单仍未交，未 AI 生图。沿用上方 ART/NEW 及交付 ID，不新增平行规格。当前父项均未勾选；具体新层级、制作默认和同步/运行证据见 [11 接入记录](11-剩余美术与格点地形接入.md)。

- [ ] `NEW-008-a` 圆形草地笔刷 — **已接入待验收｜P1｜ENV-01/02（扩展原 NEW-008 的工具用途）**。用户说明 `IMG_6075.PNG` 的原设想为笔刷，本轮提出“按半径选格、方块落地”的方案；`Delivery02/terrain_brush.png` 为 1024²、1024 PPU、中心 pivot，现有 Chapter Level Editor `Round grass brush` 的预览图；`RemainingArtAuthoring.PaintGrassDisk` 生成整格 Solid 行区块、选左右端/中段，跳过已占地形且可 Undo。新写入元件复用 `Solid.prefab/GrassCells` 与 Runtime Catalog；圆图不产生独立圆形物理台。原 NEW-008 装饰/布局仍未齐。
- [ ] `NEW-003-a` 陶墙与鼓波当前技术占位 — **占位｜P2｜ME-08、FX-09/10（保留原 NEW-003 范围）**。新 `BrittleWall.prefab/Visual` 沿用 `P0/stone.png` 的 128 PPU Multiple-mode 原 Sprite/fileID、陶土临时染色；`Drum/DrumWave` 由 `InstrumentArtView` / `LineRenderer` 使用既有 `P0/Trajectory.mat` 生成，半径对应实际 3-unit 作用区。Gallery `04_Instruments/DrumWaveWall` 是实际使用点，工厂/运行 Catalog 已登记。美术原稿/分层源/正式 manifest 未交付；P0 原创来源沿用 `P0/README.md`，新增用途及程序修订记录于 11 文档。不得将其当作已交正式陶墙/波纹。

`IMG_6091.PNG`（灰白破损石柱）按用户 2026-11-10 决定暂缓；本轮无项目拷贝或引用，记录于 Delivery02 manifest.deferred。两 GIF 沿用备用动作参考，未叠加第二套角色动画。Delivery01 已接入资源保留，新静态地形的 Mesh 为受控运行生成物，不保存假 `.meta` 或手改 Sprite fileID。

| 更新日期 | 条目与范围 | 接入与验收记录 |
| --- | --- | --- |
| 2026-11-10 | ART-001/008/009、NEW-002/003/008-a，新增占位 NEW-003-a | 93 个任务文件已同步 Windows/WSL；三场景与四类 Prefab 已导入/保存。最终 EditMode 26/26、PlayMode 29/29；实际走跳、两乐器发射、碎墙及笔刷去重/Undo 通过，720p/1080p/16:10、静音/无后处理/灰度局部检查完成。最终 Console 0 错误，Play 与临时设置已恢复。证据、热重载导致的早期失败及剩余教学/状态/音频边界见 [11 接入记录](11-剩余美术与格点地形接入.md)；各父项仍未勾选，不授予整批正式美术验收 |

更新记录：本轮实现并导入跳跃、三方地块/土层和圆形编辑笔刷、里拉/手鼓试区，保留三个旧试区和两技术样例；正式两关、最终状态/音画包仍待验收。最终运行范围与限制以 11 记录为准。

### 2026-11-10 Windows Demo 工具层级修复

KEEP-004 的当前/最终实现路径均为 `Assets/Game/Runtime/Presentation/RuntimeLevelEditor.cs`，运行层级为 `MechanismGallery/RuntimeLevelEditorCanvas`；本轮唯一视觉改动是 Canvas 排序 40→60，盖在 `Presentation/GalleryNavigation`（50）上方。原程序生成控件、内置 LegacyRuntime 字体及既有素材引用不变，没有新增图片、可编辑美术源、manifest 或交付批次；这是保留工具修复，不提升任何 ART/NEW 父项验收状态。

两份工程已逐文件比较，397 个 `Assets/Game` 文件内容一致；本轮只同步上述脚本和 `M2GalleryGameplayTests.cs`，增加真实 UI 射线验证 Save 按钮优先收到点击。最终 EditMode 26/26、PlayMode 30/30，Editor 测试期间观测帧数 10769→后续测试完成；Windows x64 Development 独立构建 0 错误、1 条既有 App UI Settings 警告。Windows Unity 6000.0.84f1 / RTX 4060 Laptop、键鼠、1280×720 下实测主菜单、走动/发射/暂停、工具栏、新建/放置/连线、Save/Load、Play test/Stop test、退出后重启加载与推石压板开门。独立日志无异常；Editor 当前 Console 0 错误。Play 已退出，原 MainMenu 启动场景、auto tick=true、runInBackground=false 已恢复，没有修改项目渲染配置。

精确源码范围和哈希、两份 Git 状态、测试和 BuildReport 见本机忽略目录 `artifacts/H-ONE-Demo-Windows-x64-20261110-194118/`；实际独立程序截图包括 `final-03-editor-toolbar.png`、`final-05-gallery-saved.png`、`final-32-loaded-persisted.png`、`final-34-stone-on-plate.png`，打包说明见 [Windows Demo 使用说明](12-Windows-Demo与关卡编辑器使用说明.md)。本次只验工具可用性，未重新执行最终美术的多分辨率/灰度/静音整批验收。

## 11. 2026-11-10：无新增图源的碰撞与操作反馈

本轮沿用原 ID/批次，以下为技术反馈部分接入，ART/NEW 父项继续 `[ ]`。程序形状没有 PNG/PPU/pivot/子 Sprite fileID；所有既有 Delivery01/02/P0 `.meta`、画布、裁切、锚点和玩法根/Collider/稳定 ID 均保留。可编辑源是下表 C#，修订 `procedural-feedback-20261110`；逐文件 SHA-256 与来源记录见 [程序反馈 manifest](procedural-feedback-manifest.json)，运行/同步证据见 [13 记录](13-程序反馈改进与碰撞质感.md)。代码为项目内新增程序，不含外部图片/字体/音频；既有图源授权快照仍在 Delivery01/02 `SOURCE_USAGE.md`，P0 来源在原 README。正式美术源工程/完整许可交付仍缺，技术代码不能补齐它们。

| 既有条目 / 交付与批次 | 当前路径、实际使用与制作默认 | 剩余正式状态 |
| --- | --- | --- |
| ART-002/003/004/005/007；CH-07/09、UI-02、ME-01/13、FX-02/05/07，P1 | `Runtime/Presentation/PlayerView.cs` 只变形玩家 `Visual/Star`；`ImpactFeedbackView.cs` 运行生成短尾迹/星闪/消散/聚合/收片/检查点；`GameplayFeedbackView.cs` 独立蓄力/满蓄形状。`GameFeelInstaller.cs` 从有效 `PlayerStateMachine.Start` 安装 `Actors/ChapterPlayer/ProceduralFeedback/{ImpactGeometry,ChargeAndShadowShapes,GameplayFeedbackHUD}`，覆盖 Core/ShadowSlice/Gallery 和制作/Runtime Catalog 玩家。星弹/碎片/星台仍原图，不拉伸角色根 | 月桂拆层、绘本纹理、独特碎片/星图与完整动作；ART-007 的持续当前星台状态仍缺 |
| NEW-002/003/003-a/009；ME-04/05/08、FX-03/04/06/09/10，P1/P2 | `InstrumentArtView.cs` 在里拉 `Visual` 做约 0.36 s 轻振，保留 Delivery02 鼓帧与 3-unit 波纹；`ImpactFeedbackView` 用真命中法线/破墙完成事件画石粉、星闪和 0.6 s 陶片。实际使用为 Gallery `04_Instruments/Lyre|Drum|DrumWaveWall` 及新建相同 Prefab/运行元件；24 个有界事件、96 路径点，0.14 s 短尾迹 | 琴弦独立层、完整陶墙/残缘及最终 FX；走路/落地/推石轻尘未在本轮实现，NEW-009 原范围保留 |
| ART-011/012/013/014；ME-02/03、CH-08/10、FX-08，P1/P2 | 两型槽暗/亮原画、既有星影核心/顶条不改；`GameplayFeedbackView` 用开口/菱形/锁形/召回弧与星点回流补四态，并按真实 Collider 顶边标记承重面。用于 ShadowSlice `Puzzle_A`、Gallery `02_ShadowBridge` 及动态元件；`ShadowFeedback` 原音频/核心缩放保持 | 正式月桂影、召回纹理/拆层及无说明教学仍缺；程序不提前撤除支撑 |
| ART-020/021；UI-03，P1 | `Runtime/Star/TrajectoryPreview.cs` 在既有 `Presentation/Trajectory/StarPathShapes` 运行生成星点/箭头/反弹菱形、范围开环/实体阻断/入槽/锁定标；既有 LineRenderer 与 `TrajectoryEnd` 仅运行时隐藏，制作引用仍保留。复用 `P0/Trajectory.mat` / `Sprites/Default`，同一求解器 | 正式绘本笔触/层次与完整两关预测/教学验收；不删除保留的灰盒资源 |
| NEW-007；UI-01/04/05，P1/P2 | `GameplayFeedbackView.cs` + `ProceduralHudGraphic.cs` 图形碎片栏按当前 `AllowedFragmentIds` 显示，单影四态/召回进度、动态 Input Action E/R 字形；1920×1080 参考 uGUI、排序 25，低于开发工具/暂停 Canvas，所有新增图形不抢射线 | 两关独特图标、出口映射、完整壁画/幽灵教学仍缺；ART-006 世界旧节点没有被冒认成 HUD |
| ART-023/NEW-010；UI-06–10，P1/P2 | `FeedbackMenuStyle.cs` 给 MainMenu/PuzzlePauseMenu 既有按钮加暗蓝底、金边与悬停/按下色；`MainMenuController` / `PuzzlePauseMenu` 运行安装，不改层级布局/回调/字体/原插画。`ChapterCamera2D.cs` 平滑瞄准构图、把可调短冲击与跟随位置分开 | 独立按钮原画、过场与完整菜单套件仍未交付；原字体 manifest/OFL 保持，未迁移 UI 框架 |

`CoreSceneAuthoring`、`LevelPieceFactory`、`M2SceneAuthoring` 与 Runtime Catalog 的既有引用未重写；新反馈依附于共用玩家/轨迹/乐器组件自动生效。没有全局替换 `stone/ring/dais/empty_node`，没有重建美术 Prefab/生产场景，也没有修改渲染配置或外部源目录。

| 更新日期 | 范围 | 同步与运行状态 |
| --- | --- | --- |
| 2026-11-10 | 上表技术层；声音另见音频交付清单 | Windows 已导入并完成本轮局部程序反馈验证：EditMode 29/29、PlayMode 35/35，画面返修后 CoreGameplay 8/8；Game view 四弹/召回暂停/三分辨率/静音/无后处理与灰度检查，最终 Console 0 errors。帧数、证据、恢复设置及听感限制见 13 记录；正式美术批次未签收 |


## 12. 2026-11-10：首章两关第一版与逐步开放教学

修订 `chapter-first-pass-20261110`。本节是原 ART/NEW ID 的新增使用与状态记录，父项全部保持 `[ ]`。新场景 `Assets/Game/Scenes/Chapter01_01.unity`、`Chapter01_02.unity` 的固定层级仍为 `LevelRoot/{Geometry,Background,Mechanisms,Collectibles,Checkpoints,Tutorial,Actors,Presentation}`。场景与新建配置的 .meta 由 Windows Editor 生成并保留；没有修改既有纹理 GUID、子 Sprite fileID、裁切、PPU、pivot、角色画布或共享 Prefab 的物理根。独立新场景的碰撞/ID/出生点按本版布局创作，没有覆盖既有用户场景。

| 原条目 / 交付批次 | 本轮实际使用与程序 / 作者默认 | 当前源 / 最终源与剩余状态 |
| --- | --- | --- |
| ART-001–005/007–021；CH/ENV/ME/UI/FX 原批次 | 两关 Actors/ChapterPlayer、Collectibles/l1-f1–f3 或 l2-f1–f3、Checkpoints 各区星台、Geometry/草地块、Background、Mechanisms/星石/压板/门/桥/槽与 Presentation/Trajectory 复用对应 Prefab 和现有反馈。NEW-002/003/003-a 的 Lyre/Drum/BrittleWall 新增到第二关 C/D，第一关 B 用 Lyre | 当前仍 Delivery01/02、P0 同路径；源修订/PNG SHA/导入尺寸与来源许可快照沿用各自 manifest / SOURCE_USAGE、P0 README，不产生新原稿。正式角色/星台/星影/地形/陶墙/乐器缺失状态继续见 §3、§10/11，新增两关不等于整批验收 |
| ART-022 / NEW-007；CH-12、TUT-01–08、UI-01/04/05，P1/P2 | `Tutorial/ChapterTutorial` 运行生成 `MosaicAndDemonstration`：实际路径地贴、空心人形/飞星幽灵；占槽/升台后换后续动作。`Presentation/ProgressiveGlyphs/CurrentAction` 每次只给当前动作字形；`Actors/ChapterPlayer/ProceduralFeedback/GameplayFeedbackHUD` 片栏/蓄力/单影/召回逐步开放。首次片、进入对应区、实际踩影/过桥为可见条件，重开重教、谜题复位保留 | 可编辑源 `Runtime/Presentation/ChapterTutorial.cs` / `GameplayFeedbackView.cs` 与 `Editor/ChapterFirstPassAuthoring.cs`，无新增 PNG/字体/PPU。动态键名沿用 Fonts/MenuSans 既有 manifest/OFL。最终黑绘壁画、手绘键帽/鼠标、独立演示重播图标和新玩家可读性签收仍缺 |
| NEW-004；ME-06/07、FX-12，P2 | 第二关 `Mechanisms/E_Flute/UpperPipe|LowerPipe|FluteLift`；`ChapterMechanismView/SwitchAndFluteSymbols` 程序石管/上下箭纹，平台用原 `P0/stone.png` 单 Sprite（约 3×0.3 unit）承重条。`FlutePort.cs`、`PanFluteLift.cs` 持有控制/碰撞，Visual 不控制运动 | 可编辑源为上述 C#；程序形状无图源，stone 沿用 P0 源/子 Sprite；精确场景与代码 SHA 在本轮 manifest。最终石管、独立流光/移动端点、平台美术及到站/受阻反馈未交，不冒用暂缓的灰白石柱 |
| NEW-005；ME-12/16，P2 | 第二关 `Mechanisms/D_Lyre/l2-s1`、`E_Flute/l2-s2`；`StarSwitch` 实际扫掠触发，`ChapterMechanismView` 画空/实心不同轮廓；普通门显式读 ProgressCondition，持续压板不锁存 | 当前为项目程序几何；最终两开关独立纹样、接收端/连线原画未交。源与 revision 同本节 manifest，不以颜色独立承担语义 |
| NEW-006 / ART-006；ME-15、UI-01/08、FX-13，P1/P2 | 两关 `Mechanisms/ConstellationDoor/ProgressNodes/ConstellationNodes`：三片节点/连线；第二关再加两开关节点，`ChapterGoalView.cs` 读取必需 ID。`ChapterExit` 只在解锁后人形真正过门切关。Core 的历史 ART-006 小节点保留 | 当前门扇复用 P0/stone，图形由代码生成；最终星座陶片、完整门框/开关徽记/开门和通关演出未交。不能把通用节点当两关独特最终星图 |
| ART-023 / NEW-010；UI-06–10，P1/P2 | 两关复用原暂停插画/音量；MainMenu 开始第一章；第二关 `Presentation/ChapterCompletion` 给重玩/回标题菜单。`ChapterCameraLayout` / `ChapterCamera2D` 按区取景，塔区包含高目标 | 原 Delivery01 标题/暂停/背景及 Fonts 同源同哈希；新菜单无新图。最终按钮、章节徽章、过场与完整音画未签收 |
| KEEP-004 | 两张首版场景暂不挂 RuntimeLevelEditor；新专属机关尚无完整 JSON 契约。原工具、Gallery 与已有 Factory/Catalog 不删除/改图 | `ChapterFirstPassAuthoring` new-only 入口复用现有 Prefab。CoreSceneAuthoring/LevelPieceFactory/M2SceneAuthoring 继续保持原 greybox/已接入默认；未以新场景创建为由全局换七图或生成 P0 |

程序几何/教学布局为项目内新编代码，未用外部图片 AI 生成。当前外部源 README 只读政策已复核，哈希、原 Delivery manifest 指向、源脚本和新场景 revision 见 [首章 manifest](chapter-first-pass-manifest.json)；正式分层原稿/完整独立授权清单仍缺。每关镜头/布局、碰撞与提示开放需在 Game view 验证；测试数量不能替代正式美术验收。

| 日期 | 范围 | 验证边界 |
| --- | --- | --- |
| 2026-11-10/11 | 上表全部新增使用 | 两关已创作、导入、同步；EditMode 29/29、PlayMode 39/39，真实输入路线、实际 720p/1080p/16:10 与静音/灰度局部画面已检查；完成/暂停叠层返修通过。普通 Windows 构建 0 error / 186 条既有包警告，当前 Console 0 错误，Play/临时设置恢复。精确 dirty-file 哈希、设备/截图/局部布置与独立程序未通关等边界见 [15 记录](15-首章关卡第一版.md) 和本轮 manifest。正式交付父项不勾选，新玩家教学与最终音画签收仍待 |


## 13. 2026-11-11：主页制作名单与字形补全

- [ ] `NEW-010-a` Credits 补页 — **已接入待验收｜P1/P2｜UI-06/09/10**。保留 NEW-010 标题/设置/转场父项与 ART-023 暂停范围，本项只记录新增署名页。当前/最终程序路径为 `Runtime/Presentation/MainMenuCredits.cs`、`Editor/MainMenuPresentationAuthoring.cs`，使用点 `MainMenu/MainMenuCanvas/{Credits,CreditsPanel/{BlurredMenu,Shade,List}}`；作者默认由 `M2MenuAuthoring.CreateMainMenu` 调用增量 `Apply`，不重建原 Home/Settings。Canvas 仍 1920×1080 / match 0.5；入口右下角锚点/枢轴 (1,0)，152×48、边距 (32,28)，列表中央锚点 (0.16,0.04)–(0.84,0.96)。程序按钮沿用 `FeedbackMenuStyle`；返回/既有 UI Cancel Action 关闭，模态期间阻止后层按钮。

背景为运行时四分之一分辨率 RenderTexture，复用 `Delivery01/PauseBlur.mat/.shader`，无需新 PNG、PPU、pivot 或 Sprite fileID。原 `Delivery01/title.png`、暂停图、所有 Prefab/关卡物理根/Collider/ID/出生点不变；`CoreSceneAuthoring`、`LevelPieceFactory`、`M2SceneAuthoring` 与 Runtime Catalog 没有新增占位依赖。保留图源的 SHA/配方/许可快照仍见 Delivery01/02 `manifest.json` / `SOURCE_USAGE.md`；本轮仅补署名“不眠巷”。分层源/最终按钮图仍缺，不将完整菜单套件标记完成。

字形资源仍为 `Assets/Game/Art/Fonts/MenuSans.ttf`，初次接入保留原 .meta GUID 与 178 个既存 codepoint 并扩至 219；本次署名更正后为 223 个（保留此前 219 个），当前修订 `credits-attribution-20261111`，源 Noto Sans SC regular 400、OFL-1.1。源/产物 SHA 见同目录 `manifest.json`，OFL 与来源见 `LICENSE.txt` / `README.md`；本轮只扩充字体子集，没有图片生成。音乐在独立音频清单与 Music manifest 登记。

| 日期 | 范围 | 同步与验证 |
| --- | --- | --- |
| 2026-11-11 | NEW-010-a / NEW-010、ART-023 共享字体依赖；UI-06/09/10，P1/P2 | 增量保存 MainMenu，35 个任务文件双副本内容一致；三档 Game view、真实 UI 射线、后层遮挡/返回/Esc 与字形检查通过，Console 0 errors、Windows 构建成功。EditMode 29/29，PlayMode 41/42 加既有输入失败项单独复测 1/1；精确边界/证据见 [17 记录](17-主页音乐与制作名单.md)。未签收正式美术批次，父项维持未勾选。 |

## 14. 2026-11-11：主页透明按钮与场景淡黑

- [ ] `NEW-010-b` 主页留白布局与转场程序层 — **已接入待验收｜P1/P2｜UI-06/09/10**。保留 NEW-010 全套菜单及 NEW-010-a 署名页范围。当前/最终路径为 `Assets/Game/Scenes/MainMenu.unity`、`Runtime/Presentation/{MainMenuController,FeedbackMenuStyle,SceneTransition,GameFlow}.cs` 和 `Editor/MainMenuPresentationAuthoring.cs`；可编辑程序修订 `menu-transition-20261111`，逐文件 SHA/来源依赖见 [manifest](menu-transition-manifest.json)。无新增外部图稿，程序代码归项目；标题/字体的来源、许可及指纹继续见 `Art/Delivery01/{manifest.json,SOURCE_USAGE.md}`、`Art/Fonts/{manifest.json,LICENSE.txt}`。正式分层源/完整菜单套件仍缺，父项不勾选。

使用点 `MainMenu/MainMenuCanvas/Home/{Start,Settings,Quit}` 去掉矩形底及 Home 面板，文字按钮移至插画右侧；Home 锚点由 (0.65,0.08)–(0.96,0.92) 调至 (0.79,0.08)–(0.99,0.92)，标题/副标/底注字号 40/22/20，按钮仍 30。Credits 入口同为透明底，设置页与署名页模态沿用原布局。原场景对象/fileID、图片/字体 GUID、Sprite rect/PPU/pivot、1920×1080 Canvas/match 0.5 均保留；无新切片或 PNG。

运行使用 `DontDestroyOnLoad/SceneTransition/Black`：临时排序 32767 的纯黑 uGUI，开始按钮三次闪烁（合计目标 0.48 秒）后 0.65 秒压黑；全黑异步加载，至少保留一帧黑幕，0.65 秒渐隐显露场景。过程锁重复提交、阻挡 UI/玩法输入，使用非缩放时间，不改变机关/收片状态；结束释放遮罩和输入。`MenuMusic` 同步淡出只改自身增益，音乐/音效设置不改。`GameFlow` 的开始/试场/返回、`ChapterCompletionView` 的重玩与 `ChapterExit` 的下一关复用转场，额外闪烁仅主页开始。

新建菜单默认通过 `M2MenuAuthoring.CreateMainMenu → MainMenuPresentationAuthoring.Apply → ApplyHomeLayout` 保持一致；本次现有主页只调用布局修正并明确保存，无重建。`CoreSceneAuthoring`、`LevelPieceFactory`、`M2SceneAuthoring`、Runtime Catalog/实例元件无新增图源或占位依赖；无关卡/Prefab 物理根、Collider、稳定 ID 或出生点改动。转场仅负责场景流程，世界教学保持原渐进提示。

| 日期 | 范围 | 同步与验证 |
| --- | --- | --- |
| 2026-11-11 | NEW-010-b / NEW-010-a 入口、UI-06/09/10，P1/P2 | 已同步 Windows，live Editor 增量保存 MainMenu 并回拷 WSL；具体测试、分辨率、帧推进、截图/运行证据、临时设置恢复及限制见 [17 记录](17-主页音乐与制作名单.md) 末节。此项是程序布局/转场验证，不签收完整美术批次。 |

2026-11-11 同日追加（修订 `menu-title-20261111`）：NEW-010-b / UI-06 标题改为“希腊星”，`Home/Subtitle` 与 `Home/Scope` 清空文字并停用，保留既有对象 ID；新建菜单不再创建这两段文字。可见文字仅为 Title 和 Start/Settings/Quit/Credits 的按钮字样。当前/最终程序仍为上表路径，新增制作默认引用 `M2MenuAuthoring.cs`、`ChapterFirstPassAuthoring.cs`；`GameFlow.GameTitle` 供产品/CI/制作入口共用。标题/按钮字号、锚点、原图与字体的 manifest/许可/GUID/PPU/pivot 不改，现有 MenuSans 已含三个字形，无新图稿或字体交付。最新任务 SHA 与两份产品设置的保留差异补记于同一 manifest；Windows 已导入，三档实际 Game view 和全部可见文字/字形检查通过，帧 7243→20323，运行 Console 0 errors。证据、恢复设置与本轮未重跑自动测试/未重建包的边界见 17 记录；父项维持未勾选。


## 15. 2026-11-11：暂停控件移至右侧

ART-023 / UI-06、UI-07、UI-10（P1/P2）维持“部分已接入待验收”。本轮程序布局修订 `pause-right-20261111`：`Chapter01_01.unity`、`Chapter01_02.unity` 与 `M2_MechanismGallery.unity` 的 `Presentation/PuzzlePauseMenu/Panel/MenuControls` 锚点从 (0,0)–(0.35,1) 改为 (0.65,0)–(1,1)，偏移仍为零；四个按钮、两个音量滑条、标题与提示整体随父容器移动，留出左侧主插画和三根进度柱。`M2MenuAuthoring.UpgradePause` 同步新建默认；ChapterFirstPassAuthoring / M2ReleaseAuthoring 复用此入口。保留的 ShadowSlice 原菜单本来位于右侧，Core 无该 Canvas；CoreSceneAuthoring、LevelPieceFactory、M2SceneAuthoring 与 Runtime Catalog 没有新增素材依赖。

当前/最终图片仍为 `Assets/Game/Art/Delivery01/pause_0–3.png`，程序字体仍为 `Art/Fonts/MenuSans.ttf`；原始栅格源、修订、SHA、裁切、PPU、pivot、子 Sprite fileID、来源规范/授权继续见 Delivery01 的 `manifest.json|SOURCE_USAGE.md|README.md` 与 Fonts 的 `manifest.json|LICENSE.txt`。本轮只读复核源 README，未改图片/导入设置或制作新素材；分层源和完整按钮四态交付仍缺。精确代码/场景/文档范围与依赖哈希见 [本轮 manifest](pause-menu-manifest.json)。三个场景由 Windows live Editor 增量保存后回写 WSL；原对象 ID 和引用保留，Unity 保存时补写了既有组件的空引用/默认字段及空白格式，没有重建关卡。

2026-11-11：Windows Unity 6000.0.84f1 / RTX 4060 Laptop，Chapter01_01 Game view 在 1280×720、1920×1080、1280×800 下实际暂停，检查右侧布局、左侧完整插画/进度柱、文字边界和继续按钮；帧 607→5545，四按钮与两滑条的 UI 射线命中通过，实际 PointerClick 继续回调通过。证据 `Screenshots/PauseRight20261111/editor-{1280x720,1920x1080,1280x800}.png`（Windows 本机产物，不提交）；其余两场景核对相同锚点和对象 ID。此次仅调整布局，未重跑无关玩法测试、未作整关通关或正式美术批次签收。最终构建、同步和独立包检查见 manifest。


本轮诊断与交付补记：初次 Editor 测试会话恢复控制时出现 PlayerStateMachine / TrajectoryPreview 初始化异常和 Input System 状态断言，诊断保留在同目录 `editor-errors.json`；不把该会话算作玩法回归通过。另行全新启动 Windows x64 普通包，以真实键鼠完成主页进入、移动、Esc 暂停、右侧按钮继续和正常关闭，`player-{menu,game,moved,paused,resumed}.png` 与 `player.log` 留证，独立日志未发现异常。新构建 `GreekStar-Windows-x64-20261111-r2` 为 0 errors / 186 条既有包警告；ZIP 完整性校验通过。原包保留，r2 包含本次右移。Play 已退出、MainMenu 保持干净，启动场景/Free Aspect/自定义尺寸和构建临时 preloadedAssets 均恢复；诊断导出后清理测试 Console，最终状态另见 manifest。


## 16. 2026-11-11：Credits 署名更正

NEW-010-a / UI-06/09/10（P1/P2）维持“已接入待验收”。按用户指定，美术署名为“不眠巷”，音效为“阿锰”；当前/最终使用点 `MainMenu/MainMenuCanvas/CreditsPanel/List/{Art,Sound}`，制作源 `Assets/Game/Editor/MainMenuPresentationAuthoring.cs`，现有场景 `Assets/Game/Scenes/MainMenu.unity`。根 CREDITS/README、Delivery01/02 美术与 Delivery01 音效的 README/manifest、相关清单同步署名。

共享字体 `Assets/Game/Art/Fonts/MenuSans.ttf` 修订 `credits-attribution-20261111`：保留此前 219 个 codepoint，仅新增“眠、巷、阿、锰”（223 个），源 Noto Sans SC regular 400 / OFL-1.1，源/产物 SHA、工具版本见同目录 `manifest.json`，许可/来源见 `LICENSE.txt` / `README.md`。原字体 .meta GUID 保留；本轮不涉及 Sprite rect/PPU/pivot/9-slice、图片或图源政策变更。代码/运行时使用点只改变两段署名文字，CoreSceneAuthoring、LevelPieceFactory、M2SceneAuthoring、Runtime Catalog 与 Prefab/关卡物理引用不新增素材依赖；分层源与完整菜单套件继续待交，父项不勾选。

更新记录：代码、字体和文档已更新；Windows 导入、场景保存/回写及本轮精确验证范围见 [17 记录](17-主页音乐与制作名单.md) 的署名更正记录。历史三档截图和整批签收不视为本轮字形的验收。本次 1423×656 实际截图、灰度副本、字形与返回检查通过；精确范围和未完成检查见 [本轮 manifest](credits-attribution-manifest.json)。

2026-11-11 r3 交付补记：NEW-010-a / NEW-010-b / ART-023 已随当前菜单与署名构建 Windows x64 包，0 errors / 186 包警告，快速 ZIP/CRC 通过；独立 1280×720 启动/退出检查通过，Credits 仍以上述 Editor 实际画面验证为界。未重跑整关或正式美术验收，父项状态不变。包指纹与限制见 [17 记录](17-主页音乐与制作名单.md) r3 节及 [署名 manifest](credits-attribution-manifest.json)。
