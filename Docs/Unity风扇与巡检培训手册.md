# Unity 风扇动画与巡检培训手册

适用编辑器为 Unity 2022.3.55f1c1，采用 3D 内置渲染管线。读者是具备基本编程概念的 Unity 新手，讲师可据此开展五天培训。课程先用一个风扇、两个按钮建立完整操作链，再进入机房设备交互、HTTP 与 Editor 扩展。

当前主讲场景使用“巡检”Blend List Camera，依次播放正面、右面、后面、左面；保存的 Loop 已开启，按开始/停止控制，重新开始从正面重播。第 10 章给出当前四面方案的完整配置、按钮绑定和脚本讲解，第 11 章保留十二秒 Timeline 对照实训。以 Inspector 实际引用和 Loop 设置为准。

本手册覆盖环境安装、基础操作、Animation、Animator、uGUI、Cinemachine、Timeline、C#、巡检、HTTP 和 Editor。每个实训都说明操作入口、具体步骤、预期现象和排错方法。Word 版用于编辑与打印，HTML 版用于浏览，Markdown 为内容源。

## 1 课程结果与项目入口

完成培训后，学员应能在空场景中搭建风扇模型，制作循环扇叶动画，用按钮和脚本控制 Animator，用 Cinemachine 设置镜头，再用 Timeline 编排一次巡检。学员还应能解释场景引用如何连接、接口失败如何处理，以及 Editor 工具为何需要 Undo 和保存。

| 场景 | 用途 | 入口 |
|---|---|---|
| 00_Welcome | 演示入口 | Assets/Training/Scenes/00_Welcome.unity |
| 03_FanTraining | 主讲案例，一个风扇和两个按钮 | Assets/Training/Scenes/03_FanTraining.unity |
| 01_MachineRoom | 多设备、手动漫游、巡检记录和 HTTP 扩展 | Assets/Training/Scenes/01_MachineRoom.unity |
| 02_CinematicsLab | 多设备镜头与时间轴进阶对照 | Assets/Training/Scenes/02_CinematicsLab.unity |

推荐先打开 03_FanTraining，点击 Play。“打开风扇”使扇叶旋转，再次点击关闭。“开始巡检”播放正面、右面、后面、左面序列；当前 Loop 开启会循环，再次点击“停止巡检”或按 Esc 停止，重新开始从正面重播。两个按钮独立，巡检不改变风扇运行状态。原十二秒 Timeline 的暂停/继续只在第 11 章的对照模式中使用。

讲解前先体验结果，再另存一个练习场景并从基础步骤复现。不要一开始就运行生成器代替练习。重新生成会重建生成资源，课堂修改应先备份。

## 2 五天课时安排

每天建议六小时，合计三十小时。新手需在课前具备变量、条件、方法等基本编程概念；完全零编程基础者建议增加两天 C# 预备课。

| 天 | 六小时内容 | 当日交付 |
|---|---|---|
| 第一天 | 环境与包安装 1.5 小时，界面与基础操作 2 小时，风扇建模与 Prefab 2 小时，检查 0.5 小时 | 可保存、可重开的风扇练习场景 |
| 第二天 | Animation 2 小时，Animator 2 小时，uGUI 与按钮脚本 1.5 小时，检查 0.5 小时 | 风扇开关真正控制扇叶动画 |
| 第三天 | Cinemachine 四面配置和按钮逻辑 2 小时，Timeline 对照 2.5 小时，组合演练 1 小时，检查 0.5 小时 | 四面开始停止与循环，Timeline 暂停继续 |
| 第四天 | C# 与组件协作 1.5 小时，多点巡检 1.5 小时，HTTP 2 小时，异常练习 1 小时 | 设备控制、检查记录与真实提交 |
| 第五天 | Editor 扩展 3 小时，综合改造 1.5 小时，测试构建 1 小时，考核 0.5 小时 | 可撤销的工具与 Windows 演示 |

三天压缩版适合已有 C# 基础者：第一天合并基础、Animation、Animator 与按钮；第二天完整保留 Cinemachine、Timeline 和巡检；第三天完成 HTTP、Editor 实操与构建。完整练习可课后继续，不能把只看演示当作全部掌握。

## 3 下载与环境准备

### 3.1 编辑器和项目

1. 从 Unity 中国官方入口获取 Unity Hub，在 Hub 中确认已安装的编辑器为 2022.3.55f1c1。中国版末尾的 c1 是版本组成部分，培训统一该版本。
2. 本机已经安装，无需重复下载。若培训机没有该版本，从官方历史版本入口查找，并在安装后使用 Hub 的 Locate 定位 Unity.exe；不要把其他补丁版本当作相同版本。
3. 在 Hub 的 Projects 页面选择 Add，添加 D:\UnityTrain；选择 2022.3.55f1c1 打开。
4. 等待首次导入和包解析完成。Console 红色错误必须先处理，再进入 Play。
5. Windows 演示使用 Mono。Android、WebGL、IL2CPP 模块不是本课必需项；需要扩展平台时再从 Hub 的 Add modules 安装对应模块。
6. 安装 Visual Studio 的 Unity 开发工作负载，或使用已支持 C# 的其他编辑器；在 Edit → Preferences → External Tools 选择 External Script Editor。双击 .cs 应能打开代码。

项目目录中的 Assets 保存资源，Packages 记录包依赖，ProjectSettings 保存项目配置。Library 是可重建缓存。交换项目时保留 .meta，不把 Library 当作项目源码提交。

### 3.2 四项技术是否需要下载

| 技术 | 是否单独安装 | 本项目版本或入口 | 安装后的确认方式 |
|---|---|---|---|
| Animation 窗口 | 不需要，编辑器内置 | Window → Animation → Animation | 可以创建和编辑 .anim |
| Animator 窗口及组件 | 不需要，编辑器内置 | Window → Animation → Animator | 可以编辑 .controller 并设置参数 |
| Cinemachine | 通过 Package Manager 安装 | com.unity.cinemachine 2.9.7 | In Project 显示该版本，能添加 Cinemachine Brain |
| Timeline | 通过 Package Manager 安装，本项目已带 | com.unity.timeline 1.6.2 | Window → Sequencing → Timeline 可打开 |

Animation 窗口用于制作 Animation Clip；不要为了本课再安装同名第三方插件。旧版 Animation 组件属于 Legacy 播放方式，本课使用 Animator，不需要额外添加 Legacy Animation 组件。

### 3.3 安装 Cinemachine

1. 打开 Window → Package Manager。
2. 左上 Packages 过滤器从 In Project 切为 Unity Registry。In Project 只显示已安装包，未安装时搜不到并不代表包不存在。
3. 搜索 Cinemachine，检查版本。课程使用 2.9.7；不要直接按其他版本教程寻找菜单。
4. 如列表默认显示 3.x，使用左上角加号 → Add package by name，Name 填 com.unity.cinemachine，Version 填 2.9.7，点击 Add。
5. 等待下载和编译，切回 In Project，确认版本 2.9.7。Project 的 Packages 中应出现 Cinemachine。
6. 本课程的对象名为 Cinemachine Virtual Camera，常用类型是 CinemachineVirtualCamera，命名空间 Cinemachine。3.x 的 Cinemachine Camera 和 Unity.Cinemachine 命名不用于本工程。

项目已完成实际安装。新建个人练习工程时仍需按上述步骤练习一次，避免只会使用预装结果。

### 3.4 安装和确认 Timeline

1. 在 Package Manager 的 Unity Registry 搜索 Timeline。
2. 本项目已安装 1.6.2；在 In Project 查看版本，不必重复安装。
3. 在空练习工程需要固定版本时，用 Add package by name，填 com.unity.timeline 和 1.6.2。
4. 编译完成后打开 Window → Sequencing → Timeline。若已有菜单而窗口空白，需要选择带 Playable Director 的对象或创建序列，不是重新安装包。
5. 课程使用 Timeline 1.6 系列 UI。本地安装包的 Documentation~ 与官方 1.6 文档可供查阅；官方网页可能显示同一系列的更新补丁号，项目实际版本以 Package Manager 和 packages-lock.json 为准。

### 3.5 安装故障与培训前检查

下载失败先检查网络、组织代理和 Package Manager 错误文本；不要关闭证书校验。确认使用官方 Unity Registry，而非把 npm 搜索结果当作 Unity 包。必要时由培训管理员准备合规包缓存，提前在每台机器导入一次，正式授课时不依赖临时下载。

培训包版本以 Packages/manifest.json 和 packages-lock.json 为依据。工程提供“培训 → 安装培训所需包”菜单，使用 Unity Package Manager API 安装固定版本，便于讲师恢复环境；课堂仍讲解 Package Manager 的手动操作。

讲师需确认两个 uGUI 按钮、中文字体、扇叶旋转、四面顺序、Loop 循环、停止与正面重播均可运行。Timeline 对照课另验证暂停继续及两次检查信号。HTTP 章节额外安装 Python 3.10+，无需 pip 依赖。

## 4 Unity 基础界面和操作

Hierarchy 显示场景对象与父子关系；Project 显示磁盘资源；Inspector 显示当前选择对象的组件；Scene 用于编辑；Game 展示摄像机输出；Console 用于检查日志。切换 Play 会进入运行状态，运行时修改通常不会保存到场景。

1. 在 Scene 中选中 TrainingFan 并按 F 聚焦。W、E、R 切换移动、旋转、缩放工具。
2. 展开 TrainingFan，选择 Rotor，再选择一片 Blade。比较父节点与子节点的 Local Position。
3. 移动 TrainingFan，会带动全部部件；旋转 Rotor 只带动扇叶与轮毂，护圈和支架应保持静止。
4. 在 Scene 右键拖动视角，滚轮缩放。编辑视角不等于 Main Camera 的输出视角。
5. 修改对象后 Ctrl+S 保存，关闭再打开场景检查。给模型添加组件后检查 Inspector 引用，空字段不会自动猜测目标。

练习：复制一个风扇实例，移动到侧面，换一个新材质。解释 GameObject、Component、Transform 和资源文件之间的区别。

## 5 从几何体搭建风扇模型

主讲 Prefab 位于 Assets/Training/Prefabs/TrainingFan.prefab。它由 Unity 几何体和一个程序生成的护圈网格组成，无需下载风扇模型。

| 节点 | 职责 | 是否随扇叶旋转 |
|---|---|---|
| TrainingFan | 模型根节点，挂 Animator | 不整体旋转 |
| Base 和 Stand | 底座与支架 | 否 |
| Motor | 电机外壳 | 否 |
| CageRing 和 CageSpoke | 护圈和护网 | 否 |
| Rotor | 扇叶旋转轴，局部位置约为 0 2.5 -0.25 | 是 |
| Rotor 下的 Hub 和 Blade_0 至 Blade_3 | 轮毂和四片扇叶 | 是 |

手动复现：创建空对象 TrainingFan，再用 Cylinder 制作底座、支架与电机；电机沿 Z 轴放置，可把 Cylinder 绕 X 旋转 90 度。新建空对象 Rotor，把旋转中心放在电机中心。用四个 Cube 制作扇叶，分别旋转和偏移到四个方向，并全部放到 Rotor 下。最后添加静止护圈、灯光和地面，拖回 Project 保存成 Prefab。

转轴错位时不要用脚本补偿一串坐标。先检查 Rotor 的位置和各 Blade 的局部偏移。旋转轴应在电机中心，扇叶位于轴的周围，而不是把每片扇叶分别绕自身中心转。

验收：手动修改 Rotor 的 Z 旋转，四片叶片围绕中心旋转，支架和护圈不动。之后撤销手动旋转，保持初始 Z 为 0。

## 6 Animation 制作扇叶动画

### 6.1 创建动画片段

1. 另存练习场景，选中 TrainingFan 根节点，打开 Window → Animation → Animation。
2. 选择 Create，保存 FanRotorSpinPractice.anim。已有动画时通过片段下拉菜单创建新片段。
3. 动画绑定根节点为 TrainingFan；在 Add Property 中展开 Rotor，添加 Transform 的旋转属性。观察属性路径应指向 Rotor，不是根节点。
4. 使用 Curves 视图编辑 Rotor 的 Z 欧拉角。添加 0 秒 0 度、0.25 秒 90 度、0.5 秒 180 度、0.75 秒 270 度、1 秒 360 度。
5. 若时间轴按 60 fps 显示，对应帧为 0、15、30、45、60；如果帧率不同，按秒换算。不要把帧数误当秒。
6. 将曲线切线设为 Linear，避免每转一圈先加速再减速。检查最终曲线值确实为 360，不是被归一化成 0。
7. 选择动画片段，在 Inspector 开启 Loop Time。退出红色录制状态，再预览。

工程中的 FanRotorSpin.anim 已使用 Rotor/localEulerAnglesRaw.z 的线性 0→360 曲线和一秒循环。不要用单条 Quaternion 0→360 的最短路径插值来代替完整旋转。

### 6.2 制作停止片段和修改速度

创建 FanRotorIdlePractice.anim，记录 Rotor 的 Z 角度为 0 并保持不变。这个片段用于教学中的停止状态。工程的停机设计会在短过渡后回到初始角度，不是机械惯性滑停。

转速可通过延长 Clip 时长或调整 Animator 状态 Speed 改变。本课先把 Running 状态 Speed 从 1 改为 0.5，验证每圈从一秒变成两秒。不要同时修改时长、状态速度和脚本速度，否则难以判断来源。

练习：制作两秒一圈的副本，保留原动画作对照。验收时观察至少五圈，确认连续、轴心不偏移且支架不转。

## 7 Animator 编写开关状态机

### 7.1 建立 Controller

1. 在 Project 新建 Animator Controller，命名 FanPractice.controller，赋给 TrainingFan 的 Animator.Controller。
2. 打开 Window → Animation → Animator，把停止片段和旋转片段拖入，命名 Stopped、Running。
3. 右键 Stopped，设为默认状态。橙色默认状态表示进入状态机后的起始状态。
4. 在 Parameters 新建 Bool 参数 Running，默认 false。状态名和参数可以同名，但属于不同对象。
5. 从 Stopped 建到 Running 的 Transition，关闭 Has Exit Time，Conditions 设 Running 为 true。
6. 建反向 Transition，关闭 Has Exit Time，Conditions 设 Running 为 false。
7. 本工程两条过渡使用固定时长 0.15 秒，保持短暂混合。需要完全瞬时切换可在练习副本把时长设为零。

### 7.2 验证参数与脚本关系

进入 Play，选中风扇对象，在 Animator 窗口切换 Running 参数。只有扇叶应旋转，取消参数后回到停止状态。按钮最终调用的是 fanAnimator.SetBool("Running", IsRunning)，不会在 Update 中手动 Rotate 扇叶。

若无反应，按顺序检查 Controller 是否赋值、默认状态是否正确、Motion 是否为空、参数拼写是否一致、过渡条件是否满足、Has Exit Time 是否导致等待，以及 Animator 的速度是否为零。

练习：把 Running 参数改成另一个名字并同步修改脚本，然后恢复原名。用这个实验理解参数名是脚本与动画资源的接口。

## 8 uGUI 两个按钮与引用绑定

1. Hierarchy 创建 UI → Canvas，设置 Screen Space Overlay。
2. Canvas Scaler 选 Scale With Screen Size，Reference Resolution 设 1600×900，Match 设 0.5。
3. 创建两个 UI Button，命名 FanSwitchButton 和 InspectionButton。使用普通 uGUI Text 时，不需要导入 TMP Essentials；若选用 TextMeshPro，应另学 TMP 资源和组件差异，不要混用 Text 与 TMP_Text 类型。
4. 按钮文字设“打开风扇”和“开始巡检”，使用随工程提供的中文字体。Canvas 需有 GraphicRaycaster，场景需有一个 EventSystem 和 StandaloneInputModule。
5. 先在 03_FanTraining 场景副本中找到挂 FanTrainingController 的对象（当前名为“脚本控制”）。当前四面模式绑定风扇 Animator、Cinemachine Brain、“巡检”Blend List、两个 Button 和状态 Text；详细对应关系见第 10.6 节。空场景按章节跟做时，本章只绑定风扇 Animator、两个 Button 和状态 Text，第 10 章创建镜头后再补齐巡检引用。Director、Probe、Highlight 留到第 11 章 Timeline 对照课处理。
6. FanSwitchButton 的 Button → On Click 点加号，拖入挂脚本的对象，选择 FanTrainingController.ToggleFan。
7. InspectionButton 同样绑定 FanTrainingController.ToggleInspection。每个按钮只保留一次绑定，避免一次点击执行两次后看起来没有变化。
8. 已完成场景副本可验证两个按钮。空场景本章先验收风扇启停，第 10 章完成后验收四面巡检开始、停止、循环与正面重播，第 11 章再单独验收 Timeline 的暂停/继续。修改引用前先退出 Play，配置后重新运行。

工程使用 Inspector 可见的持久化 On Click 绑定，不在 Start 中再次 AddListener。练习若改为代码绑定，应先移除 Inspector 绑定，避免重复订阅。

## 9 主讲脚本的工作方式

完整源码是 Assets/Training/Scripts/FanTrainingController.cs。建议讲师按字段引用、生命周期、ToggleFan、ToggleInspection、停止事件、状态文字的顺序阅读。

| 方法或字段 | 学习要点 |
|---|---|
| public Animator fanAnimator | 通过 Inspector 显式引用另一个组件 |
| IsRunning | C# 运行状态，控制 Animator 的 Bool 参数 |
| ToggleFan | 取反状态并调用 SetBool，然后刷新按钮文字 |
| IsInspecting | 四面模式判断开始/停止；Timeline 模式额外区分待机和暂停 |
| inspectionCamera 与 UsesCameraSequence | 已绑定 Blend List 时优先走四面模式 |
| ToggleInspection | 四面模式开始/停止；Timeline 模式 Play/Pause/Resume |
| PreviousStateIsValid | 使旧镜头状态失效，配合重新启用实现正面重播 |
| stopped 事件 | 正常结束或手动 Stop 后恢复按钮、探针和检查灯 |
| OnInspectionCheckpoint | Signal Receiver 调用的公共方法，累计检查节点 |
| Update | 展示进度；Esc 提前停止，不直接修改摄像机或扇叶 |

四面模式通过 IsInspecting 选择开始或停止，不读取 Director 的暂停状态。在 Timeline 对照模式中，待机和暂停都可能对应 director.state == Paused，因此仍需要独立业务状态。

风扇按钮只写 Running 参数，四面镜头交给 Blend List，真实摄像机交给 Brain；只有 Timeline 对照模式才由时间轴编排镜头、探针和提示。两种巡检驱动互斥，避免争抢同一个 Transform。

## 10 Cinemachine 四面巡检配置与按钮控制

本章对应 03_FanTraining 场景中的“巡检”对象，使用 Cinemachine 2.9.7 的 Blend List Camera，顺序为正面、右面、后面、左面。按钮先调用 FanTrainingController.ToggleInspection，再由 Cinemachine Brain 把镜头结果应用到 Main Camera。四面巡检由 Blend List 的 Instructions 编排，原 Timeline 保留为下一章的对照练习。

学习目标：从空配置建立四个视角，理解 Hold、Blend、Loop 和 Priority，完成 uGUI 事件与脚本引用绑定，并能解释开始、停止、重播和风扇开关独立工作的原因。建议讲解 45 分钟、跟做 45 分钟、练习与验收 30 分钟。

### 10.1 对象分工与层级

当前工程把摄像机放在 [Camera] 分组，控制脚本放在 [Main] 下的“脚本控制”对象。分组名可改，连接依靠 Inspector 引用；不能因为对象改名就重新创建一套相机。核心层级如下：

```text
[Camera]
    Main Camera                 Camera + Cinemachine Brain
    巡检                        Cinemachine Blend List Camera
        正面                    Cinemachine Virtual Camera
        右面                    Cinemachine Virtual Camera
        后面                    Cinemachine Virtual Camera
        左面                    Cinemachine Virtual Camera
[Main]
    脚本控制                    FanTrainingController
[UI]
    Fan Training Canvas
        Controls
            打开风扇            Button → ToggleFan
            开始巡检            Button → ToggleInspection
```

Main Camera 是真实渲染摄像机，Brain 接收 Cinemachine 镜头结果。“巡检”管理四个子虚拟相机；虚拟相机只提供位置、朝向和镜头参数，不应额外挂 Camera 或 Audio Listener。四个子相机必须是“巡检”的直接子节点；移动到孙节点或其他父节点后，要重新检查 Instructions 引用。

风扇的 Animator 只负责扇叶。控制脚本只切换巡检状态和相机组件，实际摄像机姿态由 Brain 更新。不要同时让 FlyCamera、PatrolController 或另一个 Update 脚本写 Main Camera 的 Transform。

### 10.2 创建真实摄像机和巡检管理器

1. 打开 03_FanTraining 的练习副本，退出 Play 后再配置。已存在的 Main Camera 和“巡检”对象直接复用。
2. 检查 Main Camera 的 Camera 组件已启用，Tag 为 MainCamera，并添加一个 Cinemachine Brain。保持场景中只有需要的 Audio Listener。
3. 若从空场景复现，使用 GameObject → Cinemachine → Blend List Camera 创建管理器，命名“巡检”。该菜单属于本课的 Cinemachine 2.9.7。
4. 若菜单创建了示例子相机，可复用、改名并补齐到四个，避免保留多余重复相机。四个子相机分别命名为正面、右面、后面、左面。
5. “巡检”GameObject 及其父节点保持活动状态。等待按钮触发时可以取消 Blend List Camera 组件的启用勾选；本课脚本切换的是 enabled，不负责激活被 SetActive(false) 隐藏的整个对象。
6. 保持管理器普通 Priority 为 10。脚本开始时临时设为 10000，停止时恢复开始前的值。10000 是本项目约定，不是 Unity 的优先级上限；其他参与输出的顶层虚拟相机不要设得更高。

当前 Start 方法会主动禁用已绑定的 Blend List 组件，因此进入 Play 后不会自动开始巡检。仅在编辑器中勾选组件，不代表按钮已经绑定成功。

### 10.3 摆放正面右面后面左面镜头

1. 选中正面虚拟相机，在 Scene 视图把观察角度调整为能看完整风扇正面的构图。
2. 保持虚拟相机为当前选择对象，执行 GameObject → Align With View，把它对齐 Scene 视角；也可直接编辑世界位置和旋转。
3. 分别从风扇右侧、背面、左侧调整视角，对齐其余三个虚拟相机。不要把风扇模型旋转四次来冒充四个镜头。
4. 当前四个子镜头的 Follow、Look At 都为空，靠保存的位置和旋转构图，Field Of View 约为 60。新建固定视角时可使用 Body 的 Do Nothing、Aim 的 Do Nothing，手动确定朝向；已有可用构图不必为统一参数而重置。
5. 如要练习持续朝向风扇，可建立 FanLookAt，把子镜头的 Look At 绑定它，并使用 Composer；这属于另一种构图方式，应在副本中对照验证。
6. 逐个检查风扇是否在画面内、Near Clip 是否过大、地面是否遮挡主体。检查 [Camera] 父对象是否有偏移和旋转，避免误把局部坐标当世界坐标。

当前子镜头 Priority 均为 10，但巡检内部顺序由 Instructions 决定，不由四个子相机的 Priority 或 Hierarchy 排列自动推断。Game 视图表示真实输出，Scene 视图只是编辑观察窗口。

### 10.4 填写 Instructions 与时间参数

选中“巡检”，在 Blend List Camera 的 Instructions 中设置四行，每行的 Virtual Camera 都拖入相应子镜头。核对当前保存的场景配置：

| 顺序与镜头 | Hold 秒 | Blend 方式 | Blend 时间秒 |
|---|---|---|---|
| 1 正面 | 1 | Cut | 0 |
| 2 右面 | 0 | Ease In Out | 3 |
| 3 后面 | 0 | Ease In Out | 3 |
| 4 左面 | 0 | Ease In Out | 3 |

Hold 表示该条镜头完成进入混合后的额外停留；Blend 表示进入这一条镜头时如何与上一条混合。例如右面这一行的 3 秒，是正面切向右面的混合时间，不是右面切向后面的时间。Hold 为 0 不会跳过这条镜头，仍会经历它配置的混合。

按当前参数，从正面开始约停留 1 秒，随后用约 3 秒切向右面、3 秒切向后面、3 秒切向左面。大约 10 秒完成一轮四面浏览，实际受帧更新和进入巡检时的外层镜头切换影响；不要把它写死成原 Timeline 的 12 秒。

管理器内部的 Blend 与 Main Camera 上 Brain.Default Blend 分工不同：前者定义四个子镜头之间的转换，后者可能影响其他顶层镜头与巡检管理器之间的进入或退出转换。只改 Brain.Default Blend，不会把 Instructions 中三个 3 秒都改掉。

### 10.5 Loop 的两种结果

本次文档核对时，场景中的 Loop 已勾选。脚本不会修改 Loop，运行行为沿用你在 Inspector 的设置：

| Loop 设置 | 到达左面后的行为 | 如何结束 |
|---|---|---|
| 开启，当前场景配置 | 返回正面并重复四面巡检 | 点击停止巡检或按 Esc |
| 关闭，课堂对照练习 | 持续保持最后一个镜头 | 点击停止巡检或按 Esc |

Loop 关闭不等于自动调用 StopInspection，也不会自动恢复“开始巡检”按钮。最后一个镜头保持是 Blend List 的正常行为，本课没有另写自动完成计时器。

当前第一行是 Cut、0 秒，因此循环从左面回正面时会直接切回。想练习平滑闭环，可把第一行改为 Ease In Out、1 秒并观察循环衔接；这也会改变周期长度。完成练习后恢复原值，或明确记录新的授课参数。

### 10.6 绑定 FanTrainingController 字段

选中挂 FanTrainingController 的“脚本控制”对象，逐项拖入引用。对象改过名时以组件类型和引用为准，不要只按对象名猜测。

| Inspector 字段 | 拖入的组件或对象 | 本例作用 |
|---|---|---|
| Fan Animator | 风扇根对象上的 Animator | 保留风扇启停功能 |
| Camera Brain | Main Camera 上的 Cinemachine Brain | 相机输出与状态读取 |
| Inspection Camera | “巡检”上的 Blend List Camera | 选择当前四面巡检模式 |
| Fan Button | 风扇开关按钮的 Button | 刷新开关按钮文字 |
| Inspection Button | 开始巡检按钮的 Button | 开始与停止文字及可交互状态 |
| Status Text | Controls 下的状态 Text | 显示运行状态和子镜头名称 |

Inspection Director、Inspection Probe、Inspection Highlight 是原 Timeline 示例相关引用。在四面巡检模式下，Camera Brain 有值、Inspection Camera 有值且 Instructions 非空，就不会因为缺少探针而禁用按钮。每行镜头仍须人工检查非空且层级正确；HasInspectionSetup 不会逐项核验四个相机是否构图正确。

不要把“正面”子相机拖到 Inspection Camera：这个字段需要 CinemachineBlendListCamera 类型。也不要把 Main Camera 的 Camera 组件当成 Cinemachine Brain。

### 10.7 绑定 uGUI 的 On Click 事件

1. 选中 Canvas/Controls 下的“开始巡检”按钮，找到 UnityEngine.UI.Button 组件，展开 On Click。
2. 点击加号添加一条事件，把场景中的“脚本控制”对象拖到事件目标框，不能拖 Project 中的 .cs 脚本文件。
3. 在方法列表选择 FanTrainingController → ToggleInspection()，这是无参数的 public 方法。
4. 检查事件目标确实挂着 FanTrainingController，每次点击只调用一次。不要再加一条 PlayableDirector.Play，也不要同时绑定 ToggleInspection 和 StopInspection。
5. 当前工程使用 Inspector 的持久化绑定，Start 中没有再次 AddListener。若练习改为代码绑定，先移除 Inspector 事件，避免双重调用把“开始”立即变成“停止”。
6. 风扇开关仍只绑定 ToggleFan()，不需要改为巡检方法。
7. 检查 Canvas 的 GraphicRaycaster、场景中的 EventSystem 和 StandaloneInputModule，并排除覆盖按钮的可射线 UI。

Inspection Button 字段与 Button.On Click 是两条不同的连接：前者让脚本修改按钮文字和 interactable，后者让点击调用脚本。只填其中一处，不能保证完整交互。

### 10.8 脚本如何选择模式与检查配置

打开 Assets/Training/Scripts/FanTrainingController.cs，先看两个属性：

```csharp
public bool UsesCameraSequence => inspectionCamera != null;
public bool HasInspectionSetup => cameraBrain && (UsesCameraSequence ?
    inspectionCamera.m_Instructions != null &&
    inspectionCamera.m_Instructions.Length > 0 :
    inspectionDirector && inspectionDirector.playableAsset &&
    inspectionProbe && inspectionHighlight);
```

UsesCameraSequence 有值时走四面镜头模式；为空时才检查 Timeline 的完整引用。RefreshLabels 用 HasInspectionSetup 设置按钮能否点击。看到“巡检待配置”，先检查字段与 Instructions，不要为了点亮按钮强行把 interactable 永远设 true。

Start 先记录巡检管理器的原 Priority，并把组件 enabled 设 false。ToggleFan 只修改 Animator 的 Running 参数，始终不参与摄像机序列。这个分支结构使风扇旋转与巡检按钮互不抢占职责。

### 10.9 第一次点击开始巡检

Button.On Click 调用 ToggleInspection，脚本按以下顺序执行四面模式：

1. 检查 HasInspectionSetup；不完整则提示并返回，不开始巡检。
2. 检查 UsesCameraSequence，确认本次使用已绑定的 Blend List。
3. 如果 IsInspecting 已为 true，就调用 StopInspection；否则进入开始分支。
4. 停止仍存在的旧 inspectionDirector，避免 Timeline 与 Blend List 同时争抢 Main Camera。
5. 保存开始前的 Priority，设置 PreviousStateIsValid 为 false，让镜头序列重新计算起点。
6. 临时提高 Priority，启用 Blend List，清零本轮显示时间，设置 IsInspecting 为 true，刷新按钮文字。

实际开始分支中的关键代码如下，完整类见附录：

```csharp
if (inspectionDirector) inspectionDirector.Stop();
originalInspectionPriority = inspectionCamera.Priority;
inspectionCamera.PreviousStateIsValid = false;
inspectionCamera.Priority = 10000;
inspectionCamera.enabled = true;
sequenceTime = 0;
IsInspecting = true;
RefreshLabels();
```

脚本没有在 Update 里计算四个镜头的位置，也没有用四个 if 切换视角。Blend List 按 Instructions 选择子镜头、计算混合，Brain 再把结果写入真实摄像机。因此调整巡检节奏时，应先改组件里的 Hold/Blend，而不是在脚本中寻找固定的 3 秒等待代码。

### 10.10 再次点击停止与重新开始

运行期间按钮文字为“停止巡检”。再次点击仍调用同一个 ToggleInspection，此时 IsInspecting 为 true，转而调用 StopInspection：

```csharp
inspectionCamera.enabled = false;
inspectionCamera.Priority = originalInspectionPriority;
inspectionCamera.PreviousStateIsValid = false;
```

之后停止可能存在的 Director，并调用统一收尾方法，把 IsInspecting 设 false，按存在性复位原示例的探针和检查灯，刷新按钮。显式收尾保证即使 Director 从未运行，也能恢复“开始巡检”文字。风扇 Running 状态不会被这个过程改变。

再次开始时会重新清零显示时间、使相机旧状态失效并重新启用序列，实际效果是从第一条“正面”重播。四面模式没有 Pause/Resume 分支；停止后不是从中断处续播。Esc 同样调用 StopInspection。

停止管理器相当于释放相机控制，之后画面由 Brain 根据场景中其他可用镜头决定。不要把“禁用巡检”理解为脚本一定把 Main Camera 瞬移到某个硬编码坐标。

### 10.11 状态显示与 Timeline 教学对照

Update 在巡检运行时累计 sequenceTime，并读取 inspectionCamera.LiveChild.Name 显示当前选中的子镜头。该时间是已运行时长，不是固定长度进度条；混合时子镜头名可能已变为目标镜头，不能据此认定画面已经完全切换到位。停止后已运行时长会保留，下一次开始才清零。

| 对照项 | 当前四面 Blend List 模式 | 第 11 章 Timeline 模式 |
|---|---|---|
| 模式入口 | Inspection Camera 已绑定 | Inspection Camera 清空并补齐 Director 等引用 |
| 镜头顺序 | Instructions 四行 | Cinemachine Track 的 Shot 片段 |
| 节奏 | Hold、Blend、Loop | 片段开始、长度和重叠 |
| 按钮 | 开始、停止、从正面重播 | 开始、暂停、继续 |
| 结束行为 | 循环或停在最后镜头，手动停止 | 固定序列结束后 stopped 收尾 |
| 附加内容 | 四个固定视角 | 探针动画、检查灯、Signal 等轨道 |

进入第 11 章前请另存场景副本，在副本中清空 Inspection Camera，补齐 Inspection Director、Playable Asset、Inspection Probe 和 Inspection Highlight。当前场景的探针或检查灯可能已移除，不能仅清空一个引用就断言 Timeline 一定可运行。原“巡检”组件保持禁用，避免手工启用后与 Timeline 抢占镜头。

### 10.12 课堂验收与排错练习

建议讲师先演示，学员再根据以下步骤逐项验收。不要同时更改镜头坐标、顺序、时间和代码，先只改一个因素。

1. 退出 Play，确认四个子镜头是直接子节点，Instructions 的四行均不为空。
2. 进入 Play，确认没有自动开始；风扇开关可以独立启停。
3. 点击开始，观察正面、右面、后面、左面的顺序，按钮变为停止巡检。
4. 保持当前 Loop 开启，观察左面结束后返回正面；理解当前 Cut 回环产生的直接切换。
5. 点击停止，确认不再继续轮换，按钮恢复开始；再次开始应从正面重播。
6. 退出 Play 后关闭 Loop，再运行到左面，确认它保持最后视角而不是自动停止。
7. 修改右面的 Hold 为 2 秒，观察进入混合后额外停留；再把后面的 Blend 时间改为 1 秒，观察只有进入后面的过渡变化。完成后恢复授课值。

| 问题现象 | 检查顺序 |
|---|---|
| 按钮显示巡检待配置 | Camera Brain、Inspection Camera、Instructions 是否为空 |
| 点击没有执行 | On Click 目标是否为场景对象、方法是否正确、是否有 UI 遮挡 |
| 点一下马上停止 | 是否重复绑定，是否另有 AddListener 再调用一次 |
| 进入 Play 已经轮换 | 是否正确引用管理器，是否另有脚本或自动播放 Timeline 接管 |
| 只显示一个方向 | Instructions 是否填齐，子镜头是否直接隶属巡检，Hold/Blend 是否异常 |
| 循环接头突变 | 第一条是否 Cut；要平滑回环需调整第一条 Blend |
| 点击开始后镜头不接管 | GameObject 及父级是否活动、Brain 与 Camera 是否启用、其他顶层 Priority 是否更高 |
| 移动场景对象后画面偏了 | 是否更改了父节点的位移、旋转或局部坐标 |
| 到左面仍显示播放中 | Loop 关闭时最后镜头保持，脚本没有自动完成计时器 |
| 停止后仍显示上一个子镜头名 | LiveChild 可能保留最后一次选择；以待机状态和实际是否继续轮换判断停止 |

练习一：交换右面与后面两行。答案是在 Instructions 重排或更换两行引用；只拖动 Hierarchy 的显示顺序不等于修改序列。

练习二：新增俯视镜头。答案是创建“巡检”的直接子 Virtual Camera，摆好构图，再向 Instructions 增加一行并设置 Hold/Blend；不用增加第三个 uGUI 按钮，也不需要新增 Camera 组件。

练习三：解释为何停止时恢复原 Priority 并清除旧状态。参考回答：原 Priority 用于释放本次临时接管，PreviousStateIsValid=false 配合重新启用让下一次从第一条重播，而非沿用上次镜头状态。

本章依据工程实际脚本、已保存场景与 Cinemachine 2.9.7 包源码编写。组件概念可查 [Cinemachine Blend List Camera 官方说明](https://docs.unity3d.com/Packages/com.unity.cinemachine@2.9/manual/CinemachineBlendListCamera.html)，uGUI 事件入口可查 [Unity Button 官方说明](https://docs.unity3d.com/2022.3/Documentation/Manual/script-Button.html)。修改本章不改变场景、镜头或脚本行为。


## 11 Timeline 编排十二秒巡检

本章是对照方案，不是当前四面按钮的默认路径。请另存练习副本，清空 Inspection Camera，补齐已移除的探针、检查灯及其 Director 轨道绑定，再按本章操作；“巡检”Blend List 组件保持禁用。只有这个模式使用十二秒时长、暂停/继续和 Signal 节点。

### 11.1 创建 Timeline 和 Director

1. 选中挂有 FanTrainingController 的“脚本控制”对象（原名 Fan Lesson Controller），添加 Playable Director，关闭 Play On Awake。
2. 打开 Window → Sequencing → Timeline，选中该对象，点击 Create，保存 FanInspectionPractice.playable。
3. 理解 Timeline Asset 保存轨道和片段，Playable Director 保存场景绑定与播放状态。只复制 .playable 文件不能替代场景中的引用绑定。
4. 将序列总时长设为 12 秒。练习时可使用 Fixed Length；本项目 Wrap Mode 使用 None，结束后释放控制。

### 11.2 Cinemachine Track

在 Timeline 添加 Cinemachine Track，把 Main Camera 上的 Cinemachine Brain 绑定到轨道。拖入三个 Virtual Camera，创建或设置对应 Cinemachine Shot。

| Shot | 开始 | 时长 | 讲解现象 |
|---|---|---|---|
| 整体外观 | 0 秒 | 4 秒 | 先看完整风扇模型 |
| 扇叶特写 | 3 秒 | 5 秒 | 看旋转扇叶和电机 |
| 跟随巡检探针 | 7 秒 | 5 秒 | 镜头随探针移动并看向风扇 |

第一、二段在 3～4 秒重叠，第二、三段在 7～8 秒重叠，形成一秒镜头混合。这里的混合由 Timeline 片段重叠表达；Brain.Default Blend 用于普通镜头切换，不应把二者混为同一个设置。

### 11.3 Animation Track

创建 InspectionProbe 作为可移动巡检点，挂 Animator，但不给它状态机 Controller。制作 FanInspectionProbe.anim，记录探针局部 X 从 -2.5 到 2.5，持续 12 秒，Y 和 Z 保持原位。

添加 Animation Track，绑定探针的 Animator，拖入该动画，覆盖 0～12 秒。不要绑定风扇的 Rotor：它已经由 Fan.controller 控制；本课用不同对象演示两种动画驱动，避免重复控制扇叶。

预览时观察探针移动，第三段镜头的 Follow 目标会随之变化。若动画使对象突然回到原点，检查动画中的局部坐标、轨道绑定、Track Offset 和对象父级。

### 11.4 Activation Track

创建 InspectionHighlight 检查灯，初始隐藏。添加 Activation Track，绑定该 GameObject，在 2～10 秒放激活片段，Post Playback State 设 Inactive。

激活轨道用于控制对象是否显示，不会自动调用开关脚本。停止后应隐藏；如果停止还亮着，先检查 Post Playback State，再检查脚本是否另外把它激活。

### 11.5 Signal Track

1. 在“脚本控制”对象上添加 Signal Receiver。
2. 新增 Signal Track，并绑定这个 Receiver。
3. 在 5.5 秒创建 Signal Emitter，创建 FanBladeChecked.signal；在 Receiver 中为该信号添加 Reaction。
4. Reaction 绑定 FanTrainingController.OnInspectionCheckpoint。
5. 在 9.5 秒创建 FanHousingChecked.signal，绑定同一个方法。
6. 本例使用 Emit Once，Retroactive 关闭。正常从 0 播放应到达两个节点，状态文字最终显示 2/2。

Signal 是时间节点通知，真正的业务处理在脚本。它不是 HTTP 请求，也不会自动代表设备检查已经通过。手动拖动预览时间不等同于从头连续播放；验收节点次数应使用正常播放。

### 11.6 巡检按钮与结束复位

巡检按钮第一次调用 Play；播放中点击 Pause；暂停后点击 Resume。自然结束触发 stopped 事件，脚本把 IsInspecting 设 false，复位探针并关闭检查灯。Esc 调用 Stop 走同一收尾流程。

验收：暂停两秒时进度不增加，继续从原时间播放；风扇运行状态不受暂停影响；结束后可以再次巡检，而且节点计数重新从零开始。

## 12 主讲案例综合练习与答案

### 12.1 风扇速度减半

题目：只修改一处配置，让风扇每两秒转一圈，按钮仍能启停。答案：在 Fan.controller 的 Running 状态把 Speed 设 0.5，保持原片段时长与脚本不变。观察至少三圈，停机后能再次启动。

### 12.2 扩展四面镜头与时间轴

当前四面方案新增俯视镜头：创建“巡检”的直接子 Virtual Camera，摆好视角，在 Instructions 增加第五行并设置 Hold/Blend，不用新增按钮。

Timeline 对照作业：在副本的 Cinemachine Track 增加一个电机背面 Shot，调整相邻片段和总时长；脚本 Timeline 显示仍固定为 12 秒，延长序列时应同步显示或改读 director.duration。两种方案的顺序编辑入口不同。

### 12.3 添加启动按钮音效

题目：在 ToggleFan 中播放一次启动/停机提示音。答案：添加 AudioSource 和已授权音频引用，状态切换后调用 PlayOneShot；不要在 Update 每帧调用。使用新资源时保留授权来源。此题为扩展，不要求下载第三方音频。

### 12.4 排查按钮失效

题目：运行时点击按钮没有反应。答案按顺序检查 EventSystem、GraphicRaycaster、Button.Interactable、遮挡 UI 的 Raycast Target、On Click 目标、方法名及脚本字段引用。最后检查 Console；不要一上来重新安装 Cinemachine。

### 12.5 解释四项技术的边界

参考回答：Animation Clip 记录属性随时间如何变化；Animator 根据 Running 参数选择动画状态；Cinemachine 定义镜头构图、跟随和混合；Timeline 把镜头、探针动画、激活与信号按时间编排。uGUI 负责输入，C# 脚本连接按钮与这些组件。

## 13 C# 基础与设备交互扩展

在主讲脚本中讲解 bool、取反、方法调用、public 引用、属性、if/else 和事件订阅。Awake 记录初始位置，Start 设置初始参数，Update 展示时间，OnEnable/OnDisable 成对订阅和取消订阅。不要把所有初始化都放入每帧 Update。

转到 01_MachineRoom，按 DeviceController → DevicePicker → TrainingDashboard 的顺序阅读。左键用 Camera.ScreenPointToRay 生成射线，Physics.Raycast 找 Collider，通过 GetComponentInParent 找到设备，再把设备交给 UI。EventSystem.IsPointerOverGameObject 阻止点 UI 时误选后方设备。

DeviceController 用稳定 deviceId 关联场景、接口和记录；displayName 只负责显示。ApplyCommand 修改本地状态，ApplyState 应用接口响应，RefreshVisuals 同步 Animator 参数和指示灯。名称改成中文不应影响 ID。

练习：在 DescribeStatus 中优先返回告警，其次返回运行或停止。参考答案见原配套“练习与参考答案”文档；不要在多个地方复制一套互不一致的状态规则。

## 14 多点巡检与手动漫游扩展

当前风扇场景使用 Blend List Camera 按正面、右面、后面、左面循环巡检；Timeline 作为固定时间轴演出的对照练习。机房巡检则按点位和检查结果推进业务流程。学员应能区分镜头序列、时间轴演出与可交互业务状态机的用途。

机房使用 InspectionPoint 数组和 PatrolController。状态为 Manual、Moving、AtPoint、Paused、Completed。到点后选中设备，记录正常或异常，再前往下一点；记录按设备覆盖。暂停记住之前状态，恢复后不能把原先等待检查误变为移动。

手动模式用 WASD、按住右键、Q/E 升降。输入框获得焦点时不移动摄像机；自动模式停止手动写 Transform。重置恢复摄像机初始位置并清空本轮结果，提交期间阻止重置。

练习：新增一个点、绑定设备并修改顺序。验收：保存重开仍保留，暂停时不移动，切手动后不被自动拉回。

## 15 HTTP 请求和本地服务

### 15.1 启动与首次查询

1. 安装 Python 3.10+，命令行运行 python --version 确认。服务只使用标准库。
2. 双击 Server/启动服务.cmd，或在项目根目录执行 python Server/server.py。
3. 浏览器访问 http://127.0.0.1:8080/api/devices，应返回四台设备 JSON。
4. 在机房场景切换 HTTP 模式，点击同步设备，再执行启停和告警。
5. 风扇主讲场景是纯本地的 Animation/Cinemachine/Timeline 示例，不依赖这个服务。

| 方法和路径 | 请求 | 成功响应 |
|---|---|---|
| GET /api/devices | 无请求体 | devices 数组 |
| POST 设备控制接口 | command 字符串 | device 状态 |
| POST /api/inspections | batchId 和 records | ok、batchId、count |

设备控制接口完整路径为 POST /api/devices/{id}/commands，其中 {id} 替换为设备 ID。

命令为 start、stop、open、close、alarm、clearAlarm。四台设备 ID 为 cabinet-01、fan-01、pump-01、control-01。

### 15.2 UnityWebRequest 的讲解顺序

阅读 TrainingApi.Send：构造地址和方法，设置超时，POST 时把 UTF-8 JSON 放进 UploadHandlerRaw，使用 DownloadHandlerBuffer 接收结果，yield return 等待，再判断 request.result，最后通过回调返回数据与错误。using 在完成后释放请求。

JsonUtility 通过可序列化类解析对象，因此 GET 返回外层 devices 包装，而非只返回裸数组。解析成功仍需校验 ID、必需字段及提交确认；HTTP 200 不自动表示业务已成功。

### 15.3 提交和失败处理

完成检查后提交，服务端把批次写入 Server/data/inspections.jsonl。正常响应需确认 ok、batchId 和 count，界面才显示成功。相同 batchId 重试去重，修改检查记录会产生新批次。

课堂先停止服务再提交，观察连接失败、记录保留；重启服务后重试。再测试未知设备返回 404、错误 JSON 返回 400、无效响应和请求超时。不要在错误时自动伪装成本地成功。具体 JSON 示例见“接口与架构”。

## 16 Editor 脚本编写

运行时代码放 Scripts，UnityEditor 代码放 Editor 或 Editor-only asmdef。使用 UnityEditor 的脚本不能进入 Windows Player。

### 16.1 风扇场景的 Inspector 与教学窗口

选中挂有 FanTrainingController 的“脚本控制”对象（原名 Fan Lesson Controller），可见 FanTrainingInspector 的引用检查与播放态调试按钮。打开“培训 → 风扇教学工具”，点“查找当前场景”，再点“定位扇叶转轴”。在非播放状态调节 Running 状态的播放速度，Ctrl+Z 撤销，点击“保存资源”写入 Fan.controller。该窗口使用 Undo.RecordObject 和 EditorUtility.SetDirty，修改会影响所有共用 Controller 的实例。

练习：把速度设为 0.5，撤销恢复 1，再改成 0.5 并保存；重开项目确认保持，最后恢复 1。暂时清空控制器上的 Camera Brain 引用，Inspector 应提示错误，重新拖入 Main Camera 上的 Cinemachine Brain 后消失。四面模式允许 Director 为空，不能用清空 Director 检验该模式的缺失引用提示。不要把测试用的空引用保存为最终场景。

阅读 FanTrainingInspector.cs 中的 CustomEditor、EditorWindow 和 Check 方法，比较运行时按钮和编辑器工具的入口。再阅读机房扩展的 DeviceInspector：CustomEditor 指定 DeviceController，OnInspectorGUI 先绘制字段，再给出 ID 提示，只有播放时允许调试按钮。使用 SerializedObject/SerializedProperty 编辑字段可接入 Unity 的序列化和撤销；不要仅把画面做出来却忽略保存。

练习：在风扇脚本的 Inspector 提示三个必需引用是否为空，但不要在编辑态点击按钮就偷偷启动时间轴。扩展按钮需明确是否只在 Play 中可用。

### 16.2 路线 EditorWindow 与 Handles

机房菜单“培训 → 巡检路线编辑器”展示 EditorWindow、对象引用和有序点数组。Scene Handles 移动前先 Undo.RecordObject，改位置后 MarkSceneDirty。新建对象使用 Undo.RegisterCreatedObjectUndo。保存场景后重新打开，确认修改仍在。

练习：添加点、拖动手柄、Ctrl+Z 撤销、恢复后保存。仅看到 Gizmos 不代表配置已保存。

### 16.3 配置检查

“培训 → 配置检查”针对机房设备与路线，检测重复 ID、空引用、空路线和丢失脚本，并定位对象。风扇主讲场景使用独立组件，没有机房路线；不要把“没有 PatrolController”误认为主讲场景损坏。

练习：增加设备显示名称不能为空的规则，再人为制造问题并定位。批处理测试中的 FanTrainingTests 验证风扇场景自身引用、按钮、Animator 和 Timeline。

## 17 测试 构建与课堂验收

1. 编辑器 Window → General → Test Runner 执行 EditMode 测试。批处理需要先关闭同一项目的交互编辑器，避免两个进程同时打开工程。
2. 在项目根目录运行 python -m unittest discover -s Server -v 验证 HTTP 服务。
3. 使用“培训 → 构建 Windows 演示”，或关闭该项目编辑器后运行 Tools/Verify.ps1 -Task Build。
4. 构建输出为 Builds/Windows/UnityTrain.exe。分发时复制整个 Windows 文件夹，不能只复制 exe。
5. 构建后运行 python Tools/run_smoke.py，验证实际程序。自动化脚本会启动隔离接口服务，不占用日常教学的固定端口。

| 考核项 | 通过标准 |
|---|---|
| 环境安装 | 能解释内置窗口与可下载包，能在 Package Manager 确认版本 |
| 风扇模型 | 转轴正确，扇叶动、护圈和支架不动 |
| Animation | 能制作线性循环、解释关键帧与 Loop Time |
| Animator | 能独立建立两条条件过渡并用参数触发 |
| uGUI 和脚本 | 两个按钮各只绑定一次；四面模式开始/停止/重播，Timeline 模式暂停/继续 |
| Cinemachine | 能建立四个直接子镜头，解释 Instructions、Hold、Blend、Loop 与 Brain |
| Timeline | 能绑定四类轨道，解释镜头重叠、事件与结束复位 |
| HTTP | 能成功查询提交，并演示失败保留和重试 |
| Editor | 修改可撤销、可保存，校验能定位错误 |

综合作业：在不破坏原场景的副本中修改风扇转速，增加一个巡检镜头和一个 Signal，解释每项改动发生在哪个资源或脚本；再完成一次机房 HTTP 提交和一次 Editor 撤销练习。

## 18 常见问题速查

| 现象 | 优先检查 |
|---|---|
| 搜不到 Cinemachine | Package Manager 是否选 Unity Registry，是否用了 Add package by name |
| 菜单与教程不同 | 是否误装 Cinemachine 3.x，本课固定 2.9.7 |
| 找不到 Animation 下载项 | 它是编辑器内置窗口，无需下载 |
| 扇叶不转 | Animator.Controller、Running 参数、状态 Motion、Loop Time、Console |
| 整个风扇转 | Clip 是否误绑定 TrainingFan 根节点而非 Rotor |
| 每圈速度忽快忽慢 | Rotation 曲线是否 Linear |
| 一个按钮点击后状态没变化 | 是否 Inspector 和 AddListener 重复绑定 |
| 镜头抖动或被拉回 | 是否有输入脚本和 Cinemachine Brain 同时改 Main Camera |
| 改 Priority 不生效 | “巡检”组件是否以临时高 Priority 接管，或 Timeline 是否仍在接管镜头 |
| Timeline 窗口空白 | 是否选择了带 Director 的对象，Playable 是否已赋值 |
| 轨道显示 None | 绑定到场景对象，而不只是 Project 中的资产 |
| Timeline 暂停后不能继续 | 是否用 Resume 而非从零 Play，业务 IsInspecting 是否保留；四面模式使用停止和重播，不提供暂停继续 |
| 检查灯结束后仍亮 | Activation Post Playback State 和停止回调 |
| HTTP 无法连接 | 服务是否启动、端口和地址是否一致；127.0.0.1 是当前机器 |
| 中文方块或文字消失 | Font 是否赋值、文字高度是否容纳字体行高 |

## 19 官方资料与版本依据

以下入口用于讲师核对概念与界面，课程中的资源名和参数值以本工程为准。课程内容依据已安装包源码及 Unity 官方说明编写，未依赖第三方视频中的版本差异。

- [Unity Package Manager 安装包](https://docs.unity3d.com/2022.3/Documentation/Manual/upm-ui-install.html)
- [Animation 窗口](https://docs.unity3d.com/2022.3/Documentation/Manual/AnimationEditorGuide.html)
- [Animator Controller](https://docs.unity3d.com/2022.3/Documentation/Manual/AnimatorControllers.html)
- [Cinemachine 2.9 虚拟镜头](https://docs.unity3d.com/Packages/com.unity.cinemachine@2.9/manual/CinemachineVirtualCamera.html)
- [Cinemachine Blend List Camera](https://docs.unity3d.com/Packages/com.unity.cinemachine@2.9/manual/CinemachineBlendListCamera.html)
- [uGUI Button](https://docs.unity3d.com/2022.3/Documentation/Manual/script-Button.html)
- [Cinemachine 与 Timeline](https://docs.unity3d.com/Packages/com.unity.cinemachine@2.9/manual/CinemachineTimeline.html)
- [Timeline 1.6 系列](https://docs.unity3d.com/Packages/com.unity.timeline@1.6/manual/index.html)
- [Unity 中国官网](https://unity.cn/)，从官方 Hub 与历史版本入口确认 2022.3.55f1c1，不把跳转到其他引擎的下载页当作本课版本。

## 20 配套文档与维护

“课程指南”提供五天速览，“练习与参考答案”保留机房扩展题，“接口与架构”给出 JSON 和脚本阅读顺序，“第三方资源”记录字体来源与许可。主讲脚本和资源路径的变化应同步更新本手册。

本课不要求导入收费模型或动画。风扇护圈网格和其他部件由工程生成；字体为随包 Noto Sans CJK SC。安装 Unity 包需要联网，安装完成后的风扇本地演示不依赖 HTTP 服务。
