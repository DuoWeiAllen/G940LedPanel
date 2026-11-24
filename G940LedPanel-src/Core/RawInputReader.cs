using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace G940LedPanel.Core
{
    /// <summary>
    /// Raw Input（WM_INPUT）原始输入通道：
    /// 绕过 DirectInput 的 HID 格式映射 / 协作级别 / 获取流程，
    /// 直接从操作系统接收 HID 报告中的按钮字节。
    /// 用于解决 DirectInput GetDeviceState 在本机对所有设备返回全 0 的系统性问题。
    /// 按钮偏移直接使用设备 EnumObjects 的真实偏移（即 HID 报告字节偏移）。
    ///
    /// 注意「注册」和「解析」不是一回事：下面 Register() 把摇杆(0x04)/手柄(0x05)/
    /// 多轴(0x08)三类 HID 控制器都登记了，但真正解析的只有本会话连上的那一台
    /// （G940 油门，见 DevicePicker），因为偏移表只从它 EnumObjects 得来。
    /// 摇杆和脚舵的报告照样会进来，只是取不到值——界面上「按摇杆没反应」是这个原因，
    /// 不是漏了注册。所以界面文案统一叫「油门按钮/油门按键」，不再用会把三台设备
    /// 混为一谈的笼统叫法。
    /// </summary>
    public class RawInputReader
    {
        // ---- user32 ----
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterRawInputDevices(
            [In] RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetRawInputData(IntPtr hRawInput, int uiCommand,
            IntPtr pData, ref int pcbSize, int cbSizeHeader);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetRawInputDeviceList(IntPtr pRawInputDeviceList,
            ref int puiNumDevices, int cbSize);

        // 必须显式声明 Unicode：RIDI_DEVICENAME 返回的是宽字符串，
        // 不写 CharSet 会按默认 A 版调用、拿到 ANSI 字节，再用 PtrToStringUni 解就是乱码
        // （曾经诊断窗里一整列「屜尿䥈⍄䥖彄…」就是这个）。
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetRawInputDeviceInfo(IntPtr hDevice, int uiCommand,
            IntPtr pData, ref int pcbSize);

        private const int RIM_TYPEHID = 2;
        private const int RID_INPUT = 0x10000003;
        private const int RID_HEADER = 0x10000005;
        private const int RIDI_DEVICENAME = 0x20000007;
        private const int RIDI_DEVICEINFO = 0x2000000b;
        private const int RIDEV_INPUTSINK = 0x00000100;
        private const int RIDEV_DEVNOTIFY = 0x00002000;
        private const int GIDC_ARRIVAL = 1;
        private const int GIDC_REMOVAL = 2;

        /// <summary>WM_INPUT：原始输入消息。</summary>
        public const int WM_INPUT = 0x00FF;
        /// <summary>WM_INPUT_DEVICE_CHANGE：设备接入/拔出通知。</summary>
        public const int WM_INPUT_DEVICE_CHANGE = 0x00FE;

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public int dwFlags;
            public IntPtr hwndTarget;
        }

        private bool _registered;

        // 目标按钮偏移（来自当前连接设备的 EnumObjects：G940 油门=32-43 等）
        private int[] _buttonOffsets;

        // 各原始设备的最近报告（用于变化检测与校准）
        private readonly Dictionary<IntPtr, byte[]> _reports = new Dictionary<IntPtr, byte[]>();
        private IntPtr _activeDevice;
        private byte[] _activeReport;

        public string LastError { get; private set; } = "";
        public int RegisteredCount { get; private set; }
        public bool RegisteredOk => _registered;

        /// <summary>设备是否已锁定（校准完成：某个设备的目标按钮区出现过 0→1）。</summary>
        public bool Active => _activeDevice != IntPtr.Zero;

        /// <summary>目标按钮偏移表（0 起）。</summary>
        public int[] ButtonOffsets => _buttonOffsets;

        /// <summary>锁定设备最近报告的字节长度。</summary>
        public int ActiveReportLength => _activeReport?.Length ?? 0;

        /// <summary>锁定设备的原始设备名。</summary>
        public string ActiveDeviceName { get; private set; } = "";

        /// <summary>设置按钮偏移表（连接设备时由 EnumObjects 结果填充）；切换目标后需重新校准。</summary>
        public void SetButtonOffsets(int[] offsets)
        {
            _buttonOffsets = offsets;
            _activeDevice = IntPtr.Zero;
            _activeReport = null;
            ActiveDeviceName = "";
        }

        /// <summary>注册原始输入（HID 游戏控制器：usage 0x04 摇杆 / 0x05 手柄 / 0x08 多轴控制器）。</summary>
        public bool Register(IntPtr hwnd)
        {
            try
            {
                var devs = new[]
                {
                    new RAWINPUTDEVICE { usUsagePage = 0x01, usUsage = 0x04, dwFlags = RIDEV_INPUTSINK | RIDEV_DEVNOTIFY, hwndTarget = hwnd },
                    new RAWINPUTDEVICE { usUsagePage = 0x01, usUsage = 0x05, dwFlags = RIDEV_INPUTSINK | RIDEV_DEVNOTIFY, hwndTarget = hwnd },
                    new RAWINPUTDEVICE { usUsagePage = 0x01, usUsage = 0x08, dwFlags = RIDEV_INPUTSINK | RIDEV_DEVNOTIFY, hwndTarget = hwnd }
                };
                _registered = RegisterRawInputDevices(devs, (uint)devs.Length,
                    (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE)));
                if (!_registered)
                    LastError = "RegisterRawInputDevices 失败：Win32错误 " + Marshal.GetLastWin32Error();
                RegisteredCount = _registered ? devs.Length : 0;
                return _registered;
            }
            catch (Exception ex)
            {
                LastError = "注册原始输入异常：" + ex.Message;
                return false;
            }
        }

        /// <summary>处理 WM_INPUT 消息（在窗体 WndProc 中调用）。</summary>
        public void HandleMessage(IntPtr hRawInput)
        {
            try
            {
                int headerSize = IntPtr.Size == 8 ? 24 : 16; // sizeof(RAWINPUTHEADER)
                IntPtr head = Marshal.AllocHGlobal(headerSize);
                int cb = headerSize;
                int hr;
                int type;
                int total;
                IntPtr hDev;
                // 必须在释放 head 之前把三个字段取出来：先 Free 再 Read 是读已释放内存，
                // 拿到的是别人写进去的垃圾（hDevice 错了会污染校准状态）。
                try
                {
                    hr = GetRawInputData(hRawInput, RID_HEADER, head, ref cb, headerSize);
                    if (hr < 0) return;
                    type = Marshal.ReadInt32(head, 0);       // RAWINPUTHEADER.dwType
                    total = Marshal.ReadInt32(head, 4);      // RAWINPUTHEADER.dwSize
                    hDev = Marshal.ReadIntPtr(head, 8);      // RAWINPUTHEADER.hDevice
                }
                finally { Marshal.FreeHGlobal(head); }

                if (type != RIM_TYPEHID) return;
                if (total < headerSize + 8 || total > 4096) return;

                IntPtr buf = Marshal.AllocHGlobal(total);
                cb = total;
                try
                {
                    hr = GetRawInputData(hRawInput, RID_INPUT, buf, ref cb, headerSize);
                    if (hr < 0) return;
                    int hidSize = Marshal.ReadInt32(buf, headerSize);       // RAWHID.dwSizeHid
                    int count = Marshal.ReadInt32(buf, headerSize + 4);     // RAWHID.dwCount
                    if (hidSize <= 0 || count <= 0 || hidSize * count > 512) return;
                    var report = new byte[hidSize];
                    Marshal.Copy(IntPtr.Add(buf, headerSize + 8), report, 0, hidSize);
                    OnReport(hDev, report);
                }
                finally { Marshal.FreeHGlobal(buf); }
            }
            catch (Exception)
            {
                // 单条消息异常不影响主流程
            }
        }

        private void OnReport(IntPtr hDev, byte[] report)
        {
            if (_buttonOffsets == null || _buttonOffsets.Length == 0) return;

            bool hasPrev = _reports.TryGetValue(hDev, out var prev);
            _reports[hDev] = report;

            if (_activeDevice != IntPtr.Zero)
            {
                if (hDev == _activeDevice) _activeReport = report;
                return;
            }

            // 校准：目标按钮区某位 0→1 的“第一个”设备被锁定。
            //
            // 这套判定看着糙（谁先动就认谁），但 G940 摇杆、脚舵同时插着也不会认错，
            // 原因在下一行的长度检查：按钮偏移来自**本会话连的那台**（DevicePicker 精确匹配
            // Logitech G940 Throttle），油门那 12 个按钮落在报告偏移 32–43；摇杆/脚舵的
            // 报告够不到这个区间，逐位 continue 掉，永远拿不到锁定资格。实测：先摇杆后油门，
            // 摇杆无反应、油门正常锁定。
            //
            // 再退一步，就算真锁错了设备，CurrentButtons() 也只按油门这张偏移表取值，
            // 最坏是读不到按钮，不会把摇杆的键当成油门键去触发效果。
            // 所以这里不加"报告长度 + 按钮数"之类的二次特征判定：那是为一个证伪了的
            // 场景多压一层机制（见 1.3.49 发布说明第五节）。
            if (hasPrev)
            {
                for (int i = 0; i < _buttonOffsets.Length; i++)
                {
                    int ofs = _buttonOffsets[i];
                    if (ofs < 0 || ofs >= report.Length || ofs >= prev.Length) continue;
                    if (report[ofs] != 0 && prev[ofs] == 0)
                    {
                        _activeDevice = hDev;
                        _activeReport = report;
                        ActiveDeviceName = GetDeviceName(hDev);
                        return;
                    }
                }
            }
        }

        /// <summary>当前按钮状态（按目标偏移表解析；仅在校准完成后有效）。</summary>
        public bool[] CurrentButtons(int maxButtons)
        {
            var res = new bool[maxButtons > 0 ? maxButtons : 128];
            if (_activeDevice == IntPtr.Zero || _activeReport == null || _buttonOffsets == null) return res;
            try
            {
                for (int i = 0; i < res.Length && i < _buttonOffsets.Length; i++)
                {
                    int ofs = _buttonOffsets[i];
                    if (ofs < 0 || ofs >= _activeReport.Length) continue;
                    res[i] = _activeReport[ofs] != 0;
                }
            }
            catch (Exception) { }
            return res;
        }

        /// <summary>锁定设备最近报告的十六进制（诊断）。</summary>
        public string ActiveReportHex()
        {
            if (_activeReport == null) return "（尚无报告）";
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _activeReport.Length; i++)
            {
                if (i % 16 == 0 && i > 0) sb.AppendLine();
                sb.Append(_activeReport[i].ToString("X2")).Append(' ');
            }
            return sb.ToString();
        }

        /// <summary>状态文案（主界面/诊断显示）。</summary>
        public string StatusText
        {
            get
            {
                if (!_registered) return "原始输入：未注册（" + (string.IsNullOrEmpty(LastError) ? "未知原因" : LastError) + "）";
                if (_buttonOffsets == null || _buttonOffsets.Length == 0) return "原始输入：已注册，等待连接设备";
                if (_activeDevice == IntPtr.Zero) return "原始输入：已就绪，等待校准（请按面板按钮）";
                return "原始输入：已校准 → " + ActiveDeviceName;
            }
        }

        // ---- 原始设备枚举（诊断用） ----

        public class RawDeviceInfo
        {
            public IntPtr Handle;
            public string Name = "";
            public int Vid, Pid;
            public int UsagePage, Usage;
            public int Type;
            public override string ToString() => Name;
        }

        /// <summary>枚举当前全部原始输入设备（名称 + VID/PID + 用法）。</summary>
        public List<RawDeviceInfo> EnumerateRawDevices()
        {
            var list = new List<RawDeviceInfo>();
            try
            {
                int n = 0;
                int itemSize = IntPtr.Size == 8 ? 16 : 8; // RAWINPUTDEVICELIST
                if (GetRawInputDeviceList(IntPtr.Zero, ref n, itemSize) < 0 || n <= 0) return list;
                IntPtr arr = Marshal.AllocHGlobal(itemSize * n);
                try
                {
                    int got = GetRawInputDeviceList(arr, ref n, itemSize);
                    if (got > 0)
                    {
                        for (int i = 0; i < got; i++)
                        {
                            IntPtr h = Marshal.ReadIntPtr(arr, i * itemSize);
                            var d = new RawDeviceInfo { Handle = h, Name = GetDeviceName(h) };
                            IntPtr info = Marshal.AllocHGlobal(32); // RID_DEVICE_INFO：x64/x86 实为 24 字节，留余量给驱动的版本差异
                            try
                            {
                                int sz = 32;
                                if (GetRawInputDeviceInfo(h, RIDI_DEVICEINFO, info, ref sz) >= 0)
                                {
                                    d.Type = Marshal.ReadInt32(info, 4);
                                    d.Vid = Marshal.ReadInt32(info, 8);
                                    d.Pid = Marshal.ReadInt32(info, 12);
                                    d.UsagePage = (ushort)Marshal.ReadInt16(info, 20);
                                    d.Usage = (ushort)Marshal.ReadInt16(info, 22);
                                }
                            }
                            finally { Marshal.FreeHGlobal(info); }
                            list.Add(d);
                        }
                    }
                }
                finally { Marshal.FreeHGlobal(arr); }
            }
            catch (Exception) { }
            return list;
        }

        private static string GetDeviceName(IntPtr hDevice)
        {
            try
            {
                int cb = 0;
                if (GetRawInputDeviceInfo(hDevice, RIDI_DEVICENAME, IntPtr.Zero, ref cb) < 0 || cb <= 0) return "";

                // RIDI_DEVICENAME 的 pcbSize 单位是「字符」，不是字节——这是这条命令唯一的例外。
                // 所以缓冲区要按 cap * 2 字节申请，而传给 API 的仍是字符数 cap。
                // 曾经按字节申请（AllocHGlobal(cb + 2)）配 Unicode 版调用：设备名 200 字符就要写
                // 400 字节，缓冲区只有 202 字节，越界踩坏相邻堆块，一打开设备诊断就
                // ntdll c0000374 闪退。ANSI 版下字符数恰好等于字节数，才一直没暴露。
                int cap = cb + 1;                 // 留 1 个字符给结尾 '\0'
                IntPtr p = Marshal.AllocHGlobal(cap * 2);
                try
                {
                    int sz = cap;
                    if (GetRawInputDeviceInfo(hDevice, RIDI_DEVICENAME, p, ref sz) < 0) return "";
                    return Marshal.PtrToStringUni(p) ?? "";
                }
                finally { Marshal.FreeHGlobal(p); }
            }
            catch (Exception) { return ""; }
        }
    }
}
