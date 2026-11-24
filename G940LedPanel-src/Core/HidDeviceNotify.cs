using System;
using System.Runtime.InteropServices;

namespace G940LedPanel.Core
{
    /// <summary>
    /// 监听 HID 设备接口的到达与移除（RegisterDeviceNotification）。
    ///
    /// 为什么要注册而不是收 DBT_DEVNODES_CHANGED（0x0007）：那条是「设备树有变化」的粗广播，
    /// 插个 U 盘、读卡器都会触发，而且不说明是哪个设备，只能收到之后自己再猜。
    /// 注册成 HID 接口之后，消息里带着设备接口路径，一眼就能判断是不是 G940 的灯接口。
    ///
    /// 本类只管「谁是目标设备」这一件事，不碰 G940Hid、不碰界面——收到通知后做什么由调用方决定。
    /// </summary>
    public sealed class HidDeviceNotify : IDisposable
    {
        public const int WM_DEVICECHANGE = 0x0219;

        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        private const int DBT_DEVTYP_DEVICEINTERFACE = 5;
        private const int DEVICE_NOTIFY_WINDOW_HANDLE = 0;

        // DEV_BROADCAST_DEVICEINTERFACE 在 x86/x64 下布局相同：
        //   dbcc_size(4) dbcc_devicetype(4) dbcc_reserved(4) dbcc_classguid(16) dbcc_name[]
        // 所以设备名恒定在 offset 28 开始。
        private const int BroadcastHeadSize = 32;
        private const int BroadcastNameOffset = 28;

        private static readonly Guid HidInterfaceGuid =
            new Guid("4D1E55B2-F16F-11CF-88CB-001111000030");

        private IntPtr _notify = IntPtr.Zero;

        /// <summary>目标设备接入。</summary>
        public event Action Arrived;

        /// <summary>目标设备移除。</summary>
        public event Action Removed;

        public bool IsRegistered { get { return _notify != IntPtr.Zero; } }

        public string LastError { get; private set; } = "";

        public bool Register(IntPtr hwnd)
        {
            if (_notify != IntPtr.Zero) return true;
            if (hwnd == IntPtr.Zero) { LastError = "窗口句柄为空，无法注册设备通知。"; return false; }

            IntPtr buf = Marshal.AllocHGlobal(BroadcastHeadSize);
            try
            {
                for (int i = 0; i < BroadcastHeadSize; i++) Marshal.WriteByte(buf, i, 0);
                Marshal.WriteInt32(buf, 0, BroadcastHeadSize);          // dbcc_size
                Marshal.WriteInt32(buf, 4, DBT_DEVTYP_DEVICEINTERFACE); // dbcc_devicetype
                Marshal.StructureToPtr(HidInterfaceGuid, new IntPtr(buf.ToInt64() + 12), false);

                _notify = RegisterDeviceNotification(hwnd, buf, DEVICE_NOTIFY_WINDOW_HANDLE);
                if (_notify == IntPtr.Zero)
                {
                    LastError = "RegisterDeviceNotification 失败：Win32 错误 " + Marshal.GetLastWin32Error();
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                LastError = "注册设备通知异常：" + ex.Message;
                return false;
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        /// <summary>
        /// 在窗体 WndProc 里调用。返回 true 表示这条消息是本类关心的目标设备，
        /// 调用方不应再往下传。
        /// </summary>
        public bool HandleMessage(int msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg != WM_DEVICECHANGE || lParam == IntPtr.Zero) return false;

            int evt = wParam.ToInt32();
            if (evt != DBT_DEVICEARRIVAL && evt != DBT_DEVICEREMOVECOMPLETE) return false;

            try
            {
                if (Marshal.ReadInt32(lParam, 4) != DBT_DEVTYP_DEVICEINTERFACE) return false;
                string name = Marshal.PtrToStringUni(new IntPtr(lParam.ToInt64() + BroadcastNameOffset));
                if (!IsTargetPath(name)) return false;

                Action h = evt == DBT_DEVICEARRIVAL ? Arrived : Removed;
                if (h != null) h();
                return true;
            }
            catch (Exception)
            {
                // 消息里带的结构不完整（系统在设备移除途中发来的），当作与本程序无关
                return false;
            }
        }

        /// <summary>是否 G940 的灯接口（VID 046D / PID C287）的接口路径。</summary>
        public static bool IsTargetPath(string devicePath)
        {
            if (string.IsNullOrEmpty(devicePath)) return false;
            return devicePath.IndexOf("vid_046d&pid_c287", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public void Dispose()
        {
            if (_notify != IntPtr.Zero)
            {
                try { UnregisterDeviceNotification(_notify); } catch (Exception) { }
                _notify = IntPtr.Zero;
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr notificationFilter, int flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterDeviceNotification(IntPtr handle);
    }
}
