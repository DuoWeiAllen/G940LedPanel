using System;
using System.Collections.Generic;

namespace G940LedPanel.Core
{
    /// <summary>一个相机状态值在判定里的归属。只有这三种，没有第四种。</summary>
    public enum CamPhase
    {
        /// <summary>实际飞行画面（座舱、追尾、外部…）→ 点灯。</summary>
        Flight = 0,

        /// <summary>稳定的菜单画面（主菜单、世界地图、机库…）→ 等待。</summary>
        Menu = 1,

        /// <summary>过渡帧：画面正在切换、还没落到确定状态 → 不改判定，画面不动。</summary>
        Skip = 2,
    }

    /// <summary>
    /// 用 MSFS 的「相机状态」（SimVar <c>CAMERA STATE</c>）判断用户现在是在飞，还是在菜单。
    ///
    /// 为什么只能靠它：
    ///   MSFS 在菜单里照样推送全套飞行数据 —— 那是机库里摆着的那架默认飞机，
    ///   在地面、发动机停车、速度 0，从数值上和"落地停稳"完全分不开。所以菜单里的乱闪
    ///   不是数据错，是数据真但没意义；只能靠"用户此刻在看什么画面"这一状态量来分辨。
    ///
    /// 为什么不用 SimConnect 的 "Sim" 状态 / SimStart 事件：
    ///   MSFS2024 在菜单里也回 Sim=1、也不发 SimStop（Asobo 已知问题）。实测日志印证：
    ///   程序连上后 17 毫秒就答 1，而当时画面正是主菜单。这条路已证伪。
    ///
    /// 为什么表外一律判"在飞"：
    ///   判错方向代价不对称 —— 把在飞误判成菜单 = 正常飞行熄灯（最严重）；
    ///   把菜单误判成在飞 = 菜单里继续闪（碍眼但安全）。所以"认不出来就放行"。
    ///
    /// 这张表只有一份，存在 config.json 的 <c>Sim.CameraStates</c> 里；
    /// 下面的 <see cref="_defaults"/> 只在配置里没有这张表时用来生成初始内容，
    /// 不参与运行期合并 —— 没有"内置 + 追加 + 覆盖"三层，也没有优先级。
    /// </summary>
    public static class CameraStateJudge
    {
        /// <summary>判据用的变量名（与 <see cref="SimVars"/> 里那条内部变量一致）。</summary>
        public const string VarName = "CAMERA STATE";

        /// <summary>配置里三个分类词（大小写不敏感）。</summary>
        public const string KeyMenu = "Menu";
        public const string KeySkip = "Skip";
        public const string KeyFlight = "Flight";

        /// <summary>相机状态是 0 起的整数枚举，给宽一点，只挡明显的手误。</summary>
        public const int MinState = 0;
        public const int MaxState = 255;

        /// <summary>
        /// 默认分类。写进新生成的配置文件；配置里已有这张表时，一个字都不改用户的。
        /// 加一行 = 把一个新见到的场景值归位，注释写清依据（实测比文档重要）。
        /// </summary>
        private static readonly KeyValuePair<int, CamPhase>[] _defaults =
        {
            new KeyValuePair<int, CamPhase>(10, CamPhase.Menu),   // 世界地图
            new KeyValuePair<int, CamPhase>(11, CamPhase.Menu),   // 机库·实时背景
            new KeyValuePair<int, CamPhase>(12, CamPhase.Menu),   // 机库·自定义
            new KeyValuePair<int, CamPhase>(13, CamPhase.Menu),   // 菜单·实时背景
            // 16：文档写 "Drone Developer"，原先按"进座舱前的加载机位"判等待，现改判在飞。
            // 表外一律 Flight，所以这里不再列它；代价只是加载那几秒可能亮一下，
            // 比"无人机视角下长时间被当成菜单、一直等待"轻得多。
            new KeyValuePair<int, CamPhase>(19, CamPhase.Menu),   // 机库
            // 29：实测是"进出游戏暂停菜单"的双向过渡帧（按 Esc 出去、回来都经过它），
            // 每次只 1 帧。它不是菜单画面，所以不判等待，而是跳过 —— 判定不动，画面就不动。
            new KeyValuePair<int, CamPhase>(29, CamPhase.Skip),   // 空闲：进出暂停菜单的过渡帧（实测）
            new KeyValuePair<int, CamPhase>(30, CamPhase.Menu),   // 世界地图·空闲
            new KeyValuePair<int, CamPhase>(31, CamPhase.Menu),   // 世界地图·探索航线
            new KeyValuePair<int, CamPhase>(32, CamPhase.Menu),   // 聚焦地点
            new KeyValuePair<int, CamPhase>(33, CamPhase.Menu),   // 世界地图·过渡
            // 35：SDK 文档的枚举到 33 为止，34/35 是 2024 实际会给出的值（文档没跟上）。
            // 实测 35 出现 5 次、每次 1～4 秒，之后一律进 30/32/12（菜单画面），从没进过座舱
            // —— 它是菜单之间的过渡机位。34 则与座舱交替、单段 9～19 秒，判为在飞，不动。
            new KeyValuePair<int, CamPhase>(35, CamPhase.Menu),   // 菜单之间的过渡机位（实测）
        };

        /// <summary>
        /// 已知值的含义，只用于日志措辞（"相机状态 10 世界地图 → 等待"）和说明书，
        /// <b>不参与任何判定</b>。这里放的是"这个数字代表什么"，不是"它算哪一边"。
        /// </summary>
        private static readonly Dictionary<int, string> _meaning = new Dictionary<int, string>
        {
            { 2,  "座舱" },
            { 3,  "追尾" },
            { 6,  "Gameplay" },
            { 10, "世界地图" },
            { 11, "机库·实时背景" },
            { 12, "机库·自定义" },
            { 13, "菜单·实时背景" },
            { 16, "Drone Developer / 进座舱前的加载机位（实测判在飞）" },
            { 19, "机库" },
            { 27, "Transition（文档；未实测）" },
            { 29, "空闲：进出暂停菜单的过渡帧（实测）" },
            { 30, "世界地图·空闲" },
            { 31, "世界地图·探索航线" },
            { 32, "聚焦地点" },
            { 33, "世界地图·过渡" },
            { 34, "座舱与菜单之间的画面（文档外，实测判在飞）" },
            { 35, "菜单之间的过渡机位（文档外，实测）" },
        };

        // ---------------------------------------------------------------- 默认表

        /// <summary>默认分类（值 → 三态）。程序自己用，也给断言用。</summary>
        public static Dictionary<int, CamPhase> DefaultPhases()
        {
            var d = new Dictionary<int, CamPhase>();
            for (int i = 0; i < _defaults.Length; i++) d[_defaults[i].Key] = _defaults[i].Value;
            return d;
        }

        /// <summary>三态表 → 配置形态（值 → 分类词），按键升序，便于人读。</summary>
        public static Dictionary<string, string> ToConfigTable(Dictionary<int, CamPhase> table)
        {
            var d = new Dictionary<string, string>();
            if (table == null) return d;

            var keys = new List<int>(table.Keys);
            keys.Sort();
            foreach (int k in keys) d[k.ToString()] = Word(table[k]);
            return d;
        }

        /// <summary>三态 → 配置里的词。</summary>
        public static string Word(CamPhase p)
        {
            if (p == CamPhase.Menu) return KeyMenu;
            if (p == CamPhase.Skip) return KeySkip;
            return KeyFlight;
        }

        /// <summary>配置里的词 → 三态。认大小写，认不出返回 false。</summary>
        public static bool TryParseWord(string s, out CamPhase p)
        {
            p = CamPhase.Flight;
            if (s == null) return false;
            string t = s.Trim();
            if (string.Equals(t, KeyMenu, StringComparison.OrdinalIgnoreCase)) { p = CamPhase.Menu; return true; }
            if (string.Equals(t, KeySkip, StringComparison.OrdinalIgnoreCase)) { p = CamPhase.Skip; return true; }
            if (string.Equals(t, KeyFlight, StringComparison.OrdinalIgnoreCase)) { p = CamPhase.Flight; return true; }
            return false;
        }

        // ---------------------------------------------------------------- 接线

        /// <summary>
        /// 把配置里的表解析成三态表（纯函数，可离线穷举）。
        /// <para><paramref name="raw"/> 为 null（配置里没有这一项）→ 用默认表；
        /// 为空表（用户写成 <c>{}</c>）→ 空表，即全部按在飞处理 —— 这是用户的明确选择。</para>
        /// <para>键不是 0～255 的整数、值不是三个词之一，一律丢弃并计入 <paramref name="dropped"/>。
        /// 配错方向的代价不对称（把在飞值写成 Menu = 飞行中熄灯），所以宁可少认。</para>
        /// </summary>
        public static Dictionary<int, CamPhase> Build(IDictionary<string, string> raw, out int dropped)
        {
            dropped = 0;
            var table = new Dictionary<int, CamPhase>();

            if (raw == null) return DefaultPhases();   // 字段缺失：给默认全套

            foreach (var kv in raw)
            {
                int v;
                if (!TryParseState(kv.Key, out v)) { dropped++; continue; }

                CamPhase p;
                if (!TryParseWord(kv.Value, out p)) { dropped++; continue; }

                if (table.ContainsKey(v)) { dropped++; continue; }   // 同键重复（JSON 里手写两次）
                table[v] = p;
            }
            return table;
        }

        /// <summary>把旧的 <c>ExtraMenuCameraStates</c>（一串"算菜单"的值）并进表，用于升级兼容。</summary>
        public static void MergeLegacyMenuValues(Dictionary<int, CamPhase> table, IEnumerable<int> legacy)
        {
            if (table == null || legacy == null) return;
            foreach (int v in legacy)
                if (v >= MinState && v <= MaxState) table[v] = CamPhase.Menu;
        }

        /// <summary>这个相机状态属于哪一边。取不到值、非整数、表外值 → 在飞（放行）。</summary>
        public static CamPhase Classify(double cameraState, IDictionary<int, CamPhase> table)
        {
            int v;
            if (!TryToInt(cameraState, out v)) return CamPhase.Flight;
            CamPhase p;
            if (table != null && table.TryGetValue(v, out p)) return p;
            return CamPhase.Flight;
        }

        /// <summary>给日志用的说明，一眼看出"值、含义、判成什么、是不是自己配的"。</summary>
        public static string Describe(double cameraState, IDictionary<int, CamPhase> table)
        {
            int v;
            if (!TryToInt(cameraState, out v))
                return "相机状态 " + cameraState + "（非整数，不判）→ 放行";

            CamPhase p = Classify(v, table);

            string name;
            string tag = "";
            if (!_meaning.TryGetValue(v, out name))
            {
                name = "";
                if (table != null && table.ContainsKey(v)) tag = "（配置）";
            }

            string body = name.Length > 0 ? (v + " " + name + tag) : (v.ToString() + tag);

            if (p == CamPhase.Menu) return "相机状态 " + body + " → 等待";
            if (p == CamPhase.Skip) return "相机状态 " + body + " → 跳过（判定不动）";
            return "相机状态 " + body + " → 在飞";
        }

        // ---------------------------------------------------------------- 内部

        private static bool TryParseState(string s, out int v)
        {
            v = 0;
            if (string.IsNullOrWhiteSpace(s)) return false;
            if (!int.TryParse(s.Trim(), out v)) return false;
            return v >= MinState && v <= MaxState;
        }

        private static bool TryToInt(double value, out int result)
        {
            result = 0;
            if (double.IsNaN(value) || double.IsInfinity(value)) return false;
            if (value < int.MinValue || value > int.MaxValue) return false;
            int v = (int)Math.Round(value);
            if (Math.Abs(value - v) > 1e-6) return false;
            result = v;
            return true;
        }
    }
}
