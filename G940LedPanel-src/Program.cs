using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using G940LedPanel.Core;
using G940LedPanel.UI;

namespace G940LedPanel
{
    internal static class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// “把已经开着的窗口叫到前面来”这条消息的编号。
        /// 用 RegisterWindowMessage 注册，保证不会和系统消息撞号。
        /// </summary>
        internal static readonly int ActivateMessage =
            unchecked((int)RegisterWindowMessage("G940LedPanel.Activate.v1"));

        /// <summary>给所有顶层窗口广播一次；正在跑的那个实例收到后会自己 Show + Activate。</summary>
        private static void WakeRunningInstance()
        {
            try { PostMessage(new IntPtr(0xFFFF), (uint)ActivateMessage, IntPtr.Zero, IntPtr.Zero); }
            catch (Exception) { }
        }

        private static long _lastShownTicks;

        /// <summary>
        /// 拼装异常详情（类型 + 消息 + 堆栈前若干帧），便于定位。
        ///
        /// 这里的文字走 L.T：启动早期 MainForm 还没跑、语言还没从 config.json 读出来，
        /// 那会儿 <see cref="L.Lang"/> 是默认的中文，弹中文可接受——崩溃时能看懂
        /// 比语言对不对重要。L.T 自身全 try/catch、缺 key 返回「!key」，不会在这里再抛一次。
        /// </summary>
        private static string ExceptionText(Exception ex)
        {
            if (ex == null) return L.T("prog.ex.unknown");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(L.T("prog.ex.type") + " " + ex.GetType().FullName);
            sb.AppendLine(L.T("prog.ex.message") + " " + ex.Message);
            if (ex.InnerException != null)
            {
                sb.AppendLine(L.T("prog.ex.inner") + " " + ex.InnerException.GetType().Name
                    + ": " + ex.InnerException.Message);
            }
            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                sb.AppendLine(L.T("prog.ex.stack"));
                var lines = ex.StackTrace.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                int n = Math.Min(lines.Length, 12);
                for (int i = 0; i < n; i++) sb.AppendLine("  " + lines[i]);
            }
            return sb.ToString();
        }

        /// <summary>同一异常 3 秒内只弹一次，避免弹窗风暴阻塞界面。</summary>
        private static bool ThrottleDialog()
        {
            long now = DateTime.UtcNow.Ticks;
            if (now - _lastShownTicks < TimeSpan.FromSeconds(3).Ticks) return false;
            _lastShownTicks = now;
            return true;
        }

        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 全局异常兜底：UI 线程任何未处理异常不再让程序崩溃，而是提示后继续
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) =>
            {
                try
                {
                    if (!ThrottleDialog()) return;
                    MessageBox.Show(L.T("prog.ex.recovered") + "\r\n" + ExceptionText(e.Exception)
                        + "\r\n\r\n" + L.T("prog.ex.recovered.tail"),
                        L.T("prog.ex.title.notice"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch (Exception)
                {
                    // 兜底失败也绝不崩溃
                }
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try
                {
                    if (!ThrottleDialog()) return;
                    MessageBox.Show(L.T("prog.ex.fatal") + "\r\n"
                        + ExceptionText(e.ExceptionObject as Exception),
                        L.T("prog.ex.title.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception)
                {
                }
            };

            // 单实例：主窗口点 × 是"收到托盘"，进程仍在跑。这时再双击 exe，
            // 以前会起第二个进程，两份内存配置互相覆盖 —— 用户看到的就是"我加的行不见了"。
            // 这里先抢互斥体：抢不到说明已经有一个在跑，把它的窗口叫到前面，自己立刻退出。
            System.Threading.Mutex mutex;
            if (!TryAcquireSingleInstance(out mutex))
            {
                WakeRunningInstance();
                return;
            }

            try
            {
                Application.Run(new MainForm());
            }
            finally
            {
                try { mutex.ReleaseMutex(); } catch (Exception) { }
                mutex.Dispose();
            }
        }

        /// <summary>
        /// 抢"只允许一个实例"的互斥体。抢到返回 true（<paramref name="mutex"/> 需要一直持有到进程结束）。
        /// 上一个进程是被强杀的，互斥体会处于 abandoned 状态——那也算抢到。
        /// </summary>
        private static bool TryAcquireSingleInstance(out System.Threading.Mutex mutex)
        {
            mutex = null;
            try
            {
                bool created;
                mutex = new System.Threading.Mutex(true, @"Local\G940LedPanel.SingleInstance", out created);
                return created;
            }
            catch (System.Threading.AbandonedMutexException)
            {
                return true;   // 上一个进程没正常退出，互斥体现在归我们
            }
            catch (Exception)
            {
                return true;   // 互斥体本身出问题时，宁可允许启动也不要卡死用户
            }
        }
    }
}