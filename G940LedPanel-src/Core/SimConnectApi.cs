using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace G940LedPanel.Core
{
    /// <summary>
    /// 原生 SimConnect.dll 的动态封装：用 LoadLibrary + GetProcAddress 手工绑定，
    /// 因此 dll 可以放在任意目录（不要求和 exe 同目录），也不需要在编译期引用 SDK。
    /// 只调用 C API，不依赖任何托管包装 dll。
    /// </summary>
    public sealed class SimConnectApi : IDisposable
    {
        // ---- SimConnect 常量 ----
        public const uint SIMCONNECT_OBJECT_ID_USER = 0;
        public const uint SIMCONNECT_DATATYPE_FLOAT64 = 4;
        public const uint SIMCONNECT_PERIOD_ONCE = 1;
        public const uint SIMCONNECT_PERIOD_SECOND = 4;

        // SIMCONNECT_RECV_ID：NULL=0, EXCEPTION=1, OPEN=2, QUIT=3, SIMOBJECT_DATA=8
        public const int RECV_ID_EXCEPTION = 1;
        public const int RECV_ID_OPEN = 2;
        public const int RECV_ID_QUIT = 3;
        public const int RECV_ID_SIMOBJECT_DATA = 8;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void DispatchProc(IntPtr pData, uint cbData, IntPtr pContext);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int OpenFn(out IntPtr phSimConnect,
            [MarshalAs(UnmanagedType.LPStr)] string szName,
            IntPtr hWnd, uint userEvent, IntPtr hEventHandle, uint configIndex);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int CloseFn(IntPtr hSimConnect);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int CallDispatchFn(IntPtr hSimConnect, DispatchProc dispatch, IntPtr pContext);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int AddToDataDefinitionFn(IntPtr hSimConnect, uint defineId,
            [MarshalAs(UnmanagedType.LPStr)] string datumName,
            [MarshalAs(UnmanagedType.LPStr)] string unitsName,
            uint datumType, float fEpsilon, uint datumId);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int RequestDataOnSimObjectFn(IntPtr hSimConnect, uint requestId, uint defineId,
            uint objectId, uint period, uint flags, uint origin, uint interval, uint limit);

        private IntPtr _lib;
        private OpenFn _open;
        private CloseFn _close;
        private CallDispatchFn _callDispatch;
        private AddToDataDefinitionFn _addToDataDefinition;
        private RequestDataOnSimObjectFn _requestData;

        public string Path { get; private set; }
        public string Version { get; private set; }

        // ---- kernel32 ----
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibraryExW(string fileName, IntPtr hFile, uint flags);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi, BestFitMapping = false)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool SetDllDirectoryW(string path);

        private const uint LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008;

        /// <summary>加载指定路径的 SimConnect.dll，并绑定所需导出函数。失败返回 null 并给出原因。</summary>
        public static SimConnectApi Load(string path, out string error)
        {
            error = null;
            var api = new SimConnectApi();
            try
            {
                if (!File.Exists(path))
                {
                    error = L.T("api.err.nofile", path);
                    return null;
                }
                if (!SimConnectLocator.Is64Bit(path))
                {
                    error = L.T("api.err.bits32", path);
                    return null;
                }

                // 让 dll 的依赖项优先从它自己所在目录查找
                try { SetDllDirectoryW(System.IO.Path.GetDirectoryName(path)); } catch (Exception) { }

                api._lib = LoadLibraryExW(path, IntPtr.Zero, LOAD_WITH_ALTERED_SEARCH_PATH);
                try { SetDllDirectoryW(null); } catch (Exception) { } // 还原全局搜索路径，避免影响其它 dll
                if (api._lib == IntPtr.Zero)
                {
                    int err = Marshal.GetLastWin32Error();
                    error = L.T("api.err.loadlib", err)
                        + (err == 193 ? L.T("api.err.loadlib.193") : "")
                        + L.T("api.err.path", path);
                    return null;
                }

                string miss;
                if (!api.Bind(out miss))
                {
                    error = L.T("api.err.export", miss);
                    api.Dispose();
                    return null;
                }

                api.Path = path;
                api.Version = ReadVersion(path);
                return api;
            }
            catch (Exception ex)
            {
                try { api.Dispose(); } catch (Exception) { }
                error = L.T("api.err.exception", ex.GetType().Name, ex.Message);
                return null;
            }
        }

        private static string ReadVersion(string path)
        {
            try
            {
                var vi = FileVersionInfo.GetVersionInfo(path);
                string v = vi.FileVersion;
                if (string.IsNullOrEmpty(v))
                    v = (vi.FileMajorPart + "." + vi.FileMinorPart + "." + vi.FileBuildPart).Trim('.');
                return string.IsNullOrEmpty(v) ? L.T("api.err.unknown") : v;
            }
            catch (Exception) { return L.T("api.err.unknown"); }
        }

        private bool Bind(out string missing)
        {
            missing = null;
            IntPtr p;

            p = GetProcAddress(_lib, "SimConnect_Open");
            if (p == IntPtr.Zero) { missing = "SimConnect_Open"; return false; }
            _open = (OpenFn)Marshal.GetDelegateForFunctionPointer(p, typeof(OpenFn));

            p = GetProcAddress(_lib, "SimConnect_Close");
            if (p == IntPtr.Zero) { missing = "SimConnect_Close"; return false; }
            _close = (CloseFn)Marshal.GetDelegateForFunctionPointer(p, typeof(CloseFn));

            p = GetProcAddress(_lib, "SimConnect_CallDispatch");
            if (p == IntPtr.Zero) { missing = "SimConnect_CallDispatch"; return false; }
            _callDispatch = (CallDispatchFn)Marshal.GetDelegateForFunctionPointer(p, typeof(CallDispatchFn));

            p = GetProcAddress(_lib, "SimConnect_AddToDataDefinition");
            if (p == IntPtr.Zero) { missing = "SimConnect_AddToDataDefinition"; return false; }
            _addToDataDefinition = (AddToDataDefinitionFn)Marshal.GetDelegateForFunctionPointer(p, typeof(AddToDataDefinitionFn));

            p = GetProcAddress(_lib, "SimConnect_RequestDataOnSimObject");
            if (p == IntPtr.Zero) { missing = "SimConnect_RequestDataOnSimObject"; return false; }
            _requestData = (RequestDataOnSimObjectFn)Marshal.GetDelegateForFunctionPointer(p, typeof(RequestDataOnSimObjectFn));

            return true;
        }

        public int Open(out IntPtr hSimConnect, string appName, IntPtr hWnd, uint userEvent, IntPtr hEvent, uint configIndex)
        {
            return _open(out hSimConnect, appName, hWnd, userEvent, hEvent, configIndex);
        }

        public int Close(IntPtr hSimConnect)
        {
            return _close(hSimConnect);
        }

        public int CallDispatch(IntPtr hSimConnect, DispatchProc dispatch)
        {
            return _callDispatch(hSimConnect, dispatch, IntPtr.Zero);
        }

        public int AddToDataDefinition(IntPtr hSimConnect, uint defineId, string name, string unit, uint type, uint datumId)
        {
            return _addToDataDefinition(hSimConnect, defineId, name, unit, type, 0f, datumId);
        }

        public int RequestDataOnSimObject(IntPtr hSimConnect, uint requestId, uint defineId, uint period)
        {
            return _requestData(hSimConnect, requestId, defineId, SIMCONNECT_OBJECT_ID_USER, period, 0, 0, 0, 0);
        }

        public void Dispose()
        {
            if (_lib != IntPtr.Zero)
            {
                try { FreeLibrary(_lib); } catch (Exception) { }
                _lib = IntPtr.Zero;
            }
            _open = null; _close = null; _callDispatch = null;
            _addToDataDefinition = null; _requestData = null;
        }
    }
}
