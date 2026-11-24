using System;

namespace G940LedPanel.Core
{
    /// <summary>一帧 8 个 LED 的颜色状态（P1~P8），可整体/单灯设置。</summary>
    public class LedFrame
    {
        public readonly byte[] C = new byte[8]; // LedColor 值

        public void Clear()
        {
            Array.Clear(C, 0, C.Length);
        }

        public void SetAll(byte color)
        {
            for (int i = 0; i < C.Length; i++) C[i] = color;
        }

        public void Set(int led, byte color)
        {
            if (led >= 0 && led < C.Length) C[led] = color;
        }

        /// <summary>targetLed &lt; 0 表示全部，否则只设置单个灯。</summary>
        public void SetTarget(int targetLed, byte color)
        {
            if (targetLed < 0) SetAll(color);
            else Set(targetLed, color);
        }

        /// <summary>按 LogiJoystick 位掩码约定把颜色数组转成 red/green 掩码。</summary>
        public void ToMasks(out byte red, out byte green)
        {
            red = 0;
            green = 0;
            for (int i = 0; i < 8; i++)
            {
                byte c = C[i];
                if (c == (byte)LedColor.Red || c == (byte)LedColor.Amber) red |= (byte)(1 << i);
                if (c == (byte)LedColor.Green || c == (byte)LedColor.Amber) green |= (byte)(1 << i);
            }
        }

        public LedFrame Clone()
        {
            var f = new LedFrame();
            Array.Copy(C, f.C, 8);
            return f;
        }
    }
}
