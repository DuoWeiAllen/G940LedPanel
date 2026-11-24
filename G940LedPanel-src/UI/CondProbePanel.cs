using System;
using System.Drawing;
using System.Windows.Forms;
using G940LedPanel.Core;

namespace G940LedPanel.UI
{
    /// <summary>
    /// 试算面板：把一条条件按"叶子"摊开，逐条显示 左值当前值 / 比较符 / 右值门槛 / 三态结果。
    /// 界面在 CondProbePanel.Designer.cs（VS 设计器可直接打开），本文件只放填充逻辑。
    ///
    /// 三态的意义：变量没数据时既不算真也不算假，整条条件因此"无数据"。
    /// 如果不这么显示，用户会以为"条件成立了但灯不亮是程序坏了"。
    /// </summary>
    public partial class CondProbePanel : UserControl
    {
        private static readonly Color C_True = UiColors.Ok;
        private static readonly Color C_False = SystemColors.ControlText;
        private static readonly Color C_Unknown = UiColors.Warn;

        /// <summary>数据源说明（"模拟器实时" / "手动试算"），由编辑器设置后随 Render 一起显示。</summary>
        public string SourceNote = "";

        /// <summary>供 VS 设计器实例化用。</summary>
        public CondProbePanel()
        {
            InitializeComponent();
            ApplyLang();
        }

        /// <summary>Designer 里那份中文是设计时预览值，运行时以语言包为准。</summary>
        private void ApplyLang()
        {
            _colCond.Text = L.T("probe.col.cond");
            _colLeft.Text = L.T("probe.col.left");
            _colCmp.Text = L.T("probe.col.cmp");
            _colRight.Text = L.T("probe.col.right");
            _colResult.Text = L.T("probe.col.result");
            _lblEmpty.Text = L.T("probe.head.nodata");
        }

        /// <summary>
        /// 重算并显示。<paramref name="state"/> 为 null 表示还没接到模拟器数据。
        /// </summary>
        public void Render(CondNode root, FlightState state)
        {
            _lst.BeginUpdate();
            try
            {
                _lst.Items.Clear();

                if (root == null)
                {
                    _lblEmpty.Text = L.T("probe.empty.nocond");
                    return;
                }

                bool hasData = state != null;
                string note = string.IsNullOrEmpty(SourceNote) ? "" : L.T("probe.note", SourceNote);
                _lblEmpty.Text = hasData
                    ? L.T("probe.rows", note)
                    : L.T("probe.nodata", note);

                var probes = CondEval.Probe(root, state);
                for (int i = 0; i < probes.Count; i++)
                {
                    var p = probes[i];

                    var it = new ListViewItem(p.LeftLabel);
                    it.SubItems.Add(p.LeftText);
                    it.SubItems.Add(p.CmpText);
                    it.SubItems.Add(p.RightText);
                    it.SubItems.Add(CondEval.TriText(p.Result));

                    switch (p.Result)
                    {
                        case Tri.True: it.ForeColor = C_True; break;
                        case Tri.False: it.ForeColor = C_False; break;
                        default: it.ForeColor = C_Unknown; break;
                    }
                    if (!hasData) it.ForeColor = C_Unknown;

                    _lst.Items.Add(it);
                }

                // 一眼看到整条结论
                Tri all = CondEval.Test(root, state);
                _lblEmpty.Text += L.T("probe.total", CondEval.TriText(all));
                HintText.Set(_lblEmpty, all == Tri.True ? C_True : (all == Tri.Unknown ? C_Unknown : C_False));
            }
            finally
            {
                _lst.EndUpdate();
            }
        }

        /// <summary>只清空，用于"没有选中任何条件"的状态。</summary>
        public void Clear()
        {
            _lst.Items.Clear();
            _lblEmpty.Text = L.T("probe.empty.noselect");
            HintText.Set(_lblEmpty, SystemColors.ControlText);
        }
    }
}
