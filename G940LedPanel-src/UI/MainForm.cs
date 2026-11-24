using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Windows.Forms;
using G940LedPanel.Core;
using G940LedPanel.Effects;

namespace G940LedPanel.UI
{
    /// <summary>
    /// v3.0 主界面逻辑（控件创建/布局/事件订阅见 MainForm.Designer.cs）：
    /// 顶部 = 默认效果（左）+ 全部效果总开关（右），设置即生效；
    /// 中栏 = LED 方形预览（左，两排 4+4 与真实按钮一一对应）｜设备连接（中，自动连 G940 油门+失败原因）｜SimConnect（右）；
    /// 下方 = 简单模式 / SimConnect 模式 Tab（切换立即生效），两种模式各自独立的一套配置。
    /// </summary>
    public partial class MainForm : Form
    {
        /// <summary>
        /// 8 颗预览灯。刻意不在设计器里声明：它们是运行时按 4×2 网格生成的，
        /// 放进 Designer 会让"设计器保存"和手写数组打架。
        /// </summary>
        private LedIndicator[] _leds;

        // ---- LED 预览按压模拟：点击预览灯后保持绿色，不被引擎帧覆盖 ----
        private readonly bool[] _previewHeld = new bool[8];
        private readonly bool[] _virtualButtons = new bool[128]; // 点击预览灯模拟按下的油门按钮状态
        /// <summary>简单模式表格中“内容重复”的行号 → 该行所属重复组的标记颜色。</summary>
        private readonly Dictionary<int, Color> _dupRows = new Dictionary<int, Color>();

        /// <summary>状态条重复提示文本（简单模式）。</summary>
        private string _simpleDupText = "";

        // ---- 核心对象 ----
        private AppConfig _cfg;

        /// <summary>当前告警配置（alerts\ 里的某个文件，或内置的某一套）。</summary>
        private AlertProfile _alert;

        /// <summary>当前按键配置（buttons\ 里的某个文件，或内置的某一套）。</summary>
        private ButtonProfile _buttons;

        private DeviceSession _session;
        private EffectEngine _engine;
        private Timer _pollTimer;
        /// <summary>插回设备后延迟重连用的一次性定时器（见 ScheduleReconnect）。</summary>
        private Timer _reconnectTimer;
        private readonly RawInputReader _rawInput = new RawInputReader();
        private readonly HidDeviceNotify _devNotify = new HidDeviceNotify();
        private bool[] _prevButtons = new bool[128];

        /// <summary>用户主动断开过设备：不再自动连回来（否则按钮等于没用）。</summary>
        private bool _userDisconnected;

        // 设备状态词的"输入"：SetDevStatus 收到的就是这两个值。
        // 切语言时要照原样再算一遍——状态词和悬停气泡都是这两个值的投影，
        // 不留下来就只能等下一次状态变化才换语言（1.3.63 截图就是这么来的）。
        private DevState _devState = DevState.Disconnected;
        private Color _devColor = SystemColors.ControlText;
        private bool _capturing;
        private Timer _captureTimer;
        private bool _suppress;
        /// <summary>界面初始化完成、允许勾选框动作写配置。之前都是程序在设初始值。</summary>
        private bool _sceneLogReady;
        private readonly Dictionary<ButtonBinding, Overlay> _bindingOverlays =
            new Dictionary<ButtonBinding, Overlay>();
        private int _activeMode; // 0=简单模式（按钮驱动），1=SimConnect 模式（状态条件驱动）
        private string _pollError = "";

        // ---- SimConnect（MSFS2024 数据读取）----
        private SimConnectSource _sim;
        private FlightState _simState;                       // 最近一帧模拟器状态
        private int _lastFrameAt;                            // 最近一帧到达时刻（判数据是否还在刷新）
        private SimStatus _simStatus;                        // 最近一次连接状态（诊断用）
        private readonly List<Overlay> _alertOverlays = new List<Overlay>();
        private string _alertHitKey = "";                      // 命中指纹，未变化时不重建叠加（避免闪烁重启）
        private DataSourcePhase _lastPhase = DataSourcePhase.Unknown;  // 上次的就绪状态（变化时要整体刷新）
        private string _simDiagText = "";                    // “SDK 检测”结果

        // ---- 告警页（P2）----
        /// <summary>表格行 → 规则，渲染时固定顺序，与 AlertRuleCatalog.Rules 一一对应。</summary>
        private readonly List<AlertRule> _alertRows = new List<AlertRule>();
        /// <summary>告警表的滚动/选中是否已还原（切到告警页时才真正设得进去）。</summary>
        private bool _simViewRestored;

        // ---- 系统托盘 ----
        private NotifyIcon _tray;
        private bool _quitFromTray;
        private bool _trayBalloonShown;
        // 托盘菜单不是在 Designer 里建的，靠这两个字段才能在切语言时改回文字
        private ToolStripMenuItem _miTrayShow;
        private ToolStripMenuItem _miTrayExit;

        // 显示名一律走 TextNames 现取（1.3.53）：静态数组会缓存初始语言，切语言后不跟着变。
        // 判据、索引、写入都只认 id / 序号，名字只用于显示与「从表格里读回来」的那一刻。
        private static readonly double[] SpeedSteps = AlertRuleCatalog.SpeedSteps;

        // ================= 界面文字配色：全项目只有四色 =================
        //
        // 一、正文与说明文字 → 系统默认色，一律不设 ForeColor。
        // 二、需要提醒时才有颜色，就绿/橙/红这三支，橙红加粗（见 BoldFont）。
        //
        // 三支的色值定义在 UiColors（全项目唯一来源），这里只是短名，方便调用处写。
        // 不要拿 LedPalette 的灯色来当字色——#3FE07B 在白底上只有 1.5:1，
        // 加粗也补不回来。灯要亮、字要深，这是两种材质，各用各的。
        private static readonly Color C_OkInk = UiColors.Ok;      // 绿 #1F7A46 · 正常、已连接  4.7:1
        private static readonly Color C_WarnInk = UiColors.Warn;  // 橙 #9A5B00 · 提醒、进行中  4.8:1
        private static readonly Color C_ErrInk = UiColors.Err;    // 红 #C02020 · 错误、被拦下  5.3:1

        private static readonly Color C_RowA = Color.FromArgb(255, 255, 255); // 告警表交替底色 A（背景，不计入四色）
        private static readonly Color C_RowB = Color.FromArgb(246, 248, 252); // 告警表交替底色 B（背景，不计入四色）
        private static readonly Color C_CustomMark = Color.FromArgb(214, 232, 255);  // 用户新增规则：行头色块（背景，不计入四色）

        public MainForm()
        {
            _cfg = AppConfig.Load();   // 先建配置，保证任何初始化期事件回调都不缺 _cfg
            L.Use(L.Resolve(_cfg.Language));   // 语言要在任何文字生成之前定下来（空=跟随系统）
            _suppress = true;          // 方案 A：搭界面（InitializeComponent 设列头等）全程抑制，
                                      // 避免初始化期的程序化变更被当成用户改表
            InitializeComponent();
            FillLangCombo();           // 语言名不翻，但清单来自 L.All，运行时填（1.3.67）
            Text = AppInfo.Title;      // 版本号唯一来源：程序集版本（见 AppInfo）

            // VS 设计器会真的执行构造函数。设备枚举、定时器、托盘图标在 VS 进程里
            // 要么卡住要么弹错，所以设计期到此为止 —— 只摆界面，不碰硬件。
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            InitRuntime();
        }

        // ================= 配置读写 =================

        /// <summary>
        /// 按身份取一套告警配置：先找预置，再找 alerts\ 下的文件。
        /// 找不到 / 读不出来 → 回落第一套预置；连预置都被删光 → 给一份空配置，并把原因挂进 ⚠ 清单。
        /// 注意空配置不是"崩"，是"一套没有名字的出厂默认"：规则表照常可看可改，用户点新建就有一份真的。
        /// </summary>
        private AlertProfile LoadAlertProfile(string key, bool report)
        {
            string why = null;

            if (!string.IsNullOrWhiteSpace(key))
            {
                var builtin = ProfileStore.FindBuiltInAlertByKey(key);
                if (builtin != null) return builtin;

                var entry = ProfileStore.FindByKey(key, true);
                if (entry != null)
                {
                    AlertProfile p;
                    if (ProfileStore.LoadAlert(entry.FilePath, out p, out why))
                    {
                        foreach (var note in p.Sanitize()) AddConfigNote(note);
                        return p;
                    }
                }
                if (why == null) why = L.T("main.prof.notfound", key);
            }

            var presets = AlertProfile.BuiltInProfiles();
            var fallback = presets.Count > 0 ? presets[0] : AlertProfile.CreateDefault();
            fallback.Sanitize();
            if (report && why != null)
                AddConfigNote(presets.Count > 0
                    ? L.T("main.prof.loadfail", why, TextNames.ProfileName(fallback))
                    : L.T("main.prof.loadfail.none", why));
            return fallback;
        }

        /// <summary>同上，按键配置。</summary>
        private ButtonProfile LoadButtonProfile(string key, bool report)
        {
            string why = null;

            if (!string.IsNullOrWhiteSpace(key))
            {
                var builtin = ProfileStore.FindBuiltInButtonByKey(key);
                if (builtin != null) return builtin;

                var entry = ProfileStore.FindByKey(key, false);
                if (entry != null)
                {
                    ButtonProfile p;
                    if (ProfileStore.LoadButton(entry.FilePath, out p, out why))
                    {
                        return p;
                    }
                }
                if (why == null) why = L.T("main.prof.notfound", key);
            }

            var presets = AppConfig.BuiltInButtonProfiles();
            var fallback = presets.Count > 0 ? presets[0] : new ButtonProfile();
            if (report && why != null)
                AddConfigNote(presets.Count > 0
                    ? L.T("main.prof.loadfail.b", why, TextNames.ProfileName(fallback))
                    : L.T("main.prof.loadfail.b.none", why));
            return fallback;
        }

        /// <summary>
        /// 存当前告警配置。内置配置不能覆盖（要改就"另存为"），其余写回自己的文件。
        /// 返回错误说明；成功返回 null。
        ///
        /// 1.3.49 起：内置配置**不再自动复制**。原来一旦改动就悄悄另存成「原名（我的）」，
        /// 结果是"删掉配置 → 保存 → 副本又出现"，用户永远删不干净。现在改动被拦下、
        /// 返回 null（不算失败，因为这是设计行为不是错误），复制由用户从「管理 → 另存为」主动完成。
        /// </summary>
        private string SaveAlertProfile()
        {
            if (_alert == null) return L.T("main.prof.noalert");
            if (_alert.IsBuiltIn) { MarkBuiltInTouched(true); return null; }
            string err = ProfileStore.SaveAlert(_alert);
            if (err != null) { AddConfigNote(err); return err; }
            return null;
        }

        private string SaveButtonProfile()
        {
            if (_buttons == null) return L.T("main.prof.nobutton");
            if (_buttons.IsBuiltIn) { MarkBuiltInTouched(false); return null; }
            string err = ProfileStore.SaveButton(_buttons);
            if (err != null) { AddConfigNote(err); return err; }
            return null;
        }

        /// <summary>
        /// 内置配置上发生了改动：点亮红字，不写盘、不复制。
        ///
        /// 这里刻意不再播一次性提示。那句话和红字内容重复，更要紧的是：它和红字
        /// 写的是同一个标签（告警页的 _lblSimHint2），播出去的瞬间就把红字盖掉，
        /// 用户看到的是"红字闪一下就变成别的提示"。1.3.49 修的就是这个。
        /// 状态位由切换配置时清零（见 <see cref="UpdateBuiltInHints"/>），
        /// 不跟踪"是否改回原样"：那需要快照比对，为一个小概率场景不值得加一层机制。
        /// </summary>
        private void MarkBuiltInTouched(bool alert)
        {
            if (alert) _builtInTouched2 = true; else _builtInTouched1 = true;
            UpdateBuiltInHints();
        }

        // ================= 运行时初始化（设计器无关） =================

        private void InitRuntime()
        {
            // 表格下拉列的候选项统一由 InitComboItems → ReloadComboColumnItems 建（1.3.54）。
            // 这里原来单独填过 colAEffect / colAColor，两处各填一次，多一份就是多一处会忘的地方。

            // 诊断记录开关必须先定下来：数据源一连上就会写记录表头，等不到界面搭完。
            SceneStateLog.Enabled = _cfg.SceneStateLog;

            // 绿色版自检：解压到 Program Files 或只读 U 盘时配置存不下来。
            // 不查这一句的话，用户只会看到"改了不生效"，根本查不出原因。
            string whyNotWritable;
            if (!ProfileStore.CheckWritable(out whyNotWritable))
                AddConfigNote(whyNotWritable);

            // 配置内容与程序状态分开存：config.json 只记"我在用哪套"，内容在 buttons\ / alerts\
            _alert = LoadAlertProfile(_cfg.ActiveAlertFile, true);
            _buttons = LoadButtonProfile(_cfg.ActiveButtonFile, true);

            _session = new DeviceSession();
            _engine = new EffectEngine(_session);

            // SimConnect 数据源（独立后台线程；未连接时只报状态，不影响按钮/LED）
            _sim = new SimConnectSource(this);
            // 相机状态表（Menu / Skip / Flight），改后重启生效。数据源只认这张表，
            // 不关心某一行是默认值还是用户自己填的 —— 那层区别只影响日志怎么写。
            ApplyCameraStates();
            _sim.StatusChanged += OnSimStatus;
            _sim.StateUpdated += OnSimState;

            // 模式 Tab 事件在运行时订阅（避免设计器/初始化期提前触发访问未初始化字段）
            _tabs.SelectedIndexChanged += tabs_SelectedIndexChanged;
            _chkSim.CheckedChanged += (s, e) => ApplySimConnect();

            // 场景状态记录：勾选状态存配置，重启保持。默认关——平时不写盘。
            _chkSceneLog.Checked = _cfg.SceneStateLog;   // 事件里会同步 Enabled 与文案
            UpdateSceneLogUi();
            _sceneLogReady = true;
            // 1.3.43：配置下拉选中即载入（运行时订阅，避免初始化铺数据期间连锁触发）
            _cboPreset1.SelectedIndexChanged += cboPreset1_SelectedIndexChanged;
            _cboPreset2.SelectedIndexChanged += cboPreset2_SelectedIndexChanged;

            // 勾选/下拉一改就提交：否则要先点进编辑态、再点别处才生效，手感像坏了。
            // 两张表都要接——之前只接了告警表，按键表改效果得先点一下空白处。
            _dgv.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (_dgv.IsCurrentCellDirty) _dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            _dgvAlert.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (_dgvAlert.IsCurrentCellDirty) _dgvAlert.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            // 窗口/分页尺寸变了就重排列宽（两张表各算各的）
            _dgv.SizeChanged += (s, e) => AutoFitAllGrids();
            _dgvAlert.SizeChanged += (s, e) => AutoFitAllGrids();

            // 双击表头：只把被双击那一列按内容撑开（1.3.71）。
            // 以前只有「拖」和「窗口尺寸变化时整体重排」两种方式，想看全某列得靠手拖。
            _dgv.ColumnHeaderMouseDoubleClick += (s, e) => FitColumnToContent(_dgv, e.ColumnIndex);
            _dgvAlert.ColumnHeaderMouseDoubleClick += (s, e) => FitColumnToContent(_dgvAlert, e.ColumnIndex);

            // 原始值列 250ms 刷一次：既看得出变化，又不至于整表重绘闪
            _rawTimer = new Timer { Interval = 250 };
            _rawTimer.Tick += (s, e) => RefreshRawValues();
            _rawTimer.Start();

            // 状态兜底：400ms 把"提示 + 灯效"整体重算一遍。
            // 光靠事件驱动一定会漏——一次性动画播完了没人再喊一声、
            // 某条路径忘了调用刷新，就会出现"灯不转圈""提示还是旧的"。
            // 有了这个节拍，任何状态变化最多 400ms 就收敛到一致。
            _hintTimer = new Timer { Interval = 400 };
            _hintTimer.Tick += (s, e) => RefreshStatus();
            _hintTimer.Start();

            InitLedPreview();              // LED 预览灯运行时创建（设计器保存不重写）
            InitComboItems();              // 下拉框/表格列选项（设计器不填充）
            AttachColorSwatch(_dgv, BindColColor, false);       // 颜色列整格填充（简单模式，可编辑）
            AttachColorSwatch(_dgvAlert, AlertColColor, true);  // 告警页颜色列只读，仍整格填色
            SyncUiFromConfig();
            ReloadTexts();                 // 1.3.50：Designer 里那份中文是"设计时预览值"，运行时以语言包为准
            SyncLangCombo();               // 1.3.52：语言下拉拨到当前语言（空配置时是跟随系统的结果）

            // 两页的常驻说明文字统一橙色加粗：红是唯一强信号，留给真正出错/被拦下的情况。
            // 会随状态变色的页脚标签（_lblPresetInfo1 / _lblSimHint2 / _lblAlertCounts）
            // 不在这么设，由各自的刷新函数下发，免得每次刷新又把颜色抹回默认。
            HintText.Set(_lblBgHint, C_WarnInk);   // 按键页：底灯那一行的说明
            HintText.Set(_lblCapture, C_WarnInk);  // 按键页页脚：操作提示与动作反馈

            // 告警页图例不走 HintText：整条橙色加粗会把三色盖掉，也把一行小字堆成噪音。
            // 这一处是「常驻说明必须橙+加粗」的唯一例外——它解释的就是颜色，必须显示真色，
            // 所以三个色点各自绑定 UiColors 的绿/琥珀/红（DocLint 按这条判据检查）。
            _lblLgOk.ForeColor = UiColors.Ok;
            _lblLgWarn.ForeColor = UiColors.Warn;
            _lblLgErr.ForeColor = UiColors.Err;

            // 停在上次那一页：调告警规则时不用每次开机都手点一下。
            // 必须显式调用 SwitchMode，不能靠"设 SelectedIndex 触发事件"：
            // 构造函数里窗体句柄还没创建，TabControl 的 SelectedIndexChanged 不会触发，
            // 结果页签停到了告警页、_activeMode 却还停在 0（底灯不清、开机播按键模式的提示圈）。
            SwitchMode(_cfg.LastMode);

            // 窗口位置、表格滚动位置、上次选中的规则都还原
            RestoreGridState();
            RestoreWindowBounds();

            // 载入配置时的体检结论（迁移、坏条件被回落、无效规则被丢弃）提示一次；
            // 明细已经进了 ConfigNotes，告警页坏条件的那些行会标橙，这里只做一次"提一句"。
            if (_cfg.ConfigNotes != null && _cfg.ConfigNotes.Count > 0)
                FlashNote(L.T("main.tip.confignotes", _cfg.ConfigNotes[0])
                        + (_cfg.ConfigNotes.Count > 1 ? L.T("main.tip.confignotes.more", _cfg.ConfigNotes.Count - 1) : ""));

            // 设置卡片底部那行的初始文案与气泡（之后由保存动作驱动更新）
            UpdateSaveState();

            // 两页页脚右侧的初始状态：开机用的若是内置配置，先亮灰字说明"改了也存不下"，
            // 免得用户改了半天才发现白改。
            UpdateBuiltInHints();

            InitTray();

            _pollTimer = new Timer { Interval = 15 };
            _pollTimer.Tick += (s, e) => PollTick();
            _engine.FrameRendered += f => UpdateIndicators(f);

            // 原始输入通道（按钮读取兜底，绕过 DirectInput 格式映射）
            try { _rawInput.Register(Handle); } catch (Exception) { }

            // 设备变更通知：插拔油门时叫醒灯通道，让它自己重新枚举。
            // 不注册的话，画面静止时程序根本不知道设备已经拔掉了。
            // 插入与拔出要分开处理：拔出除了叫醒灯，还得把输入通道真的释放掉并改界面状态，
            // 否则 Connected 一直为 true，卡片会一直显示"已连接"、按钮一直是"断开"。
            _devNotify.Arrived += OnTargetDeviceArrived;
            _devNotify.Removed += OnTargetDeviceRemoved;
            try { _devNotify.Register(Handle); } catch (Exception) { }

            // 启动即自动连接 G940 油门装置。interactive=false：认不出只写状态词，
            // 不弹框——拷到没设备的电脑上打开程序不该被弹窗拦住。
            AutoConnectThrottle(false);

            // Designer 里这几个预览值是中文（● 正在检测… / 断开 / 当前按下: - / 当前模式：…），
            // 自动连接只覆盖到其中一部分，英文界面下剩下的会原样露出来。
            // 这里按当前语言 + 当前状态补齐；不跑整个 RefreshRuntimeTexts，
            // 是因为它包着 RefreshStatus，会重算灯效，刚起的开机动画可能被打断。
            SetDevStatus(_devState, _devColor);
            UpdateLinkTexts();
            UpdateModeLabel();
            UpdateSimDataLabel();
        }

        /// <summary>
        /// 运行时创建 LED 预览灯（4×2 网格 + 点击模拟按下）。
        /// 放在运行时而非 InitializeComponent：VS 设计器保存时会重写 InitializeComponent，
        /// 手写的灯数组/事件订阅会被破坏，导致"拖动 panel 后预览失效"。
        /// </summary>
        private void InitLedPreview()
        {
            if (_ledCard == null) return;

            // 卡片内容用一行 TableLayoutPanel 承载（1.3.71：原第二行那句中英操作说明已删除，
            // 灯是手型光标，按住即按钮，一试即知，那句属于重复解释）。
            var host = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 1,
                Padding = new Padding(8, 6, 8, 2)
            };
            host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            host.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 2,
                Margin = new Padding(0)
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            _leds = new LedIndicator[8];
            for (int i = 0; i < 8; i++)
            {
                var led = new LedIndicator
                {
                    Caption = "P" + (i + 1),
                    ShowLabel = _cfg == null || _cfg.ShowLedLabel,
                    LedIndex = i,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(4),
                    Tag = i
                };
                int idx = i; // 闭包捕获
                // 1.3.43 两道隔离，别和按键模式搞混：
                //   按键模式（mode 0）：按住 = 持续模拟按下，松开 = 松开，行为完全不变；
                //   告警模式（mode 1）：不再模拟按下，点一下就弹这颗灯的系统配置框。
                // 事件类型也分开：配置框走 MouseUp，不占 MouseDown。
                led.MouseDown += (s, e) =>
                {
                    if (e.Button != MouseButtons.Left) return;
                    if (_activeMode == 0) SetPreviewPress(idx, true);
                };
                led.MouseUp += (s, e) =>
                {
                    if (e.Button != MouseButtons.Left) return;
                    if (_activeMode == 0) SetPreviewPress(idx, false);
                    else EditLedAssign(idx);
                };
                led.MouseCaptureChanged += (s, e) =>
                {
                    // 鼠标在控件外松开时兜底复位（只在按键模式有意义）
                    if (led.Capture) return;
                    if (_activeMode == 0) SetPreviewPress(idx, false);
                };
                grid.Controls.Add(led, i % 4, i / 4);
                _leds[i] = led;
            }

            host.Controls.Add(grid, 0, 0);
            _ledCard.Controls.Add(host);

            // 灯上文字按当前模式铺一遍（按键模式 P1..P8），顺带把悬停提示挂上
            UpdateLedLabels();
        }

        /// <summary>
        /// 每颗灯当前命中的告警规则（仅告警模式）。灯上写不下规则名，靠悬停提示补。
        ///
        /// 存规则对象而不是拼好的文案：表格行号必须渲染时现算 —— _alertRows 里还有
        /// 自定义规则，且按「系统→优先级」重排过，和 AlertRuleCatalog.Rules 的下标不是一回事，
        /// 存字符串会留着加规则之前的旧行号。
        /// </summary>
        private readonly AlertRule[] _ledRules = new AlertRule[8];

        /// <summary>
        /// 数据源显示名。现在只有 MSFS2024；将来接 DCS 时改这一处，
        /// Tab 文案和状态条就跟着变，不用再翻一遍界面文字。
        /// </summary>
        private string DataSourceName
        {
            get { return "MSFS2024"; }
        }

        /// <summary>刷新灯上文字：按键模式 P1..P8，告警模式换成灯位映射挂上来的系统名。</summary>
        private void UpdateLedLabels()
        {
            if (_leds == null) return;
            bool show = _cfg == null || _cfg.ShowLedLabel;
            for (int i = 0; i < 8; i++)
            {
                if (_leds[i] == null) continue;
                _leds[i].Caption = _activeMode == 0 ? "P" + (i + 1) : SystemNameOfLed(i);
                _leds[i].ShowLabel = show;
            }
            UpdateLedHints();
            // 页签文案不在这里改：它只随语言变（见 ReloadTexts → UpdateModeTabTexts）。
            // 原先顺手调了一下，结果是切语言后被这行中文顶回去——两处写同一段文字必踩。
        }

        /// <summary>
        /// 这颗灯挂的系统名；没挂系统（或映射异常）返回空串。
        /// 系统数扩到 10 后允许「某颗灯不挂系统」，此时不能把兜底文字当成系统名往外拼，
        /// 否则气泡抬头会出现 "P3 · P3"。
        /// </summary>
        private string SystemLabelOfLed(int led)
        {
            if (_alert == null) return "";
            int sys = SystemLedMap.SysOf(_alert.SystemLedMap, led);
            if (sys < 0 || sys >= AlertRuleCatalog.Systems.Length) return "";
            return AlertRuleCatalog.SystemName(AlertRuleCatalog.Systems[sys].Key);
        }

        /// <summary>灯体文字：告警模式显示系统名，取不到就退回 Pn（按键模式恒 P1..P8）。</summary>
        private string SystemNameOfLed(int led)
        {
            string name = SystemLabelOfLed(led);
            return name.Length > 0 ? name : "P" + (led + 1);
        }

        /// <summary>
        /// 灯的气泡：抬头一行，下面说清「这颗灯为什么亮」。
        ///
        /// 两个模式共用一套骨架（抬头 + 命中 / 无命中），只是数据源不同：
        ///   告警模式 → 命中的那条告警规则
        ///   按键模式 → 绑到这颗灯上的按钮
        /// 行号是表格左侧显示的顺序号，几十行的表里照着数字找那一条最省事。
        /// </summary>
        private void UpdateLedHints()
        {
            if (_leds == null || _tip == null) return;
            for (int i = 0; i < 8; i++)
            {
                if (_leds[i] == null) continue;
                // 抬头只在真的挂了系统时才拼「· 系统名」；空灯只留 Pn，避免 "P3 · P3"
                string name = _activeMode == 1 ? SystemLabelOfLed(i) : "";
                string head = "P" + (i + 1) + (name.Length > 0 ? " · " + name : "");
                _tip.SetToolTip(_leds[i], head + "\r\n" + (_activeMode == 1 ? AlarmHint(i) : KeyHint(i)));
            }
        }

        /// <summary>告警模式：这颗灯命中的那条规则；没命中就写「当前无告警」。</summary>
        private string AlarmHint(int led)
        {
            var r = _ledRules[led];
            if (r == null) return L.T("bind.led.noalert");
            // 行号按表格实际顺序算（_alertRows 里有自定义规则，且按系统→优先级重排过）
            int row = _alertRows.IndexOf(r) + 1;
            string disp = TextNames.RuleDisplay(r);
            return L.T("bind.led.hit", row > 0 ? L.T("bind.led.hitrow", row, disp) : disp);
        }

        /// <summary>
        /// 按键模式：绑到这颗灯上的按钮，一条一行，内容与表格那一行一致
        /// （按钮# · 名字 · 触发 · 效果 · 颜色 · 速度 · 目标）。
        /// 目标为「全部」的绑定同样算命中，后面那个「全部」就是在提醒它不止管这颗灯。
        /// </summary>
        private string KeyHint(int led)
        {
            if (_buttons == null || _buttons.Bindings == null) return L.T("bind.led.nobind");

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _buttons.Bindings.Count; i++)
            {
                var b = _buttons.Bindings[i];
                if (!b.Enabled || (b.TargetLed != led && b.TargetLed != -1)) continue;

                if (sb.Length > 0) sb.Append("\r\n");
                sb.Append(L.T("bind.led.bindrow", i + 1, b.ButtonIndex));
                if (!string.IsNullOrEmpty(b.ButtonName)) sb.Append(' ').Append(b.ButtonName);
                sb.Append(" · ").Append(TriggerName(b.Trigger))
                  .Append(" · ").Append(TextNames.Effect(b.EffectId))
                  .Append(" · ").Append(TextNames.Color(b.Color))
                  .Append(" · ").Append(b.Speed.ToString("0.0"))
                  .Append(" · ").Append(TextNames.Target(b.TargetLed >= 0 ? b.TargetLed + 1 : 0));
            }
            return sb.Length == 0 ? L.T("bind.led.nobind") : sb.ToString();
        }

        /// <summary>Tab 文案跟着数据源名走（写死 MSFS2024 的话，接 DCS 就得改代码）。</summary>
        private void UpdateModeTabTexts()
        {
            if (_tabs == null || _tabs.TabPages.Count < 2) return;
            // 1.3.52：页签文案只有这一个真源。原先 ReloadTexts 里也直接写过这两行，
            // 两处并存的结果是「谁后跑谁赢」——切语言后被这里的中文字面量顶回去。
            _tabs.TabPages[0].Text = L.T("main.tab.buttons");
            _tabs.TabPages[1].Text = L.T("main.tab.alert", DataSourceName);
        }

        private void chkLedLabel_CheckedChanged(object sender, EventArgs e)
        {
            if (_suppress || _cfg == null) return;
            _cfg.ShowLedLabel = _chkLedLabel.Checked;
            PersistState();
            UpdateLedLabels();
        }

        private void chkMinimizeToTray_CheckedChanged(object sender, EventArgs e)
        {
            if (_suppress || _cfg == null) return;
            _cfg.MinimizeToTray = _chkMinimizeToTray.Checked;
            ApplyTrayMode();
            PersistState();
            FlashNote(_cfg.MinimizeToTray ? L.T("main.tray.on") : L.T("main.tray.off"));
        }

        private int _fitAlertWidth = -1;
        private int _fitBindWidth = -1;

        // ---- 表格列索引 ----
        // 写成常量，不散落 Cells[6]：1.3.49 给告警表加了一列「速度」，
        // 弹性列索引没跟着改，结果把速度列当成触发条件列，条件列被封顶裁字。
        // 同一次漏改还留下一个更隐蔽的：双击处理里写死 != 5，修完列宽才暴露出来——
        // 双击任何一行的速度格都弹条件编辑器，真正的「触发条件」列点不动。
        // 现在事件处理一律认列对象（colACond）或这些常量，DocLint 会拦住新的魔法数字。
        private const int BindColEnabled = 0;
        private const int BindColBtn = 1;
        private const int BindColName = 2;
        private const int BindColTrigger = 3;
        private const int BindColEffect = 4;
        private const int BindColColor = 5;
        private const int BindColSpeed = 6;
        private const int BindColTarget = 7;

        private const int AlertColEnabled = 0;
        private const int AlertColLed = 1;
        private const int AlertColRule = 2;
        private const int AlertColColor = 3;
        private const int AlertColEffect = 4;
        private const int AlertColSpeed = 5;
        private const int AlertColCond = 6;
        private const int AlertColRaw = 7;

        /// <summary>
        /// 重排列宽：短列按内容撑满（一个字都不裁），弹性列分掉剩下的空间。
        ///
        /// 用 None 模式而不是 Fill：Fill 模式下用户手拖的宽度会被下一次重排算回去，等于禁掉拖动。
        /// 只在可用宽度真的变了才重算——测量要遍历全部行，每帧跑一遍没有必要。
        /// </summary>
        private void AutoFitColumns(DataGridView g, int[] flexCols, int[] weights)
        {
            if (g == null || g.Columns.Count == 0) return;
            if (g.AutoSizeColumnsMode != DataGridViewAutoSizeColumnsMode.None)
                g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            // ClientSize 把垂直滚动条也算在内。告警表四十多行必然出滚动条，
            // 不扣这 17px 就等于每次都多算，列总宽超出可用区，反而挤出一条横向滚动条。
            // 用"显示不全的行"判断滚动条，不去认 Controls 里的控件类型——那是内部实现，换版本会变。
            int avail = g.ClientSize.Width;

            // 行头（画行号那一条）也在 ClientSize 之内，它不是独立的子控件。
            // 不扣掉它，列总宽就恒比可用区宽出一个行头的宽度，结果是不管窗口拉多大，
            // 底部永远挂着一条横向滚动条。
            if (g.RowHeadersVisible)
                avail -= g.RowHeadersWidth;

            if (g.DisplayedRowCount(false) < g.Rows.Count)
                avail -= SystemInformation.VerticalScrollBarWidth;
            avail -= 2;
            if (avail < 200) return;

            int n = g.Columns.Count;

            // ---- 第一遍：非弹性列按内容撑满 ----
            int fixedTotal = 0;
            for (int c = 0; c < n; c++)
            {
                if (Array.IndexOf(flexCols, c) >= 0) continue;
                int w = MeasureColumnWidth(g, c);
                if (w < g.Columns[c].MinimumWidth) w = g.Columns[c].MinimumWidth;
                g.Columns[c].Width = w;
                fixedTotal += w;
            }

            // ---- 第二遍：剩下的按权重分给弹性列 ----
            int rest = avail - fixedTotal;
            if (rest < 0) rest = 0;

            int wsum = 0;
            foreach (int w in weights) wsum += w;
            if (wsum <= 0) wsum = 1;

            int used = 0;
            for (int i = 0; i < flexCols.Length; i++)
            {
                var col = g.Columns[flexCols[i]];
                // 最后一个吃掉除不尽的余数，右边不留 1~2px 的缝
                int target = (i == flexCols.Length - 1)
                    ? rest - used
                    : rest * weights[i] / wsum;
                if (target < col.MinimumWidth) target = col.MinimumWidth;
                col.Width = target;
                used += target;
            }
        }

        /// <summary>
        /// 一列要放得下全部内容所需的宽度。
        ///
        /// 下拉列按**全部候选项**量，不按当前单元格的值：按当前值量的话，某列恰好都是短值
        /// 就量得窄，用户选一个长选项立刻被裁，而且列宽会随内容来回跳。
        /// 表头也参与，不然「油门按钮#」这类长表头会被裁。行不分页，全部量。
        /// </summary>
        private static int MeasureColumnWidth(DataGridView g, int c)
        {
            var col = g.Columns[c];
            const TextFormatFlags Flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
            var any = new Size(int.MaxValue, int.MaxValue);

            Font headFont = g.ColumnHeadersDefaultCellStyle.Font ?? g.Font;
            int w = TextRenderer.MeasureText(col.HeaderText, headFont, any, Flags).Width;

            var combo = col as DataGridViewComboBoxColumn;
            if (combo != null)
            {
                foreach (object item in combo.Items)
                {
                    string s = Convert.ToString(item);
                    if (string.IsNullOrEmpty(s)) continue;
                    int tw = TextRenderer.MeasureText(s, g.Font, any, Flags).Width;
                    if (tw > w) w = tw;
                }
                return w + 26;          // 下拉箭头 + 左右内边距
            }

            for (int r = 0; r < g.Rows.Count; r++)
            {
                string t = Convert.ToString(g.Rows[r].Cells[c].Value);
                if (string.IsNullOrEmpty(t)) continue;
                int tw = TextRenderer.MeasureText(t, g.Font, any, Flags).Width;
                if (tw > w) w = tw;
            }
            return w + 18;              // 左右内边距
        }

        /// <summary>
        /// 双击表头 → 这一列按内容撑到刚好。只动被双击的那一列，其它列原样。
        ///
        /// 上限 600：触发条件那种长句全展开能到一千多像素，真撑开就把别的列全顶出屏幕，
        /// 用户还得再拖回来。要看全文有悬停提示，不必靠列宽。
        /// </summary>
        private void FitColumnToContent(DataGridView g, int col)
        {
            if (g == null || col < 0 || col >= g.Columns.Count) return;
            var c = g.Columns[col];
            int w = MeasureColumnWidth(g, col);
            if (w < c.MinimumWidth) w = c.MinimumWidth;
            if (w > 600) w = 600;
            c.Width = w;

            // 触发条件列是「按当前列宽现算摘要」的，列宽一变就得重排文字，
            // 否则刚撑开还留着上一轮的省略号。
            if (g == _dgvAlert) RefreshCondBriefs();
        }

        /// <summary>
        /// 按「触发条件」列当前宽度重算每行摘要：能放几个字就放几个字，放不下补省略号。
        ///
        /// 不写死字数。列宽是算出来的，字数写死就一定会出现「半个字被切走」——
        /// 那看着像 bug，用户也不知道后面还有内容。悬停有全文（ToolTipText 在建表时挂好）。
        /// </summary>
        private void RefreshCondBriefs()
        {
            if (_dgvAlert == null || _alert == null || _alertRows == null) return;
            if (_dgvAlert.Rows.Count != _alertRows.Count) return;
            if (AlertColCond >= _dgvAlert.Columns.Count) return;

            int maxWidth = _dgvAlert.Columns[AlertColCond].Width - 22;
            if (maxWidth < 40) maxWidth = 40;

            bool prev = _suppress;
            _suppress = true;
            try
            {
                for (int i = 0; i < _alertRows.Count; i++)
                {
                    var r = _alertRows[i];
                    CondNode cond = _alert.CondOf(r);
                    string full = cond != null ? cond.Describe() : "";
                    // 「改过条件」的行前面有个小圆点，这是建表时加的前缀，这里一并重算
                    string text = Ellipsize((_alert.IsCustomized(r.Id) ? "● " : "") + full,
                                            _dgvAlert.Font, maxWidth);

                    var cell = _dgvAlert.Rows[i].Cells[AlertColCond];
                    if (!string.Equals(Convert.ToString(cell.Value), text, StringComparison.Ordinal))
                        cell.Value = text;
                }
            }
            finally
            {
                _suppress = prev;
            }
        }

        /// <summary>把文本裁到指定像素宽：放得下原样返回，放不下截断补省略号（二分找最长可放前缀）。</summary>
        private static string Ellipsize(string text, Font font, int maxWidth)
        {
            if (string.IsNullOrEmpty(text)) return text;

            const TextFormatFlags Flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
            var any = new Size(int.MaxValue, int.MaxValue);

            if (TextRenderer.MeasureText(text, font, any, Flags).Width <= maxWidth) return text;

            int lo = 0, hi = text.Length;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                int w = TextRenderer.MeasureText(text.Substring(0, mid) + "…", font, any, Flags).Width;
                if (w <= maxWidth) lo = mid; else hi = mid - 1;
            }
            return lo <= 0 ? "…" : text.Substring(0, lo) + "…";
        }

        /// <summary>两张表按当前宽度重排（窗口尺寸变化、切页、数据重建后调用）。</summary>
        private void AutoFitAllGrids()
        {
            if (_dgv != null && _dgv.ClientSize.Width != _fitBindWidth)
            {
                _fitBindWidth = _dgv.ClientSize.Width;
                // 按键表：只有「效果」吃剩余——效果名是这页最长的固定内容，
                // 其余（含按钮名）按内容撑满，不会有列被裁
                AutoFitColumns(_dgv, new[] { BindColEffect }, new[] { 100 });
            }
            if (_dgvAlert != null && _dgvAlert.ClientSize.Width != _fitAlertWidth)
            {
                _fitAlertWidth = _dgvAlert.ClientSize.Width;
                // 告警表：触发条件与原始值对半分。原始值里是纯数字、行数会随条件变长，不能压
                AutoFitColumns(_dgvAlert, new[] { AlertColCond, AlertColRaw }, new[] { 50, 50 });
                RefreshCondBriefs();     // 列宽变了，一条条件能放几个字也跟着变
            }
        }


        // ================= 设置卡片底部的「保存状态」行 =================
        //
        // 1.3.32 这里放的是「⚠ N 条 · 点击查看」，点开一个集中的异常清单。
        // 那个清单要求“消息有一个自己的容器”，于是必须维护一份随时会过期的状态列表
        // （谁发、谁消、什么时候清），结果就是“异常消除了条目还在”。
        // 1.3.33 取消容器：消息回到产生它的地方（状态词 + 悬停气泡），
        // 这一行只留一件在容器外确实无处可去的事——「我改的东西到底存下来了没」。

        private string _saveErr = "";

        /// <summary>
        /// 渲染保存状态行。正常灰字一句；存不下时红字 + 悬停给原因和位置。
        /// 只在保存动作发生时调用（PersistState / PersistConfig），不做定时轮询——
        /// 它对应的是一次动作的结果，不是持续变化的运行状态。
        /// </summary>
        private void UpdateSaveState()
        {
            if (_lblDevHint == null) return;

            if (string.IsNullOrEmpty(_saveErr))
            {
                _lblDevHint.Text = L.T("dev.hint.saved");
                HintText.Set(_lblDevHint, SystemColors.ControlText);
                if (_tip != null)
                    _tip.SetToolTip(_lblDevHint,
                        L.T("main.save.tip", G940LedPanel.Core.AppConfig.ConfigPath));
            }
            else
            {
                _lblDevHint.Text = L.T("dev.hint.unsaved");
                HintText.Set(_lblDevHint, C_ErrInk);
                if (_tip != null)
                    _tip.SetToolTip(_lblDevHint,
                        L.T("main.save.errtip", _saveErr, G940LedPanel.Core.AppConfig.ConfigPath));
            }
        }


        /// <summary>
        /// 记一条"本次加载的说明"（配置读失败、条件回落、规则被丢弃、目录不可写…）。
        ///
        /// 同一句不重复记：切配置 / 导入配置会反复走 LoadAlertProfile → Sanitize()，
        /// 每次都回来同样的几句话，累积起来就会让启动那句"另有 N 条"报出虚数。
        /// 这里不做"重新加载前清空"——目录不可写这类说明是加载早期写进去的，
        /// 清空会把它一起清掉，反而丢信息。
        /// </summary>
        private void AddConfigNote(string note)
        {
            if (_cfg == null || string.IsNullOrEmpty(note)) return;
            if (_cfg.ConfigNotes == null) _cfg.ConfigNotes = new System.Collections.Generic.List<string>();
            if (!_cfg.ConfigNotes.Contains(note)) _cfg.ConfigNotes.Add(note);
        }

        /// <summary>实时数据窗：单实例，关掉只是隐藏，采集连续不断。</summary>
        private void btnSimData_Click(object sender, EventArgs e)
        {
            if (_simData == null || _simData.IsDisposed)
            {
                _simData = new SimDataForm(() => _simState);
                _simData.ApplyBounds(ParseBounds(_cfg.SimDataBounds), _cfg.SimDataOnTop);
            }
            _simData.Show();
            _simData.BringToFront();
        }

        /// <summary>"x,y,w,h" → Rectangle；解析失败返回空（保持默认位置）。</summary>
        private static System.Drawing.Rectangle ParseBounds(string text)
        {
            if (string.IsNullOrEmpty(text)) return System.Drawing.Rectangle.Empty;
            var p = text.Split(',');
            if (p.Length != 4) return System.Drawing.Rectangle.Empty;
            int x, y, w, h;
            if (!int.TryParse(p[0], out x) || !int.TryParse(p[1], out y)
                || !int.TryParse(p[2], out w) || !int.TryParse(p[3], out h))
                return System.Drawing.Rectangle.Empty;
            return new System.Drawing.Rectangle(x, y, w, h);
        }

        private static string BoundsText(System.Drawing.Rectangle r)
        {
            return r.X + "," + r.Y + "," + r.Width + "," + r.Height;
        }

        /// <summary>窗口还原：最大化优先，其次按记住的位置。</summary>
        private void RestoreWindowBounds()
        {
            var r = ParseBounds(_cfg.WindowBounds);
            if (r.Width < 600 || r.Height < 400) return;

            // 夹回屏幕可见区。以前只判了尺寸下限，没和屏幕求交：
            // 把整个程序目录拷到屏幕更小的电脑上（1920×1080 → 1366×768），
            // 窗口按旧坐标恢复，标题栏会落到屏幕外——拖不回来，也看不见。
            try
            {
                var wa = Screen.FromPoint(r.Location).WorkingArea;
                if (r.Width > wa.Width) r.Width = wa.Width;
                if (r.Height > wa.Height) r.Height = wa.Height;
                if (r.Right > wa.Right) r.X = wa.Right - r.Width;
                if (r.Bottom > wa.Bottom) r.Y = wa.Bottom - r.Height;
                if (r.Left < wa.Left) r.X = wa.Left;
                if (r.Top < wa.Top) r.Y = wa.Top;
            }
            catch (Exception) { }

            try
            {
                StartPosition = FormStartPosition.Manual;
                Bounds = r;
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 保存与"看到的界面"有关的一切：窗口、两张表的滚动与选中、
        /// 两个配置下拉框里的名字、实时数据窗。
        /// 目标是重开程序一眼看过去和关之前完全一样。
        /// </summary>
        private void SaveViewState()
        {
            try
            {
                _cfg.WindowBounds = BoundsText(WindowState == FormWindowState.Normal ? Bounds : RestoreBounds);

                if (_simData != null && !_simData.IsDisposed && _simData.Visible)
                {
                    _cfg.SimDataBounds = BoundsText(_simData.Bounds);
                    _cfg.SimDataOnTop = _simData.IsOnTop;
                }
                _cfg.SimDataVisible = _simData != null && !_simData.IsDisposed && _simData.Visible;

                // 用对象名而不是下拉文字：下拉已是只读的，且内置配置自动另存后对象名才是权威
                if (_buttons != null) _cfg.ActiveButtonFile = ProfileStore.KeyOf(_buttons);
                if (_alert != null) _cfg.ActiveAlertFile = ProfileStore.KeyOf(_alert);

                if (_dgvAlert != null)
                {
                    try { _cfg.AlertScrollRow = _dgvAlert.FirstDisplayedScrollingRowIndex; } catch (Exception) { }

                    if (_dgvAlert.CurrentRow != null && _alertRows != null)
                    {
                        int i = _dgvAlert.CurrentRow.Index;
                        if (i >= 0 && i < _alertRows.Count) _cfg.AlertSelectedRule = _alertRows[i].Id;
                    }
                }

                if (_dgv != null)
                {
                    try { _cfg.BindScrollRow = _dgv.FirstDisplayedScrollingRowIndex; } catch (Exception) { }
                    if (_dgv.CurrentRow != null) _cfg.BindSelectedRow = _dgv.CurrentRow.Index;
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 恢复两张表的滚动位置与选中行。
        /// 告警表按"规则 Id"恢复而不是行号——规则表顺序变过，行号会错位，Id 不会。
        /// 要在表格真正显示出来之后（OnShown / 首次切到该页）调用，否则滚动位置设不进去。
        /// </summary>
        private void RestoreGridState()
        {
            if (_cfg == null) return;

            if (_dgv != null)
            {
                try
                {
                    if (_cfg.BindSelectedRow >= 0 && _cfg.BindSelectedRow < _dgv.Rows.Count)
                        _dgv.CurrentCell = _dgv.Rows[_cfg.BindSelectedRow].Cells[BindColEnabled];
                    if (_cfg.BindScrollRow > 0 && _cfg.BindScrollRow < _dgv.Rows.Count)
                        _dgv.FirstDisplayedScrollingRowIndex = _cfg.BindScrollRow;
                }
                catch (Exception) { }
            }

            if (_dgvAlert == null || _alertRows == null) return;
            try
            {
                if (!string.IsNullOrEmpty(_cfg.AlertSelectedRule))
                {
                    for (int i = 0; i < _alertRows.Count && i < _dgvAlert.Rows.Count; i++)
                    {
                        if (_alertRows[i].Id != _cfg.AlertSelectedRule) continue;
                        _dgvAlert.CurrentCell = _dgvAlert.Rows[i].Cells[AlertColRule];
                        break;
                    }
                }
                if (_cfg.AlertScrollRow >= 0 && _cfg.AlertScrollRow < _dgvAlert.Rows.Count)
                    _dgvAlert.FirstDisplayedScrollingRowIndex = _cfg.AlertScrollRow;
            }
            catch (Exception) { }
        }

        // ================= 告警表的「原始值」列 =================

        private Timer _rawTimer;
        private Timer _hintTimer;
        private bool _closing;            // 正在退出：别在这个阶段弹模态框

        /// <summary>设备连接失败的完整原因（卡片上只显示一行摘要，明细进悬停/诊断窗）。</summary>
        private string _devFailText = "";
        private SimDataForm _simData;

        /// <summary>
        /// 这条规则用到的变量，按出现顺序把数值摊开：只放数字，不要名字、不要等号。
        /// 派生量（#三腿差）展开成它依赖的原始量，所以一行里全是"真取到的数"。
        /// 没应答的变量占一个 "-"，个数稳定，一眼能看出缺哪个。
        /// </summary>
        private string RawValuesOf(CondNode cond, out string tooltip)
        {
            tooltip = "";
            if (cond == null) return "—";

            var vars = ManualProbe.VarsUsed(cond);
            if (vars == null || vars.Count == 0) return "—";

            var sb = new System.Text.StringBuilder();
            var tip = new System.Text.StringBuilder();
            for (int i = 0; i < vars.Count; i++)
            {
                if (i > 0) { sb.Append(" · "); tip.Append("\r\n"); }

                string name = vars[i];
                bool has = _simState != null && _simState.Has(name);
                if (has)
                {
                    double v = _simState.N(name);
                    sb.Append(NumShort(v));
                    tip.Append(SimVars.LabelOf(name) + " = " + CondEval.Fmt(v) + " " + SimVars.UnitOf(name));
                }
                else
                {
                    sb.Append("-");
                    tip.Append(SimVars.LabelOf(name) + L.T("main.raw.nodata"));
                }
            }

            tooltip = tip.ToString();
            return sb.ToString();
        }

        private static string NumShort(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return "-";
            if (Math.Abs(v) >= 1000) return Math.Round(v).ToString("0");
            if (Math.Abs(v - Math.Round(v)) < 0.05) return Math.Round(v).ToString("0");
            return v.ToString("0.0");
        }

        /// <summary>刷新整列原始值。只改变了的单元格，避免整表重绘闪。</summary>
        private void RefreshRawValues()
        {
            if (_dgvAlert == null || _cfg == null || _alert == null || _alertRows == null) return;
            if (_activeMode != 1) return;
            if (_dgvAlert.Rows.Count != _alertRows.Count) return;

            bool prev = _suppress;
            _suppress = true;
            try
            {
                for (int i = 0; i < _alertRows.Count; i++)
                {
                    var row = _dgvAlert.Rows[i];
                    string tip;
                    string text = RawValuesOf(_alert.CondOf(_alertRows[i]), out tip);

                    string old = Convert.ToString(row.Cells[AlertColRaw].Value);
                    if (!string.Equals(old, text, StringComparison.Ordinal))
                        row.Cells[AlertColRaw].Value = text;
                    if (!string.Equals(row.Cells[AlertColRaw].ToolTipText, tip, StringComparison.Ordinal))
                        row.Cells[AlertColRaw].ToolTipText = tip;
                }
            }
            finally
            {
                _suppress = prev;
            }
        }

        /// <summary>运行时填充下拉框/表格列选项（InitializeComponent 保持设计器可解析的纯标准语句）。</summary>
        private void InitComboItems()
        {
            _cboBgEffect.Items.AddRange(TextNames.BackgroundEffects());
            _cboBgColor.Items.AddRange(TextNames.ColorAll());
            _cboBgSpeed.Items.AddRange(new string[] { "0.5×", "1.0×", "2.0×", "4.0×" });

            // 两个表的下拉列：Designer 里放的是设计期中文字面量，运行时以这里为准。
            // 此刻两张表都还没有行，先后顺序无所谓。
            ReloadBindColumnItems();
            ReloadAlertColumnItems();

            // 提示条颜色（Designer 里只放字面量，颜色在这里给）
            HintText.Set(_lblSimHint2, C_WarnInk);
        }

        /// <summary>
        /// 按当前语言重建**按键表**四个下拉列的候选项（1.3.54）。
        ///
        /// 为什么必须刷：这 6 个列的 Items 在 Designer 里各写一段中文字面量，
        /// 切语言时只有列头跟着变，候选项不动；而重刷表格写进单元格的是新语言的
        /// 显示名（「Green」「Toggle」），值不在 Items 里，WinForms 直接抛
        /// DataGridViewComboBoxCell value is not valid。
        ///
        /// 为什么按表拆开、而不合成一个"两个表一起刷"的方法（1.3.55 修）：
        /// 调用它的两个重刷方法各自只清自己那张表的行。合成一个，就会在刷按键表时
        /// 顺手把告警表的候选项也换掉——那一刻告警表里还留着旧语言的「绿」，
        /// 值立刻非法：**颜色列变空白，不报错**；等下次再切，那个非法值才炸出来。
        /// 所以每个方法只许碰自己那张表的列，宁可多写几行，也不留跨表的中间态。
        ///
        /// 顺序是硬约束：**先清行、再换候选项、后写值**，且必须在清行之后调用。
        /// </summary>
        private void ReloadBindColumnItems()
        {
            FillComboColumn(colBTrigger, TextNames.TriggerAll());
            FillComboColumn(colBColor, TextNames.ColorAll());
            FillComboColumn(colBTarget, TextNames.TargetAll());
            FillComboColumn(colBEffect, TextNames.BindEffects());
        }

        /// <summary>
        /// 按当前语言重建**告警表**两个下拉列的候选项。拆开的原因见
        /// <see cref="ReloadBindColumnItems"/>：跨表刷会造出「另一张表的值不在候选项里」。
        /// </summary>
        private void ReloadAlertColumnItems()
        {
            FillComboColumn(colAColor, TextNames.ColorAll());

            // 告警页的效果列只给白名单里的 6 种（不含「关」）：AlertRuleCatalog 加载时会校验，
            // 选了别的会被当成坏配置拒掉。这里不能图省事也用 EffectAll()，
            // 否则下拉里三分之二的选项是选不中的。1.3.70 起颜色是唯一的关灯开关。
            string[] allowed = new string[AlertRuleCatalog.AllowedEffects.Length];
            for (int i = 0; i < allowed.Length; i++)
                allowed[i] = TextNames.Effect(AlertRuleCatalog.AllowedEffects[i]);
            FillComboColumn(colAEffect, allowed);
        }

        /// <summary>填一个下拉列。认列对象，不认列头文字——列头会随语言变。</summary>
        private static void FillComboColumn(DataGridViewComboBoxColumn col, string[] items)
        {
            if (col == null || items == null) return;
            col.Items.Clear();
            col.Items.AddRange(items);
        }

        /// <summary>
        /// 切语言后重刷按键表的内容（1.3.53）。
        ///
        /// 为什么非刷不可：单元格里存的是**显示名**（「绿」「常亮」「全部」），
        /// 换语言只会换列头，格子里的字不会自己变。用户看到的就是「Effect 列里写着中文」。
        ///
        /// 为什么不用 SyncUiFromConfig：那个还会重设控件选中态、引擎静音、重应用底灯，
        /// 切个语言让正在闪的灯跳一下，属于不该发生的副作用。
        ///
        /// 调用前提：必须在 _suppress 段内。Rows.Add 会触发 CellValueChanged，
        /// 不抑制就会把刚铺好的行当成"用户改表"写回配置，而且会把内置配置标脏。
        /// 调用前已在 ApplyLanguage 里做过 EndEdit。
        /// </summary>
        private void ReloadBindGrid()
        {
            if (_dgv == null || _buttons == null) return;

            // 重铺会丢选中行。记下来再放回去——切语言不该让人找不到刚才在看的那一行
            int row = _dgv.CurrentRow != null ? _dgv.CurrentRow.Index : -1;
            int col = _dgv.CurrentCell != null ? _dgv.CurrentCell.ColumnIndex : -1;

            _dgv.Rows.Clear();
            // 只换本表的列：行刚清空、值还没写，中间态里这张表一个值都没有。
            // 顺手换另一张表的列会把它打成非法值（1.3.54 就是这么出的错）。
            ReloadBindColumnItems();

            foreach (var b in _buttons.Bindings)
                _dgv.Rows.Add(b.Enabled, b.ButtonIndex, b.ButtonName, TriggerName(b.Trigger),
                    TextNames.Effect(b.EffectId), TextNames.Color(b.Color),
                    b.Speed.ToString("0.0"), TextNames.Target(b.TargetLed >= 0 ? b.TargetLed + 1 : 0));

            if (row >= 0 && row < _dgv.Rows.Count && col >= 0 && col < _dgv.Columns.Count)
                _dgv.CurrentCell = _dgv.Rows[row].Cells[col];
        }

        // ================= 界面文案 =================

        /// <summary>
        /// 按 <see cref="L.All"/> 填语言下拉（1.3.67）。
        ///
        /// 1.3.52 ~ 1.3.66 这里是 Designer 里硬编码两行 Items，另配一个写死的 LangIds 数组，
        /// 两者只靠**下标顺序**对齐：加一门语言要同时改两处，顺序错了就会选中文变英文。
        /// 现在两项都从语言表来，加语言不必再碰这个文件。
        ///
        /// 显示名走 L.NativeName（"中文" / "English"），即它自己那种语言，不翻——
        /// 翻成「Chinese」之后英文用户反而认不出哪个是自己正用的。
        /// 语言名不随语言变，所以只在构造时填一次，切语言不必重填。
        /// </summary>
        private void FillLangCombo()
        {
            if (_cboLang == null) return;
            bool prev = _suppress;
            _suppress = true;      // Items.Clear 会把 SelectedIndex 打到 -1，别让它触发切换
            try
            {
                _cboLang.BeginUpdate();
                _cboLang.Items.Clear();
                foreach (var id in L.All) _cboLang.Items.Add(L.NativeName(id));
                _cboLang.EndUpdate();
            }
            finally { _suppress = prev; }
            SyncLangCombo();
        }

        /// <summary>把下拉拨到当前语言。初始化与切换后都要对一次。</summary>
        private void SyncLangCombo()
        {
            if (_cboLang == null) return;
            int idx = Array.IndexOf(L.All, L.Lang);
            if (idx < 0) idx = 0;
            bool prev = _suppress;
            _suppress = true;
            try { _cboLang.SelectedIndex = idx; }
            finally { _suppress = prev; }
        }

        private void cboLang_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suppress || _cfg == null) return;
            int i = _cboLang.SelectedIndex;
            if (i < 0 || i >= L.All.Length) return;
            ApplyLanguage(L.All[i]);
        }

        /// <summary>
        /// 运行中切界面语言（1.3.52）。
        ///
        /// 切一次语言要同时顾三样东西，缺一样用户就会觉得「切坏了」：
        ///   1. 静态文案（ReloadTexts）
        ///   2. 运行时拼出来的状态文字（RefreshStatus 那一族：页脚、计数、橙字红字、提示气泡）
        ///   3. 界面上的临时状态（当前页签、正在编辑的单元格、还没回落的动作反馈）
        ///
        /// 顺序不能反过来：先把文案换成新语言，再重算状态——状态函数里很多要读
        /// 刚换过的文案（例如「勾选「启用 SimConnect 联动」后…」会引用复选框的文字）。
        /// </summary>
        private void ApplyLanguage(string lang)
        {
            string target = L.Resolve(lang);
            bool already = string.Equals(target, L.Lang, StringComparison.Ordinal);

            // 已经是这门语言、且配置里也记着它 → 无事可做
            if (already && string.Equals(_cfg.Language, target, StringComparison.Ordinal)) return;

            // 已经是这门语言，但配置里还空着（= 跟随系统）：只把它记下来。
            // 不记的话，下次系统语言一变界面就跟着变，用户会以为自己的选择没生效。
            if (already)
            {
                _cfg.Language = target;
                PersistState();
                return;
            }

            // ---- 1. 切换前：把「不能丢」的东西记下来 ----
            int tab = _tabs != null ? _tabs.SelectedIndex : 0;

            // 按键页页脚有两种内容：默认操作说明、以及刚做完动作的反馈（"已删除第 3 行…"）。
            // ReloadTexts 会无条件写默认说明，那样一切语言反馈就没了。
            // 判断办法：现在显示的是不是**旧语言**下那句默认说明——是才允许被替换。
            string prevDefault = L.T("bind.foot.capture");
            string capture = _lblCapture != null ? _lblCapture.Text : "";
            bool captureIsDefault = string.IsNullOrEmpty(capture)
                                    || string.Equals(capture, prevDefault, StringComparison.Ordinal);

            // 正在编辑的单元格先收掉：列头/下拉项要动，编辑态留着重绘会错位
            if (_dgv != null && _dgv.IsCurrentCellInEditMode) _dgv.EndEdit();
            if (_dgvAlert != null && _dgvAlert.IsCurrentCellInEditMode) _dgvAlert.EndEdit();

            // ---- 2. 换语言并记住 ----
            L.Use(target);
            _cfg.Language = target;      // 用户显式选过就不再跟随系统
            PersistState();              // 只写 config.json，不碰 profiles

            // ---- 3. 静态文案 + 依赖文案重建的列表（全程抑制事件） ----
            bool prev = _suppress;
            _suppress = true;
            try
            {
                ReloadTexts();
                RefreshPresetCombos();   // 下拉项里带「（内置）」前缀，得按新语言重拼
                if (!captureIsDefault) _lblCapture.Text = capture;
                SyncLangCombo();
            }
            finally { _suppress = prev; }

            // ---- 4. 运行时状态重算（这些函数多数自带 _suppress 守卫，必须放在抑制外面） ----
            RefreshRuntimeTexts();

            // ---- 4b. 开着的小窗口 ----
            // 实时数据窗是非模态的，切语言时它可能还在屏幕上。它是唯一一个
            // 会在主界面被操作期间保持可见的窗——其余对话框都是模态，打开时
            // 主界面点不了，构造时读到的已经是当前语言，不需要这里管。
            if (_simData != null && !_simData.IsDisposed)
                _simData.ApplyLang();

            // ---- 5. 还原页签 ----
            if (_tabs != null && tab >= 0 && tab < _tabs.TabPages.Count) _tabs.SelectedIndex = tab;
        }

        /// <summary>
        /// 所有「运行时拼字符串」的刷新函数跑一遍。切语言后必须重跑，
        /// 否则这些地方会一直停在旧语言——它们是拼接的，ReloadTexts 管不到。
        ///
        /// 400ms 的兜底定时器只调 RefreshStatus，覆盖不到页脚计数、橙字红字、
        /// 条件简述这些，所以这里要逐个点到。
        /// </summary>
        private void RefreshRuntimeTexts()
        {
            UpdateSaveState();        // 设置卡：改动已自动保存 / 存不下
            UpdateMasterState();      // 灯光引擎：运行中 / 已关闭
            UpdateBuiltInHints();     // 页脚：只读橙字 / 被拦红字
            RefreshAlertCounts();     // 页脚：每颗灯 N/M 条
            RefreshAlertFooter();     // 告警页页脚：配置名 + 条数
            RefreshCondBriefs();      // 条件列：规则简述
            RefreshDuplicateMarks();  // 重复标记
            UpdateDupStatus();
            UpdateLedLabels();        // 灯下标签
            UpdateLedHints();         // 灯的提示气泡
            // 采集器手里的状态文案是成品文字（套了模板的 Headline / 原因句），得让它按新语言重算一次，
            // 否则「SDK 检测」弹窗里那句连接状态会停在旧语言（1.3.62 截图）。
            if (_sim != null) _sim.RefreshStatusText();

            // 下面四处原先**只写在"状态变化"的分支里**，切语言没有任何一条路径会再写一遍，
            // 界面就一直停在切换前的语言——1.3.63 两张截图（英文露中文、中文露英文）都是这个原因。
            // 现在它们都只依赖已有的状态字段（_devState/_session.Connected/_activeMode/_chkSim），
            // 重调一次等于原地重画，没有副作用，也不需要另加"记住上次写了什么"的机制。
            SetDevStatus(_devState, _devColor);   // 设备状态词 + 悬停气泡
            UpdateLinkTexts();                    // 连接按钮 + 「当前按下」
            UpdateModeLabel();                    // 底部「当前模式」
            UpdateSimDataLabel();                 // SimConnect 卡片的正文（含"未启用"那句）

            // 窗口标题与托盘提示里的产品名走语言包（app.title），切语言要跟着换
            Text = AppInfo.Title;
            if (_tray != null) _tray.Text = AppInfo.Title;

            RefreshStatus();          // 状态行、SimConnect 卡片
        }

        /// <summary>
        /// 把界面文案重设一遍（1.3.50 起）。
        ///
        /// 与 Designer 的分工：Designer 里写的中文是**设计时预览值**——保证在 VS 里打开看得到界面、
        /// 拖得动、DesignerLint 还能核算宽度；运行时一律以语言包为准。
        /// 两边必须逐字一致，DocLint 会查，否则会出现「在设计器里改了字、跑起来没变」。
        ///
        /// 用 _suppress 包住：改列头、改 ComboBox.Items 都会触发事件，
        /// 底灯下拉一旦写回 _buttons.Background，就会把内置配置标脏、页脚亮红字。
        ///
        /// 1.3.50 只覆盖 20 条，用来验证「.resx → 卫星程序集 → 打包 → 解压运行」这条链。
        /// 剩下的文案在后续阶段搬。
        /// </summary>
        private void ReloadTexts()
        {
            bool prev = _suppress;
            _suppress = true;
            try
            {
                // 页签：文案与数据源名绑在一起，只走 UpdateModeTabTexts 这一个真源
                UpdateModeTabTexts();

                // ---- 设置卡 ----
                // 语言下拉的标题（下拉项本身不翻——语言名用该语言自身写，用户才认得）
                _lblLang.Text = L.T("main.lbl.language");
                _chkMaster.Text = L.T("main.chk.master");
                _chkSim.Text = L.T("main.chk.sim");
                _chkLedLabel.Text = L.T("main.chk.ledlabel");
                _chkMinimizeToTray.Text = L.T("main.chk.tray");
                _chkSceneLog.Text = L.T("main.chk.scenelog");

                // 悬停说明：Designer 里设过一次，切语言要重设——SetToolTip 不跟着 Text 变
                if (_tip != null)
                {
                    _tip.SetToolTip(_chkLedLabel, L.T("main.chk.ledlabel.tip"));
                    _tip.SetToolTip(_chkMinimizeToTray, L.T("main.chk.tray.tip"));
                }

                // ---- 卡片标题 ----
                _ledCard.Text = L.T("main.card.led");
                _devCard.Text = L.T("main.card.settings");

                // ---- 底灯行 ----
                _lblDef.Text = L.T("bind.lbl.bg");
                _lblBgColor.Text = L.T("header.color");
                _lblSpd.Text = L.T("header.speed");

                // ---- 设备卡 ----
                _lblDevName.Text = L.T("dev.name.g940");
                _btnDiag.Text = L.T("main.btn.diag");
                // 按钮文字在「连接 / 断开」之间切，归状态函数管；这里只在它还没被状态函数
                // 写过的时候给个初值——切语言时状态函数会重算（见 RefreshRuntimeTexts）

                // ---- SimConnect 卡 ----
                _btnSimApply.Text = L.T("main.btn.simapply");
                _btnSimCheck.Text = L.T("main.btn.simcheck");
                _btnSimData.Text = L.T("main.btn.simdata");

                // ---- 按键页 ----
                _lblPresetTitle1.Text = L.T("common.profile");
                _btnManage1.Text = L.T("common.manage");
                _lblPresetInfo1.Text = L.T("bind.foot.hint");
                _lblCapture.Text = L.T("bind.foot.capture");
                _lblBgHint.Text = L.T("bind.foot.bg");

                _miCapture.Text = L.T("bind.menu.capture");
                _miAddBind.Text = L.T("bind.menu.add");
                _miDelBind.Text = L.T("bind.menu.del");

                // ---- 告警页 ----
                _lblPresetTitle2.Text = L.T("common.profile");
                _btnManage2.Text = L.T("common.manage");
                _lblSimHint2.Text = L.T("alert.foot.hint");
                // 图例只留三个色点短词，完整解释（颜色/效果各自的含义、同灯只显示最高优先级）
                // 挂到悬停提示上——那行整句英文在 1024 宽下必被裁，中文也已经占满一行。
                _lblLgOk.Text = L.T("alert.legend.ok");
                _lblLgWarn.Text = L.T("alert.legend.warn");
                _lblLgErr.Text = L.T("alert.legend.err");
                if (_tip != null)
                {
                    string legend = L.T("alert.legend");
                    _tip.SetToolTip(_flLegend, legend);
                    _tip.SetToolTip(_lblLgOk, legend);
                    _tip.SetToolTip(_lblLgWarn, legend);
                    _tip.SetToolTip(_lblLgErr, legend);
                }

                _miAudit.Text = L.T("alert.menu.audit");
                _miEditCond.Text = L.T("alert.menu.editcond");
                _miAddRule.Text = L.T("alert.menu.add");
                _miDelRule.Text = L.T("alert.menu.del");

                // ---- 两个表格的列头（共用文案的用 header.*，专有的带页前缀）----
                colBEnabled.HeaderText = L.T("header.enabled");
                colBBtn.HeaderText = L.T("bind.header.btn");
                colBName.HeaderText = L.T("bind.header.name");
                colBTrigger.HeaderText = L.T("bind.header.trigger");
                colBEffect.HeaderText = L.T("header.effect");
                colBColor.HeaderText = L.T("header.color");
                colBSpeed.HeaderText = L.T("header.speed");
                colBTarget.HeaderText = L.T("bind.header.target");

                colAEnabled.HeaderText = L.T("header.enabled");
                colALed.HeaderText = L.T("alert.header.led");
                colARule.HeaderText = L.T("alert.header.rule");
                colAColor.HeaderText = L.T("header.color");
                colAEffect.HeaderText = L.T("header.effect");
                colASpeed.HeaderText = L.T("header.speed");
                colACond.HeaderText = L.T("alert.header.cond");
                colARaw.HeaderText = L.T("alert.header.raw");

                // 底灯效果下拉：20 项整表重填。
                // 1.3.50 只换了前 4 项，后面 16 项切到英文后仍是中文——那是个真实的漏，
                // 因为 20 项同属一张表，没有理由只翻前四个。
                int bgSel = _cboBgEffect.SelectedIndex;
                _cboBgEffect.BeginUpdate();
                try
                {
                    _cboBgEffect.Items.Clear();
                    _cboBgEffect.Items.AddRange(TextNames.BackgroundEffects());
                }
                finally { _cboBgEffect.EndUpdate(); }
                // 重建 Items 一定把选中项顶掉，必须显式恢复：
                // 否则切一次语言，底灯就从「常亮」变成「关灯」，而且用户看不出来是被改的
                if (bgSel >= 0 && bgSel < _cboBgEffect.Items.Count) _cboBgEffect.SelectedIndex = bgSel;

                // 底灯颜色下拉同理
                int bgColorSel = _cboBgColor.SelectedIndex;
                _cboBgColor.Items.Clear();
                _cboBgColor.Items.AddRange(TextNames.ColorAll());
                if (bgColorSel >= 0 && bgColorSel < _cboBgColor.Items.Count) _cboBgColor.SelectedIndex = bgColorSel;

                // 表格单元格里存的是显示名（颜色 / 效果 / 目标），下拉项换了格子也得换
                ReloadBindGrid();
                RefreshAlertGrid();

                // 托盘菜单：不是 Designer 建的，最容易漏，所以先验这一条
                if (_miTrayShow != null) _miTrayShow.Text = L.T("tray.menu.show");
                if (_miTrayExit != null) _miTrayExit.Text = L.T("tray.menu.exit");
            }
            finally
            {
                _suppress = prev;
            }
        }

        // ================= 系统托盘 =================

        private void InitTray()
        {
            ApplyTrayMode();
        }

        /// <summary>
        /// 按「最小化到托盘」开关决定要不要常驻托盘。
        ///
        /// 开：建图标、挂最小化事件；关：销毁图标、摘掉事件（最小化就是普通最小化）。
        /// 运行中改复选框也走这里，所以必须能重复调用——先无条件摘事件再按需挂，
        /// 不然反复切换会挂出好几份。
        /// </summary>
        private void ApplyTrayMode()
        {
            bool on = _cfg == null || _cfg.MinimizeToTray;

            Resize -= OnTrayResize;

            if (on)
            {
                if (_tray == null)
                {
                    _tray = new NotifyIcon
                    {
                        Icon = MakeTrayIcon(),
                        Text = AppInfo.Title,
                        Visible = true
                    };
                    var menu = new ContextMenuStrip();
                    _miTrayShow = new ToolStripMenuItem(L.T("tray.menu.show"));
                    _miTrayShow.Click += (s, e) => ShowMainWindow();
                    menu.Items.Add(_miTrayShow);
                    menu.Items.Add(new ToolStripSeparator());
                    _miTrayExit = new ToolStripMenuItem(L.T("tray.menu.exit"));
                    _miTrayExit.Click += (s, e) =>
                    {
                        _quitFromTray = true;
                        Close();
                    };
                    menu.Items.Add(_miTrayExit);
                    _tray.ContextMenuStrip = menu;
                    _tray.DoubleClick += (s, e) => ShowMainWindow();
                }
                _tray.Visible = true;
                Resize += OnTrayResize;
            }
            else
            {
                if (_tray != null)
                {
                    _tray.Visible = false;
                    _tray.Dispose();
                    _tray = null;
                }
                _trayBalloonShown = false;   // 下次重新打开开关还能提示一次
            }
        }

        private void OnTrayResize(object sender, EventArgs e)
        {
            if (WindowState != FormWindowState.Minimized) return;
            Hide();
            ShowTrayBalloonOnce();
        }

        private static Icon MakeTrayIcon()
        {
            using (var bmp = new Bitmap(16, 16))
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                using (var b = new SolidBrush(Color.FromArgb(0, 168, 0)))
                    g.FillEllipse(b, 1, 1, 14, 14);
                using (var p = new Pen(Color.FromArgb(0, 96, 0)))
                    g.DrawEllipse(p, 1, 1, 14, 14);
                return Icon.FromHandle(bmp.GetHicon());
            }
        }

        private void ShowMainWindow()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void ShowTrayBalloonOnce()
        {
            if (_trayBalloonShown || _tray == null) return;
            _trayBalloonShown = true;
            _tray.ShowBalloonTip(3000, AppInfo.Name, L.T("main.tray.balloon"), ToolTipIcon.Info);
        }

        private void ShowDiagnostics()
        {
            // 以前这里要求"先连接设备"，可连接失败时用户最想看的恰恰是诊断窗口里的原因。
            // 现在任何时候都能打开：没连上就显示上次的失败原因 + 设备清单 + 原始输入报告。
            using (var dlg = new DiagForm(_rawInput, BuildSdkDiagText, () => _devFailText))
            {
                dlg.ShowDialog(this);
            }
        }

        // ================= 原始输入（WM_INPUT） =================

        protected override void WndProc(ref Message m)
        {
            // 第二个进程发现已经有一个在跑，会广播这条消息：把窗口叫出来
            if (m.Msg == G940LedPanel.Program.ActivateMessage)
            {
                ShowMainWindow();
                return;
            }
            if (m.Msg == RawInputReader.WM_INPUT && m.LParam != IntPtr.Zero)
            {
                try { _rawInput.HandleMessage(m.LParam); } catch (Exception) { }
                return;
            }
            // 插拔 G940：叫醒灯通道。只认 VID 046D / PID C287，插个 U 盘不会惊动它。
            if (_devNotify.HandleMessage(m.Msg, m.WParam, m.LParam)) return;
            base.WndProc(ref m);
        }

        /// <summary>目标设备接入：叫醒灯通道，并把输入通道自动接回来。</summary>
        private void OnTargetDeviceArrived()
        {
            try { _session.MarkLedChannelStale(); } catch (Exception) { }

            // 已经连着就不用管（开机时插着设备、程序刚启动那种情形）。
            // 用户自己点过「断开」也不抢着连——按钮不能被程序悄悄盖掉。
            if (_session.Connected || _userDisconnected) return;
            ScheduleReconnect();
        }

        /// <summary>
        /// 目标设备移除：灯通道保留（插回时它自己重开），输入通道必须真的释放。
        ///
        /// 只用 MarkLedChannelStale 是不够的：拔线不会让 DirectInput 设备指针归零，
        /// Connected 于是永远为 true，卡片停在"已连接"、按钮停在"断开"，
        /// 而每帧读到的都是失效对象。这里把输入侧断干净，状态就跟着对了。
        /// 灯句柄留着——它和输入是两条独立通道，插回时靠它自己恢复。
        /// </summary>
        private void OnTargetDeviceRemoved()
        {
            try { _session.MarkLedChannelStale(); } catch (Exception) { }

            if (!_session.Connected) return;      // 本来就没连（或用户已手动断开），界面不用动

            try { _pollTimer.Stop(); } catch (Exception) { }
            try { _session.ReleaseInput(); } catch (Exception) { }

            _devFailText = "";                    // 清掉上一次的失败原因，免得气泡挂着旧话
            SetDevStatus(DevState.Unplugged, C_ErrInk);
            UpdateLinkTexts();                    // 按钮与「当前按下」按"已断开"重算
            RefreshStatus();
        }

        /// <summary>
        /// 插回后隔一小会儿再连，只试一次。
        ///
        /// 不能在这里立刻枚举：DBT_DEVICEARRIVAL 到达时驱动往往还没把设备节点挂全，
        /// 当场枚举经常一个设备都看不到，白白报一次"没认出 G940"。
        /// 800ms 够它出来；这一次没连上就停在"未连接"等用户点「连接」，
        /// 不做退避重试——反复自己重连比手动点一下更让人摸不着头脑。
        /// </summary>
        private void ScheduleReconnect()
        {
            if (_reconnectTimer == null)
            {
                _reconnectTimer = new Timer { Interval = 800 };
                _reconnectTimer.Tick += (s, e) =>
                {
                    _reconnectTimer.Stop();
                    if (_session.Connected || _userDisconnected) return;
                    AutoConnectThrottle(false, false);   // 不播自检动画：拔插不该像启动那样闪一遍
                };
            }
            _reconnectTimer.Stop();
            _reconnectTimer.Start();
        }

        // ================= 数据同步 =================

        private void SyncUiFromConfig()
        {
            _suppress = true;
            try
            {
                _cboBgEffect.SelectedIndex = Clamp(IndexOf(EffectFactory.Ids, EffectFactory.Normalize(_buttons.Background.EffectId)), 0, EffectFactory.Ids.Length - 1);
                _cboBgColor.SelectedIndex = Clamp(_buttons.Background.Color, 0, 3);
                _cboBgSpeed.SelectedIndex = Clamp(IndexOf(SpeedSteps, _buttons.Background.Speed), 0, 3);
                _chkMaster.Checked = _cfg.MasterOn;
                _chkSim.Checked = _cfg.SimConnectEnabled;
                _chkLedLabel.Checked = _cfg.ShowLedLabel;
                _chkMinimizeToTray.Checked = _cfg.MinimizeToTray;
                _engine.Muted = !_cfg.MasterOn;

                _dgv.Rows.Clear();
                foreach (var b in _buttons.Bindings)
                    _dgv.Rows.Add(b.Enabled, b.ButtonIndex, b.ButtonName, TriggerName(b.Trigger),
                        TextNames.Effect(b.EffectId), TextNames.Color(b.Color),
                        b.Speed.ToString("0.0"), TextNames.Target(b.TargetLed >= 0 ? b.TargetLed + 1 : 0));

                // 告警页：从 AlertProfile 铺表格（内置 55 条 + 自定义规则）
                ApplyAlertConfigToUi();
                RefreshPresetCombos();

                // 配置下拉的选中项由 RefreshPresetCombos() 按当前配置名回填；
                // 只回填"显示"，不重新载入配置内容——否则会把用户后来手改的表格覆盖掉。

                UpdateMasterState();
                UpdateBgControlState();
                RefreshDuplicateMarks();
            }
            finally
            {
                _suppress = false;
            }

            // 界面上显示的默认效果必须真的接到引擎上。
            // 少了这一步：引擎照样在跑，但 Background 是 null，每帧合成都全灭 ——
            // 设备连着却一颗灯不亮（预览灯全显示"灭"的深蓝），
            // 再叠加连接瞬间的 FlashTest 直写设备，就是绿→灭→绿 的乱闪。
            ApplyBackground();

            // 配置铺完就把状态/提示/灯效整体算一遍，不留"刚打开时提示是空的"这种窗口
            RefreshStatus();
        }

        /// <summary>
        /// 把表格内容搬进配置对象（只改内存，不写盘）。
        /// 和落盘分开是因为两件事的时机不同：值一变内存就必须是最新的（预览灯和叠加层直接读它），
        /// 而磁盘等"这次编辑结束"再写——打字过程中不必每敲一下写一次盘。
        /// </summary>
        private void SyncBindingsFromGrid()
        {
            if (_suppress) return;

            ClearBindingOverlays();

            // 重建对象前先把 toggle 的"开着"状态按按钮号记下来，
            // 否则改一行效果会把其它行的开关状态一起清掉。
            var wasOn = new Dictionary<int, bool>();
            foreach (var old in _buttons.Bindings)
                if (old.RuntimeOn) wasOn[old.ButtonIndex] = true;

            var rebuilt = new List<ButtonBinding>();
            foreach (DataGridViewRow r in _dgv.Rows)
            {
                if (r.Cells[BindColBtn].Value == null) continue;
                var b = new ButtonBinding();
                b.Enabled = GetBool(r.Cells[BindColEnabled].Value, true);
                int btnIdx;
                int.TryParse(Convert.ToString(r.Cells[BindColBtn].Value), out btnIdx);
                b.ButtonIndex = btnIdx;
                b.ButtonName = Convert.ToString(r.Cells[BindColName].Value) ?? "";
                b.Trigger = Convert.ToString(r.Cells[BindColTrigger].Value) == TextNames.Trigger("toggle") ? "toggle" : "hold";
                b.EffectId = IdOfEffectName(Convert.ToString(r.Cells[BindColEffect].Value));
                b.Color = IndexOf(TextNames.ColorAll(), Convert.ToString(r.Cells[BindColColor].Value));
                if (b.Color < 0) b.Color = 0;
                double spd;
                double.TryParse(Convert.ToString(r.Cells[BindColSpeed].Value), out spd);
                b.Speed = spd;
                if (b.Speed <= 0) b.Speed = 1.0;
                int ti = IndexOf(TextNames.TargetAll(), Convert.ToString(r.Cells[BindColTarget].Value));
                b.TargetLed = ti > 0 ? ti - 1 : -1;
                b.RuntimeOn = wasOn.ContainsKey(b.ButtonIndex);
                rebuilt.Add(b);
            }

            // 整份替换而不是 Clear 再 Add：中途抛异常时配置里不会只剩半张表
            _buttons.Bindings = rebuilt;

            // 清完必须建：上面刚把所有叠加层移除，而 toggle 的"开着"状态被保留了下来。
            // 只清不建就会出现"开关说开着、灯不亮"，用户按第一下被用来关掉一个本来
            // 就没亮的灯（1.3.34 修的就是这个：从告警模式切回按键模式要按两遍）。
            if (_activeMode == 0) RebuildBindingOverlays();
        }

        /// <summary>内存同步 + 立刻落盘。保存配置、增删行、关窗用这个。</summary>
        private void SaveBindingsFromGrid()
        {
            SyncBindingsFromGrid();
            PersistConfig();
            // 增行 / 删行会让后面每一行的行号都变，灯上的气泡写着行号，要跟着刷
            UpdateLedHints();
        }

        /// <summary>
        /// 只写 config.json（本机状态）：页签、窗口位置、总开关、诊断记录开关这些。
        ///
        /// 为什么不复用 <see cref="PersistConfig"/>：那个会顺手写两套配置，而"改内置配置要自动
        /// 另存为我的"这条规则就在写配置的路上。切一下页签就多出一个"xxx（我的）"配置，
        /// 用户会以为程序在乱建文件。
        /// </summary>
        private void PersistState()
        {
            if (_cfg == null) return;
            if (_cfg.Save()) _saveErr = "";
            else
            {
                _saveErr = _cfg.LastSaveError;
                ReportSaveFailure();
            }
            UpdateSaveState();
        }

        /// <summary>
        /// 把"当前状态"和"当前两套配置"一起写盘；失败会报到设置卡片的保存状态行，不再静默吞掉。
        ///
        /// 为什么一次写三个文件而不是让调用方各自判断：改配置内容的调用点有十几处，
        /// 逐处判断"这次改的是哪套配置"迟早会漏一处，表现就是"改了没保存"。
        /// 配置文件只有几十 KB，全写一遍的代价远低于漏存的代价。
        ///
        /// 注意：改配置内容才用这个。纯状态改动请用 <see cref="PersistState"/>。
        /// </summary>
        private void PersistConfig()
        {
            if (_cfg == null) return;
            PersistState();

            string btn = SaveButtonProfile();
            string alt = SaveAlertProfile();

            // 三个文件同目录，一般一起成功或一起失败；但只读目录里也可能
            // config.json 写得进、profiles 子目录写不进，所以这里再对一次。
            if (btn != null || alt != null)
            {
                _saveErr = btn ?? alt;
                UpdateSaveState();
            }
        }

        /// <summary>
        /// 保存失败不弹窗，只让设置卡片的保存状态行变红（悬停看原因）。
        /// 装到只读目录时每次改东西都弹一次框，界面会被堵死，人也会点"确定"点到麻木。
        /// </summary>
        private void ReportSaveFailure()
        {
            if (_cfg == null || string.IsNullOrEmpty(_cfg.LastSaveError)) return;
            if (_closing) return;
            RefreshStatus();
        }

        // ================= 告警页（内置 55 条 + 用户自定义规则） =================

        /// <summary>把 AlertProfile 铺到界面：表格、底部计数。载入配置/改灯位后调用。</summary>
        private void ApplyAlertConfigToUi()
        {
            if (_cfg == null || _alert == null) return;

            RefreshAlertGrid();
            RefreshAlertCounts();
            RefreshAlertFooter();   // 页脚写着当前配置名与启用条数，载入配置后跟着变
        }

        /// <summary>
        /// 重建告警表。行序固定 = 系统序 → 优先级升序（就是 AlertRuleCatalog.Rules 的顺序），
        /// 不排序、不拖动、不增删。只有「启用」列可勾，其余列只读。
        /// </summary>
        private void RefreshAlertGrid()
        {
            if (_dgvAlert == null || _cfg == null || _alert == null) return;

            _alertRows.Clear();
            _alertRows.AddRange(_alert.AllRules());
            // 按"系统顺序 + 优先级"排：自定义规则本来追加在列表末尾，
            // 不排的话，用户在"起落架"里加的一条会跑到表尾"燃油"下面，一眼找不到。
            _alertRows.Sort((a, b) =>
            {
                int sa = AlertRuleCatalog.SysIndex(a.SystemKey);
                int sb = AlertRuleCatalog.SysIndex(b.SystemKey);
                if (sa != sb) return sa.CompareTo(sb);
                return _alert.PriorityOf(a).CompareTo(_alert.PriorityOf(b));
            });

            bool prev = _suppress;
            _suppress = true;
            try
            {
                _dgvAlert.Rows.Clear();
                // 只换本表的列，理由同上
                ReloadAlertColumnItems();

                for (int i = 0; i < _alertRows.Count; i++)
                {
                    var r = _alertRows[i];
                    int sys = AlertRuleCatalog.SysIndex(r.SystemKey);
                    int led = SystemLedMap.LedOf(_alert.SystemLedMap, sys);
                    CondNode cond = _alert.CondOf(r);
                    string full = cond != null ? cond.Describe() : "";

                    int rowIndex = _dgvAlert.Rows.Add(
                        _alert.IsEnabled(r.Id),
                        led >= 0 ? "P" + (led + 1) : "?",
                        TextNames.RuleDisplay(r) + (r.Sample ? L.T("alert.rule.sample") : ""),
                        TextNames.Color(_alert.ColorOf(r)),
                        TextNames.Effect(_alert.EffectOf(r)),
                        _alert.SpeedOf(r).ToString("0.0"),
                        full,         // 先放全文；列宽定下来后 RefreshCondBriefs 按宽度截
                        "");          // 原始值列：由 RefreshRawValues 每 250ms 填

                    var row = _dgvAlert.Rows[rowIndex];

                    // 相邻系统交替底色（按系统下标 2 色轮换，系统数扩到 10 也一样），比插分组行安静得多
                    row.DefaultCellStyle.BackColor = (sys % 2 == 0) ? C_RowA : C_RowB;

                    // 兜底规则（不点灯）原先是整行字色变淡来区分。
                    // 1.3.49 起文字只有四色，不再用灰度做弱化——兜底规则本来就在
                    // “兜底”分组里，分组边界已经说明它只是解释，不需要再叠一层颜色。

                    row.Cells[AlertColCond].ToolTipText = full;

                    // 加载时被判定"不合法"而回落到内置条件的行：标橙，让用户知道改的那条没生效
                    if (_alert.DroppedRuleIds != null && _alert.DroppedRuleIds.Contains(r.Id))
                    {
                        row.Cells[AlertColCond].Style.ForeColor = C_WarnInk;
                        row.Cells[AlertColCond].ToolTipText = full + "\r\n" + L.T("alert.grid.dropped");
                    }

                    // 条件被改过的行：摘要前面那个小圆点由 RefreshCondBriefs 加（它要知道列宽），
                    // 这里只负责解释圆点的意思
                    if (_alert.IsCustomized(r.Id))
                        row.Cells[AlertColRule].ToolTipText = L.T("alert.grid.customized");

                    // 用户新增的规则（usr_ 开头）用行头色块区分。
                    // 原先规则名还是蓝字，1.3.49 起文字只留四色，蓝字取消——色块已经能说明
                    // "这条是我加的"，蓝字属于重复强调。不动交替底色（按系统分组），
                    // 也不碰橙字告警 / 圆点（那是"条件被改过"），三套标记各管一件事。
                    if (_alert.IsCustomRule(r.Id))
                    {
                        row.HeaderCell.Style.BackColor = C_CustomMark;
                        row.Cells[AlertColRule].ToolTipText = L.T("alert.grid.userrule");
                    }

                    // 样板规则：条件由程序自算，既不是 MSFS 判定也不是真机的灯。默认停用，
                    // 价值在"照着改"，所以只加一句说明，不参与颜色/色块那三套标记。
                    if (r.Sample)
                    {
                        row.Cells[AlertColRule].ToolTipText = L.T("alert.grid.sample");
                    }
                }
            }
            finally
            {
                _suppress = prev;
            }

            // 内容变了，理想列宽也跟着变（切页/载入配置/改条件都会重建表）
            _fitAlertWidth = -1;
            AutoFitAllGrids();

            // 行序可能变了（自定义规则插进对应系统），灯的气泡里写着行号，
            // 必须跟着重算——_ledRules 存的是规则对象，这里刷新一下就能拿到新行号。
            UpdateLedHints();
        }

        /// <summary>
        /// 底部：每颗灯「启用数/总数」。某颗灯有规则却一条都没启用时整行变红——
        /// 那意味着这个系统出问题灯不会亮，看着有监控其实没监控。
        /// 「0/0」（系统挂了灯但一条规则都没建）不算漏洞，只是还没配，仍是橙。
        /// </summary>
        private void RefreshAlertCounts()
        {
            if (_lblAlertCounts == null || _cfg == null || _alert == null) return;

            // 页脚按「灯」排，长度恒为物理灯数 8。系统数扩到 10 后，没上灯的系统
            // 不占页脚格子（否则会冒出 P9 / P10 两颗不存在的灯）。
            var parts = new string[SystemLedMap.Count];
            for (int i = 0; i < parts.Length; i++) parts[i] = "P" + (i + 1) + " —";
            bool anyEmpty = false;

            for (int s = 0; s < AlertRuleCatalog.Systems.Length; s++)
            {
                string key = AlertRuleCatalog.Systems[s].Key;
                int total = 0, on = 0;
                foreach (var r in _alert.AllRules())
                {
                    if (r.SystemKey != key) continue;
                    total++;
                    if (_alert.IsEnabled(r.Id)) on++;
                }

                int led = SystemLedMap.LedOf(_alert.SystemLedMap, s);
                if (led < 0 || led >= parts.Length) continue;   // 该系统没挂灯，不进页脚
                parts[led] = "P" + (led + 1) + " " + on + "/" + total;
                if (total > 0 && on == 0) anyEmpty = true;
            }

            _lblAlertCounts.Text = string.Join(" · ", parts);
            HintText.Set(_lblAlertCounts, anyEmpty ? C_ErrInk : C_WarnInk);
            // 页脚文字统一由 RefreshAlertFooter 管（静态说明 / 一次性反馈），
            // 这里只负责"每颗灯启用几条"这一行数字。
        }

        /// <summary>勾选/取消启用。只改 EnabledRules，不重建表格（避免选中行跳走）。</summary>
        private void OnAlertEnabledChanged(int rowIndex)
        {
            if (_suppress) return;
            if (rowIndex < 0 || rowIndex >= _alertRows.Count) return;
            if (_dgvAlert == null || rowIndex >= _dgvAlert.Rows.Count) return;

            var r = _alertRows[rowIndex];
            bool on = GetBool(_dgvAlert.Rows[rowIndex].Cells[AlertColEnabled].Value, true);
            if (_alert.IsEnabled(r.Id) == on) return;

            _alert.SetEnabled(r.Id, on);
            PersistConfig();
            RefreshAlertCounts();
            if (_activeMode == 1) EvaluateAlerts(true);
        }

        // 1.3.43：「按系统 全选 / 清空」整组移除。8 颗灯、10 系统、55 条规则，
        // 启停本来就逐条勾选更可控；批量清空还会让 8 颗灯一起黑，分不清是配置还是程序问题。
        // 想整体恢复，载入内置配置即可。

        /// <summary>点某颗灯 → 弹出这颗灯的系统配置框（只在告警模式触发）。</summary>
        private void EditLedAssign(int led)
        {
            if (_cfg == null || _alert == null || _activeMode != 1) return;

            using (var dlg = new LedAssignForm(_alert.SystemLedMap, led))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK || !dlg.Changed) return;
                ApplyLedMap(dlg.Map);
            }
        }

        /// <summary>落一张新的灯位映射表：写盘、重铺表格、刷灯上文字、立即重新判定。</summary>
        private void ApplyLedMap(int[] map)
        {
            if (_cfg == null || _alert == null || map == null) return;
            int[] before = SystemLedMap.Clone(_alert.SystemLedMap);
            _alert.SystemLedMap = SystemLedMap.Pad(map);

            PersistConfig();
            RefreshAlertGrid();
            RefreshAlertCounts();
            UpdateLedLabels();
            _fitAlertWidth = -1;
            AutoFitAllGrids();          // 灯位换了，灯上写的系统名也要跟着换
            if (_activeMode == 1) EvaluateAlerts(true);

            AuditJustLitSystems(before, _alert.SystemLedMap);
        }

        /// <summary>
        /// 右键「检查我的规则…」：查当前选中行所属系统；没有选中行就查全部系统。
        /// 列出全部结论（含纯内置之间的），用户才好判断自己那条是压住了谁、还是被谁压住。
        /// </summary>
        private void miAudit_Click(object sender, EventArgs e)
        {
            if (_alert == null) return;

            string sysKey = null;
            if (_dgvAlert != null && _dgvAlert.CurrentRow != null)
            {
                int row = _dgvAlert.CurrentRow.Index;
                if (row >= 0 && row < _alertRows.Count) sysKey = _alertRows[row].SystemKey;
            }

            var items = RuleAudit.Snapshot(_alert);
            if (!string.IsNullOrEmpty(sysKey))
                items = items.FindAll(delegate (RuleAudit.Item it) { return it.SystemKey == sysKey; });

            var found = RuleAudit.Run(items, false);
            string title = string.IsNullOrEmpty(sysKey)
                ? L.T("alert.audit.title.all")
                : L.T("alert.audit.title.sys", TextNames.SysName(sysKey));

            using (var dlg = new RuleAuditForm())
            {
                dlg.Render(found, title);
                dlg.ShowDialog(this);
            }
        }

        /// <summary>
        /// 灯位改完后的自动体检：只查"这次刚从不上灯变成上灯"的系统。
        ///
        /// 为什么只查这些：系统没上灯时它的规则是休眠的，改什么都影响不到灯；一上灯，
        /// 它这一套（内置 + 用户改过的）就开始抢这颗灯了 —— 这一刻才值得提醒一次。
        /// 换灯、下灯都不查：规则集没变，冲突关系也不会变。
        ///
        /// 只报"用户能负责"的（自己新增的、自己改过的）。纯内置的问题归出包自检，
        /// 报给用户他也改不了，只会困惑。
        /// </summary>
        private void AuditJustLitSystems(int[] before, int[] after)
        {
            if (before == null || after == null || _alert == null) return;

            var justLit = new List<string>();
            for (int s = 0; s < SystemLedMap.SystemCount && s < AlertRuleCatalog.Systems.Length; s++)
            {
                bool was = SystemLedMap.LedOf(before, s) >= 0;
                bool now = SystemLedMap.LedOf(after, s) >= 0;
                if (!was && now) justLit.Add(AlertRuleCatalog.Systems[s].Key);
            }
            if (justLit.Count == 0) return;

            var items = RuleAudit.Snapshot(_alert).FindAll(
                delegate (RuleAudit.Item it) { return justLit.Contains(it.SystemKey); });
            var found = RuleAudit.Run(items, true);
            if (found.Count == 0) return;

            var sb = new StringBuilder();
            sb.Append(L.T("main.justlit.head", justLit.Count, found.Count));

            int shown = 0;
            foreach (var f in found)
            {
                if (shown >= 5)
                {
                    sb.Append(L.T("main.justlit.more", found.Count - shown));
                    break;
                }
                sb.Append("· ").Append(f.SystemName).Append(" · ").Append(f.RuleName);
                if (!string.IsNullOrEmpty(f.OtherName)) sb.Append(" × ").Append(f.OtherName);
                sb.Append("\r\n　　").Append(f.Text).Append("\r\n");
                shown++;
            }
            sb.Append(L.T("main.justlit.foot"));

            MessageBox.Show(this, sb.ToString(), L.T("main.audit.title"),
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ================= 设备：自动连接 G940 油门 =================

        /// <summary>
        /// 枚举并连接 G940 油门。
        ///
        /// 两种调用场合，差别只在一个地方——**认不出设备时要不要问用户**：
        ///   interactive = false（启动）：静默失败，只写状态词和气泡。
        ///   interactive = true （点「连接」）：弹框让用户从列表里挑一个。
        ///
        /// 为什么启动时不问：把程序目录拷到一台没接设备的电脑上看配置，
        /// 一开机就被弹框拦住不合理；拔掉设备后桌上还挂着手柄，同样不该弹。
        /// 用户想用时自然会点「连接」，那时才是该问的时候。
        ///
        /// 为什么不再用 GUID 兜底：GUID 是「机器 + USB 口」的指纹，换电脑必然失效，
        /// 而且失效是静默的。更关键的是它读不到——只有在「名字匹配失败」时才读，
        /// 而能写进去的前提是「曾经匹配成功」，于是这条分支永远走不到。1.3.34 删掉。
        /// </summary>
        private void AutoConnectThrottle(bool interactive, bool playBoot = true)
        {
            _devFailText = "";
            SetDevStatus(DevState.Detecting, C_WarnInk);
            try
            {
                var list = DeviceSession.Enumerate();
                if (list.Count == 0)
                {
                    ShowDevFail(DevState.NotFound,
                        L.T("dev.fail.none"), L.T("dev.fail.none.detail"));
                    return;
                }

                // 一级判据：用户指定过就用他指定的名字，没指定就用出厂默认名。
                // 不设「先试指定名、再退回默认名」两级——指定了就信指定的，
                // 指定的设备不在这台机器上时，让用户重选一次比悄悄换一个更好。
                string want = string.IsNullOrEmpty(_cfg.DeviceName)
                            ? DevicePicker.DefaultName
                            : _cfg.DeviceName;

                var target = DevicePicker.Find(list, want);
                if (target != null) { TryConnect(target, playBoot); return; }

                if (!interactive)
                {
                    // 启动时不弹框。把枚举到的名字写进气泡，用户截图就能说清现场
                    // （一套 G940 是三个设备，光说"共 3 个"看不出是插错还是认错）。
                    ShowDevFail(DevState.NotFound,
                        L.T("dev.fail.nomatch", list.Count),
                        L.T("dev.fail.nomatch.detail") + DeviceNameList(list));
                    return;
                }

                var picked = PickDeviceForm.Ask(this, list);
                if (picked == null)
                {
                    ShowDevFail(DevState.NotFound, L.T("dev.fail.nopick"), L.T("dev.fail.nopick.detail"));
                    return;
                }

                // 记名字不记 GUID：名字是型号标识，拷到别的电脑仍然有效。
                _cfg.DeviceName = DevicePicker.NameOf(picked);
                PersistState();
                TryConnect(picked);
            }
            catch (Exception ex)
            {
                ShowDevFail(DevState.NotFound, L.T("dev.fail.enum"), ex.Message);
            }
        }

        /// <summary>把枚举结果拍成「· 名字」的若干行，用于气泡/诊断窗。</summary>
        private static string DeviceNameList(List<DeviceInfo> list)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append("\r\n");
                string n = DevicePicker.NameOf(list[i]);
                sb.Append("· ").Append(string.IsNullOrEmpty(n) ? L.T("dev.devicelist.unnamed") : n);
            }
            return sb.ToString();
        }

        private void TryConnect(DeviceInfo info, bool playBoot = true)
        {
            try
            {
                string err;
                if (!_session.Connect(info, Handle, out err))
                {
                    // 1.3.38 起灯与输入都在 Connect 内部各自打开，失败原因由它给全。
                    // 原来这里先探一次 G940LedInterface.dll，那个 DLL 已经不需要了。
                    string detail = err;
                    if (!string.IsNullOrEmpty(_session.LastWarnings)) detail += "\r\n" + _session.LastWarnings;
                    ShowDevFail(DevState.InitFailed, L.T("dev.fail.init"), detail);
                    return;
                }
            }
            catch (Exception ex)
            {
                ShowDevFail(DevState.InitFailed, L.T("dev.fail.connect"), ex.Message);
                return;
            }

            // 这里原来会把 InstanceGuid 记进配置当「下次直接连它」的兜底。
            // 1.3.34 删掉：GUID 换 USB 口/换电脑就变，且那条分支在逻辑上永远走不到
            // （读到它前提是名字匹配失败，写入它的前提是匹配成功过）。

            try { _rawInput.SetButtonOffsets(_session.GetButtonOffsets()); } catch (Exception) { }

            _userDisconnected = false;
            UpdateLinkTexts();

            // 连上了就把上次的失败原因收掉——必须在设状态词之前，
            // 否则气泡里还挂着上一次为什么没连上。
            _devFailText = "";

            // "连上了"不等于"能用"：读不到按钮时灯会亮、按键却毫无反应。
            // 这种状态词不能是绿色，否则用户根本不会去悬停看气泡。
            int btnCount = (_session.Info != null) ? _session.Info.ButtonCount : 0;
            SetDevStatus(btnCount > 0 ? DevState.Connected : DevState.ConnectedNoButtons,
                         btnCount > 0 ? C_OkInk : C_ErrInk);
            _pollTimer.Start();
            if (_chkMaster.Checked) StartEngine();

            // 连上就播一遍"自检 → 模式提示"，这是用户唯一能确认"灯和程序都通了"的信号。
            // 自动重连（拔了再插）不播：那不是启动，闪一遍反而像出了状况。
            if (playBoot) StartBootSequence();
            RefreshStatus();   // 状态词、气泡、灯效当场对齐，不用等下一次节拍

            // 这里原来会调 FlashTest() 直接 SetAllButtonsColor 闪一下表示"连上了"。
            // 但那是第二个写入方：引擎每 25ms 用 SetLEDs(掩码) 写同一批灯，
            // 两套 API 各写各的，设备上就是绿→灭→绿 的乱闪，而且退出后还留着
            // "按按钮单独设色"的残留状态，后面的掩码写不一定能清掉。
            // 结论：灯的写入方只能有一个。连通提示改由合成器的自检动画负责（P6）。
        }

        /// <summary>
        /// 设备连接失败：卡片上只放**一行**状态词，明细（含排查步骤）放悬停气泡和「设备诊断」窗口。
        ///
        /// 原来是把整段原因（四五行走 \r\n）直接塞进卡片里，卡片高度是固定的，
        /// 后面的“2) 已安装…3) USB 线…”就被切掉了——用户看到的永远是半句话。
        /// 现在状态词固定一行不裁，悬停给前两行 + 指向诊断窗看全文。
        ///
        /// 1.3.57 起多一个 `st` 参数：原来靠 summary 里有没有"初始化/驱动/出错"来区分
        /// 「设备在、通道不通」和「压根没找到设备」。那是在**拿中文文案当判据**，
        /// 一旦文案跟着语言变，这个判断在英文下必然落空，气泡就会指错排查方向。
        /// 现在由调用方明确给出状态，文字怎么翻都不影响判断。
        /// </summary>
        private void ShowDevFail(DevState st, string summary, string detail)
        {
            // 先备好原因，再设状态词：状态词的气泡是直接从 _devFailText 取的，
            // 反过来写就会挂上上一次的旧原因（或者空）。
            _devFailText = string.IsNullOrEmpty(detail) ? summary : (summary + "\r\n" + detail);
            SetDevStatus(st, C_ErrInk);
            RefreshStatus();   // 让灯效与状态栏当场对上，不用等下一次节拍
        }

        /// <summary>
        /// 设备状态（1.3.57）。
        ///
        /// 为什么要有这个枚举：原来状态词是一段中文，气泡靠 `text.IndexOf("已连接")` 判断该说什么。
        /// 状态词现在要跟着语言变，这些反查在英文下会**全部落空**——不报错、不抛异常，
        /// 只是安静地走到最后那个 `return null`，气泡什么都不显示。
        /// 这是「拿显示名当语义」的又一个版本（1.3.56 的颜色列同理，只是那个是 == ，这个是 IndexOf）。
        /// 现在：文字由状态生成，判断由状态做，两者不再互相依赖。
        /// </summary>
        private enum DevState
        {
            Detecting,
            Connected,
            ConnectedNoButtons,
            InitFailed,
            NotFound,
            Unplugged,
            Disconnected
        }

        /// <summary>写设备状态词，并把「为什么是这样、要动手做什么」挂到悬停气泡上。</summary>
        private void SetDevStatus(DevState st, Color color)
        {
            // 记下输入：切语言时照原样再调一次就能换语言（见 RefreshRuntimeTexts）。
            // 不记这两个值，状态词和气泡就只认"状态变化"这一个触发点。
            _devState = st;
            _devColor = color;
            _lblDevStatus.Text = L.T("dev.st." + DevStateKey(st));
            HintText.Set(_lblDevStatus, color);
            if (_tip != null) _tip.SetToolTip(_lblDevStatus, DevStatusTip(st, color));
        }

        private static string DevStateKey(DevState st)
        {
            switch (st)
            {
                case DevState.Detecting: return "detecting";
                case DevState.Connected: return "connected";
                case DevState.ConnectedNoButtons: return "connected.nobtn";
                case DevState.InitFailed: return "initfail";
                case DevState.NotFound: return "notfound";
                case DevState.Unplugged: return "unplugged";
                default: return "disconnected";
            }
        }

        /// <summary>状态词的气泡文案。没有可说的就返回 null（气泡自动消失）。</summary>
        private string DevStatusTip(DevState st, Color color)
        {
            if (color == C_ErrInk && !string.IsNullOrEmpty(_devFailText))
            {
                // 只取前两行：ToolTip 太长会拉成一条横线甚至跑出屏幕
                var lines = _devFailText.Replace("\r\n", "\n").Split('\n');
                string head = lines.Length <= 2
                    ? _devFailText
                    : lines[0] + "\r\n" + lines[1] + "…";
                return head + "\r\n\r\n" + L.T("dev.tip.failtail");
            }

            if (st == DevState.Connected)
            {
                int n = (_session != null && _session.Info != null) ? _session.Info.ButtonCount : 0;
                if (n <= 0) return L.T("dev.tip.nobtn");

                string t = L.T("dev.tip.connected", n);
                if (!_rawInput.Active)
                    t += "\r\n\r\n" + L.T("dev.tip.rawinput");
                return t;
            }

            if (st == DevState.ConnectedNoButtons) return L.T("dev.tip.nobtn");
            if (st == DevState.Detecting) return L.T("dev.tip.detecting");
            if (st == DevState.Unplugged) return L.T("dev.tip.unplugged");
            if (st == DevState.Disconnected) return L.T("dev.tip.disconnected");

            return null;
        }

        // ================= 轮询 =================

        private void PollTick()
        {
            try
            {
                PollTickCore();
            }
            catch (Exception ex)
            {
                _pollError = ex.GetType().Name + ": " + ex.Message
                    + " @ " + (ex.StackTrace ?? "").Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)[0];
            }
        }

        private void PollTickCore()
        {
            if (!_session.Connected) return;

            // 两条读按钮的通道：DirectInput 主 + 原始输入兜底（绕过 DI 的格式映射）。
            bool[] di = _session.GetButtons(128);
            bool[] raw = _rawInput.Active ? _rawInput.CurrentButtons(128) : new bool[128];
            bool[] states;
            string channel;
            if (AnyPressed(di)) { states = di; channel = "DirectInput"; }
            else if (AnyPressed(raw)) { states = raw; channel = L.T("dev.channel.raw"); }
            else { states = di; channel = "DirectInput"; }

            int first = -1;
            for (int i = 0; i < states.Length; i++)
                if (states[i]) { first = i; break; }

            // 预览灯模拟按下的按钮（无物理按下时用于状态条显示）
            int simFirst = -1;
            for (int i = 0; i < _virtualButtons.Length; i++)
                if (_virtualButtons[i]) { simFirst = i; break; }

            if (!string.IsNullOrEmpty(_pollError))
                _lblPressed.Text = L.T("main.pressed.pollerr", _pollError);
            else if (!string.IsNullOrEmpty(_session.ReadError))
                _lblPressed.Text = L.T("main.pressed.readerr", _session.ReadError);
            else if (first >= 0)
                _lblPressed.Text = L.T("main.pressed.btn", first, channel);
            else if (simFirst >= 0)
                _lblPressed.Text = L.T("main.pressed.sim", simFirst);
            else
                _lblPressed.Text = L.T("main.pressed.channel", channel);

            if (_capturing)
            {
                for (int i = 0; i < states.Length && i < _prevButtons.Length; i++)
                {
                    if (states[i] && !_prevButtons[i])
                    {
                        AssignCaptured(i);
                        break;
                    }
                }
                Array.Copy(states, _prevButtons, Math.Min(states.Length, _prevButtons.Length));
                return;
            }

            // 合并模拟按钮（点击预览灯 = 模拟按下对应油门按钮，仅参与灯光驱动，不参与捕获）
            for (int i = 0; i < states.Length; i++)
                if (_virtualButtons[i]) states[i] = true;

            // 只有简单模式才由按钮驱动灯光；SimConnect 模式由状态条件驱动
            if (_activeMode == 0) UpdateBindingOverlays(states);

            Array.Copy(states, _prevButtons, Math.Min(states.Length, _prevButtons.Length));
        }

        private static bool AnyPressed(bool[] states)
        {
            for (int i = 0; i < states.Length; i++)
                if (states[i]) return true;
            return false;
        }

        private void UpdateBindingOverlays(bool[] states)
        {
            foreach (var b in _buttons.Bindings)
            {
                if (!b.Enabled || b.ButtonIndex < 0 || b.ButtonIndex >= states.Length) continue;
                bool pressed = states[b.ButtonIndex];
                _bindingOverlays.TryGetValue(b, out var ov);

                if (b.Trigger == "toggle")
                {
                    // 只在"松开→按下"那一帧翻转；按住不放不会来回跳
                    if (!pressed || _prevButtons[b.ButtonIndex]) continue;
                    b.RuntimeOn = !b.RuntimeOn;
                }
                else
                {
                    b.RuntimeOn = pressed;
                }

                if (b.RuntimeOn)
                {
                    if (ov == null) _bindingOverlays[b] = _engine.AddOverlay(MakeEffect(b), LedLayer.Press);
                }
                else if (ov != null)
                {
                    _engine.RemoveOverlay(ov);
                    _bindingOverlays.Remove(b);
                }
            }
        }

        /// <summary>
        /// 移除并清空所有按键叠加层。
        ///
        /// 抽成一个方法的理由不是省几行，是**清完必须建**这件事只能有一个入口：
        /// 以前有四处各写一遍"移除 + Clear"，其中只有 RebuildBindingOverlays 自己会重建，
        /// 另外三处清完就走，于是 b.RuntimeOn 说"开着"、引擎里却没有那一层——
        /// 表现就是从告警模式切回按键模式后，按钮要按第二遍才生效。
        /// 现在凡是清，都必须紧跟着 mode==0 时调一次 <see cref="RebuildBindingOverlays"/>。
        /// </summary>
        private void ClearBindingOverlays()
        {
            if (_engine != null)
                foreach (var ov in _bindingOverlays.Values) _engine.RemoveOverlay(ov);
            _bindingOverlays.Clear();
        }

        /// <summary>
        /// 按当前绑定表 + 当前按下状态整体重建按下层。
        /// 表格里改了效果/颜色/目标灯，要立刻在灯上体现——不能等下一次按钮跳变，
        /// 否则用户看到的是"改了没反应"。
        /// </summary>
        private void RebuildBindingOverlays()
        {
            ClearBindingOverlays();
            if (_cfg == null) return;

            var states = new bool[_prevButtons.Length];
            Array.Copy(_prevButtons, states, states.Length);
            for (int i = 0; i < states.Length && i < _virtualButtons.Length; i++)
                if (_virtualButtons[i]) states[i] = true;

            foreach (var b in _buttons.Bindings)
            {
                if (!b.Enabled || b.ButtonIndex < 0 || b.ButtonIndex >= states.Length) continue;
                bool on = b.Trigger == "toggle" ? b.RuntimeOn : states[b.ButtonIndex];
                b.RuntimeOn = on;
                if (on) _bindingOverlays[b] = _engine.AddOverlay(MakeEffect(b), LedLayer.Press);
            }
        }

        /// <summary>
        /// 按下反馈的效果：目标为「全部」时原样整帧跑；目标为单颗时，套一层涟漪
        /// —— 被按那颗照它自己的颜色/节奏亮，同时从它出发向两侧扩散一圈琥珀波（320ms）。
        /// </summary>
        private static LedEffect MakeEffect(ButtonBinding b)
        {
            var inner = EffectFactory.Create(b.EffectId, (LedColor)Clamp(b.Color, 0, 3), b.Speed, b.TargetLed);
            if (b.TargetLed < 0) return inner;
            return new PressEffect(inner, b.TargetLed);
        }

        // ================= 捕获 =================

        private void StartCapture()
        {
            if (!_session.Connected)
            {
                MessageBox.Show(this, L.T("main.cap.noconnect"), L.T("common.tip.title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_dgv.SelectedRows.Count == 0 && _dgv.Rows.Count > 0)
                _dgv.Rows[0].Selected = true;
            if (_dgv.SelectedRows.Count == 0)
            {
                MessageBox.Show(this, L.T("main.cap.norow"), L.T("common.tip.title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            _capturing = true;
            // 按钮没了，状态只能挂在标签上：原来靠按钮变字提示"正在等"，现在靠这一行。
            _lblCapture.Text = L.T("main.cap.waiting");
            // 先停上一次的定时器：它到点时只看 _capturing，会把这一次新开的捕获掐掉
            StopCaptureTimer();
            _captureTimer = new Timer { Interval = 10000 };
            _captureTimer.Tick += (s, e) => { StopCaptureTimer(); if (_capturing) EndCapture(L.T("main.cap.timeout")); };
            _captureTimer.Start();
        }

        private void AssignCaptured(int button)
        {
            int rowIdx = _dgv.SelectedRows.Count > 0 ? _dgv.SelectedRows[0].Index : 0;
            if (rowIdx < 0 || rowIdx >= _dgv.Rows.Count) return;
            _suppress = true;
            try
            {
                _dgv.Rows[rowIdx].Cells[BindColBtn].Value = button;
                _dgv.Rows[rowIdx].Cells[BindColName].Value = L.T("main.btn.defaultname", button);
            }
            finally
            {
                _suppress = false;
            }
            EndCapture(L.T("bind.cap.done", button, rowIdx + 1));
            SaveBindingsFromGrid();
            RefreshDuplicateMarks();
        }

        private void EndCapture(string msg)
        {
            _capturing = false;
            StopCaptureTimer();
            _lblCapture.Text = msg;
        }

        /// <summary>停掉并释放捕获超时定时器。开始一次新捕获、结束一次捕获，都要先停掉旧的。</summary>
        private void StopCaptureTimer()
        {
            if (_captureTimer == null) return;
            _captureTimer.Stop();
            _captureTimer.Dispose();
            _captureTimer = null;
        }

        // ================= 引擎与效果 =================

        /// <summary>
        /// 把底灯接到引擎上。底灯只在按键模式（L1 背景层）有意义：
        /// 告警模式必须是 null，否则整帧装饰会盖住告警灯，谁也看不出哪颗是真告警。
        /// </summary>
        private void ApplyBackground()
        {
            if (_activeMode != 0)
            {
                _engine.Background = null;
                return;
            }
            _engine.Background = EffectFactory.Create(
                _buttons.Background.EffectId, (LedColor)Clamp(_buttons.Background.Color, 0, 3),
                _buttons.Background.Speed, -1);
        }

        /// <summary>底灯任一设置变化：立即保存、应用并启动（设置即生效）。</summary>
        private void ApplyBackgroundFromUi()
        {
            if (_suppress) return;
            _buttons.Background.EffectId = EffectFactory.Ids[Clamp(_cboBgEffect.SelectedIndex, 0, EffectFactory.Ids.Length - 1)];
            _buttons.Background.Color = Clamp(_cboBgColor.SelectedIndex, 0, 3);
            _buttons.Background.Speed = SpeedFromCbo();
            PersistConfig();
            ApplyBackground();
            UpdateBgControlState();
            if (_chkMaster.Checked) StartEngine();
        }

        /// <summary>关灯时颜色/速度没有意义，一并置灰，免得用户以为调了没反应。</summary>
        private void UpdateBgControlState()
        {
            bool on = _cfg != null && EffectFactory.Normalize(_buttons.Background.EffectId) != "off";
            bool master = _chkMaster == null || _chkMaster.Checked;
            bool enable = on && master;
            if (_cboBgColor != null) _cboBgColor.Enabled = enable;
            if (_cboBgSpeed != null) _cboBgSpeed.Enabled = enable;
            if (_lblSpd != null) _lblSpd.Enabled = enable;
        }

        private double SpeedFromCbo()
        {
            int i = Clamp(_cboBgSpeed.SelectedIndex, 0, 3);
            return SpeedSteps[i];
        }

        private void ToggleMaster()
        {
            _cfg.MasterOn = _chkMaster.Checked;
            PersistState();

            // 关总开关只"静音"，不停引擎：停掉之后预览画面会冻在最后一帧，
            // 用户改任何设置都看不到反应，分不清是没生效还是引擎停了。
            // 静音后每帧输出全灭，设备上的灯照样灭，overlay 与配置全部保留，
            // 再打开开关立刻恢复，不用重新点一遍。
            _engine.Muted = !_chkMaster.Checked;

            if (_chkMaster.Checked)
            {
                ApplyBackground();
                StartEngine();
                if (_activeMode == 1) EvaluateAlerts(true);
            }

            UpdateMasterState();
            RefreshStatus();   // 状态栏要跟着说清"灯不输出"，不能还写着"循环提示中"
        }

        private void UpdateMasterState()
        {
            if (_lblMasterSt == null) return;
            _lblMasterSt.Text = L.T(_chkMaster.Checked ? "main.master.running" : "main.master.off");
            HintText.Set(_lblMasterSt, _chkMaster.Checked ? C_OkInk : C_ErrInk);
            UpdateBgControlState();   // 总开关关掉时底灯控件一起置灰（不自相矛盾）
        }

        private void StartEngine()
        {
            _engine.Start();
            UpdateMasterState();
        }

        private void UpdateIndicators(LedFrame f)
        {
            if (_leds == null || _leds.Length != 8) return;
            // 预览灯颜色一律跟随引擎渲染帧（即配置里效果的实际输出），不再被点击强行改色
            for (int i = 0; i < 8 && i < f.C.Length; i++)
                _leds[i].LedColorValue = f.C[i];
        }

        /// <summary>
        /// 模拟按下/松开预览灯对应油门按钮（P1..P8 ↔ 按钮#4..#11）。
        /// 只改按钮状态（由引擎按配置里的绑定渲染灯光），不直接改灯色。
        /// </summary>
        private void SetPreviewPress(int idx, bool down)
        {
            if (_chkMaster == null || _leds == null) return;
            if (idx < 0 || idx >= _leds.Length) return;
            if (_previewHeld[idx] == down) return;
            int btn = 4 + idx;
            _previewHeld[idx] = down;
            _virtualButtons[btn] = down;
            _leds[idx].Pressed = down;
            if (down && _chkMaster.Checked && !_engine.Running) StartEngine();

            // 未连接设备时也立即驱动一次（纯预览调试用）
            if (!_session.Connected && _activeMode == 0)
            {
                var st = new bool[_virtualButtons.Length];
                for (int i = 0; i < st.Length; i++) st[i] = _virtualButtons[i];
                UpdateBindingOverlays(st);
                Array.Copy(st, _prevButtons, Math.Min(st.Length, _prevButtons.Length));
            }
        }

        // ================= 模式切换（立即生效） =================

        /// <summary>
        /// 底部「当前模式」那一行。文字只由 <see cref="_activeMode"/> 决定，
        /// 所以切页签与切语言都调它——原先这句只写在 SwitchMode 里，
        /// 切语言时没人再写，底部就一直停在旧语言（1.3.63 截图）。
        /// </summary>
        private void UpdateModeLabel()
        {
            if (_lblMode == null) return;
            _lblMode.Text = L.T("main.lbl.mode",
                _activeMode == 0 ? L.T("main.tab.buttons") : L.T("main.tab.alert", DataSourceName));
        }

        private void SwitchMode(int mode)
        {
            if (_cfg == null) return; // 初始化期防御（设计器/构造早期事件）
            if (_activeMode == mode) return;
            _activeMode = mode;
            if (_tabs.SelectedIndex != mode) _tabs.SelectedIndex = mode;

            // 切模式时把"按住预览"状态清干净：告警模式不再产生按下，
            // 若残留 true，切回按键模式后第一下会被 _previewHeld 的相等判断吞掉。
            for (int i = 0; i < _previewHeld.Length; i++)
            {
                if (!_previewHeld[i]) continue;
                _previewHeld[i] = false;
                _virtualButtons[4 + i] = false;
                if (_leds != null && _leds[i] != null) _leds[i].Pressed = false;
            }

            // 立即生效：清空两种模式的叠加，切到谁就重建谁（互斥，避免两套规则同时点灯）。
            // "重建"这件事原来漏了——只清不建，切回按键模式时 toggle 开着的按钮
            // 在引擎里没有层，用户要按第二遍才亮（1.3.34 修的就是这个）。
            ClearBindingOverlays();
            ClearAlertOverlays();
            _alertHitKey = "";

            // 模式名直接复用页签文案（main.tab.*）：两处各写一遍，改词就会漏一处
            UpdateModeLabel();

            // 告警模式必须关掉按键模式的底灯：那是整帧铺满 8 颗灯的装饰动画，
            // 留着就会"底下一圈绿灯在跑、上面盖着告警"，谁也看不懂哪颗是真告警。
            _engine.AlarmMode = mode == 1;
            ApplyBackground();          // 内部按模式决定是否真的挂背景

            // 切回按键模式：按 RuntimeOn 把"开着"的按钮层重新挂回去。
            // 开关状态是用户设的，切页不该替他关掉；层跟状态一致了，按一下就是一下。
            if (mode == 0) RebuildBindingOverlays();

            // 模式提示：顺时针 = 按键模式，逆时针 = 告警模式（1.2s，播完自动回落到常态）
            _engine.PlaySysAnim(mode == 0 ? SysAnim.ModeKey : SysAnim.ModeAlarm);

            UpdateLedLabels();          // 灯上文字随模式换（P1..P8 ↔ 系统名）

            if (mode == 1)
            {
                RefreshAlertCounts();
                EvaluateAlerts(true);
                // 第一次进告警页才还原滚动/选中：表格要真的显示出来，位置才设得进去
                if (!_simViewRestored) { _simViewRestored = true; RestoreGridState(); }
            }

            if (_cfg.LastMode != mode)
            {
                _cfg.LastMode = mode;
                PersistState();            // 下次启动停在同一页，调告警规则时少点一次
            }

            RefreshStatus();
            UpdateDupStatus();
        }

        // ---- 开机序列：自检 960ms → 模式提示 1200ms → 常态 ----

        private readonly Queue<SysAnim> _bootQueue = new Queue<SysAnim>();
        private Timer _bootTimer;
        private bool _bootActive;

        /// <summary>连上设备后播一遍。手动"重新连接"也会走这里（重播），平时不会自己触发。</summary>
        private void StartBootSequence()
        {
            if (_engine == null || _chkMaster == null || !_chkMaster.Checked) return;

            _bootQueue.Clear();
            _bootQueue.Enqueue(SysAnim.SelfTest);
            _bootQueue.Enqueue(_activeMode == 0 ? SysAnim.ModeKey : SysAnim.ModeAlarm);

            if (_bootTimer == null)
            {
                _bootTimer = new Timer { Interval = 1000 };
                _bootTimer.Tick += (s2, e2) => NextBootAnim();
            }

            _bootActive = true;
            NextBootAnim();
        }

        private void NextBootAnim()
        {
            if (_bootQueue.Count == 0)
            {
                _bootActive = false;
                if (_bootTimer != null) _bootTimer.Stop();
                RefreshStatus();     // 自检/模式提示播完，立刻把常态（含循环提示）接上
                return;
            }

            var a = _bootQueue.Dequeue();
            _engine.PlaySysAnim(a);

            // 上一条播完再切下一条：留 40ms 余量，避免两条动画在同一定时器节拍里打架
            int ms = (int)Math.Round(LedCompositor.AnimDuration(a) * 1000) + 40;
            _bootTimer.Interval = Math.Max(200, ms);
            _bootTimer.Start();
        }

        // ================= 状态与提示：一处判定，处处派生 =================

        /// <summary>
        /// 唯一的状态判定入口。所有提示（状态栏 / 数据源卡片 / 灯效）
        /// 都从这一个结果派生 —— 以后多一种状态，只改 <see cref="RunStatus"/>，
        /// 不会出现"灯在转圈、某处提示还写着全灭"这种自相矛盾。
        /// </summary>
        private RunStatus CurrentStatus()
        {
            return RunStatus.Evaluate(
                _cfg != null && _cfg.MasterOn,
                _activeMode == 1,
                _cfg != null && _cfg.SimConnectEnabled,
                _sim != null && _sim.IsRunning,
                _simStatus != null && _simStatus.Connected,
                HasFreshData(),
                DataSourceName,
                _simStatus != null ? _simStatus.LastError : "",
                CurrentPhase(),
                _simCard != null ? _simCard.Text : null,   // 提示里的"去哪点"用控件自己的文字
                _chkSim != null ? _chkSim.Text : null);    // 开关名同理，不另写一份
        }

        /// <summary>
        /// 数据源现在可不可信 —— 唯一的判定点，状态栏和点灯都从这里派生。
        /// 只问数据源「连着吗 / 给过就绪信号吗 / 信号说什么」，不关心它是 MSFS 还是 DCS。
        /// </summary>
        private DataSourcePhase CurrentPhase()
        {
            return DataSourceGate.Evaluate(
                _simStatus != null && _simStatus.Connected,
                _sim != null && _sim.SimEventSeen,
                _sim != null && _sim.SimRunning);
        }

        /// <summary>
        /// 数据是不是"活的"：既要收到过帧，还要在 3.5 秒内收到过新的。
        /// 只看"有没有收到过"不够——模拟器退出/数据流断掉时，最后一帧会一直留着，
        /// 灯会按过期数据点亮，看着就像一切正常。
        /// </summary>
        private bool HasFreshData()
        {
            if (_simState == null) return false;
            return unchecked(Environment.TickCount - _lastFrameAt) < 3500;
        }

        /// <summary>
        /// 刷新所有提示与灯效。任何会影响状态的动作（切页、开关、连断、来数据、定时器兜底）
        /// 都只调这一个方法，保证"没有哪一处提示被落下"。
        /// </summary>
        private void RefreshStatus()
        {
            if (_engine == null || _cfg == null) return;

            var s = CurrentStatus();

            // 1) 状态栏：数据源（短文案，保证一格放得下；完整解释在悬停里）
            if (_lblSource != null)
            {
                _lblSource.Text = s.SourceText;
                HintText.Set(_lblSource, StatusColor(s));
                if (_tip != null) _tip.SetToolTip(_lblSource, s.SourceTip);
            }

            // 2) 中栏数据源卡片：只写开关与连接结论（原因在状态栏，不在这里复述）
            if (_lblSimStatus != null)
            {
                _lblSimStatus.Text = s.CardText;
                HintText.Set(_lblSimStatus, StatusColor(s));
                // 卡片的完整解释挂在气泡上：原来这段只说给状态栏那一格，
                // 用户盯着卡片时反而什么也问不到。清单取消后这里就是数据源消息的落点。
                if (_tip != null) _tip.SetToolTip(_lblSimStatus, s.SourceTip);
            }

            // 3) 灯效
            ApplyLampAnim(s);

            // 4) 告警页页脚（静态说明 / 一次性反馈）
            RefreshAlertFooter();

            // 5) 设置卡片底部的「保存状态」行不在这里刷：
            //    它对应的是一次保存动作的结果，不是持续变化的运行状态。
            //    只在 PersistState / PersistConfig 里更新。
        }

        /// <summary>
        /// 状态 → 文字颜色。系统默认=未启用/未启动（不需要提醒），橙=在等，绿=正常，红=异常。
        /// DataOff 原先用灰字，1.3.49 起文字只有四色，中性状态直接回系统默认。
        /// </summary>
        private Color StatusColor(RunStatus s)
        {
            if (s.State == RunState.Ok) return C_OkInk;
            if (s.State == RunState.DataOff) return SystemColors.ControlText;
            if (s.State == RunState.NotStarted) return C_ErrInk;
            if (s.State == RunState.Standby) return C_OkInk;   // 等待中也算正常，不用警告色
            return C_WarnInk;
        }

        /// <summary>
        /// 灯效：告警模式下只要不是"有数据"就红色反向环行，一直转。
        /// 不区分是联动没开、正在找模拟器、还是连上了没帧——原因交给文字，灯只表达"不正常"。
        /// </summary>
        private void ApplyLampAnim(RunStatus s)
        {
            _engine.AlarmMode = _activeMode == 1;

            // 开机序列（自检 → 模式提示）播完之前不抢，
            // 否则连上设备的瞬间正好"没数据"，用户看到的是红色环行而不是自检。
            if (_bootActive) return;

            if (_activeMode != 1)
            {
                if (IsHintAnim(_engine.Anim)) _engine.StopSysAnim();
                return;
            }

            // 一次性提示（模式切换 1.2s、自检）正在播，先让它播完再接管。
            // 这条不能靠"状态没变就不重播"来省事：一次性动画播完后必须有人再来一次，
            // 否则提示就永远停在"切页动画"，看起来就是没有循环提醒。
            if (_engine.Anim != SysAnim.None && !_engine.AnimLoops)
            {
                // 在它播完的那一刻精确接上循环提示，中间不留"全黑"的空档
                if (s.LampLoop || s.LampStandby) ScheduleLampHandoff();
                return;
            }

            // 等待中：模拟器在菜单/加载画面。绿色反向三灯追，表示"工作正常、在等"。
            // 放在红色警示之前判：这不是故障，不该用环行红色吓人。
            if (s.LampStandby)
            {
                _engine.SetSysAnimLoop(SysAnim.Standby);
                return;
            }

            if (!s.LampLoop)
            {
                if (IsHintAnim(_engine.Anim)) _engine.StopSysAnim();
                return;
            }

            _engine.SetSysAnimLoop(SysAnim.Offline);
        }

        // 一次性动画 → 循环提示的交接定时器
        private Timer _lampHandoff;

        /// <summary>
        /// 按"这一条一次性动画还剩多久"定一个点，到点重算状态。
        /// 光靠 400ms 兜底会出现最长 0.4 秒的全黑空档，看起来像卡了一下。
        /// </summary>
        private void ScheduleLampHandoff()
        {
            if (_lampHandoff == null)
            {
                _lampHandoff = new Timer { Interval = 100 };
                _lampHandoff.Tick += (s2, e2) => { _lampHandoff.Stop(); RefreshStatus(); };
            }

            double left = LedCompositor.AnimDuration(_engine.Anim)
                        - (_engine.Now - _engine.Compositor.AnimStart);
            _lampHandoff.Interval = (int)Math.Max(60, left * 1000 + 50);
            _lampHandoff.Stop();
            _lampHandoff.Start();
        }

        // ---- 一次性反馈（6 秒后自动回到静态说明，由 400ms 兜底定时器清理） ----

        private string _flashText = "";
        private int _flashUntil;

        /// <summary>播一条一次性反馈（切换配置、增删规则、条件改完等）。</summary>
        private void FlashNote(string text, int ms = 6000)
        {
            if (_cfg == null) return;
            // 按键页有自己的动作反馈行；告警页放在页脚，6 秒后自动回到静态说明
            if (_activeMode == 0 && _lblCapture != null)
            {
                _lblCapture.Text = text ?? "";
                return;
            }
            _flashText = text ?? "";
            _flashUntil = Environment.TickCount + ms;
            RefreshStatus();   // 动作刚发生，页脚与状态栏要当场对上
        }

        /// <summary>取出仍在有效期内的反馈文本；过期的顺手清掉。</summary>
        private string TakeFlash()
        {
            if (_flashText.Length == 0) return "";
            if (Environment.TickCount - _flashUntil < 0) return _flashText;
            _flashText = "";
            return "";
        }

        /// <summary>
        /// 告警页页脚：只放"这是什么 / 怎么操作"的静态说明，以及最近一次动作的反馈。
        /// 状态类的信息一律不进这里（状态栏已有一处），避免同一件事两种说法。
        /// </summary>
        private void RefreshAlertFooter()
        {
            if (_lblSimHint2 == null || _cfg == null || _alert == null) return;

            // 内置配置：页脚右侧归 UpdateBuiltInHints 管（只读橙字 / 改动被拦红字），
            // 这里必须先让位——不管是"改过"还是"只是切过来"。
            //
            // 只挡"改过"那一档是不够的：没改动时下面那行"配置：xxx（N 条启用）"
            // 照样会把橙字顶掉，只是它开机就被顶掉、没人看见。红字之所以被注意到，
            // 是因为它是用户动手那一刻才写进去的，肉眼能看到它闪一下。
            if (CurrentIsBuiltIn(true))
            {
                UpdateBuiltInHints();
                return;
            }

            string flash = TakeFlash();
            if (flash.Length > 0)
            {
                _lblSimHint2.Text = flash;
                HintText.Set(_lblSimHint2, C_WarnInk);
                return;
            }

            _lblSimHint2.Text = L.T("alert.foot.profile", TextNames.ProfileName(_alert), _alert.EnabledCount);
            HintText.Set(_lblSimHint2, C_WarnInk);
        }

        private static bool IsHintAnim(SysAnim a)
        {
            return a == SysAnim.Offline || a == SysAnim.Searching || a == SysAnim.Standby;
        }

        // ================= SimConnect（MSFS2024 数据读取） =================

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // 引擎不需要设备也能跑：它的帧还负责驱动屏幕上的 8 个预览灯。
            // 总开关关闭时也不停引擎，只静音（输出全灭）——
            // 停掉的话预览会冻在最后一帧，用户改设置看不到任何反应。
            _engine.Muted = !_cfg.MasterOn;
            if (!_engine.Running) StartEngine();

            _fitBindWidth = -1;
            _fitAlertWidth = -1;
            AutoFitAllGrids();

            // 窗口已经显示出来了，表格才有"显示中的行"，这时还原滚动位置才有效
            if (_activeMode == 1) { _simViewRestored = true; RestoreGridState(); }

            // 上次关程序时开着的实时数据窗，原样再开出来
            if (_cfg.SimDataVisible) btnSimData_Click(null, EventArgs.Empty);

            if (_cfg.SimConnectEnabled) StartSim();
            RefreshStatus();
        }

        /// <summary>
        /// 把相机状态表交给数据源（配置缺失时 <see cref="CameraStateJudge.Build"/> 会给默认整套）。
        /// 只有两处会改它：启动时、载入配置后（配置不含这一项，必须原样留住）。
        /// </summary>
        /// <summary>
        /// 「记录场景状态」勾选：立即生效并落盘。
        /// 立即生效很重要——用户是"重现问题的同时"去勾的，不能要求先关再开程序。
        /// </summary>
        private void chkSceneLog_CheckedChanged(object sender, EventArgs e)
        {
            if (!_sceneLogReady) return;   // 界面初始化期（设置初始值）不写配置
            SceneStateLog.Enabled = _chkSceneLog.Checked;
            _cfg.SceneStateLog = _chkSceneLog.Checked;
            PersistState();
            UpdateSceneLogUi();
        }

        /// <summary>勾选框自己兼作状态提示：勾着就是录着，顺便把文件位置写在上面。</summary>
        private void UpdateSceneLogUi()
        {
            bool on = _chkSceneLog.Checked;
            _chkSceneLog.Text = on ? L.T("main.scene.on") : L.T("main.scene.off");
            _chkSceneLog.ForeColor = on ? C_WarnInk : SystemColors.ControlText;
        }

        private void ApplyCameraStates()
        {
            if (_sim == null) return;
            int dropped;
            var raw = _cfg != null ? _cfg.CameraStates : null;
            _sim.CameraTable = CameraStateJudge.Build(raw, out dropped);
        }

        private void StartSim()
        {
            if (_sim == null) return;
            if (!_sim.IsRunning) _sim.Start();   // 卡片文字由 RefreshStatus 统一给
            RefreshStatus();
        }

        private void ApplySimConnect()
        {
            if (_suppress) return; // 初始化同步期间不触发连接
            _cfg.SimConnectEnabled = _chkSim.Checked;
            PersistState();

            if (_chkSim.Checked)
            {
                // “应用并连接”：断开后重新走一遍定位+连接，便于换飞机/重开模拟器
                if (_sim.IsRunning) _sim.Restart();
                else StartSim();
            }
            else
            {
                if (_sim != null) _sim.Stop();
                _simState = null;
                _simStatus = null;          // 关掉就别再拿旧的连接结论说事
                ClearAlertOverlays();
                _alertHitKey = "";
                UpdateSimDataLabel();       // 空闲那句（"未启用…"）由它统一写
            }
            RefreshStatus();
        }

        /// <summary>连接状态变化（已在 UI 线程）。</summary>
        private void OnSimStatus(SimStatus st)
        {
            if (st == null) return;
            _simStatus = st;

            if (!st.Connected)
            {
                // 断线/未连接：丢掉旧帧，灯回到“仅默认效果”，避免按过期数据点灯
                _simState = null;
                if (_activeMode == 1) EvaluateAlerts(true);
            }
            if (string.IsNullOrEmpty(_simDiagText) || !st.Connected) UpdateSimDataLabel();

            // 卡片文字不再直接抄 st.Headline：同一件事只允许有一种说法，
            // 这里只更新状态数据，文字统一由 RefreshStatus 生成。
            RefreshStatus();
        }

        /// <summary>收到一帧模拟器状态（已在 UI 线程）。</summary>
        private void OnSimState(FlightState st)
        {
            if (st == null) return;

            bool firstFrame = _simState == null;   // 从“没数据”到“有数据”，状态要整体翻一遍
            _simState = st;
            _lastFrameAt = Environment.TickCount;  // 判"数据是否还在刷新"用

            // 就绪状态变了（菜单 → 飞行 / 飞行 → 菜单）要立刻整体翻一遍：
            // 否则灯会停在等待动画上最多 400ms，看着像“进了飞行灯还没醒”。
            var ph = CurrentPhase();
            bool phaseChanged = ph != _lastPhase;
            _lastPhase = ph;

            UpdateSimDataLabel();
            if (firstFrame || phaseChanged) { RefreshAlertCounts(); RefreshStatus(); }
            if (_activeMode == 1) EvaluateAlerts(false);
        }

        private void ClearAlertOverlays()
        {
            for (int i = 0; i < _alertOverlays.Count; i++) _engine.RemoveOverlay(_alertOverlays[i]);
            _alertOverlays.Clear();
            for (int i = 0; i < 8; i++) _ledRules[i] = null;
        }

        /// <summary>
        /// 告警判定：每颗灯只显示「优先级最高的那条命中规则」。
        /// 按灯位归并（不是按行序叠），所以改灯位映射后行为跟着变；
        /// 未命中（含三态"未知"）一律不点灯——未知不是假，也不是真。
        /// 命中指纹不变就不重建，避免闪烁类效果每帧重启相位。
        /// </summary>
        private void EvaluateAlerts(bool force)
        {
            if (_activeMode != 1 || _cfg == null || _alert == null)
            {
                ClearAlertOverlays();
                _alertHitKey = "";
                return;
            }

            // 数据源明确说「不在飞行」（菜单/加载/暂停）时，收到的数值不可信，不能拿来点灯。
            // 这是唯一一次拦，不在 55 条规则里各加前提——加在规则里就会漏。
            // Unknown（数据源不提供该信号）走 AcceptsData 放行，退回旧行为：
            // 宁可在不派发信号的模拟器上照常按数据判定，也不能让正常飞行熄灯。
            if (!DataSourceGate.AcceptsData(CurrentPhase()))
            {
                ClearAlertOverlays();
                _alertHitKey = "";
                return;
            }

            // 规则表要含用户新增的规则：只传 AlertRuleCatalog.Rules 会让自定义规则永远不亮，
            // 而它照样出现在表格里、照样被规则体检分析——"界面有、灯没有"是最难查的那种错。
            var rules = _alert.AllRules();
            int[] hitPerLed = AlertResolver.Resolve(_alert, _simState, rules);

            var keyParts = new string[8];
            for (int i = 0; i < 8; i++) keyParts[i] = hitPerLed[i].ToString();
            string key = string.Join(",", keyParts);
            if (!force && key == _alertHitKey) return;
            _alertHitKey = key;

            ClearAlertOverlays();
            for (int led = 0; led < 8; led++)
            {
                if (hitPerLed[led] < 0) continue;
                var r = rules[hitPerLed[led]];
                _ledRules[led] = r;
                // 颜色/效果/速度必须取生效值：用户在表格里改过样式，存的是覆盖，
                // 直接读 r.EffectId / r.Color / r.Speed 拿到的是内置原值 —— 表格显示新色、
                // 灯还是旧色，正是"界面说一套、硬件做一套"。
                var e = EffectFactory.Create(_alert.EffectOf(r),
                                             (LedColor)Clamp(_alert.ColorOf(r), 0, 3),
                                             _alert.SpeedOf(r), led);
                _alertOverlays.Add(_engine.AddOverlay(e, LedLayer.Content));
            }

            UpdateLedHints();
            if (_cfg.MasterOn && _alertOverlays.Count > 0 && !_engine.Running) StartEngine();
        }

        private void UpdateSimDataLabel()
        {
            if (_lblSimData == null) return;

            var f = _simState;
            if (f == null)
            {
                // 联动没开：说清"是没开"。这句原先写死在 ApplySimConnect 的分支里，
                // 关掉联动之后再切语言就没人重算它，卡片会停在旧语言（1.3.63）。
                // 收进这里以后，"没开"和"开了没数据"两种空闲态由同一个函数分流，
                // 也不会出现两句话互相覆盖。
                if (_chkSim != null && !_chkSim.Checked)
                {
                    _lblSimData.Text = L.T("sim.idle.hint", _chkSim.Text);
                    return;
                }
                // 卡片这里只说"没有数据"，原因统一在状态栏那一条上说（不重复两份说法）
                string t = L.T("main.sim.nodata");
                if (_simDiagText.Length > 0) t += "\r\n" + _simDiagText;
                _lblSimData.Text = t;
                return;
            }

            int eng = (int)Math.Round(f.N("NUMBER OF ENGINES", 1));
            if (eng < 1) eng = 1; if (eng > 4) eng = 4;
            int running = 0;
            double maxThr = 0, maxRpm = 0;
            for (int i = 1; i <= eng; i++)
            {
                if (f.B("GENERAL ENG COMBUSTION:" + i)) running++;
                maxThr = Math.Max(maxThr, f.N("GENERAL ENG THROTTLE LEVER POSITION:" + i));
                maxRpm = Math.Max(maxRpm, f.N("GENERAL ENG RPM:" + i));
            }

            double cap = f.N("FUEL TOTAL CAPACITY");
            double fuelPct = cap > 0.0001 ? f.N("FUEL TOTAL QUANTITY") / cap * 100.0 : 0;

            string state = f.B("SIM ON GROUND") ? L.T("main.sim.ground") : L.T("main.sim.air");
            double vs = f.N("VERTICAL SPEED");

            // 这张卡是"一眼看飞行状态"，中文靠顿号与空格断句，英文靠逗号和句点。
            // 所以每一行都是**整句带占位符**，不做"中文前缀 + 值 + 中文后缀"的拼接——
            // 那样英文语序一定错（"Airspeed 120 kn · Altitude 3000 ft" 通，反过来就不通）。
            _lblSimData.Text =
                L.T("main.sim.line1", Num(f.N("AIRSPEED INDICATED")), Num(f.N("PLANE ALTITUDE")),
                    (vs >= 0 ? "+" : ""), Num(vs), state, f.N("G FORCE").ToString("0.0")) + "\r\n"
                + L.T("main.sim.line2", eng, running, Num(maxThr), Num(maxRpm)) + "\r\n"
                + (Derived.IsFixedGear(f)
                    ? L.T("main.sim.gear.fixed")
                    : L.T("main.sim.gear", f.B("GEAR HANDLE POSITION") ? L.T("main.sim.geardown") : L.T("main.sim.gearup"),
                          Num(f.N("GEAR TOTAL PCT EXTENDED"))))
                + L.T("main.sim.line3", Num(f.N("FLAPS HANDLE PERCENT")), Num(fuelPct),
                      f.B("AUTOPILOT MASTER") ? L.T("main.sim.on") : L.T("main.sim.off"))
                + (f.B("STALL WARNING") ? L.T("main.sim.stall") : "")
                + (f.B("OVERSPEED WARNING") ? L.T("main.sim.overspeed") : "");

            if (_sim != null)
                _lblSimData.Text += "\r\n" + L.T("main.sim.replies", _sim.AnsweredCount, SimVars.Count);
        }

        private static string Num(double v)
        {
            return Math.Round(v).ToString("0");
        }

        /// <summary>SDK 检测：定位 dll、检查位数、列出已连接状态与未应答变量。</summary>
        private void CheckSdk()
        {
            MessageBox.Show(this, BuildSdkDiagText(), L.T("main.sdk.title"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            UpdateSimDataLabel();
        }

        /// <summary>生成 SimConnect 诊断文本（弹窗与诊断报告共用）。</summary>
        private string BuildSdkDiagText()
        {
            string path, log;
            bool found = SimConnectLocator.TryFind(out path, out log);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(L.T("main.sdk.title"));
            sb.AppendLine("────────────────────────");
            sb.AppendLine(found ? L.T("main.sdk.found", System.IO.Path.GetFileName(path))
                                : L.T("main.sdk.notfound"));
            if (found)
            {
                sb.AppendLine(L.T("main.sdk.path", path));
                sb.AppendLine(L.T("main.sdk.version", DllVersionOf(path)));
                if (path.IndexOf("_internal", StringComparison.OrdinalIgnoreCase) >= 0)
                    sb.AppendLine(L.T("main.sdk.internal"));
            }
            sb.AppendLine();

            var st = _simStatus;
            sb.AppendLine(L.T("main.sdk.conn", st != null ? st.Headline : L.T("main.sdk.conn.pending")));
            if (!string.IsNullOrEmpty(SimVersionOf()))
                sb.AppendLine(L.T("main.sdk.simver", SimVersionOf()));

            int total = SimVars.Count;
            int answered = _sim != null ? _sim.AnsweredCount : 0;
            sb.AppendLine(L.T("main.sdk.vars", answered, total));

            if (_sim != null && answered > 0 && answered < total)
            {
                var miss = _sim.MissingVars();
                if (miss.Count > 0)
                {
                    if (miss.Count > 10) miss.RemoveRange(10, miss.Count - 10);
                    sb.AppendLine(L.T("main.sdk.missing"));
                    foreach (var m in miss) sb.AppendLine("  · " + m);
                }
            }
            if (st != null && !string.IsNullOrEmpty(st.LastError))
                sb.AppendLine(L.T("main.sdk.lasterr", st.LastError));

            sb.AppendLine();
            sb.AppendLine(L.T("main.sdk.logtitle"));
            sb.AppendLine(TrimLog(log ?? SimConnectLocator.LastLog, 24));

            _simDiagText = (found ? L.T("main.sdk.chip.dll", System.IO.Path.GetFileName(path))
                                  : L.T("main.sdk.chip.nodll"))
                + L.T("main.sdk.chip.vars", answered, total);

            return sb.ToString();
        }

        private string SimVersionOf()
        {
            return _sim != null ? _sim.SimVersion : "";
        }

        private static string DllVersionOf(string path)
        {
            try
            {
                var vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(path);
                string v = vi.FileVersion;
                return string.IsNullOrEmpty(v) ? L.T("main.ver.unknown") : v;
            }
            catch (Exception) { return L.T("main.ver.unknown"); }
        }

        private static string TrimLog(string log, int maxLines)
        {
            if (string.IsNullOrEmpty(log)) return L.T("common.none");
            var lines = log.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length <= maxLines) return log;
            return string.Join("\r\n", lines, 0, maxLines) + "\r\n" + L.T("common.trunc", lines.Length - maxLines);
        }

        // ================= 配置管理（buttons\ / alerts\：一套配置一个文件） =================

        private void RefreshPresetCombos()
        {
            FillPresetCombo(_cboPreset1, ProfileStore.ListButtons(),
                            _buttons != null ? ProfileStore.KeyOf(_buttons) : "", _lblChip1);
            FillPresetCombo(_cboPreset2, ProfileStore.ListAlerts(),
                            _alert != null ? ProfileStore.KeyOf(_alert) : "", _lblChip2);
        }

        /// <summary>
        /// 配置下拉是只读 DropDownList：只按「当前正在用的那套」回填选中项。
        /// 1.3.43 起选中即载入，所以刷列表期间必须 _suppress，否则 Items 变动会连锁触发一次载入；
        /// 也不再保留旧的 ComboBox.Text —— 新建/另存为/重命名后那样会停在上一个名字。
        /// 1.3.48 起 Items 装的是 ProfileEntry 本身，靠身份（Key）回填——显示名可能重名，Key 不会。
        /// </summary>
        private void FillPresetCombo(ComboBox cbo, List<ProfileEntry> entries, string activeKey, Label chip)
        {
            if (cbo == null) return;
            bool was = _suppress;
            _suppress = true;
            try
            {
                cbo.Items.Clear();
                int builtin = 0, mine = 0;
                foreach (var e in entries)
                {
                    // 显示名现取：内置配置的名要跟语言变，「（内置）」前缀也在这里加。
                    // 必须在 Items.Add 之前设好——ComboBox 取的是 ToString()。
                    e.Display = TextNames.ProfileName(e);
                    cbo.Items.Add(e);
                    if (e.IsBuiltIn) builtin++; else mine++;
                }

                int idx = -1;
                if (!string.IsNullOrEmpty(activeKey))
                {
                    for (int i = 0; i < cbo.Items.Count; i++)
                    {
                        var e = cbo.Items[i] as ProfileEntry;
                        if (e != null && string.Equals(e.Key, activeKey, StringComparison.OrdinalIgnoreCase)) { idx = i; break; }
                    }
                }
                cbo.SelectedIndex = idx >= 0 ? idx : (cbo.Items.Count > 0 ? 0 : -1);
                if (chip != null) chip.Text = L.T("main.preset.chip", mine, builtin);
            }
            finally { _suppress = was; }
        }

        // 1.3.43：「保存」「载入」按钮已移除。配置内容本来就由 PersistConfig() 即时写盘，
        // 命名/另存为归「管理 ▾」菜单（NewProfile / SaveProfileAs / RenameProfile），
        // 切换配置由下拉 SelectedIndexChanged 直接触发，不再需要一对按钮。

        /// <summary>
        /// 载入指定身份的配置：内容从文件读（预置也是文件），读失败就保持当前不动。
        /// 身份显式传入，不再从 ComboBox.Text 里猜 —— 新建/另存为/重命名后能准确切到新配置。
        /// 1.3.48 起传的是 Key（"builtin/twin_piston" 或 "我的配置"），不是显示名。
        /// </summary>
        private void LoadPreset(int which, string key)
        {
            if (key == null) return;
            key = key.Trim();
            if (key.Length == 0) return;

            // 初始化/批量刷新期间不许切换：那时界面还在铺数据，
            // 半路的切换会把刚铺好的表格换成另一套内容，看起来就是"我的行没了"。
            if (_suppress) return;

            bool wasSuppress = _suppress;
            _suppress = true;
            try
            {
                if (which == 1)
                {
                    var p = LoadButtonProfile(key, true);
                    if (p == null) return;

                    _buttons = p;
                    _cfg.ActiveButtonFile = ProfileStore.KeyOf(p);

                    _dgv.Rows.Clear();
                    foreach (var b in _buttons.Bindings)
                        _dgv.Rows.Add(b.Enabled, b.ButtonIndex, b.ButtonName, TriggerName(b.Trigger),
                            TextNames.Effect(b.EffectId), TextNames.Color(b.Color),
                            b.Speed.ToString("0.0"), TextNames.Target(b.TargetLed >= 0 ? b.TargetLed + 1 : 0));

                    _cboBgEffect.SelectedIndex = Clamp(
                        IndexOf(EffectFactory.Ids, EffectFactory.Normalize(_buttons.Background.EffectId)),
                        0, EffectFactory.Ids.Length - 1);
                    _cboBgColor.SelectedIndex = Clamp(_buttons.Background.Color, 0, 3);
                    _cboBgSpeed.SelectedIndex = Clamp(IndexOf(SpeedSteps, _buttons.Background.Speed), 0, 3);

                    PersistState();

                    // 这里原来还有一次"移除 + Clear"。没意义：函数末尾 which==1 分支
                    // 会调 RebuildBindingOverlays()，它自己先清再建，重复清只是白跑一遍。
                    _lblCapture.Text = L.T("main.preset.switched.btn", TextNames.ProfileName(_buttons), _buttons.Bindings.Count);
                }
                else
                {
                    var p = LoadAlertProfile(key, true);
                    if (p == null) return;

                    _alert = p;
                    _cfg.ActiveAlertFile = ProfileStore.KeyOf(p);
                    ApplyCameraStates();
                    PersistState();

                    FlashNote(L.T("main.preset.switched.alert", TextNames.ProfileName(_alert), _alert.EnabledCount));
                }
            }
            finally
            {
                _suppress = wasSuppress;
            }
            RefreshDuplicateMarks();
            ApplyAlertConfigToUi();

            // 切到另一套配置，页脚状态要重新算：上一套的"改动被拦下"不该跟到新配置头上。
            if (which == 1) _builtInTouched1 = false; else _builtInTouched2 = false;
            UpdateBuiltInHints();

            if (which == 1)
            {
                // 配置里的底灯要真的挂到引擎上。只改下拉框和配置、不重挂 Background，
                // 灯跑的还是旧效果——这就是"切换了但底灯没变"的真正原因。
                ApplyBackground();
                UpdateBgControlState();
                RebuildBindingOverlays();   // 表格内容也立刻生效，不等下一次按钮跳变
                _fitBindWidth = -1;
                AutoFitAllGrids();
            }

            if (_activeMode == 1) EvaluateAlerts(true);
        }

        // ================= 行操作 =================

        private void AddBindingRow()
        {
            _suppress = true;
            try
            {
                _dgv.Rows.Add(true, 0, L.T("main.btn.defaultname", 0), TextNames.Trigger("toggle"), TextNames.Effect(EffectFactory.Ids[1]), TextNames.Color(1), "1.0", TextNames.Target(0));
                if (_dgv.Rows.Count > 0) _dgv.Rows[_dgv.Rows.Count - 1].Selected = true;
                _lblCapture.Text = L.T("main.bind.added");
            }
            finally { _suppress = false; }
            SaveBindingsFromGrid();     // 增行属于"明确的动作"，当场落盘，不等关程序
            RefreshDuplicateMarks();
        }

        private void RemoveBindingRow()
        {
            int idx = SelectedRowIndex(_dgv);
            if (idx < 0) return;
            _dgv.Rows.RemoveAt(idx);
            // 删完把选中落到相邻行：不然菜单项立刻变灰，想连删还得先用鼠标点一行。
            if (_dgv.Rows.Count > 0)
            {
                int next = Math.Min(idx, _dgv.Rows.Count - 1);
                _dgv.Rows[next].Selected = true;
                _lblCapture.Text = L.T("main.bind.removed", idx + 1);
            }
            else
            {
                _lblCapture.Text = L.T("main.bind.removedlast");
            }
            SaveBindingsFromGrid();
            RefreshDuplicateMarks();
        }

        // ================= 工具函数 =================

        /// <summary>
        /// 表格值一变就把内容搬进内存：预览灯和叠加层立刻按新配置跑，不等写盘。
        /// 这一层不能省——否则"改了一行效果要等编辑结束灯才变"，看着像没反应。
        ///
        /// 叠加层的重建不在这里做：SyncBindingsFromGrid 自己负责"清完就建"，
        /// 这里再建一次是白跑（清空 + 逐条重挂 21 条）。
        /// </summary>
        private void SyncGridToMemory()
        {
            if (_suppress) return;
            SyncBindingsFromGrid();
            RefreshDuplicateMarks();
        }

        /// <summary>同步内存 + 立刻写盘。只在"一次编辑真的结束"和增删行时调。</summary>
        private void CommitGrid()
        {
            if (_suppress) return;
            SyncGridToMemory();
            PersistConfig();
            // 表格里改了目标灯 / 效果 / 颜色，灯上的气泡要跟着变，
            // 否则用户改完还看到旧话，以为没生效。
            UpdateLedHints();
        }

        private static int IndexOf(string[] arr, string v)
        {
            if (string.IsNullOrEmpty(v)) return -1;
            for (int i = 0; i < arr.Length; i++)
                if (arr[i] == v) return i;
            return -1;
        }

        private static int IndexOf(double[] arr, double v)
        {
            for (int i = 0; i < arr.Length; i++)
                if (Math.Abs(arr[i] - v) < 1e-6) return i;
            return -1;
        }

        private static string IdOfEffectName(string name)
        {
            return EffectFactory.IdOfName(name, TextNames.EffectAll());
        }

        private static string TriggerName(string t) => TextNames.Trigger(t);

        private static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);

        private static bool GetBool(object v, bool def)
        {
            if (v is bool b) return b;
            return def;
        }

        // ================= 事件包装（设计器订阅 → 业务方法） =================

        private void cboBgEffect_SelectedIndexChanged(object sender, EventArgs e) => ApplyBackgroundFromUi();
        private void cboBgColor_SelectedIndexChanged(object sender, EventArgs e) => ApplyBackgroundFromUi();
        private void cboBgSpeed_SelectedIndexChanged(object sender, EventArgs e) => ApplyBackgroundFromUi();
        // 初始化同步时 _suppress 为真：那时改 Checked 不是用户在点，不能顺手启停引擎
        private void chkMaster_CheckedChanged(object sender, EventArgs e)
        {
            if (_suppress) return;
            ToggleMaster();
        }
        private void btnReconnect_Click(object sender, EventArgs e)
        {
            if (_session != null && _session.Connected) DisconnectDevice(true);
            // 用户主动点「连接」：interactive=true，认不出就弹框让他从列表里选
            else { _userDisconnected = false; AutoConnectThrottle(true); }
        }

        /// <summary>
        /// 重写「连接 / 断开」按钮与「当前按下」两处文字。
        ///
        /// 原先这两处文字各写在自己的分支里（连上、断开、拔出、用户点断开），
        /// 语言一变就没有任何一条路径会再写一遍，于是界面停在切换前的语言——
        /// 1.3.63 那两张截图（英文界面露中文、中文界面露英文）都是这个原因。
        /// 现在状态变化与切语言都调这一个函数，判断依据只有 <see cref="DeviceSession.Connected"/>
        /// 和 <see cref="_userDisconnected"/>，两处文字与状态因此不会各说各话。
        ///
        /// 连着的时候不碰「当前按下」：那一行由轮询每 100ms 重写，
        /// 这里插一手只会在两次轮询之间闪一个旧值。
        /// </summary>
        private void UpdateLinkTexts()
        {
            if (_btnReconnect != null)
                _btnReconnect.Text = L.T(_session != null && _session.Connected
                    ? "dev.btn.disconnect" : "dev.btn.connect");

            if (_lblPressed != null && (_session == null || !_session.Connected))
                _lblPressed.Text = L.T(_userDisconnected ? "main.pressed.disconnected" : "main.pressed.detached");
        }

        /// <summary>
        /// 用户主动断开：只停"写硬件"，引擎和预览照常跑。
        /// 这样断开状态下仍然能调底灯、改表格、看告警预览，
        /// 只是灯不会再写到设备上（设备本身被释放，硬件自然熄灭）。
        /// </summary>
        private void DisconnectDevice(bool byUser)
        {
            try { _pollTimer.Stop(); } catch (Exception) { }
            // 手动断开要顺手掐掉待触发的自动重连，否则刚点完「断开」又被自己连回来
            try { if (_reconnectTimer != null) _reconnectTimer.Stop(); } catch (Exception) { }
            try { _session.Disconnect(); } catch (Exception) { }

            _userDisconnected = byUser;
            UpdateLinkTexts();
            SetDevStatus(DevState.Disconnected, SystemColors.ControlText);
            _devFailText = "";
            RefreshStatus();
        }
        private void btnDiag_Click(object sender, EventArgs e) => ShowDiagnostics();
        private void btnSimApply_Click(object sender, EventArgs e) => ApplySimConnect();
        private void btnSimCheck_Click(object sender, EventArgs e) => CheckSdk();
        private void tabs_SelectedIndexChanged(object sender, EventArgs e) => SwitchMode(_tabs.SelectedIndex);
        private void miCaptureBinding_Click(object sender, EventArgs e) => StartCapture();
        private void miAddBinding_Click(object sender, EventArgs e) => AddBindingRow();
        private void miDelBinding_Click(object sender, EventArgs e) => RemoveBindingRow();
        /// <summary>配置下拉选中即载入。只读下拉，选中项本身就是名字；_suppress 期间不响应。</summary>
        private void cboPreset1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suppress || _cfg == null || _buttons == null) return;
            var entry = _cboPreset1.SelectedItem as ProfileEntry;
            if (entry == null) return;
            if (string.Equals(ProfileStore.KeyOf(_buttons), entry.Key, StringComparison.OrdinalIgnoreCase)) return;
            LoadPreset(1, entry.Key);
        }

        private void cboPreset2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suppress || _cfg == null || _alert == null) return;
            var entry = _cboPreset2.SelectedItem as ProfileEntry;
            if (entry == null) return;
            if (string.Equals(ProfileStore.KeyOf(_alert), entry.Key, StringComparison.OrdinalIgnoreCase)) return;
            LoadPreset(2, entry.Key);
        }
        // 值一变只同步内存（灯马上反应），编辑结束才写盘。
        // 这样"打个 1.5"不会写三次盘，也不会出现"打了一半就被存进去"。
        private void dgv_CellValueChanged(object sender, DataGridViewCellEventArgs e) => SyncGridToMemory();
        private void dgv_CellEndEdit(object sender, DataGridViewCellEventArgs e) => CommitGrid();
        private void dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e) => CommitGrid();
        private void dgv_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e) => CommitGrid();

        /// <summary>
        /// 行头画行号（按键表、告警表共用）。
        ///
        /// 画在行头、不新增一列：两个表的列索引常量（BindCol* / AlertCol*）和 Cells[n]
        /// 散落在很多地方，插一列要全部改一遍——上一版给告警表加「速度」列就是这么
        /// 把条件列裁掉的。行头不参与列索引，画上去零影响。
        /// </summary>
        private void dgv_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            var g = sender as DataGridView;
            if (g == null || e.RowIndex < 0) return;

            var area = new Rectangle(e.RowBounds.Left, e.RowBounds.Top,
                                     g.RowHeadersWidth - 8, e.RowBounds.Height);

            // 兜底规则整行字色被调淡过，行号跟着淡，看上去才是一整行
            Color ink = g.Rows[e.RowIndex].DefaultCellStyle.ForeColor;
            if (ink.IsEmpty) ink = g.RowHeadersDefaultCellStyle.ForeColor;

            TextRenderer.DrawText(e.Graphics, (e.RowIndex + 1).ToString(),
                g.RowHeadersDefaultCellStyle.Font ?? g.Font, area, ink,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
        }
        private void dgv_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.ColumnIndex == BindColBtn) _lblCapture.Text = L.T("main.btn.tip.edit");
        }
        private void dgvSim_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_cfg == null || _alert == null) return;
            if (e.RowIndex < 0 || e.RowIndex >= _alertRows.Count) return;

            if (e.ColumnIndex == AlertColEnabled) { OnAlertEnabledChanged(e.RowIndex); return; }

            // 颜色 / 效果 / 速度：写成对这条规则的覆盖，立刻重算灯
            if (e.ColumnIndex == AlertColColor || e.ColumnIndex == AlertColEffect || e.ColumnIndex == AlertColSpeed)
                OnAlertStyleChanged(e.RowIndex);
        }

        /// <summary>
        /// 改了颜色 / 效果 / 速度。只写这三个字段（其余仍走内置），所以以后程序调整
        /// 某条规则的条件时，这里改过的观感不会被一起冲掉。
        /// </summary>
        private void OnAlertStyleChanged(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _alertRows.Count) return;
            var r = _alertRows[rowIndex];
            var row = _dgvAlert.Rows[rowIndex];

            int color = Clamp(IndexOf(TextNames.ColorAll(), Convert.ToString(row.Cells[AlertColColor].Value)), 0, 3);
            string effectText = Convert.ToString(row.Cells[AlertColEffect].Value);
            string effect = null;
            foreach (var id in AlertRuleCatalog.AllowedEffects)
                if (TextNames.Effect(id) == effectText) { effect = id; break; }
            if (effect == null) effect = _alert.EffectOf(r);

            double speed;
            if (!double.TryParse(Convert.ToString(row.Cells[AlertColSpeed].Value), out speed) || speed <= 0 || speed > 9.99)
                speed = _alert.SpeedOf(r);

            // 1.3.70：颜色是唯一的关灯开关，效果只作「亮起来后的预设」，任何组合都自洽，
            // 不再需要回滚判据。以前这里要求颜色与效果必须配对，而表格一次只提交一列，
            // 7 条「关/关」内置规则从任一列出发都被回滚，永远改不了颜色和效果。
            _alert.SetStyle(r.Id, color, effect, speed);
            PersistConfig();
            if (_activeMode == 1) EvaluateAlerts(true);
        }

        // ================= 自定义规则（新增 / 删除） =================

        /// <summary>
        /// 右键表格：先把右键那一行选中，让「删除这条规则」知道删谁；空白处右键则只留新增。
        /// 内置规则仍由 btnDelRule_Click 提示"不能删，只能停用"。
        /// </summary>
        private void ctxAlert_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_dgvAlert == null) return;
            SelectHitRow(_dgvAlert);

            // 启用与否看「有没有选中行」，不看右键落点：键盘选中一行后按菜单键
            // （Shift+F10）时落点在表格外，只看落点会让这条菜单在该亮的时候灰掉。
            int row = SelectedRowIndex(_dgvAlert);
            if (_miDelRule != null)
            {
                _miDelRule.Enabled = row >= 0;
                _miDelRule.Text = row >= 0
                    ? L.T("alert.menu.delrow", row + 1)
                    : L.T("alert.menu.delrow.plain");
            }

            // 「编辑条件」与双击「触发条件」列同一条路径。双击是唯一入口，
            // 一旦列号或事件出问题，改条件这条功能就彻底点不到了，所以菜单里留一条明路。
            if (_miEditCond != null)
            {
                _miEditCond.Enabled = row >= 0 && row < _alertRows.Count;
                _miEditCond.Text = _miEditCond.Enabled
                    ? L.T("alert.menu.editcond.row", row + 1)
                    : L.T("alert.menu.editcond.plain");
            }
        }

        /// <summary>右键「编辑本条规则的条件…」：与双击「触发条件」列走同一个方法。</summary>
        private void miEditCond_Click(object sender, EventArgs e)
        {
            if (_dgvAlert == null) return;
            int row = SelectedRowIndex(_dgvAlert);
            if (row < 0) return;
            EditCondition(row);
        }

        /// <summary>
        /// 按键表右键：三段式，与告警页同骨架（读 ── 增 ── 删）。
        /// 「捕获」和「删除」都要有选中行才能用，「新增」始终可用。
        /// </summary>
        private void ctxBinding_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_dgv == null) return;
            SelectHitRow(_dgv);

            int row = SelectedRowIndex(_dgv);
            if (_miDelBind != null)
            {
                _miDelBind.Enabled = row >= 0;
                _miDelBind.Text = row >= 0
                    ? L.T("bind.menu.delrow", row + 1)
                    : L.T("bind.menu.delrow.plain");
            }
            // 捕获要往某一行里写编号，没选中行就没有"这一行"可写。
            // 以前按钮版会自动替你选第一行，结果号码进了不该进的那条，很难发现。
            //
            // 这里不再往文案里塞行号：同一行号在菜单里出现两次（捕获一行、删除一行）
            // 只会吵。行号只挂在删除上——删错了没法撤回，捕获填错了改一格就行。
            if (_miCapture != null) _miCapture.Enabled = row >= 0;
        }

        /// <summary>把鼠标右键落点那一行选中。落在空白处则不动现有选中。</summary>
        private static void SelectHitRow(DataGridView g)
        {
            if (g == null) return;
            var p = g.PointToClient(Cursor.Position);
            var hit = g.HitTest(p.X, p.Y);
            if (hit.RowIndex < 0 || hit.RowIndex >= g.Rows.Count) return;
            g.ClearSelection();
            g.Rows[hit.RowIndex].Selected = true;
            // 两张表的第 0 列都是「启用」，所以这里可以直接用 0，不区分表
            g.CurrentCell = g.Rows[hit.RowIndex].Cells[g.Columns[0].Index];
        }

        /// <summary>
        /// 当前选中行号，-1 表示没选中。两张表都是 MultiSelect=false + FullRowSelect，
        /// 所以最多一行；仍取 [0] 而不是断言只有一行——万一以后开了多选，取第一行也说得通。
        /// </summary>
        private static int SelectedRowIndex(DataGridView g)
        {
            if (g == null || g.SelectedRows.Count == 0) return -1;
            int i = g.SelectedRows[0].Index;
            return i >= 0 && i < g.Rows.Count ? i : -1;
        }

        /// <summary>
        /// 「新增规则…」：默认把用户当前选中的那一行所在系统、且排在它后面，作为预填值。
        /// 想让新规则在某条之前生效，选中它、新增、把"插入位置"改成更前面即可。
        /// </summary>
        private void btnAddRule_Click(object sender, EventArgs e)
        {
            if (_alert == null) return;

            string sysKey = AlertRuleCatalog.Systems[0].Key;
            string afterId = null;
            if (_dgvAlert != null && _dgvAlert.CurrentRow != null)
            {
                int i = _dgvAlert.CurrentRow.Index;
                if (i >= 0 && i < _alertRows.Count)
                {
                    sysKey = _alertRows[i].SystemKey;
                    afterId = _alertRows[i].Fallback ? null : _alertRows[i].Id;
                }
            }

            var f = new NewRuleForm(_alert, sysKey, afterId);
            // 条件编辑要用主窗体的试算数据（模拟器实时值），所以把服务从这里注入
            f.EditCondition = (rule, cur) => AskCondition(rule, cur);

            using (f)
            {
                if (f.ShowDialog(this) != DialogResult.OK) return;

                var rule = _alert.AddCustomRule(f.SystemKey, f.RuleName, f.AfterId,
                                                f.Color, f.Effect, f.Speed, f.Cond);
                if (rule == null)
                {
                    // 1.3.47 起基本走不到这里：空隙用尽会自动重排接手。
                    // 留着是兜底——真出现了说明这个系统里一条可排的规则都没有。
                    MessageBox.Show(this, L.T("alert.rule.nopos"), L.T("alert.rule.nopos.title"),
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                _alert.SetEnabled(rule.Id, true);      // 新增的规则默认启用，否则"加了却不亮"

                PersistConfig();
                ApplyAlertConfigToUi();      // 含重建表格 + 计数 + 页脚，不单点哪几个，免得漏
                SelectAlertRule(rule.Id);
                EvaluateAlerts(true);

                FlashNote(L.T("alert.rule.added", TextNames.RuleDisplay(rule)));
            }
        }

        /// <summary>「删除规则」：只能删自己加的（usr_ 开头），内置规则最多停用。</summary>
        private void btnDelRule_Click(object sender, EventArgs e)
        {
            if (_alert == null || _dgvAlert == null) return;

            int i = _dgvAlert.CurrentRow != null ? _dgvAlert.CurrentRow.Index : -1;
            if (i < 0 || i >= _alertRows.Count)
            {
                MessageBox.Show(this, L.T("alert.rule.del.select"), L.T("alert.rule.del.title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var rule = _alertRows[i];
            if (!_alert.IsCustomRule(rule.Id))
            {
                MessageBox.Show(this,
                    L.T("alert.rule.builtin", TextNames.RuleDisplay(rule)),
                    L.T("alert.rule.builtin.title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(this,
                    L.T("alert.rule.del.confirm", TextNames.RuleDisplay(rule)),
                    L.T("alert.rule.del.title"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

            _alert.RemoveCustomRule(rule.Id);
            PersistConfig();
            ApplyAlertConfigToUi();
            EvaluateAlerts(true);
            FlashNote(L.T("alert.rule.deleted", TextNames.RuleDisplay(rule)));
        }

        /// <summary>
        /// 打开条件编辑器拿一个条件；取消返回 null。
        /// 条件编辑器需要"现在这些变量到底取不取得到"，那只有主窗体知道。
        /// </summary>
        private CondNode AskCondition(AlertRule rule, CondNode current)
        {
            bool simConnected = _simStatus != null && _simStatus.Connected;
            using (var f = new CondEditorForm(rule, current, () => _simState,
                                              n => _simState != null && _simState.Has(n), simConnected))
            {
                if (f.ShowDialog(this) != DialogResult.OK) return null;
                if (f.RestoreDefault || f.Result == null) return null;

                var errs = f.Result.Validate();
                if (errs.Count > 0)
                {
                    MessageBox.Show(this, L.T("main.cond.invalid", string.Join("\r\n· ", errs.ToArray())),
                        L.T("main.cond.invalid.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
                }
                return f.Result;
            }
        }

        /// <summary>把某条规则设为选中并滚到可见处（新增/删除后让用户看到结果）。</summary>
        private void SelectAlertRule(string ruleId)
        {
            if (_dgvAlert == null) return;
            for (int i = 0; i < _alertRows.Count && i < _dgvAlert.Rows.Count; i++)
            {
                if (_alertRows[i].Id != ruleId) continue;
                _dgvAlert.ClearSelection();
                _dgvAlert.Rows[i].Selected = true;
                _dgvAlert.CurrentCell = _dgvAlert.Rows[i].Cells[AlertColRule];
                return;
            }
        }
        // ================= 配置管理（新建 / 另存为 / 重命名 / 删除 / 导入 / 导出） =================
        //
        // 两个页签共用一份逻辑：用 alert 区分操作对象，界面只多一个按钮。
        // 分成两套写法的话，"另存为"改一次就得记得改两处，迟早在某一页漏掉。

        private string CurrentProfileName(bool alert)
        {
            if (alert) return TextNames.ProfileName(_alert);
            return TextNames.ProfileName(_buttons);
        }

        private bool CurrentIsBuiltIn(bool alert)
        {
            if (alert) return _alert == null || _alert.IsBuiltIn;
            return _buttons == null || _buttons.IsBuiltIn;
        }

        // 页脚红字的两个状态位，按页签编号命名：1=按键页，2=告警页。
        // 用「页」而不用 alert 布尔命名，是因为它们服务于两个标签而不是两份数据——
        // 按 alert 命名的话，看 _builtInTouched2 还得回头查它到底是哪一页。
        private bool _builtInTouched1;
        private bool _builtInTouched2;

        /// <summary>
        /// 刷新两页页脚右侧的状态提示。两页列宽相同，标签左边界因此落在同一处。
        ///
        /// 橙字 = 只是说明"这套是内置的，改不动"；红字 = 用户真的动手改了、被拦下了。
        /// 分两档不是为了好看：红字如果天天挂着，两天就没人看了，真正出错时也就没了信号。
        ///
        /// 告警页那行由 RefreshAlertFooter 在内置时让位调到这里（见那里的守卫），
        /// 所以它的 editable 一档实际不命中——传 normalText 主要是给按键页用：
        /// 按键页红字在 _lblPresetInfo1、一次性反馈在 _lblCapture，两个标签互不打扰。
        /// </summary>
        private void UpdateBuiltInHints()
        {
            ApplyBuiltInHint(_lblPresetInfo1, _builtInTouched1, !CurrentIsBuiltIn(false),
                             CurrentProfileName(false), L.T("main.bind.hint.normal"));
            ApplyBuiltInHint(_lblSimHint2, _builtInTouched2, !CurrentIsBuiltIn(true),
                             CurrentProfileName(true), L.T("main.alert.hint.normal"));
        }

        /// <summary>
        /// name 只在"这套是内置的"时才用得上：页脚写得出「（内置）通用双发活塞 · 只读」，
        /// 用户不必抬头去看下拉才知道自己在改哪一套。可编辑那一档仍只写操作提示——
        /// 配置名在下拉里就在正上方，再重复一遍是噪音。
        /// </summary>
        private static void ApplyBuiltInHint(Label lbl, bool touched, bool editable,
                                             string name, string normalText)
        {
            if (lbl == null) return;
            // 三档文案，两档颜色：可编辑/只读都算常态，用提示橙；
            // 只有"改动被拦下"才值得上红。红是唯一强信号，留给真出错的地方。
            if (editable)
            {
                lbl.Text = normalText;
                HintText.Set(lbl, C_WarnInk);
                return;
            }

            // name 来自 TextNames.ProfileName，内置时**已经带**「（内置）」前缀（下拉里就这么显示）。
            // 这里再包一层 common.builtin.tag，页脚就印成「（内置）（内置）通用双发活塞」（1.3.63 截图）。
            // 只有 name 为空（取不到文件名）时才补一个纯前缀，避免页脚只剩「· 只读」。
            string which = string.IsNullOrEmpty(name) ? L.T("common.builtin.only") : name;

            if (touched)
            {
                lbl.Text = L.T("main.builtin.touched", which);
                HintText.Set(lbl, C_ErrInk);
            }
            else
            {
                lbl.Text = L.T("main.builtin.readonly", which);
                HintText.Set(lbl, C_WarnInk);
            }
        }

        /// <summary>「管理 ▾」：按键页、告警页各一个按钮，共用这一个处理。</summary>
        private void btnManage_Click(object sender, EventArgs e)
        {
            // 用 sender 判断是哪一页，不靠 _tabs.SelectedIndex：
            // 页签切换是异步的，选中项和"用户点的那个按钮"可能不是同一页。
            bool alert = ReferenceEquals(sender, _btnManage2);
            if (alert ? _alert == null : _buttons == null) return;

            bool builtIn = CurrentIsBuiltIn(alert);
            // 不设 ShowImageMargin：与两张表格的右键菜单保持一致。
            // 那边也是默认值（留出图标位），三处菜单并排看才是同一套东西。
            var menu = new ContextMenuStrip();

            // 前缀形式与下拉里的「（内置）名字」一致；以前这里是后缀「名字（内置）」，
            // 同一件事两种写法，用户要在两处各认一次。
            // CurrentProfileName 在内置时已带前缀，这边不再补——补了就成「（内置）（内置）名字」。
            string headName = CurrentProfileName(alert);
            if (string.IsNullOrEmpty(headName)) headName = L.T("common.builtin.only");
            var head = menu.Items.Add(L.T("main.prof.head", headName));
            head.Enabled = false;
            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add(L.T("main.prof.new"), null, (s, a) => NewProfile(alert));
            menu.Items.Add(L.T("main.prof.saveas"), null, (s, a) => SaveProfileAs(alert));

            var rename = menu.Items.Add(L.T("main.prof.rename"), null, (s, a) => RenameProfile(alert));
            var del = menu.Items.Add(L.T("main.prof.del"), null, (s, a) => DeleteProfile(alert));
            rename.Enabled = !builtIn;     // 内置配置在代码里，改不了名也删不掉
            del.Enabled = !builtIn;

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(L.T("main.prof.import"), null, (s, a) => ImportProfile(alert));
            menu.Items.Add(L.T("main.prof.export"), null, (s, a) => ExportProfile(alert));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(L.T("main.prof.openfolder"), null, (s, a) => OpenProfileFolder(alert));

            var btn = sender as Control;
            if (btn == null) return;
            menu.Show(btn, new Point(0, btn.Height));
        }

        /// <summary>名字校验：同类型配置里不能重名（allowName = 重命名时允许沿用原名）。</summary>
        private Func<string, string> NameValidator(bool alert, string allowName)
        {
            return n =>
            {
                // 先拦非法字符（1.4 乙方案）：在输入框当场提示，用户不用等到保存才看到错误。
                // 判据与 ProfileStore.SaveAlert/SaveButton 的入口校验是同一个，不另写一套——
                // 两处判据一分叉，就会出现"输入框说能存、保存时说不让"。
                if (!ProfileStore.IsNameUsable(n, out string whyName)) return whyName;
                if (allowName != null && string.Equals(n, allowName, StringComparison.CurrentCultureIgnoreCase))
                    return null;
                bool hit = alert
                    ? (ProfileStore.FindBuiltInAlert(n) != null || ProfileStore.FindByName(n, true) != null)
                    : (ProfileStore.FindBuiltInButton(n) != null || ProfileStore.FindByName(n, false) != null);
                return hit ? L.T("main.prof.dup", n) : null;
            };
        }

        /// <summary>
        /// 新建：从内置第一套（出厂默认）开始，不带当前这套的任何改动。
        /// 与「另存为」的区别就在这——另存为是"复制我现在的"，新建是"回到干净的"。
        /// </summary>
        private void NewProfile(bool alert)
        {
            string name = NameForm.Ask(this,
                alert ? L.T("main.prof.new.title.a") : L.T("main.prof.new.title.b"),
                L.T("main.prof.new.prompt"),
                alert ? L.T("main.prof.new.def.a") : L.T("main.prof.new.def.b"),
                NameValidator(alert, null));
            if (name == null) return;

            if (alert)
            {
                // 预置被用户删光时 CreateDefault() 顶上：一份没有任何覆盖的出厂配置
                var presets = AlertProfile.BuiltInProfiles();
                var p = presets.Count > 0 ? presets[0].Clone() : AlertProfile.CreateDefault();
                p.Name = name;
                p.IsBuiltIn = false;
                p.FilePath = "";
                // 出厂默认 = 内置规则原样，不带任何用户覆盖与自定义规则
                p.Overrides = new Dictionary<string, RuleOverride>();
                p.CustomRules = new List<AlertRule>();
                p.Sanitize();

                string err = ProfileStore.SaveAlert(p);
                if (err != null) { MessageBox.Show(this, err, L.T("main.prof.newfail"), MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                _alert = p;
                _cfg.ActiveAlertFile = ProfileStore.KeyOf(p);
            }
            else
            {
                // 新建按键配置以第一套演示为模板（带 8 条绑定）；预置没了就从空配置开始
                var presets = AppConfig.BuiltInButtonProfiles();
                var p = presets.Count > 0 ? presets[0].Clone() : new ButtonProfile();
                p.Name = name;
                p.IsBuiltIn = false;
                p.FilePath = "";

                string err = ProfileStore.SaveButton(p);
                if (err != null) { MessageBox.Show(this, err, L.T("main.prof.newfail"), MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                _buttons = p;
                _cfg.ActiveButtonFile = ProfileStore.KeyOf(p);
            }

            PersistConfig();
            RefreshPresetCombos();
            LoadPreset(alert ? 2 : 1, alert ? ProfileStore.KeyOf(_alert) : ProfileStore.KeyOf(_buttons));
            FlashNote(L.T("main.prof.created", name));
        }

        /// <summary>另存为：把当前这套内容复制成一个新名字，并切换过去。</summary>
        private void SaveProfileAs(bool alert)
        {
            string cur = CurrentProfileName(alert);
            string name = NameForm.Ask(this, L.T("main.prof.saveas.title"),
                L.T("main.prof.saveas.prompt"),
                L.T("main.prof.saveas.def", cur.Length > 0 ? cur : L.T("main.prof.saveas.defempty")),
                NameValidator(alert, null));
            if (name == null) return;

            if (alert)
            {
                var copy = _alert.Clone();
                copy.Name = name;
                copy.IsBuiltIn = false;
                copy.FilePath = "";

                string err = ProfileStore.SaveAlert(copy);
                if (err != null) { MessageBox.Show(this, err, L.T("main.prof.saveasfail"), MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                _alert = copy;
                _cfg.ActiveAlertFile = ProfileStore.KeyOf(copy);
            }
            else
            {
                // 只同步内存、不落盘：落盘会先 fork 出"xxx（我的）"，再存新名字，白多一个文件
                if (!_suppress) SyncBindingsFromGrid();
                var copy = _buttons.Clone();
                copy.Name = name;
                copy.IsBuiltIn = false;
                copy.FilePath = "";

                string err = ProfileStore.SaveButton(copy);
                if (err != null) { MessageBox.Show(this, err, L.T("main.prof.saveasfail"), MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                _buttons = copy;
                _cfg.ActiveButtonFile = ProfileStore.KeyOf(copy);
            }

            PersistConfig();
            RefreshPresetCombos();
            LoadPreset(alert ? 2 : 1, alert ? ProfileStore.KeyOf(_alert) : ProfileStore.KeyOf(_buttons));
            FlashNote(L.T("main.prof.savedas", name));
        }

        /// <summary>
        /// 重命名：改名字 = 换文件名。新文件写成功后必须把旧文件删掉，
        /// 否则列表里会同时出现新旧两份内容一样的配置。
        /// </summary>
        private void RenameProfile(bool alert)
        {
            string old = CurrentProfileName(alert);
            string name = NameForm.Ask(this, L.T("main.prof.rename.title"),
                L.T("main.prof.rename.prompt", old), old,
                NameValidator(alert, old));
            if (name == null || name == old) return;

            if (alert)
            {
                string oldPath = _alert.FilePath;
                _alert.Name = name;
                _alert.FilePath = "";

                string err = ProfileStore.SaveAlert(_alert);
                if (err != null)
                {
                    _alert.Name = old;
                    MessageBox.Show(this, err, L.T("main.prof.renamefail"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _alert.FilePath = Path.Combine(ProfileStore.AlertsDir,
                    ProfileStore.SanitizeFileName(name) + ".json");
                DeleteOldFile(oldPath);
                _cfg.ActiveAlertFile = ProfileStore.KeyOf(_alert);
            }
            else
            {
                if (!_suppress) SyncBindingsFromGrid();
                string oldPath = _buttons.FilePath;
                _buttons.Name = name;
                _buttons.FilePath = "";

                string err = ProfileStore.SaveButton(_buttons);
                if (err != null)
                {
                    _buttons.Name = old;
                    MessageBox.Show(this, err, L.T("main.prof.renamefail"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _buttons.FilePath = Path.Combine(ProfileStore.ButtonsDir,
                    ProfileStore.SanitizeFileName(name) + ".json");
                DeleteOldFile(oldPath);
                _cfg.ActiveButtonFile = ProfileStore.KeyOf(_buttons);
            }

            PersistConfig();
            RefreshPresetCombos();
            LoadPreset(alert ? 2 : 1, alert ? ProfileStore.KeyOf(_alert) : ProfileStore.KeyOf(_buttons));
            FlashNote(L.T("main.prof.renamed", name));
        }

        /// <summary>改名后清理旧文件（连同它的 .bak）。旧路径为空 = 原来是内置配置，没有文件要删。</summary>
        private static void DeleteOldFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try { if (File.Exists(path)) File.Delete(path); } catch (Exception) { }
            try { if (File.Exists(path + ".bak")) File.Delete(path + ".bak"); } catch (Exception) { }
        }

        /// <summary>删除当前配置。删完必须切到内置第一套：内存里不能留一套"文件已经不在了"的配置。</summary>
        private void DeleteProfile(bool alert)
        {
            string name = CurrentProfileName(alert);
            var r = MessageBox.Show(this,
                L.T("main.prof.del.confirm", name),
                L.T("main.prof.del.title"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (r != DialogResult.Yes) return;

            // 按身份（文件名）定位，不按显示名（1.3.69）。
            // 两个 json 里写了同一个 Name（手动改文件就会出现）时，按名找只会返回列表里靠前的那套，
            // 拿它去删就会删掉不是当前选中的那套，且不可撤销。
            string key = alert ? ProfileStore.KeyOf(_alert) : ProfileStore.KeyOf(_buttons);
            ProfileEntry entry = ProfileStore.FindByKey(key, alert);
            // 内置配置不在可删范围内：原逻辑用 Find（跳过内置）来挡，换成 FindByKey 后会匹配到
            // "builtin/xxx"，这里显式补回同一道门，否则会去删预置文件。
            if (entry != null && entry.IsBuiltIn) entry = null;
            if (entry == null)
            {
                MessageBox.Show(this, L.T("main.prof.del.notfound", name),
                    L.T("main.prof.del.title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string err = ProfileStore.Delete(entry);
            if (err != null) { MessageBox.Show(this, err, L.T("main.prof.delfail"), MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (alert)
            {
                var presets = AlertProfile.BuiltInProfiles();
                var fallback = presets.Count > 0 ? presets[0] : AlertProfile.CreateDefault();
                fallback.Sanitize();
                _alert = fallback;
                _cfg.ActiveAlertFile = ProfileStore.KeyOf(fallback);
            }
            else
            {
                var presets = AppConfig.BuiltInButtonProfiles();
                _buttons = presets.Count > 0 ? presets[0] : new ButtonProfile();
                _cfg.ActiveButtonFile = ProfileStore.KeyOf(_buttons);
            }

            // 只存"当前在用哪一套"（PersistState），不再走 PersistConfig：
            // 删完切回的是内置配置，内容本来就不该写盘；走 PersistConfig 会进
            // SaveAlertProfile 的内置分支，白点亮一次"改动未保存"红字，还闪一句无关提示。
            PersistState();
            RefreshPresetCombos();
            LoadPreset(alert ? 2 : 1, alert ? ProfileStore.KeyOf(_alert) : ProfileStore.KeyOf(_buttons));
            FlashNote(L.T("main.prof.deleted", name));
        }

        /// <summary>打开配置文件夹：绿色版分享配置就是"把文件拷过去"，得让用户容易找到它。</summary>
        private void OpenProfileFolder(bool alert)
        {
            string dir = alert ? ProfileStore.AlertsDir : ProfileStore.ButtonsDir;
            try
            {
                Directory.CreateDirectory(dir);
                System.Diagnostics.Process.Start("explorer.exe", "\"" + dir + "\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, L.T("main.prof.openfail", ex.Message), L.T("main.prof.openfail.title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ================= 导出 / 导入 =================
        //
        // 导出的就是 alerts\ / buttons\ 里那种文件：对方放进文件夹，或者直接「导入」，
        // 两条路都通，格式完全一致，没有第二套。

        private void ExportProfile(bool alert)
        {
            if (alert ? _alert == null : _buttons == null) return;

            string name = CurrentProfileName(alert);
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = alert ? L.T("main.prof.export.title.a") : L.T("main.prof.export.title.b");
                dlg.Filter = alert ? L.T("main.prof.filter.a") : L.T("main.prof.filter.b");
                dlg.FileName = ProfileStore.SanitizeFileName(name) + ".json";
                dlg.AddExtension = true;
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    string json;
                    if (alert)
                    {
                        var export = _alert.Clone();
                        export.Name = name;
                        export.IsBuiltIn = false;
                        export.Kind = AlertProfile.KindValue;
                        export.Version = AlertProfile.FormatVersion;
                        json = SerializeProfile(export);
                    }
                    else
                    {
                        SaveBindingsFromGrid();
                        var export = _buttons.Clone();
                        export.Name = name;
                        export.IsBuiltIn = false;
                        export.Kind = ButtonProfile.KindValue;
                        export.Version = ButtonProfile.FormatVersion;
                        json = SerializeProfile(export);
                    }
                    File.WriteAllText(dlg.FileName, json, new System.Text.UTF8Encoding(false));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, L.T("main.prof.export.err", ex.Message), L.T("main.prof.export"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                MessageBox.Show(this, L.T("main.prof.export.ok", dlg.FileName), L.T("main.prof.export"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private static string SerializeProfile(object profile)
        {
            return System.Text.Json.JsonSerializer.Serialize(profile,
                new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    IncludeFields = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
        }

        /// <summary>导入：同名不静默覆盖，问"覆盖 / 改名 / 取消"。内置配置只能改名导入。</summary>
        private void ImportProfile(bool alert)
        {
            if (_cfg == null) return;

            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = alert ? L.T("main.prof.import.title.a") : L.T("main.prof.import.title.b");
                dlg.Filter = alert ? L.T("main.prof.filter.a") : L.T("main.prof.filter.b");
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                if (alert) ImportAlertFile(dlg.FileName);
                else ImportButtonFile(dlg.FileName);
            }
        }

        private void ImportAlertFile(string path)
        {
            AlertProfile imported;
            string err;
            if (!ProfileStore.LoadAlert(path, out imported, out err))
            {
                MessageBox.Show(this, L.T("main.prof.import.bad", err), L.T("main.prof.importfail"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (ProfileStore.FindBuiltInAlert(imported.Name) != null)
            {
                string old = imported.Name;
                imported.Name = UniqueName(imported.Name, true);
                MessageBox.Show(this, L.T("main.prof.import.builtin", old, imported.Name),
                    L.T("main.prof.import.builtin.title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (ProfileStore.FindByName(imported.Name, true) != null)
            {
                var r = MessageBox.Show(this,
                    L.T("main.prof.import.dup", imported.Name),
                    L.T("main.prof.import.dup.title"), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (r == DialogResult.Cancel) return;
                if (r == DialogResult.No) imported.Name = UniqueName(imported.Name, true);
            }

            foreach (var note in imported.Sanitize()) AddConfigNote(note);
            string saveErr = ProfileStore.SaveAlert(imported);
            if (saveErr != null)
            {
                MessageBox.Show(this, saveErr, L.T("main.prof.importfail"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RefreshPresetCombos();
            MessageBox.Show(this, L.T("main.prof.import.ok", imported.Name),
                L.T("main.prof.import.ok.title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ImportButtonFile(string path)
        {
            ButtonProfile imported;
            string err;
            if (!ProfileStore.LoadButton(path, out imported, out err))
            {
                MessageBox.Show(this, L.T("main.prof.import.bad", err), L.T("main.prof.importfail"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (ProfileStore.FindBuiltInButton(imported.Name) != null)
            {
                string old = imported.Name;
                imported.Name = UniqueName(imported.Name, false);
                MessageBox.Show(this, L.T("main.prof.import.builtin", old, imported.Name),
                    L.T("main.prof.import.builtin.title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (ProfileStore.FindByName(imported.Name, false) != null)
            {
                var r = MessageBox.Show(this,
                    L.T("main.prof.import.dup", imported.Name),
                    L.T("main.prof.import.dup.title"), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (r == DialogResult.Cancel) return;
                if (r == DialogResult.No) imported.Name = UniqueName(imported.Name, false);
            }

            string saveErr = ProfileStore.SaveButton(imported);
            if (saveErr != null)
            {
                MessageBox.Show(this, saveErr, L.T("main.prof.importfail"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RefreshPresetCombos();
            MessageBox.Show(this, L.T("main.prof.import.ok", imported.Name),
                L.T("main.prof.import.ok.title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>同名时自动加序号，如「军用双发 2」。</summary>
        private string UniqueName(string baseName, bool alert)
        {
            string name = baseName;
            int n = 2;
            while (true)
            {
                bool hit = alert
                    ? (ProfileStore.FindBuiltInAlert(name) != null || ProfileStore.FindByName(name, true) != null)
                    : (ProfileStore.FindBuiltInButton(name) != null || ProfileStore.FindByName(name, false) != null);
                if (!hit) return name;
                name = baseName + " " + n;
                n++;
            }
        }

        /// <summary>alerts\ 或 buttons\ 里有没有这个同名配置（不含内置）。</summary>
        /// <summary>双击「触发条件」列打开条件编辑器。</summary>
        private void dgvSim_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            // 认列对象，不认列号：上一版这里写的是 5，而 Designer 里第 5 列是「速度」——
            // 加「速度」列时条件列从 5 挪到 6，这处没跟上，结果双击任何一行的速度格都会
            // 弹出条件编辑器，真正的「触发条件」列反而点不动。
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_dgvAlert == null || e.ColumnIndex >= _dgvAlert.Columns.Count) return;
            if (_dgvAlert.Columns[e.ColumnIndex] != colACond) return;
            EditCondition(e.RowIndex);
        }

        /// <summary>
        /// 打开条件编辑器并落盘。只存差量：改过的进 RuleConditions，点"还原默认"就删掉自己那条，
        /// 以后版本升级内置条件时，没改过的规则会自动跟着变。
        /// </summary>
        private void EditCondition(int rowIndex)
        {
            if (_cfg == null || _alert == null) return;
            if (rowIndex < 0 || rowIndex >= _alertRows.Count) return;

            var rule = _alertRows[rowIndex];

            // 顺手告诉编辑器"这些变量现在到底取不取得到"：
            // 已连接却没应答的变量，是机型不支持或拼写不同，不是条件写错了。
            bool simConnected = _simStatus != null && _simStatus.Connected;
            using (var f = new CondEditorForm(rule, _alert.CondOf(rule), () => _simState,
                                              n => _simState != null && _simState.Has(n), simConnected))
            {
                if (f.ShowDialog(this) != DialogResult.OK) return;

                if (f.RestoreDefault || f.Result == null) _alert.ResetRule(rule.Id);
                else
                {
                    var errs = f.Result.Validate();
                    if (errs.Count > 0)
                    {
                        MessageBox.Show(this, L.T("main.cond.invalid", string.Join("\r\n· ", errs.ToArray())),
                            L.T("main.cond.invalid.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    _alert.SetCond(rule.Id, f.Result);
                }

                PersistConfig();
            }

            // 整表重建最稳（四十来行），重建后把选中行放回去，用户不会"跳"
            RefreshAlertGrid();
            RefreshAlertCounts();
            if (rowIndex < _dgvAlert.Rows.Count) _dgvAlert.Rows[rowIndex].Selected = true;

            // 条件变了，命中集合的指纹可能没变（同样是"没命中"），所以强制重建
            EvaluateAlerts(true);
        }

        // ================= 颜色列自绘（Designer 表格调用） =================

        /// <summary>
        /// 颜色列自绘：整格背景填充为对应 LED 颜色，文字自动取对比色。
        /// 可编辑列在编辑态交还给系统 ComboBox；告警页颜色列只读，永远填色。
        /// </summary>
        private static void AttachColorSwatch(DataGridView g, int colorCol, bool readOnly)
        {
            g.CellPainting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex != colorCol) return;
                if (!readOnly && g.CurrentCell != null &&
                    g.CurrentCell.RowIndex == e.RowIndex &&
                    g.CurrentCell.ColumnIndex == e.ColumnIndex) return; // 编辑中交给系统 ComboBox

                var val = Convert.ToString(e.Value);
                var col = ColorOfName(val);
                using (var b = new SolidBrush(col))
                    e.Graphics.FillRectangle(b, e.CellBounds);

                using (var p = new Pen(Color.FromArgb(70, 255, 255, 255), 1f))
                    e.Graphics.DrawRectangle(p, e.CellBounds.X, e.CellBounds.Y,
                        e.CellBounds.Width - 1, e.CellBounds.Height - 1);

                TextRenderer.DrawText(e.Graphics, val ?? "", e.CellStyle.Font, e.CellBounds,
                    ReadableInkOn(col),
                    TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
                e.Handled = true;
            };
        }

        /// <summary>在指定颜色背景上清晰可读的文字色（亮底深字、暗底浅字）。</summary>
        private static Color ReadableInkOn(Color bg)
        {
            int lum = (int)(0.299 * bg.R + 0.587 * bg.G + 0.114 * bg.B);
            return lum > 140 ? Color.FromArgb(16, 26, 46) : Color.FromArgb(235, 241, 250);
        }

        // ================= 重复行标记（仅简单模式；告警页规则固定，不可能重复） =================

        /// <summary>简单模式重复判定列：按钮# + 触发 + 效果 + 颜色 + 速度 + 目标。</summary>
        private static readonly int[] DupKeyColsSimple =
        {
            BindColBtn, BindColTrigger, BindColEffect, BindColColor, BindColSpeed, BindColTarget
        };

        /// <summary>重新计算简单模式重复行（组内全部标色，不同组不同色）。</summary>
        private void RefreshDuplicateMarks()
        {
            _simpleDupText = MarkDuplicates(_dgv, _dupRows, DupKeyColsSimple, L.T("main.dup.tail"));
            UpdateDupStatus();
        }

        /// <summary>状态条只显示简单模式的重复提示（告警页没有"重复行"这回事）。</summary>
        private void UpdateDupStatus()
        {
            if (_lblPollErr == null) return;
            HintText.Set(_lblPollErr, C_WarnInk);
            _lblPollErr.Text = (_tabs != null && _tabs.SelectedIndex == 1 ? "" : _simpleDupText) ?? "";
        }

        /// <summary>通用重复行标记：返回状态条提示文本（无重复返回空串）。</summary>
        private static string MarkDuplicates(DataGridView g, Dictionary<int, Color> dict, int[] keyCols, string tail)
        {
            if (g == null || dict == null) return "";
            dict.Clear();

            // 1) 按行指纹分组（保持首次出现顺序，颜色稳定）
            var groups = new Dictionary<string, List<int>>();
            var order = new List<string>();
            for (int i = 0; i < g.Rows.Count; i++)
            {
                var r = g.Rows[i];
                if (r.IsNewRow) continue;
                string key = RowKey(r, keyCols);
                if (key.Length == 0) continue;
                List<int> list;
                if (!groups.TryGetValue(key, out list))
                {
                    list = new List<int>();
                    groups[key] = list;
                    order.Add(key);
                }
                list.Add(i);
            }

            // 2) 组内 ≥2 行 = 重复组：组内所有行都标记，颜色按组序轮换
            int groupNo = 0;
            for (int k = 0; k < order.Count; k++)
            {
                var rows = groups[order[k]];
                if (rows.Count < 2) continue;
                Color c = DupPalette[groupNo % DupPalette.Length];
                groupNo++;
                foreach (int i in rows) dict[i] = c;
            }

            // 3) 应用/清除行样式
            for (int i = 0; i < g.Rows.Count; i++)
            {
                var r = g.Rows[i];
                Color c;
                bool dup = dict.TryGetValue(i, out c);
                r.DefaultCellStyle.BackColor = dup ? DupBackOf(c) : Color.Empty;
                r.DefaultCellStyle.SelectionBackColor = dup ? DupSelOf(c) : Color.Empty;
                r.DefaultCellStyle.ForeColor = dup ? DupInkOf(c) : Color.Empty;
                r.DefaultCellStyle.SelectionForeColor = dup ? DupInkOf(c) : Color.Empty;
            }
            g.Invalidate();

            if (dict.Count == 0) return "";
            var parts = new List<string>();
            for (int k = 0; k < order.Count; k++)
            {
                var rows = groups[order[k]];
                if (rows.Count < 2) continue;
                var idxs = new List<string>();
                foreach (int i in rows) idxs.Add((i + 1).ToString());
                parts.Add(L.T("main.dup.rowgroup", string.Join("/", idxs.ToArray())));
            }
            return L.T("main.dup.found", dict.Count, string.Join(L.T("common.listsep"), parts.ToArray()), tail);
        }

        /// <summary>重复组调色板（6 组循环，色相区分明显）。</summary>
        private static readonly Color[] DupPalette =
        {
            Color.FromArgb(255, 176, 32),   // 橙
            Color.FromArgb(64, 196, 255),   // 青
            Color.FromArgb(236, 96, 190),   // 品红
            Color.FromArgb(126, 217, 87),   // 绿
            Color.FromArgb(178, 140, 255),  // 紫
            Color.FromArgb(255, 138, 128)   // 珊瑚红
        };

        /// <summary>按标记色生成浅色行背景（与白混合 72%）。</summary>
        private static Color DupBackOf(Color c) => Mix(c, Color.White, 0.72);

        /// <summary>选中态行背景（与白混合 55%）。</summary>
        private static Color DupSelOf(Color c) => Mix(c, Color.White, 0.52);

        /// <summary>行文字色（标记色加深，保证浅底可读）。</summary>
        private static Color DupInkOf(Color c) => Color.FromArgb(
            (int)(c.R * 0.42), (int)(c.G * 0.42), (int)(c.B * 0.42));

        private static Color Mix(Color a, Color b, double f)
        {
            f = f < 0 ? 0 : (f > 1 ? 1 : f);
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * f),
                (int)(a.G + (b.G - a.G) * f),
                (int)(a.B + (b.B - a.B) * f));
        }

        /// <summary>行指纹：按指定列拼接（首列空视为无效行）。</summary>
        private static string RowKey(DataGridViewRow r, int[] cols)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] >= r.Cells.Count) return "";
                string v = Convert.ToString(r.Cells[cols[i]].Value);
                if (i == 0 && string.IsNullOrEmpty(v)) return "";
                sb.Append(v).Append('|');
            }
            return sb.ToString();
        }

        /// <summary>
        /// 颜色名 → 与 LED 发光一致的颜色。
        ///
        /// **不许在这里写死任何语言的颜色名**（1.3.56 修）：这里原先比的是
        /// `"绿" / "琥珀" / "红"` 三个中文字面量，切到英文后格子里的值成了
        /// Green/Amber/Red，三个分支全部落空，整列画成一片灰——不报错、
        /// 不弹框，用户看到的就是"颜色丢了"。
        ///
        /// 现在名字只用于显示，语义靠序号：先把名字在当前语言的颜色表里查回
        /// 序号（与读表存配置、改格写配置两处同一个写法），再交 LedPalette 取色。
        /// 查不到（空格 / 非法值 / 语言包缺 key）返回 -1，Of 按「关」处理。
        /// </summary>
        private static Color ColorOfName(string name)
        {
            return LedPalette.Of(IndexOf(TextNames.ColorAll(), name));
        }

        // ================= 关闭清理 =================

        /// <summary>
        /// 丢掉正在编辑但没提交的那一格。
        ///
        /// 单元格还在编辑状态时，值只在编辑控件里，没进 cell.Value。
        /// 这时直接读表格保存，读到的仍是上一次提交的值——正好是我们想要的：
        /// 宁可少存一次，也不要把打了一半的内容（比如速度打成 "1."）写进配置。
        /// </summary>
        private void CancelEditing()
        {
            try
            {
                if (_dgv != null && _dgv.IsCurrentCellInEditMode) _dgv.CancelEdit();
                if (_dgvAlert != null && _dgvAlert.IsCurrentCellInEditMode) _dgvAlert.CancelEdit();
            }
            catch (Exception) { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 关掉「最小化到托盘」时，点 × 就是真退出：不再拦。
            if (_cfg != null && _cfg.MinimizeToTray &&
                !_quitFromTray &&
                e.CloseReason != CloseReason.WindowsShutDown &&
                e.CloseReason != CloseReason.TaskManagerClosing)
            {
                e.Cancel = true;
                // 收到托盘也算一次"关窗"：窗口位置、表格滚动、当前页都先记下来，
                // 免得托盘模式下被关机/结束进程时这些"看到的状态"全丢。
                CancelEditing();        // 编辑到一半：丢掉未提交的那一格
                SaveBindingsFromGrid();
                SaveViewState();
                PersistState();
                Hide();
                ShowTrayBalloonOnce();
                return;
            }

            _closing = true;
            CancelEditing();            // 编辑到一半：丢掉未提交的那一格
            SaveBindingsFromGrid();     // 已提交的改动落地
            SaveViewState();
            _cfg.SimConnectEnabled = _chkSim.Checked;
            _cfg.LastMode = _activeMode;
            PersistState();
            _pollTimer.Stop();
            try { if (_reconnectTimer != null) _reconnectTimer.Stop(); } catch (Exception) { }
            try { if (_rawTimer != null) _rawTimer.Stop(); } catch (Exception) { }
            try { if (_hintTimer != null) _hintTimer.Stop(); } catch (Exception) { }
            try { if (_lampHandoff != null) _lampHandoff.Stop(); } catch (Exception) { }
            try { if (_simData != null && !_simData.IsDisposed) _simData.Dispose(); } catch (Exception) { }
            _engine.Stop();
            try { if (_sim != null) _sim.Dispose(); } catch (Exception) { }
            try { _devNotify.Dispose(); } catch (Exception) { }
            _session.Dispose();
            if (_tray != null)
            {
                _tray.Visible = false;
                _tray.Dispose();
            }
            base.OnFormClosing(e);
        }
    }
}
