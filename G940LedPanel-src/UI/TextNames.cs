using System;
using G940LedPanel.Core;
using G940LedPanel.Effects;

namespace G940LedPanel.UI
{
    /// <summary>
    /// 显示名的唯一真源（1.3.53）。
    ///
    /// 为什么单独立一个类：效果名、颜色名、触发名、目标名这四张表，表格里显示、
    /// 下拉里列、状态行里拼、右键菜单里印，一处改了另一处没改就会出现
    /// 「表格写英文、下拉写中文」。集中成一个入口，将来加语言只加 .resx，这里零改动。
    ///
    /// 为什么放 UI 层而不是 Core 或 Effects：
    /// Core 的断言工程只链纯逻辑文件、不编 L.cs（也不需要语言包），
    /// Effects 只负责出帧、不该知道显示叫什么。放 UI 才是依赖方向正确的位置。
    ///
    /// 为什么用方法而不是静态数组：数组在类型初始化时求值一次，一旦缓存，
    /// 切语言后全是旧语言。这里每次现取，代价是几十次 ResourceManager 查找，可忽略。
    /// </summary>
    public static class TextNames
    {
        private static int Clamp(int v, int lo, int hi) { return v < lo ? lo : (v > hi ? hi : v); }

        // ---------- 效果 ----------

        /// <summary>效果名。id 取 <see cref="EffectFactory.Ids"/> 里的值（off / solid / blink …）。</summary>
        public static string Effect(string id)
        {
            return L.T("effect." + EffectFactory.Normalize(id));
        }

        /// <summary>当前语言下的全部效果名，索引与 <see cref="EffectFactory.Ids"/> 一一对应。</summary>
        public static string[] EffectAll()
        {
            string[] ids = EffectFactory.Ids;
            var a = new string[ids.Length];
            for (int i = 0; i < ids.Length; i++) a[i] = Effect(ids[i]);
            return a;
        }

        /// <summary>
        /// 按键行的效果名：不含「关灯」。
        /// 按键行的效果是「按下这颗键会怎样」，选关灯等于这一行没有反馈，属于没配；
        /// 底灯相反，底灯本来就要能关，所以底灯走 <see cref="BackgroundEffects"/>，不去掉。
        /// </summary>
        public static string[] BindEffects()
        {
            string[] ids = EffectFactory.Ids;
            var a = new System.Collections.Generic.List<string>(ids.Length);
            for (int i = 0; i < ids.Length; i++)
                if (ids[i] != "off") a.Add(Effect(ids[i]));
            return a.ToArray();
        }

        /// <summary>
        /// 底灯下拉的显示名：第 0 项是「不装饰」，其余标「装饰」。
        /// 只改显示文本，索引仍和 Ids 一一对应——效果对象本身存的是 id，不是名字。
        /// </summary>
        public static string[] BackgroundEffects()
        {
            string[] ids = EffectFactory.Ids;
            var a = new string[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                a[i] = i == 0
                    ? Effect(ids[0]) + L.T("effect.suffix.default")
                    : Effect(ids[i]) + L.T("effect.suffix.decor");
            }
            return a;
        }

        // ---------- 颜色 ----------

        /// <summary>颜色名。0=关 1=绿 2=琥珀 3=红，越界按端点算。</summary>
        public static string Color(int i)
        {
            return L.T("color." + Clamp(i, 0, 3));
        }

        public static string[] ColorAll()
        {
            var a = new string[4];
            for (int i = 0; i < 4; i++) a[i] = Color(i);
            return a;
        }

        // ---------- 触发方式 ----------

        /// <summary>触发方式名。"hold" = 按住生效，"toggle" = 按一下切换。</summary>
        public static string Trigger(string id)
        {
            return L.T(string.Equals(id, "toggle", StringComparison.OrdinalIgnoreCase)
                ? "bind.trigger.toggle"
                : "bind.trigger.hold");
        }

        public static string[] TriggerAll()
        {
            return new[] { Trigger("hold"), Trigger("toggle") };
        }

        // ---------- 目标灯 ----------

        /// <summary>
        /// 目标灯名。0 = 全部；1..8 = P1..P8。
        /// P1..P8 不译：它是面板上的丝印，翻了就和实物对不上。
        /// </summary>
        public static string Target(int i)
        {
            return i <= 0 ? L.T("bind.target.all") : "P" + i;
        }

        public static string[] TargetAll()
        {
            var a = new string[9];
            for (int i = 0; i < 9; i++) a[i] = Target(i);
            return a;
        }

        // ---------- 规则名 / 系统名（1.3.57） ----------

        /// <summary>
        /// 系统名。key = `sys.<SystemKey>`（Power / Engine / Gear …）。
        ///
        /// 为什么用 `Tor` 而不是 `T`：用户可以在配置里自建系统（`Custom` 之外的未知 key），
        /// 语言包不会有它。缺 key 时回落到 Core 里那份中文名，至少不是「!sys_xxx」。
        /// </summary>
        public static string SysName(string systemKey)
        {
            return L.Tor("sys." + systemKey, AlertRuleCatalog.SystemName(systemKey));
        }

        /// <summary>
        /// 规则名。key = `rule.<规则 id>`。用户新增的规则 id 以 `usr_` 开头、名字是自己起的，
        /// 语言包不会有它——`Tor` 直接回落到用户起的那个名字，**不翻译用户自建内容**。
        /// </summary>
        public static string RuleName(AlertRule r)
        {
            if (r == null) return "";
            return L.Tor("rule." + r.Id, r.Name);
        }

        /// <summary>表格与提示里显示的一整条：「系统 · 规则」。</summary>
        public static string RuleDisplay(AlertRule r)
        {
            if (r == null) return "";
            return SysName(r.SystemKey) + " · " + RuleName(r);
        }

        // ---------- 配置名（1.3.58） ----------

        /// <summary>
        /// 内置配置的显示名。key = `profile.<文件名>`（twin_piston / demo_ring …）。
        ///
        /// 三条规矩：
        ///   1. 只有内置配置才查语言包——用户自己起的名字**不翻译**，翻了就认不出哪套是哪套；
        ///   2. 「（内置）」前缀在这儿加。原来它写在 <c>ProfileEntry.ToString()</c> 里，
        ///      等于把界面文案写死在数据层，英文界面下变不出来；
        ///   3. 语言包里没有的（用户导入的、老版本留下的）回落到 Core 那份中文名，不是 `!profile.xxx`。
        /// </summary>
        public static string ProfileName(string stem, bool isBuiltIn, string fallback)
        {
            if (!isBuiltIn) return fallback ?? "";
            return L.T("common.builtin.tag", L.Tor("profile." + stem, fallback ?? ""));
        }

        /// <summary>下拉列表项（预置与用户配置共用）。stem 取 <see cref="ProfileEntry.Key"/>。</summary>
        public static string ProfileName(ProfileEntry e)
        {
            if (e == null) return "";
            return ProfileName(StemOf(e.FilePath), e.IsBuiltIn, e.Name);
        }

        public static string ProfileName(AlertProfile p)
        {
            if (p == null) return "";
            return ProfileName(StemOf(p.FilePath), p.IsBuiltIn, p.Name);
        }

        public static string ProfileName(ButtonProfile p)
        {
            if (p == null) return "";
            return ProfileName(StemOf(p.FilePath), p.IsBuiltIn, p.Name);
        }

        /// <summary>从配置文件路径取文件名（不含扩展名）。取不到时返回空串，由调用方回落。</summary>
        private static string StemOf(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return "";
            try
            {
                string s = System.IO.Path.GetFileNameWithoutExtension(filePath);
                return s ?? "";
            }
            catch (ArgumentException) { return ""; }   // 路径里有非法字符：当取不到处理
        }
    }
}
