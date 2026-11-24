using System;
using System.Collections.Generic;

namespace G940LedPanel.Core
{
    /// <summary>
    /// 脱离模拟器的试算工具：把"人手填的一组变量值"做成一个 <see cref="FlightState"/>。
    ///
    /// 为什么需要它：条件编辑器原来只能拿模拟器实时数据试算，
    /// 没开模拟器、或模拟器当前状态根本不满足条件（比如想试"三腿不一致"，
    /// 总不能真把起落架掰歪），就永远看不到结果，等于没法验证自己改的条件对不对。
    ///
    /// 关键约束：**没填的变量按"无数据"处理**，而不是按 0 处理。
    /// 否则三态就退化成两态，"未除冰"这类条件在试算里会假成立，和真机表现不一致。
    /// </summary>
    public static class ManualProbe
    {
        /// <summary>
        /// 一条条件实际依赖的原始变量（派生量会展开成它依赖的底层量），按出现顺序去重。
        /// 派生量本身不列——它算不出来就没人能"手填"它。
        /// </summary>
        public static List<string> VarsUsed(CondNode root)
        {
            var list = new List<string>();
            Collect(root, list);
            return list;
        }

        private static void Collect(CondNode n, List<string> into)
        {
            if (n == null) return;

            if (n.Op == "cmp")
            {
                AddVar(n.Var, into);
                AddVar(n.Var2, into);
                return;
            }

            if (n.Items == null) return;
            foreach (var c in n.Items) Collect(c, into);
        }

        private static void AddVar(string name, List<string> into)
        {
            if (string.IsNullOrEmpty(name)) return;

            if (Derived.IsDerived(name))
            {
                foreach (var raw in Derived.RawVarsOf(name)) AddVar(raw, into);
                return;
            }

            for (int i = 0; i < into.Count; i++)
                if (string.Equals(into[i], name, StringComparison.OrdinalIgnoreCase)) return;
            into.Add(name);
        }

        /// <summary>用"变量名 → 值"构造一帧。没出现在字典里的变量一律算"没有数据"。</summary>
        public static FlightState Build(IDictionary<string, double> values)
        {
            int n = SimVars.Count;
            var v = new double[n];
            var has = new bool[n];

            if (values != null)
            {
                foreach (var kv in values)
                {
                    int i = SimVars.IndexOf(kv.Key);
                    if (i < 0 || i >= n) continue;
                    v[i] = kv.Value;
                    has[i] = true;
                }
            }

            return new FlightState(v, has, 0);
        }

        /// <summary>从模拟器真实帧里把指定变量抄出来（"从模拟器快照填充"用）。</summary>
        public static void CopyFrom(FlightState src, IEnumerable<string> names, IDictionary<string, double> into)
        {
            if (src == null || names == null || into == null) return;
            foreach (var name in names)
            {
                double v;
                int i = SimVars.IndexOf(name);
                if (i < 0) continue;
                if (!src.Has(name)) { into.Remove(name); continue; }
                v = src.N(name);
                into[name] = v;
            }
        }
    }
}
