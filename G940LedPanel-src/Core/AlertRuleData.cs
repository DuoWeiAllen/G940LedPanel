using System;
using System.Collections.Generic;

namespace G940LedPanel.Core
{
    /// <summary>
    /// 内置告警规则的**数据表**：10 个系统、55 条规则。
    ///
    /// 为什么从 AlertRuleCatalog 拆出来（1.3.63）：本文件里的中文全是**兜底值**——
    /// 规则名由 UI/TextNames.RuleName 经 `L.Tor("rule." + Id, Name)` 先查语言包，
    /// 系统名同理（`sys.<Key>`），55 条 rule.* key 齐全，英文界面下这些中文一条都不显示。
    /// 但它们原先和「用户能看到的错误提示」挤在同一个文件里，而体检按**文件**判「翻没翻」，
    /// 于是只剩两个坏选择：整文件豁免（真文案跟着漏）或整文件翻译（兜底数据白翻一遍，
    /// 而 Name 还要写进用户配置，改了会破坏老文件的显示）。
    ///
    /// 拆成纯数据文件的判据与 SimVars.cs 同一条：这段字**不参与判断**，
    /// 中文是数据本身（规则名 / 系统名），不是要翻译的界面文案。
    /// </summary>
    internal static class AlertRuleData
    {
        internal static readonly AlertSystem[] Systems =
        {
            new AlertSystem("Power",         "电源",     0),
            new AlertSystem("Engine",        "发动机",   1),
            new AlertSystem("Gear",          "起落架",   2),
            new AlertSystem("Flaps",         "襟翼",     3),
            new AlertSystem("Lights",        "外部灯光", 4),
            new AlertSystem("Autopilot",     "自动驾驶", 5),
            new AlertSystem("MasterCaution", "告警总灯", 6),
            new AlertSystem("Fuel",          "燃油",     7),
            // 1.3.42 新增的两个系统都是「容器」：默认不上灯（-1），由用户点 LED 弹配置框
            // 从 10 组里挑 8 组配上去。特种装置内建一批常用规则，自定义留空给用户自己组。
            new AlertSystem("Special",      "特种装置", -1),
            new AlertSystem("Custom",       "自定义",   -1)
        };

        // ---------------------------------------------------------------- 构造助手

        private static AlertRule R(string id, string sys, string name, int prio, LedColor color,
                                 string effect, double speed, CondNode cond, bool optional = false, bool sample = false, bool fallback = false)
        {
            return new AlertRule
            {
                Id = id,
                SystemKey = sys,
                Name = name,
                Priority = prio,
                Optional = optional,
                Sample = sample,
                Fallback = fallback,
                Color = (byte)color,
                EffectId = effect,
                Speed = speed,
                DefaultCond = cond
            };
        }

        private static CondNode B(string name) { return Cond.B(name); }
        private static CondNode NB(string name) { return Cond.NB(name); }
        private static CondNode N(string name, string cmp, double v) { return Cond.N(name, cmp, v); }
        private static CondNode And(params CondNode[] items) { return Cond.And(items); }
        private static CondNode Or(params CondNode[] items) { return Cond.Or(items); }
        private static CondNode Not(CondNode n) { return Cond.Not(n); }

        // ---------------------------------------------------------------- 55 条规则

        internal static List<AlertRule> BuildRules()
        {
            var L = new List<AlertRule>();

            // ============================ 电源（P1，3 条） ============================
            L.Add(R("pwr_bat_only", "Power", "电瓶开 · 航电关", 10, LedColor.Amber, "solid", 1.0,
                    And(B("ELECTRICAL MASTER BATTERY"), NB("AVIONICS MASTER SWITCH"))));

            L.Add(R("pwr_avionics_on", "Power", "航电已开", 20, LedColor.Green, "solid", 1.0,
                    B("AVIONICS MASTER SWITCH")));

            L.Add(R("pwr_avionics_no_bat", "Power", "航电开但电瓶关", 30, LedColor.Red, "flash", 1.2,
                    And(B("AVIONICS MASTER SWITCH"), NB("ELECTRICAL MASTER BATTERY"))));

            // ============================ 发动机（P2，4 条） ============================
            L.Add(R("eng_all_off", "Engine", "全部关车", 10, LedColor.Off, "solid", 1.0,
                    N(Derived.EngBurning, "==", 0)));

            L.Add(R("eng_all_run", "Engine", "全部运转", 20, LedColor.Green, "solid", 1.0,
                    And(N(Derived.EngBurning, ">", 0), Cond.V(Derived.EngBurning, "==", Derived.EngCount))));

            // 1.3.44：改用官方 ENG FAILED。自算「运转台数少于总台数」分不清“失效”和“还没启动”，
            // 总台数在部分机型上读数也不准；ENG FAILED:1~4 是模拟器直接给的结论。
            L.Add(R("eng_partial", "Engine", "发动机失效", 30, LedColor.Amber, "blink", 1.2,
                    Or(B("ENG FAILED:1"), B("ENG FAILED:2"), B("ENG FAILED:3"), B("ENG FAILED:4"))));

            L.Add(R("eng_fire", "Engine", "发动机火警", 40, LedColor.Red, "flash", 2.0,
                    Or(B("ENG ON FIRE:1"), B("ENG ON FIRE:2"), B("ENG ON FIRE:3"), B("ENG ON FIRE:4"))));

            // ============================ 起落架（P3，8 条） ============================
            L.Add(R("gear_up_locked", "Gear", "收起锁定", 10, LedColor.Off, "solid", 1.0,
                    And(NB("GEAR HANDLE POSITION"), N(Derived.GearTotal, "<", 5))));

            L.Add(R("gear_transit", "Gear", "收放中", 20, LedColor.Green, "blink", 1.5,
                    Cond.Between(Derived.GearTotal, 5, 95)));

            L.Add(R("gear_down_locked", "Gear", "放下锁定", 30, LedColor.Green, "solid", 1.0,
                    N(Derived.GearTotal, ">", 95)));

            L.Add(R("gear_disagree", "Gear", "三腿不一致", 40, LedColor.Amber, "flash", 1.5,
                    N(Derived.GearSpread, ">", 20)));

            // 1.3.45：优先级 45 → 35。原来比"三腿不一致"(40) 高，于是这条"程序自算的建议类"
            // 压过了"真机座舱里就有的警告"——方向反了。降到 35：介于"放下锁定"(30) 与
            // "三腿不一致"(40) 之间，仍是进近/起飞阶段的提醒，但不再盖住真实故障。
            L.Add(R("adv_gear_retract", "Gear", "起飞后收轮提醒", 35, LedColor.Amber, "blink", 1.5,
                    And(B("IS GEAR RETRACTABLE"),
                        NB("SIM ON GROUND"),
                        N("PLANE ALT ABOVE GROUND", ">", 500),
                        N(Derived.GearTotal, ">", 5),
                        N("AIRSPEED INDICATED", ">", 120)),
                    optional: true, sample: true));

            // 1.3.44：改用官方 GEAR SPEED EXCEEDED。原来按机型档位自算上限（165/200/250 kn），
            // 每架飞机的真实上限只有模拟器自己知道。
            L.Add(R("gear_overspeed", "Gear", "起落架超速", 50, LedColor.Red, "flash", 2.0,
                    B("GEAR SPEED EXCEEDED")));

            L.Add(R("adv_approach_cfg", "Gear", "进近构型提醒", 55, LedColor.Red, "flash", 2.0,
                    And(NB("SIM ON GROUND"),
                        N("PLANE ALT ABOVE GROUND", "<", 1000),
                        N("AIRSPEED INDICATED", "<", 130),
                        Or(N(Derived.GearTotal, "<", 95), N("FLAPS HANDLE PERCENT", "<", 20))),
                    optional: true, sample: true));

            // 1.3.44 补记：这条当初被删，理由是「已由官方 GEAR SPEED EXCEEDED 与三腿不一致覆盖」，
            // 该理由不成立 —— 超速判定管"已超速"，三腿不一致管"腿之间不齐"，
            // 「离地了、高度低、速度慢、轮还收着」两者都不触发，是独立的一种状态。
            // 真正成立的只有定位那条：它不是 MSFS 判定，也不是真机座舱里的灯，所以只能是样板。
            // 优先级取 58：低于"已损坏"(60)，高于"进近构型提醒"(55)。
            // 两个原来的阈值（AglWarn / IasGearDown）按活塞档写成常量 500 ft / 130 kn，
            // 与 adv_gear_retract 的 500、adv_approach_cfg 的 130 同一口径。
            // #GearTotal<95 前件顺带解决固定起落架机型：该机型恒为 100，条件自然不成立。
            L.Add(R("gear_low_alt", "Gear", "低空未放起落架", 58, LedColor.Red, "flash", 2.0,
                    And(NB("SIM ON GROUND"),
                        N("PLANE ALT ABOVE GROUND", "<", 500),
                        N("AIRSPEED INDICATED", "<", 130),
                        N(Derived.GearTotal, "<", 95)),
                    optional: true, sample: true));

            // 1.3.44：官方「已因超速损坏」占 60 档：模拟器给定的事实，真机上也有损坏指示。
            L.Add(R("gear_damaged", "Gear", "起落架超速损坏", 60, LedColor.Red, "solid", 2.5,
                    B("GEAR DAMAGE BY SPEED")));

            // ============================ 襟翼 / 扰流板（P4，6 条） ============================
            L.Add(R("flap_up", "Flaps", "襟翼收上", 10, LedColor.Off, "solid", 1.0,
                    N("FLAPS HANDLE PERCENT", "<", 2)));

            L.Add(R("flap_deployed", "Flaps", "襟翼已放出", 20, LedColor.Green, "blink", 1.0,
                    Cond.Between("FLAPS HANDLE PERCENT", 2, 90)));

            L.Add(R("flap_full", "Flaps", "襟翼全放", 30, LedColor.Green, "solid", 1.0,
                    N("FLAPS HANDLE PERCENT", ">", 90)));

            L.Add(R("spoiler_armed", "Flaps", "扰流板预位", 40, LedColor.Amber, "solid", 1.0,
                    B("SPOILERS ARMED")));

            L.Add(R("spoiler_deployed", "Flaps", "扰流板已放出（空中）", 50, LedColor.Amber, "blink", 2.0,
                    And(N("SPOILERS HANDLE POSITION", ">", 5), NB("SIM ON GROUND"))));

            L.Add(R("to_config_warn", "Flaps", "起飞构型警告", 60, LedColor.Amber, "flash", 2.0,
                    And(B("SIM ON GROUND"),
                        N(Derived.MaxThrottle, ">", 90),
                        Or(N("FLAPS HANDLE PERCENT", "<", 10), N("SPOILERS HANDLE POSITION", ">", 5)))));

            // 1.3.44 新增：两条都用 MSFS 官方判定量，不自算门槛。
            L.Add(R("flap_overspeed", "Flaps", "襟翼超速", 70, LedColor.Red, "flash", 2.0,
                    B("FLAP SPEED EXCEEDED")));

            L.Add(R("flap_damaged", "Flaps", "襟翼超速损坏", 80, LedColor.Red, "solid", 2.5,
                    B("FLAP DAMAGE BY SPEED")));

            // ============================ 外部灯光（P5，4 条） ============================
            L.Add(R("lights_all_off", "Lights", "外部灯全关", 10, LedColor.Off, "solid", 1.0,
                    And(NB("LIGHT NAV"), NB("LIGHT BEACON"), NB("LIGHT STROBE"),
                        NB("LIGHT LANDING"), NB("LIGHT TAXI"))));

            L.Add(R("lights_complete", "Lights", "外部灯齐全", 20, LedColor.Green, "solid", 1.0,
                    And(B("LIGHT NAV"), B("LIGHT BEACON"), B("LIGHT STROBE"))));

            L.Add(R("lights_land_taxi", "Lights", "着陆 / 滑行灯已开", 30, LedColor.Green, "blink", 1.2,
                    Or(B("LIGHT LANDING"), B("LIGHT TAXI"))));

            L.Add(R("lights_incomplete", "Lights", "灯不齐全", 40, LedColor.Amber, "solid", 1.0,
                    And(Or(B("LIGHT NAV"), B("LIGHT BEACON"), B("LIGHT STROBE"),
                           B("LIGHT LANDING"), B("LIGHT TAXI")),
                        Or(NB("LIGHT NAV"), NB("LIGHT BEACON")))));

            // ============================ 自动驾驶（P6，4 条） ============================
            L.Add(R("ap_off", "Autopilot", "AP 关闭", 10, LedColor.Off, "solid", 1.0,
                    NB("AUTOPILOT MASTER")));

            L.Add(R("ap_on", "Autopilot", "AP 接通", 20, LedColor.Green, "solid", 1.0,
                    B("AUTOPILOT MASTER")));

            L.Add(R("ap_approach", "Autopilot", "进近 / 背航道", 30, LedColor.Green, "blink", 0.8,
                    Or(B("AUTOPILOT APPROACH HOLD"), B("AUTOPILOT BACKCOURSE HOLD"))));

            // 写成“七个模式都为假”而不是“非(任一为真)”：De Morgan 展开后结构更扁，
            // 用户打开编辑器看到的是 8 个并列条件，比一层套一层好改。
            L.Add(R("ap_no_mode", "Autopilot", "接通但无模式", 40, LedColor.Amber, "flash", 1.5,
                    And(B("AUTOPILOT MASTER"),
                        NB("AUTOPILOT HEADING LOCK"), NB("AUTOPILOT ALTITUDE LOCK"),
                        NB("AUTOPILOT NAV1 LOCK"), NB("AUTOPILOT APPROACH HOLD"),
                        NB("AUTOPILOT AIRSPEED HOLD"), NB("AUTOPILOT VERTICAL HOLD"),
                        NB("AUTOPILOT FLIGHT LEVEL CHANGE"))));

            // ============================ 告警总灯（P7，11 条） ============================
            var mc = new List<AlertRule>();

            mc.Add(R("adv_wind", "MasterCaution", "大风提示", 15, LedColor.Amber, "double", 1.0,
                    N("AMBIENT WIND VELOCITY", ">", 30),
                    optional: true, sample: true));

            mc.Add(R("adv_ice_risk", "MasterCaution", "低温结冰风险", 16, LedColor.Amber, "double", 1.0,
                    And(N("AMBIENT TEMPERATURE", "<", 3), N("AMBIENT PRECIP STATE", ">", 0)),
                    optional: true, sample: true));

            mc.Add(R("ice_structural", "MasterCaution", "结构结冰", 20, LedColor.Amber, "heartbeat", 1.0,
                    N("STRUCTURAL ICE PCT", ">", 0.02)));

            mc.Add(R("ice_no_deice", "MasterCaution", "结冰且未开除冰", 30, LedColor.Amber, "blink", 1.5,
                    And(N("STRUCTURAL ICE PCT", ">", 0.02), NB("STRUCTURAL DEICE SWITCH"))));

            mc.Add(R("ice_pitot", "MasterCaution", "皮托管结冰且未加热", 40, LedColor.Amber, "blink", 1.5,
                    And(N("PITOT ICE PCT", ">", 0.02), NB("PITOT HEAT"))));

            // 1.3.46：加热已开但冰没化。原「皮托管结冰且未加热」一开加热就灭灯，
            // 之后冰有没有化开完全看不见 —— 加热失效是最容易被忽略的那种。与上一条互斥。
            mc.Add(R("ice_pitot_fail", "MasterCaution", "皮托管加热已开仍结冰", 45, LedColor.Amber, "blink", 1.5,
                    And(N("PITOT ICE PCT", ">", 0.02), B("PITOT HEAT"))));

            mc.Add(R("canopy_open", "MasterCaution", "座舱盖未关（空中）", 50, LedColor.Amber, "flash", 1.5,
                    And(B("CANOPY OPEN"), NB("SIM ON GROUND"))));

            mc.Add(R("brake_park_to", "MasterCaution", "起飞时驻车刹车未松", 60, LedColor.Amber, "flash", 2.0,
                    And(B("SIM ON GROUND"),
                        B("BRAKE PARKING POSITION"),
                        N(Derived.MaxThrottle, ">", 60))));

            mc.Add(R("stall", "MasterCaution", "失速告警", 70, LedColor.Red, "flash", 2.5,
                    B("STALL WARNING")));

            mc.Add(R("overspeed", "MasterCaution", "超速告警", 80, LedColor.Red, "flash", 2.5,
                    B("OVERSPEED WARNING")));

            // 兜底规则：条件 = 本组其它规则全不成立。排在本组最前，用来解释“为什么这颗灯是灭的”。
            // 它不产生灯光（Fallback），引擎会跳过；保留它是为了让表格每一行都能自解释。
            var mcOthers = new List<CondNode>();
            foreach (var r in mc) mcOthers.Add(r.DefaultCond.Clone());
            L.Add(R("mc_clear", "MasterCaution", "无告警", 10, LedColor.Off, "solid", 1.0,
                    Cond.Not(Cond.Or(mcOthers.ToArray())), fallback: true));
            L.AddRange(mc);

            // ============================ 燃油（P8，5 条） ============================
            L.Add(R("fuel_na", "Fuel", "油量数据不可用", 10, LedColor.Off, "solid", 1.0,
                    N("FUEL TOTAL CAPACITY", "<=", 0)));

            // 油量三档用固定数（偏低 30% / 危急 5%），与机型脱钩：这两个数不是机型能力，
            // 是“什么时候提醒我”的操作门槛，MSFS 不提供，按机型分档只是把主观值换了个写法。
            L.Add(R("fuel_ok", "Fuel", "油量充足", 20, LedColor.Green, "solid", 1.0,
                    N(Derived.FuelRatio, ">", 0.30)));

            L.Add(R("fuel_low", "Fuel", "油量偏低", 30, LedColor.Amber, "solid", 1.0,
                    And(N(Derived.FuelRatio, ">", 0.05),
                        N(Derived.FuelRatio, "<=", 0.30))));

            L.Add(R("fuel_critical", "Fuel", "油量低", 40, LedColor.Red, "solid", 1.0,
                    And(N(Derived.FuelRatio, ">", 0.02),
                        N(Derived.FuelRatio, "<=", 0.05))));

            L.Add(R("fuel_min", "Fuel", "油量极低", 50, LedColor.Red, "flash", 2.0,
                    N(Derived.FuelRatio, "<=", 0.02)));

            // ============================ 特种装置（Special，7 条，全默认停用） ============================
            // 这些规则用到的变量都是 SimConnect 能直读、单位明确的（已收或本版新增）。
            // 偏门装置不是每架飞机都有：整组标 optional 默认停用，且系统默认不上灯。
            // 用户把「特种装置」挂到某颗灯后，再按机型逐条挑着启用；未应答的量三态为 Unknown，不会误亮。
            L.Add(R("sp_parking", "Special", "已停入机位", 10, LedColor.Green, "solid", 1.0,
                    B("PLANE IN PARKING STATE"), optional: true));

            // 1.3.45：优先级 20 → 45。原来排在"尾轮放下"(30)、"牵引连接"(40) 之后，
            // 而那两条是绿灯——结果"尾勾放下"这个需要留意的琥珀状态，被两条正常状态压住。
            // 抬到 45：仍低于"拖动/推移"(50) 与"超红线"(60)，顺序语义不变，只是不再被绿压。
            L.Add(R("sp_hook", "Special", "尾勾放下", 45, LedColor.Amber, "solid", 1.0,
                    N("TAILHOOK POSITION", ">", 50), optional: true));

            L.Add(R("sp_tailwheel", "Special", "尾轮放下", 30, LedColor.Green, "solid", 1.0,
                    N("GEAR TAIL POSITION", ">", 50), optional: true));

            // 1.3.46：起飞滑跑没锁尾轮 = 方向会跑偏。
            // **仅后三点机型有意义**。注意 TAILWHEEL LOCK ON 在没这功能的机型上返回 0（假），
            // 不是"无应答"，三态兜不住 —— 所以规则名里写明适用机型，并保持默认停用，
            // 由用户只在后三点机上自己开。别把它改成默认启用。
            // 优先级 55：必须压过「牵引连接」40（绿，否则琥珀被绿压住）与「拖动/推移」50
            // （双闪比快闪缓），又要低于「超红线」60（红 flash 是更严重的状态）。这是体检当场报出来的。
            L.Add(R("sp_tail_lock", "Special", "起飞时尾轮未锁（仅后三点）", 55, LedColor.Amber, "flash", 2.0,
                    And(B("SIM ON GROUND"), N(Derived.MaxThrottle, ">", 60), NB("TAILWHEEL LOCK ON")),
                    optional: true));

            L.Add(R("sp_tow", "Special", "牵引连接", 40, LedColor.Green, "blink", 1.0,
                    B("TOW CONNECTION"), optional: true));

            L.Add(R("sp_slew", "Special", "拖动 / 推移模式", 50, LedColor.Amber, "double", 1.0,
                    B("IS SLEW ACTIVE"), optional: true));

            // 红线空速是官方按机型算好的限值，用变量对变量比较，不必按机型手填阈值
            L.Add(R("sp_redline", "Special", "超红线空速", 60, LedColor.Red, "flash", 2.0,
                    Cond.V("AIRSPEED INDICATED", ">", "AIRSPEED BARBER POLE"), optional: true));

            return L;
        }
    }
}
