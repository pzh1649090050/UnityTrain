# UnityTrain · 风扇动画与巡检培训

使用 **Unity 2022.3.55f1c1（中国版）** 的 3D 内置渲染管线项目。以“一个风扇、两个 uGUI 按钮”为主讲案例，学习 Animation、Animator、Cinemachine、Timeline 和 C#；机房案例用于设备交互、多点巡检、HTTP 和 Editor 扩展。

完整教材：[Word 版](Docs/Unity风扇与巡检培训手册.docx)、[HTML 版](Docs/Unity风扇与巡检培训手册.html)、[Markdown 源文档](Docs/Unity风扇与巡检培训手册.md)。包含安装下载、从空场景复现、五天课时、练习答案、排错和主讲脚本完整源码。

## 风扇主讲场景

当前场景已接入你创建的“巡检”Blend List Camera，按钮按“开始/停止”切换，按正面→右面→后面→左面播放；当前 Loop 开启会循环。完整教材第 10.1～10.12 节详细讲解配置、接线与代码。见 [巡检按钮接线说明](Docs/巡检按钮接线说明.md)。下文 Timeline 功能作为清空 Inspection Camera 引用后的教学备选。

打开 `Assets/Training/Scenes/03_FanTraining.unity`，点击 Play。场景只有两个 uGUI 按钮：

- **打开/关闭风扇**：`FanTrainingController.ToggleFan` 设置 Animator 的 `Running` 参数，切换扇叶循环动画。
- **开始/暂停/继续巡检**：`ToggleInspection` 控制十二秒 Timeline，依次播放全景、扇叶特写、跟随巡检探针三个 Cinemachine 镜头。自然结束后可再次启动，Esc 提前停止。

模型 Prefab：`Assets/Training/Prefabs/TrainingFan.prefab`；旋转状态机：`Animations/Fan.controller`；巡检序列：`Timelines/FanInspection.playable`。开关与巡检互相独立，Inspector 可查看两个持久化按钮事件。

安装版本已固定为 **Cinemachine 2.9.7、Timeline 1.6.2**。Animation 和 Animator 是编辑器内置功能，不需要单独下载；Package Manager 操作和版本差异见完整教材第 3 章。

`02_CinematicsLab` 是机房镜头编排进阶场景，可以直接从 Project 打开。

## 快速开始

1. 在 Unity Hub 中添加本目录 `D:\UnityTrain`，选择 **2022.3.55f1c1** 打开。第一次导入需要解析模板自带的 Unity 包。
2. 打开 `Assets/Training/Scenes/00_Welcome.unity`，点击 Play，选择“风扇动画与巡检实训”开始主讲案例，或点“进入机房演示”学习扩展案例。也可直接打开 `03_FanTraining.unity`。
3. 默认**本地演示模式**，不需要后端。左侧设备目录或直接点击 3D 设备均可选择，右侧可启停、开关柜门、设置告警。
4. 点“开始巡检”，到达设备后记录正常/异常，再点“下一点”。支持暂停、继续、手动漫游和重置。重置会清空当前检查记录。
5. 学习网络章节时，安装 Python 3（建议 3.10+），双击 `Server/启动服务.cmd`；在机房底部切换 HTTP 模式，再同步设备。仅在 HTTP 模式提交成功后才显示服务端确认。

已构建的版本位于 `Builds/Windows/UnityTrain.exe`（构建目录不进入版本管理）。发布给学员时应复制整个 Windows 文件夹，不能只复制 exe。

## 操作

| 操作 | 输入 |
|---|---|
| 水平移动 | WASD |
| 观察方向 | 按住鼠标右键并移动 |
| 降低 / 升高 | Q / E |
| 选择设备 | 鼠标左键或左侧目录 |
| 自动巡检 | 底部开始、暂停、继续、下一点 |
| 解除自动控制 | 手动漫游 |
| 重置位置与记录 | 重置（网络请求期间禁用） |

自动巡检到点后等待学员检查，**不会自动跳过设备**。点击别的设备查看详情不会改变巡检顺序；继续前需记录当前巡检点绑定的设备。手动漫游不使用碰撞角色控制器，适合讲解摄像机 Transform；活动范围限制在机房附近。

## 文件入口

| 路径 | 内容 |
|---|---|
| `Assets/Training/Scenes` | 入口与完整机房场景 |
| `Assets/Training/Prefabs` | 四类设备，可直接拖入场景 |
| `Assets/Training/Animations` | 柜门、转子、告警 Animation Clip 与三层 Animator |
| `Assets/Training/Scripts` | 独立职责的运行时 C# 组件 |
| `Assets/Training/Editor` | Inspector、路线工具、配置检查、资源生成与构建 |
| `Assets/Training/Config/Network.asset` | HTTP 地址与超时配置 |
| `Assets/Training/Tests` | Unity 自动化测试 |
| `Server` | Python 标准库 HTTP 服务及测试 |
| `Docs/课程指南.md` | 五天教案及三天压缩安排 |
| `Docs/练习与参考答案.md` | 每天的练习、答案和验收方法 |
| `Docs/接口与架构.md` | 数据流、接口字段、代码阅读顺序 |

## Editor 教学菜单

- **培训 → 巡检路线编辑器**：查找路线、编辑有序点数组、从 Scene 视角添加点、使用手柄移动、保存；新点需绑定设备。
- **培训 → 配置检查**：针对机房场景检测重复/空 ID、缺失引用、空路线及丢失脚本；风扇场景用独立自动化测试验收。
- **培训 → 安装培训所需包**：通过 Package Manager API 安装固定的 Cinemachine/Timeline 及内置模块。
- **培训 → 生成风扇主讲场景**：生成风扇模型、两种扇叶动画、状态机、Timeline 与两个按钮；生成前关闭该场景，先备份课堂修改。
- 选中设备可见自定义 Inspector，播放时可用调试按钮。
- **培训 → 重新生成演示资源**：覆盖生成的场景和四种设备预制体，用于恢复教学初始状态；先备份修改。不修改课程脚本。
- **培训 → 构建 Windows 演示**：Development、Mono、Windows x64 构建。

## 网络与记录

默认服务只监听 `127.0.0.1:8080`。改变端口可运行 `python Server/server.py --port 8081`，再修改机房地址栏或 Network 配置。运行时修改地址用于本次演示，不作为项目配置保存方式。

设备初始状态为停止、柜门关闭、无告警。HTTP 同步以服务端状态为准；本地模式的操作不会自动上传，切换 HTTP 后需手动同步。设备状态随服务重启复位，巡检提交保存在 `Server/data/inspections.jsonl`。

提交失败时记录保留，允许重试；同批记录重试通过 batchId 去重。编辑检查记录会产生新的 batchId。Unity 同时保存最近的本地草稿到 `Application.persistentDataPath/inspection-draft.json`，可作课堂排错材料，**不会自动恢复到新一轮巡检**。

## 验证与构建

```powershell
python -m unittest discover -s Server -v
# 指定编辑器执行自动化测试、重新生成资源或构建：
powershell -ExecutionPolicy Bypass -File Tools/Verify.ps1 -Task Test
powershell -ExecutionPolicy Bypass -File Tools/Verify.ps1 -Task Generate
powershell -ExecutionPolicy Bypass -File Tools/Verify.ps1 -Task Build
# 构建之后验证真实 Windows 程序；测试服务使用随机空闲端口与临时记录目录：
python Tools/run_smoke.py
```

同一项目不要同时由交互编辑器与批处理进程打开。可在编辑器的 Test Runner 执行测试。运行日志位于 `Logs`，验证输出位于 `TestResults`；这些生成文件不属于课程交付内容。

## 环境与素材说明

- 随包提供 **Noto Sans CJK SC** 开源中文字体，编辑器与构建程序都能离线显示中文；字体许可见 `Assets/Training/Fonts/OFL.txt`，来源见 `Docs/第三方资源.md`。
- 场景由 Unity 自带几何体与程序创建的材质构成，无付费模型、外部动画或第三方服务依赖。
- uGUI、传统 Input Manager、UnityWebRequest、协程、JsonUtility；模板自带的包按原版保留。
- 本地 HTTP 使用 Development 构建允许的明文 HTTP 配置。该服务用于教学，无账号认证和生产部署配置。
- 提供 `.gitignore`，保留所有 Assets 的 `.meta`；Unity 缓存、构建输出、测试结果和本地编辑器配置不纳入版本管理。
