using System;
using System.Collections.Generic;

namespace G940LedPanel.Core
{
    /// <summary>
    /// 派生量：由多个原始变量算出来的中间值，规则条件可以直接引用（名字以 # 开头）。
    ///
    /// 为什么要有它：像“三腿最大差 &gt; 20”“全部发动机都在燃烧”这类条件，
    /// 用单个变量的比较表达不出来。如果为它们开特例（在 C# 里写委托），
    /// 用户就改不了这些条件，而且三态保护要另外再写一遍。
    /// 做成派生量之后，规则依旧是纯粹的 CondNode，能被编辑、能被试算、能被三态统一处理。
    /// </summary>
    public static class Derived
    {
        public const string GearTotal = "#GearTotal";
        public const string GearSpread = "#GearSpread";
        public const string MaxThrottle = "#MaxThrottle";
        public const string EngCount = "#EngCount";
        public const string EngBurning = "#EngBurning";
        public const string FuelRatio = "#FuelRatio";

        private static readonly string[] Names =
        {
            GearTotal, GearSpread, MaxThrottle, EngCount, EngBurning, FuelRatio
        };

        /// <summary>
        /// 派生量的显示名，存的是语言键（1.3.59）。
        /// 与 SimVars 的 115 条变量标签不同：那些量自带英文名（AMBIENT DEW POINT），
        /// 中文标签只是给中文用户看的注释，不翻也不影响使用；派生量没有英文名，
        /// 只有 #GearTotal 这种代号，所以必须翻，否则英文界面下拉里挂着中文。
        /// </summary>
        private static readonly string[] Labels =
        {
            "derived.gearTotal", "derived.gearSpread", "derived.maxThrottle",
            "derived.engCount", "derived.engBurning", "derived.fuelRatio"
        };

        public static bool IsDerived(string name)
        {
            if (string.IsNullOrEmpty(name) || name[0] != '#') return false;
            for (int i = 0; i < Names.Length; i++)
                if (string.Equals(Names[i], name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public static string LabelOf(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            for (int i = 0; i < Names.Length; i++)
                if (string.Equals(Names[i], name, StringComparison.OrdinalIgnoreCase)) return L.T(Labels[i]);
            if (name[0] == '#') return name;
            return SimVars.LabelOf(name);
        }

        /// <summary>是否受限制的派生量（编辑器里不列出，但条件里可能已存在）。</summary>
        public static string[] All { get { return Names; } }

        /// <summary>
        /// 这个派生量依赖哪些原始变量。手动试算要知道"想算出它得先填哪几个量"，
        /// 不然用户在试算面板里看到 #GearSpread，却不知道该填什么。
        /// </summary>
        public static string[] RawVarsOf(string name)
        {
            if (Eq(name, GearTotal))
                return new[] { "GEAR TOTAL PCT EXTENDED", "GEAR LEFT POSITION", "GEAR RIGHT POSITION", "GEAR CENTER POSITION" };
            if (Eq(name, GearSpread))
                return new[] { "GEAR LEFT POSITION", "GEAR RIGHT POSITION", "GEAR CENTER POSITION", "GEAR TAIL POSITION" };
            if (Eq(name, MaxThrottle))
                return new[] { "GENERAL ENG THROTTLE LEVER POSITION:1", "GENERAL ENG THROTTLE LEVER POSITION:2",
                               "GENERAL ENG THROTTLE LEVER POSITION:3", "GENERAL ENG THROTTLE LEVER POSITION:4" };
            if (Eq(name, EngCount))
                return new[] { "NUMBER OF ENGINES" };
            if (Eq(name, EngBurning))
                return new[] { "NUMBER OF ENGINES", "GENERAL ENG COMBUSTION:1", "GENERAL ENG COMBUSTION:2",
                               "GENERAL ENG COMBUSTION:3", "GENERAL ENG COMBUSTION:4" };
            if (Eq(name, FuelRatio))
                return new[] { "FUEL TOTAL QUANTITY", "FUEL TOTAL CAPACITY" };
            return new string[0];
        }

        /// <summary>取派生量。返回 false 表示“数据不足”，调用方按未知处理。</summary>
        public static bool TryGet(string name, FlightState fs, out double value)
        {
            value = 0;
            if (fs == null || string.IsNullOrEmpty(name)) return false;

            if (Eq(name, GearTotal)) return TryGearTotal(fs, out value);
            if (Eq(name, GearSpread)) return TryGearSpread(fs, out value);
            if (Eq(name, MaxThrottle)) return TryMaxThrottle(fs, out value);
            if (Eq(name, EngCount)) return TryEngCount(fs, out value);
            if (Eq(name, EngBurning)) return TryEngBurning(fs, out value);
            if (Eq(name, FuelRatio)) return TryFuelRatio(fs, out value);
            return false;
        }

        private static bool Eq(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 该机型是否为固定起落架。依据是 IS GEAR RETRACTABLE 明确应答为 0；
        /// 变量未应答时返回 false —— 不能把“没有数据”当成“收不起来”。
        ///
        /// 为什么需要单独判一次：固定起落架机型（如 C172）的 GEAR TOTAL PCT EXTENDED
        /// 只返回 1 左右，既不满足“已放下”（&gt;95），也不会真的收起；同时该机型不存在的
        /// 起落架腿（172 的尾轮）恒为 0。两者叠加会让“三腿不一致”一直成立、
        /// “放下锁定”永不成立，派生量必须按机型归位。
        /// </summary>
        public static bool IsFixedGear(FlightState fs)
        {
            return fs.Has("IS GEAR RETRACTABLE") && fs.N("IS GEAR RETRACTABLE") < 0.5;
        }

        /// <summary>
        /// 起落架总伸出。优先用 GEAR TOTAL PCT EXTENDED（0–100）；
        /// 机型不返回该变量时，降级为“各腿伸出百分比的最小值”——
        /// 取最小值而不是平均值，因为只要有一条腿没收，就是没收好。
        /// </summary>
        private static bool TryGearTotal(FlightState fs, out double value)
        {
            // 固定起落架永远处于放下状态。必须放在读 GEAR TOTAL PCT EXTENDED 之前：
            // 该变量在固定起落架机型上返回的是 1 左右，直接采信会把整机判成“没放下”。
            if (IsFixedGear(fs)) { value = 100; return true; }

            value = 0;
            if (fs.Has("GEAR TOTAL PCT EXTENDED"))
            {
                value = fs.N("GEAR TOTAL PCT EXTENDED");
                return true;
            }

            var legs = GearLegs(fs);
            if (legs.Count < 2) return false;    // 只有一条腿的数据不足以判断整机

            double min = legs[0];
            for (int i = 1; i < legs.Count; i++) if (legs[i] < min) min = legs[i];
            value = min;
            return true;
        }

        /// <summary>
        /// 三腿（或含尾轮）伸出的最大差值。用于“三腿不一致”。
        /// 只比较真实存在的起落架腿：三轮机的尾轮、尾轮机的鼻轮在该机型上不存在，
        /// SimConnect 仍会返回一个恒为 0 的“假腿”。当整机已完全放下（total&amp;gt;95）而某条腿仍≈0，
        /// 必是假腿，排除后不计入不一致比较；其余状态（收放中、真实卡阻）一律照常比较，
        /// 确保真实故障仍能被发现。固定起落架机型由 IsFixedGear 先判掉——那条路径下
        /// GEAR TOTAL PCT EXTENDED 只有 1 左右，靠 total 判假腿永远不成立。
        /// </summary>
        private static bool TryGearSpread(FlightState fs, out double value)
        {
            value = 0;

            // 固定起落架的腿不可收放，不存在“收放不一致”，差值恒为 0。
            if (IsFixedGear(fs)) return true;

            var legs = GearLegs(fs);
            if (legs.Count < 2) return false;

            // 整机伸出量：有 GEAR TOTAL PCT EXTENDED 就用它（0–100），没有则无法判定假腿，保留原逻辑。
            double? total = fs.Has("GEAR TOTAL PCT EXTENDED")
                ? (double?)fs.N("GEAR TOTAL PCT EXTENDED") : null;

            var real = new List<double>(legs.Count);
            for (int i = 0; i < legs.Count; i++)
            {
                // 整机已完全放下（total>95）却仍读≈0 的腿，是该机型不存在的假腿，排除。
                if (total.HasValue && total.Value > 95 && legs[i] < 5) continue;
                real.Add(legs[i]);
            }
            if (real.Count < 2) return false;

            double min = real[0], max = real[0];
            for (int i = 1; i < real.Count; i++)
            {
                if (real[i] < min) min = real[i];
                if (real[i] > max) max = real[i];
            }
            value = max - min;
            return true;
        }

        /// <summary>各腿伸出百分比归一到 0–100（原始变量是 percent over 100）。</summary>
        private static List<double> GearLegs(FlightState fs)
        {
            var list = new List<double>(4);
            AddLeg(fs, "GEAR LEFT POSITION", list);
            AddLeg(fs, "GEAR RIGHT POSITION", list);
            AddLeg(fs, "GEAR CENTER POSITION", list);
            AddLeg(fs, "GEAR TAIL POSITION", list);
            return list;
        }

        private static void AddLeg(FlightState fs, string name, List<double> into)
        {
            if (!fs.Has(name)) return;
            double v = fs.N(name) * 100.0;      // percent over 100 → 0..100
            if (v < 0) v = 0;
            if (v > 100) v = 100;
            into.Add(v);
        }

        /// <summary>四台油门里的最大值（部分机型油门可到 110）。</summary>
        private static bool TryMaxThrottle(FlightState fs, out double value)
        {
            value = 0;
            bool any = false;
            for (int i = 1; i <= 4; i++)
            {
                string n = "GENERAL ENG THROTTLE LEVER POSITION:" + i;
                if (!fs.Has(n)) continue;
                double v = fs.N(n);
                if (!any || v > value) value = v;
                any = true;
            }
            return any;
        }

        /// <summary>在用发动机台数，钳到 1..4。</summary>
        private static bool TryEngCount(FlightState fs, out double value)
        {
            value = 0;
            if (!fs.Has("NUMBER OF ENGINES")) return false;

            int n = (int)Math.Round(fs.N("NUMBER OF ENGINES"));
            if (n < 1) return false;
            if (n > 4) n = 4;
            value = n;
            return true;
        }

        /// <summary>燃烧中的发动机台数。任一在用发动机的燃烧状态缺失，整条返回未知。</summary>
        private static bool TryEngBurning(FlightState fs, out double value)
        {
            value = 0;
            double raw;
            if (!TryEngCount(fs, out raw)) return false;
            int n = (int)raw;

            int burning = 0;
            for (int i = 1; i <= n; i++)
            {
                string name = "GENERAL ENG COMBUSTION:" + i;
                if (!fs.Has(name)) return false;
                if (fs.N(name) > 0.5) burning++;
            }
            value = burning;
            return true;
        }

        /// <summary>油量比 = 现有油量 / 油箱总容量。容量为 0 视为不可用。</summary>
        private static bool TryFuelRatio(FlightState fs, out double value)
        {
            value = 0;
            if (!fs.Has("FUEL TOTAL QUANTITY") || !fs.Has("FUEL TOTAL CAPACITY")) return false;

            double cap = fs.N("FUEL TOTAL CAPACITY");
            if (cap <= 0) return false;

            double ratio = fs.N("FUEL TOTAL QUANTITY") / cap;
            if (ratio < 0) ratio = 0;
            value = ratio;
            return true;
        }
    }

    /// <summary>
    /// 条件求值器。返回三态，而不是 bool。
    ///
    /// 这是整套设计最关键的一处：变量取不到时不能当成 false，否则 "非某变量" 会变成 true，
    /// 那条"未除冰"的规则就恒亮——v3.0 里真的发生过。三态让"未知"在取反后仍是未知，
    /// 最终一律按不触发处理，规则里再也不用写 fs.Has() 来防。
    /// </summary>
    public static class CondEval
    {
        public static Tri Test(CondNode n, FlightState fs)
        {
            if (n == null) return Tri.Unknown;

            switch (n.Op)
            {
                case "and":
                    return EvalAnd(n, fs);

                case "or":
                    return EvalOr(n, fs);

                case "not":
                    {
                        if (n.Items == null || n.Items.Count != 1) return Tri.Unknown;
                        Tri inner = Test(n.Items[0], fs);
                        if (inner == Tri.True) return Tri.False;
                        if (inner == Tri.False) return Tri.True;
                        return Tri.Unknown;
                    }

                case "cmp":
                    return EvalLeaf(n, fs);

                default:
                    return Tri.Unknown;
            }
        }

        private static Tri EvalAnd(CondNode n, FlightState fs)
        {
            if (n.Items == null || n.Items.Count == 0) return Tri.Unknown;

            bool unknown = false;
            foreach (var c in n.Items)
            {
                Tri t = Test(c, fs);
                if (t == Tri.False) return Tri.False;      // 短路：已经不可能成立
                if (t == Tri.Unknown) unknown = true;
            }
            return unknown ? Tri.Unknown : Tri.True;
        }

        private static Tri EvalOr(CondNode n, FlightState fs)
        {
            if (n.Items == null || n.Items.Count == 0) return Tri.Unknown;

            bool unknown = false;
            foreach (var c in n.Items)
            {
                Tri t = Test(c, fs);
                if (t == Tri.True) return Tri.True;        // 短路：已经成立
                if (t == Tri.Unknown) unknown = true;
            }
            return unknown ? Tri.Unknown : Tri.False;
        }

        private static Tri EvalLeaf(CondNode n, FlightState fs)
        {
            double left;
            if (!TryValue(n.Var, fs, out left)) return Tri.Unknown;

            // 布尔比较不需要右值
            if (CondNode.IsBoolCmp(n.Cmp))
            {
                bool b = left > 0.5;
                if (n.Cmp == "nbool") b = !b;
                return b ? Tri.True : Tri.False;
            }

            double right;
            if (!string.IsNullOrEmpty(n.Var2))
            {
                if (!TryValue(n.Var2, fs, out right)) return Tri.Unknown;
            }
            else if (!string.IsNullOrEmpty(n.ThresholdKey))
            {
                // 阈值机制已移除；旧条件按“未知”处理，Validate 会先行把它判无效并丢弃。
                return Tri.Unknown;
            }
            else
            {
                right = n.Value;
            }

            return Compare(left, n.Cmp, right, n.Value2) ? Tri.True : Tri.False;
        }

        private static bool Compare(double a, string cmp, double b, double b2)
        {
            switch (cmp)
            {
                case ">": return a > b;
                case ">=": return a >= b;
                case "<": return a < b;
                case "<=": return a <= b;
                case "==": return Math.Abs(a - b) < 1e-9;
                case "!=": return Math.Abs(a - b) >= 1e-9;
                case "between": return a >= b && a <= b2;
                default: return false;
            }
        }

        /// <summary>取一个量（原始变量或派生量）。返回 false 表示无数据。</summary>
        public static bool TryValue(string name, FlightState fs, out double value)
        {
            value = 0;
            if (fs == null || string.IsNullOrEmpty(name)) return false;

            if (Derived.IsDerived(name)) return Derived.TryGet(name, fs, out value);

            if (!fs.Has(name)) return false;
            value = fs.N(name);
            return true;
        }

        /// <summary>
        /// 试算：把整条条件拆成“逐叶子”的明细，供编辑器底部的试算面板显示。
        /// 每项：左值中文名、左值文本、比较文本、右值文本、三态结果。
        /// </summary>
        public sealed class LeafProbe
        {
            public CondNode Leaf;
            public string LeftLabel;
            public string LeftText;
            public string CmpText;
            public string RightText;
            public Tri Result;
            public bool LeftKnown;
        }

        public static List<LeafProbe> Probe(CondNode root, FlightState fs)
        {
            var list = new List<LeafProbe>();
            if (root == null) return list;

            foreach (var leaf in root.Leaves())
            {
                var p = new LeafProbe();
                p.Leaf = leaf;
                p.LeftLabel = Derived.LabelOf(leaf.Var);
                p.CmpText = CondNode.CmpText(leaf.Cmp);

                double v;
                p.LeftKnown = TryValue(leaf.Var, fs, out v);
                p.LeftText = p.LeftKnown ? Fmt(v) : L.T("common.nodata");

                if (!string.IsNullOrEmpty(leaf.Var2))
                {
                    double v2;
                    bool ok2 = TryValue(leaf.Var2, fs, out v2);
                    p.RightText = ok2 ? Fmt(v2) : L.T("common.nodata");
                }
                else if (!string.IsNullOrEmpty(leaf.ThresholdKey))
                {
                    p.RightText = L.T("cond.right.threshold", leaf.ThresholdKey);
                }
                else if (leaf.Cmp == "between")
                {
                    p.RightText = Fmt(leaf.Value) + " ~ " + Fmt(leaf.Value2);
                }
                else
                {
                    p.RightText = CondNode.IsBoolCmp(leaf.Cmp) ? "—" : Fmt(leaf.Value);
                }

                p.Result = Test(leaf, fs);
                list.Add(p);
            }
            return list;
        }

        public static string Fmt(double v)
        {
            if (Math.Abs(v - Math.Round(v)) < 1e-9) return ((long)Math.Round(v)).ToString();
            return v.ToString("0.###");
        }

        public static string TriText(Tri t)
        {
            switch (t)
            {
                case Tri.True: return L.T("tri.true");
                case Tri.False: return L.T("tri.false");
                // 三态里的「未判定」与「无数据」不是一回事：前者是结论，
                // 后者是左值没有数。混用同一个词，试算表里 Value 和 Result 两列
                // 会写成一样的字，用户分不清哪边缺数据。
                default: return L.T("tri.none");
            }
        }
    }
}
