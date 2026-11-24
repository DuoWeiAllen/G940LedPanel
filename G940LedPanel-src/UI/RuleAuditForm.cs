using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using G940LedPanel.Core;

namespace G940LedPanel.UI
{
    /// <summary>
    /// 规则体检清单窗口。只负责把 <see cref="RuleAudit.Finding"/> 列出来，判断全在 Core 里。
    ///
    /// 排序按"该看的先后"：错 → 冲突 → 冗余 → 提示。同一级别内保持 Core 给的系统顺序。
    /// </summary>
    public sealed partial class RuleAuditForm : Form
    {
        private static readonly Color C_Err = UiColors.Err;
        private static readonly Color C_Warn = UiColors.Warn;
        private static readonly Color C_Info = SystemColors.ControlText;

        public RuleAuditForm()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            ApplyLang();
        }

        /// <summary>
        /// Designer 里那份中文是"设计时预览值"，运行时以语言包为准。
        /// 只在构造时调一次：模态窗口开着时主界面被挡住，切不了语言，不需要运行时刷新。
        ///
        /// 1.3.63 补的：这是最后一个没翻的窗体。原先 _lblCount 与悬停提示里的文案
        /// 在 Render 里现拼（已改走 L.T），窗体自身的标题/列头/说明一直是写死的。
        /// </summary>
        private void ApplyLang()
        {
            Text = L.T("raf.title");
            _lblTip.Text = L.T("raf.tip.body");
            _colLevel.Text = L.T("raf.col.level");
            _colSys.Text = L.T("raf.col.sys");
            _colRule.Text = L.T("raf.col.rule");
            _colText.Text = L.T("raf.col.text");
            _btnClose.Text = L.T("common.close");
        }

        /// <summary>铺一遍结果。title 为窗口标题，一般带上"查的是哪个系统"。</summary>
        public void Render(IList<RuleAudit.Finding> findings, string title)
        {
            if (!string.IsNullOrEmpty(title)) this.Text = title;
            if (_list == null) return;

            var list = new List<RuleAudit.Finding>();
            if (findings != null) foreach (var f in findings) if (f != null) list.Add(f);
            list.Sort(delegate (RuleAudit.Finding a, RuleAudit.Finding b) { return Rank(a).CompareTo(Rank(b)); });

            _list.BeginUpdate();
            try
            {
                _list.Items.Clear();
                foreach (var f in list)
                {
                    var row = new ListViewItem(RuleAudit.LevelText(f.Level));
                    row.SubItems.Add(f.SystemName);

                    string rule = f.RuleName ?? "";
                    if (!string.IsNullOrEmpty(f.OtherName)) rule += " × " + f.OtherName;
                    row.SubItems.Add(rule);
                    row.SubItems.Add(f.Text ?? "");

                    row.ForeColor = LevelColor(f.Level);

                    string tip = (f.Text ?? "");
                    if (!string.IsNullOrEmpty(f.Evidence)) tip += L.T("raf.tip.evidence", f.Evidence);
                    tip += L.T("raf.tip.code", f.Code);
                    row.ToolTipText = tip;

                    _list.Items.Add(row);
                }
            }
            finally
            {
                _list.EndUpdate();
            }

            if (list.Count == 0)
            {
                _lblCount.Text = L.T("raf.count.none");
                HintText.Set(_lblCount, C_Info);
            }
            else
            {
                int err = 0;
                foreach (var f in list) if (f.Level == RuleAudit.FLevel.Error) err++;
                _lblCount.Text = L.T("raf.count.some", list.Count, err);
                HintText.Set(_lblCount, err > 0 ? C_Err : C_Warn);
            }
        }

        private static int Rank(RuleAudit.Finding f)
        {
            if (f == null) return 9;
            return (int)f.Level;      // 枚举值就是排序优先级（Error=0 最先显示）
        }

        private static Color LevelColor(RuleAudit.FLevel level)
        {
            switch (level)
            {
                case RuleAudit.FLevel.Error: return C_Err;
                case RuleAudit.FLevel.Conflict: return C_Warn;
                default: return SystemColors.ControlText;
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
