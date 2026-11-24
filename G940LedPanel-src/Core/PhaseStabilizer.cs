using System;

namespace G940LedPanel.Core
{
    /// <summary>
    /// 判定稳定器：新判定要**连续保持一段时间**才真的生效，短暂抖动一律不采信。
    ///
    /// 为什么需要它（实测依据，v3.20 日志）：
    ///   相机状态里有一类"1 秒空档"的值（如 29 空闲），飞行中切视角、开 Esc 菜单都会闪一下，
    ///   菜单里也会出现。放进菜单表 → 飞行中灯会顿一下；移出菜单表 → 菜单里会亮 1 秒。
    ///   两边都不对。它不是"哪个值"的问题，是"抖一下就变"这件事本身不对。
    ///
    /// 为什么是纯逻辑、时间由调用方给：
    ///   这样能离线穷举"抖 1 秒 / 稳 2 秒 / 卡在边界"这些情况，不用等真机。
    ///   时间是单调毫秒（调用方传 Environment.TickCount），用无符号减法，49 天回绕也不会算错。
    ///
    /// 首次观测**立即生效**：那是"现在真实是什么"，没有抖动的嫌疑；
    /// 若首次也延迟，程序启动后的头两秒会退回"不知道"，等于门没装。
    /// </summary>
    public sealed class PhaseStabilizer
    {
        private readonly uint _holdMs;

        private bool _seen;
        private bool _stable;       // 当前生效的判定
        private bool _pending;      // 有一个"还不算数"的新判定在观察中
        private bool _pendingValue;
        private uint _pendingSince;

        /// <param name="holdMs">新判定要连续保持多久才生效（建议 2000：数据帧 1 秒一帧，2 秒即至少两帧一致）</param>
        public PhaseStabilizer(int holdMs)
        {
            _holdMs = (uint)Math.Max(0, holdMs);
        }

        /// <summary>是否读到过观测。false 时上层应退回"不知道"，不做门。</summary>
        public bool Seen { get { return _seen; } }

        /// <summary>当前生效的判定（true = 在飞）。</summary>
        public bool InFlight { get { return _stable; } }

        /// <summary>是否有新判定正在观察、尚未生效（日志/排查用）。</summary>
        public bool Settling { get { return _pending; } }

        /// <summary>喂一次观测。nowMs 用单调毫秒（Environment.TickCount 转 uint）。</summary>
        public void Feed(bool rawInFlight, uint nowMs)
        {
            if (!_seen)
            {
                _seen = true;
                _stable = rawInFlight;
                _pending = false;
                return;
            }

            // 与生效值一致 → 抖动结束，取消观察
            if (rawInFlight == _stable)
            {
                _pending = false;
                return;
            }

            // 出现一个新的相反判定，或观察中又换了一个 → 重新起算
            if (!_pending || _pendingValue != rawInFlight)
            {
                _pending = true;
                _pendingValue = rawInFlight;
                _pendingSince = nowMs;
                return;
            }

            if (unchecked(nowMs - _pendingSince) >= _holdMs)
            {
                _stable = rawInFlight;
                _pending = false;
            }
        }

        /// <summary>断线 / 重连时调用：忘掉一切，等下一次观测重新起算。</summary>
        public void Reset()
        {
            _seen = false;
            _stable = false;
            _pending = false;
            _pendingValue = false;
            _pendingSince = 0;
        }
    }
}
