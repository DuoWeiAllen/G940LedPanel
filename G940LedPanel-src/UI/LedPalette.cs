using System.Drawing;

namespace G940LedPanel.UI
{
    /// <summary>
    /// 界面上"灯亮起来"用的三种颜色，全程序唯一来源。
    ///
    /// 为什么单独抽出来：原来灯体（LedIndicator）、状态栏文字（C_Ok/C_Warn/C_Err）、
    /// 告警表里的颜色小方块三处各自硬编码同一组值。调一次色要改三个文件，
    /// 漏一处就变成"灯是橘的、字是黄的"这种花脸。现在只改这里。
    ///
    /// 注意：**这只影响界面显示，不影响真机**。G940 硬件只有绿/琥珀/红三种，
    /// 软件往设备上写的一直是 0/1/2/3 四个枚举值（见 LedFrame / LedColor）。
    ///
    /// 取色依据：
    /// - 琥珀取 #FF7A18，色相 25°，贴近真机琥珀 LED（约 590nm，偏橘红而不是偏黄）。
    /// - 绿取 #3FE07B，比原来的 #34D399 更淡更嫩，深色面板上不刺眼。
    /// - 红取 #FF2D2D，正红，饱和拉满。
    /// </summary>
    public static class LedPalette
    {
        /// <summary>绿：正常、已连接、允许。</summary>
        public static readonly Color Green = Color.FromArgb(0x3F, 0xE0, 0x7B);

        /// <summary>琥珀：警告、等待中、需要注意。</summary>
        public static readonly Color Amber = Color.FromArgb(0xFF, 0x7A, 0x18);

        /// <summary>红：异常、失败、危险。</summary>
        public static readonly Color Red = Color.FromArgb(0xFF, 0x2D, 0x2D);

        /// <summary>灭灯/未知：深蓝灰，只用于"这颗灯没亮"的示意，不属于灯色。</summary>
        public static readonly Color Dark = Color.FromArgb(26, 39, 64);

        /// <summary>
        /// 表格里「关」那一格的底色。
        ///
        /// 比 <see cref="Dark"/> 亮一档（70,77,92 对 26,39,64），故意不共用一个值：
        /// Dark 是画在深色面板上的灯体灭灯色，搬到白底表格里就是一整格黑洞，
        /// 看着像那一格坏了；这里要的是"安静的灰"，不是"没亮"。
        /// </summary>
        public static readonly Color CellOff = Color.FromArgb(70, 77, 92);

        /// <summary>
        /// 颜色位（0=关 1=绿 2=琥珀 3=红，同 Core 里的 <c>LedColor</c> 枚举）→ 界面色。越界一律当关。
        ///
        /// 为什么要这个方法（1.3.56）：颜色列是自绘的，取色原先按**显示名**比中文字面量
        /// （`if (name == "绿")`）。切英文后格子里的值成了 Green/Amber/Red，三个分支全落空，
        /// 整列变成一片灰——不报错、不抛异常，只是"颜色丢了"。
        /// 现在统一走颜色位，名字只负责显示、不承担语义，切语言与取色彻底解耦。
        /// </summary>
        public static Color Of(int colorIndex)
        {
            switch (colorIndex)
            {
                case 1: return Green;
                case 2: return Amber;
                case 3: return Red;
                default: return CellOff;   // 0=关，以及任何越界/查不到的名字
            }
        }
    }
}
