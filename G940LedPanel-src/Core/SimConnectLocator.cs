using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace G940LedPanel.Core
{
    /// <summary>
    /// 从 MSFS2024 安装目录自动定位原生 SimConnect（不随程序打包）。
    /// 注意：MSFS2024 把模拟器自带的这份 dll 改名为 <c>SimConnect_internal.dll</c>，
    /// 与 SDK 里分发的 <c>SimConnect.dll</c> 除“同进程客户端（WASM）版本号处理”外完全相同，
    /// 外部进程可以照常调用（Asobo 官方确认）。因此两个文件名都接受，优先用 SimConnect.dll。
    /// 探测顺序：exe 目录 → 环境变量 → 注册表卸载项 → XboxGames → WindowsApps → Steam 库 → 常见固定路径。
    /// 只接受 x64 版本（本程序为 x64，32 位 dll 会加载失败）。
    /// </summary>
    public static class SimConnectLocator
    {
        /// <summary>候选文件名，按优先级排列。MSFS2024 自带的是第二个。</summary>
        public static readonly string[] FileNames = { "SimConnect.dll", "SimConnect_internal.dll" };

        /// <summary>最近一次采用的 dll 文件名（供界面显示）。</summary>
        public static string UsedFileName { get; private set; }

        /// <summary>最近一次探测的完整日志（供“SDK 检测”显示）。</summary>
        public static string LastLog { get; private set; }

        public static bool TryFind(out string path, out string log)
        {
            var sb = new StringBuilder();
            path = null;

            string wrongArch = null;
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int dirBudget = 400;

            foreach (var root in CandidateRoots(sb))
            {
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) continue;
                if (!visited.Add(root)) continue;

                string hit = SearchDir(root, 3, ref dirBudget, sb, ref wrongArch);
                if (hit != null)
                {
                    path = hit;
                    UsedFileName = Path.GetFileName(hit);
                    sb.AppendLine(L.T("sl.log.adopt", hit));
                    if (string.Equals(UsedFileName, "SimConnect_internal.dll", StringComparison.OrdinalIgnoreCase))
                        sb.AppendLine(L.T("sl.log.internal"));
                    LastLog = sb.ToString();
                    log = LastLog;
                    return true;
                }
            }

            if (wrongArch != null)
                sb.AppendLine(L.T("sl.log.wrongarch", wrongArch));
            sb.AppendLine(L.T("sl.log.notfound"));
            sb.AppendLine(L.T("sl.log.notfound.hint"));
            sb.AppendLine(L.T("sl.log.notfound.env"));

            LastLog = sb.ToString();
            log = LastLog;
            return false;
        }

        private static IEnumerable<string> CandidateRoots(StringBuilder log)
        {
            // 1) 程序自身目录（含子目录）
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            log.AppendLine(L.T("sl.log.appdir", baseDir));
            yield return baseDir;
            yield return Path.Combine(baseDir, "SimConnect");
            yield return Path.Combine(baseDir, "lib");

            // 2) 环境变量
            foreach (var env in new[] { "MSFS2024_SIMCONNECT_DIR", "MSFS_SIMCONNECT_DIR", "MSFS2024_PATH", "MSFS_PATH" })
            {
                string v = null;
                try { v = Environment.GetEnvironmentVariable(env); } catch (Exception) { }
                if (!string.IsNullOrEmpty(v))
                {
                    log.AppendLine(L.T("sl.log.env", env, v));
                    yield return v;
                }
            }

            // 3) 注册表卸载项（含 Store / Steam / 独立安装器）
            foreach (var reg in RegistryRoots(log)) yield return reg;

            // 4) Xbox 应用默认安装盘
            foreach (var p in XboxGameDirs(log)) yield return p;

            // 5) WindowsApps 包目录（MSFS2024 包名 Microsoft.Limitless_*）
            foreach (var p in WindowsAppsDirs(log)) yield return p;

            // 6) Steam 库
            foreach (var p in SteamDirs(log)) yield return p;

            // 7) 常见固定路径兜底
            string[] fixedDirs =
            {
                @"C:\XboxGames\Microsoft Flight Simulator 2024\Content",
                @"C:\XboxGames\Microsoft Flight Simulator 2024\Content\Packages",
                @"D:\XboxGames\Microsoft Flight Simulator 2024\Content",
                @"E:\XboxGames\Microsoft Flight Simulator 2024\Content",
                @"C:\Program Files\Microsoft Flight Simulator 2024",
                @"C:\MSFS2024\Content",
                @"C:\MSFS 2024 SDK\SimConnect",
            };
            foreach (var p in fixedDirs)
            {
                if (Directory.Exists(p)) log.AppendLine(L.T("sl.log.fixed", p));
                yield return p;
            }
        }

        private static IEnumerable<string> RegistryRoots(StringBuilder log)
        {
            var roots = new List<string>();
            RegistryKey[] bases = { Registry.LocalMachine, Registry.CurrentUser };
            string[] subKeys =
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
            };

            foreach (var kb in bases)
            {
                foreach (var sk in subKeys)
                {
                    try
                    {
                        using (var k = kb.OpenSubKey(sk))
                        {
                            if (k == null) continue;
                            foreach (var name in k.GetSubKeyNames())
                            {
                                try
                                {
                                    using (var app = k.OpenSubKey(name))
                                    {
                                        if (app == null) continue;
                                        string dn = Convert.ToString(app.GetValue("DisplayName"));
                                        if (string.IsNullOrEmpty(dn)) continue;
                                        if (dn.IndexOf("Flight Simulator", StringComparison.OrdinalIgnoreCase) < 0) continue;
                                        string loc = Convert.ToString(app.GetValue("InstallLocation"));
                                        if (!string.IsNullOrEmpty(loc) && Directory.Exists(loc))
                                        {
                                            log.AppendLine(L.T("sl.log.registry", dn, loc));
                                            roots.Add(loc);
                                        }
                                    }
                                }
                                catch (Exception) { }
                            }
                        }
                    }
                    catch (Exception) { }
                }
            }
            return roots;
        }

        private static IEnumerable<string> XboxGameDirs(StringBuilder log)
        {
            var list = new List<string>();
            foreach (var drive in Drives())
            {
                string root = drive + @"\XboxGames";
                if (!Directory.Exists(root)) continue;
                try
                {
                    foreach (var d in Directory.GetDirectories(root))
                    {
                        string n = Path.GetFileName(d);
                        if (n.IndexOf("Flight Simulator", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            log.AppendLine(L.T("sl.log.xboxgames", d));
                            list.Add(d);
                            list.Add(Path.Combine(d, "Content"));
                        }
                    }
                }
                catch (Exception) { }
            }
            return list;
        }

        private static IEnumerable<string> WindowsAppsDirs(StringBuilder log)
        {
            var list = new List<string>();
            foreach (var drive in Drives())
            {
                string root = drive + @"\Program Files\WindowsApps";
                if (!Directory.Exists(root)) continue;
                try
                {
                    foreach (var d in Directory.GetDirectories(root, "Microsoft.Limitless_*"))
                    {
                        log.AppendLine(L.T("sl.log.winapps2024", d));
                        list.Add(d);
                    }
                }
                catch (Exception) { }
                try
                {
                    foreach (var d in Directory.GetDirectories(root, "Microsoft.FlightSimulator_*"))
                    {
                        log.AppendLine(L.T("sl.log.winapps2020", d));
                        list.Add(d);
                    }
                }
                catch (Exception) { }
            }
            return list;
        }

        private static IEnumerable<string> SteamDirs(StringBuilder log)
        {
            var list = new List<string>();
            var steamRoots = new List<string>();

            try
            {
                using (var k = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    if (k != null)
                    {
                        string p = Convert.ToString(k.GetValue("SteamPath"));
                        if (!string.IsNullOrEmpty(p)) steamRoots.Add(p.Replace('/', '\\'));
                        p = Convert.ToString(k.GetValue("InstallPath"));
                        if (!string.IsNullOrEmpty(p)) steamRoots.Add(p.Replace('/', '\\'));
                    }
                }
            }
            catch (Exception) { }

            steamRoots.Add(@"C:\Program Files (x86)\Steam");
            steamRoots.Add(@"D:\Steam");
            steamRoots.Add(@"D:\SteamLibrary");
            steamRoots.Add(@"E:\SteamLibrary");

            var libs = new List<string>();
            foreach (var s in steamRoots)
            {
                if (string.IsNullOrEmpty(s) || !Directory.Exists(s)) continue;
                if (!libs.Contains(s)) libs.Add(s);
                try
                {
                    string vdf = Path.Combine(s, @"steamapps\libraryfolders.vdf");
                    if (!File.Exists(vdf)) continue;
                    foreach (var line in File.ReadAllLines(vdf))
                    {
                        int a = line.IndexOf("\"path\"", StringComparison.OrdinalIgnoreCase);
                        if (a < 0) continue;
                        int q1 = line.IndexOf('"', a + 6);
                        if (q1 < 0) continue;
                        int q2 = line.IndexOf('"', q1 + 1);
                        if (q2 < 0) continue;
                        string p = line.Substring(q1 + 1, q2 - q1 - 1).Replace("\\\\", "\\");
                        if (Directory.Exists(p) && !libs.Contains(p)) libs.Add(p);
                    }
                }
                catch (Exception) { }
            }

            foreach (var l in libs)
            {
                foreach (var game in new[] { "Microsoft Flight Simulator 2024", "Microsoft Flight Simulator" })
                {
                    string p = Path.Combine(l, @"steamapps\common\" + game);
                    if (Directory.Exists(p))
                    {
                        log.AppendLine(L.T("sl.log.steam", p));
                        list.Add(p);
                    }
                }
            }
            return list;
        }

        private static IEnumerable<string> Drives()
        {
            var list = new List<string>();
            try
            {
                foreach (var d in DriveInfo.GetDrives())
                {
                    try { if (d.IsReady && d.DriveType == DriveType.Fixed) list.Add(d.RootDirectory.FullName.TrimEnd('\\')); }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
            if (list.Count == 0) list.Add("C:");
            return list;
        }

        /// <summary>在 root 下限制深度查找 SimConnect.dll / SimConnect_internal.dll（同目录优先前者）。</summary>
        private static string SearchDir(string root, int maxDepth, ref int budget, StringBuilder log, ref string wrongArch)
        {
            foreach (var fn in FileNames)
            {
                string direct = Path.Combine(root, fn);
                if (!File.Exists(direct)) continue;
                if (Is64Bit(direct)) return direct;
                if (wrongArch == null) wrongArch = direct;
                log.AppendLine(L.T("sl.log.is32", direct));
            }

            if (maxDepth <= 0) return null;

            string[] subs;
            try { subs = Directory.GetDirectories(root); }
            catch (Exception) { return null; }

            foreach (var sub in subs)
            {
                if (budget-- <= 0) return null;
                string name = Path.GetFileName(sub);
                // 跳过明显无关的大目录，避免全盘扫描
                if (name.Equals("Windows", StringComparison.OrdinalIgnoreCase)) continue;
                if (name.Equals("WindowsApps", StringComparison.OrdinalIgnoreCase) && root.Length < 4) continue;

                string hit = SearchDir(sub, maxDepth - 1, ref budget, log, ref wrongArch);
                if (hit != null) return hit;
            }
            return null;
        }

        /// <summary>读 PE 头判断是否 x64（0x8664）。异常时按 x64 处理，交给 LoadLibrary 报错。</summary>
        public static bool Is64Bit(string path)
        {
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var br = new BinaryReader(fs))
                {
                    fs.Seek(0x3C, SeekOrigin.Begin);
                    int pe = br.ReadInt32();
                    fs.Seek(pe + 4, SeekOrigin.Begin);
                    ushort machine = br.ReadUInt16();
                    return machine == 0x8664;
                }
            }
            catch (Exception)
            {
                return true;
            }
        }
    }
}
