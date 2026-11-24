using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using G940LedPanel.Core;
using G940LedPanel.Effects;

namespace G940LedPanel.UI
{
    /// <summary>
    /// 新增一条告警规则。
    ///
    /// 用户填的是"排在哪一条后面"，不是优先级数字 —— 优先级只是程序内部用来比较大小的，
    /// 让人填数字等于把内部约定摊给用户看；而且填错了不会报错，只会让灯显示得莫名其妙。
    /// 界面在 NewRuleForm.Designer.cs（VS 设计器可直接打开），本文件只放逻辑。
    ///
    /// 1.3.43：系统下拉选了就得生效。原来 SystemKey 返回构造时定死的 _systemKey，
    /// 用户在下拉里换到「自定义」后，规则仍然落到「当前选中行所属系统」——
    /// 表现为"新增规则里建不出自定义分类"。现在读下拉当前项，重名校验与插入位置一并跟随。
    /// </summary>
    public partial class NewRuleForm : Form
    {
        private readonly AlertProfile _profile;
        private readonly string _defaultSystemKey;
        private readonly string _afterId;

        private CondNode _cond;                 // 条件；null = 还没设
        private readonly AlertRule _draft;      // 给条件编辑器看的"临时规则"（未入库，只用来显示标题/取默认值）

        /// <summary>条件编辑器服务（由主窗体提供：它才知道模拟器的实时值）。返回 null = 用户取消。</summary>
        public Func<AlertRule, CondNode, CondNode> EditCondition { get; set; }

        /// <summary>用户当前在下拉里选的系统。下拉换系统后，这个值立刻跟着变。</summary>
        public string SystemKey
        {
            get
            {
                int i = _cboSys.SelectedIndex;
                if (i < 0 || i >= AlertRuleCatalog.Systems.Length) i = 0;
                return AlertRuleCatalog.Systems[i].Key;
            }
        }

        public string RuleName { get { return _txtName.Text.Trim(); } }
        public string AfterId { get; private set; }
        public int Color { get { return _cboColor.SelectedIndex; } }
        public string Effect { get { return AlertRuleCatalog.AllowedEffects[Clamp(_cboEffect.SelectedIndex)]; } }
        public double Speed
        {
            get
            {
                int i = Clamp(_cboSpeed.SelectedIndex);
                return i < AlertRuleCatalog.SpeedSteps.Length ? AlertRuleCatalog.SpeedSteps[i] : 1.0;
            }
        }
        public CondNode Cond { get { return _cond; } }

        public NewRuleForm()
        {
            InitializeComponent();
            ApplyLang();
        }

        /// <summary>Designer 里那份中文是设计时预览值，运行时以语言包为准。</summary>
        private void ApplyLang()
        {
            Text = L.T("newrule.title");
            _lblSys.Text = L.T("newrule.sys");
            _lblName.Text = L.T("newrule.name");
            _lblPos.Text = L.T("newrule.pos");
            _lblColor.Text = L.T("newrule.color");
            _lblEffect.Text = L.T("newrule.effect");
            _lblSpeed.Text = L.T("newrule.speed");
            _lblCondTitle.Text = L.T("newrule.condtitle");
            _btnCond.Text = L.T("newrule.editcond");
            _btnOk.Text = L.T("common.ok");
            _btnCancel.Text = L.T("common.cancel");
        }

        /// <param name="profile">将要改动的那套配置（用来列出"同系统的其它规则"，供选插入点）。</param>
        /// <param name="systemKey">默认系统：取用户当前选中的那一行，只作预填，用户可在下拉里改。</param>
        /// <param name="afterId">默认插入点：选中行本身，即"加在它后面"。</param>
        public NewRuleForm(AlertProfile profile, string systemKey, string afterId) : this()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            _profile = profile;
            _defaultSystemKey = string.IsNullOrEmpty(systemKey) ? AlertRuleCatalog.Systems[0].Key : systemKey;
            _afterId = afterId;

            _draft = new AlertRule
            {
                Id = "draft",
                SystemKey = _defaultSystemKey,
                Name = L.T("newrule.defaultname"),
                Color = 1,
                EffectId = "blink",
                Speed = 1.0
            };

            FillSystems();
            FillColors();
            FillEffects();
            FillSpeeds();
            FillPositions();

            _cboSys.SelectedIndex = IndexOfSystem(_defaultSystemKey);
            _cboColor.SelectedIndex = 1;      // 绿
            // 1.3.70：按名字找，不写死索引。以前写 2，白名单去掉「关」后整体左移一位，
            // 默认效果会从慢闪悄悄变成双闪——这种漂移编译、断言、中文界面全都看不出来。
            _cboEffect.SelectedIndex = Array.IndexOf(AlertRuleCatalog.AllowedEffects, "blink");
            _cboSpeed.SelectedIndex = 1;      // 1.0×

            UpdateCondLabel();
        }

        private static int Clamp(int v) { return v < 0 ? 0 : v; }

        private void FillSystems()
        {
            _cboSys.Items.Clear();
            foreach (var s in AlertRuleCatalog.Systems) _cboSys.Items.Add(TextNames.SysName(s.Key));
        }

        private static int IndexOfSystem(string key)
        {
            for (int i = 0; i < AlertRuleCatalog.Systems.Length; i++)
                if (AlertRuleCatalog.Systems[i].Key == key) return i;
            return 0;
        }

        private void FillColors()
        {
            _cboColor.Items.Clear();
            foreach (var c in TextNames.ColorAll()) _cboColor.Items.Add(c);
        }

        private void FillEffects()
        {            _cboEffect.Items.Clear();
            foreach (var id in AlertRuleCatalog.AllowedEffects)
                _cboEffect.Items.Add(TextNames.Effect(id));
        }

        private void FillSpeeds()
        {
            _cboSpeed.Items.Clear();
            foreach (var s in AlertRuleCatalog.SpeedSteps)
                _cboSpeed.Items.Add(s.ToString("0.0") + "×");
        }

        /// <summary>
        /// 重填"插入位置"：第 0 项是"排在最前"，之后每项都是"排在 X 之后"。
        ///
        /// 方向容易搞反，钉在这里：数字越大越优先、表格按升序排、判定取最大。
        /// 所以"排在最前"= 优先级最低 = 显示在表格最上面 = 最不容易亮；
        /// 想让它优先，要选"排在最后一条之后"。
        ///
        /// 兜底规则不列出来：它不参与插入位置的优先级计算（`PickPriority` 跳过 Fallback），
        /// 自身固定优先级 10，显示在本组第一行。
        /// </summary>
        private void FillPositions()
        {
            _cboPos.Items.Clear();
            _cboPos.Items.Add(L.T("newrule.pos.first"));

            string sys = SystemKey;
            var list = new List<AlertRule>();
            foreach (var r in _profile.AllRules())
            {
                if (r == null || r.Fallback) continue;
                if (r.SystemKey != sys) continue;
                list.Add(r);
            }
            list.Sort(delegate (AlertRule a, AlertRule b)
            {
                int c = _profile.PriorityOf(a).CompareTo(_profile.PriorityOf(b));
                return c != 0 ? c : string.CompareOrdinal(a.Id, b.Id);
            });
            foreach (var r in list) _cboPos.Items.Add(L.T("newrule.pos.after", TextNames.RuleName(r)));

            // 默认选中"排在原来那个插入点之后"
            int sel = 0;
            if (!string.IsNullOrEmpty(_afterId))
            {
                for (int i = 0; i < list.Count; i++)
                    if (list[i].Id == _afterId) { sel = i + 1; break; }
            }
            _cboPos.SelectedIndex = sel;
        }

        private string PickedAfterId()
        {
            int i = _cboPos.SelectedIndex;
            if (i <= 0) return null;      // 排在最前

            string sys = SystemKey;
            var list = new List<AlertRule>();
            foreach (var r in _profile.AllRules())
            {
                if (r == null || r.Fallback || r.SystemKey != sys) continue;
                list.Add(r);
            }
            list.Sort(delegate (AlertRule a, AlertRule b)
            {
                int c = _profile.PriorityOf(a).CompareTo(_profile.PriorityOf(b));
                return c != 0 ? c : string.CompareOrdinal(a.Id, b.Id);
            });

            int idx = i - 1;
            return idx >= 0 && idx < list.Count ? list[idx].Id : null;
        }

        private void cboSys_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 换了系统，原来那套"插入位置"就不适用了，重填一遍
            _draft.SystemKey = SystemKey;
            FillPositions();
        }

        private void btnCond_Click(object sender, EventArgs e)
        {
            if (EditCondition == null) return;
            _draft.Name = RuleName.Length > 0 ? RuleName : L.T("newrule.defaultname");
            _draft.Color = (byte)Color;
            _draft.EffectId = Effect;
            _draft.Speed = Speed;

            var r = EditCondition(_draft, _cond);
            if (r == null) return;        // 取消：保留原条件

            _cond = r;
            UpdateCondLabel();
        }

        private void UpdateCondLabel()
        {
            if (_cond == null)
            {
                HintText.Set(_lblCond, UiColors.Err);
                _lblCond.Text = L.T("newrule.cond.none");
                return;
            }
            HintText.Set(_lblCond, SystemColors.ControlText);
            _lblCond.Text = L.T("newrule.cond.prefix", _cond.Describe());
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (RuleName.Length == 0)
            {
                Warn(L.T("newrule.err.noname"), _txtName);
                return;
            }

            // 同一系统里重名的规则会让人分不清是哪条，当场拦下
            foreach (var r in _profile.AllRules())
            {
                if (r == null || r.SystemKey != SystemKey) continue;
                if (string.Equals(r.Name, RuleName, StringComparison.CurrentCulture))
                {
                    Warn(L.T("newrule.err.dupname", SystemKeyName(), RuleName), _txtName);
                    return;
                }
            }

            if (_cond == null)
            {
                Warn(L.T("newrule.err.nocond"), null);
                return;
            }

            // 1.3.70：删掉自洽校验。颜色是唯一开关，效果只作亮起来后的预设，
            // 任何组合都合法，不会再出现「存进去下次载入被整条丢掉」。
            AfterId = PickedAfterId();
            DialogResult = DialogResult.OK;
            Close();
        }

        private string SystemKeyName()
        {
            return TextNames.SysName(SystemKey);
        }

        private void Warn(string msg, Control focus)
        {
            _lblWarn.Text = msg;
            HintText.Set(_lblWarn, UiColors.Err);
            if (focus != null) { focus.Focus(); }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
