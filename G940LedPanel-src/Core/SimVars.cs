using System;
using System.Collections.Generic;

namespace G940LedPanel.Core
{
    /// <summary>
    /// 从 MSFS2024 SimConnect 读取的变量清单（全部为数值型，统一按 FLOAT64 接收）。
    /// 每个变量单独注册一个 DataDefinition 并单独请求，因此某个变量名不被模拟器识别时
    /// 只会导致该变量无数据，不会让其它变量的字节偏移错位。
    /// 单位一律使用 SimConnect 单位串，由模拟器负责换算。
    /// </summary>
    public static class SimVars
    {
        public sealed class Var
        {
            public readonly string Name;   // SimConnect 变量名
            public readonly string Unit;   // 单位串
            public readonly string Label;  // 中文短名（英文界面走 LabelOf 回退到 Name）
            /// <summary>
            /// 分组：存的是语言包 key（<c>grp.flight</c> 这种），不是中文名。
            ///
            /// 为什么存 key 不存中文（1.3.59）：分类逻辑只拿它做相等比较
            /// （<c>v.Group != g</c> 归组），显示时才要文字。存 key 之后，
            /// 中文界面 <c>L.T("grp.flight")</c> 得「飞行」，英文界面得「Flight」，
            /// 分类代码一行都不用改。分组一共 10 个，翻 10 条就够。
            /// </summary>
            public readonly string Group;
            /// <summary>程序自用变量：照常读取、照常出现在数据监视表，但不进条件编辑器的候选列表。</summary>
            public readonly bool Internal;
            /// <summary>
            /// 是否「官方判定 / 机型能力门 / 按机型算好的限值」量。只用于界面加一个来源标签，
            /// 不参与任何判定，也不影响采集。内置规则一条都不引用这些量，它们只是给用户组条件的原料。
            ///
            /// 原来这里是个 string Source，取值为 "官方" 或空，靠 `Source == "官方"` 反推真假
            /// （1.3.59 改成 bool）。改的理由：DocumentLint 有一条「不许拿显示名做判断」，
            /// 而语言包里一旦有了「官方」这条译文，那个比较就会被判为拿显示名当身份。
            /// 这个字段从来没被当字符串显示过，只有真假两种状态，直接用 bool 是它本来的样子。
            /// </summary>
            public readonly bool Official;

            public Var(string name, string unit, string label, string group,
                       bool isInternal = false, bool official = false)
            {
                Name = name; Unit = unit; Label = label; Group = group;
                Internal = isInternal; Official = official;
            }

            public override string ToString() { return Label + " (" + Name + ")"; }
        }

        private static readonly Var[] _all =
        {
            // ---------------- 飞行 / 姿态 ----------------
            new Var("AIRSPEED INDICATED", "knots", "指示空速", "grp.flight"),
            new Var("AIRSPEED TRUE", "knots", "真空速", "grp.flight"),
            new Var("GROUND VELOCITY", "knots", "地速", "grp.flight"),
            new Var("AIRSPEED MACH", "mach", "马赫数", "grp.flight"),
            new Var("PLANE ALTITUDE", "feet", "气压高度", "grp.flight"),
            new Var("PLANE ALT ABOVE GROUND", "feet", "离地高度", "grp.flight"),
            new Var("VERTICAL SPEED", "feet per minute", "升降率", "grp.flight"),
            new Var("PLANE PITCH DEGREES", "degrees", "俯仰角", "grp.flight"),
            new Var("PLANE BANK DEGREES", "degrees", "滚转角", "grp.flight"),
            new Var("PLANE HEADING DEGREES MAGNETIC", "degrees", "磁航向", "grp.flight"),
            new Var("G FORCE", "gforce", "G 力", "grp.flight"),
            new Var("SIM ON GROUND", "bool", "在地面", "grp.flight"),
            new Var("TOTAL WEIGHT", "pounds", "总重", "grp.flight"),

            // ---------------- 告警 / 环境 ----------------
            new Var("STALL WARNING", "bool", "失速告警", "grp.warn"),
            new Var("OVERSPEED WARNING", "bool", "超速告警", "grp.warn"),
            new Var("AMBIENT WIND VELOCITY", "knots", "风速", "grp.warn"),
            new Var("AMBIENT TEMPERATURE", "celsius", "外界温度", "grp.warn"),
            new Var("AMBIENT PRECIP STATE", "number", "降水状态", "grp.warn"),
            new Var("STRUCTURAL ICE PCT", "percent over 100", "结构结冰", "grp.warn"),
            new Var("KOHLSMAN SETTING HG", "inches of mercury", "高度表拨正", "grp.warn"),

            // ---------------- 发动机 ----------------
            new Var("NUMBER OF ENGINES", "number", "发动机数量", "grp.engine"),
            new Var("GENERAL ENG THROTTLE LEVER POSITION:1", "percent", "1 号油门", "grp.engine"),
            new Var("GENERAL ENG THROTTLE LEVER POSITION:2", "percent", "2 号油门", "grp.engine"),
            new Var("GENERAL ENG THROTTLE LEVER POSITION:3", "percent", "3 号油门", "grp.engine"),
            new Var("GENERAL ENG THROTTLE LEVER POSITION:4", "percent", "4 号油门", "grp.engine"),
            new Var("GENERAL ENG COMBUSTION:1", "bool", "1 号燃烧", "grp.engine"),
            new Var("GENERAL ENG COMBUSTION:2", "bool", "2 号燃烧", "grp.engine"),
            new Var("GENERAL ENG COMBUSTION:3", "bool", "3 号燃烧", "grp.engine"),
            new Var("GENERAL ENG COMBUSTION:4", "bool", "4 号燃烧", "grp.engine"),
            new Var("GENERAL ENG RPM:1", "rpm", "1 号转速", "grp.engine"),
            new Var("GENERAL ENG RPM:2", "rpm", "2 号转速", "grp.engine"),
            new Var("GENERAL ENG RPM:3", "rpm", "3 号转速", "grp.engine"),
            new Var("GENERAL ENG RPM:4", "rpm", "4 号转速", "grp.engine"),
            new Var("ENG ON FIRE:1", "bool", "1 号着火", "grp.engine"),
            new Var("ENG ON FIRE:2", "bool", "2 号着火", "grp.engine"),
            new Var("ENG ON FIRE:3", "bool", "3 号着火", "grp.engine"),
            new Var("ENG ON FIRE:4", "bool", "4 号着火", "grp.engine"),
            new Var("ENG ANTI ICE:1", "bool", "1 号发动机防冰", "grp.engine"),
            new Var("ENG ANTI ICE:2", "bool", "2 号发动机防冰", "grp.engine"),
            new Var("ENG ANTI ICE:3", "bool", "3 号发动机防冰", "grp.engine"),
            new Var("ENG ANTI ICE:4", "bool", "4 号发动机防冰", "grp.engine"),

            // ---------------- 系统开关 ----------------
            new Var("ELECTRICAL MASTER BATTERY", "bool", "电瓶", "grp.system"),
            new Var("AVIONICS MASTER SWITCH", "bool", "航电总开关", "grp.system"),
            new Var("PITOT HEAT", "bool", "皮托管加热", "grp.system"),
            new Var("STRUCTURAL DEICE SWITCH", "bool", "结构除冰", "grp.system"),
            new Var("BRAKE PARKING POSITION", "bool", "驻车刹车", "grp.system"),
            new Var("CANOPY OPEN", "bool", "座舱盖打开", "grp.system"),
            new Var("TAILHOOK POSITION", "percent", "尾钩位置", "grp.system"),
            new Var("BRAKE PARKING INDICATOR", "bool", "驻车刹车指示", "grp.system"),
            new Var("AUTO BRAKE SWITCH CB", "number", "自动刹车档位", "grp.system"),
            new Var("PRESSURIZATION DUMP SWITCH", "bool", "增压泄压开关", "grp.system"),
            new Var("WATER RUDDER HANDLE POSITION", "percent", "水舵手柄", "grp.system"),
            new Var("LIGHT ON STATES", "number", "灯光状态掩码", "grp.system"),

            // ---------------- 起落架 ----------------
            new Var("IS GEAR RETRACTABLE", "bool", "起落架可收放", "grp.gear"),
            new Var("GEAR HANDLE POSITION", "bool", "起落架手柄放下", "grp.gear"),
            new Var("GEAR TOTAL PCT EXTENDED", "percent", "起落架总伸出", "grp.gear"),
            new Var("GEAR LEFT POSITION", "percent", "左主起落架", "grp.gear"),
            new Var("GEAR RIGHT POSITION", "percent", "右主起落架", "grp.gear"),
            new Var("GEAR CENTER POSITION", "percent", "前起落架", "grp.gear"),
            new Var("GEAR TAIL POSITION", "percent", "尾轮", "grp.gear"),
            // 后三点才有：尾轮锁。机型没这功能时该值恒 0（不是无应答），所以不能靠三态兜底，
            // 引用它的规则必须自己写清适用机型（见 AlertRuleCatalog.sp_tail_lock）。
            new Var("TAILWHEEL LOCK ON", "bool", "尾轮锁", "grp.gear"),
            // 水上飞机浮筒：1.3.46 加进原料池，只为让用户能自建规则，内置一条都不引用。
            new Var("RETRACT LEFT FLOAT EXTENDED", "percent", "左浮筒", "grp.gear"),
            new Var("RETRACT RIGHT FLOAT EXTENDED", "percent", "右浮筒", "grp.gear"),
            new Var("PITOT ICE PCT", "percent over 100", "皮托管结冰", "grp.warn"),

            // ---------------- 襟翼 / 扰流板 ----------------
            new Var("FLAPS HANDLE PERCENT", "percent", "襟翼手柄位置", "grp.flap"),
            new Var("FLAPS HANDLE INDEX", "number", "襟翼档位", "grp.flap"),
            new Var("FLAPS NUM HANDLE POSITIONS", "number", "襟翼档位数", "grp.flap"),
            new Var("SPOILERS HANDLE POSITION", "percent", "扰流板手柄", "grp.flap"),
            new Var("SPOILERS ARMED", "bool", "扰流板预位", "grp.flap"),

            // ---------------- 灯光 ----------------
            new Var("LIGHT NAV", "bool", "航行灯", "grp.light"),
            new Var("LIGHT BEACON", "bool", "防撞灯", "grp.light"),
            new Var("LIGHT LANDING", "bool", "着陆灯", "grp.light"),
            new Var("LIGHT TAXI", "bool", "滑行灯", "grp.light"),
            new Var("LIGHT STROBE", "bool", "频闪灯", "grp.light"),
            new Var("LIGHT PANEL", "bool", "仪表灯", "grp.light"),
            new Var("LIGHT RECOGNITION", "bool", "识别灯", "grp.light"),
            new Var("LIGHT WING", "bool", "机翼灯", "grp.light"),
            new Var("LIGHT LOGO", "bool", "标识灯", "grp.light"),
            new Var("LIGHT CABIN", "bool", "客舱灯", "grp.light"),

            // ---------------- 自动驾驶 ----------------
            new Var("AUTOPILOT AVAILABLE", "bool", "AP 可用", "grp.ap"),
            new Var("AUTOPILOT MASTER", "bool", "AP 主开关", "grp.ap"),
            new Var("AUTOPILOT HEADING LOCK", "bool", "AP 航向", "grp.ap"),
            new Var("AUTOPILOT APPROACH HOLD", "bool", "AP 进近", "grp.ap"),
            new Var("AUTOPILOT BACKCOURSE HOLD", "bool", "AP 背航道", "grp.ap"),
            new Var("AUTOPILOT ALTITUDE LOCK", "bool", "AP 高度保持", "grp.ap"),
            new Var("AUTOPILOT NAV1 LOCK", "bool", "AP NAV1", "grp.ap"),
            new Var("AUTOPILOT AIRSPEED HOLD", "bool", "AP 空速保持", "grp.ap"),
            new Var("AUTOPILOT VERTICAL HOLD", "bool", "AP 垂直速度", "grp.ap"),
            new Var("AUTOPILOT FLIGHT LEVEL CHANGE", "bool", "AP 高度层改变", "grp.ap"),

            // ---------------- 燃油 ----------------
            new Var("FUEL TOTAL QUANTITY", "gallons", "总油量", "grp.fuel"),
            new Var("FUEL TOTAL CAPACITY", "gallons", "油箱总容量", "grp.fuel"),

            // ---------------- 扩展原料（1.3.42）----------------
            // 这一整段是「原料池」：只供用户在条件编辑器里自己组规则，内置规则一条都不引用。
            // 名称与单位按 MSFS2024 官方文档逐条核实；标「官方」的是模拟器已判完的结论 /
            // 机型能力门 / 按机型算好的限值，界面会给出处标签，但与现有内置规则并存、不替换。
            // 排除：Gauge API 的 Token Vars（2024 已弃用、SimConnect 读不到）、机型自定义 L: 量。
            //
            // -- 官方：判定结果（模拟器判完，值就是成立与否）--
            new Var("GEAR SPEED EXCEEDED", "bool", "起落架超速", "grp.gear", false, true),
            new Var("GEAR DAMAGE BY SPEED", "bool", "起落架超速损坏", "grp.gear", false, true),
            new Var("FLAP SPEED EXCEEDED", "bool", "襟翼超速", "grp.flap", false, true),
            new Var("FLAP DAMAGE BY SPEED", "bool", "襟翼超速损坏", "grp.flap", false, true),
            new Var("ENG FAILED:1", "bool", "1 号发动机失效", "grp.engine", false, true),
            new Var("ENG FAILED:2", "bool", "2 号发动机失效", "grp.engine", false, true),
            new Var("ENG FAILED:3", "bool", "3 号发动机失效", "grp.engine", false, true),
            new Var("ENG FAILED:4", "bool", "4 号发动机失效", "grp.engine", false, true),
            // -- 官方：机型能力门 / 按机型算好的限值 --
            new Var("FLAPS AVAILABLE", "bool", "襟翼可用", "grp.flap", false, true),
            new Var("SPOILER AVAILABLE", "bool", "扰流板可用", "grp.flap", false, true),
            new Var("AIRSPEED BARBER POLE", "knots", "红线空速", "grp.flight", false, true),
            new Var("BARBER POLE MACH", "mach", "红线马赫", "grp.flight", false, true),
            // -- 普通状态量（座舱 / 地面 / 特种装置，供用户自建规则）--
            new Var("PLANE IN PARKING STATE", "bool", "已停入机位", "grp.system"),
            new Var("TOW CONNECTION", "bool", "牵引连接", "grp.system"),
            new Var("IS SLEW ACTIVE", "bool", "拖动模式", "grp.system"),
            new Var("IS SLEW ALLOWED", "bool", "允许拖动", "grp.system"),
            new Var("IS USER SIM", "bool", "玩家机", "grp.system"),
            new Var("ON ANY RUNWAY", "bool", "在跑道上", "grp.system"),
            new Var("SURFACE CONDITION", "number", "道面状况（枚举）", "grp.system"),
            new Var("SURFACE INFO VALID", "bool", "道面信息有效", "grp.system"),
            new Var("AMBIENT IN CLOUD", "bool", "云中", "grp.warn"),
            new Var("SPOILERS LEFT POSITION", "percent over 100", "左扰流板位置", "grp.flap"),
            new Var("SPOILERS RIGHT POSITION", "percent over 100", "右扰流板位置", "grp.flap"),

            // ---------------- 内部（程序自用，不进条件编辑器） ----------------
            // 相机状态不参与告警判定，只回答一个问题：用户现在是在飞，还是在菜单/机库/世界地图。
            // 为什么不拿飞行数据本身去猜：菜单里那架默认飞机是在地面停着的真数据，
            // 从数值上和"落地停稳"完全分不开，只能靠这个状态量。详见 CameraStateJudge。
            new Var("CAMERA STATE", "number", "相机状态", "grp.internal", true),
        };

        public static Var[] All { get { return _all; } }
        public static int Count { get { return _all.Length; } }

        private static readonly Dictionary<string, int> _index = BuildIndex();

        private static Dictionary<string, int> BuildIndex()
        {
            var d = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _all.Length; i++) d[_all[i].Name] = i;
            return d;
        }

        /// <summary>按变量名取下标；不存在返回 -1。</summary>
        public static int IndexOf(string name)
        {
            int i;
            return (name != null && _index.TryGetValue(name, out i)) ? i : -1;
        }

        /// <summary>
        /// 变量的显示名。
        ///
        /// 英文界面下直接返回 SimConnect 代号（<c>AIRSPEED INDICATED</c>）——它本来就是英文，
        /// 再翻一份英文标签属于纯重复劳动。中文界面返回中文短名。
        ///
        /// 为什么不在字段上分中英两份（1.3.59）：真正显示出来的地方全部走这个方法
        /// （条件树、变量下拉、手动试算表、试算表左列、主界面悬停提示），改这一处就全覆盖。
        /// 加字段的话 115 条都要写两遍，还得有人保证两份不错位。
        /// </summary>
        public static string LabelOf(string name)
        {
            int i = IndexOf(name);
            if (i < 0) return name;
            return L.Lang == L.English ? _all[i].Name : _all[i].Label;
        }

        /// <summary>变量的单位串（手动试算面板显示"填的是米还是英尺"）。</summary>
        public static string UnitOf(string name)
        {
            int i = IndexOf(name);
            return i >= 0 ? _all[i].Unit : "";
        }
    }
}
