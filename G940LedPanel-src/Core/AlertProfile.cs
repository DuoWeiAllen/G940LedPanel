using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace G940LedPanel.Core
{
    /// <summary>
    /// 对某一条内置规则的**字段级覆盖**。null 字段 = 用内置值。
    ///
    /// 为什么用一个对象而不是几个平行字典：改条件、改颜色、改效果、改速度、停用，
    /// 本质上都是"这条规则和出厂不一样"，放一个对象里，判断"改没改过"就是
    /// <c>Overrides.ContainsKey(id)</c> 一句，不会出现几个字典各说一套。
    /// </summary>
    public sealed class RuleOverride
    {
        /// <summary>改过条件；null = 用内置条件。</summary>
        [JsonPropertyName("cond")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CondNode Cond { get; set; }

        /// <summary>启用/停用；null = 用内置默认（可选规则默认停用，其余默认启用）。</summary>
        [JsonPropertyName("enabled")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Enabled { get; set; }

        /// <summary>改过颜色；null = 用内置颜色。</summary>
        [JsonPropertyName("color")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Color { get; set; }

        /// <summary>改过效果（solid / blink / flash …）；null = 用内置效果。</summary>
        [JsonPropertyName("effect")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Effect { get; set; }

        /// <summary>改过速度；null = 用内置速度。</summary>
        [JsonPropertyName("speed")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Speed { get; set; }

        /// <summary>
        /// 改过优先级；null = 用内置优先级。
        /// 只有「重排」会写它：同一颗灯里两条规则之间挤不进新的了，就把该灯全部规则按原次序
        /// 重新等距编号，内置那几条也一起编号——所以必须能覆盖内置值。
        /// </summary>
        [JsonPropertyName("priority")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Priority { get; set; }

        /// <summary>六项都没设：这条覆盖没有任何作用，保存时应删掉，别在文件里留空壳。</summary>
        [JsonIgnore]
        public bool IsEmpty
        {
            get
            {
                return Cond == null && Enabled == null && Color == null
                       && string.IsNullOrEmpty(Effect) && Speed == null && Priority == null;
            }
        }

        public RuleOverride Clone()
        {
            return new RuleOverride
            {
                Cond = Cond != null ? Cond.Clone() : null,
                Enabled = Enabled,
                Color = Color,
                Effect = Effect,
                Speed = Speed,
                Priority = Priority
            };
        }
    }

    /// <summary>
    /// 一套告警配置 = 一个文件。
    ///
    /// 它同时承担了原来 <c>AlertConfig</c>（运行配置）和 <c>AlertPreset</c>（命名预设）两个类的职责——
    /// 那两个类的字段几乎完全一样，留两套只会让人问"到底听谁的"。现在只有这一个：
    /// "当前生效的配置"就是"某个 profile 文件"，切换配置就是换文件。
    ///
    /// 存储原则（与内置规则的界线）：
    ///   内置 55 条规则的条件/颜色/效果/速度**不复制进文件**，只存"和内置不一样的"（<see cref="Overrides"/>）；
    ///   这样以后程序修了某条内置规则，没被用户改过的规则会自动跟着变。
    ///   用户**新增**的规则整条存在 <see cref="CustomRules"/> 里，因为它们在内置表里根本不存在。
    /// </summary>
    public sealed class AlertProfile
    {
        public const string KindValue = "g940.alert";
        public const int FormatVersion = 1;

        [JsonPropertyName("kind")]
        public string Kind { get; set; } = KindValue;

        [JsonPropertyName("version")]
        public int Version { get; set; } = FormatVersion;

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        /// <summary>10 个系统 → 灯位（-1 = 不上灯，物理灯只有 8 颗）。</summary>
        [JsonPropertyName("ledMap")]
        public int[] SystemLedMap { get; set; } = G940LedPanel.Core.SystemLedMap.Default();

        /// <summary>规则 id → 对内置规则的覆盖（只存改过的）。</summary>
        [JsonPropertyName("ruleOverrides")]
        public Dictionary<string, RuleOverride> Overrides { get; set; } = new Dictionary<string, RuleOverride>();

        /// <summary>用户新增的规则（内置表里没有的）。</summary>
        [JsonPropertyName("customRules")]
        public List<AlertRule> CustomRules { get; set; } = new List<AlertRule>();

        // 相机状态表（cameraStates）不放这里：它是"这台机器对模拟器画面的认知"，
        // 和用哪一套告警规则无关，所以归 config.json（见 AppConfig.CameraStates）。

        // ---------------------------------------------------------------- 运行时字段（不进文件）

        /// <summary>从哪个文件读出来的（内置配置为空）。</summary>
        [JsonIgnore]
        public string FilePath { get; set; } = "";

        /// <summary>内置配置：只能载入、另存为，不能改、不能删。</summary>
        [JsonIgnore]
        public bool IsBuiltIn { get; set; }

        /// <summary>本次体检里被丢弃的覆盖/自定义规则（界面用来把那几行标橙）。</summary>
        [JsonIgnore]
        public List<string> DroppedRuleIds { get; set; } = new List<string>();

        // ---------------------------------------------------------------- 构造

        public static AlertProfile CreateDefault()
        {
            return new AlertProfile();
        }

        /// <summary>
        /// 预置告警配置：读 alerts\builtin\ 下的文件（1.3.48 起，不再写死在代码里）。
        ///
        /// 读不到就是没有——用户把文件删了，列表里就少一套，程序不重建、不兜底。
        /// 显示名由 <see cref="AlertNames"/> 按文件名给出；用户改了文件里的 name 也不影响，
        /// 因为译名只在代码里。加一套预置 = 加一个 json + 在 AlertNames 里加一条。
        /// </summary>
        public static List<AlertProfile> BuiltInProfiles()
        {
            var list = new List<AlertProfile>();

            string[] files;
            try { files = Directory.GetFiles(ProfileStore.BuiltInAlertsDir, "*.json"); }
            catch (Exception) { return list; }

            foreach (var f in files)
            {
                AlertProfile p;
                string err;
                if (!ProfileStore.LoadAlert(f, out p, out err)) continue;

                string stem = Path.GetFileNameWithoutExtension(f);
                string disp = AlertNames.Of(stem);
                if (disp != null) p.Name = disp;
                list.Add(p);
            }

            list.Sort((a, b) =>
            {
                int ia = AlertNames.Index(Path.GetFileNameWithoutExtension(a.FilePath));
                int ib = AlertNames.Index(Path.GetFileNameWithoutExtension(b.FilePath));
                return ia != ib ? ia.CompareTo(ib)
                                : string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase);
            });
            return list;
        }

        // ---------------------------------------------------------------- 取值

        /// <summary>按 id 找规则：先找内置，再找自定义。</summary>
        public AlertRule FindRule(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var r = AlertRuleCatalog.Find(id);
            if (r != null) return r;
            if (CustomRules != null)
                for (int i = 0; i < CustomRules.Count; i++)
                    if (CustomRules[i] != null && string.Equals(CustomRules[i].Id, id, StringComparison.OrdinalIgnoreCase))
                        return CustomRules[i];
            return null;
        }

        /// <summary>运行时规则表 = 内置 55 条 + 用户新增的。判定与兜底重算都要用它。</summary>
        /// <remarks>
        /// 结果缓存：点灯判定每收到一帧就调一次（每秒几十次），每次重建 60 来个元素的列表没必要。
        /// 规则表变过（增删自定义规则、载入体检）一律走 <see cref="InvalidateClearCond"/> 清缓存，
        /// 只改字段不增删（改条件、改样式、改优先级）不用清：缓存里装的是对象引用，字段变了自然读到新值。
        /// </remarks>
        public List<AlertRule> AllRules()
        {
            if (_allCache != null) return _allCache;

            var list = new List<AlertRule>(AlertRuleCatalog.Rules.Count + (CustomRules != null ? CustomRules.Count : 0));
            list.AddRange(AlertRuleCatalog.Rules);
            if (CustomRules != null)
                for (int i = 0; i < CustomRules.Count; i++)
                    if (CustomRules[i] != null) list.Add(CustomRules[i]);
            _allCache = list;
            return list;
        }

        private List<AlertRule> _allCache;

        private RuleOverride Of(string id)
        {
            RuleOverride o;
            if (Overrides != null && Overrides.TryGetValue(id, out o)) return o;
            return null;
        }

        /// <summary>
        /// 兜底条件的缓存（系统 key → 条件）。不进文件：它是"由规则表算出来的"，存了反而会跟规则表对不上。
        /// 表格的原始值列 250ms 刷一次，没有缓存的话每秒要重建 30 多棵条件树。
        /// </summary>
        private Dictionary<string, CondNode> _clearCache;

        /// <summary>条件或规则表变过 —— 兜底条件与规则表缓存都要重算。</summary>
        public void InvalidateClearCond()
        {
            if (_clearCache != null) _clearCache.Clear();
            _allCache = null;
        }

        /// <summary>取某条规则当前生效的条件：用户改过就用用户的，否则用内置（自定义规则的内置就是它自己）。</summary>
        public CondNode CondOf(AlertRule rule)
        {
            if (rule == null) return null;
            var o = Of(rule.Id);
            if (o != null && o.Cond != null) return o.Cond;

            // 兜底规则的条件是"本组其它规则全不成立"拼出来的。内置里那份是编译期算的，
            // 用户新增/删掉自定义规则后就过时了 —— 不重算会出现"灯灭了、兜底却说都对得上"。
            if (rule.Fallback) return ClearCondOf(rule.SystemKey);

            return rule.DefaultCond;
        }

        private CondNode ClearCondOf(string systemKey)
        {
            if (_clearCache == null) _clearCache = new Dictionary<string, CondNode>();
            CondNode c;
            if (_clearCache.TryGetValue(systemKey, out c)) return c;

            c = AlertRuleCatalog.BuildClearCond(systemKey, AllRules(), r => CondOf(r));
            _clearCache[systemKey] = c;
            return c;
        }

        public int ColorOf(AlertRule rule)
        {
            if (rule == null) return 0;
            var o = Of(rule.Id);
            if (o != null && o.Color.HasValue) return o.Color.Value;
            return rule.Color;
        }

        public string EffectOf(AlertRule rule)
        {
            if (rule == null) return "off";
            var o = Of(rule.Id);
            if (o != null && !string.IsNullOrEmpty(o.Effect)) return o.Effect;
            return rule.EffectId;
        }

        public double SpeedOf(AlertRule rule)
        {
            if (rule == null) return 1.0;
            var o = Of(rule.Id);
            if (o != null && o.Speed.HasValue) return o.Speed.Value;
            return rule.Speed;
        }

        /// <summary>
        /// 取某条规则当前生效的优先级。重排过的内置规则以文件里那份为准，其余用编译期值。
        /// **凡是判断"同一颗灯里谁压谁"的地方都必须走这里**：点灯判定、表格排序、插入位置列表、
        /// 规则体检。直接读 rule.Priority 只对没重排过的规则正确。
        /// </summary>
        public int PriorityOf(AlertRule rule)
        {
            if (rule == null) return 0;
            var o = Of(rule.Id);
            if (o != null && o.Priority.HasValue) return o.Priority.Value;
            return rule.Priority;
        }

        /// <summary>
        /// 写一条规则的生效优先级。内置规则写覆盖（与编译期值相同就把这项删掉，不留空壳），
        /// 自定义规则改自身字段——它的优先级本来就存在规则里，没有"内置值"可言。
        /// </summary>
        private void SetPriority(AlertRule rule, int priority)
        {
            if (rule == null) return;

            if (AlertRuleCatalog.Find(rule.Id) == null)   // 不在内置表里 = 自定义规则
            {
                rule.Priority = priority;
                return;
            }

            var o = GetOrCreate(rule.Id);
            o.Priority = priority == rule.Priority ? (int?)null : priority;
            if (o.IsEmpty) Overrides.Remove(rule.Id);
        }

        // ---------------------------------------------------------------- 启用

        public bool IsEnabled(string ruleId)
        {
            var r = FindRule(ruleId);
            if (r == null) return false;
            var o = Of(ruleId);
            if (o != null && o.Enabled.HasValue) return o.Enabled.Value;
            return !r.Optional;                 // 可选规则默认停用，其余默认启用
        }

        public void SetEnabled(string ruleId, bool on)
        {
            var r = FindRule(ruleId);
            if (r == null) return;

            bool dflt = !r.Optional;
            var o = GetOrCreate(ruleId);
            if (on == dflt)
            {
                // 和内置默认一致 → 不需要覆盖
                o.Enabled = null;
                if (o.IsEmpty) Overrides.Remove(ruleId);
            }
            else
            {
                o.Enabled = on;
            }
        }

        /// <summary>当前启用了几条（含自定义规则）。界面显示计数用。</summary>
        [JsonIgnore]
        public int EnabledCount
        {
            get
            {
                int n = 0;
                foreach (var r in AlertRuleCatalog.Rules) if (IsEnabled(r.Id)) n++;
                if (CustomRules != null)
                    foreach (var r in CustomRules) if (r != null && IsEnabled(r.Id)) n++;
                return n;
            }
        }

        // ---------------------------------------------------------------- 覆盖维护

        private RuleOverride GetOrCreate(string id)
        {
            if (Overrides == null) Overrides = new Dictionary<string, RuleOverride>();
            RuleOverride o;
            if (!Overrides.TryGetValue(id, out o) || o == null)
            {
                o = new RuleOverride();
                Overrides[id] = o;
            }
            return o;
        }

        /// <summary>
        /// 这条规则的**触发条件**有没有被改过（界面据此在行上加小圆点）。
        /// 只看条件，不看其余四项：勾一下启用、改个颜色、重排改优先级，都不该被标成"条件改过"。
        /// </summary>
        public bool IsCustomized(string ruleId)
        {
            var o = Of(ruleId);
            return o != null && o.Cond != null;
        }

        /// <summary>还原某条规则的全部内置值：删掉它的覆盖。</summary>
        public void ResetRule(string ruleId)
        {
            if (Overrides != null) Overrides.Remove(ruleId);
            InvalidateClearCond();
        }

        public void SetCond(string ruleId, CondNode cond)
        {
            var o = GetOrCreate(ruleId);
            o.Cond = cond;
            if (o.IsEmpty) Overrides.Remove(ruleId);
            InvalidateClearCond();
        }

        public void SetStyle(string ruleId, int? color, string effect, double? speed)
        {
            var o = GetOrCreate(ruleId);
            o.Color = color;
            o.Effect = effect;
            o.Speed = speed;
            if (o.IsEmpty) Overrides.Remove(ruleId);
        }

        // ---------------------------------------------------------------- 新增规则

        /// <summary>
        /// 在指定系统的 <paramref name="afterId"/> 之后插入一条自定义规则，返回新规则。
        /// 优先级由"前后两条的中值"算出，内置规则的优先级一个都不动。
        /// afterId 为空 = 排在最前；找不到 = 排在最后。
        /// </summary>
        public AlertRule AddCustomRule(string systemKey, string name, string afterId,
                                       int color, string effect, double speed, CondNode cond)
        {
            if (CustomRules == null) CustomRules = new List<AlertRule>();

            var rule = new AlertRule
            {
                Id = NextCustomId(),
                SystemKey = systemKey,
                Name = string.IsNullOrWhiteSpace(name) ? L.T("ap.custom.defaultname") : name.Trim(),
                Color = (byte)color,
                EffectId = effect,
                Speed = speed > 0 ? speed : 1.0,
                DefaultCond = cond,
                Optional = false,
                Fallback = false
            };
            rule.Priority = PickPriority(systemKey, afterId, rule.Id);
            if (rule.Priority <= 0) return null;      // 这个位置插不进去，由调用方提示换个位置

            CustomRules.Add(rule);
            InvalidateClearCond();        // 兜底条件要把新规则算进去
            return rule;
        }

        private string NextCustomId()
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in AlertRuleCatalog.Rules) used.Add(r.Id);
            if (CustomRules != null)
                foreach (var r in CustomRules) if (r != null) used.Add(r.Id);

            for (int i = 1; i < 100000; i++)
            {
                string id = "usr_" + i;
                if (!used.Contains(id)) return id;
            }
            return "usr_" + DateTime.Now.Ticks;
        }

        /// <summary>
        /// 算一个能插进"两条之间"的优先级：取前后两条的中值。
        /// 中值算不出来（两档之间已经没有整数空隙，或"最前面"只剩 1）时，先把该系统的规则
        /// 按原次序重新等距编号，空隙回来后再算一次——见 <see cref="Renumber"/>。
        /// 只有连重排都腾不出位置（该系统没有规则可排）才返回 -1。
        /// </summary>
        public int PickPriority(string systemKey, string afterId, string excludeId)
        {
            int v = PickPriorityOnce(systemKey, afterId, excludeId);
            if (v > 0) return v;

            // 重排不改相对次序，所以"插在某条之后"这个语义不变，用户不用重新选位置
            if (Renumber(systemKey) == 0) return -1;
            return PickPriorityOnce(systemKey, afterId, excludeId);
        }

        private int PickPriorityOnce(string systemKey, string afterId, string excludeId)
        {
            var list = new List<AlertRule>();
            foreach (var r in AllRules())
            {
                if (r == null || r.Fallback) continue;
                if (r.SystemKey != systemKey) continue;
                if (excludeId != null && r.Id == excludeId) continue;
                list.Add(r);
            }
            list.Sort(delegate (AlertRule a, AlertRule b)
            {
                int c = PriorityOf(a).CompareTo(PriorityOf(b));
                return c != 0 ? c : string.CompareOrdinal(a.Id, b.Id);
            });

            if (string.IsNullOrEmpty(afterId))
            {
                // 排在最前：取最前面那条的一半，而不是"减 10"。
                // 减 10 会在最小值为 10 时算出 0，而 0 是"还没算过"的标记值，
                // 落盘体检时会被当成无效再算一遍，结果位置跑了。
                if (list.Count == 0) return 10;
                int first = PriorityOf(list[0]);
                return first >= 2 ? first / 2 : -1;
            }

            int at = -1;
            for (int i = 0; i < list.Count; i++)
                if (list[i].Id == afterId) { at = i; break; }
            if (at < 0) return list.Count > 0 ? PriorityOf(list[list.Count - 1]) + 10 : 10;

            if (at == list.Count - 1) return PriorityOf(list[at]) + 10;

            int lo = PriorityOf(list[at]), hi = PriorityOf(list[at + 1]);
            if (hi - lo < 2) return -1;              // 没有空位
            return lo + (hi - lo) / 2;
        }

        /// <summary>重排后的第一档。取 100 而非 10：给"插到最前"留 6 次二分余量（100→50→25→12→6→3→1）。</summary>
        private const int RenumberStart = 100;

        /// <summary>重排后的档距，与内置表的 10 一致。</summary>
        private const int RenumberStep = 10;

        /// <summary>
        /// 把某个系统（= 一颗灯）里的规则按当前相对次序重新等距编号，返回改了几条。
        /// **相对次序一条都不动**，所以内置规则谁压谁完全不变，变的只是数字本身。
        /// 内置规则通过覆盖生效（编译期常量一个都不改），自定义规则改自身字段。
        /// 兜底规则不参与：它不点灯、优先级固定最低，编进去只会跟内置表撞值。
        /// </summary>
        public int Renumber(string systemKey)
        {
            if (string.IsNullOrEmpty(systemKey)) return 0;

            var list = new List<AlertRule>();
            foreach (var r in AllRules())
                if (r != null && !r.Fallback && r.SystemKey == systemKey) list.Add(r);
            if (list.Count == 0) return 0;

            // 同值时按 id 排：保证重排结果确定，不受遍历顺序影响
            list.Sort(delegate (AlertRule a, AlertRule b)
            {
                int c = PriorityOf(a).CompareTo(PriorityOf(b));
                return c != 0 ? c : string.CompareOrdinal(a.Id, b.Id);
            });

            int changed = 0, p = RenumberStart;
            foreach (var r in list)
            {
                if (PriorityOf(r) != p) { SetPriority(r, p); changed++; }
                p += RenumberStep;
            }
            return changed;
        }

        /// <summary>删除一条自定义规则（内置规则不能删，只能停用）。</summary>
        public bool RemoveCustomRule(string ruleId)
        {
            if (CustomRules == null) return false;
            for (int i = 0; i < CustomRules.Count; i++)
            {
                if (CustomRules[i] != null && CustomRules[i].Id == ruleId)
                {
                    CustomRules.RemoveAt(i);
                    if (Overrides != null) Overrides.Remove(ruleId);   // 它的覆盖也一起清掉
                    InvalidateClearCond();
                    return true;
                }
            }
            return false;
        }

        public bool IsCustomRule(string ruleId)
        {
            if (CustomRules == null) return false;
            foreach (var r in CustomRules)
                if (r != null && r.Id == ruleId) return true;
            return false;
        }

        // ---------------------------------------------------------------- 体检

        /// <summary>
        /// 1.3.70 老配置归一：以前效果列有个「关」，与颜色关成对出现；现在颜色是唯一
        /// 开关，效果列不再有 off。把残留的 off 就地改成 solid（颜色仍是关，行为一字不变）。
        /// 必须在 Sanitize 的白名单校验之前做，否则 CustomRules 里的 off 会被当坏规则整条丢弃；
        /// Overrides 那一段本来就不校验 Effect，不改的话表格会显示一个下拉里没有的值。
        /// 只在内存归一，不改写用户文件——每次载入幂等，Core 层也不碰磁盘。
        /// </summary>
        private void NormalizeLegacyOff()
        {
            if (Overrides != null)
                foreach (var kv in Overrides)
                    if (kv.Value != null && kv.Value.Effect == "off") kv.Value.Effect = "solid";
            if (CustomRules != null)
                foreach (var r in CustomRules)
                    if (r != null && r.EffectId == "off") r.EffectId = "solid";
        }

        /// <summary>
        /// 载入后的体检。所有异常都"修掉并说明"，绝不让程序带着坏配置跑。
        /// </summary>
        public List<string> Sanitize()
        {
            var notes = new List<string>();
            if (DroppedRuleIds == null) DroppedRuleIds = new List<string>();
            // 体检会增删规则，先让规则表缓存过期：否则后面查"生效优先级有没有重复"看到的还是旧表
            InvalidateClearCond();
            // 每次体检都从零开始记：这个列表是"这一次载入的结论"，不是累计日志。
            // 不清的话，切三次配置就会把同一句"X 的条件不合法"记三遍，
            // 告警表那一行也被重复标记（虽然肉眼看不出，但长度会一直涨）。
            DroppedRuleIds.Clear();
            NormalizeLegacyOff();   // 1.3.70：先归一旧值，下面才会做白名单校验

            // 老配置是 8 个系统的 int[8]。扩到 10 系统后必须先补齐（前 8 项原样保留、
            // 新增 2 项填 -1 不上灯），再交给 Validate —— 否则老配置会因"长度不对"
            // 被判非法而复位成默认，用户自定义的灯位就全丢了。
            SystemLedMap = G940LedPanel.Core.SystemLedMap.Pad(SystemLedMap);
            if (!G940LedPanel.Core.SystemLedMap.Validate(SystemLedMap))
            {
                SystemLedMap = G940LedPanel.Core.SystemLedMap.Default();
                notes.Add(L.T("ap.note.ledmap"));
            }


            // ---- 覆盖：逐条校验，坏的回落到内置 ----
            if (Overrides == null) Overrides = new Dictionary<string, RuleOverride>();
            var badOverrides = new List<string>();
            var orphanOverrides = new List<string>();
            var emptyOverrides = new List<string>();
            foreach (var kv in Overrides)
            {
                // 规则已经不存在，和"条件写坏了"是两回事：前者用户改不了，只能被告知删了；
                // 后者用户能进编辑器修。原来共用一句"条件不合法"，会让人去翻一条不属于自己的错。
                if (FindRule(kv.Key) == null) { orphanOverrides.Add(kv.Key); continue; }
                if (kv.Value == null) { emptyOverrides.Add(kv.Key); continue; }
                if (kv.Value.Cond != null && kv.Value.Cond.Validate().Count > 0) { badOverrides.Add(kv.Key); continue; }
                // 重排写进来的优先级：<=0 是无效值（0 是"还没算过"的标记），清掉回落到内置值
                if (kv.Value.Priority.HasValue && kv.Value.Priority.Value <= 0) kv.Value.Priority = null;
                if (kv.Value.IsEmpty) emptyOverrides.Add(kv.Key);
            }
            foreach (var id in badOverrides) Overrides.Remove(id);
            foreach (var id in orphanOverrides) Overrides.Remove(id);
            foreach (var id in emptyOverrides) Overrides.Remove(id);
            foreach (var id in badOverrides) DroppedRuleIds.Add(id);
            foreach (var id in orphanOverrides) DroppedRuleIds.Add(id);
            if (badOverrides.Count > 0)
                notes.Add(L.T("ap.note.badcond", badOverrides.Count,
                          string.Join(L.T("common.listsep"), badOverrides.ToArray())));
            if (orphanOverrides.Count > 0)
                notes.Add(L.T("ap.note.orphanrule", orphanOverrides.Count,
                          string.Join(L.T("common.listsep"), orphanOverrides.ToArray())));

            // ---- 自定义规则：id/系统/颜色/效果/优先级 ----
            if (CustomRules == null) CustomRules = new List<AlertRule>();
            var badRules = new List<AlertRule>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in AlertRuleCatalog.Rules) ids.Add(r.Id);

            foreach (var r in CustomRules)
            {
                if (r == null) continue;
                if (string.IsNullOrEmpty(r.Id)) { badRules.Add(r); continue; }
                if (!r.Id.StartsWith("usr_", StringComparison.OrdinalIgnoreCase)) r.Id = "usr_" + r.Id;
                if (!ids.Add(r.Id)) { badRules.Add(r); DroppedRuleIds.Add(r.Id); continue; }
                if (AlertRuleCatalog.SysIndex(r.SystemKey) < 0) { badRules.Add(r); DroppedRuleIds.Add(r.Id); continue; }
                if (r.DefaultCond == null || r.DefaultCond.Validate().Count > 0) { badRules.Add(r); DroppedRuleIds.Add(r.Id); continue; }
                // 1.3.70：只判「效果在不在告警页白名单里」。颜色与效果不再需要配对判据，
                // 颜色是唯一开关；老配置里的 off 已在 Sanitize 开头归一成 solid。
                if (Array.IndexOf(AlertRuleCatalog.AllowedEffects, r.EffectId) < 0) { badRules.Add(r); DroppedRuleIds.Add(r.Id); continue; }
                if (r.Speed <= 0) r.Speed = 1.0;
                // 优先级缺失/坏了：排到本组最后。不能用 PickPriority（那是"插到最前"，
                // 连续修几条时会越修越小，最后挤成 0 又变成无效）
                if (r.Priority <= 0) r.Priority = EndPriority(r.SystemKey, r.Id);
                r.Fallback = false;
            }
            foreach (var r in badRules) CustomRules.Remove(r);
            if (badRules.Count > 0)
                notes.Add(L.T("ap.note.badrule", badRules.Count,
                          string.Join(L.T("common.listsep"), BadNames(badRules).ToArray())));

            // 同系统优先级重复：判定会变成"谁先遍历谁赢"，等于随机，必须修
            var dup = FixDuplicatePriorities();
            if (dup > 0) notes.Add(L.T("ap.note.duppri", dup));

            // 修完还撞（重排过又还原了某条、或手工改过 config 里的 priority）：
            // 不用挑谁对谁错，整组按当前次序重新编号即可，相对次序一条都不变。
            foreach (var sys in AlertRuleCatalog.Systems)
            {
                if (!HasDuplicatePriority(sys.Key)) continue;
                Renumber(sys.Key);
                notes.Add(L.T("ap.note.renumber", AlertRuleCatalog.SystemName(sys.Key)));
            }

            // ---- 相机状态表不在这里体检：它属于 config.json，见 AppConfig.SanitizeCamera ----

            // 体检过程动过 Overrides / CustomRules，兜底条件的缓存可能存着旧结果
            InvalidateClearCond();

            return notes;
        }

        /// <summary>本组最后一个位置的优先级（最大 + 10）。</summary>
        private int EndPriority(string systemKey, string excludeId)
        {
            int max = 0;
            foreach (var r in AllRules())
            {
                if (r == null || r.Fallback || r.SystemKey != systemKey) continue;
                if (excludeId != null && r.Id == excludeId) continue;
                int p = PriorityOf(r);
                if (p > max) max = p;
            }
            return max + 10;
        }

        private static List<string> BadNames(List<AlertRule> rules)
        {
            var list = new List<string>();
            foreach (var r in rules)
                list.Add(r == null ? L.T("ap.empty") : (string.IsNullOrEmpty(r.Name) ? r.Id : r.Name));
            return list;
        }

        /// <summary>同一颗灯里有两条规则生效优先级相同 —— 谁压谁取决于遍历顺序，等于随机。</summary>
        private bool HasDuplicatePriority(string systemKey)
        {
            var seen = new HashSet<int>();
            foreach (var r in AllRules())
            {
                if (r == null || r.Fallback || r.SystemKey != systemKey) continue;
                if (!seen.Add(PriorityOf(r))) return true;
            }
            return false;
        }

        /// <summary>同一系统内优先级重复 → 给重复的自定义规则让出一个空位。</summary>
        private int FixDuplicatePriorities()
        {
            if (CustomRules == null || CustomRules.Count == 0) return 0;
            int fixedCount = 0;

            foreach (var sys in AlertRuleCatalog.Systems)
            {
                var used = new HashSet<int>();
                foreach (var r in AlertRuleCatalog.Rules)
                    if (r.SystemKey == sys.Key && !r.Fallback) used.Add(PriorityOf(r));

                foreach (var r in CustomRules)
                {
                    if (r == null || r.SystemKey != sys.Key) continue;
                    if (used.Add(PriorityOf(r))) continue;

                    // 撞了：往后找第一个空位（间隔 1），找不到就往前
                    int p = PriorityOf(r);
                    while (used.Contains(p)) p++;
                    r.Priority = p;
                    used.Add(p);
                    fixedCount++;
                }
            }
            return fixedCount;
        }

        /// <summary>深拷贝（配置下拉切换、另存为都用它，避免两份共享同一个 Overrides 字典）。</summary>
        public AlertProfile Clone()
        {
            var c = new AlertProfile
            {
                Kind = Kind,
                Version = Version,
                Name = Name,
                SystemLedMap = G940LedPanel.Core.SystemLedMap.Clone(SystemLedMap),
                Overrides = new Dictionary<string, RuleOverride>(),
                CustomRules = new List<AlertRule>(),
                FilePath = FilePath,
                IsBuiltIn = IsBuiltIn
            };
            if (Overrides != null)
                foreach (var kv in Overrides)
                    c.Overrides[kv.Key] = kv.Value == null ? null : kv.Value.Clone();

            if (CustomRules != null)
                foreach (var r in CustomRules)
                    if (r != null) c.CustomRules.Add(r.Clone());

            return c;
        }

        /// <summary>把另一套配置的内容搬进来（名字与身份不动）。</summary>
        public void CopyFrom(AlertProfile other)
        {
            if (other == null) return;
            SystemLedMap = G940LedPanel.Core.SystemLedMap.Clone(other.SystemLedMap);
            Overrides = new Dictionary<string, RuleOverride>();
            if (other.Overrides != null)
                foreach (var kv in other.Overrides)
                    Overrides[kv.Key] = kv.Value == null ? null : kv.Value.Clone();
            CustomRules = new List<AlertRule>();
            if (other.CustomRules != null)
                foreach (var r in other.CustomRules)
                    if (r != null) CustomRules.Add(r.Clone());
            InvalidateClearCond();
        }
    }
}
