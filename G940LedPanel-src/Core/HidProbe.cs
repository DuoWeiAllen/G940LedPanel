using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace G940LedPanel.Core
{
    /// <summary>
    /// HID 探针：把系统里所有 HID 设备的身份、报告长度、原始报告描述符、
    /// 以及各 Feature 报告的当前内容原样打印出来（只读，不写设备）。
    ///
    /// 用途：确认 G940 的 LED 走哪个接口、Feature 报告 ID 是几、
    /// 一个报告里有多少 bit。有了这些数据，才谈得上自己发 HID 报告替代罗技 DLL。
    ///
    /// 不修改任何设备状态：只调用 HidD_GetFeature（读），不发 SetFeature（写）。
    /// </summary>
    public static class HidProbe
    {
        // ---------------------------------------------------------------- P/Invoke

        private const uint DIGCF_PRESENT = 0x02;
        private const uint DIGCF_DEVICEINTERFACE = 0x10;
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x01;
        private const uint FILE_SHARE_WRITE = 0x02;
        private const uint OPEN_EXISTING = 3;
        private static readonly Guid GUID_DEVINTERFACE_HID =
            new Guid("4D1E55B2-F16F-11CF-88CB-001111000030");

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

        [DllImport("hid.dll", CharSet = CharSet.Unicode)]
        private static extern bool HidD_GetManufacturerString(IntPtr hidDeviceObject, byte[] buffer, int bufferLength);

        [DllImport("hid.dll", CharSet = CharSet.Unicode)]
        private static extern bool HidD_GetProductString(IntPtr hidDeviceObject, byte[] buffer, int bufferLength);

        [DllImport("hid.dll", CharSet = CharSet.Unicode)]
        private static extern bool HidD_GetSerialNumberString(IntPtr hidDeviceObject, byte[] buffer, int bufferLength);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_GetPreparsedData(IntPtr hidDeviceObject, out IntPtr preparsedData);

        [DllImport("hid.dll")]
        private static extern bool HidD_FreePreparsedData(IntPtr preparsedData);

        [DllImport("hid.dll")]
        private static extern int HidP_GetCaps(IntPtr preparsedData, ref HIDP_CAPS capabilities);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_GetFeature(IntPtr hidDeviceObject, byte[] reportBuffer, int reportBufferLength);

        // ---------------------------------------------------------------- HidP 字段枚举
        //
        // 为什么不用 IOCTL_HID_GET_REPORT_DESCRIPTOR：
        // 它定义在 hidport.h（内核态微型驱动头文件），微软只在驱动侧提供，
        // 用户态经 CreateFile 打开的句柄发给它，I/O 管理器一律拒绝，
        // 表现为 ERROR_INVALID_FUNCTION（错误 1）。常量算对也没用。
        // 参见 OSR 讨论与 hidport.h 文档。
        //
        // 但是 HidD_GetPreparsedData 是用户态 API，它拿到的 preparsed data
        // 里已经含全部字段信息。HidP_GetValueCaps 可以逐条取出，
        // 直接给出 ReportID / BitSize / ReportCount —— 不需要原始描述符。

        private const int HidP_Feature = 2;
        private const int HIDP_STATUS_SUCCESS = 0x00110000;
        private const int HIDP_STATUS_USAGE_NOT_FOUND = unchecked((int)0xC0110004);

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct HIDP_VALUE_CAPS
        {
            public ushort UsagePage;
            public byte ReportID;
            public byte IsAlias;
            public ushort BitField;
            public ushort LinkCollection;
            public ushort LinkUsage;
            public ushort LinkUsagePage;
            public byte IsRange;
            public byte IsStringRange;
            public byte IsDesignatorRange;
            public byte IsAbsolute;
            public byte HasNull;
            public byte Reserved;
            public ushort BitSize;
            public ushort ReportCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public ushort[] Reserved2;
            public uint UnitsExp;
            public uint Units;
            public int LogicalMin;
            public int LogicalMax;
            public int PhysicalMin;
            public int PhysicalMax;
            // 尾部是 union，Range 与 NotRange 都是 8 个 USHORT，展开即可
            public ushort UsageMin;
            public ushort UsageMax;
            public ushort StringMin;
            public ushort StringMax;
            public ushort DesignatorMin;
            public ushort DesignatorMax;
            public ushort DataIndexMin;
            public ushort DataIndexMax;
        }

        [DllImport("hid.dll")]
        private static extern int HidP_GetValueCaps(int reportType,
            [Out] HIDP_VALUE_CAPS[] valueCaps, ref short valueCapsLength, IntPtr preparsedData);

        // 72 字节：2+1+1+2+2+2+2+4+40（Reserved[10]）+16（union）
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct HIDP_BUTTON_CAPS
        {
            public ushort UsagePage;
            public byte ReportID;
            public byte IsAlias;
            public ushort BitField;
            public ushort LinkCollection;
            public ushort LinkUsage;
            public ushort LinkUsagePage;
            public byte IsRange;
            public byte IsStringRange;
            public byte IsDesignatorRange;
            public byte IsAbsolute;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public uint[] Reserved;
            public ushort UsageMin;
            public ushort UsageMax;
            public ushort StringMin;
            public ushort StringMax;
            public ushort DesignatorMin;
            public ushort DesignatorMax;
            public ushort DataIndexMin;
            public ushort DataIndexMax;
        }

        [DllImport("hid.dll")]
        private static extern int HidP_GetButtonCaps(int reportType,
            [Out] HIDP_BUTTON_CAPS[] buttonCaps, ref short buttonCapsLength, IntPtr preparsedData);

        private const int HidP_Input = 0;
        private const int HidP_Output = 1;

        // ---------------------------------------------------------------- 对外入口

        /// <summary>
        /// 扫描全部 HID 设备并生成报告文本。
        /// </summary>
        /// <param name="verboseAll">
        /// true = 所有设备都展开（会很长）；false = 只展开罗技设备（VID 046D），
        /// 其余每个设备只占一行摘要。
        /// </param>
        public static string Scan(bool verboseAll)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== HID 设备探针（只读，不写设备）===");
            sb.AppendLine("时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine();

            List<string> paths = EnumeratePaths();
            if (paths.Count == 0)
            {
                sb.AppendLine("没有枚举到任何 HID 设备接口（SetupAPI 返回 0 个）。");
                return sb.ToString();
            }

            sb.AppendLine("共枚举到 " + paths.Count + " 个 HID 接口。");
            sb.AppendLine();

            int logitechCount = 0;
            int idx = 0;
            foreach (string path in paths)
            {
                idx++;
                try
                {
                    string block;
                    bool expanded = Describe(path, verboseAll, out block);
                    if (expanded) logitechCount++;
                    sb.Append(block);
                }
                catch (Exception ex)
                {
                    sb.AppendLine("[" + idx + "] 读取异常：" + ex.Message);
                    sb.AppendLine("    路径：" + ShortPath(path));
                    sb.AppendLine();
                }
            }

            sb.AppendLine("---");
            sb.AppendLine("日志设备（VID 046D）共 " + logitechCount + " 个接口。");
            if (logitechCount == 0)
            {
                sb.AppendLine("没找到 VID 046D（罗技）的设备。请确认 G940 已插入；");
                sb.AppendLine("设备管理器里能看到这台设备，且没有带感叹号的条目。");
            }
            return sb.ToString();
        }

        // ---------------------------------------------------------------- 枚举与描述

        private static List<string> EnumeratePaths()
        {
            var list = new List<string>();
            Guid guid = GUID_DEVINTERFACE_HID;
            IntPtr set = SetupDiGetClassDevs(ref guid, IntPtr.Zero, IntPtr.Zero,
                DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
            if (set == IntPtr.Zero || set == new IntPtr(-1)) return list;

            try
            {
                var did = new SP_DEVICE_INTERFACE_DATA();
                did.cbSize = Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA));

                for (int i = 0; ; i++)
                {
                    if (!SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref guid, i, ref did)) break;

                    int required;
                    SetupDiGetDeviceInterfaceDetail(set, ref did, IntPtr.Zero, 0, out required, IntPtr.Zero);
                    if (required <= 0) continue;

                    IntPtr detail = Marshal.AllocHGlobal(required);
                    try
                    {
                        // 两个数，别合并：
                        //   headSize（cbSize）：x64 = 8，x86 = 6 —— 结构体尾部按 8 对齐补齐。
                        //   路径起点：恒为 4 —— DevicePath 紧跟 DWORD cbSize（WCHAR 对齐 2）。
                        // 1.3.39 曾按 "两者是同一个值" 把路径改成 +8，读出来是 "id#vid_046d&…"，
                        // 少了开头 4 个字符，整个通道打不开。真机实测 +4 才对，本版改回。
                        int headSize = IntPtr.Size == 8 ? 8 : 6;
                        Marshal.WriteInt32(detail, 0, headSize);
                        if (SetupDiGetDeviceInterfaceDetail(set, ref did, detail, required, out required, IntPtr.Zero))
                        {
                            string path = Marshal.PtrToStringUni(new IntPtr(detail.ToInt64() + 4));
                            if (!string.IsNullOrEmpty(path)) list.Add(path);
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

        /// <summary>描述一个设备。返回值表示是否做了展开（用于统计罗技设备数）。</summary>
        private static bool Describe(string path, bool verboseAll, out string block)
        {
            var sb = new StringBuilder();

            IntPtr h = CreateFile(path, GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);

            // 有些系统设备只允许只读打开
            if (h == IntPtr.Zero || h == new IntPtr(-1))
                h = CreateFile(path, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE,
                    IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);

            if (h == IntPtr.Zero || h == new IntPtr(-1))
            {
                int err = Marshal.GetLastWin32Error();
                sb.AppendLine("路径：" + ShortPath(path));
                sb.AppendLine("    打不开（Win32 错误 " + err + "：" + ErrorText(err) + "）");
                sb.AppendLine();
                block = sb.ToString();
                return false;
            }

            try
            {
                var attrs = new HIDD_ATTRIBUTES();
                attrs.Size = Marshal.SizeOf(typeof(HIDD_ATTRIBUTES));
                bool hasAttrs = HidD_GetAttributes(h, ref attrs);

                string product = ReadString(HidD_GetProductString, h);
                string vendor = ReadString(HidD_GetManufacturerString, h);
                string serial = ReadString(HidD_GetSerialNumberString, h);

                bool isLogitech = hasAttrs && attrs.VendorID == 0x046D;
                bool expand = verboseAll || isLogitech;

                string head = "VID " + (hasAttrs ? attrs.VendorID.ToString("X4") : "????")
                            + " PID " + (hasAttrs ? attrs.ProductID.ToString("X4") : "????")
                            + "  " + (product.Length > 0 ? product : "(无产品名)")
                            + (vendor.Length > 0 ? "  [" + vendor + "]" : "");

                if (!expand)
                {
                    sb.AppendLine(head);
                    block = sb.ToString();
                    return false;
                }

                sb.AppendLine("════════════════════════════════════════");
                sb.AppendLine(head);
                if (attrs.VersionNumber != 0)
                    sb.AppendLine("    版本号：" + attrs.VersionNumber.ToString("X4"));
                if (serial.Length > 0)
                    sb.AppendLine("    序列号：" + serial);
                sb.AppendLine("    路径：" + path);

                HIDP_CAPS caps = new HIDP_CAPS();
                List<int> featureIds = new List<int>();
                IntPtr preparsed;
                if (HidD_GetPreparsedData(h, out preparsed))
                {
                    try
                    {
                        if (HidP_GetCaps(preparsed, ref caps) >= 0)
                        {
                            sb.AppendLine("    UsagePage:Usage = " + caps.UsagePage.ToString("X4")
                                        + ":" + caps.Usage.ToString("X4"));
                            sb.AppendLine("    报告长度  输入 " + caps.InputReportByteLength
                                        + " / 输出 " + caps.OutputReportByteLength
                                        + " / Feature " + caps.FeatureReportByteLength + " 字节");
                            sb.AppendLine("    字段计数  输入 " + caps.NumberInputValueCaps + "v/"
                                        + caps.NumberInputButtonCaps + "b"
                                        + "  输出 " + caps.NumberOutputValueCaps + "v/"
                                        + caps.NumberOutputButtonCaps + "b"
                                        + "  Feature " + caps.NumberFeatureValueCaps + "v/"
                                        + caps.NumberFeatureButtonCaps + "b");
                        }
                        // 输入报告只报汇总：摇杆按键几十条，逐条列会淹掉重点
                        AppendCaps(sb, preparsed, HidP_Input, "输入报告", false);
                        AppendCaps(sb, preparsed, HidP_Output, "输出报告", true);
                        featureIds = AppendCaps(sb, preparsed, HidP_Feature, "Feature 报告", true);
                    }
                    finally
                    {
                        HidD_FreePreparsedData(preparsed);
                    }
                }
                else
                {
                    sb.AppendLine("    HidD_GetPreparsedData 失败（错误 "
                                + Marshal.GetLastWin32Error() + "）");
                }

                string regDiag;
                byte[] desc = ReadDescriptorFromRegistry(path, out regDiag);
                if (desc != null && desc.Length > 0)
                {
                    sb.AppendLine("    原始描述符 " + desc.Length + " 字节（注册表）：");
                    sb.AppendLine("      " + HidDescriptor.Hex(desc));
                    HidDescriptorInfo info = HidDescriptor.Parse(desc);
                    sb.AppendLine("    解析：UsagePage=" + info.UsagePage
                                + "；" + info.Summary());
                }
                else
                {
                    sb.AppendLine("    原始描述符：读不到（" + regDiag + "）");
                    sb.AppendLine("      不影响结论 —— 上面的 Feature 字段是 HidP_GetValueCaps 给的，");
                    sb.AppendLine("      不依赖描述符。用户态本来就发不出 IOCTL_HID_GET_REPORT_DESCRIPTOR。");
                }

                // 按 caps 枚举出的报告 ID 逐条读当前值（只读，GetFeature）。
                // 只对通用桌面页（0x0001）与厂商页（0xFF00）做：
                // 某些驱动（如 Microsoft VHF）GetFeature 会超时到 6 秒一次，
                // 一个设备读几条就能让整轮探针卡十几秒，纯属白等。
                bool worthRead = caps.UsagePage == 0x0001 || caps.UsagePage == 0xFF00;
                if (!worthRead) featureIds.Clear();

                for (int i = 0; i < featureIds.Count; i++)
                {
                    int id = featureIds[i];
                    int len = FeatureReportLength(id, caps);
                    if (len <= 0) len = 1;
                    byte[] buf = new byte[len];
                    if (id != 0) buf[0] = (byte)id;
                    if (HidD_GetFeature(h, buf, buf.Length))
                    {
                        sb.AppendLine("    Feature(" + id + ") 当前值：" + HidDescriptor.Hex(buf));

                        // G940 的灯就是这一条：ID=3，长度 ≥3，byte1 红 byte2 绿。
                        // 只给 hex 的话，「灯不亮」到底是主程序没写上、还是写成了全灭，
                        // 看的人分不出来——所以这里顺手翻成人话。
                        if (id == G940Hid.LedReportId && len >= 3
                            && hasAttrs && attrs.ProductID == G940Hid.TargetPid)
                        {
                            sb.AppendLine("        ↑ 灯位图：红 " + LedBits(buf[1])
                                        + "；绿 " + LedBits(buf[2])
                                        + "；琥珀 " + LedBits((byte)(buf[1] & buf[2]))
                                        + "（bit0=P1 … bit7=P8）");
                        }
                    }
                    else
                        sb.AppendLine("    Feature(" + id + ") 读取失败（错误 "
                                    + Marshal.GetLastWin32Error() + "）");
                }

                sb.AppendLine();
                block = sb.ToString();
                return true;
            }
            finally
            {
                CloseHandle(h);
            }
        }

        /// <summary>
        /// 枚举某种报告的字段（value + button 两类都要）。
        /// 返回该报告类型里出现过的 report ID，供随后读当前值用。
        ///
        /// 为什么 button 也要看：HID 描述符里 Report Size=1 的 Feature 字段
        /// 会被 HidP 归到 button caps，value caps 里根本看不到——Feature 3 的
        /// 灯位图正是这种形态（1.3.36 探针实测）。
        /// </summary>
        private static List<int> AppendCaps(StringBuilder sb, IntPtr preparsed,
            int reportType, string label, bool detail)
        {
            var ids = new List<int>();

            var vbuf = new HIDP_VALUE_CAPS[128];
            short vcount = 128;
            int vst = HidP_GetValueCaps(reportType, vbuf, ref vcount, preparsed);

            var bbuf = new HIDP_BUTTON_CAPS[128];
            short bcount = 128;
            int bst = HidP_GetButtonCaps(reportType, bbuf, ref bcount, preparsed);

            bool vok = vst == HIDP_STATUS_SUCCESS && vcount > 0;
            bool bok = bst == HIDP_STATUS_SUCCESS && bcount > 0;

            if (!vok && !bok)
            {
                sb.AppendLine("    " + label + "：没有字段（状态 0x" + vst.ToString("X8") + "）");
                return ids;
            }

            sb.AppendLine("    " + label + "：value " + (vok ? vcount : 0)
                        + " 组 / button " + (bok ? bcount : 0) + " 组");

            var bitsPerId = new Dictionary<int, int>();
            var spanPerId = new Dictionary<int, int>();   // 该 ID 下 button range 的编号跨度（仅作线索）

            if (vok)
            {
                for (int i = 0; i < vcount; i++)
                {
                    HIDP_VALUE_CAPS c = vbuf[i];
                    int bits = c.BitSize * c.ReportCount;
                    if (detail)
                        sb.AppendLine("      [v" + (i + 1) + "] ID=" + c.ReportID
                                    + "  Page=" + c.UsagePage.ToString("X4")
                                    + "  Usage=" + UsageText(c.IsRange != 0, c.UsageMin, c.UsageMax)
                                    + "  Size=" + c.BitSize
                                    + "  Count=" + c.ReportCount
                                    + "  → " + bits + " bit");
                    Note(ids, bitsPerId, c.ReportID, bits);
                }
            }

            if (bok)
            {
                for (int i = 0; i < bcount; i++)
                {
                    HIDP_BUTTON_CAPS c = bbuf[i];

                    // IsRange=0：单条按钮，HID 里一个按钮固定占 1 bit，可以算。
                    // IsRange=1：UsageMin~UsageMax 描述的是这组按钮"逻辑上支持的编号集合"，
                    //   不等于报告里实际的位数。真机上见过填 0x0001~0x02FF（767 个）而该设备
                    //   报告长度只有 5 字节的，按 767 bit 算会得出 96 字节这种不可能的数。
                    //   这种一律不计入合计，只标注范围，免得合计行给出错误结论。
                    if (c.IsRange == 0)
                    {
                        if (detail)
                            sb.AppendLine("      [b" + (i + 1) + "] ID=" + c.ReportID
                                        + "  Page=" + c.UsagePage.ToString("X4")
                                        + "  Usage=" + c.UsageMin.ToString("X4")
                                        + "  Count=1  → 1 bit");
                        Note(ids, bitsPerId, c.ReportID, 1);
                    }
                    else
                    {
                        int span = c.UsageMax >= c.UsageMin ? (c.UsageMax - c.UsageMin + 1) : 0;
                        if (detail)
                            sb.AppendLine("      [b" + (i + 1) + "] ID=" + c.ReportID
                                        + "  Page=" + c.UsagePage.ToString("X4")
                                        + "  Usage=" + UsageText(true, c.UsageMin, c.UsageMax)
                                        + "  跨度=" + span + " 个编号"
                                        + "  （编号集合≠报告位数，不计入合计）");
                        // 仍记下 ID，后面的读当前值要用它；位宽记 0 表示"算不出"。
                        // 跨度单独记一份：G940 灯的 16 个位就是这种形态（一条 range 装 8 红 + 8 绿），
                        // 只报"算不出"会让人误以为那组字段跟灯无关。
                        Note(ids, bitsPerId, c.ReportID, 0);
                        int oldSpan;
                        spanPerId.TryGetValue(c.ReportID, out oldSpan);
                        if (span > oldSpan) spanPerId[c.ReportID] = span;
                    }
                }
            }

            foreach (int id in ids)
            {
                int bits;
                int span;
                spanPerId.TryGetValue(id, out span);

                // 同一个 ID 下既有 value 字段、又有 button range 时，value 的合计往往远小于实际位宽。
                // G940 的灯就是这样：Feature 3 的 value 只有 1 bit，真正的 16 个灯位在那条 range 里。
                // 早先只报"合计 1 bit"，看着像跟灯无关，于是找错了三轮通道——这里必须把线索摆出来。
                string spanHint = span > 0
                    ? "  ⚠ 另有 button range 跨 " + span + " 个编号，实际位宽可能就是它"
                    : "";

                if (!bitsPerId.TryGetValue(id, out bits) || bits <= 0)
                {
                    sb.AppendLine("      → ID=" + id + " 位宽无法从 caps 推算（只有 button range 字段）"
                                + (span > 0 ? "，跨度 " + span + " 个编号" : ""));
                    continue;
                }
                sb.AppendLine("      → ID=" + id + " 合计 " + bits + " bit = "
                            + ((bits + 7) / 8) + " 字节数据"
                            + (id != 0 ? " + ID = " + (((bits + 7) / 8) + 1) + " 字节" : "")
                            + spanHint);
            }
            return ids;
        }

        private static void Note(List<int> ids, Dictionary<int, int> bits, int id, int n)
        {
            if (!ids.Contains(id)) ids.Add(id);
            int sum;
            bits.TryGetValue(id, out sum);
            bits[id] = sum + n;
        }

        private static string UsageText(bool isRange, ushort min, ushort max)
        {
            if (!isRange) return min.ToString("X4");
            if (min == max) return min.ToString("X4");
            return min.ToString("X4") + "~" + max.ToString("X4");
        }

        /// <summary>Feature 报告要发几个字节。caps 里那条是"最长报告"的长度。</summary>
        private static int FeatureReportLength(int reportId, HIDP_CAPS caps)
        {
            // FeatureReportByteLength 是本设备"最长那条" Feature 报告的总长（含 ID 字节）。
            // HidD_GetFeature 要求缓冲区不小于该值，给足即可。
            int total = caps.FeatureReportByteLength;
            if (total <= 0) return 1;
            return total;
        }

        /// <summary>
        /// 原始描述符：HID 类驱动把它缓存在设备实例的 Device Parameters 里。
        /// 接口路径 \\?\hid#vid_046d&amp;pid_c287#8&amp;20f756a8&amp;0&amp;0000#{guid}
        /// 对应 HID\VID_046D&amp;PID_C287\8&amp;20f756a8&amp;0&amp;0000。
        ///
        /// 这条路是用户态唯一能拿到原始描述符的方式（IOCTL 那条走不通）。
        /// 失败时把原因写进 diag，便于定位是权限、路径还是值缺失。
        /// </summary>
        private static byte[] ReadDescriptorFromRegistry(string interfacePath, out string diag)
        {
            diag = "";

            string s = interfacePath;
            if (s.StartsWith(@"\\?\", StringComparison.Ordinal)) s = s.Substring(4);
            int mark = s.LastIndexOf("#{", StringComparison.Ordinal);
            if (mark > 0) s = s.Substring(0, mark);

            string[] parts = s.Split('#');
            if (parts.Length < 3)
            {
                diag = "接口路径段数不足(" + parts.Length + ")";
                return null;
            }

            string instance = @"HID\" + parts[1].ToUpperInvariant() + @"\" + parts[2];
            byte[] got = ReadUnder(@"SYSTEM\CurrentControlSet\Enum\" + instance, out diag);
            if (got != null) return got;

            // 不扫 Enum\HID 兜底：真机上实测键存在但值缺失，
            // 扫描只是把每个失败设备都遍历一遍子键，白等十几秒，还扫不出东西。
            return null;
        }

        private static byte[] ReadUnder(string instancePath, out string diag)
        {
            diag = "";
            string full = instancePath + @"\Device Parameters";
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(full))
                {
                    if (key == null)
                    {
                        diag = "键不存在 " + full;
                        return null;
                    }
                    object v = key.GetValue("ReportDescriptor");
                    byte[] b = v as byte[];
                    if (b == null || b.Length == 0)
                    {
                        diag = "值缺失 " + full + "\\ReportDescriptor（类型 " + (v == null ? "null" : v.GetType().Name) + "）";
                        return null;
                    }
                    return b;
                }
            }
            catch (Exception ex)
            {
                diag = ex.GetType().Name + "：" + ex.Message;
                return null;
            }
        }

        private delegate bool StringGetter(IntPtr handle, byte[] buffer, int length);

        private static string ReadString(StringGetter getter, IntPtr h)
        {
            byte[] buf = new byte[256];
            if (!getter(h, buf, buf.Length)) return "";
            string s = Encoding.Unicode.GetString(buf);
            int end = s.IndexOf('\0');
            if (end >= 0) s = s.Substring(0, end);
            return s.Trim();
        }

        /// <summary>只保留路径里 vid/pid 之后的部分，避免整行过长。</summary>
        private static string ShortPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "(空)";
            int i = path.IndexOf("vid_", StringComparison.OrdinalIgnoreCase);
            if (i < 0) return path;
            int j = path.IndexOf('#', i);
            if (j < 0) return path.Substring(i);
            int k = path.IndexOf('#', j + 1);
            string body = k < 0 ? path.Substring(i) : path.Substring(i, k - i);
            return "hid:" + body;
        }

        /// <summary>把 8 位灯掩码翻成「点亮了哪几颗」，bit0 = P1 … bit7 = P8。</summary>
        private static string LedBits(byte mask)
        {
            if (mask == 0) return "全灭";
            if (mask == 0xFF) return "全部 8 颗";

            var sb = new StringBuilder();
            for (int bit = 0; bit < 8; bit++)
            {
                if ((mask & (1 << bit)) == 0) continue;
                if (sb.Length > 0) sb.Append(',');
                sb.Append('P').Append(bit + 1);
            }
            return sb.ToString();
        }

        private static string ErrorText(int err)
        {
            switch (err)
            {
                case 2: return "设备不存在";
                case 5: return "拒绝访问（权限不足）";
                case 32: return "被占用（有程序独占打开了该接口）";
                case 87: return "参数错误";
                case 1168: return "找不到元素";
                default: return "未知";
            }
        }
    }
}
