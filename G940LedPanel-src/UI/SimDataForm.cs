using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using G940LedPanel.Core;

namespace G940LedPanel.UI
{
    /// <summary>
    /// MSFS2024 实时数据窗：把 SimConnect 到底取到了什么、值是多少、哪些变量根本没应答
    /// 全部列出来。非模态，开着它照样操作主窗口。
    ///
    /// 「变化 / 最近更新」由窗口自己采样比较得出，不改采集层：
    /// 每 250ms 比一次当前值，变了就计数并记下时刻，能把"数据卡住"一眼看出来。
    ///
    /// 七列一律 <c>SortMode = NotSortable</c>（在 Designer 里逐列写死）。两列默认是可排的，
    /// 点了列头只会打乱行序：分组本来就连在一起，变量顺序也是 SimVars 里排好的，
    /// 排序排不出新信息，却让"第几行是哪个变量"变得要现查。禁掉省心。
    /// 值列取变量一律走 <c>row.Tag</c>，万一将来放开排序也不会串值。
    /// </summary>
    public partial class SimDataForm : Form
    {
        private readonly Func<FlightState> _state;
        private readonly Timer _timer;

        private readonly double[] _last = new double[SimVars.Count];
        private readonly bool[] _lastHas = new bool[SimVars.Count];
        private readonly int[] _changes = new int[SimVars.Count];
        // 存 TickCount 的原始 32 位值。用 uint 而不是 long：Environment.TickCount 是 int，
        // 49.7 天翻成负数后，long 会把"翻了"算成"回到过去"，Age 列显示 -4294967.3s。
        // 无符号减法天然处理回绕，与 MainForm、PhaseStabilizer 的做法一致。
        private readonly uint[] _lastChange = new uint[SimVars.Count];
        private bool _primed;

        private string _statText = "";

        /// <summary>供 VS 设计器使用。</summary>
        public SimDataForm()
            : this(null)
        {
        }

        public SimDataForm(Func<FlightState> state)
        {
            _state = state;
            InitializeComponent();

            if (System.ComponentModel.LicenseManager.UsageMode
                == System.ComponentModel.LicenseUsageMode.Designtime) return;

            ApplyLang();
            BuildRows();

            _timer = new Timer { Interval = 250 };
            _timer.Tick += (s, e) => Refresh_();
            _timer.Start();

            Refresh_();
        }

        /// <summary>
        /// 换界面语言。这个窗是**非模态**的（开着它照样操作主窗口），所以主界面切语言时
        /// 它还可能在屏幕上——主窗体会直接调这里，不需要另做刷新机制。
        ///
        /// 行文字（分组名、变量短名）也要在这里重算。1.3.59 已经把 <see cref="BuildRows"/>
        /// 改成走 <c>L.T</c> 与 <c>LabelOf</c>，但那只在打开窗口时算一次；窗口非模态，
        /// 切语言时它还开着，于是列头换了语言、行还是旧的（1.3.65 实机截图）。
        /// 只改文字、不重建行：选中行与滚动位置不动，变化计数也不在行里。
        /// <c>ApplyFilter</c> 必须跟着调——搜索是按行文字匹配的，文字变了命中就会变。
        /// </summary>
        public void ApplyLang()
        {
            Text = L.T("sim.title");
            _chkOnlyAnswered.Text = L.T("sim.onlyanswered");
            _chkOnlyChanged.Text = L.T("sim.onlychanged");
            _chkOnTop.Text = L.T("sim.ontop");

            colGroup.HeaderText = L.T("sim.col.group");
            colVar.HeaderText = L.T("sim.col.var");
            colUnit.HeaderText = L.T("sim.col.unit");
            colValue.HeaderText = L.T("sim.col.value");
            colAnswered.HeaderText = L.T("sim.col.answered");
            colChanges.HeaderText = L.T("sim.col.changes");
            colAge.HeaderText = L.T("sim.col.age");

            _btnCopy.Text = L.T("sim.copy");
            _btnExport.Text = L.T("sim.export");
            _btnClose.Text = L.T("common.close");

            // 状态行会被下一次 250ms 采样覆盖成真实统计；这里只是不让它
            // 在英文界面下停着一句中文。
            _lblStat.Text = L.T("sim.nodata");

            ReloadRowTexts();
            ApplyFilter();
        }

        /// <summary>分组列的文字。Group 存的是语言包 key，显示时才翻（见 <c>SimVars.Var.Group</c>）。</summary>
        private static string GroupText(SimVars.Var v) { return L.T(v.Group); }

        /// <summary>
        /// 变量列的文字。官方判定 / 能力 / 限值量在变量列后标出处，上方搜索框输「官方」
        /// 也能直接把这一批筛出来。格式由语言包 <c>common.namecode</c> 决定：
        /// 中文是「短名（原名）」，英文只有短名——英文下 <c>LabelOf</c> 回退到原名，
        /// 再套一层括号就成 "X (X)" 了。
        /// </summary>
        private static string VarText(SimVars.Var v)
        {
            return L.T("common.namecode", SimVars.LabelOf(v.Name), v.Name)
                 + (v.Official ? L.T("common.official") : "");
        }

        private void BuildRows()
        {
            _grid.Rows.Clear();
            for (int i = 0; i < SimVars.Count; i++)
            {
                var v = SimVars.All[i];
                int r = _grid.Rows.Add(GroupText(v), VarText(v), v.Unit, "—", "—", "0", "—");
                _grid.Rows[r].Tag = i;
                _grid.Rows[r].Cells[colAnswered.Index].Style.ForeColor = UiColors.Warn;
            }
        }

        /// <summary>
        /// 按当前语言重写每一行的分组名与变量短名。只碰这两列，不重建行——
        /// 重建会连选中行、滚动位置一起清掉，而它们与语言无关。
        /// 变量下标从 <c>row.Tag</c> 取，不靠行序：行序在排序/将来加了别的显示逻辑时
        /// 不保证与 <c>SimVars.All</c> 一致（<see cref="ApplyFilter"/> 一直是这么取的）。
        /// </summary>
        private void ReloadRowTexts()
        {
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (!(row.Tag is int)) continue;
                int i = (int)row.Tag;
                if (i < 0 || i >= SimVars.Count) continue;
                SetCell(row, colGroup.Index, GroupText(SimVars.All[i]));
                SetCell(row, colVar.Index, VarText(SimVars.All[i]));
            }
        }

        private void Refresh_()
        {
            var st = _state != null ? _state() : null;
            int answered = 0, changedInView = 0;
            uint now = unchecked((uint)Environment.TickCount);

            foreach (DataGridViewRow row in _grid.Rows)
            {
                // 变量下标一律走 Tag。原来这里用行序配 SimVars.All[i]，
                // 与 ApplyFilter 的取法不一致：列头一旦能排序，值列就会显示成别的变量的值。
                // 1.3.65 起各列 SortMode = NotSortable 断了这条路，取法也统一成 Tag。
                if (!(row.Tag is int)) continue;
                int i = (int)row.Tag;
                if (i < 0 || i >= SimVars.Count) continue;

                var vv = SimVars.All[i];
                bool has = st != null && st.Has(vv.Name);
                double v = has ? st.N(vv.Name) : 0;

                if (has) answered++;

                // 变化计数
                if (_primed && has && _lastHas[i])
                {
                    if (Math.Abs(v - _last[i]) > 1e-6)
                    {
                        _changes[i]++;
                        _lastChange[i] = now;
                    }
                }
                _last[i] = v;
                _lastHas[i] = has;

                if (_changes[i] > 0) changedInView++;

                SetCell(row, colValue.Index, has ? CondEval.Fmt(v) : "—");
                SetCell(row, colAnswered.Index, has ? "✓" : L.T("sim.noanswer"));
                SetCell(row, colChanges.Index, _changes[i].ToString());
                SetCell(row, colAge.Index,
                    _lastChange[i] == 0 ? "—" : (unchecked(now - _lastChange[i]) / 1000.0).ToString("0.0") + "s");

                row.Cells[colAnswered.Index].Style.ForeColor = has
                    ? UiColors.Ok
                    : UiColors.Warn;
                row.Cells[colValue.Index].Style.ForeColor = has
                    ? SystemColors.ControlText
                    : UiColors.Warn;
            }

            _primed = true;
            _statText = L.T("sim.stat", answered, SimVars.Count, SimVars.Count - answered)
                + (st == null ? L.T("sim.stat.noframe") : "");

            _lblStat.Text = _statText;
            HintText.Set(_lblStat, answered == SimVars.Count ? UiColors.Ok : UiColors.Warn);

            ApplyFilter();
        }

        private static void SetCell(DataGridViewRow row, int col, string text)
        {
            string old = Convert.ToString(row.Cells[col].Value);
            if (!string.Equals(old, text, StringComparison.Ordinal)) row.Cells[col].Value = text;
        }

        private void ApplyFilter()
        {
            string key = (_txtSearch.Text ?? "").Trim();
            bool onlyAnswered = _chkOnlyAnswered.Checked;
            bool onlyChanged = _chkOnlyChanged.Checked;

            for (int i = 0; i < _grid.Rows.Count; i++)
            {
                var row = _grid.Rows[i];
                int idx = row.Tag is int ? (int)row.Tag : -1;
                bool show = true;

                if (key.Length > 0)
                {
                    string hay = Convert.ToString(row.Cells[colVar.Index].Value)
                               + " " + Convert.ToString(row.Cells[colGroup.Index].Value);
                    if (hay.IndexOf(key, StringComparison.OrdinalIgnoreCase) < 0) show = false;
                }
                if (show && onlyAnswered && idx >= 0 && !_lastHas[idx]) show = false;
                if (show && onlyChanged && idx >= 0 && _changes[idx] == 0) show = false;

                if (row.Visible != show) row.Visible = show;
            }
        }

        private void Filter_Changed(object sender, EventArgs e) { ApplyFilter(); }

        private void chkOnTop_CheckedChanged(object sender, EventArgs e)
        {
            TopMost = _chkOnTop.Checked;
        }

        private string BuildText()
        {
            var sb = new StringBuilder();
            // 标题本身就是「实时数据 — 数据源」。原来这里是
            // "实时数据 — " + Text.Replace("实时数据 — ", "")，剥掉前缀再拼回来，
            // 等于白绕一圈，而且一翻语言就变成"中文前缀 + 英文标题"。
            sb.AppendLine(Text);
            sb.AppendLine(L.T("sim.time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            sb.AppendLine(_statText);
            sb.AppendLine(new string('-', 78));
            // 表头直接用表格的列头：界面怎么叫，导出就怎么叫，只有一份真源
            sb.AppendLine(string.Join("\t", new string[]
            {
                colGroup.HeaderText, colVar.HeaderText, colUnit.HeaderText, colValue.HeaderText,
                colAnswered.HeaderText, colChanges.HeaderText, colAge.HeaderText
            }));
            for (int i = 0; i < SimVars.Count && i < _grid.Rows.Count; i++)
            {
                var row = _grid.Rows[i];
                sb.AppendLine(string.Join("\t", new string[]
                {
                    Convert.ToString(row.Cells[colGroup.Index].Value),
                    Convert.ToString(row.Cells[colVar.Index].Value),
                    Convert.ToString(row.Cells[colUnit.Index].Value),
                    Convert.ToString(row.Cells[colValue.Index].Value),
                    Convert.ToString(row.Cells[colAnswered.Index].Value),
                    Convert.ToString(row.Cells[colChanges.Index].Value),
                    Convert.ToString(row.Cells[colAge.Index].Value)
                }));
            }
            return sb.ToString();
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            try { Clipboard.SetText(BuildText()); }
            catch (Exception ex) { MessageBox.Show(this, L.T("sim.copyfail", ex.Message), L.T("common.tip.title")); }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = L.T("sim.export.title");
                dlg.Filter = L.T("sim.export.filter");
                dlg.FileName = L.T("sim.export.filename",
                    DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    System.IO.File.WriteAllText(dlg.FileName, BuildText(), Encoding.UTF8);
                    _lblStat.Text = L.T("sim.export.ok", System.IO.Path.GetFileName(dlg.FileName));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, L.T("sim.export.err", ex.Message), L.T("common.tip.title"));
                }
            }
        }

        private void btnClose_Click(object sender, EventArgs e) { Hide(); }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 关窗口只是隐藏：采集一直在跑，再点按钮能立刻看到连续的变化计数
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }
            try { if (_timer != null) _timer.Stop(); } catch (Exception) { }
            base.OnFormClosing(e);
        }

        /// <summary>显示时按记住的位置摆放。</summary>
        public void ApplyBounds(System.Drawing.Rectangle r, bool onTop)
        {
            if (r.Width > 200 && r.Height > 150)
            {
                StartPosition = FormStartPosition.Manual;
                Bounds = r;
            }
            _chkOnTop.Checked = onTop;
            TopMost = onTop;
        }

        public bool IsOnTop { get { return _chkOnTop.Checked; } }
    }
}
