using System;
using System.Collections.Generic;
using G940LedPanel.Core;

namespace G940LedPanel.Effects
{
    /// <summary>
    /// 四层渲染的层号。数字就是绘制顺序，越大越靠上。
    /// 渲染时按 2 → 3 → 4 逐层叠加（L1 背景由 <see cref="LedCompositor.Background"/> 单独先画），
    /// 不靠 List 里的添加工顺序——加 overlay 的早晚不该影响画面。
    /// </summary>
    public static class LedLayer
    {
        /// <summary>L1 背景层：仅按键模式，由预设携带，整帧。</summary>
        public const int Background = 1;

        /// <summary>L2 按下反馈层：按住的那颗灯 + 涟漪，单灯写入。</summary>
        public const int Press = 2;

        /// <summary>L3 内容层：按键绑定命中 / 告警规则命中，单灯写入。</summary>
        public const int Content = 3;

        /// <summary>L4 系统动画层：自检 / 模式提示 / 离线警示 / 搜索中，整帧接管。</summary>
        public const int Anim = 4;
    }

    /// <summary>L4 系统动画。None 表示没有动画，画面回落到下层。</summary>
    public enum SysAnim
    {
        None = 0,
        SelfTest = 1,
        ModeKey = 2,
        ModeAlarm = 3,
        Offline = 4,
        Searching = 5,
        /// <summary>数据源连着、但不在飞行（菜单/加载中）：绿色反向三灯追，表示“工作正常，等着”。</summary>
        Standby = 6
    }

    /// <summary>一个叠加效果实例。</summary>
    public class Overlay
    {
        public LedEffect Effect;

        /// <summary>层号，见 <see cref="LedLayer"/>。默认内容层。</summary>
        public int Layer = LedLayer.Content;

        public bool IsActive = true;
    }

    /// <summary>
    /// 帧合成器：把背景 + 各层 overlay + 系统动画合成为一帧。
    /// 刻意不依赖 WinForms / 设备，纯逻辑，可在离线测试里逐帧断言。
    /// </summary>
    public class LedCompositor
    {
        private readonly List<Overlay> _overlays = new List<Overlay>();
        private readonly LedFrame _tmp = new LedFrame();

        /// <summary>L1 背景（仅按键模式用）。</summary>
        public LedEffect Background { get; set; }

        /// <summary>
        /// 告警页模式。为 true 时 L3 内容层只允许单灯效果：
        /// 收到整帧（TargetLed&lt;0）的 overlay 直接丢弃并记日志，防止一行"环行·全部"
        /// 把另外 7 颗灯清黑、盖掉正在报的告警。
        /// </summary>
        public bool AlarmMode { get; set; }

        /// <summary>当前系统动画。None = 无。</summary>
        public SysAnim Anim { get; private set; }

        /// <summary>当前动画是否循环。</summary>
        public bool AnimLoops { get; private set; }

        /// <summary>当前动画的起始时刻（全局秒），动画内部用它换算局部帧号。</summary>
        public double AnimStart { get; private set; }

        /// <summary>
        /// 本次一次性动画要播几圈。默认 1。
        /// 离线/搜索这类“提示性”动画播 3 圈就停，既看得见又不至于一直转。
        /// 循环动画（<see cref="SetSysAnimLoop"/>）不看这个值。
        /// </summary>
        public int AnimCycles { get; private set; }

        /// <summary>最近一次被丢弃的 overlay 说明；null 表示没丢过。供状态栏与测试查看。</summary>
        public string LastReject { get; private set; }

        /// <summary>丢弃日志回调（可空）。</summary>
        public Action<string> Log;

        public IReadOnlyList<Overlay> Overlays
        {
            get { return _overlays; }
        }

        // ==================== overlay 管理 ====================

        public Overlay Add(LedEffect effect, int layer)
        {
            var o = new Overlay { Effect = effect, Layer = layer };
            _overlays.Add(o);
            return o;
        }

        public Overlay Add(LedEffect effect)
        {
            return Add(effect, LedLayer.Content);
        }

        public void Remove(Overlay o)
        {
            if (o != null) o.IsActive = false;
        }

        public void ClearOverlays()
        {
            _overlays.Clear();
        }

        /// <summary>清掉本帧已失效的 overlay，避免列表无限增长。</summary>
        private void PurgeInactive()
        {
            _overlays.RemoveAll(o => !o.IsActive || o.Effect == null);
        }

        // ==================== 系统动画 ====================

        /// <summary>播一次动画，播完自动回落到下层。</summary>
        public void PlaySysAnim(SysAnim anim, double t)
        {
            PlaySysAnim(anim, t, 1);
        }

        /// <summary>播一次动画，可以指定圈数（圈数 × 单圈时长 = 总时长）。</summary>
        public void PlaySysAnim(SysAnim anim, double t, int cycles)
        {
            if (anim == SysAnim.None) { StopSysAnim(); return; }
            Anim = anim;
            AnimLoops = false;
            AnimStart = t;
            AnimCycles = cycles < 1 ? 1 : cycles;
        }

        /// <summary>开始循环动画。同一种动画已经在循环时不重置相位，可以每帧安全调用。</summary>
        public void SetSysAnimLoop(SysAnim anim, double t)
        {
            if (anim == SysAnim.None) { StopSysAnim(); return; }
            if (Anim == anim && AnimLoops) return;
            Anim = anim;
            AnimLoops = true;
            AnimStart = t;
            AnimCycles = 1;
        }

        public void StopSysAnim()
        {
            Anim = SysAnim.None;
            AnimLoops = false;
            AnimStart = 0;
            AnimCycles = 1;
        }

        /// <summary>动画时长（秒）。0 表示无动画。</summary>
        public static double AnimDuration(SysAnim anim)
        {
            switch (anim)
            {
                case SysAnim.SelfTest: return 24 / 25.0;    // 24 帧 960ms
                case SysAnim.ModeKey: return 30 / 25.0;     // 30 帧 1200ms
                case SysAnim.ModeAlarm: return 30 / 25.0;   // 30 帧 1200ms
                case SysAnim.Offline: return 50 / 25.0;     // 50 帧 2000ms
                case SysAnim.Searching: return 30 / 25.0;   // 30 帧 1200ms
                case SysAnim.Standby: return 24 / 25.0;    // 24 帧 960ms（一圈；循环播放，此值只用于首帧定长）
                default: return 0;
            }
        }

        // 动画实例只读不改，做成静态单例，免得每帧 new。
        private static readonly LedEffect FxSelfTest =
            new SpreadEffect(new[] { 3, 1 }, new[] { 3, 2 }, new[] { 6, 3 }, new[] { 3, 2 }, new[] { 3, 1 }, new[] { 6, 0 });

        private static readonly LedEffect FxModeKey =
            new RingEffect { Step = 3, Gap = 2 };

        private static readonly LedEffect FxModeAlarm =
            new RingEffect { Step = 3, Gap = 2, Reverse = true };

        private static readonly LedEffect FxOffline =
            new RingEffect { Step = 5, Gap = 2, Reverse = true, Color = LedColor.Red };

        /// <summary>
        /// 等待中：绿色反向三灯追（三颗灯连着反向绕环）。
        /// 与“离线”的红色反向环行形成一对语义：红色 = 没连上，绿色 = 连上了、在等。
        /// 用 Tail=3 而不是 Tail=1，肉眼一眼能分清是“等待”还是“告警”。
        ///
        /// <para>Gap=0（首尾相接），其余环行效果是 Gap=2：那个 2 格空档是给“一圈一圈顶”的
        /// 警示圈做收尾的，一圈灭 240ms 有节奏、有压迫感。等待要长时间挂在背景上，
        /// 同一个空档就变成“跑一段、黑一下”——实机看到的“不连贯、像两段”就是它。
        /// 警示圈保持原样不动，只把等待改成连续流动。</para>
        /// </summary>
        private static readonly LedEffect FxStandby =
            new RingEffect { Step = 3, Gap = 0, Reverse = true, Tail = 3, Color = LedColor.Green };

        private static readonly LedEffect FxSearching =
            new SweepEffect { Step = 5 };

        /// <summary>取动画对应的效果实例（已配好节奏和颜色）。</summary>
        public static LedEffect AnimEffect(SysAnim anim)
        {
            switch (anim)
            {
                case SysAnim.SelfTest: return FxSelfTest;
                case SysAnim.ModeKey: return FxModeKey;
                case SysAnim.ModeAlarm: return FxModeAlarm;
                case SysAnim.Offline: return FxOffline;
                case SysAnim.Searching: return FxSearching;
                case SysAnim.Standby: return FxStandby;
                default: return null;
            }
        }

        // ==================== 合成 ====================

        /// <summary>
        /// 合成一帧。<paramref name="t"/> 是引擎启动以来的全局秒数——
        /// 背景与各层都用它算相位，所以叠加、动画播完回落都不会打乱下层的节奏。
        /// </summary>
        public void Compose(LedFrame frame, double t)
        {
            if (frame == null) return;

            // 一次性动画到点自动回落。动画内部用局部时间（t - AnimStart），
            // 下层仍用全局 t，因此相位天然连续，不需要任何"恢复"动作。
            if (Anim != SysAnim.None && !AnimLoops
                && t - AnimStart >= AnimDuration(Anim) * Math.Max(1, AnimCycles))
                StopSysAnim();

            PurgeInactive();

            frame.Clear();
            if (Background != null) Background.Render(frame, t);

            for (int layer = LedLayer.Press; layer <= LedLayer.Anim; layer++)
            {
                for (int i = 0; i < _overlays.Count; i++)
                {
                    var o = _overlays[i];
                    if (o.Layer != layer || !o.IsActive || o.Effect == null) continue;
                    RenderOverlay(frame, o, t);
                }
            }

            if (Anim != SysAnim.None)
            {
                var fx = AnimEffect(Anim);
                // 局部时间：动画永远从第 0 帧起播，跟引擎时钟起点无关。
                if (fx != null) fx.Render(frame, t - AnimStart);
            }
        }

        private void RenderOverlay(LedFrame frame, Overlay o, double t)
        {
            int target = o.Effect.TargetLed;

            if (target >= 0 && !o.Effect.SpansMultipleLeds)
            {
                // 单灯 overlay：只把自己那颗灯写回去，另外 7 颗一个字节都不碰。
                Array.Copy(frame.C, _tmp.C, 8);
                o.Effect.Render(_tmp, t);
                frame.C[target] = _tmp.C[target];
                return;
            }

            // 整帧 overlay 会清屏。告警页只允许单灯效果，这里挡住。
            if (AlarmMode && o.Layer == LedLayer.Content)
            {
                LastReject = "告警页拒绝整帧效果：" + (o.Effect.Id ?? "?") + "（内容层只允许单灯）";
                if (Log != null) Log(LastReject);
                return;
            }

            o.Effect.Render(frame, t);
        }
    }
}
