using System;
using System.Collections.Generic;

namespace G940LedPanel.Core
{
    /// <summary>被监视的系统之一（1.3.42 起共 10 个，系统数可以多于 8 颗物理灯）。</summary>
    public sealed class AlertSystem
    {
        public string Key;
        public string Name;
        public int DefaultLed;

        public AlertSystem(string key, string name, int defaultLed)
        {
            Key = key; Name = name; DefaultLed = defaultLed;
        }
    }

    /// <summary>一条告警规则。内置 55 条固定；用户在配置里可以新增自己的（id 以 usr_ 开头）。</summary>
    public sealed class AlertRule
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id;

        /// <summary>挂在哪个系统（决定哪颗灯）。</summary>
        [System.Text.Json.Serialization.JsonPropertyName("system")]
        public string SystemKey;

        /// <summary>规则本身的名字，表格显示为「系统 · 规则」。</summary>
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name;

        /// <summary>同一颗灯内，数值大者胜。内置规则的优先级是编译期固定的，用户新增的由程序算中值。</summary>
        [System.Text.Json.Serialization.JsonPropertyName("priority")]
        public int Priority;

        /// <summary>默认不启用（只是内置规则的概念，不进文件）。</summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public bool Optional;

        /// <summary>兜底规则：不产生灯光，只解释"为什么这颗灯不亮"。</summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public bool Fallback;

        /// <summary>
        /// 样板规则：条件由程序自算、不是 MSFS 判定的事实，也不是真机座舱里的灯。
        /// 表格上标「程序自算 · 非 MSFS 判定」，供用户照着改成自己的规则。全部默认停用。
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public bool Sample;

        [System.Text.Json.Serialization.JsonPropertyName("color")]
        public byte Color;

        [System.Text.Json.Serialization.JsonPropertyName("effect")]
        public string EffectId;

        [System.Text.Json.Serialization.JsonPropertyName("speed")]
        public double Speed;

        /// <summary>条件；运行时可能被配置里的覆盖替换（见 AlertProfile.CondOf）。</summary>
        [System.Text.Json.Serialization.JsonPropertyName("cond")]
        public CondNode DefaultCond;

        /// <summary>
        /// 1.3.57：原来的 `DisplayName` 属性（系统名 + " · " + 规则名）删掉了。
        ///
        /// 它把两个**显示名**拼在 Core 里，而 Core 不编语言包（断言工程只链纯逻辑），
        /// 于是它只能给出中文——英文界面下规则列永远半中半英。
        /// 现在由 `UI/TextNames.RuleDisplay(r)` 现拼，本层只出 Id / SystemKey 这两个身份。
        /// 删属性而不是留着不用，是为了让"还想直接拿显示名"的代码**编译不过**。
        /// </summary>

        public AlertRule Clone()
        {
            return new AlertRule
            {
                Id = Id,
                SystemKey = SystemKey,
                Name = Name,
                Priority = Priority,
                Optional = Optional,
                Fallback = Fallback,
                Sample = Sample,
                Color = Color,
                EffectId = EffectId,
                Speed = Speed,
                DefaultCond = DefaultCond != null ? DefaultCond.Clone() : null
            };
        }
    }

    /// <summary>
    /// 内置规则库。这里的东西是"写死"的：10 个系统、55 条规则、每条的颜色/效果/优先级。
    ///
    /// 用户能改的只有两件事：每条规则的条件（换变量、改数值、加"且/或"）与是否启用，
    /// 以及**自己新增规则**（id 以 usr_ 开头，存在 alerts\ 的配置文件里，不进这里）。
    ///
    /// 内置规则的优先级一个都不许被用户改动 —— 那是判定"同灯多命中取谁"的底线，
    /// 一旦有副本被改过，同一个飞行状态在两台机器上就会亮得不一样，排查时无从下手。
    /// 用户新增规则时，优先级由"前后两条取中值"算出来，插进档位链而不重排它。
    /// </summary>
    public static class AlertRuleCatalog
    {
        /// <summary>告警页允许的效果（只有单灯节奏类，不含「关」）。</summary>
        /// 1.3.70：颜色是唯一的关灯开关，效果只决定「亮起来怎么亮」，所以这里不再有 "off"。
        /// 引擎级 EffectFactory.Ids 仍保留 off（底灯、按键默认值、非法 id 兜底要用），
        /// 那是引擎的开关，不是告警页的显示选项，两者不要混为一谈。
        public static readonly string[] AllowedEffects =
        {
            "solid", "blink", "double", "longshort", "flash", "heartbeat"
        };

        // 1.3.53：这里原来有一份 ColorNames = { "关", "绿", "琥珀", "红" }。
        // 删掉它是因为它是**显示名**，而显示名要跟着语言变，不能写死在 Core（Core 的断言工程
        // 只链纯逻辑、不编语言包）。现在由 UI/TextNames.Color(i) 现取，本层只认序号 0..3。
        // 反过来也解决了一个老问题：原来表格的单元格里存的就是「绿」这种显示文字，
        // 切语言后拿英文格子去查中文表必然查不到。现在读写都经过同一份当前语言的名表。

        /// <summary>速度档位（倍速，越大越快）。规则里的 Speed 可以是任意值，下拉只给常用档。</summary>
        public static readonly double[] SpeedSteps = { 0.5, 1.0, 2.0, 4.0 };

        // 1.3.70 删掉 StyleMismatch：颜色与效果曾经各有一个「关」，从「关/关」出发改任
        // 一列都会被自洽判据回滚，而表格一次只能提交一列，7 条内置规则因此永远改不了样式。
        // 现在颜色是唯一开关，效果只作「亮起来后的预设」，任何组合都自洽，不需要判据。
        // 三处调用（载入体检、表格写回、新建窗）与静态构造里的强校验一并删除。

        /// <summary>10 个被监视的系统。数据表在 AlertRuleData（纯数据，理由见那个文件）。</summary>
        public static readonly AlertSystem[] Systems = AlertRuleData.Systems;

        private static readonly List<AlertRule> _rules = AlertRuleData.BuildRules();
        private static readonly Dictionary<string, AlertRule> _byId = BuildIndex();
        private static readonly Dictionary<string, int> _sysIndex = BuildSysIndex();

        static AlertRuleCatalog()
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var prioPerSys = new Dictionary<string, HashSet<int>>();

            foreach (var r in _rules)
            {
                if (!ids.Add(r.Id))
                    throw new InvalidOperationException(L.T("acl.err.dupid", r.Id));

                if (Array.IndexOf(AllowedEffects, r.EffectId) < 0)
                    throw new InvalidOperationException(L.T("acl.err.effect", r.Id, r.EffectId));

                if (SysIndex(r.SystemKey) < 0)
                    throw new InvalidOperationException(L.T("acl.err.nosys", r.Id, r.SystemKey));

                if (r.DefaultCond == null)
                    throw new InvalidOperationException(L.T("acl.err.nocond", r.Id));

                // 1.3.70 删掉「off 只能配灭、非 off 不能配灭」的强校验：颜色关 + 任意亮效果
                // 现在是合法组合（引擎取色为 0，仍不亮），留着它反而启动即抛异常。

                // 同一颗灯内优先级不能重复，否则谁覆盖谁取决于遍历顺序，等于随机
                HashSet<int> set;
                if (!prioPerSys.TryGetValue(r.SystemKey, out set))
                {
                    set = new HashSet<int>();
                    prioPerSys[r.SystemKey] = set;
                }
                if (!set.Add(r.Priority))
                    throw new InvalidOperationException(L.T("acl.err.prio", r.SystemKey, r.Priority, r.Id));
            }
        }

        public static List<AlertRule> Rules { get { return _rules; } }

        public static int Count { get { return _rules.Count; } }

        public static AlertRule Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            AlertRule r;
            return _byId.TryGetValue(id, out r) ? r : null;
        }

        public static int IndexOf(string id)
        {
            for (int i = 0; i < _rules.Count; i++) if (_rules[i].Id == id) return i;
            return -1;
        }

        public static int SysIndex(string key)
        {
            if (string.IsNullOrEmpty(key)) return -1;
            int i;
            return _sysIndex.TryGetValue(key, out i) ? i : -1;
        }

        /// <summary>
        /// 系统显示名。1.3.63 起走语言包（`sys.&lt;Key&gt;`），查不到才退回内置中文名。
        ///
        /// 为什么改在这里而不是每个调用点各拼一次：SystemName 有 4 个消费点
        /// （规则体检、告警页脚注、体检窗、灯位分配），其中两处（MainForm 的灯体文字、
        /// RuleAudit 的结论句）原先直接读 `Systems[i].Name`，英文界面下印的是中文，
        /// 而体检扫的是「字面量」——读字段不产生字面量，扫不到。
        /// </summary>
        public static string SystemName(string key)
        {
            int i = SysIndex(key);
            return L.Tor("sys." + key, i >= 0 ? Systems[i].Name : key);
        }

        /// <summary>默认启用的 42 条（非可选）规则 id。</summary>
        public static List<string> DefaultEnabledIds()
        {
            var list = new List<string>();
            foreach (var r in _rules) if (!r.Optional) list.Add(r.Id);
            return list;
        }

        public static int OptionalCount
        {
            get
            {
                int n = 0;
                foreach (var r in _rules) if (r.Optional) n++;
                return n;
            }
        }

        // ---------------------------------------------------------------- 取值助手

        /// <summary>某系统的“为什么灭”兜底规则的条件由同组其它规则拼出来，用户改了某条也仍然一致。</summary>
        public static CondNode BuildClearCond(string systemKey, Func<AlertRule, CondNode> condOf)
        {
            return BuildClearCond(systemKey, _rules, condOf);
        }

        /// <summary>
        /// 同上，但按传入的规则表算——运行时规则表可能是"内置 + 用户新增"，
        /// 兜底条件必须把新增的也算进去，否则会出现"灯灭了、兜底却说都对得上"。
        /// </summary>
        public static CondNode BuildClearCond(string systemKey, IList<AlertRule> rules, Func<AlertRule, CondNode> condOf)
        {
            var others = new List<CondNode>();
            if (rules != null)
            {
                for (int i = 0; i < rules.Count; i++)
                {
                    var r = rules[i];
                    if (r == null || r.SystemKey != systemKey || r.Fallback) continue;
                    var c = condOf != null ? condOf(r) : r.DefaultCond;
                    if (c != null) others.Add(c);
                }
            }
            return Cond.Not(Cond.Or(others.ToArray()));
        }


        private static Dictionary<string, AlertRule> BuildIndex()
        {
            var d = new Dictionary<string, AlertRule>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in _rules) d[r.Id] = r;
            return d;
        }

        private static Dictionary<string, int> BuildSysIndex()
        {
            var d = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < Systems.Length; i++) d[Systems[i].Key] = i;
            return d;
        }
    }
}
