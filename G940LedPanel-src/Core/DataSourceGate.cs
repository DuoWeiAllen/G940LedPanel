using System;

namespace G940LedPanel.Core
{
    /// <summary>
    /// 飞行数据源的「就绪状态」。这是告警模式唯一需要知道的、关于模拟器的事。
    ///
    /// 为什么要单独抽象这一层，而不是在界面里判 "Sim" 事件：
    /// 告警模式的设计目的是「连接某个模拟器（MSFS / DCS / 以后别的），把它的状态指示到灯上」。
    /// 不同模拟器给出「我到底在不在飞」的方式完全不同——
    /// MSFS 看的是相机状态这个 SimVar（见 <see cref="CameraStateJudge"/>），
    /// DCS 可能用 Export.lua 的心跳 + 自己的标志位。
    /// 界面只该关心「现在能不能信这份数据」，不该关心「是哪个模拟器、用哪个接口问的」。
    /// 因此：数据源负责把各自的信号翻译成下面四种状态，界面只消费这四种状态。
    /// 将来接 DCS，只要它的数据源也报这四种状态，界面一行都不用改。
    ///
    /// 四种状态的语义（判据只有一条，别再加第二处）：
    ///   · Ready    —— 确认在飞行中，数据可信，正常判定点灯
    ///   · Standby  —— 数据源连着、但确认不在飞行（菜单 / 加载 / 暂停），数据不可信，显示等待提示
    ///   · Offline  —— 连不上数据源，或数据流已断，显示离线提示
    ///   · Unknown  —— 数据源不提供「是否在飞」的信号，无从判断
    ///
    /// <see cref="Unknown"/> 的处理是这套设计的安全底线：退回旧行为（照常按数据判定），
    /// 而不是一律当成 Standby。因为「不提供信号」≠「不在飞」，
    /// 若把 Unknown 当 Standby，遇到不派发事件的模拟器就会在正常飞行时熄灯——比乱闪严重得多。
    /// </summary>
    public enum DataSourcePhase
    {
        Unknown = 0,
        Ready = 1,
        Standby = 2,
        Offline = 3
    }

    /// <summary>
    /// 把「连接状态 + 是否收到过就绪信号 + 信号说了什么」压成一个 <see cref="DataSourcePhase"/>。
    /// 纯函数，不碰界面、不碰 IO，因此可以离线穷举断言（见 CoreTests 的 P19 段）。
    ///
    /// 注意这里**没有「信号有效期」**。这是刻意的：
    /// 「在不在飞」是一份状态，不是心跳。状态是持久的：
    /// 要么被下一次应答改写，要么因连接断开（QUIT / 重连失败）自然归入 Offline。
    /// 若按「多久没收到就算过期」来判断，一旦某段时间没有新应答就会误判成 Unknown，
    /// 门随之失效 —— 菜单里的乱闪问题会原样复发。
    /// </summary>
    public static class DataSourceGate
    {
        /// <param name="connected">数据源是否连着</param>
        /// <param name="signalSeen">是否收到过「是否在飞」的信号</param>
        /// <param name="running">信号最近一次说的是「在飞」（true）还是「不在飞」（false）</param>
        public static DataSourcePhase Evaluate(bool connected, bool signalSeen, bool running)
        {
            // 连不上：与「在不在飞」无关，一律离线。
            if (!connected) return DataSourcePhase.Offline;

            // 没收到过信号：不知道 —— 退回旧行为，不能当成「不在飞」。
            // 「这个模拟器不派发该信号」和「它说了不在飞」是两件完全不同的事，
            // 混淆的代价是正常飞行时熄灯。
            if (!signalSeen) return DataSourcePhase.Unknown;

            return running ? DataSourcePhase.Ready : DataSourcePhase.Standby;
        }

        /// <summary>这份数据可不可以拿来点灯。只有「确认不在飞」才拒绝。</summary>
        public static bool AcceptsData(DataSourcePhase p)
        {
            return p != DataSourcePhase.Standby && p != DataSourcePhase.Offline;
        }

        /// <summary>是否该显示「等待中」的提示动画（确认不在飞行）。</summary>
        public static bool ShouldShowStandby(DataSourcePhase p)
        {
            return p == DataSourcePhase.Standby;
        }
    }
}
