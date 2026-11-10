# CI/CD 与 GitHub 配置

本项目使用 GitHub Actions + GameCI，Unity 版本从 `ProjectSettings/ProjectVersion.txt` 自动读取，当前为 **6000.0.84f1**。配置没有升级 Unity、修改玩法，也没有生成或覆盖关卡。

## 已配置的流程

入口：[`.github/workflows/unity-ci.yml`](../.github/workflows/unity-ci.yml)。

| 触发方式 | 自动测试 | 构建 | Pages 部署 |
| --- | --- | --- | --- |
| 向 `main` 提交 PR | EditMode + PlayMode | 不构建 | 不部署 |
| push 到 `main` | EditMode + PlayMode | Windows x64、Linux x64、macOS、WebGL | 所有测试和四个平台构建成功后部署 |
| Actions → Unity CI → Run workflow | EditMode + PlayMode | 同上 | 仅选择 `main` 且勾选 `deploy_pages` 时部署 |

执行顺序：工作流 lint → 检查许可证 Secrets → 两类 Unity 测试 → 四个平台构建 → `CI gate` → Pages。任一前置阶段失败，后续部署停止，现有网站保留上次成功版本。`CI gate` 汇总前置阶段的真实状态，适合作为分支保护的必需检查；它不包含部署结果，部署结果见独立的 Pages job。

- 测试使用 GameCI 默认 Linux 镜像，EditMode 与 PlayMode 均以 batchmode 顺序执行。2026-11-10 按用户决定，远程 CI 临时通过 `customParameters` 的 `-testFilter` 排除且仅排除 `HOne.Game.Tests.DeliveredArtTests.PauseShowsActualProgressFreezesAnimationAndReleasesCapture`；完整测试名使用锚定正则，其他用例继续执行，失败继续阻止构建与部署。
- 暂停截图测试及原断言仍保留在项目中，本地 GUI Editor 运行时不受 CI 参数影响。被过滤的用例不算通过；当前基线预计为 EditMode 26 个、PlayMode 21 个执行。临时非 batchmode 镜像、启动器及原生调用栈诊断已移除，相关尝试可从 Git 历史查阅。恢复此测试前须先解决下方“待解决问题”中的远程图形 Editor 启动故障。
- 通过 `GAME_CI_COVERAGE_ENABLED: 'false'` 关闭未安装 Code Coverage 包的覆盖率采集。当前固定的 Action 会将 `coverageEnabled: false` 转成 CLI 不接受的 `--no-coverageEnabled`，因此使用 CLI 支持的环境选项；不要改回该 Action input。
- 桌面版本在 Ubuntu runner 的 Unity 容器中使用 Mono 交叉构建；WebGL 使用 Unity 的 WebGL 工具链。构建成功不等于各平台实机运行已验收。
- 构建矩阵默认一次运行一个任务，减少单个 workflow 的许可证并发占用。其他分支的手动运行和其他 PR 仍可能同时占用许可证；按实际授权席位安排运行。
- `Library` 按测试/目标平台分别缓存，包含包和项目设置的指纹；源文件变化继续由 Unity 增量导入。
- 自动 checkout Git LFS 资源。不要把只有 LFS pointer 的素材当作实际图片/音频使用。
- 官方 Actions、GameCI Actions 固定到完整 commit SHA；测试用 GameCI CLI 固定为 `v0.1.72`，actionlint 固定为 `v1.7.12`。Dependabot 每月检查 Actions 更新；CLI、lint 工具版本需手动维护。

## GitHub 首次设置

### 1. 提交配置和需要构建的项目文件

将本次 CI 文件及其 Unity `.meta` 一起提交、推送到仓库，最终合入 `main`。仓库当前为 [`OriSnow420/H-ONE`](https://github.com/OriSnow420/H-ONE)。只有 Git 中的文件会进入 runner；WSL 和 Windows 上未提交的改动不会进入云端构建。

本次配置文件包括：

- `.github/workflows/unity-ci.yml`
- `.github/dependabot.yml`
- `.github/unity-build-scenes.txt`
- `Assets/Game/Editor/CiBuild.cs` 及 `.meta`
- `.gitignore` 中新增的 CI 输出和许可证忽略规则
- 本说明及 `docs/README.md` 的索引

不要提交本机 `AGENTS.md`、Unity 许可证、`Library`、`Temp` 或测试/构建输出。工作区已有其他玩法、场景、文档改动；提交时只选择本次任务文件，已有修改的 `.gitignore` / `docs/README.md` 按实际 diff 选择对应变更。

### 2. 启用 Actions

进入 [Settings → Actions → General](https://github.com/OriSnow420/H-ONE/settings/actions)：

1. 允许 GitHub Actions；若启用了 allowlist，至少允许 `actions/*`、`game-ci/unity-builder`、`game-ci/unity-test-runner`，以及当前固定的 SHA。
2. `Workflow permissions` 可保持 **Read repository contents and packages permissions**。workflow 会分别声明测试所需的 `checks: write` 和发布所需的 `pages: write` / `id-token: write`，无须全局开放写权限。
3. 无须勾选 “Allow GitHub Actions to create and approve pull requests”，也无须创建 PAT。`GITHUB_TOKEN` 由 GitHub 自动提供。

### 3. 添加 Unity 许可证 Secrets

进入 [Settings → Secrets and variables → Actions](https://github.com/OriSnow420/H-ONE/settings/secrets/actions)，在 **Repository secrets** 中按使用的授权方式添加下表。不要把敏感内容放入普通 Variables、YAML、日志或提交中。

| Secret | Personal | 有序列号的 Pro 授权 | 浮动许可服务器 |
| --- | --- | --- | --- |
| `UNITY_EMAIL` | Unity 账号邮箱 | Unity 账号邮箱 | 不需要 |
| `UNITY_PASSWORD` | Unity 账号密码 | Unity 账号密码 | 不需要 |
| `UNITY_LICENSE` | `.ulf` 文件的完整原始内容 | 留空 | 留空 |
| `UNITY_SERIAL` | 留空 | 有效授权序列号 | 留空 |
| `UNITY_LICENSE_SERVER` | 留空 | 留空 | runner 可访问的许可服务器地址 |

只配置实际使用的一种方式。预检查只确认值已填写，Unity 激活步骤才会检查账号和许可证是否有效。许可证放在 Repository secrets，测试和构建才能读取；只放到 `github-pages` Environment 的 Secrets 中无法供这些前置 job 使用。

Personal 的获取方式以 [GameCI 当前激活说明](https://game.ci/docs/github/activation/)为准：在 Unity Hub 登录对应账号，进入 Preferences → Licenses → Add → Get a free personal license，然后找到生成的 `.ulf` 文件。GameCI 文档列出的 Windows 常用位置为 `C:\ProgramData\Unity\Unity_lic.ulf`；将文件**完整内容**粘贴到 `UNITY_LICENSE`，不是文件路径，也不需要 Base64。

Unity 6 / Hub 的授权界面可能不同；如果没有生成可用的 `.ulf`，按上述 GameCI 文档的当前兼容说明处理，或使用已有的 Pro / 浮动许可授权。旧的手动上传 `.alf` 激活教程不能保证适用于当前 Personal 账号。不要为了 CI 降级本项目 Unity 版本，也不要把许可证提交到公开仓库。

GameCI 负责在每次测试/构建前激活、结束后归还相应许可证。若授权带有账号验证、席位或服务端限制，仍须按 Unity 账号的实际情况处理；取消任务导致归还失败时，检查 Unity 授权管理再重试。

### 4. 开启 GitHub Pages

进入 [Settings → Pages](https://github.com/OriSnow420/H-ONE/settings/pages)，在 **Build and deployment → Source** 选择 **GitHub Actions**。

workflow 使用官方 Pages artifact + OIDC 部署，不需要 `gh-pages` 分支、部署 SSH key 或自建 PAT。首次部署使用 `github-pages` Environment；如该环境已有分支限制，只允许 `main` 即可。如设置了 Required reviewers，部署会等待其审批；不需要审批时无需添加 reviewers。

部署成功后的默认地址为：

**https://orisnow420.github.io/H-ONE/**

最终以 deploy job 输出的 `page_url` 和 Environment 链接为准。当前仓库是 public，能使用公开 Pages；若以后改为 private，需检查 GitHub 套餐对 Pages 的支持。

### 5. 首次运行和分支保护

1. 配好 Secrets 和 Pages 后，进入 [Actions](https://github.com/OriSnow420/H-ONE/actions)，选择 **Unity CI → Run workflow → main**。勾选 `deploy_pages`。
2. 查看 `Unity tests (EditMode + PlayMode)` 及 `Unity test results` 检查；失败时下载 `unity-test-results`，检查 XML 和 Editor 日志。
3. 四个 Build job 都应成功；从该次运行底部的 Artifacts 下载 `H-ONE-Windows` / `Linux` / `macOS` / `WebGL`。每份包含一个 `.tar.gz`；解压后再运行。保留期为 14 天。
4. `Deploy WebGL to GitHub Pages` 成功后打开 Environment 的网址，验证游戏加载、输入、画面和声音。浏览器通常需要用户先点击页面才允许播放音频。
5. 至少完成一次运行，让检查名称出现后，再到 **Settings → Rules → Rulesets**（或 Branches → Branch protection）为 `main` 启用 PR 和必需状态检查，选择 **`CI gate`**。

首次没有 Secrets 的运行会明确失败；配置 Secrets 后重新运行即可。不要将只有 lint 成功、而 Unity 测试被跳过的情况视为通过。

## 构建入口和 WebGL 约定

场景清单为 [`.github/unity-build-scenes.txt`](../.github/unity-build-scenes.txt)，**第一条有效路径就是启动场景**。2026-11-10 的 M2 收尾清单为 `Assets/Game/Scenes/MainMenu.unity` → `Assets/Game/Scenes/M2_MechanismGallery.unity`，发布的是带主界面的机制试场，尚非第一章正式两关。Editor Build Settings 使用同序清单，构建保护拒绝分歧。

以后发布正式关卡时，先准备对应场景、`.meta` 及依赖，再同步更新清单、主界面导航和 M2 构建保护的发布策略。当前不将 Core/ShadowSlice/Northline/Sample 加入发布列表。清单支持空行和整行 `#` 注释；重复、缺失或无效场景会使构建失败。将场景加入包不会自动生成关卡选择菜单。

[构建脚本](../Assets/Game/Editor/CiBuild.cs)不调用 `PlatformerSceneBuilder`，不改写本地 `EditorBuildSettings.asset`，也不保存或重建场景。CI 中临时将产品名设为 H-ONE，并在构建结束后恢复设置。版本号沿用项目设置，产物所属提交可在 Actions 运行页追溯。

WebGL 构建临时使用 **Gzip + Decompression Fallback**，并关闭线程。Pages 无法自定义 `Content-Encoding`、COOP/COEP 响应头，因此由 Unity loader 在浏览器中解压资源；保留 Default WebGL 模板的相对资源路径，可在 `/H-ONE/` 仓库子路径下加载。上传到 Pages 的目录是 `build/WebGL/H-ONE`，`index.html` 必须位于站点根目录。

目前不会执行桌面代码签名、macOS notarization、安装包生成、Android/iOS 构建或 GitHub Release 发布。macOS 产物是未签名的应用，需要在实际 Mac 上验收；当前自动测试不是 Windows/macOS/WebGL 各自的 Player 测试。新增移动端需要另行配置 SDK、签名和设备验收。

## 常见问题

| 现象 | 检查方法 |
| --- | --- |
| `Unknown argument: noCoverageEnabled`，无测试 XML | 测试工具参数解析失败，Unity 尚未启动。使用测试步骤的 `GAME_CI_COVERAGE_ENABLED: 'false'`，移除 `with.coverageEnabled: false`，提交修复后新建一次运行；重新运行旧提交仍会使用旧配置 |
| `UnityTest yielded WaitForEndOfFrame, which is not evoked in batchmode` | batchmode 不执行帧末等待；暂停截图测试当前仅在远程 CI 按完整名称临时排除。确认 workflow 的 `customParameters` 仍有对应排除项；其他测试若出现相同错误，单独定位并报告，不扩大跳过范围。恢复图形测试见下方待办 |
| license configuration 失败 | 检查 Repository secrets 名称、三项 Personal/Pro 凭据或许可服务器地址是否齐全 |
| 激活失败 / No valid Unity license | 检查 `.ulf` 是否完整、账号授权是否有效、Unity 6 授权流程与 GameCI 是否兼容、是否耗尽并发席位 |
| Fork / Dependabot PR 没有 Secrets | GitHub 默认不向这些 PR 提供 Unity 凭据；本流程会明确失败。维护者审查后将可信变更带到本仓库普通分支再开 PR；不要改用 `pull_request_target` 执行外部代码或向其暴露 Secrets |
| Docker 镜像不存在 / 拉取受限 | 确认 `unityci/editor` 有当前 Editor 版本和对应模块的镜像；检查 Docker Hub 限流。没有镜像时需提供相同 Unity 版本的自建镜像或构建 runner，不能只改 CI 版本绕过 |
| LFS 资源下载失败 | 检查 Git LFS 对象已 push、配额和访问权限；runner 需要实际二进制资源 |
| Cache 频繁淘汰 / 空间不足 | 多平台 Library 较大；清理旧的 Actions caches，必要时减少缓存范围或改用容量更大的 runner |
| Pages job 返回 404 | 先在 Settings → Pages 选择 GitHub Actions，检查 Environment 和仓库套餐 |
| Pages 显示旧内容 | 检查本次 deploy 是否完成、网址和浏览器缓存；测试/构建失败时不会替换上次成功站点 |
| WebGL 报解压或线程错误 | 确认实际走 `CiBuild.Build`，不要绕过其 fallback / 单线程设置，也不要修改模板为站点绝对路径 |
| 本地改动未出现在网页 | 核对 Actions 运行的 commit SHA；只修改本地 WSL / Windows 副本不会更新远端站点 |

## 验证边界

2026-11-08 本机验证：actionlint `v1.7.12` 通过；许可证预检查的 6 种凭据组合、`CI gate` 的 9 种成功/失败/跳过/取消组合通过。`CiBuild.cs` 已同步到 Windows 工程，经 Unity `6000.0.84f1` 导入、编译通过（0 编译错误/警告），Editor 中调用 `ReadScenes()` 确认读取到 `Chapter01_Core`。Unity 生成的 `.meta` 已回写 WSL。没有修改场景、运行游戏、切换构建平台或调整 Editor 设置。

GitHub 只读检查确认：Actions 已启用，允许全部 Actions，默认 token 为只读；尚未设置 Actions Secrets，Pages 站点尚未启用。本次没有提交、推送或更改 GitHub 设置。Docker 镜像目录的网络检查超时，未确认当前版本各模块镜像的远端可用性。

该配置需在 GitHub 配置好 Unity 凭据、合入/推送 workflow 后执行首次云端验收。尚未实际运行云端 Unity 测试、多平台构建或部署；工作流静态 lint、Editor 脚本编译和本地场景清单读取通过，都不能替代云端激活、实际多平台构建、Pages 部署和浏览器游玩验证。验收状态应按实际运行结果记录。

2026-11-09 检查[运行 42481283634](https://github.com/OriSnow420/H-ONE/actions/runs/42481283634)（提交 `5814a8f7533c2da6c448ee2ce5f27583619eea2d`）：lint 和 Secrets 填写预检查通过；测试步骤在 CLI 参数解析阶段报 `Unknown argument: noCoverageEnabled`、退出码 1。Unity 尚未启动，没有测试报告或 artifacts，构建与部署被跳过。这是测试工具的参数兼容性问题，该次运行不能证明游戏测试通过或失败；Secrets 已填写也不能证明许可证已经激活成功。

修复只调整 workflow 的覆盖率参数传递方式。使用经官方 SHA-256 清单核验的 GameCI CLI `v0.1.72` 在本地复现旧错误，再用 Docker 命令替身检查修复后的实际调用：CLI 已完成参数解析，向容器传递 `COVERAGE_ENABLED=false` 和 `TEST_PLATFORMS=playmode;editmode;COMBINE_RESULTS`。此检查没有启动容器或 Unity、没有读取 GitHub Secrets；真实测试结论仍需在提交修复后的新一轮云端运行中取得。

2026-11-10 检查[运行 42760069461](https://github.com/OriSnow420/H-ONE/actions/runs/42760069461)（提交 `703b8134349e52dc22af5f36dd987f3474e247c5`）：已进入实际测试，EditMode 26/26、PlayMode 21/22 通过；暂停截图测试因 batchmode 不执行 `WaitForEndOfFrame` 失败，构建和 Pages 跳过。此次增加上述 CI 专用测试镜像及启动器，保持项目代码与测试断言不变。此处记录的是修复前运行结果，修复后的云端结果以对应新提交的 Actions 运行记录为准。

随后[运行 42762576260](https://github.com/OriSnow420/H-ONE/actions/runs/42762576260)（提交 `6ec92ceb5be39b40f04b9096ede0ba5fdf0530a7`）确认 PlayMode 的 Editor 为 `Batch mode: NO`，但在启动 LicensingClient 后立即退出，尚未执行测试或生成 PlayMode XML；EditMode 仍为 26/26。为定位这一启动问题，CI 镜像增加 `strace`，只保存子进程可执行文件名及执行状态到 `playmode.log.startup-trace.log`，命令参数与环境值不写入诊断文件。此结果不能视为 PlayMode 通过或游戏代码失败。

[运行 42764660810](https://github.com/OriSnow420/H-ONE/actions/runs/42764660810) 的启动追踪显示非 batchmode Editor 尝试执行 `dbus-launch`，镜像中未安装而返回 `ENOENT`；EditMode 26/26。CI 测试镜像补装 `dbus-x11` 提供该程序，追踪器改用独立进程，避免等待仍存活的 LicensingClient。这些调整只涉及 Linux CI 启动环境；PlayMode、四平台构建和 Pages 仍待新运行验证。

[运行 42769225402](https://github.com/OriSnow420/H-ONE/actions/runs/42769225402) 的文件追踪进一步显示：非 batchmode Editor 连续查找光标主题与 `left_ptr`、`watch` 等光标均为 `ENOENT`，随后退出。CI 镜像补装 `dmz-cursor-theme`，显式设置 `XCURSOR_THEME=DMZ-White`，并在构建镜像时检查所需光标存在；定位后移除进程追踪，避免干扰许可证后台进程和编译子进程。该轮 EditMode 的失败是 Bee 内部管道关闭，汇总步骤随后完成导入与编译，未报告项目源码错误。新的真实测试结果仍以 Actions 报告为准。

## 待解决问题：远程图形 Editor 启动与暂停截图测试

2026-11-10 用户决定暂时跳过上述单项远程截图测试，保留其他测试与四平台构建、Pages 的成功门槛。batchmode 已确认不支持该用例依赖的 `WaitForEndOfFrame`；改用 Xvfb 下的非 batchmode Editor 后，Unity `6000.0.84f1` 在项目导入与测试执行之前以退出码 1 结束，没有生成 PlayMode XML。补装 D-Bus、光标主题后仍未解决，不能将这些缺失依赖认定为最终根因。最后的原生诊断配置见提交 `7e64b3e`，[诊断运行 42771364778](https://github.com/OriSnow420/H-ONE/actions/runs/42771364778)；本次不继续调试这一启动问题。

后续验收：先让相同 Unity 版本的远程图形 Editor 稳定启动，再运行完整暂停截图用例，确认截图、暂停动画、恢复释放和重置后的进度状态通过；最后移除 workflow 的单项排除过滤器，确认该用例重新出现在真实测试报告中。跳过后的测试通过不代表该问题已修复。
