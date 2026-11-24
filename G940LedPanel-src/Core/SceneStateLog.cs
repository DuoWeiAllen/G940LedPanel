using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace G940LedPanel.Core
{
    /// <summary>
    /// 「场景状态记录」：把每次相机状态变化和由此得出的生效判定，追加到 diag\g940-simstate.txt。
    ///
    /// 它的用途只有一个：**事后能看出"当时程序认为用户在哪个画面、依据是什么"**。
    /// 排查"菜单里灯乱闪""进出飞行慢了""某个值不在表里"这几类问题，全靠这份记录；
    /// 平时关着，需要查问题的时候在告警页勾上即可（勾选状态存配置，不用每次重勾）。
    ///
    /// 三条边界：
    /// 1. 默认关闭。开着时每一行都是一次真实的判定变化，量极小（多数时刻一行都不写）。
    /// 2. 单文件超过 2MB 时，下次启动把它改名为 g940-simstate.old.txt（只留一份旧的），
    ///    然后从新文件开始——防止忘了关把盘写满，也不做"归档目录"那套。
    /// 3. 单次会话写满 2 万行就停，并记一行"已达上限"。正常使用远远到不了。
    ///
    /// 写失败（目录只读、盘满、文件被占）一律静默放弃：诊断记录坏了不能影响点灯。
    /// </summary>
    internal static class SceneStateLog
    {
        /// <summary>文件名。刻意保持英文旧名，排查脚本认这个名字。</summary>
        public const string FileName = "g940-simstate.txt";

        private const long MaxBytes = 2L * 1024 * 1024;   // 单文件 2MB 封顶
        private const int MaxLinesPerSession = 20000;     // 单次会话 2 万行封顶

        private static readonly object _lock = new object();
        private static readonly UTF8Encoding _utf8 = new UTF8Encoding(false);

        private static int _lines;
        private static bool _stopped;
        private static bool _dirOk;

        /// <summary>总开关。由界面在启动时按配置设置，运行中勾选/取消即时生效。</summary>
        public static bool Enabled { get; set; }

        /// <summary>记录文件完整路径（界面提示用）。</summary>
        public static string FilePath
        {
            get { return DiagPaths.PathOf(FileName); }
        }

        /// <summary>
        /// 开始一段记录（每次连上模拟器调用一次），写一段表头：
        /// 启动时刻、程序版本、当时的整张相机状态表。
        ///
        /// 表头里的这张表是关键——日后拿到记录，一眼就能看出当时用的是哪套分类，
        /// 不必再回头要配置文件。文件名不变，多次启动靠这段分隔线区分。
        /// </summary>
        public static void Begin(string simVersion, IDictionary<int, CamPhase> table)
        {
            if (!Enabled) return;

            lock (_lock)
            {
                _lines = 0;
                _stopped = false;
                if (!_dirOk) _dirOk = DiagPaths.Ensure();
                if (!_dirOk) return;

                RotateIfTooBig();

                var sb = new StringBuilder();
                sb.Append("================ ")
                  .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                  .Append(" 启动 | ").Append(AppInfo.Name).Append(' ').Append(AppInfo.Title)
                  .Append(" | 模拟器 ").Append(simVersion ?? "").Append(" ================");
                AppendRaw(sb.ToString());

                AppendRaw("相机状态表（Sim.CameraStates）：" + TableText(table));
            }
        }

        /// <summary>写一行。开关关闭、已停写、写失败都直接返回，不抛异常。</summary>
        public static void Write(string msg)
        {
            if (!Enabled) return;

            lock (_lock)
            {
                if (_stopped) return;

                if (_lines >= MaxLinesPerSession)
                {
                    _stopped = true;
                    AppendRaw(DateTime.Now.ToString("HH:mm:ss.fff")
                              + "  本次记录已达 " + MaxLinesPerSession + " 行上限，停止记录（重启程序可继续）");
                    return;
                }

                if (!_dirOk) _dirOk = DiagPaths.Ensure();
                if (!_dirOk) return;

                AppendRaw(DateTime.Now.ToString("HH:mm:ss.fff") + "  " + msg);
                _lines++;
            }
        }

        /// <summary>把三态表排成 "2=Flight 10=Menu 12=Menu" 一行，按键升序，便于人读。</summary>
        private static string TableText(IDictionary<int, CamPhase> table)
        {
            if (table == null || table.Count == 0) return "（空表：全部按在飞）";

            var keys = new List<int>(table.Keys);
            keys.Sort();

            var sb = new StringBuilder();
            for (int i = 0; i < keys.Count; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(keys[i]).Append('=').Append(CameraStateJudge.Word(table[keys[i]]));
            }
            return sb.ToString();
        }

        /// <summary>
        /// 文件超过 2MB 就轮转成 .old.txt（覆盖上一份旧的），再从头写。
        /// 只留一份历史：既不会无限增长，也不至于一觉醒来"日志没了"。
        /// </summary>
        private static void RotateIfTooBig()
        {
            try
            {
                string p = FilePath;
                var fi = new FileInfo(p);
                if (!fi.Exists || fi.Length <= MaxBytes) return;

                try { File.Copy(p, DiagPaths.PathOf("g940-simstate.old.txt"), true); }
                catch (Exception) { }
                try { File.Delete(p); }
                catch (Exception) { }
            }
            catch (Exception) { }
        }

        private static void AppendRaw(string line)
        {
            try
            {
                File.AppendAllText(FilePath, line + "\r\n", _utf8);
            }
            catch (Exception) { }
        }
    }
}
