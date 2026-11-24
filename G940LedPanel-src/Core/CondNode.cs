using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace G940LedPanel.Core
{
    /// <summary>三态结果：未知表示“变量没有数据”，不等于假。</summary>
    public enum Tri
    {
        False = 0,
        True = 1,
        Unknown = 2
    }

    /// <summary>
    /// 结构化条件节点。这是整套告警判定的基础数据结构。
    ///
    /// 为什么不用字符串表达式：用户要自己改条件，而手打 "ENG ON FIRE:1 > 0" 一旦打错字母，
    /// 程序不报错、灯就是不亮，要飞起来才发现。这里变量和比较符全部来自下拉选项，
    /// 打不出错，而且不需要写解析器。
    ///
    /// 节点类型：
    ///   Op = "and" / "or"  → Items 是子条件（至少 1 条）
    ///   Op = "not"         → Items 恰好 1 条
    ///   Op = "cmp"         → 叶子：Var(左值) Cmp 右值；右值取 Var2 / Value 之一
    /// </summary>
    public sealed class CondNode
    {
        /// <summary>
        /// 条件树最大深度（根算第 1 层）。
        /// 定 4 而不是 3：兜底规则“无告警”本身是 非(或(若干 且(...) ))，
        /// 天然就占 4 层。编辑器里用户新加分组时只允许加到这一层，不能更深。
        /// </summary>
        public const int MaxDepth = 4;

        [JsonPropertyName("op")]
        public string Op { get; set; }

        [JsonPropertyName("items")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<CondNode> Items { get; set; }

        /// <summary>左值：SimVars 里的变量名，或 "#" 开头的派生量。</summary>
        [JsonPropertyName("var")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Var { get; set; }

        /// <summary>右值取另一个量（变量或派生量）时使用；为空则用 Value。</summary>
        [JsonPropertyName("var2")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Var2 { get; set; }

        [JsonPropertyName("cmp")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Cmp { get; set; }

        // 值为 0 时省略：反序列化缺省即 0，语义不变，配置文件里也不会到处是 "value": 0
        [JsonPropertyName("value")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public double Value { get; set; }

        [JsonPropertyName("value2")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public double Value2 { get; set; }

        /// <summary>
        /// 旧配置里的阈值名（如 "IasGearMax"）。1.3.44 起阈值机制已移除，
        /// 本字段只为识别旧配置并给出明确提示，新条件一律写 Value 或 Var2。
        /// </summary>
        [JsonPropertyName("thresholdKey")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string ThresholdKey { get; set; }

        [JsonIgnore]
        public bool IsGroup { get { return Op == "and" || Op == "or" || Op == "not"; } }

        [JsonIgnore]
        public bool IsLeaf { get { return Op == "cmp"; } }

        // ---------------------------------------------------------------- 校验

        /// <summary>结构校验。返回空列表表示通过；否则每项是一条人话说明。</summary>
        public List<string> Validate()
        {
            var errs = new List<string>();
            ValidateAt(this, 1, errs);
            return errs;
        }

        private static void ValidateAt(CondNode n, int depth, List<string> errs)
        {
            if (n == null) { errs.Add(L.T("cond.err.empty")); return; }
            if (depth > MaxDepth) { errs.Add(L.T("cond.err.depth", MaxDepth)); return; }

            switch (n.Op)
            {
                case "and":
                case "or":
                    if (n.Items == null || n.Items.Count == 0) { errs.Add(L.T("cond.err.emptygroup")); return; }
                    foreach (var c in n.Items) ValidateAt(c, depth + 1, errs);
                    return;

                case "not":
                    if (n.Items == null || n.Items.Count != 1) { errs.Add(L.T("cond.err.notcount")); return; }
                    ValidateAt(n.Items[0], depth + 1, errs);
                    return;

                case "cmp":
                    ValidateLeaf(n, errs);
                    return;

                case null:
                case "":
                    errs.Add(L.T("cond.err.opmissing"));
                    return;

                default:
                    errs.Add(L.T("cond.err.opunknown", n.Op));
                    return;
            }
        }

        private static void ValidateLeaf(CondNode n, List<string> errs)
        {
            if (string.IsNullOrEmpty(n.Var))
            {
                errs.Add(L.T("cond.err.varmissing"));
            }
            else if (!IsKnownVar(n.Var))
            {
                errs.Add(L.T("cond.err.varunknown", n.Var));
            }

            if (string.IsNullOrEmpty(n.Cmp) || Array.IndexOf(CmpOps, n.Cmp) < 0)
            {
                errs.Add(L.T("cond.err.cmpunknown", n.Cmp ?? L.T("cond.err.cmpempty")));
            }
            else if (IsKnownVar(n.Var) && IsBoolVar(n.Var) != IsBoolCmp(n.Cmp))
            {
                // 比较符和变量类型对不上。1.3.61 之前不报这条：空速这种 knots 量
                // 也能选「为真」，选了不报错、条件只是永远不成立 —— 属最难查的一类错。
                errs.Add(L.T(IsBoolVar(n.Var) ? "cond.err.needboolcmp" : "cond.err.neednumcmp",
                             n.Var, CmpText(n.Cmp)));
            }

            // 1.3.44 移除阈值机制：旧配置里带 thresholdKey 的条件一律判无效，明确提示后丢弃。
            // 不能默认成 0 继续算——那样规则会永远不成立，比丢弃更难查。
            if (!string.IsNullOrEmpty(n.ThresholdKey))
            {
                errs.Add(L.T("cond.err.threshold", n.ThresholdKey));
            }

            if (!string.IsNullOrEmpty(n.Var2) && !IsKnownVar(n.Var2))
            {
                errs.Add(L.T("cond.err.varunknown", n.Var2));
            }
        }

        /// <summary>变量名是否可用：SimVars 的 115 个之一，或已登记的派生量。</summary>
        public static bool IsKnownVar(string name)
        {
            return !string.IsNullOrEmpty(name)
                   && (Derived.IsDerived(name) || SimVars.IndexOf(name) >= 0);
        }

        /// <summary>
        /// 这个量是不是开关量（只有真/假两种）。判据是 SimVars 里的单位串 "bool"，
        /// 全表 65 条这样标。派生量全是数值，取不到单位串自然判成数值量。
        /// </summary>
        public static bool IsBoolVar(string name)
        {
            return string.Equals(SimVars.UnitOf(name), "bool", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 某个变量此刻能用的比较符。开关量给「为真/为假」，数值量给大小比较。
        /// 界面下拉按这个给候选，`Validate` 也按这个判——两处同一个判据，不会各说各话。
        /// </summary>
        public static string[] CmpOpsFor(string name)
        {
            return IsBoolVar(name) ? BoolCmpOps : NumCmpOps;
        }

        /// <summary>开关量可用的比较符。</summary>
        public static readonly string[] BoolCmpOps = { "bool", "nbool" };

        /// <summary>数值量可用的比较符。</summary>
        public static readonly string[] NumCmpOps = { ">", ">=", "<", "<=", "==", "!=", "between" };

        // ---------------------------------------------------------------- 遍历

        /// <summary>收集本条件用到的全部变量（含派生量）。</summary>
        public void CollectVars(ICollection<string> into)
        {
            if (into == null) return;
            if (IsLeaf)
            {
                if (!string.IsNullOrEmpty(Var)) into.Add(Var);
                if (!string.IsNullOrEmpty(Var2)) into.Add(Var2);
                return;
            }
            if (Items == null) return;
            foreach (var c in Items) if (c != null) c.CollectVars(into);
        }

        public List<string> CollectVars()
        {
            var set = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var raw = new List<string>();
            CollectVars(raw);
            foreach (var v in raw) if (seen.Add(v)) set.Add(v);
            return set;
        }

        /// <summary>深度优先找第一个不满足条件的叶子，用于校验提示。</summary>
        public IEnumerable<CondNode> Leaves()
        {
            if (IsLeaf) { yield return this; yield break; }
            if (Items == null) yield break;
            foreach (var c in Items)
            {
                if (c == null) continue;
                foreach (var l in c.Leaves()) yield return l;
            }
        }

        // ---------------------------------------------------------------- 中文摘要

        public static readonly string[] CmpOps =
        {
            "bool", "nbool", ">", ">=", "<", "<=", "==", "!=", "between"
        };

        public static string CmpText(string cmp)
        {
            switch (cmp)
            {
                case "bool": return L.T("cmp.bool");
                case "nbool": return L.T("cmp.nbool");
                case ">": return L.T("cmp.gt");
                case ">=": return L.T("cmp.ge");
                case "<": return L.T("cmp.lt");
                case "<=": return L.T("cmp.le");
                case "==": return L.T("cmp.eq");
                case "!=": return L.T("cmp.ne");
                case "between": return L.T("cmp.between");
                default: return cmp ?? "?";
            }
        }

        /// <summary>判断该比较符是否不需要数值（布尔类）。</summary>
        public static bool IsBoolCmp(string cmp)
        {
            return cmp == "bool" || cmp == "nbool";
        }

        /// <summary>生成中文摘要，用于表格“触发条件”列与编辑器标题。</summary>
        public string Describe()
        {
            return DescribeAt(this, false);
        }

        private static string DescribeAt(CondNode n, bool needParen)
        {
            if (n == null) return L.T("cond.describe.empty");

            if (n.Op == "and" || n.Op == "or")
            {
                var sb = new StringBuilder();
                string joiner = n.Op == "and" ? L.T("cond.describe.and") : L.T("cond.describe.or");
                if (n.Items == null || n.Items.Count == 0) return L.T("cond.describe.emptygroup");
                for (int i = 0; i < n.Items.Count; i++)
                {
                    if (i > 0) sb.Append(joiner);
                    sb.Append(DescribeAt(n.Items[i], true));
                }
                string s = sb.ToString();
                return needParen ? "(" + s + ")" : s;
            }

            if (n.Op == "not")
            {
                string inner = (n.Items != null && n.Items.Count == 1) ? DescribeAt(n.Items[0], true) : L.T("cond.describe.empty");
                return L.T("cond.describe.not") + inner;
            }

            if (n.Op == "cmp")
            {
                string left = Derived.LabelOf(n.Var);
                if (IsBoolCmp(n.Cmp))
                    return left + " " + CmpText(n.Cmp);
                string right;
                if (!string.IsNullOrEmpty(n.Var2)) right = Derived.LabelOf(n.Var2);
                else if (n.Cmp == "between") right = Num(n.Value) + "~" + Num(n.Value2);
                else right = Num(n.Value);
                return left + " " + CmpText(n.Cmp) + " " + right;
            }

            return L.T("cond.describe.unknown");
        }

        private static string Num(double v)
        {
            if (Math.Abs(v - Math.Round(v)) < 1e-9) return ((long)Math.Round(v)).ToString();
            return v.ToString("0.###");
        }

        public CondNode Clone()
        {
            var c = new CondNode
            {
                Op = Op,
                Var = Var,
                Var2 = Var2,
                Cmp = Cmp,
                Value = Value,
                Value2 = Value2,
                ThresholdKey = ThresholdKey
            };
            if (Items != null)
            {
                c.Items = new List<CondNode>();
                foreach (var i in Items) c.Items.Add(i == null ? null : i.Clone());
            }
            return c;
        }
    }

    /// <summary>条件节点的构造助手，让 55 条规则的声明读起来接近自然语言。</summary>
    public static class Cond
    {
        public static CondNode And(params CondNode[] items)
        {
            return Group("and", items);
        }

        public static CondNode Or(params CondNode[] items)
        {
            return Group("or", items);
        }

        public static CondNode Not(CondNode item)
        {
            var n = new CondNode { Op = "not", Items = new List<CondNode>() };
            if (item != null) n.Items.Add(item);
            return n;
        }

        private static CondNode Group(string op, CondNode[] items)
        {
            var n = new CondNode { Op = op, Items = new List<CondNode>() };
            if (items != null) foreach (var i in items) if (i != null) n.Items.Add(i);
            return n;
        }

        /// <summary>布尔变量为真。</summary>
        public static CondNode B(string var)
        {
            return new CondNode { Op = "cmp", Var = var, Cmp = "bool" };
        }

        /// <summary>布尔变量为假。</summary>
        public static CondNode NB(string var)
        {
            return new CondNode { Op = "cmp", Var = var, Cmp = "nbool" };
        }

        /// <summary>数值比较（与常量）。</summary>
        public static CondNode N(string var, string cmp, double value)
        {
            return new CondNode { Op = "cmp", Var = var, Cmp = cmp, Value = value };
        }

        /// <summary>区间比较（含两端）。</summary>
        public static CondNode Between(string var, double lo, double hi)
        {
            return new CondNode { Op = "cmp", Var = var, Cmp = "between", Value = lo, Value2 = hi };
        }

        /// <summary>左值与另一个变量（或派生量）比较。</summary>
        public static CondNode V(string var, string cmp, string var2)
        {
            return new CondNode { Op = "cmp", Var = var, Cmp = cmp, Var2 = var2 };
        }
    }
}
