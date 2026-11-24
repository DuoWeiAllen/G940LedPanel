using System;
using System.Collections.Generic;

namespace G940LedPanel.Core
{
    /// <summary>
    /// 告警归并：把「内置 55 条规则 × 当前飞行状态」压成「8 颗灯各显示哪一条」。
    ///
    /// 三条硬规则，缺一条灯就会骗人：
    /// ① 一颗灯只显示一条规则 —— 同灯多命中取 priority 最大者（同系统内 priority 唯一，不会打平）；
    /// ② 三态未知一律不点灯 —— 取不到数据时宁可不亮，也不能把"不知道"当"正常"或"告警"；
    /// ③ 兜底规则不点灯 —— 它只解释"为什么这颗灯是灭的"，条件是"本组其它全不成立"，点亮会自相矛盾。
    ///
    /// 放在 Core 里而不是 UI 里，是为了能离线断言：UI 只负责把结果画成灯，不负责判断。
    /// </summary>
    public static class AlertResolver
    {
        /// <summary>返回 8 个元素，下标 = 灯位 0..7，值 = 命中的规则下标（-1 = 这颗灯不亮）。</summary>
        public static int[] Resolve(AlertProfile cfg, FlightState fs)
        {
            return Resolve(cfg, fs, cfg != null ? cfg.AllRules() : null);
        }

        public static int[] Resolve(AlertProfile cfg, FlightState fs, IList<AlertRule> rules)
        {
            var hitPerLed = new int[SystemLedMap.Count];
            for (int i = 0; i < hitPerLed.Length; i++) hitPerLed[i] = -1;

            if (cfg == null || fs == null || rules == null) return hitPerLed;

            for (int i = 0; i < rules.Count; i++)
            {
                var r = rules[i];
                if (r == null) continue;
                if (r.Fallback) continue;                 // ② 兜底不点灯
                if (!cfg.IsEnabled(r.Id)) continue;
                if (CondEval.Test(cfg.CondOf(r), fs) != Tri.True) continue;   // ③ 未知不点灯

                int sys = AlertRuleCatalog.SysIndex(r.SystemKey);
                int led = SystemLedMap.LedOf(cfg.SystemLedMap, sys);
                if (led < 0 || led >= hitPerLed.Length) continue;

                // ① 同灯取优先级最高的那条。用 PriorityOf 取生效值：重排过的内置规则
                // 以文件里那份为准，直接读 r.Priority 会拿到编译期旧值，和界面显示的次序对不上。
                if (hitPerLed[led] < 0 || cfg.PriorityOf(rules[hitPerLed[led]]) < cfg.PriorityOf(r))
                    hitPerLed[led] = i;
            }

            return hitPerLed;
        }
    }
}
