using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using G940LedPanel.Core;

namespace G940LedPanel.UI
{
    /// <summary>
    /// 点某颗灯弹出的单灯配置框：列出全部系统，挑一个挂到这颗灯上。
    ///
    /// 1.3.43：不再复用 LedMapForm（那是 8 行固定灯位表，灯位是行坐标、不是可选项，
    /// "点了 P3 再高亮第 3 行"没有下一步动作可做，和点击交互矛盾）。这里灯位由点击带入，
    /// 框里只列系统；候选项由 Core 的 SystemLedMap.OptionsForLed() 生成，可离线断言。
    ///
    /// 界面在 LedAssignForm.Designer.cs（VS 设计器可直接打开），本文件只放逻辑。
    /// </summary>
    public partial class LedAssignForm : Form
    {
        private readonly int[] _map;
        private readonly int _led;
        private readonly List<SystemLedMap.LedOption> _opts;

        /// <summary>确定后返回的新映射（副本，取消时不会被读到）。</summary>
        public int[] Map { get { return SystemLedMap.Clone(_map); } }

        /// <summary>用户是否真的改了；没改就不必走一遍落盘与重铺表格。</summary>
        public bool Changed { get; private set; }

        /// <summary>供 VS 设计器实例化用。</summary>
        public LedAssignForm() : this(SystemLedMap.Default(), 0) { }

        /// <param name="map">当前灯位映射（内部会克隆并补齐，不改动调用方的数组）。</param>
        /// <param name="led">被点的那颗灯，0..7。</param>
        public LedAssignForm(int[] map, int led)
        {
            _map = SystemLedMap.Pad(map);
            _led = led;
            InitializeComponent();

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            ApplyLang();

            int cur = SystemLedMap.SysOf(_map, led);
            string curName = cur >= 0 && cur < AlertRuleCatalog.Systems.Length
                ? TextNames.SysName(AlertRuleCatalog.Systems[cur].Key)
                : L.T("led.opt.empty");
            _lblTitle.Text = L.T("led.title", SystemLedMap.LedName(led), curName);

            _opts = SystemLedMap.OptionsForLed(_map, led);
            int sel = 0;
            foreach (var o in _opts)
            {
                _list.Items.Add(OptionText(o));
                if (o.IsCurrent) sel = _list.Items.Count - 1;
            }
            _list.SelectedIndex = sel;
        }

        /// <summary>
        /// Designer 里那份中文是"设计时预览值"，运行时以语言包为准。
        /// 只在构造时调一次：模态窗口开着时主界面被挡住，切不了语言，不需要运行时刷新。
        /// </summary>
        private void ApplyLang()
        {
            Text = L.T("led.form");
            _lblHint.Text = L.T("led.hint");
            _btnOk.Text = L.T("common.ok");
            _btnCancel.Text = L.T("common.cancel");
        }

        /// <summary>
        /// 一条候选项的显示文字。1.3.58 起由这里拼，Core 只给 key 与归属类别。
        /// </summary>
        private static string OptionText(SystemLedMap.LedOption o)
        {
            if (o.Tag == SystemLedMap.LedTag.Empty) return L.T("led.opt.empty");

            string name = o.SystemKey != null
                ? TextNames.SysName(o.SystemKey)
                : L.T("led.opt.sysn", o.SysIndex);

            string tag;
            if (o.Tag == SystemLedMap.LedTag.Current) tag = L.T("led.tag.current");
            else if (o.Tag == SystemLedMap.LedTag.NoLedYet) tag = L.T("led.tag.noled");
            else tag = L.T("led.tag.other", SystemLedMap.LedName(o.WhereLed));

            return name + " · " + tag;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            int pick = _list.SelectedIndex;
            if (pick < 0 || pick >= _opts.Count) return;

            var o = _opts[pick];
            if (o.SysIndex == SystemLedMap.NoLed)
                SystemLedMap.ClearLed(_map, _led);
            else
                SystemLedMap.Assign(_map, o.SysIndex, _led);

            Changed = true;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void list_DoubleClick(object sender, EventArgs e)
        {
            if (_list.SelectedIndex >= 0) btnOk_Click(sender, e);
        }
    }
}
