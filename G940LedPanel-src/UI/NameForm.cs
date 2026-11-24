using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using G940LedPanel.Core;

namespace G940LedPanel.UI
{
    /// <summary>
    /// 要用户填一个名字的小对话框（新建 / 另存为 / 重命名 共用）。
    ///
    /// 用自绘的小窗体而不是 Microsoft.VisualBasic.InputBox：
    /// 输入框那边是中英混排的老式界面，和程序其它窗口不一致，也没法做"重名当场提示"。
    /// 界面在 NameForm.Designer.cs（VS 设计器可直接打开），本文件只放逻辑。
    /// </summary>
    public partial class NameForm : Form
    {
        /// <summary>用户填的名字（去掉首尾空白）。</summary>
        public string Value
        {
            get { return _txt.Text.Trim(); }
        }

        /// <summary>校验回调：同一套配置里不能重名。返回 null 表示通过，否则返回提示语。</summary>
        public Func<string, string> CheckName { get; set; }

        public NameForm()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            ApplyLang();
        }

        public NameForm(string title, string prompt, string initial) : this()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            Text = title;
            _lblPrompt.Text = prompt;
            _txt.Text = initial ?? "";
        }

        /// <summary>
        /// Designer 里那份中文是"设计时预览值"，运行时以语言包为准。
        /// 标题与提示语通常由调用方（<see cref="Ask"/>）传入当前语言的文字，这里只兜默认值。
        /// </summary>
        private void ApplyLang()
        {
            Text = L.T("name.form");
            _lblPrompt.Text = L.T("name.prompt");
            _lblHint.Text = L.T("name.hint");
            _btnOk.Text = L.T("common.ok");
            _btnCancel.Text = L.T("common.cancel");
        }

        /// <summary>
        /// 弹一次，返回填好的名字；取消返回 null。
        /// </summary>
        public static string Ask(IWin32Window owner, string title, string prompt,
                                 string initial, Func<string, string> validate)
        {
            using (var f = new NameForm(title, prompt, initial))
            {
                f.CheckName = validate;
                return f.ShowDialog(owner) == DialogResult.OK ? f.Value : null;
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            string v = Value;
            // 判据只认 ProfileStore 那一张表：这里放过、存盘时再被拒，用户刚输入的整段内容就白打了。
            string whyName;
            if (!ProfileStore.IsNameUsable(v, out whyName))
            {
                Warn(whyName);
                return;
            }

            if (CheckName != null)
            {
                string why = CheckName(v);
                if (why != null) { Warn(why); return; }
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void Warn(string msg)
        {
            HintText.Set(_lblHint, UiColors.Err);
            _lblHint.Text = msg;
            _txt.Focus();
            _txt.SelectAll();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        // 回车=确定，Esc=取消
        private void txt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; btnOk_Click(sender, e); }
            else if (e.KeyCode == Keys.Escape) { e.SuppressKeyPress = true; btnCancel_Click(sender, e); }
        }
    }
}
