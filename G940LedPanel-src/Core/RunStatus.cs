using System;

namespace G940LedPanel.Core
{
    /// <summary>数据源的运行状态。</summary>
    public enum RunState
    {
        /// <summary>联动开关没打开。</summary>
        DataOff = 0,
        /// <summary>开关开了，但采集器没跑起来（多半是 SimConnect.dll 缺失）。</summary>
        NotStarted = 1,
        /// <summary>采集器在跑，还没找到模拟器。</summary>
        Searching = 2,
        /// <summary>已连上模拟器，但一帧数据都没来。</summary>
        NoFrames = 3,
        /// <summary>有数据，一切正常。</summary>
        Ok = 4,
        /// <summary>已连上模拟器，但确认不在飞行（菜单 / 加载 / 暂停），收到的数值不可信。</summary>
        Standby = 5
    }

    /// <summary>
    /// 程序“现在到底什么状态”的唯一判定点。
    ///
    /// 为什么要单独抽一个纯函数：提示文案、灯效原来分散在
    /// UpdateSourceStatus / RefreshAlertCounts / OnSimStatus 三处，
    /// 每加一种状态就得改四处，漏一处就出现自相矛盾——
    /// 比如灯在一直转圈提醒，右下角却写着“告警灯暂时全灭”。
    /// 现在所有提示都从这一个结果派生，UI 只负责搬运文字和颜色。
    /// </summary>
    public sealed class RunStatus
    {
        public RunState State { get; private set; }
        public bool AlarmMode { get; private set; }
        public bool MasterOn { get; private set; }

        /// <summary>
        /// 状态栏「数据源」那段。**必须短**：状态栏是单行表格的一格，
        /// 一格宽度固定，写长了就被截成“……灯”，反而比不写更让人糊涂。
        /// 说清“有没有数据”就够，怎么办放到 <see cref="SourceTip"/>。
        /// </summary>
        public string SourceText { get; private set; }

        /// <summary>
        /// <see cref="SourceText"/> 的完整版（状态栏与卡片的气泡用）：
        /// 原因、该点哪里、还可能是什么问题，都写在这里。
        /// 短版和长版出自同一次判定，不会出现“状态栏说一套、悬停说另一套”。
        /// </summary>
        public string SourceTip { get; private set; }

        /// <summary>中栏数据源卡片的短文案：只说开关与连接，不复述状态栏。</summary>
        public string CardText { get; private set; }

        /// <summary>一句「要动手 / 要查」的提示（正常时为 null）。1.3.33 起清单取消，只剩判定用。</summary>
        public string IssueText { get; private set; }

        /// <summary>问题严重程度：0=信息 1=提醒 2=异常。</summary>
        public int IssueLevel { get; private set; }

        /// <summary>
        /// 灯要不要一直转圈警示（红色反向环行）：告警模式下，只要不是 Ok 就一直转。
        /// 但「等待中」不算不正常 —— 那是模拟器在菜单里的正常状态，用绿色三灯追表示，
        /// 若也让它转红色，用户会以为出了故障。
        /// 与总开关无关——总开关只决定“输出不输出”，不改变“现在正不正常”。
        /// </summary>
        public bool LampLoop
        {
            get { return AlarmMode && State != RunState.Ok && State != RunState.Standby; }
        }

        /// <summary>
        /// 灯要不要显示「等待中」：告警模式 + 确认不在飞行。
        /// 绿色反向三灯追 = 工作正常、在等；与红色环行（不正常）一眼能分开。
        /// </summary>
        public bool LampStandby
        {
            get { return AlarmMode && State == RunState.Standby; }
        }

        public bool Ok
        {
            get { return State == RunState.Ok; }
        }

        /// <summary>
        /// 判定当前状态。<paramref name="hasData"/> 指最近仍在收到有效数据；
        /// 断线时调用方要把缓存的帧丢掉，否则会拿着过期数据点灯。
        /// <paramref name="phase"/> 是数据源自己报的「在不在飞」，
        /// 默认 <see cref="DataSourcePhase.Unknown"/>（不提供该信号，退回原有判定）。
        /// </summary>
        /// <param name="cardTitle">
        /// 界面上那张数据源卡片的标题（如「MSFS2024（SimConnect）」）。
        /// 提示里要告诉用户"去哪里点"，就必须用用户**看得见的那几个字**，
        /// 不能自己另编一个名字——这里以前写成固定的"数据源卡片"，
        /// 可界面上根本没有这四个字，用户只能干找。
        /// </param>
        /// <param name="toggleText">
        /// 那个联动开关自己的文字（如「启用 SimConnect 联动」）。
        /// 以前用 sourceName 拼（"启用 MSFS2024 联动"），把"数据源叫什么"
        /// 当成了"开关叫什么"，两处对不上。名字归控件所有，这里只负责引用。
        /// </param>
        public static RunStatus Evaluate(bool masterOn, bool alarmMode, bool simOn,
            bool simRunning, bool simConnected, bool hasData,
            string sourceName, string lastError,
            DataSourcePhase phase = DataSourcePhase.Unknown,
            string cardTitle = null, string toggleText = null)
        {
            var r = new RunStatus { MasterOn = masterOn, AlarmMode = alarmMode };
            if (string.IsNullOrEmpty(sourceName)) sourceName = L.T("run.sim.generic");

            // 不传就退回中性说法：宁可少说一句"去哪点"，也不能拼出个不存在的控件名。
            if (string.IsNullOrEmpty(cardTitle)) cardTitle = L.T("run.card.generic");
            if (string.IsNullOrEmpty(toggleText)) toggleText = L.T("run.toggle.generic");

            string err = string.IsNullOrEmpty(lastError) ? "" : lastError;

            if (!simOn)
            {
                r.State = RunState.DataOff;
                r.SourceText = L.T("run.off.text");
                r.SourceTip = L.T("run.off.tip", cardTitle, toggleText);
                r.CardText = L.T("run.off.card");
                r.IssueText = L.T("run.off.issue");
                r.IssueLevel = 1;
            }
            else if (phase == DataSourcePhase.Standby)
            {
                // 菜单 / 加载画面：模拟器仍在推数值，但那些值不可信（会随机跳变）。
                // 这是**正常状态**，不是故障，所以不转红色警示、也不该被当成"要动手"的事。
                r.State = RunState.Standby;
                r.SourceText = L.T("run.standby.text", sourceName);
                r.SourceTip = L.T("run.standby.tip");
                r.CardText = L.T("run.standby.card");
                r.IssueLevel = 0;
            }
            else if (hasData)
            {
                r.State = RunState.Ok;
                r.SourceText = L.T("run.ok.text", sourceName);
                r.SourceTip = L.T("run.ok.tip", sourceName);
                r.CardText = L.T("run.ok.card");
                r.IssueLevel = 0;
            }
            else if (simConnected)
            {
                r.State = RunState.NoFrames;
                r.SourceText = L.T("run.noframe.text", sourceName);
                r.SourceTip = L.T("run.noframe.tip", sourceName);
                r.CardText = L.T("run.noframe.card");
                r.IssueText = L.T("run.noframe.issue", sourceName);
                r.IssueLevel = 1;
            }
            else if (simRunning)
            {
                r.State = RunState.Searching;
                r.SourceText = L.T("run.search.text", sourceName);
                r.SourceTip = L.T("run.search.tip", sourceName)
                    + (err.Length == 0 ? "" : L.T("run.search.tip.err", err));
                r.CardText = L.T("run.search.card");
                r.IssueText = err.Length == 0
                    ? L.T("run.search.issue", sourceName)
                    : L.T("run.search.issue.err", sourceName, err);
                r.IssueLevel = 1;
            }
            else
            {
                r.State = RunState.NotStarted;
                r.SourceText = L.T("run.notstart.text");
                r.SourceTip = L.T("run.notstart.tip")
                    + (err.Length == 0 ? L.T("run.notstart.tip.hint") : L.T("run.notstart.tip.err", err));
                r.CardText = L.T("run.notstart.card");
                r.IssueText = err.Length == 0
                    ? L.T("run.notstart.issue")
                    : L.T("run.notstart.issue.err", err);
                r.IssueLevel = 2;
            }

            // 尾注：先说灯在做什么，再说为什么看不到（顺序反了会读成“灯灭了所以正常”）
            if (r.LampLoop)
            {
                r.SourceText += L.T("run.suffix.loop");
                r.SourceTip += L.T("run.suffix.loop.tip");
            }
            if (!masterOn)
            {
                r.SourceText += L.T("run.suffix.masteroff");
                r.SourceTip += L.T("run.suffix.masteroff.tip");
            }

            return r;
        }
    }
}
