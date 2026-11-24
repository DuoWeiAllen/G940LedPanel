using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace G940LedPanel.UI
{
    /// <summary>
    /// 界面文字配色，全项目唯一来源。一共四种：系统默认 + 绿 / 橙 / 红。
    ///
    /// 用法只有两条：
    ///   一、正文与说明文字一律用系统默认色，**不设 ForeColor**，不要来这里取色；
    ///   二、需要提醒时才上色，绿=正常、橙=提醒、红=错误。上色统一走 HintText.Set，
    ///       橙红会自动加粗，绿保持正常字重——正常状态是背景信息，不该抢注意力。
    ///
    /// 和 LedPalette 的关系：那是"灯亮起来"的颜色（深底、亮），这里是"字"的颜色
    /// （浅底、深）。同一色相的两档明度，不是两种颜色。
    /// 千万别拿灯色当字色——#3FE07B 压在白底上只有 1.5:1 对比度，加粗也补不回来。
    /// 灯要亮、字要深，这是两种材质，各用各的。
    ///
    /// 三支都按浅底（≈#F0F0F0）算过对比度，均 ≥4.5:1，小字也读得清。
    /// </summary>
    public static class UiColors
    {
        /// <summary>绿 #1F7A46 · 正常、已连接、通过。白底 4.7:1</summary>
        public static readonly Color Ok = Color.FromArgb(31, 122, 70);

        /// <summary>橙 #9A5B00 · 提醒、进行中、需要注意。白底 4.8:1</summary>
        public static readonly Color Warn = Color.FromArgb(154, 91, 0);

        /// <summary>红 #C02020 · 错误、失败、被拦下。白底 5.3:1</summary>
        public static readonly Color Err = Color.FromArgb(192, 32, 32);
    }

    /// <summary>
    /// 提醒文字的统一下发口：上色 + 按需加粗。
    ///
    /// 为什么不各自写两行：橙红要加粗、回默认色要取消加粗，散在几十处很容易漏掉一半，
    /// 结果同一屏里有的粗有的不粗。集中到这里，规则只有一条：
    /// **橙和红加粗，绿和默认色不加粗。**
    ///
    /// 字体对象全部缓存复用：Font 是 IDisposable，每次刷新都 new 会持续吃掉 GDI 句柄，
    /// 长时间挂着（这程序本来就是常驻的）会慢慢把手柄耗光。缓存键按
    /// "字体族 + 字号 + 样式"区分，各控件的字号不同也能各自取到对的字体。
    /// </summary>
    public static class HintText
    {
        private static readonly Dictionary<string, Font> Cache = new Dictionary<string, Font>();

        private static Font Get(Font basis, FontStyle style)
        {
            string key = basis.FontFamily.Name + "|" + basis.Size.ToString("0.##") + "|" + (int)style;
            Font f;
            if (!Cache.TryGetValue(key, out f))
            {
                f = new Font(basis.FontFamily, basis.Size, style);
                Cache[key] = f;
            }
            return f;
        }

        /// <summary>给标签上色；橙红自动加粗，其余回正常字重。</summary>
        public static void Set(Label lbl, Color c)
        {
            if (lbl == null) return;
            lbl.ForeColor = c;
            bool emph = (c == UiColors.Warn || c == UiColors.Err);
            lbl.Font = Get(lbl.Font, emph ? FontStyle.Bold : FontStyle.Regular);
        }
    }
}
