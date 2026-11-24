using System;

namespace G940LedPanel.Core
{
    /// <summary>
    /// 某一时刻的模拟器状态快照（不可变）：按 SimVars 下标存放数值。
    /// 由 SimConnect 线程按帧复制生成，供 UI 线程与条件求值只读使用。
    /// </summary>
    public sealed class FlightState
    {
        private readonly double[] _v;
        private readonly bool[] _has;

        /// <summary>收到该帧时的递增序号（每秒递增，用于判断数据是否在刷新）。</summary>
        public readonly long Tick;

        public FlightState(double[] values, bool[] has, long tick)
        {
            _v = new double[values.Length];
            _has = new bool[has.Length];
            Array.Copy(values, _v, Math.Min(values.Length, _v.Length));
            Array.Copy(has, _has, Math.Min(has.Length, _has.Length));
            Tick = tick;
        }

        /// <summary>取数值；该变量从未收到过数据时返回 defaultValue。</summary>
        public double N(string name, double defaultValue = 0.0)
        {
            int i = SimVars.IndexOf(name);
            if (i < 0 || i >= _v.Length) return defaultValue;
            if (!_has[i]) return defaultValue;
            return _v[i];
        }

        /// <summary>取布尔（&gt;0.5 为真）。</summary>
        public bool B(string name)
        {
            return N(name) > 0.5;
        }

        /// <summary>该变量是否已收到过有效数据。</summary>
        public bool Has(string name)
        {
            int i = SimVars.IndexOf(name);
            return i >= 0 && i < _has.Length && _has[i];
        }

        public int AnsweredCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _has.Length; i++) if (_has[i]) n++;
                return n;
            }
        }
    }
}
