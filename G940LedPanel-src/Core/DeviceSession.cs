using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace G940LedPanel.Core
{
    /// <summary>
    /// G940 设备会话：手写 DirectInput8 COM 互操作（不依赖任何第三方库），
    /// 负责设备枚举、连接、轮询；灯的写入交给 G940Hid（HID Feature 报告直写）。
    ///
    /// 两条通道是分开的：输入走 DirectInput，灯走 HID。1.3.38 之前灯挂在
    /// 第三方 DLL 上、要靠 DirectInput 的设备指针才能写，现在解耦了——
    /// DirectInput 打不开时灯照样能亮，HID 打不开时输入照样能读。
    /// 设备指针（LPDIRECTINPUTDEVICE8）即 IDirectInputDevice8W 接口指针，
    /// 与 FreePIE/SlimDX 方案一致。所有常量与 vtable 槽位均按 dinput.h（Windows SDK）核对。
    /// </summary>
    public class DeviceSession : IDisposable
    {
        // ---- dinput.h 常量 ----
        private const int DI8DEVCLASS_GAMECTRL = 4;
        private const int DIEDFL_ATTACHEDONLY = 0x00000001;
        private const int DISCL_NONEXCLUSIVE = 0x00000002;
        private const int DISCL_BACKGROUND = 0x00000008;
        private const int DISCL_FOREGROUND = 0x00000001;
        private const int DISCL_EXCLUSIVE = 0x00000010;
        private const int DIJOFS_BUTTON0 = 48;          // DIJOYSTATE2 按钮区偏移（dinput.h：DIJOFS_BUTTON0）
        private const int DIJOYSTATE2_SIZE = 272;       // sizeof(DIJOYSTATE2)：6轴24+2滑杆8+POV16+按钮128+速度/加速度/力96（dinput.h 核对）
        private const int DIJOYSTATE_SIZE = 80;         // sizeof(DIJOYSTATE)：6轴24+2滑杆8+POV16+按钮32（参考项目验证可用的格式）
        private const int DIDFT_AXIS = 0x00000002;
        private const int DIDFT_BUTTON = 0x00000004;
        private const int DIDFT_POV = 0x00000010;
        private const int DIDFT_ANYINSTANCE = 0x00FFFF00;
        private const int DIDFT_OPTIONAL = unchecked((int)0x80000000);
        private const int DIDF_ABSAXIS = 0x00000001;
        private const int DIERR_INPUTLOST = unchecked((int)0x8007001E);   // ERROR_READ_FAULT：状态丢失，需重新获取（dinput.h 已核对）
        private const int DIERR_NOTACQUIRED = unchecked((int)0x8007000C); // ERROR_INVALID_ACCESS：未获取设备（dinput.h 已核对）
        private const int DIERR_OTHERAPPHASPRIO = unchecked((int)0x80070005); // E_ACCESSDENIED：被其它程序独占（dinput.h 已核对）
        private static readonly Guid IID_IDirectInput8W =
            new Guid("BF798031-483A-4DA2-AA99-5D64ED369700");   // 已按 dinput.h 核对
        private static readonly Guid GuidButton =
            new Guid("A36D02F0-C9F3-11CF-BFC7-00A0C905CB5C");   // GUID_Button
        private static readonly Guid GuidXAxis =
            new Guid("A36D02E0-C9F3-11CF-BFC7-00A0C905CB5C");   // GUID_XAxis（即 GUID_Axis）
        private static readonly Guid GuidYAxis =
            new Guid("A36D02E1-C9F3-11CF-BFC7-00A0C905CB5C");
        private static readonly Guid GuidZAxis =
            new Guid("A36D02E2-C9F3-11CF-BFC7-00A0C905CB5C");
        private static readonly Guid GuidRxAxis =
            new Guid("A36D02F4-C9F3-11CF-BFC7-00A0C905CB5C");
        private static readonly Guid GuidRyAxis =
            new Guid("A36D02F5-C9F3-11CF-BFC7-00A0C905CB5C");
        private static readonly Guid GuidRzAxis =
            new Guid("A36D02E3-C9F3-11CF-BFC7-00A0C905CB5C");
        private static readonly Guid GuidSlider =
            new Guid("A36D02E4-C9F3-11CF-BFC7-00A0C905CB5C");
        private static readonly Guid GuidPov =
            new Guid("A36D02F2-C9F3-11CF-BFC7-00A0C905CB5C");   // GUID_POV
        private const int DIDOI_ASPECTPOSITION = 0x00000100;    // 轴/滑杆对象标志（dinput.h 1029 行）

        // ---- dinput8.dll / kernel32 ----
        [DllImport("dinput8.dll")]
        private static extern int DirectInput8Create(IntPtr hinst, int dwVersion,
            ref Guid riidltf, out IntPtr ppvOut, IntPtr punkOuter);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        // ---- COM vtable 委托（槽位号见 dinput.h 接口声明顺序） ----
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint ComRelease(IntPtr self);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int CreateDeviceFn(IntPtr self, ref Guid rguid, out IntPtr device, IntPtr pUnkOuter);       // 3
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int EnumDevicesFn(IntPtr self, int dwDevType, EnumDevicesCallback cb, IntPtr pvRef, int dwFlags); // 4
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetCapabilitiesFn(IntPtr self, IntPtr lpDidevCaps);                                      // 3(设备)
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int AcquireFn(IntPtr self);                                                                  // 7
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int UnacquireFn(IntPtr self);                                                                // 8
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetDeviceStateFn(IntPtr self, int cbData, IntPtr lpData);                                // 9
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetDeviceDataFn(IntPtr self, int cbObjectData, IntPtr rgdod, ref int pdwInOut, int dwFlags); // 10
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int SetDataFormatFn(IntPtr self, IntPtr lpdiDataFormat);                                     // 11
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int SetCooperativeLevelFn(IntPtr self, IntPtr hwnd, int dwFlags);                            // 13
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int PollFn(IntPtr self);                                                                     // 25

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int EnumDevicesCallback(IntPtr lpddi, IntPtr pvRef);

        private IntPtr _dinput;
        private IntPtr _device;
        private G940Hid _hid;            // 灯通道（HID Feature 报告），与 _device 独立
        private int _stateSize = DIJOYSTATE_SIZE;
        private string _activeFormat = "dev.fmt.unset"; // 当前生效的数据格式：存语言包 key（dev.fmt.standard / dev.fmt.fallback）
        private bool _acquired;      // 设备获取状态跟踪


        /// <summary>连接过程的补充说明（如：标准数据格式失败已回退、协作级别未设置等）。</summary>
        public string LastWarnings { get; private set; } = "";

        /// <summary>最近一次按钮状态读取的错误（成功读取时清空），供 UI 诊断。</summary>
        public string ReadError { get; private set; } = "";

        /// <summary>最近一次 Poll / GetDeviceState 的 HRESULT，供诊断。</summary>
        public int LastPollHr { get; private set; }
        public int LastStateHr { get; private set; }

        /// <summary>最近一次 Acquire 的 HRESULT，供诊断。</summary>
        public int LastAcquireHr { get; private set; } = 1;

        /// <summary>最近一次 SetCooperativeLevel 的 HRESULT，供诊断。</summary>
        public int LastCoopHr { get; private set; } = 1;
        public string LastCoopMode { get; private set; } = "dev.fmt.unset";

        /// <summary>设备获取状态文案（供诊断）。</summary>
        public string AcquireStatus =>
            _device == IntPtr.Zero ? L.T("dev.acq.notconnected")
            : (_acquired ? L.T("dev.acq.got") : L.T("dev.acq.notgot", Hr(LastAcquireHr)));

        public DeviceInfo Info { get; private set; }
        public bool Connected => _device != IntPtr.Zero;
        public IntPtr DevicePointer => _device;

        private static T Vtbl<T>(IntPtr obj, int slot) where T : class
        {
            IntPtr vtbl = Marshal.ReadIntPtr(obj);
            IntPtr fn = Marshal.ReadIntPtr(vtbl, slot * IntPtr.Size);
            return (T)(object)Marshal.GetDelegateForFunctionPointer(fn, typeof(T));
        }

        private static void ReleaseCom(IntPtr obj)
        {
            if (obj == IntPtr.Zero) return;
            try { Vtbl<ComRelease>(obj, 2)(obj); } catch (Exception) { }
        }

        private static Guid ReadGuid(IntPtr p, int offset)
        {
            int a = Marshal.ReadInt32(p, offset);
            short b = Marshal.ReadInt16(p, offset + 4);
            short c = Marshal.ReadInt16(p, offset + 6);
            var d = new byte[8];
            for (int i = 0; i < 8; i++) d[i] = Marshal.ReadByte(p, offset + 8 + i);
            return new Guid(a, b, c, d);
        }

        private static string Hr(int hr) => "HRESULT=0x" + ((uint)hr).ToString("X8");

        /// <summary>枚举当前已连接的游戏控制器（杆/油门/脚舵等）。</summary>
        public static List<DeviceInfo> Enumerate()
        {
            var list = new List<DeviceInfo>();
            IntPtr di = IntPtr.Zero;
            try
            {
                Guid iid = IID_IDirectInput8W;
                int hr = DirectInput8Create(GetModuleHandle(null), 0x0800, ref iid, out di, IntPtr.Zero);
                if (hr < 0 || di == IntPtr.Zero) return list;

                var cb = new EnumDevicesCallback((p, r) =>
                {
                    if (p == IntPtr.Zero) return 1;
                    // DIDEVICEINSTANCEW 布局（dinput.h）：
                    //   +0   dwSize          +4   guidInstance    +20  guidProduct
                    //   +36  dwDevType       +40  tszInstanceName (260 wchar)
                    //   +560 tszProductName  (260 wchar)
                    // 1.3.33 及以前这里两个字符串标反了（40 当产品名、560 当实例名），
                    // 注释也跟着错。不影响能不能连上（两个字符串通常都含型号名），
                    // 但会让"按产品名精确匹配设备"这件事失去依据。1.3.34 按 dinput.h 改回。
                    var info = new DeviceInfo
                    {
                        InstanceGuid = ReadGuid(p, 4),                        // DIDEVICEINSTANCEW.guidInstance
                        InstanceName = Marshal.PtrToStringUni(p + 40) ?? "",  // tszInstanceName
                        ProductName = Marshal.PtrToStringUni(p + 560) ?? ""   // tszProductName
                    };
                    list.Add(info);
                    return 1; // 继续枚举
                });

                var enumFn = Vtbl<EnumDevicesFn>(di, 4);
                enumFn(di, DI8DEVCLASS_GAMECTRL, cb, IntPtr.Zero, DIEDFL_ATTACHEDONLY);
            }
            catch (Exception)
            {
                // 无 DirectInput 或未连接任何设备
            }
            finally
            {
                ReleaseCom(di);
            }
            return list;
        }

        /// <summary>
        /// 连接指定设备。失败时通过 error 返回“步骤 + HRESULT”的可读原因。
        /// </summary>
        public bool Connect(DeviceInfo info, IntPtr hwnd, out string error)
        {
            error = "";
            LastWarnings = "";
            Disconnect(false);   // 收拾旧连接：马上要重开并重写灯，别先闪一遍全绿
            if (info == null) { error = L.T("dev.err.info.null"); return false; }

            // 0) 先开灯通道。8 颗灯是本程序的主职能，HID 打不开就没必要继续；
            //    而且放在最前面的另一个理由：这里的失败原因比 DirectInput 那串 HRESULT 直白得多，
            //    用户看一眼就知道是没插油门、还是权限不够。
            //    输入与灯是两条独立通道，开关灯不依赖 DirectInput 是否成功。
            G940Hid hid;
            string hidErr;
            if (!G940Hid.TryOpen(out hid, out hidErr))
            {
                error = hidErr;
                return false;
            }
            _hid = hid;

            try
            {
                // 1) 创建 DirectInput8 对象
                Guid iidLocal = IID_IDirectInput8W;
                int hr = DirectInput8Create(GetModuleHandle(null), 0x0800, ref iidLocal, out _dinput, IntPtr.Zero);
                if (hr < 0 || _dinput == IntPtr.Zero)
                {
                    error = L.T("dev.err.di.create", Hr(hr));
                    Disconnect();
                    return false;
                }

                // 2) 创建设备
                var create = Vtbl<CreateDeviceFn>(_dinput, 3);
                Guid guid = info.InstanceGuid;
                hr = create(_dinput, ref guid, out _device, IntPtr.Zero);
                if (hr < 0 || _device == IntPtr.Zero)
                {
                    error = L.T("dev.err.dev.create", Hr(hr));
                    Disconnect();
                    return false;
                }

                // 3) 数据格式：标准 DIJOYSTATE2，被拒时退回"128 按钮"兜底格式。
                //    以前还有一个"设备原生对象格式"（按 EnumObjects 自建），
                //    它防的是"固件把按钮塞在非标准偏移"这种极罕见情况；
                //    实际这台 G940 标准格式完全够用，两套格式反而每次改设备层都要同步维护，
                //    因此按需求删掉，只留标准 + 兜底。
                _objects = null;      // 重新枚举（诊断要列设备对象）
                bool useFallback = false;
                IntPtr fmt = BuildStandardFormat();
                _activeFormat = "dev.fmt.standard";
                if (fmt == IntPtr.Zero) { useFallback = true; fmt = BuildButtonFormat(); _activeFormat = "dev.fmt.fallback"; }
                if (fmt == IntPtr.Zero)
                {
                    error = L.T("dev.err.fmt.build");
                    Disconnect();
                    return false;
                }

                var sdf = Vtbl<SetDataFormatFn>(_device, 11);
                hr = sdf(_device, fmt);
                if (hr < 0)
                {
                    // 标准格式被拒 → 用兜底按钮格式再试一次
                    IntPtr fmt2 = IntPtr.Zero;
                    if (!useFallback) fmt2 = BuildButtonFormat();
                    if (fmt2 != IntPtr.Zero)
                    {
                        hr = sdf(_device, fmt2);
                        if (hr >= 0)
                        {
                            _activeFormat = "dev.fmt.fallback";
                            LastWarnings += L.T("dev.warn.fallback");
                        }
                    }
                    if (hr < 0)
                    {
                        error = L.T("dev.err.fmt.set", Hr(hr));
                        Disconnect();
                        return false;
                    }
                }

                // 4) 协作级别：与参考项目一致，hwnd 传 0（摇杆设备允许 NULL）。
                //    三模式依次尝试（后台非独占 → 前台非独占 → 前台独占）。
                var scl = Vtbl<SetCooperativeLevelFn>(_device, 13);
                IntPtr hwndCoop = IntPtr.Zero; // 参考实现 SetCooperativeLevel(0, ...)
                int[] coopModes = new int[]
                {
                    DISCL_NONEXCLUSIVE | DISCL_BACKGROUND,      // 后台非独占（常驻/托盘场景）
                    DISCL_NONEXCLUSIVE | DISCL_FOREGROUND,       // 前台非独占
                    DISCL_EXCLUSIVE | DISCL_FOREGROUND,          // 前台独占
                };
                string[] coopKeys = new string[] { "dev.coop.bg.nonexcl", "dev.coop.fg.nonexcl", "dev.coop.fg.excl" };
                LastCoopHr = 1;
                LastCoopMode = "dev.fmt.unset";
                for (int m = 0; m < coopModes.Length; m++)
                {
                    int h = scl(_device, hwndCoop, coopModes[m]);
                    LastCoopHr = h;
                    if (h >= 0)
                    {
                        LastCoopMode = coopKeys[m];
                        break;
                    }
                    LastWarnings += L.T("dev.warn.coop.item", L.T(coopKeys[m]), Hr(h));
                }
                if (LastCoopHr < 0)
                    LastWarnings += L.T("dev.warn.coop.fail");

                // 5) 按钮数
                try { info.ButtonCount = GetButtonCount(); } catch { info.ButtonCount = 0; }
                Info = info;

                // 6) 获取设备（失败不阻断；轮询时会自动重试）
                var acq = Vtbl<AcquireFn>(_device, 7);
                hr = acq(_device);
                LastAcquireHr = hr;
                _acquired = hr >= 0;
                if (hr < 0)
                {
                    try
                    {
                        var unacq = Vtbl<UnacquireFn>(_device, 8);
                        unacq(_device);
                        hr = acq(_device);
                        LastAcquireHr = hr;
                        _acquired = hr >= 0;
                    }
                    catch (Exception) { }
                    if (hr < 0)
                        LastWarnings += L.T("dev.warn.acquire", Hr(hr));
                }
                return true;
            }
            catch (Exception ex)
            {
                error = L.T("dev.err.connect", ex.Message);
                Disconnect();
                return false;
            }
        }

        /// <summary>
        /// 自建标准 DIJOYSTATE 数据格式（80 字节，32 按钮），**逐字段复刻**成熟项目
        /// MvRens/G940LEDControl 中验证可用的 c_dfDIJoystick（DirectX/DirectInput.pas 常量）：
        /// - 6 轴分别使用 GUID_XAxis/YAxis/ZAxis/RxAxis/RyAxis/RzAxis，并带 DIDOI_ASPECTPOSITION；
        /// - 2 滑杆用 GUID_Slider；4 POV 用 GUID_POV；
        /// - **32 个按钮 pguid = NULL（仅按类型匹配）** —— 这是关键：pguid 非空时按 GUID 精确匹配，
        ///   一旦设备对象 GUID 对不上就映射不到任何数据（GetDeviceState 成功但缓冲区全 0）。
        /// 与 dinput8.dll 导出的 c_dfDIJoystick 等价，且不依赖运行时导出。
        /// </summary>
        private IntPtr BuildStandardFormat()
        {
            try
            {
                const int nAxes = 6, nSliders = 2, nPovs = 4, nButtons = 32; // DIJOYSTATE：32 按钮（G940 仅 12/9 个，足够）
                int n = nAxes + nSliders + nPovs + nButtons;
                int objSize = Marshal.SizeOf(typeof(NativeObjectFormat)); // x64=24 / x86=16
                Guid[] axisGuids = new Guid[] { GuidXAxis, GuidYAxis, GuidZAxis, GuidRxAxis, GuidRyAxis, GuidRzAxis };
                IntPtr[] axisPtrs = new IntPtr[nAxes];
                for (int i = 0; i < nAxes; i++)
                {
                    axisPtrs[i] = Marshal.AllocHGlobal(16);
                    Marshal.Copy(axisGuids[i].ToByteArray(), 0, axisPtrs[i], 16);
                }
                IntPtr guidSlider = Marshal.AllocHGlobal(16);
                Marshal.Copy(GuidSlider.ToByteArray(), 0, guidSlider, 16);
                IntPtr guidPov = Marshal.AllocHGlobal(16);
                Marshal.Copy(GuidPov.ToByteArray(), 0, guidPov, 16);

                IntPtr objs = Marshal.AllocHGlobal(objSize * n);
                int k = 0;
                int axisType = DIDFT_AXIS | DIDFT_ANYINSTANCE | DIDFT_OPTIONAL;
                // 6 轴：X@0 Y@4 Z@8 Rx@12 Ry@16 Rz@20，独立 GUID + DIDOI_ASPECTPOSITION
                for (int i = 0; i < nAxes; i++)
                    WriteFormatObject(objs, k++, axisPtrs[i], i * 4, axisType, DIDOI_ASPECTPOSITION);
                // 2 滑杆 @24,28
                for (int i = 0; i < nSliders; i++)
                    WriteFormatObject(objs, k++, guidSlider, 24 + i * 4, axisType, DIDOI_ASPECTPOSITION);
                // 4 POV @32,36,40,44
                for (int i = 0; i < nPovs; i++)
                    WriteFormatObject(objs, k++, guidPov, 32 + i * 4, DIDFT_POV | DIDFT_ANYINSTANCE | DIDFT_OPTIONAL, 0);
                // 32 按钮 @48..79，pguid = NULL（按类型匹配，同参考实现）
                for (int i = 0; i < nButtons; i++)
                    WriteFormatObject(objs, k++, IntPtr.Zero, DIJOFS_BUTTON0 + i, DIDFT_BUTTON | DIDFT_ANYINSTANCE | DIDFT_OPTIONAL, 0);

                IntPtr fmt = Marshal.AllocHGlobal(IntPtr.Size == 8 ? 32 : 24);
                Marshal.WriteInt32(fmt, 0, IntPtr.Size == 8 ? 32 : 24); // dwSize = sizeof(DIDATAFORMAT)
                Marshal.WriteInt32(fmt, 4, objSize);                     // dwObjSize
                Marshal.WriteInt32(fmt, 8, DIDF_ABSAXIS);                // dwFlags
                Marshal.WriteInt32(fmt, 12, DIJOYSTATE_SIZE);            // dwDataSize = 80（DIJOYSTATE）
                Marshal.WriteInt32(fmt, 16, n);                          // dwNumObjs = 44
                Marshal.WriteIntPtr(fmt, IntPtr.Size == 8 ? 24 : 20, objs); // rgodf
                _stateSize = DIJOYSTATE_SIZE;
                return fmt;
            }
            catch (Exception)
            {
                return IntPtr.Zero;
            }
        }

        /// <summary>向对象数组写入一个 DIOBJECTDATAFORMAT 条目。</summary>
        private void WriteFormatObject(IntPtr objs, int index, IntPtr guid, int ofs, int type, int flags)
        {
            IntPtr p = IntPtr.Add(objs, index * Marshal.SizeOf(typeof(NativeObjectFormat)));
            var o = new NativeObjectFormat { pguid = guid, dwOfs = ofs, dwType = type, dwFlags = flags };
            Marshal.StructureToPtr(o, p, false);
        }

        /// <summary>自建“128 个按钮”的数据格式（与 c_dfDIJoystick2 按钮语义一致，作兜底；x86/x64 通用）。</summary>
        private IntPtr BuildButtonFormat()
        {
            try
            {
                const int n = 128;
                int objSize = Marshal.SizeOf(typeof(NativeObjectFormat)); // x64=24 / x86=16
                IntPtr guidPtr = Marshal.AllocHGlobal(16);
                Marshal.Copy(GuidButton.ToByteArray(), 0, guidPtr, 16);   // 共享同一 GUID_Button

                IntPtr objs = Marshal.AllocHGlobal(objSize * n);
                for (int i = 0; i < n; i++)
                {
                    IntPtr p = IntPtr.Add(objs, i * objSize);
                    var o = new NativeObjectFormat
                    {
                        pguid = guidPtr,
                        dwOfs = DIJOFS_BUTTON0 + i,
                        dwType = DIDFT_BUTTON | DIDFT_ANYINSTANCE | DIDFT_OPTIONAL,
                        dwFlags = 0
                    };
                    Marshal.StructureToPtr(o, p, false);
                }

                IntPtr fmt = Marshal.AllocHGlobal(IntPtr.Size == 8 ? 32 : 24);
                Marshal.WriteInt32(fmt, 0, IntPtr.Size == 8 ? 32 : 24); // dwSize = sizeof(DIDATAFORMAT)
                Marshal.WriteInt32(fmt, 4, objSize);                     // dwObjSize
                Marshal.WriteInt32(fmt, 8, DIDF_ABSAXIS);                // dwFlags
                Marshal.WriteInt32(fmt, 12, DIJOFS_BUTTON0 + n);         // dwDataSize
                Marshal.WriteInt32(fmt, 16, n);                          // dwNumObjs
                Marshal.WriteIntPtr(fmt, IntPtr.Size == 8 ? 24 : 20, objs); // rgodf
                _stateSize = DIJOFS_BUTTON0 + n;
                return fmt;
            }
            catch (Exception)
            {
                return IntPtr.Zero;
            }
        }

        /// <summary>DIDATAFORMAT 里的一个对象条目（DIOBJECTDATAFORMAT，x64 24 字节 / x86 16 字节）。</summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeObjectFormat
        {
            public IntPtr pguid;   // @0（8 字节指针）
            public int dwOfs;      // @8
            public int dwType;     // @12
            public int dwFlags;    // @16 → 对齐后 sizeof = 24，与 x64 版 DIOBJECTDATAFORMAT 一致
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int EnumObjectsFn(IntPtr self, EnumObjectsCallback cb, IntPtr pvRef, int dwFlags);   // 4
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int EnumObjectsCallback(IntPtr lpddi, IntPtr pvRef);

        private const int DIDEVCAPS_SIZE = 44;   // sizeof(DIDEVCAPS)=44（11 个 DWORD，无 dwFFDriverRevision），DirectInput 严格校验 dwSize 必须为此值

        private int GetButtonCount()
        {
            IntPtr caps = Marshal.AllocHGlobal(DIDEVCAPS_SIZE);
            try
            {
                Marshal.WriteInt32(caps, 0, DIDEVCAPS_SIZE); // DIDEVCAPS.dwSize（必须=48，否则 E_INVALIDARG）
                var gc = Vtbl<GetCapabilitiesFn>(_device, 3);
                int hr = gc(_device, caps);
                if (hr < 0) return 0;
                return Marshal.ReadInt32(caps, 16); // dwButtons
            }
            finally
            {
                Marshal.FreeHGlobal(caps);
            }
        }

        /// <summary>设备对象描述（来自 EnumObjects）。</summary>
        public class DeviceObjectInfo
        {
            public string Name;
            public int Offset;
            public int Type;   // DIDFT_* 低位：2=轴 4=按钮 16=POV；高位字节=实例号
            public int Instance => (Type & 0x00FFFF00) >> 8;
            public bool IsButton => (Type & 0xFF) == 4;
            public bool IsAxis => (Type & 0xFF) == 2;
            public bool IsPov => (Type & 0xFF) == 16;
        }

        private List<DeviceObjectInfo> _objects;
        private string _capsSummary;

        /// <summary>枚举设备上所有对象（轴/按钮/POV），首次调用后缓存。用于诊断按钮实际位置。</summary>
        public List<DeviceObjectInfo> GetObjects()
        {
            if (_objects != null) return _objects;
            _objects = new List<DeviceObjectInfo>();
            if (_device == IntPtr.Zero) return _objects;
            try
            {
                var cb = new EnumObjectsCallback((p, r) =>
                {
                    if (p == IntPtr.Zero) return 1;
                    var o = new DeviceObjectInfo
                    {
                        Offset = Marshal.ReadInt32(p, 20),       // DIOBJECTINSTANCEW.dwOfs
                        Type = Marshal.ReadInt32(p, 24),         // dwType
                        Name = Marshal.PtrToStringUni(p + 32) ?? "" // tszName
                    };
                    _objects.Add(o);
                    return 1;
                });
                var fn = Vtbl<EnumObjectsFn>(_device, 4);
                fn(_device, cb, IntPtr.Zero, 0); // dwFlags=0 → 枚举全部对象
            }
            catch (Exception) { }
            return _objects;
        }

        /// <summary>
        /// 当前设备“按钮 0 起 → HID 报告偏移”的映射表（128 项，未映射=-1）。
        /// 按枚举实例号升序排列；供 Raw Input 通道直接按真实报告偏移解析按钮。
        /// </summary>
        public int[] GetButtonOffsets()
        {
            var offs = new int[128];
            for (int i = 0; i < 128; i++) offs[i] = -1;
            try
            {
                var pairs = new List<KeyValuePair<int, int>>();
                foreach (var o in GetObjects())
                    if (o.IsButton)
                        pairs.Add(new KeyValuePair<int, int>(o.Instance, o.Offset));
                pairs.Sort((a, b) => a.Key.CompareTo(b.Key));
                for (int i = 0; i < pairs.Count && i < 128; i++)
                    offs[i] = pairs[i].Value;
            }
            catch (Exception) { }
            return offs;
        }

        /// <summary>GetCapabilities 探测：dwSize 必须=44（已核对 dinput.h：11 字段），失败时尝试其它尺寸并报告。</summary>
        public string GetCapabilitiesSummary()
        {
            if (_capsSummary != null) return _capsSummary;
            if (_device == IntPtr.Zero) return L.T("dev.acq.notconnected");
            int[] sizes = { 44, 48, 52, 56, 60, 64, 72, 80, 100 };
            foreach (int sz in sizes)
            {
                IntPtr caps = Marshal.AllocHGlobal(sz);
                try
                {
                    Marshal.WriteInt32(caps, 0, sz); // DIDEVCAPS.dwSize
                    var gc = Vtbl<GetCapabilitiesFn>(_device, 3);
                    int hr = gc(_device, caps);
                    if (hr >= 0)
                    {
                        _capsSummary = L.T("dev.caps.ok",
                            sz, Marshal.ReadInt32(caps, 8), Marshal.ReadInt32(caps, 12),
                            Marshal.ReadInt32(caps, 16), Marshal.ReadInt32(caps, 20));
                        return _capsSummary;
                    }
                    if (sz == 44) _capsSummary = L.T("dev.caps.fail", Hr(hr));
                }
                finally { Marshal.FreeHGlobal(caps); }
            }
            return _capsSummary;
        }

        /// <summary>当前生效的数据格式名称（标准/兜底按钮），供诊断。</summary>
        public string ActiveFormatName => L.T(_activeFormat);

        /// <summary>实时读取一次设备状态并输出完整快照（含每个枚举对象的当前值），供实时诊断窗口使用。</summary>
        public string ReadLiveSnapshot()
        {
            if (_device == IntPtr.Zero) return L.T("dev.diag.notconn.dev");
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine(L.T("dev.diag.caps", GetCapabilitiesSummary()));
                sb.AppendLine(L.T("dev.diag.format", L.T(_activeFormat), _stateSize));
                var (hr, buf) = ReadRawState();
                if (buf != IntPtr.Zero)
                {
                    try
                    {
                        if (hr >= 0)
                        {
                            ReadError = "";
                            // ★ 标准布局读数放最前（当前格式为标准/兜底时有效；按钮按下会直接反映在这一行）
                            sb.AppendLine(L.T("dev.diag.stdhead"));
                            sb.Append(L.T("dev.diag.axes",
                                Marshal.ReadInt32(buf, 0), Marshal.ReadInt32(buf, 4),
                                Marshal.ReadInt32(buf, 8), Marshal.ReadInt32(buf, 12),
                                Marshal.ReadInt32(buf, 16), Marshal.ReadInt32(buf, 20)));
                            sb.AppendLine(L.T("dev.diag.sliders", Marshal.ReadInt32(buf, 24), Marshal.ReadInt32(buf, 28)));
                            sb.Append(L.T("dev.diag.pov",
                                ((uint)Marshal.ReadInt32(buf, 32)).ToString("X4"),
                                ((uint)Marshal.ReadInt32(buf, 36)).ToString("X4"),
                                ((uint)Marshal.ReadInt32(buf, 40)).ToString("X4"),
                                ((uint)Marshal.ReadInt32(buf, 44)).ToString("X4")));
                            var st = new System.Text.StringBuilder();
                            int btnMax = Math.Min(32, _stateSize - DIJOFS_BUTTON0);
                            for (int i = 0; i < btnMax; i++)
                                st.Append((Marshal.ReadByte(buf, DIJOFS_BUTTON0 + i) & 0x80) != 0 ? "1" : "0");
                            sb.AppendLine(L.T("dev.diag.stdbtn", btnMax, st));

                            // 设备原生对象清单（无读数，仅偏移参考）
                            sb.AppendLine(L.T("dev.diag.objlist"));
                            foreach (var o in GetObjects())
                                sb.AppendLine(L.T("dev.diag.objitem", o.Name, o.Offset,
                                    o.IsButton ? L.T("dev.diag.tab.button")
                                    : o.IsAxis ? L.T("dev.diag.tab.axis")
                                    : o.IsPov ? L.T("dev.diag.tab.pov") : ""));

                            sb.AppendLine(L.T("dev.diag.raw"));
                            for (int i = 0; i < 64; i++)
                            {
                                if (i % 16 == 0 && i > 0) sb.AppendLine();
                                sb.Append(Marshal.ReadByte(buf, i).ToString("X2")).Append(' ');
                            }
                            sb.AppendLine();
                        }
                        else
                        {
                            sb.AppendLine(L.T("dev.diag.readfail", Hr(hr), ReadStateError(hr)));
                        }
                    }
                    finally { Marshal.FreeHGlobal(buf); }
                }
                sb.AppendLine(L.T("dev.diag.poll", Hr(LastPollHr), Hr(LastStateHr),
                    string.IsNullOrEmpty(ReadError) ? L.T("dev.diag.none") : ReadError));
                sb.AppendLine(L.T("dev.diag.acq", AcquireStatus, L.T(LastCoopMode), Hr(LastCoopHr)));
                sb.AppendLine(L.T("dev.diag.events", GetDeviceDataEventsText()));
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return L.T("dev.diag.ex", ex.Message);
            }
        }

        /// <summary>读取并清空设备事件队列（按钮/轴变化事件），返回事件摘要文本。</summary>
        public string GetDeviceDataEventsText()
        {
            if (_device == IntPtr.Zero) return L.T("dev.acq.notconnected");
            var sb = new System.Text.StringBuilder();
            try
            {
                const int objSize = 16; // sizeof(DIDEVICEOBJECTDATA)：4×DWORD
                const int nBuf = 64;
                IntPtr buf = Marshal.AllocHGlobal(objSize * nBuf);
                try
                {
                    int inOut = nBuf;
                    var gdd = Vtbl<GetDeviceDataFn>(_device, 10);
                    int hr = gdd(_device, objSize, buf, ref inOut, 0); // 读取并清空队列
                    if (hr >= 0 && inOut > 0)
                    {
                        int n = Math.Min(inOut, nBuf);
                        for (int i = 0; i < n; i++)
                        {
                            IntPtr p = IntPtr.Add(buf, i * objSize);
                            int ofs = Marshal.ReadInt32(p, 0);
                            int data = Marshal.ReadInt32(p, 4);
                            sb.Append(L.T("dev.evt.item", ofs, data));
                        }
                        sb.Append(L.T("dev.evt.count", n));
                    }
                    else if (hr >= 0)
                    {
                        sb.Append(L.T("dev.evt.none"));
                    }
                    else sb.Append(L.T("dev.evt.hr", Hr(hr)));
                }
                finally { Marshal.FreeHGlobal(buf); }
            }
            catch (Exception ex) { sb.Append(L.T("dev.evt.ex", ex.Message)); }
            return sb.ToString();
        }

        /// <summary>读取一次原始状态缓冲区，供诊断。</summary>
        private (int Hr, IntPtr Buf) ReadRawState()
        {
            if (_device == IntPtr.Zero) return (-1, IntPtr.Zero);
            IntPtr buf = Marshal.AllocHGlobal(_stateSize);
            try
            {
                var gds = Vtbl<GetDeviceStateFn>(_device, 9);
                Vtbl<PollFn>(_device, 25)(_device);
                int hr = gds(_device, _stateSize, buf);
                return (hr, buf);
            }
            catch (Exception)
            {
                Marshal.FreeHGlobal(buf);
                return (-1, IntPtr.Zero);
            }
        }

        public bool Acquire()
        {
            if (_device == IntPtr.Zero) return false;
            try
            {
                LastAcquireHr = Vtbl<AcquireFn>(_device, 7)(_device);
                _acquired = LastAcquireHr >= 0;
                return _acquired;
            }
            catch { _acquired = false; return false; }
        }

        public bool Poll()
        {
            if (_device == IntPtr.Zero) return false;
            try
            {
                int hr = Vtbl<PollFn>(_device, 25)(_device);
                if (hr < 0) { Acquire(); return false; }
                return true;
            }
            catch { return Acquire(); }
        }

        /// <summary>
        /// 读取当前按钮按下状态（0=未按）。
        /// 采用 DirectInput 规范流程：Poll 刷新 → GetDeviceState 读取；
        /// 遇到“状态丢失/未获取”时自动重新获取并重试一次。
        /// </summary>
        public bool[] GetButtons(int maxButtons)
        {
            var res = new bool[maxButtons > 0 ? maxButtons : 128];
            if (_device == IntPtr.Zero) return res;
            try
            {
                IntPtr ptr = Marshal.AllocHGlobal(_stateSize);
                try
                {
                    int hr = ReadDeviceState(ptr);
                    if (hr >= 0)
                    {
                        ReadError = "";
                        int n = Math.Min(res.Length, 128);
                        for (int i = 0; i < n; i++)
                        {
                            int ofs = DIJOFS_BUTTON0 + i;   // 标准 DIJOYSTATE2 固定偏移
                            if (ofs + 1 > _stateSize) { res[i] = false; continue; }
                            // DirectInput 按钮字节约定：位 0x80=按下（参考项目同款判断）
                            res[i] = (Marshal.ReadByte(ptr, ofs) & 0x80) != 0;
                        }
                    }
                    else
                    {
                        ReadError = ReadStateError(hr);
                    }
                }
                finally { Marshal.FreeHGlobal(ptr); }
            }
            catch (Exception)
            {
                Acquire();
            }
            return res;
        }

        private int ReadDeviceState(IntPtr ptr)
        {
            var gds = Vtbl<GetDeviceStateFn>(_device, 9);

            // 与参考项目 MvRens/G940LEDControl 一致：直接 GetDeviceState，不做 Poll
            // （对 HID 免轮询设备更稳；参考实现的按钮捕获即此流程）
            int hr = gds(_device, _stateSize, ptr);
            LastStateHr = hr;
            if (hr < 0 && (hr == DIERR_INPUTLOST || hr == DIERR_NOTACQUIRED))
            {
                // 状态丢失/未获取 → 重新获取后重试一次
                _acquired = false;
                if (Acquire())
                {
                    hr = gds(_device, _stateSize, ptr);
                    LastStateHr = hr;
                }
            }
            return hr;
        }

        private static string ReadStateError(int hr)
        {
            if (hr == DIERR_OTHERAPPHASPRIO) return L.T("dev.st.prio");
            if (hr == DIERR_INPUTLOST) return L.T("dev.st.lost");
            if (hr == DIERR_NOTACQUIRED) return L.T("dev.st.notacq");
            return L.T("dev.st.fail", Hr(hr));
        }

        /// <summary>把一帧颜色写入 G940 面板（一次 Feature 报告写 8 灯红/绿掩码）。返回是否真的写成功。</summary>
        public bool ApplyFrame(LedFrame frame)
        {
            if (_hid == null || frame == null) return false;
            try
            {
                frame.ToMasks(out byte red, out byte green);
                if (_hid.SetLeds(red, green))
                {
                    LedChannelDirty = false;
                    return true;
                }
            }
            catch (Exception)
            {
                // LED 写入失败不影响主流程
            }
            return false;
        }

        /// <summary>
        /// 灯通道「这一帧必须重发」。设备变更通知到达时置起，写成功一帧后自动清掉。
        /// 效果引擎靠它知道：画面虽然没变，但设备那头已经复位过了，得再写一遍。
        /// </summary>
        public bool LedChannelDirty { get; private set; }

        /// <summary>
        /// 标记灯通道已失效：丢掉句柄与路径，下一帧重新枚举。
        /// 拔掉设备时画面可能静止、根本没在写，等不到写失败，所以必须由设备变更通知来叫醒。
        /// </summary>
        public void MarkLedChannelStale()
        {
            LedChannelDirty = true;
            if (_hid != null)
            {
                try { _hid.MarkStale(); } catch (Exception) { }
            }
        }

        /// <summary>断开并恢复全绿。用户主动断开、程序退出用它。</summary>
        public void Disconnect()
        {
            Disconnect(true);
        }

        /// <summary>
        /// 只释放输入通道（DirectInput），**保留灯通道句柄**。
        ///
        /// 设备被拔时用这个方法，不要用 Disconnect：Disconnect 会把 G940Hid 一起关掉，
        /// 灯就再也不会在插回时自己恢复。灯和输入本来就是各自打开、各自收尾的两条通道
        /// （见 G940Hid 类注释第 2 条），拔线只该断输入这一条。
        /// </summary>
        public void ReleaseInput()
        {
            if (_device != IntPtr.Zero)
            {
                try { Vtbl<UnacquireFn>(_device, 8)(_device); } catch (Exception) { }
                ReleaseCom(_device);
                _device = IntPtr.Zero;
            }
            if (_dinput != IntPtr.Zero)
            {
                ReleaseCom(_dinput);
                _dinput = IntPtr.Zero;
            }
            Info = null;
            _acquired = false;
            ReadError = "";
        }

        /// <summary>
        /// 断开设备：灯与输入两条通道各收各的尾。
        /// </summary>
        /// <param name="restoreLeds">
        /// 是否把灯恢复成**全绿**再关句柄。用户主动断开、程序退出时传 true
        /// （设备出厂观感是全绿，留一块全灭的面板会让人以为设备坏了）；
        /// Connect 开头那次收拾旧连接传 false —— 马上要重开并立刻重写灯，
        /// 先写一遍全绿只会在面板上闪一下。
        /// </param>
        public void Disconnect(bool restoreLeds)
        {
            if (_hid != null)
            {
                if (restoreLeds)
                {
                    try { _hid.RestoreDefault(); } catch (Exception) { }
                }
                try { _hid.Dispose(); } catch (Exception) { }
                _hid = null;
            }

            ReleaseInput();
        }

        public void Dispose() => Disconnect();
    }
}
