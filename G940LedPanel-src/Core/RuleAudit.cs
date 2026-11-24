using System;
using System.Collections.Generic;
using System.Text;
using G940LedPanel.Effects;

namespace G940LedPanel.Core
{
    /// <summary>
    /// 规则体检：同一个系统内（= 同一颗灯上），规则之间有没有"互相压住""条件自相矛盾"这类问题。
    ///
    /// 为什么需要它：用户新增规则时可以任选系统，于是自己的规则与内置规则共用一颗灯、
    /// 共用一套优先级。而界面不显示优先级数字，也看不到条件重叠，用户没法自己回答
    /// "我这条为什么不亮"。体检就是把这件事说清楚：谁压住谁、在什么数值下、想让谁生效该怎么改。
    ///
    /// 判据不看"谁亮"——不亮是正常状态，互斥的状态划分本来就只会亮一条。
    /// 只看两件事：条件能否同时成立、同时成立时两条规则的输出是否一致。
    ///
    /// 算法是枚举代表点，不做任何结构推断：每个变量取它出现过的所有阈值、相邻阈值的中点、
    /// 两端各加一个点；两条规则的所有变量做笛卡尔积求值，统计"各自成立的点数"与"同时成立的点数"。
    /// 任何真实飞行状态都等价于其中某个点，所以结论可证、可复现——用户能拿证据点
    /// 到条件编辑器里手动试算，得到同样的结果。
    /// </summary>
    public static class RuleAudit
    {
        /// <summary>
        /// 一条结论的严重程度。数值即排序优先级（0 最先显示）。
        ///
        /// 1.3.59 从 string 改成枚举：原来存 "错"/"冲突"/"冗余"/"提示" 四个中文串，
        /// RuleAuditForm 用 `f.Level == "错"` 计数、用它当 switch 的键排颜色。
        /// 那是拿显示名当身份——切英文时这些比较全部落空，不报错，只是颜色和排序悄悄失效
        /// （1.3.56 的「一片灰」就是这个毛病，只是那次在取色函数里）。
        /// 显示文字由 UI 用 L.T 按枚举取，Core 不再生产给人看的字。
        /// </summary>
        public enum FLevel
        {
            Error = 0,
            Conflict = 1,
            Redundant = 2,
            Tip = 3,
        }

        /// <summary>严重程度的显示文字。按枚举取，不认中文——这样切语言不会影响排序与配色。</summary>
        public static string LevelText(FLevel level)
        {
            switch (level)
            {
                case FLevel.Error: return L.T("audit.level.error");
                case FLevel.Conflict: return L.T("audit.level.conflict");
                case FLevel.Redundant: return L.T("audit.level.redundant");
                default: return L.T("audit.level.tip");
            }
        }

        /// <summary>一条规则在一次笛卡尔积里的求值上限。超了就不算（宁可漏报，不拖死界面）。</summary>
        private const int MaxCombos = 200000;

        // ---------------------------------------------------------------- 数据结构

        /// <summary>体检用的一条规则快照：全部取"当前生效值"，不取内置原值。</summary>
        public sealed class Item
        {
            public string Id = "";
            public string Name = "";
            public string SystemKey = "";
            public int Priority;
            public int Color;
            public string Effect = "off";
            public double Speed = 1.0;
            public CondNode Cond;
            public bool Enabled = true;
            public bool Fallback;
            public bool Custom;               // 用户新增（usr_ 开头）
            public bool Modified;             // 内置规则，但条件/颜色/效果/速度被改过
            public bool Lit = true;           // 所属系统已绑灯；false = 规则永远不会亮

            /// <summary>这一条该由用户负责吗。内置原样不算用户的账。</summary>
            public bool UserOwned { get { return Custom || Modified; } }
        }

        /// <summary>一条体检结论。都带具体证据，不做"我觉得可能有问题"这种判断。</summary>
        public sealed class Finding
        {
            public string SystemKey = "";
            public string SystemName = "";
            public string Code = "";          // A1 A2 A3 A4 A5 B C D E F
            public FLevel Level = FLevel.Tip;  // 严重程度（显示文字由 UI 取）
            public string RuleId = "";
            public string RuleName = "";
            public string OtherId = "";
            public string OtherName = "";
            public string Text = "";          // 人话结论
            public string Evidence = "";      // 见证点，用户能拿去复现
        }

        // ---------------------------------------------------------------- 快照

        /// <summary>把一套配置展开成体检项。条件、颜色、效果、速度、启用状态全部取当前生效值。</summary>
        public static List<Item> Snapshot(AlertProfile cfg)
        {
            var list = new List<Item>();
            if (cfg == null) return list;

            int[] map = cfg.SystemLedMap;

            foreach (var r in AlertRuleCatalog.Rules)
            {
                var it = MakeItem(cfg, r, map);
                if (it != null) list.Add(it);
            }
            if (cfg.CustomRules != null)
                for (int i = 0; i < cfg.CustomRules.Count; i++)
                {
                    var it = MakeItem(cfg, cfg.CustomRules[i], map);
                    if (it != null) list.Add(it);
                }
            return list;
        }

        /// <summary>
        /// 内置表自检用的快照：条件用内置默认、全部视为启用、全部视为已上灯。
        ///
        /// 为什么不能套用 <see cref="Snapshot"/>：内置表里有 11 条默认停用（样板 5 + 特种装置 6），
        /// 特种装置与自定义默认还没绑灯。出包自检查的是"这张表本身设计得对不对"，
        /// 不是"在这台机器的当前配置下谁亮"——所以这两层过滤统统不施加。
        /// </summary>
        public static List<Item> SnapshotCatalog()
        {
            var list = new List<Item>(AlertRuleCatalog.Rules.Count);
            foreach (var r in AlertRuleCatalog.Rules)
            {
                if (r == null) continue;
                list.Add(new Item
                {
                    Id = r.Id ?? "",
                    Name = r.Name ?? "",
                    SystemKey = r.SystemKey ?? "",
                    Priority = r.Priority,
                    Color = r.Color,
                    Effect = r.EffectId ?? "off",
                    Speed = r.Speed,
                    Cond = r.DefaultCond,
                    Enabled = true,
                    Fallback = r.Fallback,
                    Custom = false,
                    Modified = false,
                    Lit = true
                });
            }
            return list;
        }

        private static Item MakeItem(AlertProfile cfg, AlertRule r, int[] map)
        {
            if (r == null) return null;

            RuleOverride ov = null;
            if (cfg.Overrides != null) cfg.Overrides.TryGetValue(r.Id, out ov);

            int sys = AlertRuleCatalog.SysIndex(r.SystemKey);

            return new Item
            {
                Id = r.Id ?? "",
                Name = r.Name ?? "",
                SystemKey = r.SystemKey ?? "",
                Priority = cfg.PriorityOf(r),      // 生效值：重排过的内置规则以文件那份为准
                Color = cfg.ColorOf(r),
                Effect = cfg.EffectOf(r),
                Speed = cfg.SpeedOf(r),
                Cond = cfg.CondOf(r),
                Enabled = cfg.IsEnabled(r.Id),
                Fallback = r.Fallback,
                Custom = !string.IsNullOrEmpty(r.Id) && r.Id.StartsWith("usr_", StringComparison.OrdinalIgnoreCase),
                Modified = ov != null && ov.Cond != null,   // 只看条件改过，与 IsCustomized 同口径
                Lit = SystemLedMap.LedOf(map, sys) >= 0
            };
        }

        // ---------------------------------------------------------------- 运行

        /// <summary>
        /// 跑体检。
        /// <paramref name="onlyUserRelated"/> = true 时只报"至少一方由用户负责"的结论（绑灯入口用），
        /// false 时报全部（右键入口与出包自检用）。
        /// </summary>
        public static List<Finding> Run(IList<Item> items, bool onlyUserRelated)
        {
            var result = new List<Finding>();
            if (items == null || items.Count == 0) return result;

            // 按系统分组。一灯一系统，所以按系统分组就等于按灯分组，不需要再查映射表。
            var bySys = new Dictionary<string, List<Item>>(StringComparer.OrdinalIgnoreCase);
            foreach (var it in items)
            {
                if (it == null) continue;
                List<Item> g;
                if (!bySys.TryGetValue(it.SystemKey, out g))
                {
                    g = new List<Item>();
                    bySys[it.SystemKey] = g;
                }
                g.Add(it);
            }

            foreach (var kv in SortedSystems(bySys))
            {
                var group = kv.Value;
                AuditOneSystem(kv.Key, group, onlyUserRelated, result);
            }
            return result;
        }

        /// <summary>按系统表原顺序输出，保证清单每次排列一致。</summary>
        private static List<KeyValuePair<string, List<Item>>> SortedSystems(Dictionary<string, List<Item>> bySys)
        {
            var keys = new List<string>(bySys.Keys);
            keys.Sort(delegate (string a, string b)
            {
                int ia = AlertRuleCatalog.SysIndex(a), ib = AlertRuleCatalog.SysIndex(b);
                if (ia != ib) return ia.CompareTo(ib);
                return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
            });
            var list = new List<KeyValuePair<string, List<Item>>>(keys.Count);
            foreach (var k in keys) list.Add(new KeyValuePair<string, List<Item>>(k, bySys[k]));
            return list;
        }

        private static void AuditOneSystem(string sysKey, List<Item> group, bool onlyUserRelated, List<Finding> result)
        {
            string sysName = AlertRuleCatalog.SystemName(sysKey);

            // ---- A5 系统没绑灯：这个系统的规则一条都不会亮
            if (group[0].Lit == false)
            {
                int n = 0;
                foreach (var it in group) if (it.Enabled && !it.Fallback) n++;
                if (n > 0)
                {
                    var f = NewFinding(sysKey, sysName, "A5", FLevel.Error, null, null);
                    f.Text = L.T("audit.a5.nolit", sysName, n);
                    result.Add(f);
                }
                // 没上灯的系统不参与后面的关系比较：它的规则压不住任何东西。
                return;
            }

            // ---- 只看启用的、非兜底的规则参与关系比较
            var live = new List<Item>();
            foreach (var it in group)
                if (it.Enabled && !it.Fallback && it.Cond != null) live.Add(it);

            // 单条检查：A1 恒假 / A2 恒真 / A3 引用未知变量
            foreach (var it in live)
            {
                if (onlyUserRelated && !it.UserOwned) continue;

                string bad = UnknownVar(it.Cond);
                if (bad != null)
                {
                    var f = NewFinding(sysKey, sysName, "A3", FLevel.Error, it, null);
                    f.Text = L.T("audit.a3.unknownvar", bad);
                    result.Add(f);
                    continue;
                }

                bool allTrue, allFalse;
                string witness;
                bool skipped;
                if (CheckConstant(it, out allTrue, out allFalse, out witness, out skipped))
                {
                    if (allFalse)
                    {
                        var f = NewFinding(sysKey, sysName, "A1", FLevel.Error, it, null);
                        f.Text = L.T("audit.a1.never");
                        result.Add(f);
                    }
                    else if (allTrue)
                    {
                        var f = NewFinding(sysKey, sysName, "A2", FLevel.Error, it, null);
                        f.Text = L.T("audit.a2.always");
                        result.Add(f);
                    }
                }
                else if (skipped)
                {
                    // 组合数超上限，这一条根本没算。必须说出来：否则报告显示"没问题"，
                    // 而实际上"没检查"，两者在界面上长得一样。
                    var f = NewFinding(sysKey, sysName, "G", FLevel.Tip, it, null);
                    f.Text = L.T("audit.g.toocomplex.one", RuleNameOf(it));
                    result.Add(f);
                }
            }

            // ---- A4 同优先级
            var seen = new Dictionary<int, Item>();
            foreach (var it in live)
            {
                Item prev;
                if (seen.TryGetValue(it.Priority, out prev))
                {
                    if (!onlyUserRelated || it.UserOwned || prev.UserOwned)
                    {
                        var f = NewFinding(sysKey, sysName, "A4", FLevel.Error, it, prev);
                        f.Text = L.T("audit.a4.sameprio", RuleNameOf(prev), it.Priority);
                        result.Add(f);
                    }
                }
                else seen[it.Priority] = it;
            }

            // ---- F 相邻间距 < 2（两条之间插不进新规则）
            // 只报涉及用户新增规则的相邻对：内置优先级在 RuleOverride 里改不了，
            // 报给用户他也无能为力。内置之间的间距问题由出包自检之外的方式处理。
            var sorted = new List<Item>(live);
            sorted.Sort(delegate (Item a, Item b) { return a.Priority.CompareTo(b.Priority); });
            for (int i = 0; i + 1 < sorted.Count; i++)
            {
                int gap = sorted[i + 1].Priority - sorted[i].Priority;
                if (gap >= 2) continue;
                if (!sorted[i].Custom && !sorted[i + 1].Custom) continue;
                var f = NewFinding(sysKey, sysName, "F", FLevel.Tip, sorted[i + 1], sorted[i]);
                f.Text = L.T("audit.f.gap", RuleNameOf(sorted[i]), sorted[i].Priority,
                             RuleNameOf(sorted[i + 1]), sorted[i + 1].Priority);
                result.Add(f);
            }

            // ---- 两两比较：B / C / D / E
            for (int i = 0; i < live.Count; i++)
                for (int j = i + 1; j < live.Count; j++)
                {
                    var a = live[i];
                    var b = live[j];
                    if (onlyUserRelated && !a.UserOwned && !b.UserOwned) continue;

                    int nA, nB, nAB;
                    string witness;
                    if (!TryRelation(a, b, out nA, out nB, out nAB, out witness))
                    {
                        var fg = NewFinding(sysKey, sysName, "G", FLevel.Tip, a, b);
                        fg.Text = L.T("audit.g.toocomplex.pair", RuleNameOf(a), RuleNameOf(b));
                        result.Add(fg);
                        continue;
                    }
                    if (nAB == 0) continue;                       // 互斥 = 状态划分，正常

                    bool eq = (nAB == nA && nAB == nB);
                    bool aInB = (nAB == nA);
                    bool bInA = (nAB == nB);

                    if (eq)
                    {
                        if (SameOutput(a, b))
                        {
                            var f = NewFinding(sysKey, sysName, "E", FLevel.Redundant, a, b);
                            f.Text = L.T("audit.e.same", RuleNameOf(b));
                            if (!string.IsNullOrEmpty(witness)) f.Evidence = witness;
                            result.Add(f);
                        }
                        else
                        {
                            var f = NewFinding(sysKey, sysName, "C", FLevel.Error, a, b);
                            f.Text = L.T("audit.c.diff", RuleNameOf(b), Describe(a), Describe(b));
                            if (!string.IsNullOrEmpty(witness)) f.Evidence = witness;
                            result.Add(f);
                        }
                        continue;
                    }

                    if (aInB || bInA)
                    {
                        // 包含关系：窄的 + 宽的。宽者优先级更高且颜色更轻 → 窄的那条永不显示。
                        Item narrow = aInB ? a : b;
                        Item wide = aInB ? b : a;
                        if (wide.Priority > narrow.Priority && Severity(wide) < Severity(narrow))
                        {
                            var f = NewFinding(sysKey, sysName, "B", FLevel.Error, narrow, wide);
                            f.Text = L.T("audit.b.shadowed", RuleNameOf(wide),
                                         ColorText(wide), ColorText(narrow));
                            if (!string.IsNullOrEmpty(witness)) f.Evidence = witness;
                            result.Add(f);
                        }
                        continue;                                  // 其余包含关系 = 正常递进
                    }

                    // 交叉重叠：各有独占区
                    if (SameOutput(a, b)) continue;                // 同签名 = 同一告警的两条路径，无害

                    Item hi = a.Priority >= b.Priority ? a : b;
                    Item lo = a.Priority >= b.Priority ? b : a;

                    if (Severity(hi) < Severity(lo))
                    {
                        var f = NewFinding(sysKey, sysName, "C", FLevel.Error, hi, lo);
                        f.Text = L.T("audit.c.overlap", RuleNameOf(lo),
                                     ColorText(hi), ColorText(lo));
                        if (!string.IsNullOrEmpty(witness)) f.Evidence = witness;
                        result.Add(f);
                    }
                    else if (Severity(hi) == Severity(lo) && Urgency(hi) < Urgency(lo)
                             && !string.Equals(hi.Effect, "solid", StringComparison.OrdinalIgnoreCase))
                    {
                        // 「常亮」单独放过：它表达的是"已经发生的事实"（起落架已损坏、油量就这么多），
                        // 与"正在发生、需要回头处置"（闪烁类）不在同一根轴上。
                        // 已损坏用常亮压住正在超速的闪烁，是真机上的习惯，不是倒置。
                        var f = NewFinding(sysKey, sysName, "D", FLevel.Error, hi, lo);
                        f.Text = L.T("audit.d.overlap", RuleNameOf(lo),
                                     EffectText(hi), EffectText(lo));
                        if (!string.IsNullOrEmpty(witness)) f.Evidence = witness;
                        result.Add(f);
                    }
                }
        }

        private static Finding NewFinding(string sysKey, string sysName, string code, FLevel level, Item a, Item b)
        {
            var f = new Finding();
            f.SystemKey = sysKey;
            f.SystemName = sysName;
            f.Code = code;
            f.Level = level;
            if (a != null) { f.RuleId = a.Id; f.RuleName = RuleNameOf(a); }
            if (b != null) { f.OtherId = b.Id; f.OtherName = RuleNameOf(b); }
            return f;
        }

        // ---------------------------------------------------------------- 求值

        /// <summary>递归找第一个不认识的变量名；没有则返回 null。</summary>
        private static string UnknownVar(CondNode n)
        {
            if (n == null) return null;
            if (n.Op == "cmp")
            {
                if (!string.IsNullOrEmpty(n.Var) && !CondNode.IsKnownVar(n.Var)) return n.Var;
                if (!string.IsNullOrEmpty(n.Var2) && !CondNode.IsKnownVar(n.Var2)) return n.Var2;
                return null;
            }
            if (n.Items == null) return null;
            foreach (var c in n.Items)
            {
                var r = UnknownVar(c);
                if (r != null) return r;
            }
            return null;
        }

        /// <summary>单条规则是否恒真/恒假。只有真值集在全代表点上一样时才算，不猜。
        /// skipped = 代表点没算出来（组合数超限），此时 true/false 两个结论都不可信，必须告知调用方。</summary>
        private static bool CheckConstant(Item it, out bool allTrue, out bool allFalse, out string witness, out bool skipped)
        {
            allTrue = allFalse = false;
            witness = null;
            skipped = true;

            var grids = BuildGrids(new[] { it.Cond });
            if (grids == null) return false;

            int nTrue = 0, nTotal = 0;
            string firstTrue = null;
            foreach (var env in Enumerate(grids))
            {
                nTotal++;
                if (Eval(it.Cond, env))
                {
                    nTrue++;
                    if (firstTrue == null) firstTrue = DescribeEnv(env);
                }
            }
            if (nTotal == 0) return false;

            skipped = false;
            allTrue = (nTrue == nTotal);
            allFalse = (nTrue == 0);
            witness = firstTrue;
            return allTrue || allFalse;
        }

        /// <summary>两条规则在联合代表点集上的真值点数，以及第一个同时成立的点。</summary>
        private static bool TryRelation(Item a, Item b, out int nA, out int nB, out int nAB, out string witness)
        {
            nA = nB = nAB = 0;
            witness = null;

            var grids = BuildGrids(new[] { a.Cond, b.Cond });
            if (grids == null) return false;

            foreach (var env in Enumerate(grids))
            {
                bool ra = Eval(a.Cond, env);
                bool rb = Eval(b.Cond, env);
                if (ra) nA++;
                if (rb) nB++;
                if (ra && rb)
                {
                    nAB++;
                    if (witness == null) witness = DescribeEnv(env);
                }
            }
            return true;
        }

        /// <summary>
        /// 为若干条条件构造联合代表点：变量 → 取值列表。
        /// 每条规则的叶子阈值汇总到一起，两边共享同一套点，才能覆盖"变量对变量"的所有相对位置。
        /// 返回 null 表示组合太大，放弃。
        /// </summary>
        private static List<KeyValuePair<string, double[]>> BuildGrids(CondNode[] conds)
        {
            var thresh = new Dictionary<string, List<double>>(StringComparer.OrdinalIgnoreCase);
            var boolVars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var varOrder = new List<string>();

            foreach (var c in conds)
                foreach (var lf in LeavesOf(c))
                {
                    if (string.IsNullOrEmpty(lf.Var)) continue;
                    if (!thresh.ContainsKey(lf.Var)) { thresh[lf.Var] = new List<double>(); varOrder.Add(lf.Var); }

                    if (CondNode.IsBoolCmp(lf.Cmp))
                    {
                        boolVars.Add(lf.Var);
                    }
                    else
                    {
                        thresh[lf.Var].Add(lf.Value);
                        if (lf.Cmp == "between") thresh[lf.Var].Add(lf.Value2);
                    }

                    if (string.IsNullOrEmpty(lf.Var2)) continue;
                    if (!thresh.ContainsKey(lf.Var2)) { thresh[lf.Var2] = new List<double>(); varOrder.Add(lf.Var2); }
                    if (CondNode.IsBoolCmp(lf.Cmp)) boolVars.Add(lf.Var2);
                    else
                    {
                        thresh[lf.Var2].Add(lf.Value);
                        if (lf.Cmp == "between") thresh[lf.Var2].Add(lf.Value2);
                    }
                }

            // 变量对变量的那一对，共享同一套阈值 —— 否则两边的点永远错开，比较不出相对关系。
            foreach (var c in conds)
                foreach (var lf in LeavesOf(c))
                {
                    if (string.IsNullOrEmpty(lf.Var2)) continue;
                    MergeInto(thresh[lf.Var], thresh[lf.Var2]);
                    MergeInto(thresh[lf.Var2], thresh[lf.Var]);
                }

            var grids = new List<KeyValuePair<string, double[]>>(varOrder.Count);
            long total = 1;
            foreach (var v in varOrder)
            {
                var pts = PointsOf(thresh[v], boolVars.Contains(v));
                if (pts.Length == 0) continue;
                total *= pts.Length;
                if (total > MaxCombos) return null;
                grids.Add(new KeyValuePair<string, double[]>(v, pts));
            }
            return grids;
        }

        private static void MergeInto(List<double> from, List<double> to)
        {
            if (from == null || to == null) return;
            foreach (var d in from) if (!to.Contains(d)) to.Add(d);
        }

        /// <summary>
        /// 阈值 → 代表点：每个阈值本身、相邻阈值的中点、两端各加一个点。
        /// 阈值本身必须有，否则 ">" 与 ">=" 分不开；中点是为了覆盖"两个阈值之间"。
        /// </summary>
        private static double[] PointsOf(List<double> thresholds, bool isBool)
        {
            if (isBool) return new double[] { 0, 1 };

            if (thresholds == null || thresholds.Count == 0)
                return new double[] { -1, 0, 1 };

            var s = new List<double>(thresholds);
            s.Sort();
            var pts = new List<double>();
            pts.Add(s[0] - 1);
            for (int i = 0; i < s.Count; i++)
            {
                if (i > 0 && s[i] - s[i - 1] > 1e-9) pts.Add((s[i - 1] + s[i]) / 2);
                if (pts.Count == 0 || Math.Abs(pts[pts.Count - 1] - s[i]) > 1e-9) pts.Add(s[i]);
            }
            pts.Add(s[s.Count - 1] + 1);
            return pts.ToArray();
        }

        private static List<CondNode> LeavesOf(CondNode n)
        {
            var list = new List<CondNode>();
            CollectLeaves(n, list);
            return list;
        }

        private static void CollectLeaves(CondNode n, List<CondNode> into)
        {
            if (n == null) return;
            if (n.Op == "cmp") { into.Add(n); return; }
            if (n.Items == null) return;
            foreach (var c in n.Items) CollectLeaves(c, into);
        }

        /// <summary>笛卡尔积迭代器。用索引数组推进，不递归建中间列表。</summary>
        private static IEnumerable<Dictionary<string, double>> Enumerate(List<KeyValuePair<string, double[]>> grids)
        {
            var env = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            if (grids.Count == 0) { yield return env; yield break; }

            var idx = new int[grids.Count];
            while (true)
            {
                env.Clear();
                for (int i = 0; i < grids.Count; i++) env[grids[i].Key] = grids[i].Value[idx[i]];
                yield return env;

                int k = grids.Count - 1;
                while (k >= 0)
                {
                    idx[k]++;
                    if (idx[k] < grids[k].Value.Length) break;
                    idx[k] = 0;
                    k--;
                }
                if (k < 0) yield break;
            }
        }

        /// <summary>把条件在某组取值下求值。审计里所有变量都有值，所以不需要三态。</summary>
        private static bool Eval(CondNode n, Dictionary<string, double> env)
        {
            if (n == null) return false;

            switch (n.Op)
            {
                case "and":
                    if (n.Items == null) return false;
                    foreach (var c in n.Items) if (!Eval(c, env)) return false;
                    return true;

                case "or":
                    if (n.Items == null) return false;
                    foreach (var c in n.Items) if (Eval(c, env)) return true;
                    return false;

                case "not":
                    if (n.Items == null || n.Items.Count != 1) return false;
                    return !Eval(n.Items[0], env);

                case "cmp":
                    {
                        double left;
                        if (string.IsNullOrEmpty(n.Var) || !env.TryGetValue(n.Var, out left)) return false;

                        if (CondNode.IsBoolCmp(n.Cmp))
                        {
                            bool b = left > 0.5;
                            if (n.Cmp == "nbool") b = !b;
                            return b;
                        }

                        double right;
                        if (!string.IsNullOrEmpty(n.Var2))
                        {
                            if (!env.TryGetValue(n.Var2, out right)) return false;
                        }
                        else right = n.Value;

                        return Compare(left, n.Cmp, right, n.Value2);
                    }

                default:
                    return false;
            }
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

        // ---------------------------------------------------------------- 序与文字

        /// <summary>颜色严重度：关 &lt; 绿 &lt; 琥珀 &lt; 红。</summary>
        private static int Severity(Item it)
        {
            if (it.Color < 0) return 0;
            if (it.Color > 3) return 3;
            return it.Color;
        }

        /// <summary>效果急缓：关 &lt; 常亮 &lt; 心跳/双闪 &lt; 慢闪/一长一短 &lt; 快闪。</summary>
        private static int Urgency(Item it)
        {
            switch (it.Effect)
            {
                case "off": return 0;
                case "solid": return 1;
                case "heartbeat": return 2;
                case "double": return 2;
                case "blink": return 3;
                case "longshort": return 3;
                case "flash": return 4;
                default: return 1;
            }
        }

        private static bool SameOutput(Item a, Item b)
        {
            return a.Color == b.Color && string.Equals(a.Effect, b.Effect, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 结论句里引用的规则名，按当前语言取。
        ///
        /// 为什么不直接用 Item.Name：那是 AlertRule 的中文兜底值（数据表在 AlertRuleData），
        /// 英文界面下会把「与「电瓶开 · 航电关」优先级都是 10」这种半中半英塞进结论。
        /// 规则名的显示口径必须和告警表一致——都走 rule.&lt;Id&gt;。
        /// </summary>
        private static string RuleNameOf(Item it)
        {
            if (it == null) return "";
            return L.Tor("rule." + it.Id, it.Name);
        }

        private static string ColorText(Item it)
        {
            int i = it.Color < 0 ? 0 : (it.Color > 3 ? 3 : it.Color);
            return L.T("color." + i);
        }

        private static string EffectText(Item it)
        {
            string id = it.Effect ?? "";
            if (id.Length == 0) return "?";
            string s = L.Tor("effect." + EffectFactory.Normalize(id), null);
            return string.IsNullOrEmpty(s) ? id : s;
        }

        private static string Describe(Item it)
        {
            return ColorText(it) + EffectText(it);
        }

        /// <summary>把一组取值写成"变量=值"，用户能照着在条件编辑器里填。</summary>
        private static string DescribeEnv(Dictionary<string, double> env)
        {
            var sb = new StringBuilder();
            foreach (var kv in env)
            {
                if (sb.Length > 0) sb.Append(L.T("common.envsep"));
                sb.Append(Derived.LabelOf(kv.Key));
                sb.Append("=");
                sb.Append(Fmt(kv.Value));
                if (sb.Length > 120) { sb.Append("…"); break; }
            }
            return sb.ToString();
        }

        private static string Fmt(double v)
        {
            if (Math.Abs(v - Math.Round(v)) < 1e-9) return ((long)Math.Round(v)).ToString();
            return v.ToString("0.###");
        }
    }
}
