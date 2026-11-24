using System;
using System.Globalization;
using System.Resources;
using System.Threading;

namespace G940LedPanel.Core
{
    /// <summary>
    /// 界面文案的唯一入口（1.3.50 起）。
    ///
    /// 为什么放 Core 不放 UI：诊断报告、规则体检、配置读取失败的提示都在 Core 层拼文字，
    /// 那些也要翻。放 Core 才不破坏「Core 不引用 UI」这条依赖方向。
    ///
    /// 为什么不用强类型资源类（ResXFileCodeGenerator 生成的那个）：那个把每个 key 变成一个属性，
    /// 加一条文案要在两处动手（resx + 生成文件），而且 Core 与 UI 各生成一份就重复了。
    /// 这里只有 T(key) 一个入口，加文案只改 resx。
    ///
    /// 文案在 Strings.resx（中性 = 中文）与 Strings.&lt;语言代码&gt;.resx，编译后前者进主程序集、
    /// 后者进同名子目录的卫星程序集（en-US\G940LedPanel.resources.dll）。
    /// **打包必须带上这些目录**，漏了不会报错，只会永远显示中文。
    /// </summary>
    public static class L
    {
        public const string Chinese = "zh-CN";
        public const string English = "en-US";

        /// <summary>
        /// 语言表，**唯一真源**。加一门语言 = 这里加一行 + 加一份 Strings.&lt;id&gt;.resx
        /// + 两份包各加一条 lang.&lt;id&gt;（语言名）。别的代码不许再写语言清单。
        ///
        /// 为什么不能像 1.3.66 之前那样代码里写死两个值、各处 if/else：
        /// `Use()` 里那句 `Equals(lang, English) ? English : Chinese` 会把新语言
        /// **静默降级成中文**——界面显示中文、config.json 里却存着新语言，重启还是中文，
        /// 用户查不出原因，日志里也没有任何异常。语言清单只有一份，就不会出现这种下落。
        ///
        /// 第一项是中性语言（Strings.resx），也是「跟系统语言对不上时」的落点。
        /// </summary>
        public static readonly string[] All = { Chinese, English };

        private static readonly ResourceManager Rm =
            new ResourceManager("G940LedPanel.Strings", typeof(L).Assembly);

        /// <summary>当前界面语言（"zh-CN" / "en-US"）。断言固定跑中文，别在断言里切它。</summary>
        public static string Lang { get; private set; } = Chinese;

        /// <summary>
        /// 语言在界面上的名字。必须用它**自己那种语言**写（中文 / English）：
        /// 翻成「Chinese」之后，英文用户反而认不出哪个是自己正用的语言。
        /// 所以 lang.zh-CN 在两份包里都是「中文」，lang.en-US 都是「English」。
        /// </summary>
        public static string NativeName(string id)
        {
            return Tor("lang." + id, id);
        }

        /// <summary>表里的规范写法（忽略大小写）；表里没有返回 null。</summary>
        private static string Canonical(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var s in All)
                if (string.Equals(s, id, StringComparison.OrdinalIgnoreCase)) return s;
            return null;
        }

        /// <summary>
        /// 把 config.json 里记的值解析成表里的某个语言。
        /// 空 = 首次运行；表里查不到 = 存了个本版本不认识的值（旧配置、手改错、装了新语言又退回旧版）。
        /// 两种情况都跟随系统：系统语言在表里就用它，否则落第一项（中性包）。
        /// 不按 country 细分——表里没有的一律落第一项，比乱猜保守。
        /// </summary>
        public static string Resolve(string stored)
        {
            string hit = Canonical(stored);
            if (hit != null) return hit;

            string sys = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            foreach (var s in All)
            {
                if (string.Equals(s, sys, StringComparison.OrdinalIgnoreCase)
                    || s.StartsWith(sys + "-", StringComparison.OrdinalIgnoreCase))
                    return s;
            }
            return All[0];
        }

        /// <summary>
        /// 切语言。只改当前线程的 UI 文化，ResourceManager 据此挑卫星程序集。
        /// 传进来的值一律先过 Resolve，所以 Lang 永远是表里的规范写法——
        /// 不存在「存着一门语言、用着另一门」的中间态。
        /// </summary>
        public static void Use(string lang)
        {
            Lang = Resolve(lang);
            var ci = CultureInfo.GetCultureInfo(Lang);
            Thread.CurrentThread.CurrentUICulture = ci;
            CultureInfo.DefaultThreadCurrentUICulture = ci;
        }

        /// <summary>取一条文案。</summary>
        public static string T(string key)
        {
            string s = null;
            try { s = Rm.GetString(key, CultureInfo.GetCultureInfo(Lang)); }
            catch (MissingManifestResourceException) { /* 资源没编进去，下面统一兜底 */ }
            // 缺 key 返回「!key」而不是空串：开发期一眼看出漏了哪条，运行时也不会出现"文字凭空消失"。
            return string.IsNullOrEmpty(s) ? "!" + key : s;
        }

        /// <summary>取一条带占位符的文案（{0} {1} …）。不要用 "共 " + n + " 条" 这种拼法——英文的单复数拼不出来。</summary>
        public static string T(string key, params object[] args)
        {
            string s = T(key);
            if (args == null || args.Length == 0) return s;
            try { return string.Format(CultureInfo.GetCultureInfo(Lang), s, args); }
            catch (FormatException) { return s; }
        }

        /// <summary>
        /// 取一条文案，缺 key 时返回调用方给的兜底（1.3.57）。
        ///
        /// 与 <see cref="T(string)"/> 的分工：`T` 缺 key 返回「!key」，那是给开发看的，
        /// 用于"必须翻"的固定文案；`Tor` 用于**不该翻或不一定有**的名字——
        /// 用户自己起的规则名、用户自建的系统、语言包还没覆盖的第三方名词。
        /// 这些走 `T` 会在界面上印出「!usr_my_rule」，而它们本来就不该有译文。
        /// </summary>
        public static string Tor(string key, string fallback)
        {
            string s = null;
            try { s = Rm.GetString(key, CultureInfo.GetCultureInfo(Lang)); }
            catch (MissingManifestResourceException) { /* 资源没编进去，走兜底 */ }
            return string.IsNullOrEmpty(s) ? fallback : s;
        }
    }
}
