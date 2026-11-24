using System;
using System.Windows.Forms;
using G940LedPanel.Core;

namespace G940LedPanel.UI
{
    /// <summary>
    /// 设备实时诊断窗口（全设备扫描）：
    /// 下拉选择任意 DirectInput 设备逐个连接，60ms 轮询显示
    /// 每个对象的实时值、事件队列、原始字节与读取状态；
    /// 同时显示 Raw Input 原始输入通道的设备清单与实时 HID 报告。
    /// 用于定位“面板按钮数据到底挂在哪个设备 / 哪条通道”。
    ///
    /// 界面在 DiagForm.Designer.cs（VS 设计器可直接打开），本文件只放逻辑。
    /// </summary>
    public partial class DiagForm : Form
    {
        private readonly Timer _timer;
        private readonly RawInputReader _raw;
        private DeviceSession _scan;

        /// <summary>SimConnect 诊断文本（由主窗体提供），跟随报告一起导出。</summary>
        private readonly Func<string> _simDiag;

        /// <summary>
        /// 主界面最后一次“设备连接失败”的原因（由主窗体提供）。
        /// 连不上设备时最想看的恰恰是这句话，所以没连上也要能打开这个窗口。
        /// </summary>
        private readonly Func<string> _devNote;

        /// <summary>暂停后不再刷新画面：想选中复制、或让画面停在一帧上截图/导出。</summary>
        private bool _paused;

        /// <summary>供 VS 设计器实例化用（设计器只解析 InitializeComponent，不执行下面的代码）。</summary>
        public DiagForm()
            : this(null, null)
        {
        }

        public DiagForm(RawInputReader raw)
            : this(raw, null)
        {
        }

        public DiagForm(RawInputReader raw, Func<string> simDiag)
            : this(raw, simDiag, null)
        {
        }

        public DiagForm(RawInputReader raw, Func<string> simDiag, Func<string> devNote)
        {
            _raw = raw ?? new RawInputReader();
            _simDiag = simDiag;
            _devNote = devNote;

            InitializeComponent();

            // 暂停/复制/导出这几句都是提醒性质的，统一走下发口（橙 + 加粗）
            HintText.Set(_lblPaused, UiColors.Warn);

            // 先创建定时器，再枚举/自动选中（避免 SelectedIndexChanged 里 Stop 空引用）
            _timer = new Timer();
            _timer.Interval = 60;
            _timer.Tick += timer_Tick;

            // VS 设计器会执行构造函数：设计期不扫设备、不起定时器
            if (System.ComponentModel.LicenseManager.UsageMode
                == System.ComponentModel.LicenseUsageMode.Designtime) return;

            _timer.Start();

            try
            {
                var devs = DeviceSession.Enumerate();
                if (devs != null)
                    foreach (var d in devs) _cboScan.Items.Add(d);

                if (_cboScan.Items.Count > 0) _cboScan.SelectedIndex = 0;
                else _box.Text = NoteSection() + "未枚举到任何 DirectInput 设备。\r\n\r\n" + RawSection();
            }
            catch (Exception ex)
            {
                _box.Text = "枚举设备失败：" + ex.Message;
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (_paused) return;
            try
            {
                if (_scan != null && _scan.Connected)
                    _box.Text = BuildSnapshot();
            }
            catch (Exception ex)
            {
                _box.Text = "读取异常：" + ex.Message;
            }
        }

        private void cboScan_SelectedIndexChanged(object sender, EventArgs e) { OpenScanDevice(); }

        private void btnClose_Click(object sender, EventArgs e) { Close(); }

        private void btnPause_Click(object sender, EventArgs e)
        {
            _paused = !_paused;
            _btnPause.Text = _paused ? "继续" : "暂停";
            _lblPaused.Text = _paused
                ? "已暂停 · 画面锁定，可选中复制或导出（设备仍在后台读取）"
                : "";
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_box.Text)) return;
                Clipboard.SetText(_box.Text);
                _lblPaused.Text = "已复制到剪贴板";
            }
            catch (Exception ex)
            {
                _lblPaused.Text = "复制失败：" + ex.Message;
            }
        }

        /// <summary>
        /// HID 探针：只读枚举全部 HID 设备，打印身份、报告长度、原始描述符与
        /// 各 Feature 报告的当前值。用来确认 G940 的灯到底走哪个接口、Report ID 是几。
        /// 结果直接铺在输出框里，所以先暂停实时刷新 —— 否则 60ms 后就被快照冲掉。
        /// </summary>
        private void btnHidProbe_Click(object sender, EventArgs e)
        {
            _paused = true;
            _btnPause.Text = "继续";
            Cursor = Cursors.WaitCursor;
            try
            {
                _box.Text = HidProbe.Scan(false);
                _lblPaused.Text = "已暂停 · 当前是 HID 探针结果，点【继续】恢复实时快照";
            }
            catch (Exception ex)
            {
                _box.Text = "HID 探针失败：" + ex.Message;
                _lblPaused.Text = "已暂停";
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            string name = "G940诊断_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "导出诊断报告";
                dlg.Filter = "文本文件|*.txt";
                dlg.FileName = name;
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("========== 环境 ==========");
                    sb.AppendLine("时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    sb.AppendLine("程序：" + Application.ProductName + " " + Application.ProductVersion);
                    sb.AppendLine("系统：" + Environment.OSVersion.VersionString + "（" + (IntPtr.Size == 8 ? "x64" : "x86") + "）");
                    sb.AppendLine("CLR：" + Environment.Version);
                    sb.AppendLine("用户：" + Environment.UserName + "  管理员：" + IsAdmin());
                    sb.AppendLine();
                    string note = SafeNote();
                    if (note.Length > 0)
                    {
                        sb.AppendLine("========== 上次主界面连接失败原因 ==========");
                        sb.AppendLine(note);
                        sb.AppendLine();
                    }
                    sb.AppendLine("========== 选中设备快照 ==========");
                    sb.AppendLine(_box.Text);
                    sb.AppendLine();
                    sb.AppendLine("========== SimConnect ==========");
                    sb.AppendLine(_simDiag != null ? SafeSimDiag() : "（未提供）");

                    System.IO.File.WriteAllText(dlg.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                    _lblPaused.Text = "已导出：" + System.IO.Path.GetFileName(dlg.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "导出失败：" + ex.Message, "提示",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private string SafeNote()
        {
            if (_devNote == null) return "";
            try { return (_devNote() ?? "").Trim(); }
            catch (Exception) { return ""; }
        }

        private string NoteSection()
        {
            string note = SafeNote();
            return note.Length == 0
                ? ""
                : "========== 上次主界面连接失败原因 ==========\r\n" + note + "\r\n\r\n";
        }

        private string SafeSimDiag()
        {
            try { return _simDiag(); }
            catch (Exception ex) { return "SimConnect 诊断读取失败：" + ex.Message; }
        }

        private static string IsAdmin()
        {
            try
            {
                var id = System.Security.Principal.WindowsIdentity.GetCurrent();
                var p = new System.Security.Principal.WindowsPrincipal(id);
                return p.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator) ? "是" : "否";
            }
            catch (Exception) { return "未知"; }
        }

        private string BuildSnapshot()
        {
            string s = NoteSection() + _scan.ReadLiveSnapshot();
            s += "\r\n\r\n========== 原始输入通道（Raw Input / HID 报告） ==========\r\n";
            s += RawSection();
            return s;
        }

        private string RawSection()
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("状态：" + _raw.StatusText);
                if (_raw.Active)
                {
                    sb.AppendLine("锁定设备报告长度：" + _raw.ActiveReportLength + " 字节");
                    sb.AppendLine("最近报告(hex)：\r\n" + _raw.ActiveReportHex());
                }
                sb.AppendLine("当前全部原始设备：");
                var devs = _raw.EnumerateRawDevices();
                if (devs.Count == 0) sb.AppendLine("（无）");
                bool vendorC287 = false;
                foreach (var d in devs)
                {
                    string type = d.Type == 2 ? "HID" : (d.Type == 1 ? "键鼠" : "其它(" + d.Type + ")");
                    sb.AppendLine("  " + d.Name + "   VID=" + d.Vid.ToString("X4") + " PID=" + d.Pid.ToString("X4")
                        + "  usage=" + d.UsagePage.ToString("X2") + ":" + d.Usage.ToString("X2") + "  " + type);
                    if (d.Vid == 0x046D && d.Pid == 0xC287 && d.UsagePage != 0x0001) vendorC287 = true;
                }
                if (vendorC287)
                    sb.AppendLine("说明：C287 的顶层集合是厂商自定义用法（usage page 0xFF00 段），"
                        + "本程序按标准摇杆用法（01:04 / 01:05 / 01:08）注册，所以收不到它的报告，"
                        + "上方会一直停在“等待校准”。按钮读取以 DirectInput 为准。");
                sb.AppendLine("提示：G940 按钮的 HID 报告偏移为 32–43；请按按钮，观察上面“最近报告”是否出现非 0 字节。");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "原始输入区异常：" + ex.Message;
            }
        }

        private void OpenScanDevice()
        {
            try
            {
                _timer.Stop();
                try
                {
                    if (_scan != null) _scan.Dispose();
                    _scan = null;
                }
                catch (Exception)
                {
                }

                var info = _cboScan.SelectedItem as DeviceInfo;
                if (info == null)
                {
                    _box.Text = "未选择设备。";
                    _timer.Start();
                    return;
                }

                _scan = new DeviceSession();
                string err;
                bool ok;
                try
                {
                    ok = _scan.Connect(info, Handle, out err);
                }
                catch (Exception ex)
                {
                    ok = false;
                    err = "连接异常：" + ex.Message;
                }
                if (ok)
                {
                    // 同步原始输入通道的按钮偏移到当前扫描设备（HID 报告偏移）
                    try { _raw.SetButtonOffsets(_scan.GetButtonOffsets()); } catch (Exception) { }
                    _box.Text = BuildSnapshot();
                }
                else
                {
                    _box.Text = "连接失败：" + err + "\r\n\r\n" + RawSection();
                }
                _timer.Start();
            }
            catch (Exception ex)
            {
                _box.Text = "诊断异常：" + ex.Message;
                try { _timer.Start(); } catch (Exception) { }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _timer.Stop();
            try { if (_scan != null) _scan.Dispose(); } catch (Exception) { }
            base.OnFormClosed(e);
        }
    }
}
