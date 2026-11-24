using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace G940LedPanel.Core
{
    /// <summary>下拉列表里的一项配置（只描述"有这么一套"，不含内容）。</summary>
    public sealed class ProfileEntry
    {
        /// <summary>显示名：可以是译文（预置），也可以是用户起的名字。</summary>
        public string Name { get; set; } = "";

        /// <summary>身份：相对本类目录的路径，不含 .json。预置是 "builtin/twin_piston"，用户是 "我的配置"。</summary>
        public string Key { get; set; } = "";

        /// <summary>文件路径；预置也指向 alerts\builtin\ 下的文件，不再是空。</summary>
        public string FilePath { get; set; } = "";

        public bool IsBuiltIn { get; set; }

        /// <summary>
        /// 下拉里要显示的文字，由 UI 按当前语言填（1.3.58 起）。
        /// 空时回落到 <see cref="Name"/>：那是用户自己起的名字，本来就不翻译。
        ///
        /// 为什么不在 Core 拼：Core 不知道界面是什么语言，「（内置）」这个前缀
        /// 也得跟着语言变，拼在这儿就等于把显示文案写死在数据层。
        /// </summary>
        public string Display { get; set; } = "";

        /// <summary>
        /// 下拉框直接装 ProfileEntry 时靠它显示（WinForms 的 Items 走 ToString）。
        /// 只回 <see cref="Display"/>，不加任何前缀——前缀归 UI（TextNames.ProfileName）。
        /// </summary>
        public override string ToString()
        {
            return string.IsNullOrEmpty(Display) ? Name : Display;
        }
    }

    /// <summary>
    /// 配置文件的唯一读写入口。程序目录下两个平级文件夹：
    ///
    ///   buttons\  按键模式的配置，一套一个 .json
    ///   alerts\   告警模式的配置，一套一个 .json
    ///   两者的 builtin\ 子目录：随包发出的预置配置
    ///
    /// 设计原则：
    ///   · **目录即清单** —— 不在 config.json 里再存一份"有哪些配置"，避免文件和清单对不上；
    ///   · **一个文件一套配置** —— 分享就是拷文件，备份就是拷文件夹；
    ///   · **身份是文件名** —— 文件名不翻译（预置用英文名），显示名另由资源给出；
    ///   · **预置也是文件** —— 与用户配置同一套读写路径，只在"能不能覆盖/删除"上不同；
    ///   · **写入前留 .bak** —— 改坏了还能找回上一版。
    /// </summary>
    public static class ProfileStore
    {
        public const string ButtonsFolder = "buttons";
        public const string AlertsFolder = "alerts";
        public const string BuiltInFolder = "builtin";

        private static readonly JsonSerializerOptions WriteOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static readonly JsonSerializerOptions ReadOptions = new JsonSerializerOptions
        {
            IncludeFields = true,
            PropertyNameCaseInsensitive = true
        };

        public static string ButtonsDir
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ButtonsFolder); }
        }

        public static string AlertsDir
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AlertsFolder); }
        }

        /// <summary>随包发出的预置按键配置目录。</summary>
        public static string BuiltInButtonsDir
        {
            get { return Path.Combine(ButtonsDir, BuiltInFolder); }
        }

        /// <summary>随包发出的预置告警配置目录。</summary>
        public static string BuiltInAlertsDir
        {
            get { return Path.Combine(AlertsDir, BuiltInFolder); }
        }

        // ---------------------------------------------------------------- 列举
        //
        // 顺序 = 界面下拉里的顺序：**自己的在前，内置的在后**。
        // 理由不是"谁的更重要"，而是可编辑性：内置只读，改了会被拦；日常要改的
        // 一定是自己那套。把它放前面，选错的机会就少一半。
        // 组内顺序各自由 ListFolder 负责——自建按显示名（拼音序），内置按
        // AlertNames.Index 固定序，两段都不受这里换序影响。

        public static List<ProfileEntry> ListButtons()
        {
            var list = ListFolder(ButtonsDir, ButtonsDir, false);
            list.AddRange(ListFolder(BuiltInButtonsDir, ButtonsDir, true));
            return list;
        }

        public static List<ProfileEntry> ListAlerts()
        {
            var list = ListFolder(AlertsDir, AlertsDir, false);
            list.AddRange(ListFolder(BuiltInAlertsDir, AlertsDir, true));
            return list;
        }

        /// <summary>
        /// 列出某目录下的 .json（只读文件名与 app 字段，不解析整份配置）。
        /// builtIn = true 时：Key 带 "builtin/" 前缀，显示名优先取 <see cref="AlertNames"/> 的译文，
        /// 并按 <see cref="AlertNames.Index"/> 固定排序——否则切语言后预置顺序会变。
        /// </summary>
        private static List<ProfileEntry> ListFolder(string dir, string rootDir, bool builtIn)
        {
            var list = new List<ProfileEntry>();
            try
            {
                if (!Directory.Exists(dir)) return list;
                foreach (var f in Directory.GetFiles(dir, "*.json"))
                {
                    var e = new ProfileEntry
                    {
                        Name = Path.GetFileNameWithoutExtension(f),
                        Key = KeyOf(f, rootDir),
                        FilePath = f,
                        IsBuiltIn = builtIn
                    };

                    // 读一下 name 字段：文件名是为了合法化而改过的，真名以文件里为准
                    try
                    {
                        using (var doc = JsonDocument.Parse(File.ReadAllText(f)))
                        {
                            JsonElement el;
                            if (doc.RootElement.TryGetProperty("name", out el) && el.ValueKind == JsonValueKind.String)
                            {
                                string n = el.GetString();
                                if (!string.IsNullOrWhiteSpace(n)) e.Name = n;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // 文件坏了也要出现在列表里：用户得能看到"这套配置有问题"，而不是莫名消失
                    }

                    if (builtIn)
                    {
                        string disp = AlertNames.Of(Path.GetFileNameWithoutExtension(f));
                        if (!string.IsNullOrEmpty(disp)) e.Name = disp;
                    }

                    list.Add(e);
                }

                if (builtIn)
                {
                    list.Sort((a, b) =>
                    {
                        int ia = AlertNames.Index(Path.GetFileNameWithoutExtension(a.FilePath));
                        int ib = AlertNames.Index(Path.GetFileNameWithoutExtension(b.FilePath));
                        return ia != ib ? ia.CompareTo(ib)
                                        : string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase);
                    });
                }
                else
                {
                    list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
                }
            }
            catch (Exception)
            {
                // 目录读不了（权限/被占用）→ 返回已收集到的，不抛
            }
            return list;
        }

        // ---------------------------------------------------------------- 读

        public static bool LoadButton(string path, out ButtonProfile profile, out string error)
        {
            profile = null;
            error = null;

            if (string.IsNullOrEmpty(path))
            {
                error = L.T("pf.err.nopath");
                return false;
            }

            string json;
            try { json = File.ReadAllText(path); }
            catch (Exception ex) { error = L.T("pf.err.read", ex.Message); return false; }

            try
            {
                var p = JsonSerializer.Deserialize<ButtonProfile>(json, ReadOptions);
                if (p == null) { error = L.T("pf.err.empty"); return false; }
                if (!string.IsNullOrEmpty(p.Kind) && p.Kind != ButtonProfile.KindValue)
                {
                    error = L.T("pf.err.kind.button", p.Kind);
                    return false;
                }
                if (p.Version > ButtonProfile.FormatVersion)
                {
                    error = L.T("pf.err.version", p.Version);
                    return false;
                }
                if (p.Bindings == null) p.Bindings = new List<ButtonBinding>();
                if (p.Background == null) p.Background = new BackgroundStyle();
                // 1.3.69：空名回落到文件名；名字含非法字符的老配置也回落到文件名。
                // 后者是 1.3.69 之前存进来的（那时保存不校验），文件名是替换过的、名字是原文。
                // 不对齐的话，用户打开自己的老配置、改完点保存，会被新的入口校验挡住——改不了自己的东西。
                // 列表本来就按文件名显示，对齐后视觉上没有变化。
                string stem = Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrWhiteSpace(p.Name) || !IsNameUsable(p.Name, out _)) p.Name = stem;

                // 按键行的效果候选里没有「关灯」（见 TextNames.BindEffects），读到 off 就按常亮。
                // 不归一的话，那一格的值不在候选列表里，DataGridView 会抛 DataError。
                // 底灯不在归一范围：底灯本来就要能关。
                if (p.Bindings != null)
                {
                    foreach (var b in p.Bindings)
                    {
                        if (b == null) continue;
                        if (string.IsNullOrEmpty(b.EffectId) ||
                            string.Equals(b.EffectId, "off", StringComparison.OrdinalIgnoreCase))
                            b.EffectId = "solid";
                    }
                }

                p.FilePath = path;
                p.IsBuiltIn = IsBuiltInPath(path);
                profile = p;
                return true;
            }
            catch (JsonException ex)
            {
                error = L.T("pf.err.json", ex.Message);
                return false;
            }
        }

        public static bool LoadAlert(string path, out AlertProfile profile, out string error)
        {
            profile = null;
            error = null;

            if (string.IsNullOrEmpty(path))
            {
                error = L.T("pf.err.nopath");
                return false;
            }

            string json;
            try { json = File.ReadAllText(path); }
            catch (Exception ex) { error = L.T("pf.err.read", ex.Message); return false; }

            if (!ParseAlert(json, out profile, out error)) return false;

            // 同上（见 LoadButton）：空名或含非法字符的老配置，名字对齐到文件名。
            string stem = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrWhiteSpace(profile.Name) || !IsNameUsable(profile.Name, out _)) profile.Name = stem;
            profile.FilePath = path;
            profile.IsBuiltIn = IsBuiltInPath(path);
            return true;
        }

        /// <summary>解析一段 JSON（不碰文件）。导入与测试都走这里，保证"磁盘上的"和"粘贴进来的"同一套规则。</summary>
        public static bool ParseAlert(string json, out AlertProfile profile, out string error)
        {
            profile = null;
            error = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                error = L.T("pf.err.blank");
                return false;
            }

            try
            {
                var p = JsonSerializer.Deserialize<AlertProfile>(json, ReadOptions);
                if (p == null) { error = L.T("pf.err.empty"); return false; }
                if (!string.IsNullOrEmpty(p.Kind) && p.Kind != AlertProfile.KindValue)
                {
                    error = L.T("pf.err.kind.alert", p.Kind);
                    return false;
                }
                if (p.Version > AlertProfile.FormatVersion)
                {
                    error = L.T("pf.err.version", p.Version);
                    return false;
                }
                profile = p;
                return true;
            }
            catch (JsonException ex)
            {
                error = L.T("pf.err.json", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 按显示名找列表里的那一项（不含预置）。找不到返回 null。
        ///
        /// **只用于"重名检查"这类按显示名的场合，定位/删除配置一律用 <see cref="FindByKey"/>。**
        /// 两个 json 里写了同一个 `Name`（手动改文件就会出现）时，这里只会返回排在前面的那套，
        /// 拿它去删就会删错——1.3.48 把配置身份换成文件名之后，按名找已经不再是身份了。
        /// 原名 `Find`，1.3.69 改名就是为了让这个误用不再"看起来很自然"。
        /// </summary>
        public static ProfileEntry FindByName(string name, bool alert)
        {
            var list = alert ? ListAlerts() : ListButtons();
            foreach (var e in list)
                if (!e.IsBuiltIn && string.Equals(e.Name, name, StringComparison.CurrentCultureIgnoreCase))
                    return e;
            return null;
        }

        /// <summary>按身份（相对路径，不含 .json）找列表里的那一项。找不到返回 null。</summary>
        public static ProfileEntry FindByKey(string key, bool alert)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            var list = alert ? ListAlerts() : ListButtons();
            foreach (var e in list)
                if (string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase))
                    return e;
            return null;
        }

        /// <summary>按名字取内置按键配置（找不到返回 null）。用于"重名检查"这类按显示名的场合。</summary>
        public static ButtonProfile FindBuiltInButton(string name)
        {
            foreach (var p in AppConfig.BuiltInButtonProfiles())
                if (string.Equals(p.Name, name, StringComparison.CurrentCultureIgnoreCase)) return p;
            return null;
        }

        /// <summary>按名字取内置告警配置（找不到返回 null）。用于"重名检查"这类按显示名的场合。</summary>
        public static AlertProfile FindBuiltInAlert(string name)
        {
            foreach (var p in AlertProfile.BuiltInProfiles())
                if (string.Equals(p.Name, name, StringComparison.CurrentCultureIgnoreCase)) return p;
            return null;
        }

        /// <summary>按身份取预置按键配置（找不到返回 null）。</summary>
        public static ButtonProfile FindBuiltInButtonByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            foreach (var p in AppConfig.BuiltInButtonProfiles())
                if (string.Equals(KeyOf(p.FilePath, ButtonsDir), key, StringComparison.OrdinalIgnoreCase)) return p;
            return null;
        }

        /// <summary>按身份取预置告警配置（找不到返回 null）。</summary>
        public static AlertProfile FindBuiltInAlertByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            foreach (var p in AlertProfile.BuiltInProfiles())
                if (string.Equals(KeyOf(p.FilePath, AlertsDir), key, StringComparison.OrdinalIgnoreCase)) return p;
            return null;
        }

        // ---------------------------------------------------------------- 写

        public static string SaveButton(ButtonProfile profile)
        {
            if (profile == null) return L.T("pf.err.save.nothing");
            if (profile.IsBuiltIn) return L.T("pf.err.save.builtin");
            if (!CheckWritable(out string why)) return why;

            if (!IsNameUsable(profile.Name, out string whyName)) return whyName;
            profile.Kind = ButtonProfile.KindValue;
            profile.Version = ButtonProfile.FormatVersion;

            string path = Path.Combine(ButtonsDir, SanitizeFileName(profile.Name) + ".json");
            string err = WriteFile(path, JsonSerializer.Serialize(profile, WriteOptions));
            if (err == null) profile.FilePath = path;   // 落盘后身份才算定下来（重命名要拿它删旧文件）
            return err;
        }

        public static string SaveAlert(AlertProfile profile)
        {
            if (profile == null) return L.T("pf.err.save.nothing");
            if (profile.IsBuiltIn) return L.T("pf.err.save.builtin");
            if (!CheckWritable(out string why)) return why;

            if (!IsNameUsable(profile.Name, out string whyName)) return whyName;
            profile.Kind = AlertProfile.KindValue;
            profile.Version = AlertProfile.FormatVersion;

            string path = Path.Combine(AlertsDir, SanitizeFileName(profile.Name) + ".json");
            string err = WriteFile(path, JsonSerializer.Serialize(profile, WriteOptions));
            if (err == null) profile.FilePath = path;   // 落盘后身份才算定下来（重命名要拿它删旧文件）
            return err;
        }

        /// <summary>
        /// 写文件：先留 .bak，再写 .tmp，最后原子替换。
        /// 直接覆盖原文件时，写到一半被结束进程会留下半截 JSON，下次打不开。
        /// </summary>
        private static string WriteFile(string path, string json)
        {
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                if (File.Exists(path))
                {
                    try { File.Copy(path, path + ".bak", true); } catch (Exception) { }
                }

                string tmp = path + ".tmp";
                File.WriteAllText(tmp, json, new UTF8Encoding(false));

                if (File.Exists(path))
                {
                    try { File.Replace(tmp, path, null); }
                    catch (Exception)
                    {
                        File.Copy(tmp, path, true);
                        try { File.Delete(tmp); } catch (Exception) { }
                    }
                }
                else
                {
                    File.Move(tmp, path);
                }
                return null;
            }
            catch (Exception ex)
            {
                return L.T("pf.err.write", ex.GetType().Name, ex.Message);
            }
        }

        // ---------------------------------------------------------------- 删

        public static string Delete(ProfileEntry entry)
        {
            if (entry == null) return L.T("pf.err.del.nothing");
            if (entry.IsBuiltIn) return L.T("pf.err.del.builtin");
            if (string.IsNullOrEmpty(entry.FilePath)) return L.T("pf.err.del.nofile");

            try
            {
                if (File.Exists(entry.FilePath)) File.Delete(entry.FilePath);
                // 备份故意留着：用户删错了，"上次保存前的版本"是唯一还能找回来的东西。
                // 下次保存同名配置时 WriteFile 会把它覆盖掉，不会长期堆积。
                return null;
            }
            catch (Exception ex)
            {
                return L.T("pf.err.del", ex.Message);
            }
        }

        // ---------------------------------------------------------------- 工具

        /// <summary>
        /// 绝对路径 → 身份（相对 rootDir 的路径，不含 .json，分隔符统一成 "/"）。
        /// 预置得到 "builtin/twin_piston"，用户配置得到 "我的配置"。不在 rootDir 下返回空。
        /// </summary>
        public static string KeyOf(string fullPath, string rootDir)
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return "";
            try
            {
                string full = Path.GetFullPath(fullPath);
                string root = Path.GetFullPath(rootDir).TrimEnd(Path.DirectorySeparatorChar)
                              + Path.DirectorySeparatorChar;
                if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return "";

                string rel = full.Substring(root.Length);
                if (rel.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    rel = rel.Substring(0, rel.Length - 5);
                return rel.Replace('\\', '/').Replace(Path.AltDirectorySeparatorChar, '/');
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>路径是否在 builtin 子目录里（父目录名等于 "builtin"）。</summary>
        public static bool IsBuiltInPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            try
            {
                string dir = Path.GetDirectoryName(Path.GetFullPath(path));
                return string.Equals(Path.GetFileName(dir), BuiltInFolder, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>身份 → 绝对路径。找不到对应文件也返回路径（调用方自己判存在）。</summary>
        public static string ResolvePath(string key, bool alert)
        {
            if (string.IsNullOrWhiteSpace(key)) return "";
            string root = alert ? AlertsDir : ButtonsDir;
            string rel = key.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(root, rel + ".json");
        }

        /// <summary>告警配置的身份。文件还没落盘时按"名字会存成的文件名"推算。</summary>
        public static string KeyOf(AlertProfile profile)
        {
            if (profile == null) return "";
            if (!string.IsNullOrEmpty(profile.FilePath)) return KeyOf(profile.FilePath, AlertsDir);
            return string.IsNullOrWhiteSpace(profile.Name) ? "" : SanitizeFileName(profile.Name);
        }

        /// <summary>按键配置的身份。文件还没落盘时按"名字会存成的文件名"推算。</summary>
        public static string KeyOf(ButtonProfile profile)
        {
            if (profile == null) return "";
            if (!string.IsNullOrEmpty(profile.FilePath)) return KeyOf(profile.FilePath, ButtonsDir);
            return string.IsNullOrWhiteSpace(profile.Name) ? "" : SanitizeFileName(profile.Name);
        }

        /// <summary>
        /// 名字里不能出现的字符。**唯一真源**：<see cref="SanitizeFileName"/> 替换的、
        /// <see cref="IsNameUsable"/> 拒绝的，都是这一张表。
        ///
        /// 不用 `Path.GetInvalidFileNameChars()`：它随平台变——Linux 上只返回 `'\0'` 与 `'/'`。
        /// 程序跑 Windows、断言跑 Linux，判据一跟平台走，同一个名字在两边会得到不同答案；
        /// 更糟的是"存下去会被替换成什么"与"要不要拒"可能用两张表，改一处就会分叉。
        /// </summary>
        private static readonly char[] BadNameChars =
            { '\\', '/', ':', '*', '?', '"', '<', '>', '|', '\r', '\n', '\t' };

        /// <summary>配置名 → 合法文件名。只替换 Windows 不允许的字符，中文原样保留。</summary>
        public static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return L.T("pf.unnamed");
            var sb = new StringBuilder(name.Trim());
            foreach (char c in BadNameChars) sb.Replace(c, '_');
            string s = sb.ToString().Trim().TrimEnd('.');
            return s.Length == 0 ? L.T("pf.unnamed") : s;
        }

        /// <summary>
        /// 这个名字能不能直接当文件名用（1.3.69）。能用返回 true，不能用返回原因。
        ///
        /// 为什么保存前要拒，而不是交给 <see cref="SanitizeFileName"/> 替换掉：
        /// `A/B` 与 `A:B` 都被替换成 `A_B`，两套配置存成同一个文件，后存的静默吃掉先存的。
        /// 用户前一天调好的参数第二天不见了，程序不报错、不提示、日志里也没有。
        /// 名字里本来就不该有这些字符，早拒绝比默默改名清楚。
        ///
        /// 首尾空白与末尾的点一并拒：它们被 <see cref="SanitizeFileName"/> 吃掉后同样会撞名
        /// （`" A "` 与 `A`、`A.` 与 `A` 都落到 `A.json`），且同样是"看起来不一样、存出来一样"。
        /// 控制字符（c &lt; 空格）也拒——它们在文件名里同样非法，且用户看不出来自己输了什么。
        /// </summary>
        public static bool IsNameUsable(string name, out string why)
        {
            why = null;
            if (string.IsNullOrWhiteSpace(name))
            {
                why = L.T("pf.err.save.noname");
                return false;
            }

            foreach (char c in name)
            {
                if (c < ' ' || Array.IndexOf(BadNameChars, c) >= 0)
                {
                    why = L.T("pf.err.save.badchar", c < ' ' ? "?" : c.ToString());
                    return false;
                }
            }

            if (char.IsWhiteSpace(name[0]) || char.IsWhiteSpace(name[name.Length - 1])
                || name[name.Length - 1] == '.')
            {
                why = L.T("pf.err.save.badedge");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 写权限自检：绿色版解压到 Program Files 或只读 U 盘时，配置存不下来。
        /// 这里试写一个临时文件，失败就返回原因，界面挂进 ⚠ 清单——否则用户只会看到"改了不生效"。
        /// </summary>
        public static bool CheckWritable(out string reason)
        {
            reason = null;
            try
            {
                string dir = ButtonsDir;
                Directory.CreateDirectory(dir);
                string probe = Path.Combine(dir, ".write-test");
                File.WriteAllText(probe, "ok");
                File.Delete(probe);
                return true;
            }
            catch (Exception ex)
            {
                reason = L.T("pf.err.readonly", ex.GetType().Name);
                return false;
            }
        }
    }
}
