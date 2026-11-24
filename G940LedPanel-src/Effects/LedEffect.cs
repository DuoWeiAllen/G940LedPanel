using System;
using G940LedPanel.Core;

namespace G940LedPanel.Effects
{
    /// <summary>
    /// LED 效果基类。所有效果都按 25fps（1 帧 = 40ms）的整数帧计算相位，
    /// 不用毫秒浮点——时序带上一格就是一帧，实机节奏才能和仿真页逐格对上。
    /// </summary>
    public abstract class LedEffect
    {
        public string Id = "";
        public string Name = "";
        public LedColor Color = LedColor.Green;
        public double Speed = 1.0;
        public int TargetLed = -1; // -1=全部

        protected byte C => (byte)Color;

        /// <summary>当前帧号（1 帧 = 40ms）。Speed 为倍速：2.0 = 快一倍。</summary>
        protected int Frame(double t) => (int)Math.Floor(t * 25.0 * Math.Max(Speed, 0.05) + 1e-6);

        protected static int Mod(int v, int m)
        {
            int r = v % m;
            return r < 0 ? r + m : r;
        }

        /// <summary>把某一列（上下两颗）一起点亮。</summary>
        protected static void SetCol(LedFrame f, int col, byte color)
        {
            var c = LedLayout.Cols[col];
            f.Set(c[0], color);
            f.Set(c[1], color);
        }

        /// <summary>
        /// 是否一次点亮多颗灯（涟漪这类）。
        /// 合成器据此决定"只把目标那颗写回去"还是"整段接受"——涟漪锚定在一颗灯上，
        /// 但波会沿途点亮别的灯，只写回一颗就等于把波切掉了。
        /// </summary>
        public virtual bool SpansMultipleLeds { get { return TargetLed < 0; } }

        public abstract void Render(LedFrame frame, double t);
    }

    // ==================== 单灯节奏（只影响 TargetLed） ====================

    /// <summary>关灯。</summary>
    public sealed class OffEffect : LedEffect
    {
        public override void Render(LedFrame f, double t) => f.SetTarget(TargetLed, (byte)LedColor.Off);
    }

    /// <summary>常亮。</summary>
    public sealed class SolidEffect : LedEffect
    {
        public override void Render(LedFrame f, double t) => f.SetTarget(TargetLed, C);
    }

    /// <summary>
    /// 帧表节奏效果：按 (段长帧数, 是否点亮) 的序列循环。
    /// 慢闪 / 双闪 / 一长一短 / 快闪 / 心跳 全部由这一套参数生成。
    /// </summary>
    public sealed class RhythmEffect : LedEffect
    {
        private readonly int[][] _segs;
        private readonly int _total;

        public RhythmEffect(params int[][] segs)
        {
            _segs = segs;
            foreach (var s in _segs) _total += s[0];
        }

        public override void Render(LedFrame f, double t)
        {
            int p = Mod(Frame(t), _total);
            bool on = false;
            foreach (var s in _segs)
            {
                if (p < s[0]) { on = s[1] != 0; break; }
                p -= s[0];
            }
            f.SetTarget(TargetLed, on ? C : (byte)LedColor.Off);
        }
    }

    // ==================== 整帧空间效果（沿环 / 按列，忽略 TargetLed） ====================

    /// <summary>
    /// 环行 / 双灯追 / 三灯追：光点沿 2×4 的扁环绕行，Tail 控制尾巴长度，
    /// Gap 控制「跑完一圈后停几格」，为 0 就是首尾相接不停顿。
    /// </summary>
    public sealed class RingEffect : LedEffect
    {
        public bool Reverse;
        public int Tail = 1;
        public int Step = 3;   // 每格帧数（3 帧 = 120ms）
        public int Gap = 0;    // 一圈之后的空档格数

        public override void Render(LedFrame f, double t)
        {
            f.Clear();
            int total = (LedLayout.Count + Gap) * Step;
            int p = Mod(Frame(t), total);
            if (p >= LedLayout.Count * Step) return;      // 空档：全灭
            int i = p / Step;
            var ring = Reverse ? LedLayout.RingRev : LedLayout.Ring;
            for (int k = 0; k < Tail; k++) f.Set(ring[(i + k) % LedLayout.Count], C);
        }
    }

    /// <summary>双子·对称转：环上相隔半圈的两点同向绕行，视觉永远对称。</summary>
    public sealed class TwinEffect : LedEffect
    {
        public int Step = 3;

        public override void Render(LedFrame f, double t)
        {
            f.Clear();
            int i = Mod(Frame(t), LedLayout.Count * Step) / Step;
            f.Set(LedLayout.Ring[i], C);
            f.Set(LedLayout.Ring[(i + 4) % LedLayout.Count], C);
        }
    }

    /// <summary>两点对扫：环上相隔半圈的两点相向而行，相遇后原路退回。</summary>
    public sealed class SweepEffect : LedEffect
    {
        private static readonly int[] Steps = { 0, 1, 2, 3, 2, 1 };
        public int Step = 3;

        public override void Render(LedFrame f, double t)
        {
            f.Clear();
            int k = Steps[Mod(Frame(t), Steps.Length * Step) / Step];
            f.Set(LedLayout.Ring[k], C);
            f.Set(LedLayout.Ring[(k + 4) % LedLayout.Count], C);
        }
    }

    /// <summary>
    /// 按列展开/收拢：段表由「段长帧数 + 模式」组成。
    /// 模式：0=全灭 1=中间两列 2=外侧两列 3=全亮。
    /// </summary>
    public sealed class SpreadEffect : LedEffect
    {
        private readonly int[][] _segs;   // {帧数, 模式}
        private readonly int _total;

        public SpreadEffect(params int[][] segs)
        {
            _segs = segs;
            foreach (var s in _segs) _total += s[0];
        }

        public override void Render(LedFrame f, double t)
        {
            f.Clear();
            int p = Mod(Frame(t), _total);
            int mode = 0;
            foreach (var s in _segs)
            {
                if (p < s[0]) { mode = s[1]; break; }
                p -= s[0];
            }
            switch (mode)
            {
                case 1:
                    foreach (int c in LedLayout.MidCols) SetCol(f, c, C);
                    break;
                case 2:
                    foreach (int c in LedLayout.OutCols) SetCol(f, c, C);
                    break;
                case 3:
                    f.SetAll(C);
                    break;
            }
        }
    }

    // ==================== 装饰集（自带配色，不读 Color） ====================

    /// <summary>颜色循环：8 套配色轮换，每 10 帧换一次。</summary>
    public sealed class ColorCycleEffect : LedEffect
    {
        private static readonly byte[][] Schemes =
        {
            new byte[] { 3,3,3,3,3,3,3,3 },
            new byte[] { 1,1,1,1,1,1,1,1 },
            new byte[] { 2,2,2,2,2,2,2,2 },
            new byte[] { 3,1,3,1,3,1,3,1 },
            new byte[] { 1,2,1,2,1,2,1,2 },
            new byte[] { 3,2,3,2,3,2,3,2 },
            new byte[] { 3,3,1,1,2,2,3,1 },
            new byte[] { 2,2,3,3,2,2,3,3 }
        };

        public override void Render(LedFrame f, double t)
        {
            int k = Mod(Frame(t), 80) / 10;
            var s = Schemes[k];
            for (int i = 0; i < LedLayout.Count; i++) f.Set(i, s[i]);
        }
    }

    /// <summary>随机彩灯：确定性伪随机（同一帧号永远同一图案，便于对照 demo）。</summary>
    public sealed class RandomEffect : LedEffect
    {
        private static double Rnd(int n)
        {
            double x = Math.Sin(n * 127.1 + 13.7) * 43758.5453;
            return x - Math.Floor(x);
        }

        public override void Render(LedFrame f, double t)
        {
            int seed = Mod(Frame(t), 120) / 3;
            f.Clear();
            for (int i = 0; i < LedLayout.Count; i++)
            {
                double r1 = Rnd(seed * 17 + i * 3);
                double r2 = Rnd(seed * 29 + i * 7);
                if (r1 <= 0.45) continue;
                f.Set(i, (byte)(Rnd(seed + i) > 0.62 ? 2 : (r2 > 0.5 ? 3 : 1)));
            }
        }
    }

    /// <summary>二进制计数：P1 为最低位，每 6 帧 +1。</summary>
    public sealed class BinaryCounterEffect : LedEffect
    {
        public override void Render(LedFrame f, double t)
        {
            int n = (Mod(Frame(t), 1536) / 6) % 256;
            for (int i = 0; i < LedLayout.Count; i++)
                f.Set(i, ((n >> i) & 1) != 0 ? C : (byte)LedColor.Off);
        }
    }

    /// <summary>绕环填充：沿环逐格填满 8 格，再逐格清空。</summary>
    public sealed class FillEffect : LedEffect
    {
        public override void Render(LedFrame f, double t)
        {
            f.Clear();
            int p = Mod(Frame(t), 48);
            int lv = p < 24 ? p / 3 : 8 - (p - 24) / 3;
            for (int k = 0; k < lv && k < LedLayout.Count; k++) f.Set(LedLayout.Ring[k], C);
        }
    }

    /// <summary>奇偶跳（棋盘）：沿环的奇偶两组交替，中间不灭。</summary>
    public sealed class AlternateEffect : LedEffect
    {
        public override void Render(LedFrame f, double t)
        {
            f.Clear();
            int g = Mod(Frame(t), 16) / 8;
            for (int k = 0; k < LedLayout.Count; k++)
                if (k % 2 == g) f.Set(LedLayout.Ring[k], C);
        }
    }

    /// <summary>
    /// 按下反馈：被按那颗跑它自己配置的效果，同时从它出发向两侧扩散一圈琥珀涟漪（320ms）。
    ///
    /// 为什么合成一个效果而不是加两个 overlay：一个按键只有一个 Overlay 槽位，
    /// 两个 overlay 会让"按住/松开"的增删逻辑翻倍，还容易漏删。
    /// </summary>
    public sealed class PressEffect : LedEffect
    {
        private const int RippleFrames = 8;      // 8 帧 = 320ms

        private readonly LedEffect _inner;
        private readonly LedFrame _tmp = new LedFrame();
        private readonly int _anchor;

        public PressEffect(LedEffect inner, int anchor)
        {
            _inner = inner;
            _anchor = anchor;
            TargetLed = anchor;
            Id = (inner != null && !string.IsNullOrEmpty(inner.Id) ? inner.Id : "off") + "+ripple";
            Name = "按下涟漪";
            Color = LedColor.Amber;
        }

        public override bool SpansMultipleLeds { get { return true; } }

        public override void Render(LedFrame f, double t)
        {
            int i = Array.IndexOf(LedLayout.Ring, _anchor);
            if (i < 0) i = 0;

            // 1) 涟漪：从锚点沿扁环向两侧扩散，第 0 帧不起波（否则按下瞬间锚点颜色就被琥珀盖掉），
            //    第 8 帧（320ms）波走出画面，自然消失。
            int k = Frame(t);
            if (k >= 1 && k < RippleFrames)
            {
                f.Set(LedLayout.Ring[Mod(i + k, LedLayout.Count)], (byte)LedColor.Amber);
                f.Set(LedLayout.Ring[Mod(i - k, LedLayout.Count)], (byte)LedColor.Amber);
            }

            // 2) 锚点最后写：它自己配置的效果（常亮/慢闪/双闪…），波怎么走都不影响它
            if (_inner != null)
            {
                Array.Clear(_tmp.C, 0, 8);
                _inner.Render(_tmp, t);
                f.Set(_anchor, _tmp.C[_anchor]);
            }
            else
            {
                f.Set(_anchor, (byte)LedColor.Amber);
            }
        }
    }

    // ==================== 工厂 ====================

    /// <summary>按 id 创建效果实例；同时把旧版 id 映射到新效果，旧配置不会变全黑。</summary>
    public static class EffectFactory
    {
        /// <summary>语义集（前 14 个，给告警规则用）</summary>
        public static readonly string[] Ids =
        {
            "off", "solid", "blink", "double", "longshort", "flash", "heartbeat",
            "ring_fwd", "ring_rev", "chase2", "chase3", "expand", "collapse", "sweep",
            "twin", "cycle", "random", "binary", "fill", "alternate"
        };

        // 1.3.53：这里原来有一份 Names 静态数组（"关灯" / "常亮" …）。
        // 删掉的原因不是"挪个地方"，而是它**一旦缓存就永远是初始语言**：
        // 静态 readonly 数组在类型初始化时求值一次，切语言后不会跟着变。
        // 显示名现在由 UI/TextNames.Effect(id) 现取，本层只认 id、不认名字。

        /// <summary>旧版 id → 新 id。旧配置里的呼吸/脉冲/频闪/波浪等在这里落地。</summary>
        private static readonly string[,] Legacy =
        {
            { "breath",   "double" },     // 硬件无亮度，呼吸做不出来，退成双闪
            { "pulse",    "blink" },
            { "strobe",   "flash" },
            { "wave",     "ring_fwd" },
            { "knight",   "sweep" },
            { "chase",    "chase3" },
            { "progress", "fill" }
        };

        public static string Normalize(string id)
        {
            if (string.IsNullOrEmpty(id)) return "off";
            for (int i = 0; i < Ids.Length; i++)
                if (Ids[i] == id) return id;
            for (int i = 0; i < Legacy.GetLength(0); i++)
                if (Legacy[i, 0] == id) return Legacy[i, 1];
            return "off";
        }

        /// <summary>
        /// 由显示名反查 id：表格里读到的是某个语言下的效果名，这里按当前语言的名表比对。
        /// 找不到返回 "off"——宁可退回关灯，也不要把非法 id 塞进效果链（那会全程不亮）。
        /// 注意：调用方传进来的必须是**当前语言**的名字。切语言时表格会整表重刷，
        /// 所以这条前提永远成立，不需要额外的旧值映射表。
        /// </summary>
        public static string IdOfName(string name, string[] names)
        {
            if (names != null && name != null)
                for (int i = 0; i < names.Length && i < Ids.Length; i++)
                    if (names[i] == name) return Ids[i];
            return "off";
        }

        public static LedEffect Create(string id, LedColor color, double speed, int target)
        {
            LedEffect e;
            switch (Normalize(id))
            {
                case "solid": e = new SolidEffect(); break;

                // 慢闪 8/8 = 640ms
                case "blink": e = new RhythmEffect(new[] { 8, 1 }, new[] { 8, 0 }); break;
                // 双闪 3/3/3/15 = 960ms（尾停 600ms）
                case "double": e = new RhythmEffect(new[] { 3, 1 }, new[] { 3, 0 }, new[] { 3, 1 }, new[] { 15, 0 }); break;
                // 一长一短 10/3/3/14 = 1200ms（长 400ms，短 120ms，长:短 = 3.33:1）
                case "longshort": e = new RhythmEffect(new[] { 10, 1 }, new[] { 3, 0 }, new[] { 3, 1 }, new[] { 14, 0 }); break;
                // 快闪 2/3 = 200ms
                case "flash": e = new RhythmEffect(new[] { 2, 1 }, new[] { 3, 0 }); break;
                // 心跳 3/3/3/36 = 1800ms
                case "heartbeat": e = new RhythmEffect(new[] { 3, 1 }, new[] { 3, 0 }, new[] { 3, 1 }, new[] { 36, 0 }); break;

                // 环行：8 格 + 2 格空档 = 30 帧 / 1200ms 一圈
                case "ring_fwd": e = new RingEffect { Step = 3, Gap = 2 }; break;
                case "ring_rev": e = new RingEffect { Step = 3, Gap = 2, Reverse = true }; break;
                // 双灯追 / 三灯追：无空档，8 格 = 24 帧 / 960ms 一圈
                case "chase2": e = new RingEffect { Step = 3, Tail = 2 }; break;
                case "chase3": e = new RingEffect { Step = 3, Tail = 3 }; break;
                // 中心扩散：中间列 → 外侧列 → 全亮 → 灭（24 帧 / 960ms）
                case "expand":
                    e = new SpreadEffect(new[] { 3, 1 }, new[] { 3, 2 }, new[] { 6, 3 }, new[] { 6, 0 }, new[] { 6, 0 });
                    break;
                // 边缘收拢：外侧列 → 中间列 → 灭（24 帧 / 960ms）
                case "collapse":
                    e = new SpreadEffect(new[] { 3, 2 }, new[] { 3, 1 }, new[] { 6, 0 }, new[] { 6, 0 }, new[] { 6, 0 });
                    break;
                case "sweep": e = new SweepEffect(); break;

                // 装饰集
                case "twin": e = new TwinEffect(); break;
                case "cycle": e = new ColorCycleEffect(); break;
                case "random": e = new RandomEffect(); break;
                case "binary": e = new BinaryCounterEffect(); break;
                case "fill": e = new FillEffect(); break;
                case "alternate": e = new AlternateEffect(); break;

                default: e = new OffEffect(); break;
            }
            e.Id = Normalize(id);
            e.Name = e.Id;   // 1.3.53：Name 只是调试标记，显示名走 UI/TextNames，别在这里塞语言
            e.Color = color;
            e.Speed = speed;
            e.TargetLed = target;
            return e;
        }
    }
}
