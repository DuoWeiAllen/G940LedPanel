using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace G940LedPanel.Core
{
    /// <summary>SimConnect 连接状态（供 UI 显示 / 诊断）。</summary>
    public sealed class SimStatus
    {
        public bool Connected;
        public string Headline = L.T("scs.headline.off");
        public string Detail = "";
        public bool DllFound;
        public string DllPath = "";
        public string DllVersion = "";
        public string SimVersion = "";
        public int VarTotal;
        public int VarAnswered;
        public string LastError = "";
        public string MissingSummary = "";
    }

    /// <summary>
    /// SimConnect 数据源：独立后台线程 + 消息专用窗口，通过 WM_USER_SIMCONNECT 分发，
    /// 每秒请求一次全部变量，凑成一帧 <see cref="FlightState"/> 抛给 UI。
    /// 断线自动重连；dll 缺失时只报状态，绝不影响按钮/LED 功能。
    /// </summary>
    public sealed class SimConnectSource : IDisposable
    {
        public const int WM_USER_SIMCONNECT = 0x0402;
        private const int WM_QUIT = 0x0012;

        public event Action<SimStatus> StatusChanged;
        public event Action<FlightState> StateUpdated;

        private readonly Control _ui;
        private readonly object _lock = new object();

        private Thread _thread;
        private SimSink _sink;
        private IntPtr _sinkHandle = IntPtr.Zero;
        private ApplicationContext _ctx;
        private System.Windows.Forms.Timer _retry;
        private System.Windows.Forms.Timer _statusTimer;

        private SimConnectApi _api;
        private SimConnectApi.DispatchProc _dispatch;
        private IntPtr _h = IntPtr.Zero;
        private IntPtr _pendingClose = IntPtr.Zero;

        private volatile bool _running;
        private volatile bool _disposed;
        private int _notifyPending;
        private long _tick;

        private readonly double[] _values = new double[SimVars.Count];
        private readonly bool[] _has = new bool[SimVars.Count];
        private readonly bool[] _requested = new bool[SimVars.Count];
        private readonly List<int> _failed = new List<int>();

        private string _simVersion = "";
        private int _reportedAnswered = -1;

        // ---- 最近一次失败原因：存 key 不存译文 ----
        //
        // 1.3.63 改。原先这里是一个拼好的中文/英文句子，于是有三个后果：
        //   ① 切语言后 _lastError 仍是旧语言，弹窗里的「最近错误：」半中半英；
        //   ② SimStatus.Headline 把 _lastError 套进模板，模板跟着变、里面那句不变；
        //   ③ 采集线程算一次就定死，重连之前不会重新求值。
        // 改存「key + 参数」后，LastErrorText 每次读都按当前语言求值，
        // 与 1.3.60 处理 SimVars.LabelOf、1.3.62 处理 _activeFormat 同一个路数。
        //
        // _lastErrorRaw 单独留着：SimConnectApi.Load 失败时给的是一整句现成文字，
        // 没有 key 可存，原样透传。
        private string _lastErrorKey = "";
        private object[] _lastErrorArgs = null;
        private string _lastErrorRaw = "";

        // ---- 模拟器就绪状态（在飞 / 在菜单）----
        // 唯一来源：每秒数据帧里的 CAMERA STATE，判定规则全在 CameraStateJudge。
        // 菜单/加载画面里 MSFS 仍会推送数值，且那些数值是真的（机库里摆着默认飞机），
        // 从数据本身分不出"在飞"还是"没飞"，只能靠相机状态这个状态量。
        //
        // 为什么不用 SimConnect 的 RequestSystemState("Sim") / SimStart 事件：
        // MSFS2024 在菜单里也回 1、也不发 SimStop（实测日志：连上后 17ms 即答 1，当时正在主菜单）。
        // 花了一次实装才证伪，别再走回去。
        private volatile bool _simRunning;         // 最近一次判定：true = 在飞
        private volatile bool _simEventSeen;       // 是否读到过相机状态（false = 该模拟器不认这个变量，不做门）
        private readonly int _camIdx = SimVars.IndexOf(CameraStateJudge.VarName);
        private double _lastLoggedCam = double.NaN;

        /// <summary>
        /// 相机状态 → 分类 的表（Menu / Skip / Flight），由界面在启动时按配置建好设一次。
        /// null = 该模拟器不做门（全部按在飞），与"空表"等价。
        /// </summary>
        public IDictionary<int, CamPhase> CameraTable
        {
            get { return _cameraTable; }
            set { _cameraTable = value; }
        }
        private IDictionary<int, CamPhase> _cameraTable = CameraStateJudge.DefaultPhases();

        // 新判定要连续保持 1 秒才生效：数据帧 1 秒一帧，所以"从第一次看到相反判定
        // 到下一帧确认"正好一帧。代价：进飞行、回菜单各晚约 1 秒。
        // 它的作用已经收窄成"兜住将来未知值的抖动" —— 已知的过渡帧（29）走 Skip，不经过这里。
        private const int PhaseHoldMs = 1000;
        private readonly PhaseStabilizer _phase = new PhaseStabilizer(PhaseHoldMs);

        public SimConnectSource(Control uiForMarshal)
        {
            _ui = uiForMarshal;
            _dispatch = new SimConnectApi.DispatchProc(OnDispatch); // 保持引用，防止 GC
        }

        public bool IsRunning { get { return _running; } }
        public bool IsConnected { get { return _h != IntPtr.Zero; } }
        public string SimVersion { get { return _simVersion; } }
        public string DllPath { get { return _api != null ? _api.Path : ""; } }
        public string DllVersion { get { return _api != null ? _api.Version : ""; } }
        public string LastError { get { return LastErrorText; } }

        /// <summary>
        /// 最近一次失败原因，按**当前**语言求值。空串 = 没有错误。
        ///
        /// 每次读都重新 Format，所以切语言后不用重连就能拿到新语言的句子。
        /// </summary>
        private string LastErrorText
        {
            get
            {
                if (_lastErrorKey.Length > 0)
                    return _lastErrorArgs == null ? L.T(_lastErrorKey) : L.T(_lastErrorKey, _lastErrorArgs);
                return _lastErrorRaw ?? "";
            }
        }

        /// <summary>记一条带 key 的失败原因（参数可以是 0 个）。</summary>
        private void SetError(string key, params object[] args)
        {
            _lastErrorKey = key ?? "";
            _lastErrorArgs = (args != null && args.Length > 0) ? args : null;
            _lastErrorRaw = "";
        }

        /// <summary>记一条现成文字（没有 key 可存的情况，例如底层库直接给的句子）。</summary>
        private void SetErrorRaw(string text)
        {
            _lastErrorKey = "";
            _lastErrorArgs = null;
            _lastErrorRaw = text ?? "";
        }

        private void ClearError()
        {
            _lastErrorKey = "";
            _lastErrorArgs = null;
            _lastErrorRaw = "";
        }

        /// <summary>最近一次判定：用户是否在飞（true = 在飞）。</summary>
        public bool SimRunning { get { return _simRunning; } }

        /// <summary>是否读到过相机状态。false 表示这个模拟器不认该变量，上层应退回旧行为。</summary>
        public bool SimEventSeen { get { return _simEventSeen; } }

        public int AnsweredCount
        {
            get
            {
                lock (_lock)
                {
                    int n = 0;
                    for (int i = 0; i < _has.Length; i++) if (_has[i]) n++;
                    return n;
                }
            }
        }

        /// <summary>未被模拟器识别的变量（诊断用）。</summary>
        public List<string> MissingVars()
        {
            var list = new List<string>();
            lock (_lock)
            {
                for (int i = 0; i < _has.Length; i++)
                    if (!_has[i]) list.Add(SimVars.All[i].Label + " / " + SimVars.All[i].Name);
            }
            return list;
        }

        // ================= 生命周期 =================

        public void Start()
        {
            if (_running) return;
            _running = true;
            _disposed = false;
            _thread = new Thread(ThreadProc) { IsBackground = true, Name = "SimConnectSource" };
            try { _thread.SetApartmentState(ApartmentState.STA); } catch (Exception) { }
            _thread.Start();
        }

        public void Stop()
        {
            if (!_running) return;
            _running = false;
            try
            {
                var hwnd = _sinkHandle;
                if (hwnd != IntPtr.Zero) PostMessage(hwnd, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception) { }
            try { if (_thread != null) _thread.Join(3000); } catch (Exception) { }
            _thread = null;
            _sink = null;
            _sinkHandle = IntPtr.Zero;
            _ctx = null;
        }

        public void Restart()
        {
            Stop();
            Start();
        }

        private void ThreadProc()
        {
            try
            {
                _ctx = new ApplicationContext();

                _sink = new SimSink(this);
                var cp = new CreateParams();
                cp.Caption = "G940SimConnectSink";
                cp.Parent = new IntPtr(-3); // HWND_MESSAGE：消息专用窗口
                cp.Style = 0;
                cp.ExStyle = 0;
                _sink.CreateHandle(cp);
                _sinkHandle = _sink.Handle;

                _retry = new System.Windows.Forms.Timer { Interval = 5000 };
                _retry.Tick += (s, e) => { _retry.Stop(); TryConnect(); };

                _statusTimer = new System.Windows.Forms.Timer { Interval = 2500 };
                _statusTimer.Tick += (s, e) => { _statusTimer.Stop(); ReportStatus(); };

                TryConnect();
                Application.Run(_ctx);
            }
            catch (Exception ex)
            {
                SetError("scs.err.exception", ex.GetType().Name, ex.Message);
                ReportStatus();
            }
            finally
            {
                CleanupConnection();
                try { if (_retry != null) _retry.Dispose(); } catch (Exception) { }
                try { if (_statusTimer != null) _statusTimer.Dispose(); } catch (Exception) { }
                try { if (_sink != null) _sink.DestroyHandle(); } catch (Exception) { }
                _sinkHandle = IntPtr.Zero;
                try { if (_api != null) { _api.Dispose(); _api = null; } } catch (Exception) { }
            }
        }

        private void CleanupConnection()
        {
            if (_h != IntPtr.Zero)
            {
                try { if (_api != null) _api.Close(_h); } catch (Exception) { }
                _h = IntPtr.Zero;
            }
            ClosePending();
            lock (_lock)
            {
                Array.Clear(_has, 0, _has.Length);
                Array.Clear(_requested, 0, _requested.Length);
            }
        }

        private void ScheduleRetry(int ms)
        {
            if (!_running || _disposed) return;
            if (_retry == null) return;
            try
            {
                _retry.Stop();
                _retry.Interval = Math.Max(1000, ms);
                _retry.Start();
            }
            catch (Exception) { }
        }

        // ================= 连接 =================

        private void TryConnect()
        {
            if (_disposed || !_running) return;
            ClosePending();

            if (_api == null)
            {
                string path, log;
                if (!SimConnectLocator.TryFind(out path, out log))
                {
                    SetError("scs.err.nodll");
                    ReportStatus(log);
                    ScheduleRetry(10000);
                    return;
                }

                string err;
                _api = SimConnectApi.Load(path, out err);
                if (_api == null)
                {
                    SetErrorRaw(err);
                    ReportStatus(log);
                    ScheduleRetry(15000);
                    return;
                }
            }

            IntPtr h;
            int hr;
            try
            {
                hr = _api.Open(out h, "G940LedPanel", _sinkHandle, WM_USER_SIMCONNECT, IntPtr.Zero, 0);
            }
            catch (Exception ex)
            {
                SetError("scs.err.open.ex", ex.Message);
                ReportStatus();
                ScheduleRetry(10000);
                return;
            }

            if (hr != 0 || h == IntPtr.Zero)
            {
                SetError("scs.err.open.hr", hr.ToString("X8"));
                ReportStatus();
                ScheduleRetry(5000);
                return;
            }

            _h = h;
            _reportedAnswered = -1;
            RegisterAll();
            _phase.Reset();             // 新的一轮连接：等第一帧数据帧把相机状态带回来
            _simEventSeen = false;
            _simRunning = false;
            _lastLoggedCam = double.NaN;
            // 每次连上都开一段新记录：写清这是哪一次启动、当时用的是哪张相机状态表。
            // 文件名不变，靠这段分隔线区分多次启动。
            SceneStateLog.Begin(_simVersion, _cameraTable);
            SceneStateLog.Write(L.T("scs.log.connected", CameraStateJudge.VarName));
            ClearError();
            ReportStatus();
            if (_statusTimer != null) { _statusTimer.Stop(); _statusTimer.Start(); }
        }

        /// <summary>
        /// 用数据帧里的相机状态判定「在飞 / 在菜单」。
        /// 三种归属（<see cref="CameraStateJudge"/>，纯函数）：Menu → 等待，Flight → 在飞，
        /// Skip（过渡帧）→ 什么都不做，判定保持原样。再经稳定器（1 秒）兜一层未知值的抖动。
        /// </summary>
        private void UpdateFlightPhase(double cameraState)
        {
            CamPhase kind = CameraStateJudge.Classify(cameraState, _cameraTable);

            // 过渡帧：不改判定、不喂稳定器。这不是"保持上一个状态"那种带记忆的规则，
            // 就是"这一帧没有信息，跳过"——进出暂停菜单的那一瞬间因此不会闪。
            if (kind == CamPhase.Skip)
            {
                if (cameraState != _lastLoggedCam)
                {
                    _lastLoggedCam = cameraState;
                    SceneStateLog.Write(CameraStateJudge.Describe(cameraState, _cameraTable)
                                   + L.T("scs.log.effective", _simRunning ? L.T("scs.phase.flying") : L.T("scs.phase.waiting")));
                }
                return;
            }

            bool raw = kind == CamPhase.Flight;
            _phase.Feed(raw, unchecked((uint)Environment.TickCount));

            bool inFlight = _phase.InFlight;
            bool seen = _phase.Seen;
            bool changed = _simEventSeen != seen || _simRunning != inFlight;

            _simRunning = inFlight;
            _simEventSeen = seen;

            // 值变了、生效判定变了、或正在稳定中都记一行：
            // 既能看清进出菜单的时刻，也能在值表漏项时直接读到菜单里的实际数值
            // （补一行表即可，机制不用动）。
            if (changed || cameraState != _lastLoggedCam)
            {
                _lastLoggedCam = cameraState;
                SceneStateLog.Write(CameraStateJudge.Describe(cameraState, _cameraTable)
                               + L.T("scs.log.effective", inFlight ? L.T("scs.phase.flying") : L.T("scs.phase.waiting"))
                               + (_phase.Settling ? L.T("scs.log.settling") : ""));
            }
        }

        /// <summary>逐个变量注册 DataDefinition 并请求数据（每变量一个定义，避免个别变量失败导致偏移错位）。</summary>
        private void RegisterAll()
        {
            _failed.Clear();
            lock (_lock)
            {
                Array.Clear(_has, 0, _has.Length);
                Array.Clear(_requested, 0, _requested.Length);
                Array.Clear(_values, 0, _values.Length);
            }

            for (int i = 0; i < SimVars.All.Length; i++)
            {
                var v = SimVars.All[i];
                uint defId = (uint)(0x1000 + i);
                uint reqId = (uint)(i + 1);

                int hr1 = _api.AddToDataDefinition(_h, defId, v.Name, v.Unit,
                    SimConnectApi.SIMCONNECT_DATATYPE_FLOAT64, (uint)i);
                if (hr1 != 0) { _failed.Add(i); continue; }

                int hr2 = _api.RequestDataOnSimObject(_h, reqId, defId, SimConnectApi.SIMCONNECT_PERIOD_SECOND);
                if (hr2 != 0) { _failed.Add(i); continue; }

                lock (_lock) { _requested[i] = true; }
            }
        }

        // ================= 消息分发 =================

        private void OnSimMessage()
        {
            if (_h == IntPtr.Zero || _api == null) return;
            try { _api.CallDispatch(_h, _dispatch); }
            catch (Exception ex) { SetError("scs.err.dispatch", ex.Message); }
        }

        private void OnDispatch(IntPtr pData, uint cbData, IntPtr pContext)
        {
            if (pData == IntPtr.Zero || cbData < 12) return;
            int recvId = Marshal.ReadInt32(pData, 8);

            switch (recvId)
            {
                case SimConnectApi.RECV_ID_OPEN:
                    ReadOpen(pData);
                    break;

                case SimConnectApi.RECV_ID_SIMOBJECT_DATA:
                    ReadData(pData);
                    break;

                case SimConnectApi.RECV_ID_QUIT:
                    OnQuit();
                    break;

                case SimConnectApi.RECV_ID_EXCEPTION:
                    if (cbData >= 16)
                    {
                        int code = Marshal.ReadInt32(pData, 12);
                        SetError("scs.err.code", code);
                    }
                    break;
            }
        }

        private void ReadOpen(IntPtr pData)
        {
            try
            {
                int major = Marshal.ReadInt32(pData, 284);
                int minor = Marshal.ReadInt32(pData, 288);
                int buildMajor = Marshal.ReadInt32(pData, 292);
                int buildMinor = Marshal.ReadInt32(pData, 296);
                _simVersion = major + "." + minor + "." + buildMajor + "." + buildMinor;
                SceneStateLog.Write(L.T("scs.log.version", _simVersion));
            }
            catch (Exception) { }
        }

        private void ReadData(IntPtr pData)
        {
            int reqId = Marshal.ReadInt32(pData, 12);
            int idx = reqId - 1;
            if (idx < 0 || idx >= _values.Length) return;

            long bits = Marshal.ReadInt64(pData, 40);
            double v = BitConverter.Int64BitsToDouble(bits);

            lock (_lock)
            {
                _values[idx] = v;
                _has[idx] = true;
                _tick++;
            }
            if (idx == _camIdx) UpdateFlightPhase(v);
            NotifyState();
        }

        private void OnQuit()
        {
            // 不在 dispatch 回调里调 SimConnect_Close（部分版本会死锁），
            // 先记下句柄，交给下一次重连前统一关闭。
            if (_h != IntPtr.Zero)
            {
                _pendingClose = _h;
                _h = IntPtr.Zero;
            }
            lock (_lock)
            {
                Array.Clear(_has, 0, _has.Length);
                Array.Clear(_requested, 0, _requested.Length);
            }
            _simVersion = "";
            _phase.Reset();             // 断线：忘掉旧判定，重连后重新读
            SetError("scs.err.disconnected");
            ReportStatus();
            ScheduleRetry(3000);
        }

        private void ClosePending()
        {
            if (_pendingClose == IntPtr.Zero) return;
            try { if (_api != null) _api.Close(_pendingClose); } catch (Exception) { }
            _pendingClose = IntPtr.Zero;
        }

        // ================= 状态推送 =================

        private void NotifyState()
        {
            // 应答数量变化时刷新一次状态条：连接成功那一刻还没有任何数据，
            // 若只在连接瞬间上报，界面会一直停在 “0/81”。
            try
            {
                int a = AnsweredCount;
                if (a != _reportedAnswered) { _reportedAnswered = a; ReportStatus(); }
            }
            catch (Exception) { }

            if (Interlocked.Exchange(ref _notifyPending, 1) == 1) return;
            try
            {
                if (_ui != null && _ui.IsHandleCreated)
                    _ui.BeginInvoke(new Action(() =>
                    {
                        Interlocked.Exchange(ref _notifyPending, 0);
                        RaiseState();
                    }));
                else
                    Interlocked.Exchange(ref _notifyPending, 0);
            }
            catch (Exception) { Interlocked.Exchange(ref _notifyPending, 0); }
        }

        private void RaiseState()
        {
            var h = StateUpdated;
            if (h == null) return;
            FlightState snap;
            lock (_lock) { snap = new FlightState(_values, _has, _tick); }
            try { h(snap); } catch (Exception) { }
        }

        /// <summary>
        /// 按当前语言重算一次状态文案（1.3.63）。
        ///
        /// 为什么需要：SimStatus 里装的是**成品文字**——Headline 套了模板、LastError 套了原因。
        /// 主界面切语言时只重画自己的控件，不会回头问采集器要一份新的，于是状态栏与
        /// 诊断弹窗停在旧语言。1.3.62 截图里那句「● 未连接：SimConnect_Open 返回 0x80004005…」
        /// 就是这么来的：中间面板的 `No data` 是当场拼的（所以是英文），弹窗读的是缓存（所以是中文）。
        ///
        /// 状态文案的真源在采集器，所以重算入口也放这里——UI 自己拼会变成两份说法。
        /// </summary>
        public void RefreshStatusText()
        {
            if (_disposed) return;
            ReportStatus();
        }

        private void ReportStatus(string detailOverride = null)
        {
            var st = new SimStatus
            {
                Connected = _h != IntPtr.Zero,
                VarTotal = SimVars.All.Length,
                VarAnswered = AnsweredCount,
                DllPath = DllPath,
                DllVersion = DllVersion,
                DllFound = !string.IsNullOrEmpty(DllPath) || SimConnectLocator.LastLog != null,
                SimVersion = _simVersion,
                LastError = LastErrorText,
            };

            if (st.Connected)
            {
                st.Headline = L.T("scs.headline.ok", st.VarAnswered, st.VarTotal);
            }
            else if (!string.IsNullOrEmpty(LastErrorText))
            {
                st.Headline = L.T("scs.headline.err", LastErrorText);
            }
            else
            {
                st.Headline = L.T("scs.headline.connecting");
            }

            var miss = new List<string>();
            lock (_lock)
            {
                for (int i = 0; i < _has.Length; i++)
                    if (_requested[i] && !_has[i]) miss.Add(SimVars.All[i].Label);
            }
            if (miss.Count > 12) miss.RemoveRange(12, miss.Count - 12);
            st.MissingSummary = string.Join(L.T("common.listsep"), miss.ToArray());
            st.Detail = detailOverride ?? "";

            try
            {
                if (_ui != null && _ui.IsHandleCreated)
                    _ui.BeginInvoke(new Action(() =>
                    {
                        var h = StatusChanged;
                        if (h != null) { try { h(st); } catch (Exception) { } }
                    }));
            }
            catch (Exception) { }
        }

        public void Dispose()
        {
            _disposed = true;
            try { Stop(); } catch (Exception) { }
            try { if (_api != null) { _api.Dispose(); _api = null; } } catch (Exception) { }
        }

        // ================= Win32 =================

        [DllImport("user32", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        /// <summary>消息专用窗口：只负责接收 WM_USER_SIMCONNECT 并触发 CallDispatch。</summary>
        private sealed class SimSink : NativeWindow
        {
            private readonly SimConnectSource _owner;

            public SimSink(SimConnectSource owner) { _owner = owner; }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_USER_SIMCONNECT)
                {
                    _owner.OnSimMessage();
                    return;
                }
                base.WndProc(ref m);
            }
        }
    }
}
