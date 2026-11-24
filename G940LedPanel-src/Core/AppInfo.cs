using System;
using System.Reflection;

namespace G940LedPanel.Core
{
    /// <summary>
    /// 程序版本号的**唯一来源**：直接读程序集版本，而程序集版本由 csproj 的
    /// <c>&lt;Version&gt;</c> 决定。标题栏、托盘提示都从这里取，出包只改 csproj 一处。
    ///
    /// <para>原来版本号散在三处（Designer 的窗体标题、托盘的提示文字、csproj），
    /// 每出一版都要手工改，漏一处就出现"标题栏还写着上一版"。改成单一来源之后，
    /// 漏改在结构上就不可能发生。</para>
    ///
    /// <para>1.3.24 起版本号是三段 <c>主.次.补丁</c>：第三段日常递增，第二段是里程碑，
    /// 第一段是代际号，后两者变动需负责人同意。规则写在 csproj 的注释里。</para>
    /// </summary>
    public static class AppInfo
    {
        /// <summary>
        /// 产品名（不含版本）。**不随语言变**：日志里的产品标识要能在两种语言下对上。
        /// 界面上显示的名字用 <see cref="Title"/>，那个走语言包。
        /// </summary>
        public const string Name = "G940LED面板";

        /// <summary>
        /// 主版本.次版本.补丁，例如 "1.3.24"。取程序集版本的前三段。
        ///
        /// <para>**必须取三段**：版本号改成 1.3.24 这种形式之后，只取前两段会得到 "1.3"，
        /// 标题栏就永远看不出是哪一版了。</para>
        /// </summary>
        public static string Version
        {
            get
            {
                try
                {
                    var v = Assembly.GetExecutingAssembly().GetName().Version;
                    if (v == null) return "0.0.0";
                    return v.Major + "." + v.Minor + "." + v.Build;
                }
                catch (Exception)
                {
                    return "0.0.0";
                }
            }
        }

        /// <summary>
        /// 窗口标题与托盘提示用："G940LED面板 v1.3.24" / "G940 LED Panel v1.3.24"。
        ///
        /// 产品名走语言包（app.title）而不是常量 <see cref="Name"/>：标题栏是界面的一部分，
        /// 1.3.63 之前英文界面顶着一行「G940LED面板」，就是这里写死的。
        /// 切语言后由 MainForm.ApplyLanguage 重新赋一次 Text。
        /// </summary>
        public static string Title
        {
            get { return L.T("app.title") + " v" + Version; }
        }
    }
}
