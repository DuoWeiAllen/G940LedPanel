using System;
using System.Collections.Generic;

namespace G940LedPanel.Core
{
    /// <summary>
    /// 系统 → 灯位 的映射。
    ///
    /// 1.3.42 起系统数（10）与灯数（8）分开：灯永远是 8 颗，但系统可以比灯多。
    /// 每个系统 map[sys] 取值为 -1（不上灯）或 0..7（挂到第几颗灯）；非 -1 的灯位互不重复。
    /// 于是「10 组里挑 8 组配到灯上」就是这张表的自然状态，未上灯的系统其规则不参与归并。
    ///
    /// 用户改某个系统的灯位时，若目标灯已被别的系统占用，两者交换——这样
    /// "一颗灯最多一个系统"这个前提永远不会被破坏，不需要额外校验"重复灯位"。
    /// 新系统从 -1 上灯时，被挤下来的原系统自动变成 -1（下灯），符合直觉。
    ///
    /// 注意 SysOf 必须扫描全部系统，不能只扫灯数个——否则新增系统（下标 8、9）
    /// 永远反查不到，灯上文字会退回 Pn。
    /// </summary>
    public static class SystemLedMap
    {
        /// <summary>物理灯数，恒定 8。绝不随系统数变化；归并结果数组也用它定长。</summary>
        public const int Count = 8;

        /// <summary>该系统不上灯。</summary>
        public const int NoLed = -1;

        /// <summary>系统数：跟随规则库的系统表，以后加系统只改 Systems 一处即可。</summary>
        public static int SystemCount { get { return AlertRuleCatalog.Systems.Length; } }

        /// <summary>默认：前 8 个系统各占一颗灯，其后新增的系统一律不上灯。</summary>
        public static int[] Default()
        {
            var m = new int[SystemCount];
            for (int i = 0; i < m.Length; i++) m[i] = i < Count ? i : NoLed;
            return m;
        }

        /// <summary>
        /// 把旧配置（可能是 8 长）补齐成 SystemCount 长：原有项原样保留，缺的补 -1。
        /// 已是当前长度则原样返回。放在 Validate 之前调用——老用户灯位分毫不变，
        /// 也不会因为"长度不对"被判非法而复位成默认、丢掉自定义灯位。
        /// </summary>
        public static int[] Pad(int[] map)
        {
            if (map != null && map.Length == SystemCount) return map;
            var m = new int[SystemCount];
            for (int i = 0; i < m.Length; i++)
                m[i] = (map != null && i < map.Length) ? map[i] : NoLed;
            return m;
        }

        /// <summary>长度 = SystemCount；每项要么 -1、要么 0..7；非 -1 的灯位不重复。</summary>
        public static bool Validate(int[] map)
        {
            if (map == null || map.Length != SystemCount) return false;

            bool[] seen = new bool[Count];
            for (int i = 0; i < map.Length; i++)
            {
                int v = map[i];
                if (v == NoLed) continue;
                if (v < 0 || v >= Count) return false;
                if (seen[v]) return false;
                seen[v] = true;
            }
            return true;
        }

        /// <summary>
        /// 把第 sys 个系统挂到灯 led；目标灯原来的占用者换到 sys 原来的灯位。
        /// sys 原来可能是 -1（未上灯），此时被挤下来的系统就下灯（-1）。
        /// </summary>
        public static void Assign(int[] map, int sys, int led)
        {
            if (map == null || map.Length != SystemCount) return;
            if (sys < 0 || sys >= SystemCount) return;
            if (led < 0 || led >= Count) return;

            int old = map[sys];
            if (old == led) return;

            int other = SysOf(map, led);
            if (other == sys) return;
            if (other >= 0) map[other] = old;   // old 可能是 -1 → 被挤者下灯
            map[sys] = led;
        }

        /// <summary>让第 led 颗灯空出来（挂在其上的系统下灯）。灯位窗口选「（空）」时用。</summary>
        public static void ClearLed(int[] map, int led)
        {
            if (map == null) return;
            int sys = SysOf(map, led);
            if (sys >= 0) map[sys] = NoLed;
        }

        /// <summary>第 sys 个系统挂在哪颗灯；未上灯或越界返回 -1。</summary>
        public static int LedOf(int[] map, int sys)
        {
            if (map == null || sys < 0 || sys >= SystemCount || sys >= map.Length) return NoLed;
            int v = map[sys];
            return (v >= 0 && v < Count) ? v : NoLed;
        }

        /// <summary>哪套系统挂在灯 led 上；没有返回 -1。扫描全部系统（不能只扫灯数个）。</summary>
        public static int SysOf(int[] map, int led)
        {
            if (map == null) return -1;
            for (int i = 0; i < map.Length && i < SystemCount; i++)
                if (map[i] == led) return i;
            return -1;
        }

        public static int[] Clone(int[] map)
        {
            if (map == null) return Default();
            var copy = new int[map.Length];
            Array.Copy(map, copy, map.Length);
            return copy;
        }

        public static bool Same(int[] a, int[] b)
        {
            if (a == null || b == null) return a == b;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        public static string LedName(int led)
        {
            return "P" + (led + 1);
        }

        // ---------------------------------------------------------------- 单灯配置候选项

        /// <summary>候选项的归属类别。文字由 UI 按当前语言给，Core 只说"属于哪一种"。</summary>
        public enum LedTag
        {
            /// <summary>首项「（空）」：这颗灯空出来。</summary>
            Empty,
            /// <summary>该系统本来就挂在这颗灯上。</summary>
            Current,
            /// <summary>该系统还没上灯。</summary>
            NoLedYet,
            /// <summary>该系统挂在别的灯上，选它会交换。</summary>
            OnOtherLed
        }

        /// <summary>
        /// 点开某颗灯时供挑选的一个候选项。SysIndex = <see cref="NoLed"/> 表示「（空）」。
        ///
        /// 1.3.58：原来这里有个 <c>Text</c> 属性，里面是 Core 拼好的中文
        /// （<c>name + " · " + tag</c>）。那是 1.3.53 明令禁止的形态
        /// ——Core 不该知道东西显示叫什么，英文界面下它变不出来。
        /// 现在只给 <see cref="SystemKey"/> / <see cref="Tag"/> / <see cref="WhereLed"/>，
        /// 显示名由 UI 经 <c>TextNames.SysName</c> 现取。
        /// </summary>
        public sealed class LedOption
        {
            public int SysIndex { get; private set; }
            /// <summary>系统 key（如 Power）。「（空）」项为 null。</summary>
            public string SystemKey { get; private set; }
            public LedTag Tag { get; private set; }
            /// <summary>Tag == OnOtherLed 时，系统当前所在灯位（0 起）；否则 -1。</summary>
            public int WhereLed { get; private set; }
            public bool IsCurrent { get; private set; }

            public LedOption(int sysIndex, string systemKey, LedTag tag, int whereLed, bool isCurrent)
            {
                SysIndex = sysIndex;
                SystemKey = systemKey;
                Tag = tag;
                WhereLed = whereLed;
                IsCurrent = isCurrent;
            }
        }

        /// <summary>
        /// 列出 led 这颗灯可挂的全部系统（含首项「（空）」），按系统表原顺序。
        /// 每项标注该系统当前在哪：本来就挂在这颗灯 =「当前」，没上灯 =「未上灯」，
        /// 挂在别的灯 =「现挂 Pn」。选后者会触发 <see cref="Assign"/> 的交换/挤下灯，
        /// 先把归属摆出来，用户才知道自己会动到另一颗灯。
        /// </summary>
        public static List<LedOption> OptionsForLed(int[] map, int led)
        {
            var list = new List<LedOption>();
            if (led < 0 || led >= Count) return list;

            int cur = SysOf(map, led);
            list.Add(new LedOption(NoLed, null, LedTag.Empty, -1, cur < 0));

            for (int s = 0; s < SystemCount; s++)
            {
                int where = LedOf(map, s);
                LedTag tag;
                if (s == cur) tag = LedTag.Current;
                else if (where < 0) tag = LedTag.NoLedYet;
                else tag = LedTag.OnOtherLed;

                string key = s < AlertRuleCatalog.Systems.Length
                    ? AlertRuleCatalog.Systems[s].Key
                    : null;                       // 超出系统表（理论上不会有）：UI 显示「系统 N」
                list.Add(new LedOption(s, key, tag, tag == LedTag.OnOtherLed ? where : -1, s == cur));
            }
            return list;
        }
    }
}
