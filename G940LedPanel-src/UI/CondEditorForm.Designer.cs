namespace G940LedPanel.UI
{
    /// <summary>
    /// 设计器文件 —— 条件编辑器。左边是条件结构树，右边上半是叶子的字段编辑，
    /// 右下半是试算面板。所有位置/尺寸都可以在 VS 设计器里直接拖。
    /// </summary>
    partial class CondEditorForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label _lblTitle;
        private System.Windows.Forms.Panel _leftPnl;
        private System.Windows.Forms.Label _lblTreeHint;
        private System.Windows.Forms.FlowLayoutPanel _flowTree;
        private System.Windows.Forms.Button _btnAddLeaf;
        private System.Windows.Forms.Button _btnAddAnd;
        private System.Windows.Forms.Button _btnAddOr;
        private System.Windows.Forms.Button _btnNot;
        private System.Windows.Forms.Button _btnDel;
        private System.Windows.Forms.Button _btnToggle;
        private System.Windows.Forms.TreeView _tree;
        private System.Windows.Forms.Panel _rightPnl;
        private System.Windows.Forms.GroupBox _grpLeaf;
        private System.Windows.Forms.Label _lblVar;
        private System.Windows.Forms.ComboBox _cboVar;
        private System.Windows.Forms.Label _lblCmp;
        private System.Windows.Forms.ComboBox _cboCmp;
        private System.Windows.Forms.Label _lblRight;
        private System.Windows.Forms.ComboBox _cboRightKind;
        private System.Windows.Forms.ComboBox _cboRight;
        private System.Windows.Forms.NumericUpDown _nudValue;
        private System.Windows.Forms.Label _lblUnit;
        private System.Windows.Forms.Label _lblTo;
        private System.Windows.Forms.NumericUpDown _nudValue2;
        private System.Windows.Forms.Label _lblLeafHint;
        private System.Windows.Forms.Panel _probeHolder;

        // 试算数据源 + 手动变量表（脱离模拟器也能验证条件）
        private System.Windows.Forms.GroupBox _grpManual;
        private System.Windows.Forms.RadioButton _rdoSim;
        private System.Windows.Forms.RadioButton _rdoManual;
        private System.Windows.Forms.Button _btnFill;
        private System.Windows.Forms.Button _btnZero;
        private System.Windows.Forms.Button _btnClear;
        private System.Windows.Forms.Label _lblManualHint;
        private System.Windows.Forms.DataGridView _dgvVar;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMVar;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMUnit;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMSim;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMManual;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMAvail;
        private System.Windows.Forms.TableLayoutPanel _foot;
        private System.Windows.Forms.Button _btnDefault;
        private System.Windows.Forms.Button _btnOk;
        private System.Windows.Forms.Button _btnCancel;

        /// <summary>清理正在使用的资源。</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// 设计器支持所需的方法 —— 不要用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this._lblTitle = new System.Windows.Forms.Label();
            this._leftPnl = new System.Windows.Forms.Panel();
            this._lblTreeHint = new System.Windows.Forms.Label();
            this._flowTree = new System.Windows.Forms.FlowLayoutPanel();
            this._btnAddLeaf = new System.Windows.Forms.Button();
            this._btnAddAnd = new System.Windows.Forms.Button();
            this._btnAddOr = new System.Windows.Forms.Button();
            this._btnNot = new System.Windows.Forms.Button();
            this._btnDel = new System.Windows.Forms.Button();
            this._btnToggle = new System.Windows.Forms.Button();
            this._tree = new System.Windows.Forms.TreeView();
            this._rightPnl = new System.Windows.Forms.Panel();
            this._grpLeaf = new System.Windows.Forms.GroupBox();
            this._probeHolder = new System.Windows.Forms.Panel();
            this._grpManual = new System.Windows.Forms.GroupBox();
            this._rdoSim = new System.Windows.Forms.RadioButton();
            this._rdoManual = new System.Windows.Forms.RadioButton();
            this._btnFill = new System.Windows.Forms.Button();
            this._btnZero = new System.Windows.Forms.Button();
            this._btnClear = new System.Windows.Forms.Button();
            this._lblManualHint = new System.Windows.Forms.Label();
            this._dgvVar = new System.Windows.Forms.DataGridView();
            this.colMVar = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMUnit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMSim = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMManual = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMAvail = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._lblVar = new System.Windows.Forms.Label();
            this._cboVar = new System.Windows.Forms.ComboBox();
            this._lblCmp = new System.Windows.Forms.Label();
            this._cboCmp = new System.Windows.Forms.ComboBox();
            this._lblRight = new System.Windows.Forms.Label();
            this._cboRightKind = new System.Windows.Forms.ComboBox();
            this._cboRight = new System.Windows.Forms.ComboBox();
            this._nudValue = new System.Windows.Forms.NumericUpDown();
            this._lblUnit = new System.Windows.Forms.Label();
            this._lblTo = new System.Windows.Forms.Label();
            this._nudValue2 = new System.Windows.Forms.NumericUpDown();
            this._lblLeafHint = new System.Windows.Forms.Label();
            this._foot = new System.Windows.Forms.TableLayoutPanel();
            this._btnDefault = new System.Windows.Forms.Button();
            this._btnOk = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this._leftPnl.SuspendLayout();
            this._flowTree.SuspendLayout();
            this._rightPnl.SuspendLayout();
            this._grpLeaf.SuspendLayout();
            this._grpManual.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._dgvVar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nudValue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nudValue2)).BeginInit();
            this._foot.SuspendLayout();
            this.SuspendLayout();
            // 
            // _lblTitle
            // 
            this._lblTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this._lblTitle.Location = new System.Drawing.Point(0, 0);
            this._lblTitle.Name = "_lblTitle";
            this._lblTitle.Padding = new System.Windows.Forms.Padding(14, 0, 8, 0);
            this._lblTitle.Size = new System.Drawing.Size(1000, 38);
            this._lblTitle.TabIndex = 0;
            this._lblTitle.Text = "触发条件";
            this._lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _leftPnl
            // 
            this._leftPnl.Controls.Add(this._tree);
            this._leftPnl.Controls.Add(this._flowTree);
            this._leftPnl.Controls.Add(this._lblTreeHint);
            this._leftPnl.Dock = System.Windows.Forms.DockStyle.Left;
            this._leftPnl.Location = new System.Drawing.Point(0, 38);
            this._leftPnl.Name = "_leftPnl";
            this._leftPnl.Size = new System.Drawing.Size(432, 674);
            this._leftPnl.TabIndex = 1;
            // 
            // _lblTreeHint
            // 
            this._lblTreeHint.AutoEllipsis = true;
            this._lblTreeHint.Dock = System.Windows.Forms.DockStyle.Top;
            this._lblTreeHint.Location = new System.Drawing.Point(0, 0);
            this._lblTreeHint.Name = "_lblTreeHint";
            this._lblTreeHint.Padding = new System.Windows.Forms.Padding(14, 0, 8, 0);
            this._lblTreeHint.Size = new System.Drawing.Size(432, 24);
            this._lblTreeHint.TabIndex = 0;
            this._lblTreeHint.Text = "条件结构（选中一项后在右侧改）";
            this._lblTreeHint.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _flowTree
            // 
            this._flowTree.Controls.Add(this._btnAddLeaf);
            this._flowTree.Controls.Add(this._btnAddAnd);
            this._flowTree.Controls.Add(this._btnAddOr);
            this._flowTree.Controls.Add(this._btnNot);
            this._flowTree.Controls.Add(this._btnDel);
            this._flowTree.Controls.Add(this._btnToggle);
            this._flowTree.Dock = System.Windows.Forms.DockStyle.Top;
            this._flowTree.Location = new System.Drawing.Point(0, 24);
            this._flowTree.Name = "_flowTree";
            this._flowTree.Padding = new System.Windows.Forms.Padding(12, 4, 0, 0);
            this._flowTree.Size = new System.Drawing.Size(432, 70);
            this._flowTree.TabIndex = 1;
            this._flowTree.WrapContents = true;
            // 
            // _btnAddLeaf
            // 
            this._btnAddLeaf.Location = new System.Drawing.Point(12, 4);
            this._btnAddLeaf.Margin = new System.Windows.Forms.Padding(0, 0, 4, 4);
            this._btnAddLeaf.Name = "_btnAddLeaf";
            this._btnAddLeaf.Size = new System.Drawing.Size(96, 28);
            this._btnAddLeaf.TabIndex = 0;
            this._btnAddLeaf.Text = "加条件";
            this._btnAddLeaf.UseVisualStyleBackColor = true;
            this._btnAddLeaf.Click += new System.EventHandler(this.btnAddLeaf_Click);
            // 
            // _btnAddAnd
            // 
            this._btnAddAnd.Location = new System.Drawing.Point(112, 4);
            this._btnAddAnd.Margin = new System.Windows.Forms.Padding(0, 0, 4, 4);
            this._btnAddAnd.Name = "_btnAddAnd";
            this._btnAddAnd.Size = new System.Drawing.Size(96, 28);
            this._btnAddAnd.TabIndex = 1;
            this._btnAddAnd.Text = "加“且”组";
            this._btnAddAnd.UseVisualStyleBackColor = true;
            this._btnAddAnd.Click += new System.EventHandler(this.btnAddAnd_Click);
            // 
            // _btnAddOr
            // 
            this._btnAddOr.Location = new System.Drawing.Point(212, 4);
            this._btnAddOr.Margin = new System.Windows.Forms.Padding(0, 0, 4, 4);
            this._btnAddOr.Name = "_btnAddOr";
            this._btnAddOr.Size = new System.Drawing.Size(96, 28);
            this._btnAddOr.TabIndex = 2;
            this._btnAddOr.Text = "加“或”组";
            this._btnAddOr.UseVisualStyleBackColor = true;
            this._btnAddOr.Click += new System.EventHandler(this.btnAddOr_Click);
            // 
            // _btnNot
            // 
            this._btnNot.Location = new System.Drawing.Point(312, 4);
            this._btnNot.Margin = new System.Windows.Forms.Padding(0, 0, 4, 4);
            this._btnNot.Name = "_btnNot";
            this._btnNot.Size = new System.Drawing.Size(96, 28);
            this._btnNot.TabIndex = 3;
            this._btnNot.Text = "加“非”";
            this._btnNot.UseVisualStyleBackColor = true;
            this._btnNot.Click += new System.EventHandler(this.btnNot_Click);
            // 
            // _btnDel
            // 
            this._btnDel.Location = new System.Drawing.Point(12, 36);
            this._btnDel.Margin = new System.Windows.Forms.Padding(0, 0, 4, 4);
            this._btnDel.Name = "_btnDel";
            this._btnDel.Size = new System.Drawing.Size(96, 28);
            this._btnDel.TabIndex = 4;
            this._btnDel.Text = "删除";
            this._btnDel.UseVisualStyleBackColor = true;
            this._btnDel.Click += new System.EventHandler(this.btnDel_Click);
            // 
            // _btnToggle
            // 
            this._btnToggle.Location = new System.Drawing.Point(112, 36);
            this._btnToggle.Margin = new System.Windows.Forms.Padding(0, 0, 4, 4);
            this._btnToggle.Name = "_btnToggle";
            this._btnToggle.Size = new System.Drawing.Size(96, 28);
            this._btnToggle.TabIndex = 5;
            this._btnToggle.Text = "且↔或";
            this._btnToggle.UseVisualStyleBackColor = true;
            this._btnToggle.Click += new System.EventHandler(this.btnToggle_Click);
            // 
            // _tree
            // 
            this._tree.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tree.FullRowSelect = true;
            this._tree.HideSelection = false;
            this._tree.ItemHeight = 22;
            this._tree.Location = new System.Drawing.Point(0, 94);
            this._tree.Name = "_tree";
            this._tree.Size = new System.Drawing.Size(432, 580);
            this._tree.TabIndex = 2;
            this._tree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tree_AfterSelect);
            // 
            // _rightPnl
            // 
            this._rightPnl.Controls.Add(this._probeHolder);
            this._rightPnl.Controls.Add(this._grpLeaf);
            this._rightPnl.Controls.Add(this._grpManual);
            this._rightPnl.Dock = System.Windows.Forms.DockStyle.Fill;
            this._rightPnl.Location = new System.Drawing.Point(432, 38);
            this._rightPnl.Name = "_rightPnl";
            this._rightPnl.Padding = new System.Windows.Forms.Padding(10, 0, 12, 0);
            this._rightPnl.Size = new System.Drawing.Size(568, 674);
            this._rightPnl.TabIndex = 2;
            // 
            // _probeHolder
            // 
            this._probeHolder.Dock = System.Windows.Forms.DockStyle.Fill;
            this._probeHolder.Location = new System.Drawing.Point(10, 216);
            this._probeHolder.Name = "_probeHolder";
            this._probeHolder.Size = new System.Drawing.Size(546, 240);
            this._probeHolder.TabIndex = 1;
            // 
            // _grpManual
            // 
            this._grpManual.Controls.Add(this._rdoSim);
            this._grpManual.Controls.Add(this._rdoManual);
            this._grpManual.Controls.Add(this._btnFill);
            this._grpManual.Controls.Add(this._btnZero);
            this._grpManual.Controls.Add(this._btnClear);
            this._grpManual.Controls.Add(this._lblManualHint);
            this._grpManual.Controls.Add(this._dgvVar);
            this._grpManual.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._grpManual.Location = new System.Drawing.Point(10, 456);
            this._grpManual.Name = "_grpManual";
            this._grpManual.Size = new System.Drawing.Size(546, 218);
            this._grpManual.TabIndex = 2;
            this._grpManual.TabStop = false;
            this._grpManual.Text = "试算数据源（可以不用连模拟器）";
            // 
            // _rdoSim
            // 
            this._rdoSim.AutoSize = true;
            this._rdoSim.Checked = true;
            this._rdoSim.Location = new System.Drawing.Point(14, 24);
            this._rdoSim.Name = "_rdoSim";
            this._rdoSim.Size = new System.Drawing.Size(97, 21);
            this._rdoSim.TabIndex = 0;
            this._rdoSim.TabStop = true;
            this._rdoSim.Text = "模拟器实时";
            this._rdoSim.UseVisualStyleBackColor = true;
            this._rdoSim.CheckedChanged += new System.EventHandler(this.rdoSource_CheckedChanged);
            // 
            // _rdoManual
            // 
            this._rdoManual.AutoSize = true;
            this._rdoManual.Location = new System.Drawing.Point(118, 24);
            this._rdoManual.Name = "_rdoManual";
            this._rdoManual.Size = new System.Drawing.Size(85, 21);
            this._rdoManual.TabIndex = 1;
            this._rdoManual.Text = "手动试算";
            this._rdoManual.UseVisualStyleBackColor = true;
            this._rdoManual.CheckedChanged += new System.EventHandler(this.rdoSource_CheckedChanged);
            // 
            // _btnFill
            // 
            this._btnFill.Location = new System.Drawing.Point(214, 21);
            this._btnFill.Name = "_btnFill";
            this._btnFill.Size = new System.Drawing.Size(140, 28);
            this._btnFill.TabIndex = 2;
            this._btnFill.Text = "从模拟器快照填充";
            this._btnFill.UseVisualStyleBackColor = true;
            this._btnFill.Click += new System.EventHandler(this.btnFill_Click);
            // 
            // _btnZero
            // 
            this._btnZero.Location = new System.Drawing.Point(360, 21);
            this._btnZero.Name = "_btnZero";
            this._btnZero.Size = new System.Drawing.Size(82, 28);
            this._btnZero.TabIndex = 3;
            this._btnZero.Text = "全部置 0";
            this._btnZero.UseVisualStyleBackColor = true;
            this._btnZero.Click += new System.EventHandler(this.btnZero_Click);
            // 
            // _btnClear
            // 
            this._btnClear.Location = new System.Drawing.Point(448, 21);
            this._btnClear.Name = "_btnClear";
            this._btnClear.Size = new System.Drawing.Size(82, 28);
            this._btnClear.TabIndex = 4;
            this._btnClear.Text = "清空";
            this._btnClear.UseVisualStyleBackColor = true;
            this._btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // _lblManualHint
            // 
            this._lblManualHint.AutoEllipsis = true;
            this._lblManualHint.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this._lblManualHint.Location = new System.Drawing.Point(14, 52);
            this._lblManualHint.Name = "_lblManualHint";
            this._lblManualHint.Size = new System.Drawing.Size(516, 20);
            this._lblManualHint.TabIndex = 5;
            this._lblManualHint.Text = "没填的变量按“无数据”算（不是按 0 算），这样三态和真机一致。";
            // 
            // _dgvVar
            // 
            this._dgvVar.AllowUserToAddRows = false;
            this._dgvVar.AllowUserToDeleteRows = false;
            this._dgvVar.AllowUserToResizeRows = false;
            this._dgvVar.BackgroundColor = System.Drawing.SystemColors.Window;
            this._dgvVar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._dgvVar.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this._dgvVar.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colMVar,
            this.colMUnit,
            this.colMSim,
            this.colMManual,
            this.colMAvail});
            this._dgvVar.Location = new System.Drawing.Point(14, 76);
            this._dgvVar.MultiSelect = false;
            this._dgvVar.Name = "_dgvVar";
            this._dgvVar.RowHeadersVisible = false;
            this._dgvVar.RowTemplate.Height = 24;
            this._dgvVar.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this._dgvVar.Size = new System.Drawing.Size(516, 132);
            this._dgvVar.TabIndex = 6;
            this._dgvVar.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvVar_CellEndEdit);
            // 
            // colMVar
            // 
            this.colMVar.HeaderText = "变量";
            this.colMVar.Name = "colMVar";
            this.colMVar.ReadOnly = true;
            this.colMVar.Width = 196;
            // 
            // colMUnit
            // 
            this.colMUnit.HeaderText = "单位";
            this.colMUnit.Name = "colMUnit";
            this.colMUnit.ReadOnly = true;
            this.colMUnit.Width = 58;
            // 
            // colMSim
            // 
            this.colMSim.HeaderText = "模拟器值";
            this.colMSim.Name = "colMSim";
            this.colMSim.ReadOnly = true;
            this.colMSim.Width = 92;
            // 
            // colMManual
            // 
            this.colMManual.HeaderText = "手动值";
            this.colMManual.Name = "colMManual";
            this.colMManual.Width = 92;
            // 
            // colMAvail
            // 
            this.colMAvail.HeaderText = "能取到";
            this.colMAvail.Name = "colMAvail";
            this.colMAvail.ReadOnly = true;
            this.colMAvail.Width = 66;
            // 
            // _grpLeaf
            // 
            this._grpLeaf.Controls.Add(this._lblVar);
            this._grpLeaf.Controls.Add(this._cboVar);
            this._grpLeaf.Controls.Add(this._lblCmp);
            this._grpLeaf.Controls.Add(this._cboCmp);
            this._grpLeaf.Controls.Add(this._lblRight);
            this._grpLeaf.Controls.Add(this._cboRightKind);
            this._grpLeaf.Controls.Add(this._cboRight);
            this._grpLeaf.Controls.Add(this._nudValue);
            this._grpLeaf.Controls.Add(this._lblUnit);
            this._grpLeaf.Controls.Add(this._lblTo);
            this._grpLeaf.Controls.Add(this._nudValue2);
            this._grpLeaf.Controls.Add(this._lblLeafHint);
            this._grpLeaf.Dock = System.Windows.Forms.DockStyle.Top;
            this._grpLeaf.Location = new System.Drawing.Point(10, 0);
            this._grpLeaf.Name = "_grpLeaf";
            this._grpLeaf.Size = new System.Drawing.Size(546, 216);
            this._grpLeaf.TabIndex = 0;
            this._grpLeaf.TabStop = false;
            this._grpLeaf.Text = "选中项";
            // 
            // _lblVar
            // 
            this._lblVar.Location = new System.Drawing.Point(12, 30);
            this._lblVar.Name = "_lblVar";
            this._lblVar.Size = new System.Drawing.Size(48, 20);
            this._lblVar.TabIndex = 0;
            this._lblVar.Text = "左值";
            this._lblVar.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cboVar
            // 
            this._cboVar.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cboVar.Location = new System.Drawing.Point(64, 26);
            this._cboVar.Name = "_cboVar";
            this._cboVar.Size = new System.Drawing.Size(368, 25);
            this._cboVar.TabIndex = 1;
            this._cboVar.SelectedIndexChanged += new System.EventHandler(this.cboVar_SelectedIndexChanged);
            // 
            // _lblCmp
            // 
            this._lblCmp.Location = new System.Drawing.Point(12, 66);
            this._lblCmp.Name = "_lblCmp";
            this._lblCmp.Size = new System.Drawing.Size(48, 20);
            this._lblCmp.TabIndex = 2;
            this._lblCmp.Text = "比较";
            this._lblCmp.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cboCmp
            // 
            this._cboCmp.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cboCmp.Location = new System.Drawing.Point(64, 62);
            this._cboCmp.Name = "_cboCmp";
            this._cboCmp.Size = new System.Drawing.Size(150, 25);
            this._cboCmp.TabIndex = 3;
            this._cboCmp.SelectedIndexChanged += new System.EventHandler(this.cboCmp_SelectedIndexChanged);
            // 
            // _lblRight
            // 
            this._lblRight.Location = new System.Drawing.Point(228, 66);
            this._lblRight.Name = "_lblRight";
            this._lblRight.Size = new System.Drawing.Size(50, 20);
            this._lblRight.TabIndex = 4;
            this._lblRight.Text = "右值";
            this._lblRight.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cboRightKind
            // 
            this._cboRightKind.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cboRightKind.Location = new System.Drawing.Point(272, 62);
            this._cboRightKind.Name = "_cboRightKind";
            this._cboRightKind.Size = new System.Drawing.Size(160, 25);
            this._cboRightKind.TabIndex = 5;
            this._cboRightKind.SelectedIndexChanged += new System.EventHandler(this.cboRightKind_SelectedIndexChanged);
            // 
            // _cboRight
            // 
            this._cboRight.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cboRight.Location = new System.Drawing.Point(64, 98);
            this._cboRight.Name = "_cboRight";
            this._cboRight.Size = new System.Drawing.Size(368, 25);
            this._cboRight.TabIndex = 6;
            this._cboRight.SelectedIndexChanged += new System.EventHandler(this.cboRight_SelectedIndexChanged);
            // 
            // _nudValue
            // 
            this._nudValue.DecimalPlaces = 1;
            this._nudValue.Location = new System.Drawing.Point(64, 98);
            this._nudValue.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this._nudValue.Minimum = new decimal(new int[] {
            1000000,
            0,
            0,
            -2147483648});
            this._nudValue.Name = "_nudValue";
            this._nudValue.Size = new System.Drawing.Size(96, 25);
            this._nudValue.TabIndex = 7;
            this._nudValue.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._nudValue.ValueChanged += new System.EventHandler(this.nudValue_ValueChanged);
            // 
            // _lblUnit
            // 
            this._lblUnit.Location = new System.Drawing.Point(166, 102);
            this._lblUnit.Name = "_lblUnit";
            this._lblUnit.Size = new System.Drawing.Size(96, 20);
            this._lblUnit.TabIndex = 8;
            this._lblUnit.Text = "";
            this._lblUnit.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblTo
            // 
            this._lblTo.Location = new System.Drawing.Point(268, 102);
            this._lblTo.Name = "_lblTo";
            this._lblTo.Size = new System.Drawing.Size(24, 20);
            this._lblTo.TabIndex = 9;
            this._lblTo.Text = "至";
            this._lblTo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // _nudValue2
            // 
            this._nudValue2.DecimalPlaces = 1;
            this._nudValue2.Location = new System.Drawing.Point(298, 98);
            this._nudValue2.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this._nudValue2.Minimum = new decimal(new int[] {
            1000000,
            0,
            0,
            -2147483648});
            this._nudValue2.Name = "_nudValue2";
            this._nudValue2.Size = new System.Drawing.Size(96, 25);
            this._nudValue2.TabIndex = 10;
            this._nudValue2.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._nudValue2.ValueChanged += new System.EventHandler(this.nudValue_ValueChanged);
            // 
            // _lblLeafHint
            // 
            this._lblLeafHint.Location = new System.Drawing.Point(64, 132);
            this._lblLeafHint.Name = "_lblLeafHint";
            this._lblLeafHint.Size = new System.Drawing.Size(368, 70);
            this._lblLeafHint.TabIndex = 11;
            this._lblLeafHint.Text = "";
            // 
            // _probeHolder
            // 
            this._probeHolder.Dock = System.Windows.Forms.DockStyle.Fill;
            this._probeHolder.Location = new System.Drawing.Point(10, 216);
            this._probeHolder.Name = "_probeHolder";
            this._probeHolder.Size = new System.Drawing.Size(446, 338);
            this._probeHolder.TabIndex = 1;
            // 
            // _foot
            // 
            this._foot.ColumnCount = 4;
            this._foot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._foot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 104F));
            this._foot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 96F));
            this._foot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 96F));
            this._foot.Controls.Add(this._btnDefault, 0, 0);
            this._foot.Controls.Add(this._btnOk, 2, 0);
            this._foot.Controls.Add(this._btnCancel, 3, 0);
            this._foot.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._foot.Location = new System.Drawing.Point(0, 712);
            this._foot.Name = "_foot";
            this._foot.Padding = new System.Windows.Forms.Padding(14, 8, 14, 10);
            this._foot.RowCount = 1;
            this._foot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._foot.Size = new System.Drawing.Size(1000, 48);
            this._foot.TabIndex = 3;
            // 
            // _btnDefault
            // 
            this._btnDefault.Dock = System.Windows.Forms.DockStyle.Left;
            this._btnDefault.Location = new System.Drawing.Point(14, 8);
            this._btnDefault.Margin = new System.Windows.Forms.Padding(0);
            this._btnDefault.Name = "_btnDefault";
            this._btnDefault.Size = new System.Drawing.Size(122, 30);
            this._btnDefault.TabIndex = 0;
            this._btnDefault.Text = "还原初始条件";
            this._btnDefault.UseVisualStyleBackColor = true;
            this._btnDefault.Click += new System.EventHandler(this.btnDefault_Click);
            // 
            // _btnOk
            // 
            this._btnOk.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnOk.Location = new System.Drawing.Point(708, 8);
            this._btnOk.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this._btnOk.Name = "_btnOk";
            this._btnOk.Size = new System.Drawing.Size(88, 30);
            this._btnOk.TabIndex = 1;
            this._btnOk.Text = "确定";
            this._btnOk.UseVisualStyleBackColor = true;
            this._btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // _btnCancel
            // 
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnCancel.Location = new System.Drawing.Point(804, 8);
            this._btnCancel.Margin = new System.Windows.Forms.Padding(0);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Size = new System.Drawing.Size(82, 30);
            this._btnCancel.TabIndex = 2;
            this._btnCancel.Text = "取消";
            this._btnCancel.UseVisualStyleBackColor = true;
            // 
            // CondEditorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(1000, 760);
            this.Controls.Add(this._rightPnl);
            this.Controls.Add(this._leftPnl);
            this.Controls.Add(this._foot);
            this.Controls.Add(this._lblTitle);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(920, 680);
            this.Name = "CondEditorForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "条件编辑器";
            this.Shown += new System.EventHandler(this.CondEditorForm_Shown);
            this._leftPnl.ResumeLayout(false);
            this._flowTree.ResumeLayout(false);
            this._rightPnl.ResumeLayout(false);
            this._grpLeaf.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._nudValue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nudValue2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._dgvVar)).EndInit();
            this._grpManual.ResumeLayout(false);
            this._foot.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
