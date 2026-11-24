using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace G940LedPanel.Core
{
    /// <summary>
    /// 程序状态配置（config.json）：**只管这台机器的事**。
    ///
    /// 装什么：设备 GUID、窗口位置、页签、上次滚动/选中行、两个"当前在用哪套配置"的指针。
    /// 不装什么：所有用户作品——按键绑定、底灯、告警规则、阈值、灯位、相机表，
    /// 这些一律在 buttons\ 与 alerts\ 两个文件夹里，一套配置一个文件，可以直接拷给别人。
    ///
    /// 为什么这样分：以前所有东西挤在一个 config.json 里，想把一套灯效发给朋友，
    /// 就得把窗口位置、设备 GUID 一起发过去，对方一载入反而认不到摇杆。
    /// 现在"分享"就是拷一个文件，"备份"就是拷一个文件夹。
    /// </summary>
    public class AppConfig
    {
        /// <summary>配置结构版本。用它把"一次性迁移"和"用户选择"区分开，迁移永远只跑一次。</summary>
        public const int CurrentConfigVersion = 3;

        public int ConfigVersion { get; set; } = CurrentConfigVersion;

        /// <summary>
        /// 用户手动指定过的设备名（空 = 用出厂默认名 "Logitech G940 Throttle"）。
        ///
        /// 只在「精确匹配失败、用户在弹框里挑了一次」时写入。记名字不记 GUID：
        /// 名字是型号标识，拷到别的电脑仍然有效；GUID 是「机器 + USB 口」的指纹，
        /// 换台机器必然失效，且失效是静默的（1.3.34 删掉了原来那个 GUID 字段）。
        /// </summary>
        public string DeviceName { get; set; } = "";

        public bool StartEngine { get; set; } = true;

        /// <summary>v1.3.0 全部效果总开关。</summary>
        public bool MasterOn { get; set; } = true;

        /// <summary>预览灯上是否显示灯位文字（P1..P8 / 系统名）。</summary>
        public bool ShowLedLabel { get; set; } = true;

        /// <summary>
        /// 关闭窗口 / 最小化时是否收到系统托盘继续运行。
        ///
        /// true（默认）：点 × 收托盘、最小化收托盘、托盘图标常驻，可从托盘恢复或退出。
        /// false：点 × 直接退出（存盘 + 断设备 + 熄灯）、最小化就是普通最小化到任务栏、
        ///        不创建托盘图标——此时退出只能点 × 或任务栏右键关窗。
        ///
        /// 默认 true 是为了让老用户升级后行为不变。
        /// </summary>
        public bool MinimizeToTray { get; set; } = true;

        /// <summary>上次停留的页：0=按键模式，1=告警模式。</summary>
        public int LastMode { get; set; }

        /// <summary>
        /// 界面语言："zh-CN" / "en-US"。
        /// 空 = 从没选过（首次运行）→ 跟随系统，见 <see cref="L.Resolve"/>。
        /// 存字符串不存枚举：以后加语言只加一个 .resx 和一个语言码，不用改这个字段的类型。
        /// </summary>
        public string Language { get; set; } = "";

        // ---- 当前正在使用的配置（指向 buttons\ / alerts\ 里的某个文件）----

        /// <summary>
        /// 当前按键配置：存**身份**（相对 buttons\ 的路径，不含 .json），不存显示名。
        /// 预置是 "builtin/demo_rhythm"，用户配置是 "我的配置"。
        /// 为什么不是名字：名字会随语言变，一变就找不到文件了。
        /// 空 = 用第一套预置。
        /// </summary>
        public string ActiveButtonFile { get; set; } = "";

        /// <summary>当前告警配置：同上，存身份而不是显示名。空 = 用第一套预置。</summary>
        public string ActiveAlertFile { get; set; } = "";

        // ---- 窗口与视图状态（下次启动原样恢复） ----

        /// <summary>主窗口位置与大小："x,y,w,h"，空=默认。</summary>
        public string WindowBounds { get; set; } = "";

        /// <summary>告警表上次滚动到的首行。</summary>
        public int AlertScrollRow { get; set; }

        /// <summary>告警表上次选中的规则 Id（用 Id 而不是行号，规则顺序变了也不会错位）。</summary>
        public string AlertSelectedRule { get; set; } = "";

        /// <summary>实时数据窗位置大小："x,y,w,h"，空=默认。</summary>
        public string SimDataBounds { get; set; } = "";

        /// <summary>实时数据窗是否总在最前。</summary>
        public bool SimDataOnTop { get; set; }

        /// <summary>关闭程序时实时数据窗是开着的——下次启动原样再开出来。</summary>
        public bool SimDataVisible { get; set; }

        /// <summary>按键表上次滚动到的首行。</summary>
        public int BindScrollRow { get; set; }

        /// <summary>按键表上次选中的行（0 基）。</summary>
        public int BindSelectedRow { get; set; }

        /// <summary>是否启用 SimConnect 数据读取（启动时自动连接模拟器）。</summary>
        public bool SimConnectEnabled { get; set; } = true;

        /// <summary>
        /// 是否记录「场景状态」（写 diag\g940-simstate.txt）。
        /// 默认关：平时不写盘；要查"菜单里为什么亮/不亮、进出飞行慢了几秒"时在告警页勾上。
        /// </summary>
        public bool SceneStateLog { get; set; }

        /// <summary>
        /// 相机状态 → 分类（Menu / Skip / Flight）。整张表就这一份（见 <see cref="CameraStateJudge"/>）。
        /// <para>放这里而不是放进 alerts\ 的配置：它描述的是"这台机器看到的模拟器画面长什么样"，
        /// 与用哪一套告警规则无关。换一套规则不该把相机表也换掉。</para>
        /// <para>null（首次运行）→ 程序写入默认整套；写成 <c>{}</c> → 空表，全部按在飞，这是用户的明确选择。</para>
        /// </summary>
        public Dictionary<string, string> CameraStates { get; set; }

        /// <summary>
        /// 旧字段（v1.3.22 的"追加菜单值"），只为读老配置。载入时并入 <see cref="CameraStates"/>，随即清空。
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore(
            Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public List<int> ExtraMenuCameraStates { get; set; }

        /// <summary>相机状态表体检：解析 → 兼容旧字段 → 写回规范形态。</summary>
        public List<string> SanitizeCamera()
        {
            var notes = new List<string>();

            int dropped;
            var table = CameraStateJudge.Build(CameraStates, out dropped);

            if (ExtraMenuCameraStates != null)
            {
                if (ExtraMenuCameraStates.Count > 0)
                {
                    CameraStateJudge.MergeLegacyMenuValues(table, ExtraMenuCameraStates);
                    notes.Add(L.T("cfg.note.migrated", ExtraMenuCameraStates.Count));
                }
                ExtraMenuCameraStates = null;
            }

            if (dropped > 0)
                notes.Add(L.T("cfg.note.dropped", dropped));

            CameraStates = CameraStateJudge.ToConfigTable(table);
            return notes;
        }

        /// <summary>配置文件位置：程序所在目录 \ config.json。</summary>
        public static string ConfigPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

        /// <summary>
        /// 上一次保存失败的原因（空 = 一切正常）。
        /// 原来 Save 把异常整个吞掉了：程序装在只读目录、U 盘拔了、磁盘满了，
        /// 界面上一句提示都没有，用户只知道"改了没保存下来"，谁也查不出为什么。
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public string LastSaveError { get; private set; } = "";

        /// <summary>
        /// 载入配置。path 为空时读程序目录下那一份（生产路径）；断言用它指向临时文件，
        /// 才能真正跑一遍"旧版 json → 升级"这条路，而不是比两个常量。
        /// </summary>
        public static AppConfig Load(string path = null)
        {
            string file = string.IsNullOrEmpty(path) ? ConfigPath : path;
            // 读不出来（写了一半 / 损坏 / 反序列化失败）就用默认，不去猜。
            try
            {
                if (File.Exists(file))
                {
                    var cfg = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(file));
                    if (cfg != null) return EnsureAll(cfg);
                }
            }
            catch (Exception)
            {
                // 读不出来 → 用默认配置
            }

            return EnsureAll(new AppConfig());
        }

        private static AppConfig EnsureAll(AppConfig cfg)
        {
            if (cfg.ConfigNotes == null) cfg.ConfigNotes = new List<string>();
            if (cfg.ConfigVersion < CurrentConfigVersion) cfg.ConfigVersion = CurrentConfigVersion;
            cfg.ConfigNotes.AddRange(cfg.SanitizeCamera());
            return cfg;
        }

        /// <summary>本次载入过程中需要提示用户的事项（迁移、修复、丢弃坏条件等）。</summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public List<string> ConfigNotes { get; set; } = new List<string>();

        /// <summary>
        /// 存盘。返回 false 表示这一次没写成功，原因记在 <see cref="LastSaveError"/>。
        ///
        /// 先写 .tmp 再原子替换：直接覆盖原文件时，如果正写到一半被结束进程、断电，
        /// config.json 会变成半截 JSON，下次启动读不出来。多这三行，磁盘上任何时刻都是完整文件。
        /// </summary>
        public bool Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                string tmp = ConfigPath + ".tmp";
                File.WriteAllText(tmp, json, new System.Text.UTF8Encoding(false));

                if (File.Exists(ConfigPath))
                {
                    try
                    {
                        File.Replace(tmp, ConfigPath, null);        // 原子替换，不留备份文件
                    }
                    catch (Exception)
                    {
                        // 网络盘 / FAT32 不支持 Replace：退化成直接覆盖
                        File.Copy(tmp, ConfigPath, true);
                        try { File.Delete(tmp); } catch (Exception) { }
                    }
                }
                else
                {
                    File.Move(tmp, ConfigPath);
                }

                LastSaveError = "";
                return true;
            }
            catch (Exception ex)
            {
                LastSaveError = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        // ---------------------------------------------------------------- 预置按键配置

        /// <summary>
        /// 预置按键配置：读 buttons\builtin\ 下的文件（1.3.48 起，不再写死在代码里）。
        ///
        /// 读不到就是没有，程序不重建。显示名由 <see cref="AlertNames"/> 按文件名给出。
        /// 底灯一律关灯：环行动画、装饰这两套本身就是整帧动画，再叠一层底灯就分不清是谁在跑。
        /// </summary>
        public static List<ButtonProfile> BuiltInButtonProfiles()
        {
            var list = new List<ButtonProfile>();

            string[] files;
            try { files = Directory.GetFiles(ProfileStore.BuiltInButtonsDir, "*.json"); }
            catch (Exception) { return list; }

            foreach (var f in files)
            {
                ButtonProfile p;
                string err;
                if (!ProfileStore.LoadButton(f, out p, out err)) continue;

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
    }
}
