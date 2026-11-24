using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using G940LedPanel.Core;

namespace G940LedPanel.UI
{
    /// <summary>
    /// 条件编辑器：左边结构树（且/或/非/比较），右边改字段，底部实时试算。
    ///
    /// 设计原则（都是踩过的坑）：
    /// 1. 只让用户"换"不让"写" —— 变量、比较符全是下拉，打不出错字；
    /// 2. 改完立刻能看到"这条条件现在成立不成立"，否则用户改完不知道对不对；
    /// 3. 空分组、超层数之类结构问题当场拦下，不让它落到配置文件里；
    /// 4. 只存差量：没改过的规则继续跟随后续版本的内置条件，"还原默认"= 从配置里删掉自己那条。
    ///
    /// 界面在 CondEditorForm.Designer.cs（VS 设计器可直接打开），本文件只放逻辑。
    /// </summary>
    public partial class CondEditorForm : Form
    {
        private const int MaxDepth = CondNode.MaxDepth;

        private readonly AlertRule _rule;
        private readonly Func<FlightState> _state;

        /// <summary>该变量在模拟器里到底取不取得到（由主窗体提供，基于已应答集合）。</summary>
        private readonly Func<string, bool> _varAnswered;

        /// <summary>模拟器是否在线。离线时"能取到"一栏只能显示"—"，不能谎报 ✓/✗。</summary>
        private readonly bool _simConnected;

        /// <summary>手动试算的变量值。没填的变量 = 没有数据（三态里的"未知"）。</summary>
        private readonly Dictionary<string, double> _manual = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        private bool _useManual;
        private List<string> _vars = new List<string>();

        private CondNode _root;
        private CondProbePanel _probe;
        private Timer _probeTimer;
        private bool _suppress;

        /// <summary>
        /// 悬停提示。目前只挂在左侧那句校验结果上：那个标签宽度固定、不换行，
        /// 英文的「⚠ 3 issues: Unknown variable: AIRSPEED INDICATED」比中文长一倍，
        /// 必然被裁掉尾巴。1.3.59 起截断处显示省略号，全文看悬停。
        /// </summary>
        private readonly ToolTip _tip = new ToolTip();

        /// <summary>确定后的条件（点取消时无意义）。</summary>
        public CondNode Result { get; private set; }

        /// <summary>true 表示用户点了"还原初始条件" → 调用方应把该规则的自定义条件删掉。</summary>
        public bool RestoreDefault { get; private set; }

        /// <summary>供 VS 设计器实例化用（设计器只解析 InitializeComponent）。</summary>
        public CondEditorForm()
            : this(null, null, null, null, false)
        {
        }

        public CondEditorForm(AlertRule rule, CondNode current, Func<FlightState> stateGetter)
            : this(rule, current, stateGetter, null, false)
        {
        }

        public CondEditorForm(AlertRule rule, CondNode current, Func<FlightState> stateGetter,
                              Func<string, bool> varAnswered, bool simConnected)
        {
            _rule = rule;
            _state = stateGetter;
            _varAnswered = varAnswered;
            _simConnected = simConnected;

            InitializeComponent();
            ApplyLang();

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            // 试算面板是自定义 UserControl，Designer 里挂上去会让 VS 反序列化失败，
            // 所以留一个空宿主 Panel，运行时装进去。
            _probe = new CondProbePanel();
            _probe.Dock = DockStyle.Fill;
            _probeHolder.Controls.Add(_probe);

            FillComboItems();

            _root = current != null
                ? current.Clone()
                : (rule != null && rule.DefaultCond != null ? rule.DefaultCond.Clone() : NewLeaf());

            BuildTree();
            BuildManualRows();
            RefreshProbe();

            // 试算跟着数据走：模拟器每帧都在变，400ms 刷一次足够看清，也不会抢 UI
            _probeTimer = new Timer { Interval = 400 };
            _probeTimer.Tick += (s, e) => RefreshProbe();
            _probeTimer.Start();
        }

        /// <summary>
        /// Designer 里那份中文是设计时预览值，运行时以语言包为准。
        /// 标题两行依赖传入的规则，所以放在这里而不是 Designer 里。
        /// </summary>
        private void ApplyLang()
        {
            if (_rule != null)
            {
                Text = L.T("cond.form.rule", TextNames.RuleName(_rule));
                _lblTitle.Text = L.T("cond.title.rule", TextNames.RuleDisplay(_rule));
            }
            else
            {
                Text = L.T("cond.form");
                _lblTitle.Text = L.T("cond.title");
            }

            _lblTreeHint.Text = L.T("cond.tree.hint");
            _btnAddLeaf.Text = L.T("cond.btn.addleaf");
            _btnAddAnd.Text = L.T("cond.btn.addand");
            _btnAddOr.Text = L.T("cond.btn.addor");
            _btnNot.Text = L.T("cond.btn.addnot");
            _btnDel.Text = L.T("cond.btn.del");
            _btnToggle.Text = L.T("cond.btn.toggle");

            _grpManual.Text = L.T("cond.manual.group");
            _rdoSim.Text = L.T("cond.src.sim");
            _rdoManual.Text = L.T("cond.src.manual");
            _btnFill.Text = L.T("cond.btn.fill");
            _btnZero.Text = L.T("cond.btn.zero");
            _btnClear.Text = L.T("cond.btn.clear");
            _lblManualHint.Text = L.T("cond.manual.hint");

            colMVar.HeaderText = L.T("cond.col.var");
            colMUnit.HeaderText = L.T("cond.col.unit");
            colMSim.HeaderText = L.T("cond.col.sim");
            colMManual.HeaderText = L.T("cond.col.manual");
            colMAvail.HeaderText = L.T("cond.col.avail");

            _grpLeaf.Text = L.T("cond.leaf.group");
            _lblVar.Text = L.T("cond.leaf.var");
            _lblCmp.Text = L.T("cond.leaf.cmp");
            _lblRight.Text = L.T("cond.leaf.right");
            _lblTo.Text = L.T("cond.leaf.to");

            _btnDefault.Text = L.T("cond.btn.default");
            _btnOk.Text = L.T("common.ok");
            _btnCancel.Text = L.T("common.cancel");
        }

        // ================= 下拉数据 =================

        private List<string> _varNames = new List<string>();

        private void FillComboItems()
        {
            // 左值 / 右值变量：中文名在前，变量名在括号里 —— 用户认中文，排查时又需要原名。
            // 变量表扩到 100+ 后平铺不好找，这里按 Group 稳定分组（组内保持原顺序），
            // 条目前加「[分组]」、官方判定 / 能力 / 限值量加「·官方」标签。
            // 关键：_varNames 与 items 必须一一对应且顺序一致——右值下拉的下标
            // 直接映射到 _varNames，顺序错了会选错值。
            _varNames.Clear();
            var items = new List<string>();

            var groups = new List<string>();
            foreach (var v in SimVars.All)
            {
                if (v.Internal) continue;
                if (!groups.Contains(v.Group)) groups.Add(v.Group);
            }
            foreach (var g in groups)
            {
                foreach (var v in SimVars.All)
                {
                    if (v.Internal || v.Group != g) continue;   // 程序自用变量不给用户当条件用
                    _varNames.Add(v.Name);
                    items.Add(L.T("cond.var.item", L.T(v.Group), SimVars.LabelOf(v.Name), v.Name,
                                  v.Official ? L.T("common.official") : ""));
                }
            }
            foreach (var d in Derived.All)
            {
                _varNames.Add(d);
                items.Add(L.T("cond.derived.item", Derived.LabelOf(d), d));
            }
            _cboVar.Items.AddRange(items.ToArray());
            _cboRight.Items.AddRange(items.ToArray());

            FillCmpOps(null);

            _cboRightKind.Items.Clear();
            _cboRightKind.Items.AddRange(new object[] { L.T("cond.rightkind.value"), L.T("cond.rightkind.var") });
        }

        private static CondNode NewLeaf()
        {
            return new CondNode { Op = "cmp", Var = "SIM ON GROUND", Cmp = "bool" };
        }

        /// <summary>
        /// 按左值类型重填比较符下拉：开关量只给「为真/为假」，数值量只给大小比较。
        /// 1.3.61 之前 9 种一起列，空速这种 knots 量也能选「为真」——选完不报错，
        /// 只是条件永远不成立。这类错不报不崩，只能从源头不让选。
        /// varName 为空表示还没选中任何条件，给全量（保持老行为）。
        /// </summary>
        private void FillCmpOps(string varName)
        {
            string keep = (_cboCmp.SelectedIndex >= 0 && _cboCmp.SelectedIndex < _cboCmpOps.Count)
                ? _cboCmpOps[_cboCmp.SelectedIndex] : null;

            _cboCmpOps.Clear();
            _cboCmp.Items.Clear();

            string[] ops = string.IsNullOrEmpty(varName) ? CondNode.CmpOps : CondNode.CmpOpsFor(varName);
            foreach (var c in ops)
            {
                _cboCmpOps.Add(c);
                _cboCmp.Items.Add(L.T("common.paren", CondNode.CmpText(c), c));
            }

            int i = keep == null ? -1 : _cboCmpOps.IndexOf(keep);
            if (i >= 0) _cboCmp.SelectedIndex = i;
        }

        // ================= 结构树 =================

        private void BuildTree()
        {
            _suppress = true;
            _tree.BeginUpdate();
            try
            {
                _tree.Nodes.Clear();
                var n = _tree.Nodes.Add(NodeText(_root));
                n.Tag = _root;
                AddChildren(n, _root);
                _tree.ExpandAll();
                _tree.SelectedNode = n;
            }
            finally
            {
                _tree.EndUpdate();
                _suppress = false;
            }
            LoadEditorFor(_root);
            UpdateButtons();
            UpdateValidation();
        }

        private void AddChildren(TreeNode parent, CondNode node)
        {
            if (node == null || node.Items == null) return;
            foreach (var c in node.Items)
            {
                if (c == null) continue;
                var n = parent.Nodes.Add(NodeText(c));
                n.Tag = c;
                AddChildren(n, c);
            }
        }

        private static string NodeText(CondNode n)
        {
            if (n == null) return L.T("cond.node.empty");
            if (n.Op == "and") return L.T("cond.node.and") + CountText(n);
            if (n.Op == "or") return L.T("cond.node.or") + CountText(n);
            if (n.Op == "not") return L.T("cond.node.not");
            if (n.Op == "cmp") return n.Describe();
            return L.T("cond.node.unknown");
        }

        private static string CountText(CondNode n)
        {
            int c = n.Items != null ? n.Items.Count : 0;
            return L.T("cond.node.count", c);
        }

        /// <summary>选中项变化后重建右侧编辑器。</summary>
        private void tree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var n = e.Node != null ? e.Node.Tag as CondNode : null;
            LoadEditorFor(n);
            UpdateButtons();
        }

        /// <summary>把某个节点刷新成最新的文字（改完字段不用整棵树重建，免得丢选中）。</summary>
        private void RefreshNodeText(CondNode n)
        {
            var node = FindNode(_tree.Nodes, n);
            if (node != null) node.Text = NodeText(n);
            RefreshProbe();
            UpdateValidation();
        }

        private static TreeNode FindNode(TreeNodeCollection nodes, CondNode target)
        {
            foreach (TreeNode n in nodes)
            {
                if (ReferenceEquals(n.Tag, target)) return n;
                var r = FindNode(n.Nodes, target);
                if (r != null) return r;
            }
            return null;
        }

        // ================= 右侧字段编辑 =================

        private CondNode _editing;

        /// <summary>比较符下拉里此刻的候选（与下拉下标一一对应）。随左值类型变化。</summary>
        private readonly List<string> _cboCmpOps = new List<string>();

        private void LoadEditorFor(CondNode n)
        {
            _editing = n;
            _suppress = true;
            try
            {
                bool leaf = n != null && n.Op == "cmp";
                _grpLeaf.Text = leaf ? L.T("cond.sel.leaf") : L.T(n == null ? "cond.sel.none" : "cond.sel.group");
                SetLeafEnabled(leaf);
                if (!leaf) return;

                int vi = _varNames.IndexOf(n.Var);
                _cboVar.SelectedIndex = vi >= 0 ? vi : -1;

                FillCmpOps(n.Var);
                // 老规则的类型可能对不上（比如数值量配了「为真」），这时下拉留空不选，
                // 靠 Validate 的红字让用户自己改。替他静默改成第一个，
                // 会变成「界面显示 A、保存下去是 B」。
                _cboCmp.SelectedIndex = _cboCmpOps.IndexOf(n.Cmp);

                if (CondNode.IsBoolCmp(n.Cmp))
                {
                    _cboRightKind.SelectedIndex = -1;
                    _cboRight.Visible = false;
                    _nudValue.Visible = false;
                    _nudValue2.Visible = false;
                    _lblTo.Visible = false;
                }
                else if (!string.IsNullOrEmpty(n.Var2))
                {
                    _cboRightKind.SelectedIndex = 1;
                    ApplyRightKind();
                    int vi2 = _varNames.IndexOf(n.Var2);
                    if (vi2 >= 0) _cboRight.SelectedIndex = vi2;
                }
                else
                {
                    _cboRightKind.SelectedIndex = 0;
                    ApplyRightKind();
                    _nudValue.Value = ClampToNud(_nudValue, (decimal)n.Value);
                    _nudValue2.Value = ClampToNud(_nudValue2, (decimal)n.Value2);
                }

                UpdateLeafHint();
            }
            finally
            {
                _suppress = false;
            }
        }

        private static decimal ClampToNud(NumericUpDown nud, decimal v)
        {
            if (v < nud.Minimum) return nud.Minimum;
            if (v > nud.Maximum) return nud.Maximum;
            return v;
        }

        private void SetLeafEnabled(bool on)
        {
            _cboVar.Enabled = on;
            _cboCmp.Enabled = on;
            _cboRightKind.Enabled = on;
            _cboRight.Enabled = on;
            _nudValue.Enabled = on;
            _nudValue2.Enabled = on;
            if (!on)
            {
                _cboRight.Visible = false;
                _nudValue.Visible = false;
                _nudValue2.Visible = false;
                _lblTo.Visible = false;
                _lblUnit.Text = "";
                _lblUnit.Visible = false;
                _lblLeafHint.Text = "";
            }
        }

        /// <summary>按"右值类型"切换显示哪个输入控件。</summary>
        private void ApplyRightKind()
        {
            int kind = _cboRightKind.SelectedIndex;
            bool isBetween = _editing != null && _editing.Cmp == "between";

            _cboRight.Visible = kind == 1;
            _nudValue.Visible = kind == 0;
            _nudValue2.Visible = kind == 0 && isBetween;
            _lblTo.Visible = kind == 0 && isBetween;
            // 单位标签不在这里管：它的显隐依赖「左值是不是布尔量」，
            // 由 UpdateLeafHint 一处决定（调用方总是紧跟着调它）。
        }

        private void cboVar_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suppress || _editing == null || !_editing.IsLeaf) return;
            int i = _cboVar.SelectedIndex;
            if (i < 0 || i >= _varNames.Count) return;
            _editing.Var = _varNames[i];

            // 换了变量就可能换了类型：开关量 ↔ 数值量。原比较符在新候选里没有时，
            // 重置成新类型的第一个，否则会留下「空速 大于 真」这种自相矛盾的条件。
            var ops = CondNode.CmpOpsFor(_editing.Var);
            if (Array.IndexOf(ops, _editing.Cmp) < 0)
            {
                _editing.Cmp = ops[0];
                _editing.Var2 = null;
                _editing.ThresholdKey = null;
                LoadEditorFor(_editing);      // 右值控件形态也跟着变
                RefreshNodeText(_editing);
                return;
            }

            FillCmpOps(_editing.Var);
            UpdateLeafHint();
            RefreshNodeText(_editing);
        }

        private void cboCmp_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suppress || _editing == null || !_editing.IsLeaf) return;
            int i = _cboCmp.SelectedIndex;
            if (i < 0 || i >= _cboCmpOps.Count) return;
            _editing.Cmp = _cboCmpOps[i];

            if (CondNode.IsBoolCmp(_editing.Cmp))
            {
                _editing.ThresholdKey = null;
                _editing.Var2 = null;
                _editing.Value = 0;
                _editing.Value2 = 0;
            }
            else if (_editing.ThresholdKey == null && _editing.Var2 == null && _cboRightKind.SelectedIndex < 0)
            {
                _cboRightKind.SelectedIndex = 0;     // 从布尔改成数值比较时，默认用固定数值
            }

            LoadEditorFor(_editing);
            RefreshNodeText(_editing);
        }

        private void cboRightKind_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suppress || _editing == null || !_editing.IsLeaf) return;
            int kind = _cboRightKind.SelectedIndex;

            if (kind == 0)
            {
                _editing.ThresholdKey = null;
                _editing.Var2 = null;
                _editing.Value = (double)_nudValue.Value;
                _editing.Value2 = (double)_nudValue2.Value;
            }
            else if (kind == 1 && _varNames.Count > 0)
            {
                _editing.ThresholdKey = null;
                _editing.Var2 = _varNames[0];
                _cboRight.SelectedIndex = 0;
            }

            ApplyRightKind();
            UpdateLeafHint();
            RefreshNodeText(_editing);
        }

        private void cboRight_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suppress || _editing == null || !_editing.IsLeaf) return;
            int i = _cboRight.SelectedIndex;
            if (i < 0) return;

            if (_cboRightKind.SelectedIndex == 1)
            {
                if (i < _varNames.Count) _editing.Var2 = _varNames[i];
            }

            UpdateLeafHint();
            RefreshNodeText(_editing);
        }

        private void nudValue_ValueChanged(object sender, EventArgs e)
        {
            if (_suppress || _editing == null || !_editing.IsLeaf) return;
            if (_cboRightKind.SelectedIndex != 1) return;
            _editing.Value = (double)_nudValue.Value;
            _editing.Value2 = (double)_nudValue2.Value;
            RefreshNodeText(_editing);
        }

        /// <summary>把"这个变量是什么、当前值多少、单位是什么"写清楚，避免用户瞎猜。</summary>
        private void UpdateLeafHint()
        {
            if (_editing == null || !_editing.IsLeaf)
            {
                _lblLeafHint.Text = "";
                _lblUnit.Text = "";
                _lblUnit.Visible = false;
                return;
            }

            string unit = SimVars.UnitOf(_editing.Var);

            // 单位标签只服务「右值是固定数值」这一种情况：告诉用户阈值该填什么单位。
            // 1.3.61 之前它的显隐和文字分散在四处（ShowNode 的 bool 分支、SetLeafEnabled、
            // ApplyRightKind、这里），后写的覆盖先写的，于是 bool 比较下它照样露出来，
            // 还挂着左值的单位——用户会以为是右值的单位。现在只在这一处决定。
            bool numeric = !CondNode.IsBoolCmp(_editing.Cmp) && _cboRightKind.SelectedIndex == 0;
            _lblUnit.Text = numeric ? unit : "";
            _lblUnit.Visible = numeric;

            string shown = null;
            double v;
            var st = CurrentState();
            if (st != null && CondEval.TryValue(_editing.Var, st, out v)) shown = CondEval.Fmt(v);

            string extra = "";
            if (!string.IsNullOrEmpty(_editing.Var2))
                extra = L.T("cond.leaf.varcmp");

            // 取不到值就只说「无数据」，不拼单位：中文「无数据 knots」、英文 "no data knots"
            // 都是病句。bool 量也不拼（单位串就是 "bool"，会印出 "true bool"）。
            string cur;
            if (shown == null) cur = L.T("common.nodata");
            else if (unit.Length > 0 && !CondNode.IsBoolCmp(_editing.Cmp)) cur = shown + " " + unit;
            else cur = shown;

            _lblLeafHint.Text = L.T("cond.leaf.cur", cur)
                              + (extra.Length > 0 ? "\r\n" + extra : "");
        }

        // ================= 结构操作 =================

        private void UpdateButtons()
        {
            var sel = _tree.SelectedNode != null ? _tree.SelectedNode.Tag as CondNode : null;
            bool has = sel != null;
            bool isGroup = has && sel.IsGroup;

            _btnAddLeaf.Enabled = has && DepthOf(sel) < MaxDepth;
            _btnAddAnd.Enabled = has && DepthOf(sel) < MaxDepth - 1;
            _btnAddOr.Enabled = has && DepthOf(sel) < MaxDepth - 1;
            _btnNot.Enabled = has && DepthOf(sel) < MaxDepth - 1;
            _btnDel.Enabled = has && !ReferenceEquals(sel, _root);
            _btnToggle.Enabled = isGroup && (sel.Op == "and" || sel.Op == "or");
            _btnOk.Enabled = _tree.Nodes.Count > 0;
        }

        private int DepthOf(CondNode target)
        {
            return DepthOf(_root, target, 1);
        }

        private static int DepthOf(CondNode node, CondNode target, int depth)
        {
            if (node == null) return 0;
            if (ReferenceEquals(node, target)) return depth;
            if (node.Items == null) return 0;
            foreach (var c in node.Items)
            {
                int d = DepthOf(c, target, depth + 1);
                if (d > 0) return d;
            }
            return 0;
        }

        /// <summary>新加的项挂到哪：选中的是分组就挂它下面，是叶子就挂它父分组下面。</summary>
        private CondNode TargetGroup()
        {
            var sel = _tree.SelectedNode != null ? _tree.SelectedNode.Tag as CondNode : null;
            if (sel == null) return _root;
            if (sel.IsGroup) return sel;

            var tn = FindNode(_tree.Nodes, sel);
            if (tn != null && tn.Parent != null && tn.Parent.Tag is CondNode)
                return tn.Parent.Tag as CondNode;
            return _root;
        }

        private void btnAddLeaf_Click(object sender, EventArgs e) => AddChild(NewLeaf());

        private void btnAddAnd_Click(object sender, EventArgs e) => AddChild(NewGroup("and"));

        private void btnAddOr_Click(object sender, EventArgs e) => AddChild(NewGroup("or"));

        private void btnNot_Click(object sender, EventArgs e)
        {
            var g = new CondNode { Op = "not", Items = new List<CondNode>() };
            g.Items.Add(NewLeaf());
            AddChild(g);
        }

        private static CondNode NewGroup(string op)
        {
            var g = new CondNode { Op = op, Items = new List<CondNode>() };
            g.Items.Add(NewLeaf());
            return g;
        }

        private void AddChild(CondNode child)
        {
            CondNode parent = TargetGroup();

            // 根是单条比较：不能往"比较"里塞子项，先升级成"且"组
            if (parent != null && parent.IsLeaf)
            {
                var old = parent.Clone();
                parent.Op = "and";
                parent.Items = new List<CondNode>();
                parent.Items.Add(old);
                parent.Var = null; parent.Var2 = null; parent.Cmp = null;
                parent.Value = 0; parent.Value2 = 0; parent.ThresholdKey = null;
            }

            if (parent == null) parent = _root;
            if (parent.Items == null) parent.Items = new List<CondNode>();

            parent.Items.Add(child);

            BuildTree();
            var tn = FindNode(_tree.Nodes, child);
            if (tn != null) _tree.SelectedNode = tn;
        }

        private void btnDel_Click(object sender, EventArgs e)
        {
            var sel = _tree.SelectedNode != null ? _tree.SelectedNode.Tag as CondNode : null;
            if (sel == null || ReferenceEquals(sel, _root)) return;

            var tn = FindNode(_tree.Nodes, sel);
            var parent = tn != null && tn.Parent != null ? tn.Parent.Tag as CondNode : null;
            if (parent == null || parent.Items == null) return;

            parent.Items.Remove(sel);

            // "非"必须有且只有一条
            if (parent.Op == "not" && parent.Items.Count == 0)
            {
                // 把空的"非"也拿掉
                var ptn = FindNode(_tree.Nodes, parent);
                var gp = ptn != null && ptn.Parent != null ? ptn.Parent.Tag as CondNode : null;
                if (gp != null && gp.Items != null && !ReferenceEquals(parent, _root)) gp.Items.Remove(parent);
            }

            BuildTree();
        }

        private void btnToggle_Click(object sender, EventArgs e)
        {
            var sel = _tree.SelectedNode != null ? _tree.SelectedNode.Tag as CondNode : null;
            if (sel == null || !(sel.Op == "and" || sel.Op == "or")) return;
            sel.Op = sel.Op == "and" ? "or" : "and";
            BuildTree();
        }

        private void btnDefault_Click(object sender, EventArgs e)
        {
            if (_rule == null || _rule.DefaultCond == null) return;
            if (MessageBox.Show(this,
                    L.T("cond.default.ask"),
                    L.T("cond.btn.default"), MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;

            _root = _rule.DefaultCond.Clone();
            RestoreDefault = true;
            BuildTree();
        }

        // ================= 校验与试算 =================

        private void UpdateValidation()
        {
            var errs = _root != null ? _root.Validate() : new List<string> { L.T("cond.err.empty") };

            if (errs.Count == 0)
            {
                _lblTreeHint.Text = L.T("cond.tree.hint");
                HintText.Set(_lblTreeHint, SystemColors.ControlText);
                _btnOk.Enabled = true;
                return;
            }

            _lblTreeHint.Text = L.T("cond.err.summary", errs.Count, errs[0]);
            _tip.SetToolTip(_lblTreeHint, _lblTreeHint.Text);   // 标签宽度固定，英文下会被裁，悬停看全文
            HintText.Set(_lblTreeHint, UiColors.Warn);
            _btnOk.Enabled = false;
        }

        /// <summary>
        /// 试算用的那一帧。手动模式 → 用手填的值现造一帧；实时模式 → 拿模拟器最新帧。
        /// 两者走的是同一个 CondEval，所以"手动能成立"就等于"真机满足时也会成立"。
        /// </summary>
        private FlightState CurrentState()
        {
            if (_useManual) return ManualProbe.Build(_manual);
            return _state != null ? _state() : null;
        }

        private void RefreshProbe()
        {
            if (_probe == null) return;

            // 条件结构变了，用到的变量集合也会变，表要跟着重建
            var vars = ManualProbe.VarsUsed(_root);
            if (!SameVars(vars, _vars)) BuildManualRows();

            // 标的是数据源模式。没连模拟器时不能只说「实时」——下面紧接着一行
            // 「Simulator not connected」，两句并列看像自相矛盾。所以加个「（未连接）」。
            _probe.SourceNote = _useManual ? L.T("cond.src.manual")
                             : (_simConnected ? L.T("cond.src.sim") : L.T("cond.src.simoff"));
            _probe.Render(_root, CurrentState());
            UpdateLeafHint();
        }

        private static bool SameVars(List<string> a, List<string> b)
        {
            if (a == null || b == null || a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
                if (!string.Equals(a[i], b[i], StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }

        // ================= 手动试算 =================

        private void BuildManualRows()
        {
            _vars = ManualProbe.VarsUsed(_root);

            _suppress = true;
            try
            {
                _dgvVar.Rows.Clear();
                var st = _state != null ? _state() : null;

                for (int i = 0; i < _vars.Count; i++)
                {
                    string name = _vars[i];
                    string simText = (st != null && st.Has(name)) ? CondEval.Fmt(st.N(name)) : "—";

                    double mv;
                    bool hasManual = _manual.TryGetValue(name, out mv);

                    int idx = _dgvVar.Rows.Add(
                        L.T("common.namecode", SimVars.LabelOf(name), name),
                        SimVars.UnitOf(name),
                        simText,
                        hasManual ? mv.ToString("0.###") : "",
                        AvailabilityText(name));

                    _dgvVar.Rows[idx].Tag = name;
                    _dgvVar.Rows[idx].Cells[colMManual.Index].ReadOnly = !_useManual;
                    _dgvVar.Rows[idx].Cells[colMAvail.Index].Style.ForeColor =
                        AvailableNow(name) ? UiColors.Ok : UiColors.Warn;
                }
            }
            finally
            {
                _suppress = false;
            }

            UpdateManualHint();
        }

        /// <summary>
        /// 这个变量此刻能不能取到。1.3.59 之前是用「显示文字的首字符是不是 ✓」判的，
        /// 一旦文案改了（或切成英文）判断就整片失效，颜色全变橙。现在按状态判，文字只管显示。
        /// </summary>
        private bool AvailableNow(string name)
        {
            return _simConnected && _varAnswered != null && _varAnswered(name);
        }

        private string AvailabilityText(string name)
        {
            if (!_simConnected) return L.T("cond.avail.noconn");
            return AvailableNow(name) ? L.T("cond.avail.yes") : L.T("cond.avail.no");
        }

        private void UpdateManualHint()
        {
            if (_lblManualHint == null) return;
            if (!_simConnected)
            {
                _lblManualHint.Text = L.T("cond.manual.offline");
                return;
            }
            _lblManualHint.Text = L.T("cond.manual.hint2");
        }

        private void rdoSource_CheckedChanged(object sender, EventArgs e)
        {
            if (_suppress) return;
            bool manual = _rdoManual.Checked;
            if (manual == _useManual) return;
            _useManual = manual;

            for (int i = 0; i < _dgvVar.Rows.Count; i++)
                _dgvVar.Rows[i].Cells[colMManual.Index].ReadOnly = !_useManual;

            RefreshProbe();
        }

        private void btnFill_Click(object sender, EventArgs e)
        {
            var st = _state != null ? _state() : null;
            if (st == null)
            {
                MessageBox.Show(this, L.T("cond.fill.empty"),
                    L.T("common.tip.title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ManualProbe.CopyFrom(st, _vars, _manual);
            if (!_useManual) _rdoManual.Checked = true;   // 切过去会触发一次刷新
            BuildManualRows();
            RefreshProbe();
        }

        private void btnZero_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < _vars.Count; i++) _manual[_vars[i]] = 0;
            if (!_useManual) _rdoManual.Checked = true;
            BuildManualRows();
            RefreshProbe();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            _manual.Clear();
            BuildManualRows();
            RefreshProbe();
        }

        private void dgvVar_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (_suppress) return;
            if (e.ColumnIndex != colMManual.Index || e.RowIndex < 0) return;

            var row = _dgvVar.Rows[e.RowIndex];
            string name = row.Tag as string;
            if (string.IsNullOrEmpty(name)) return;

            string text = Convert.ToString(row.Cells[colMManual.Index].Value).Trim();
            if (text.Length == 0)
            {
                _manual.Remove(name);
                RefreshProbe();
                return;
            }

            double v;
            if (!double.TryParse(text, out v))
            {
                double old;
                row.Cells[colMManual.Index].Value = _manual.TryGetValue(name, out old) ? old.ToString("0.###") : "";
                return;
            }
            _manual[name] = v;
            RefreshProbe();
        }

        private void CondEditorForm_Shown(object sender, EventArgs e)
        {
            RefreshProbe();
        }

        // ================= 收尾 =================

        private void btnOk_Click(object sender, EventArgs e)
        {
            var errs = _root.Validate();
            if (errs.Count > 0)
            {
                MessageBox.Show(this, L.T("cond.err.cannot", string.Join("\r\n· ", errs.ToArray())),
                    L.T("cond.err.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Result = _root.Clone();
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_probeTimer != null) { _probeTimer.Stop(); _probeTimer.Dispose(); _probeTimer = null; }
            if (_tip != null) _tip.Dispose();
            base.OnFormClosed(e);
        }
    }
}
