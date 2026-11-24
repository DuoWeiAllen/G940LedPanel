using System;

namespace G940LedPanel.Effects
{
    /// <summary>
    /// 8 颗灯在面板上的物理排列：2 行 × 4 列（上排 P1–P4，下排 P5–P8）。
    /// 把两排看成一条首尾相接的“扁环”，所以遍历一律走 Ring，不走行优先——
    /// 行优先会在 P4→P5 处横向跳一次，看起来像两排灯各跑各的。
    /// </summary>
    public static class LedLayout
    {
        public const int Count = 8;

        /// <summary>顺时针环：上排左→右（P1..P4），落到底排右端（P8），再左行回起点（P5）。</summary>
        public static readonly int[] Ring = { 0, 1, 2, 3, 7, 6, 5, 4 };

        /// <summary>逆时针环：下排左→右，升到上排右端，再左行。</summary>
        public static readonly int[] RingRev = { 4, 5, 6, 7, 3, 2, 1, 0 };

        /// <summary>每一列的两颗灯（上排 i 与下排 4+i）。</summary>
        public static readonly int[][] Cols = { new[] { 0, 4 }, new[] { 1, 5 }, new[] { 2, 6 }, new[] { 3, 7 } };

        /// <summary>中间两列的列号。</summary>
        public static readonly int[] MidCols = { 1, 2 };

        /// <summary>外侧两列的列号。</summary>
        public static readonly int[] OutCols = { 0, 3 };
    }
}
