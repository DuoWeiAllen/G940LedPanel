using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace G940LedPanel.Core
{
    /// <summary>
    /// G940 8 颗灯的自研直控通道（不再依赖 G940LedInterface.dll 与罗技 SDK）。
    ///
    /// 灯在哪个 PID 上：2026-11 真机实测，一条 USB 线的 G940 在系统里是四个 PID——
    ///   C2A8 摇杆、C2A9 油门、C2AA 脚舵（都是 0001:0004 标准游戏控制器用法，只有轴和按钮）；
    ///   C287 产品名就是 "G940"，走 FF00:0004 厂商自定义用法，63 字节力反馈输出与灯都在这里。
    /// 所以灯通道认的是 C287，与"油门"没有对应关系。
    ///
    /// 协议（2026-11 真机验证，与 Linux 内核 hid-lg.c 的实现一致）：
    ///   HID **Feature** 报告 ID = 3，长度 = FeatureReportByteLength（本机实测 5 字节）
    ///     buf[0] = 0x03       报告 ID
    ///     buf[1] = 红位图      bit0 = P1 … bit7 = P8
    ///     buf[2] = 绿位图      bit0 = P1 … bit7 = P8
    ///     buf[3..] = 0        补零
    ///   同一位红绿都置 1 = 琥珀。没有前置使能命令，写一次即刻生效。
    ///
    /// 三条从真机踩出来的教训，改动此处前请先读：
    ///   1. **不是 Output 报告**。曾经按 Output 报告 9 的 16 bit 写过三轮，灯一动不动——
    ///      因为那块 16 bit 根本没有写通路。判断「字段尺寸对得上」不等于「那就是目标字段」。
    ///   2. **不需要 DirectInput 指针**。罗技 SDK 的 _SetLEDs(IntPtr, byte, byte) 要一个
    ///      设备指针，那只是它的内部实现方式（它自己下探 HID），不是硬件要求。
    ///      所以灯的写入与输入读取彻底解耦：DirectInput 打不开时，灯照样能亮。
    ///   3. **默认就是全绿**。设备上电默认就亮全绿，写全 0 会让面板全灭，
    ///      看起来像设备坏了。退出/断开时恢复全绿，不要留全黑。
    ///
    /// 跨平台：协议层是同一套，移植到 Linux 只需把 HidD_SetFeature 换成
    ///   ioctl(fd, HIDIOCSFEATURE(len), buf)，fd 来自 /dev/hidrawX（权限靠 udev 规则）。
    ///   上层 ToMasks → 两字节 → 加 ID 前缀的逻辑一个字都不用改。
    /// </summary>
    public sealed class G940Hid : IDisposable
    {
        /// <summary>罗技 VID。</summary>
        public const ushort TargetVid = 0x046D;

        /// <summary>G940 的力反馈与灯接口 PID（产品名 "G940"，FF00:0004 厂商自定义用法）。</summary>
        public const ushort TargetPid = 0xC287;

        /// <summary>灯位图所在的 Feature 报告 ID。</summary>
        public const byte LedReportId = 3;

        /// <summary>caps 读不出来时的兜底长度（本机实测值）。</summary>
        private const int FallbackFeatureLength = 5;

        private readonly object _gate = new object();
        private IntPtr _handle = IntPtr.Zero;
        private string _path = "";
        private int _featureLength = FallbackFeatureLength;

        // 连续写失败计数。同口插回时设备几百毫秒就绪，重开旧路径就能接上，不必整套重找；
        // 换口之后旧路径永远打不开，攒够次数才清路径去重新枚举——这样两种情形都能自愈。
        private int _failCount;
        private const int FailLimit = 40;            // 25ms 一拍，约 1 秒
        private DateTime _lastProbe = DateTime.MinValue;
        private const int ProbeIntervalMs = 500;     // 重新枚举的最小间隔，避免每帧枚举 22 个接口

        private G940Hid() { }

        // ---------------------------------------------------------------- 打开

        /// <summary>
        /// 查找并打开 G940 的灯控接口。
        ///
        /// 为什么不能"第一个能打开的接口就用"：一根 USB 线上是整套 G940，
        /// 系统里对应四个 PID、更多个 HID 接口，只有 C287 那个能写进灯。取错了照样能打开，
        /// 只是写进去没反应、也不报错——这种失败最难查。
        ///
        /// 所以判据是「**试写一次能成功**」：全绿等于设备默认态，试错零副作用。
        /// 能写成功的那个接口，就是灯所在的接口。
        /// </summary>
        public static bool TryOpen(out G940Hid dev, out string error)
        {
            dev = null;
            error = "";

            List<string> paths = EnumerateHidPaths();
            if (paths.Count == 0)
            {
                error = L.T("gh.err.noiface");
                return false;
            }

            int matched = 0;        // VID/PID 命中的接口数
            int lastErr = 0;
            string lastPath = "";

            for (int i = 0; i < paths.Count; i++)
            {
                IntPtr h;
                int len;
                bool isTarget;
                int err;
                if (TryOpenOne(paths[i], out h, out len, out isTarget, out err))
                {
                    dev = new G940Hid();
                    dev._handle = h;
                    dev._path = paths[i];
                    dev._featureLength = len;
                    return true;
                }
                if (!isTarget) continue;
                matched++;
                lastErr = err;
                lastPath = paths[i];
            }

            if (matched == 0)
            {
                error = L.T("gh.err.nog940", paths.Count,
                    TargetVid.ToString("X4"), TargetPid.ToString("X4"));
                return false;
            }

            error = L.T("gh.err.write", matched, LedReportId,
                        lastErr != 0 ? L.T("gh.err.write.lasterr", lastErr, DescribeError(lastErr)) : "")
                    + L.T("gh.err.write.hint",
                        lastPath.Length > 0 ? L.T("gh.err.write.path", lastPath) : "");
            return false;
        }

        /// <summary>
        /// 试一个接口路径：打开 → 核对 VID/PID → 试写一次全绿。三关都过才算这个接口能写灯。
        ///
        /// 为什么判据是「试写成功」而不是「VID/PID 对得上」：同一根线在系统里挂着一串 HID 接口，
        /// VID/PID 全对也可能写到不会点亮的那一个，写进去还不报错。只有真写成功才算数。
        /// 试写用全绿还有一个好处——全绿就是设备默认态，试错零副作用。
        /// </summary>
        private static bool TryOpenOne(string path, out IntPtr handle, out int featureLength,
                                       out bool isTarget, out int lastErr)
        {
            handle = IntPtr.Zero;
            featureLength = FallbackFeatureLength;
            lastErr = 0;
            isTarget = false;

            IntPtr h = OpenPath(path);
            if (h == IntPtr.Zero) return false;

            HIDD_ATTRIBUTES attrs = new HIDD_ATTRIBUTES();
            attrs.Size = Marshal.SizeOf(typeof(HIDD_ATTRIBUTES));
            isTarget = HidD_GetAttributes(h, ref attrs)
                       && attrs.VendorID == TargetVid
                       && attrs.ProductID == TargetPid;
            if (!isTarget)
            {
                CloseHandle(h);
                return false;
            }

            int len = ReadFeatureLength(h);
            byte[] buf = new byte[len];
            buf[0] = LedReportId;
            buf[1] = 0x00;   // 红全灭
            buf[2] = 0xFF;   // 绿全亮 = 设备默认观感
            if (!HidD_SetFeature(h, buf, buf.Length))
            {
                lastErr = Marshal.GetLastWin32Error();
                CloseHandle(h);
                return false;
            }

            handle = h;
            featureLength = len;
            return true;
        }

        // ---------------------------------------------------------------- 写灯

        /// <summary>
        /// 写一帧 8 灯颜色。red / green 各 8 bit，bit0 = P1 … bit7 = P8，同位置 1 得琥珀。
        /// 每帧照写，不做位图比对——效果引擎 25ms 一拍，多出来的几次写入无关紧要，
        /// 而「和上一帧相同就不发」会让设备被拔插复位后停在旧色上，自己发现不了。
        /// </summary>
        public bool SetLeds(byte red, byte green)
        {
            lock (_gate)
            {
                bool ok = WriteLocked(red, green);
                if (ok)
                {
                    _failCount = 0;
                }
                else if (++_failCount >= FailLimit)
                {
                    // 连着失败一秒：多半是换了 USB 口，旧路径再也打不开了。
                    // 清掉路径，下一帧 EnsureHandle 会重新枚举整套设备去找能写灯的那个接口。
                    _path = "";
                    _failCount = 0;
                    _lastProbe = DateTime.MinValue;
                }
                return ok;
            }
        }

        /// <summary>
        /// 设备变更通知到达时调用：立刻丢掉句柄和路径，下一帧重新枚举。
        /// 比等写失败攒够次数快得多——拔掉设备时画面可能静止、根本没在写，等不到失败。
        /// </summary>
        public void MarkStale()
        {
            lock (_gate)
            {
                if (_handle != IntPtr.Zero)
                {
                    CloseHandle(_handle);
                    _handle = IntPtr.Zero;
                }
                _path = "";
                _failCount = 0;
                _lastProbe = DateTime.MinValue;
            }
        }

        /// <summary>恢复设备默认观感：8 颗全绿。断开/退出时用这个，不要写全黑。</summary>
        public bool RestoreDefault()
        {
            return SetLeds(0x00, 0xFF);
        }

        private bool WriteLocked(byte red, byte green)
        {
            if (!EnsureHandle()) return false;

            int len = _featureLength >= 3 ? _featureLength : FallbackFeatureLength;
            byte[] buf = new byte[len];
            buf[0] = LedReportId;
            buf[1] = red;
            buf[2] = green;

            if (HidD_SetFeature(_handle, buf, buf.Length)) return true;

            // 写失败通常是句柄已经失效（热插拔、休眠唤醒、驱动重启）。
            // 关掉句柄，下一帧 EnsureHandle 会按记住的路径重开，不让灯一灭到底。
            CloseHandle(_handle);
            _handle = IntPtr.Zero;
            return false;
        }

        // ---------------------------------------------------------------- 句柄维护

        /// <summary>
        /// 句柄没了就设法拿回来：先按记住的路径重开（同口插回最快），
        /// 路径打不开就整套设备重新枚举（换口之后路径变了，只能重找）。
        /// </summary>
        private bool EnsureHandle()
        {
            if (_handle != IntPtr.Zero) return true;

            if (!string.IsNullOrEmpty(_path))
            {
                IntPtr h = OpenPath(_path);
                if (h != IntPtr.Zero)
                {
                    _handle = h;
                    return true;
                }
                _path = "";   // 旧路径失效了（换口/重新枚举），下面走整套重找
            }

            // 枚举一次几十毫秒，别每帧都做。25ms 一拍下连续失败时，这个节流很关键。
            if ((DateTime.UtcNow - _lastProbe).TotalMilliseconds < ProbeIntervalMs) return false;
            _lastProbe = DateTime.UtcNow;

            List<string> paths = EnumerateHidPaths();
            for (int i = 0; i < paths.Count; i++)
            {
                IntPtr h;
                int len;
                bool isTarget;
                int err;
                if (!TryOpenOne(paths[i], out h, out len, out isTarget, out err)) continue;
                _handle = h;
                _path = paths[i];
                _featureLength = len;
                _failCount = 0;
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_handle != IntPtr.Zero)
                {
                    CloseHandle(_handle);
                    _handle = IntPtr.Zero;
                }
            }
        }

        // ---------------------------------------------------------------- 设备枚举

        /// <summary>枚举全部 HID 接口路径（GUID_DEVINTERFACE_HID）。</summary>
        private static List<string> EnumerateHidPaths()
        {
            var list = new List<string>();
            Guid hidGuid = new Guid("4D1E55B2-F16F-11CF-88CB-001111000030");

            IntPtr set = SetupDiGetClassDevs(ref hidGuid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
            if (set == IntPtr.Zero || set == new IntPtr(-1)) return list;

            try
            {
                SP_DEVICE_INTERFACE_DATA did = new SP_DEVICE_INTERFACE_DATA();
                did.cbSize = Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA));

                for (int i = 0; SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref hidGuid, i, ref did); i++)
                {
                    int need;
                    SetupDiGetDeviceInterfaceDetail(set, ref did, IntPtr.Zero, 0, out need, IntPtr.Zero);
                    if (need <= 0) continue;

                    IntPtr detail = Marshal.AllocHGlobal(need);
                    try
                    {
                        // 注意这是两个数，不是一个：
                        //   cbSize（结构头大小）：x64 = 8，x86 = 6 —— 结构体尾部按 8 对齐补齐。
                        //   路径起点：恒为 4 —— DevicePath 紧跟在 DWORD cbSize 后面（WCHAR 对齐 2）。
                        // 1.3.39 把两者当成同一个值、路径按 +8 读，读出来是 "id#vid_046d&…"
                        // （少了开头 4 个字符 "\\?\h"），CreateFile 全部失败，连接报"没有一个是 G940"。
                        // 真机实测：测试程序用 +4 能打开设备，本类用 +8 打不开。1.3.40 改回 +4。
                        Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6);
                        if (SetupDiGetDeviceInterfaceDetail(set, ref did, detail, need, out need, IntPtr.Zero))
                        {
                            string p = Marshal.PtrToStringUni(new IntPtr(detail.ToInt64() + 4));
                            if (!string.IsNullOrEmpty(p)) list.Add(p);
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(detail);
                    }
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(set);
            }

            return list;
        }

        /// <summary>
        /// 打开 HID 接口。共享模式读写——HID 没有独占概念，游戏、其它工具、本程序可以同时持有句柄，
        /// 互相之间不会因为「谁先打开」而失败，最多是谁后写谁覆盖灯色。
        /// </summary>
        private static IntPtr OpenPath(string path)
        {
            return CreateFile(path,
                GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                0,
                IntPtr.Zero);
        }

        /// <summary>读 Feature 报告长度。读不到就返回兜底值，不让整个连接因为 caps 失败而挂掉。</summary>
        private static int ReadFeatureLength(IntPtr h)
        {
            IntPtr preparsed;
            if (!HidD_GetPreparsedData(h, out preparsed)) return FallbackFeatureLength;
            try
            {
                HIDP_CAPS caps = new HIDP_CAPS();
                if (HidP_GetCaps(preparsed, ref caps) < 0) return FallbackFeatureLength;
                return caps.FeatureReportByteLength >= 3 ? caps.FeatureReportByteLength : FallbackFeatureLength;
            }
            finally
            {
                HidD_FreePreparsedData(preparsed);
            }
        }

        /// <summary>把 Win32 错误码翻成人话，只在 TryOpen 拼失败原因时用。</summary>
        private static string DescribeError(int code)
        {
            switch (code)
            {
                case 0: return L.T("gh.err.c0");
                case 1: return L.T("gh.err.c1");
                case 2: return L.T("gh.err.c2");
                case 5: return L.T("gh.err.c5");
                case 6: return L.T("gh.err.c6");
                case 87: return L.T("gh.err.c87");
                case 1167: return L.T("gh.err.c1167");
                case 32: return L.T("gh.err.c32");
                default: return L.T("gh.err.unknown");
            }
        }

        // ---------------------------------------------------------------- P/Invoke

        private const uint DIGCF_PRESENT = 0x02;
        private const uint DIGCF_DEVICEINTERFACE = 0x10;
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x01;
        private const uint FILE_SHARE_WRITE = 0x02;
        private const uint OPEN_EXISTING = 3;

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public int Flags;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HIDD_ATTRIBUTES
        {
            public int Size;
            public ushort VendorID;
            public ushort ProductID;
            public ushort VersionNumber;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HIDP_CAPS
        {
            public ushort Usage;
            public ushort UsagePage;
            public ushort InputReportByteLength;
            public ushort OutputReportByteLength;
            public ushort FeatureReportByteLength;
            public ushort Reserved0, Reserved1, Reserved2, Reserved3, Reserved4;
            public ushort Reserved5, Reserved6, Reserved7, Reserved8, Reserved9;
            public ushort Reserved10, Reserved11, Reserved12, Reserved13, Reserved14;
            public ushort Reserved15, Reserved16;
            public ushort NumberLinkCollectionNodes;
            public ushort NumberInputButtonCaps;
            public ushort NumberInputValueCaps;
            public ushort NumberInputDataIndices;
            public ushort NumberOutputButtonCaps;
            public ushort NumberOutputValueCaps;
            public ushort NumberOutputDataIndices;
            public ushort NumberFeatureButtonCaps;
            public ushort NumberFeatureValueCaps;
            public ushort NumberFeatureDataIndices;
        }

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, IntPtr enumerator,
            IntPtr hwndParent, uint flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInterfaces(IntPtr deviceInfoSet, IntPtr deviceInfoData,
            ref Guid interfaceClassGuid, int memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr deviceInfoSet,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, IntPtr deviceInterfaceDetailData,
            int deviceInterfaceDetailDataSize, out int requiredSize, IntPtr deviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateFile(string fileName, uint desiredAccess, uint shareMode,
            IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_GetAttributes(IntPtr hidDeviceObject, ref HIDD_ATTRIBUTES attributes);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_GetPreparsedData(IntPtr hidDeviceObject, out IntPtr preparsedData);

        [DllImport("hid.dll")]
        private static extern bool HidD_FreePreparsedData(IntPtr preparsedData);

        [DllImport("hid.dll")]
        private static extern int HidP_GetCaps(IntPtr preparsedData, ref HIDP_CAPS capabilities);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_SetFeature(IntPtr hidDeviceObject, byte[] reportBuffer, int reportBufferLength);
    }
}
