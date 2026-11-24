using System;

namespace G940LedPanel.Core
{
    /// <summary>
    /// 8 颗灯的颜色档位。硬件只有红/绿两颗 LED，所以实际能显示的就四态：
    /// 全灭、绿、红、红绿同亮（琥珀）。没有亮度通道，做不出真正的渐变与呼吸。
    ///
    /// 数值是内部约定（0/1/2/3），Effects 层与 LedFrame 都按它算位图，
    /// 改动会让所有内置效果变色，不要动。
    /// </summary>
    public enum LedColor
    {
        Off = 0,
        Green = 1,
        Amber = 2,
        Red = 3
    }

    /// <summary>G940 的 8 个带灯按钮 P1~P8。位图里 bit0 对应 P1。</summary>
    public enum PanelButton
    {
        Undefined = -1,
        P1 = 0,
        P2 = 1,
        P3 = 2,
        P4 = 3,
        P5 = 4,
        P6 = 5,
        P7 = 6,
        P8 = 7
    }
}
