using System;
using System.Diagnostics;
using System.Windows.Forms;
using G940LedPanel.Core;

namespace G940LedPanel.Effects
{
    /// <summary>
    /// LED 渲染引擎：一个 25ms 定时器 + 设备写入，画面由 <see cref="LedCompositor"/> 合成。
    /// 四层（背景 / 按下反馈 / 内容 / 系统动画）的规则全部在合成器里，这里只管节拍和输出。
    /// </summary>
    public class EffectEngine
    {
        private readonly DeviceSession _session;
        private readonly Timer _timer;
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly LedFrame _frame = new LedFrame();
        private readonly LedFrame _sent = new LedFrame();   // 上一次真正写给设备的帧
        private bool _sentValid;
        private readonly LedCompositor _compositor = new LedCompositor();

        /// <summary>合成器。层序、告警拒绝、动画状态机都在这里，UI 可以直接读状态。</summary>
        public LedCompositor Compositor
        {
            get { return _compositor; }
        }

        public LedEffect Background
        {
            get { return _compositor.Background; }
            set { _compositor.Background = value; }
        }

        /// <summary>是否告警页（决定是否拒绝整帧内容层）。</summary>
        public bool AlarmMode
        {
            get { return _compositor.AlarmMode; }
            set { _compositor.AlarmMode = value; }
        }

        public SysAnim Anim
        {
            get { return _compositor.Anim; }
        }

        /// <summary>当前动画是否循环（false 表示一次性动画正在播）。</summary>
        public bool AnimLoops
        {
            get { return _compositor.AnimLoops; }
        }

        /// <summary>
        /// 总开关关闭时的“静音”：引擎照常跑、照常合成，只是把每帧清成全灭。
        /// 以前这里是 Stop()，结果预览画面直接冻住——用户改了设置看不到任何反应，
        /// 分不清是"没生效"还是"引擎停了"。
        /// </summary>
        public bool Muted { get; set; }

        /// <summary>每帧渲染完成（无论是否连接设备）后触发，供预览。</summary>
        public event Action<LedFrame> FrameRendered;

        public EffectEngine(DeviceSession session)
        {
            _session = session;
            _compositor.Log = msg => Trace.WriteLine("[LED] " + msg);
            // 25ms 一跳，而不是 40ms：效果本身按“真实时间 → 帧号”算相位（1 帧 = 40ms），
            // 抖动只会让某帧重复一次，不会把 40ms 的窗口整个错过。
            // WinForms Timer 的实际粒度约 15.6ms，Interval=40 时真实间隔常在 40~56ms，
            // 会漏帧（比如快闪 2 帧亮 = 80ms 会被吃掉一帧），和仿真页对不上。
            _timer = new Timer { Interval = 25 };
            _timer.Tick += (s, e) => Tick();
        }

        public bool Running
        {
            get { return _timer.Enabled; }
        }

        /// <summary>引擎时钟（秒）。动画起点、离线计时都用它。</summary>
        public double Now
        {
            get { return _clock.Elapsed.TotalSeconds; }
        }

        public void Start()
        {
            if (_timer.Enabled) return;
            _clock.Restart();
            _sentValid = false;   // 重新开跑时不知道设备当前是什么样，第一帧无条件写一次
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
            ClearOverlays();
            _compositor.StopSysAnim();
        }

        public Overlay AddOverlay(LedEffect effect)
        {
            return _compositor.Add(effect, LedLayer.Content);
        }

        public Overlay AddOverlay(LedEffect effect, int layer)
        {
            return _compositor.Add(effect, layer);
        }

        public void RemoveOverlay(Overlay o)
        {
            _compositor.Remove(o);
        }

        public void ClearOverlays()
        {
            _compositor.ClearOverlays();
        }

        // ==================== 系统动画（P6 接线时由外部调用） ====================

        public void PlaySysAnim(SysAnim anim)
        {
            _compositor.PlaySysAnim(anim, Now);
        }

        /// <summary>播指定圈数的一次性动画（离线/搜索提示用 3 圈，播完自动熄）。</summary>
        public void PlaySysAnim(SysAnim anim, int cycles)
        {
            _compositor.PlaySysAnim(anim, Now, cycles);
        }

        public void SetSysAnimLoop(SysAnim anim)
        {
            _compositor.SetSysAnimLoop(anim, Now);
        }

        public void StopSysAnim()
        {
            _compositor.StopSysAnim();
        }

        private void Tick()
        {
            double t = _clock.Elapsed.TotalSeconds;
            _compositor.Compose(_frame, t);
            if (Muted) _frame.Clear();   // 总开关关闭：照常跑，输出全灭

            // 只有画面真的变了才写设备。静止画面（纯常亮、全灭）本来 40Hz 白写，
            // 而 G940 的 LED 是走力反馈通道写的，写得越密越容易看到闪。
            //
            // 但「画面没变」不等于「设备那头还是原样」：拔插、休眠唤醒都会让灯复位。
            // 所以设备变更通知会置 LedChannelDirty，此时哪怕画面没变也强制重发一帧，
            // 写成功后标记自动清掉——是「按需重发」，不是周期性心跳。
            if (_session != null && _session.Connected)
            {
                bool changed = !_sentValid || !SameAsSent() || _session.LedChannelDirty;
                if (changed)
                {
                    // 只在真写成功后才记账：写失败时 _sent 保持旧值，下一帧比对必然为「变了」，
                    // 于是自己重试。原来无条件记账，失败一次就再也不会发，灯会停在上一个颜色上。
                    if (_session.ApplyFrame(_frame))
                    {
                        Array.Copy(_frame.C, _sent.C, 8);
                        _sentValid = true;
                    }
                }
            }
            else
            {
                _sentValid = false;
            }

            var h = FrameRendered;
            if (h != null) h(_frame);
        }

        private bool SameAsSent()
        {
            for (int i = 0; i < 8; i++)
                if (_frame.C[i] != _sent.C[i]) return false;
            return true;
        }
    }
}
