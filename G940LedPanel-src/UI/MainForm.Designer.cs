namespace G940LedPanel.UI
{
    /// <summary>
    /// 设计器文件 —— 本文件由 Visual Studio 窗体设计器读写，请尽量在设计器里改布局。
    ///
    /// 为了能被设计器正确解析，本文件严格遵守以下约定，手工编辑时也必须遵守：
    ///   1. 只有「控件字段声明 + InitializeComponent」两部分；
    ///   2. InitializeComponent 里只允许 new / 属性赋值 / Controls.Add / 事件订阅，
    ///      不允许 for、if、lambda（=>）、局部变量、自定义方法调用；
    ///   3. 所有控件一律 this.xxx 形式引用，字段名与控件名一致；
    ///   4. TableLayoutPanel 的 ColumnCount/RowCount 必须与 ColumnStyles/RowStyles 数量相等。
    ///
    /// 业务逻辑全部在 MainForm.cs（partial class 的另一半）。
    /// </summary>
    partial class MainForm
    {
        /// <summary>设计器所需的容器组件。</summary>
        private System.ComponentModel.IContainer components = null;

        // ---- 设置面板里的两个开关（原来在顶栏，顶栏已合并进设置面板） ----
        private System.Windows.Forms.TableLayoutPanel _flSetOpts;
        private System.Windows.Forms.CheckBox _chkMaster;
        private System.Windows.Forms.Label _lblMasterSt;
        private System.Windows.Forms.CheckBox _chkLedLabel;
        private System.Windows.Forms.CheckBox _chkMinimizeToTray;
        private System.Windows.Forms.Label _lblLang;
        private System.Windows.Forms.ComboBox _cboLang;
        private System.Windows.Forms.ToolTip _tip;

        // ---- 分区 2：预览 + 连接（LED 预览 / 设备 / SimConnect 三卡） ----
        private System.Windows.Forms.Panel _barMid;
        private System.Windows.Forms.TableLayoutPanel _midTlp;
        private System.Windows.Forms.GroupBox _ledCard;
        private System.Windows.Forms.GroupBox _devCard;
        private System.Windows.Forms.GroupBox _simCard;
        private System.Windows.Forms.TableLayoutPanel _devHost;
        private System.Windows.Forms.TableLayoutPanel _simHost;
        private System.Windows.Forms.TableLayoutPanel _devBtnRow;
        private System.Windows.Forms.TableLayoutPanel _simBtnRow;
        private System.Windows.Forms.Button _btnReconnect;
        private System.Windows.Forms.Button _btnDiag;
        private System.Windows.Forms.Label _lblDevName;
        private System.Windows.Forms.Label _lblDevStatus;
        private System.Windows.Forms.Label _lblDevHint;
        private System.Windows.Forms.CheckBox _chkSim;
        private System.Windows.Forms.Label _lblSimStatus;
        private System.Windows.Forms.Label _lblSimData;
        private System.Windows.Forms.Button _btnSimApply;
        private System.Windows.Forms.Button _btnSimCheck;
        private System.Windows.Forms.Button _btnSimData;
        private System.Windows.Forms.CheckBox _chkSceneLog;

        // ---- 分区 3：模式 TabControl（简单页 / 告警页） ----
        private System.Windows.Forms.TabControl _tabs;
        private System.Windows.Forms.TabPage _pageButtons;
        private System.Windows.Forms.TabPage _pageAlert;

        // 简单页
        private System.Windows.Forms.TableLayoutPanel _tlpPreset1;
        private System.Windows.Forms.Label _lblPresetTitle1;
        private System.Windows.Forms.ComboBox _cboPreset1;
        private System.Windows.Forms.Button _btnManage1;
        private System.Windows.Forms.Label _lblChip1;

        // 底灯（原顶栏「默认效果」）：只影响按键模式的 L1 背景层
        private System.Windows.Forms.FlowLayoutPanel _flBg;
        private System.Windows.Forms.Label _lblDef;
        private System.Windows.Forms.ComboBox _cboBgEffect;
        private System.Windows.Forms.Label _lblBgColor;
        private System.Windows.Forms.ComboBox _cboBgColor;
        private System.Windows.Forms.Label _lblSpd;
        private System.Windows.Forms.ComboBox _cboBgSpeed;
        private System.Windows.Forms.Label _lblBgHint;

        private System.Windows.Forms.DataGridView _dgv;
        private System.Windows.Forms.TableLayoutPanel _tlpFoot1;
        private System.Windows.Forms.Label _lblCapture;
        // 1.3.49：右列状态提示，与告警页 _lblSimHint2 同列宽、同位
        private System.Windows.Forms.Label _lblPresetInfo1;

        // 按键表右键菜单（1.3.49：三个按钮收进来，操作逻辑与告警页统一）
        private System.Windows.Forms.ContextMenuStrip _ctxBinding;
        private System.Windows.Forms.ToolStripMenuItem _miCapture;
        private System.Windows.Forms.ToolStripSeparator _miBindSepTop;
        private System.Windows.Forms.ToolStripSeparator _miBindSepMid;
        private System.Windows.Forms.ToolStripMenuItem _miAddBind;
        private System.Windows.Forms.ToolStripMenuItem _miDelBind;

        // 告警页
        private System.Windows.Forms.TableLayoutPanel _tlpPreset2;
        private System.Windows.Forms.Label _lblPresetTitle2;
        private System.Windows.Forms.ComboBox _cboPreset2;
        private System.Windows.Forms.Button _btnManage2;
        private System.Windows.Forms.Label _lblChip2;
        private System.Windows.Forms.DataGridView _dgvAlert;

        // 告警表右键菜单（1.3.43：新增 / 删除规则收进来；1.3.45 加规则体检）
        private System.Windows.Forms.ContextMenuStrip _ctxAlert;
        private System.Windows.Forms.ToolStripMenuItem _miAudit;
        private System.Windows.Forms.ToolStripMenuItem _miEditCond;
        private System.Windows.Forms.ToolStripMenuItem _miAddRule;
        private System.Windows.Forms.ToolStripMenuItem _miDelRule;
        private System.Windows.Forms.ToolStripSeparator _miSepTop;
        private System.Windows.Forms.ToolStripSeparator _miSepMid;
        private System.Windows.Forms.FlowLayoutPanel _flLegend;
        private System.Windows.Forms.Label _lblLgOk;
        private System.Windows.Forms.Label _lblLgWarn;
        private System.Windows.Forms.Label _lblLgErr;
        private System.Windows.Forms.TableLayoutPanel _tlpFoot2;
        private System.Windows.Forms.Label _lblAlertCounts;
        private System.Windows.Forms.Label _lblSimHint2;

        // DataGridView 列（设计器要求列也是字段，否则保存时会被重写）
        private System.Windows.Forms.DataGridViewCheckBoxColumn colBEnabled;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBBtn;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBName;
        private System.Windows.Forms.DataGridViewComboBoxColumn colBTrigger;
        private System.Windows.Forms.DataGridViewComboBoxColumn colBEffect;
        private System.Windows.Forms.DataGridViewComboBoxColumn colBColor;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBSpeed;
        private System.Windows.Forms.DataGridViewComboBoxColumn colBTarget;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colAEnabled;
        private System.Windows.Forms.DataGridViewTextBoxColumn colALed;
        private System.Windows.Forms.DataGridViewTextBoxColumn colARule;
        private System.Windows.Forms.DataGridViewComboBoxColumn colAColor;
        private System.Windows.Forms.DataGridViewComboBoxColumn colAEffect;
        private System.Windows.Forms.DataGridViewTextBoxColumn colASpeed;
        private System.Windows.Forms.DataGridViewTextBoxColumn colACond;
        private System.Windows.Forms.DataGridViewTextBoxColumn colARaw;

        // ---- 分区 4：底部状态条 ----
        private System.Windows.Forms.Panel _statusBar;
        private System.Windows.Forms.TableLayoutPanel _statusTlp;
        private System.Windows.Forms.Label _lblPressed;
        private System.Windows.Forms.Label _lblMode;
        private System.Windows.Forms.Label _lblSource;
        private System.Windows.Forms.Label _lblPollErr;

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
            this.components = new System.ComponentModel.Container();
            this._flSetOpts = new System.Windows.Forms.TableLayoutPanel();
            this._chkMaster = new System.Windows.Forms.CheckBox();
            this._lblMasterSt = new System.Windows.Forms.Label();
            this._chkLedLabel = new System.Windows.Forms.CheckBox();
            this._chkMinimizeToTray = new System.Windows.Forms.CheckBox();
            this._lblLang = new System.Windows.Forms.Label();
            this._cboLang = new System.Windows.Forms.ComboBox();
            this._tip = new System.Windows.Forms.ToolTip(this.components);
            this._barMid = new System.Windows.Forms.Panel();
            this._midTlp = new System.Windows.Forms.TableLayoutPanel();
            this._ledCard = new System.Windows.Forms.GroupBox();
            this._devCard = new System.Windows.Forms.GroupBox();
            this._devHost = new System.Windows.Forms.TableLayoutPanel();
            this._lblDevName = new System.Windows.Forms.Label();
            this._lblDevStatus = new System.Windows.Forms.Label();
            this._devBtnRow = new System.Windows.Forms.TableLayoutPanel();
            this._btnReconnect = new System.Windows.Forms.Button();
            this._btnDiag = new System.Windows.Forms.Button();
            this._lblDevHint = new System.Windows.Forms.Label();
            this._simCard = new System.Windows.Forms.GroupBox();
            this._simHost = new System.Windows.Forms.TableLayoutPanel();
            this._chkSim = new System.Windows.Forms.CheckBox();
            this._lblSimStatus = new System.Windows.Forms.Label();
            this._simBtnRow = new System.Windows.Forms.TableLayoutPanel();
            this._btnSimApply = new System.Windows.Forms.Button();
            this._btnSimCheck = new System.Windows.Forms.Button();
            this._btnSimData = new System.Windows.Forms.Button();
            this._chkSceneLog = new System.Windows.Forms.CheckBox();
            this._lblSimData = new System.Windows.Forms.Label();
            this._tabs = new System.Windows.Forms.TabControl();
            this._pageButtons = new System.Windows.Forms.TabPage();
            this._dgv = new System.Windows.Forms.DataGridView();
            this.colBEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colBBtn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBTrigger = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.colBEffect = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.colBColor = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.colBSpeed = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBTarget = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this._tlpFoot1 = new System.Windows.Forms.TableLayoutPanel();
            this._lblCapture = new System.Windows.Forms.Label();
            this._lblPresetInfo1 = new System.Windows.Forms.Label();
            this._ctxBinding = new System.Windows.Forms.ContextMenuStrip();
            this._miCapture = new System.Windows.Forms.ToolStripMenuItem();
            this._miBindSepTop = new System.Windows.Forms.ToolStripSeparator();
            this._miBindSepMid = new System.Windows.Forms.ToolStripSeparator();
            this._miAddBind = new System.Windows.Forms.ToolStripMenuItem();
            this._miDelBind = new System.Windows.Forms.ToolStripMenuItem();
            this._tlpPreset1 = new System.Windows.Forms.TableLayoutPanel();
            this._lblPresetTitle1 = new System.Windows.Forms.Label();
            this._cboPreset1 = new System.Windows.Forms.ComboBox();
            this._btnManage1 = new System.Windows.Forms.Button();
            this._lblChip1 = new System.Windows.Forms.Label();
            this._ctxAlert = new System.Windows.Forms.ContextMenuStrip();
            this._miAudit = new System.Windows.Forms.ToolStripMenuItem();
            this._miEditCond = new System.Windows.Forms.ToolStripMenuItem();
            this._miAddRule = new System.Windows.Forms.ToolStripMenuItem();
            this._miDelRule = new System.Windows.Forms.ToolStripMenuItem();
            this._miSepTop = new System.Windows.Forms.ToolStripSeparator();
            this._miSepMid = new System.Windows.Forms.ToolStripSeparator();
            this._flLegend = new System.Windows.Forms.FlowLayoutPanel();
            this._lblLgOk = new System.Windows.Forms.Label();
            this._lblLgWarn = new System.Windows.Forms.Label();
            this._lblLgErr = new System.Windows.Forms.Label();
            this._flBg = new System.Windows.Forms.FlowLayoutPanel();
            this._lblDef = new System.Windows.Forms.Label();
            this._cboBgEffect = new System.Windows.Forms.ComboBox();
            this._lblBgColor = new System.Windows.Forms.Label();
            this._cboBgColor = new System.Windows.Forms.ComboBox();
            this._lblSpd = new System.Windows.Forms.Label();
            this._cboBgSpeed = new System.Windows.Forms.ComboBox();
            this._lblBgHint = new System.Windows.Forms.Label();
            this._pageAlert = new System.Windows.Forms.TabPage();
            this._dgvAlert = new System.Windows.Forms.DataGridView();
            this.colAEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colALed = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colARule = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAColor = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.colAEffect = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.colASpeed = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colACond = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colARaw = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._tlpFoot2 = new System.Windows.Forms.TableLayoutPanel();
            this._lblAlertCounts = new System.Windows.Forms.Label();
            this._lblSimHint2 = new System.Windows.Forms.Label();
            this._tlpPreset2 = new System.Windows.Forms.TableLayoutPanel();
            this._lblPresetTitle2 = new System.Windows.Forms.Label();
            this._cboPreset2 = new System.Windows.Forms.ComboBox();
            this._btnManage2 = new System.Windows.Forms.Button();
            this._lblChip2 = new System.Windows.Forms.Label();
            this._statusBar = new System.Windows.Forms.Panel();
            this._statusTlp = new System.Windows.Forms.TableLayoutPanel();
            this._lblPressed = new System.Windows.Forms.Label();
            this._lblMode = new System.Windows.Forms.Label();
            this._lblSource = new System.Windows.Forms.Label();
            this._lblPollErr = new System.Windows.Forms.Label();
            this._barMid.SuspendLayout();
            this._midTlp.SuspendLayout();
            this._devCard.SuspendLayout();
            this._devHost.SuspendLayout();
            this._devBtnRow.SuspendLayout();
            this._flSetOpts.SuspendLayout();
            this._simCard.SuspendLayout();
            this._simHost.SuspendLayout();
            this._simBtnRow.SuspendLayout();
            this._tabs.SuspendLayout();
            this._pageButtons.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._dgv)).BeginInit();
            this._tlpFoot1.SuspendLayout();
            this._tlpPreset1.SuspendLayout();
            this._flBg.SuspendLayout();
            this._pageAlert.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._dgvAlert)).BeginInit();
            this._tlpFoot2.SuspendLayout();
            this._tlpPreset2.SuspendLayout();
            this._statusBar.SuspendLayout();
            this._statusTlp.SuspendLayout();
            this.SuspendLayout();
            // 
            // _lblDef
            // 
            this._lblDef.AutoSize = true;
            this._lblDef.Location = new System.Drawing.Point(3, 9);
            this._lblDef.Margin = new System.Windows.Forms.Padding(3, 9, 6, 0);
            this._lblDef.Name = "_lblDef";
            this._lblDef.Size = new System.Drawing.Size(46, 17);
            this._lblDef.TabIndex = 0;
            this._lblDef.Text = "底灯：";
            // 
            // _cboBgEffect
            // 
            this._cboBgEffect.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cboBgEffect.Location = new System.Drawing.Point(58, 5);
            this._cboBgEffect.Margin = new System.Windows.Forms.Padding(3, 5, 10, 3);
            this._cboBgEffect.Name = "_cboBgEffect";
            this._cboBgEffect.Size = new System.Drawing.Size(150, 25);
            this._cboBgEffect.TabIndex = 1;
            this._cboBgEffect.SelectedIndexChanged += new System.EventHandler(this.cboBgEffect_SelectedIndexChanged);
            // 
            // _lblBgColor
            // 
            this._lblBgColor.AutoSize = true;
            this._lblBgColor.Location = new System.Drawing.Point(221, 9);
            this._lblBgColor.Margin = new System.Windows.Forms.Padding(3, 9, 4, 0);
            this._lblBgColor.Name = "_lblBgColor";
            this._lblBgColor.Size = new System.Drawing.Size(34, 17);
            this._lblBgColor.TabIndex = 2;
            this._lblBgColor.Text = "颜色";
            // 
            // _cboBgColor
            // 
            this._cboBgColor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cboBgColor.Location = new System.Drawing.Point(262, 5);
            this._cboBgColor.Margin = new System.Windows.Forms.Padding(3, 5, 10, 3);
            this._cboBgColor.Name = "_cboBgColor";
            this._cboBgColor.Size = new System.Drawing.Size(72, 25);
            this._cboBgColor.TabIndex = 3;
            this._cboBgColor.SelectedIndexChanged += new System.EventHandler(this.cboBgColor_SelectedIndexChanged);
            // 
            // _lblSpd
            // 
            this._lblSpd.AutoSize = true;
            this._lblSpd.Location = new System.Drawing.Point(347, 9);
            this._lblSpd.Margin = new System.Windows.Forms.Padding(3, 9, 4, 0);
            this._lblSpd.Name = "_lblSpd";
            this._lblSpd.Size = new System.Drawing.Size(34, 17);
            this._lblSpd.TabIndex = 4;
            this._lblSpd.Text = "速度";
            // 
            // _cboBgSpeed
            // 
            this._cboBgSpeed.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cboBgSpeed.Location = new System.Drawing.Point(388, 5);
            this._cboBgSpeed.Margin = new System.Windows.Forms.Padding(3, 5, 10, 3);
            this._cboBgSpeed.Name = "_cboBgSpeed";
            this._cboBgSpeed.Size = new System.Drawing.Size(88, 25);
            this._cboBgSpeed.TabIndex = 5;
            this._cboBgSpeed.SelectedIndexChanged += new System.EventHandler(this.cboBgSpeed_SelectedIndexChanged);
            // 
            // _chkLedLabel
            // 
            this._chkLedLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._chkLedLabel.AutoSize = true;
            this._chkLedLabel.Location = new System.Drawing.Point(3, 32);
            this._chkLedLabel.Margin = new System.Windows.Forms.Padding(3, 3, 8, 3);
            this._chkLedLabel.Name = "_chkLedLabel";
            this._chkLedLabel.Size = new System.Drawing.Size(99, 21);
            this._chkLedLabel.TabIndex = 2;
            this._chkLedLabel.Text = "显示灯位文字";
            this._tip.SetToolTip(this._chkLedLabel, "两种模式都生效：按键模式显示 P1..P8，告警模式显示系统名");
            this._chkLedLabel.UseVisualStyleBackColor = true;
            this._chkLedLabel.CheckedChanged += new System.EventHandler(this.chkLedLabel_CheckedChanged);
            // 
            // _chkMinimizeToTray
            // 
            this._chkMinimizeToTray.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._chkMinimizeToTray.AutoSize = true;
            this._chkMinimizeToTray.Location = new System.Drawing.Point(3, 57);
            this._chkMinimizeToTray.Margin = new System.Windows.Forms.Padding(3, 3, 8, 3);
            this._chkMinimizeToTray.Name = "_chkMinimizeToTray";
            this._chkMinimizeToTray.Size = new System.Drawing.Size(99, 21);
            this._chkMinimizeToTray.TabIndex = 3;
            this._chkMinimizeToTray.Text = "最小化到托盘";
            this._tip.SetToolTip(this._chkMinimizeToTray, "关闭或最小化时收到系统托盘继续运行；取消勾选则关闭即退出");
            this._chkMinimizeToTray.UseVisualStyleBackColor = true;
            this._chkMinimizeToTray.CheckedChanged += new System.EventHandler(this.chkMinimizeToTray_CheckedChanged);
            // 
            // _lblLang
            // 
            this._lblLang.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._lblLang.AutoSize = true;
            this._lblLang.Location = new System.Drawing.Point(3, 81);
            this._lblLang.Margin = new System.Windows.Forms.Padding(3, 0, 8, 0);
            this._lblLang.Name = "_lblLang";
            this._lblLang.Size = new System.Drawing.Size(42, 17);
            this._lblLang.TabIndex = 4;
            this._lblLang.Text = "语言";
            // 
            // _cboLang
            // 
            this._cboLang.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cboLang.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            // 候选项不在这里写死：由 MainForm.FillLangCombo() 按 L.All 运行时填（1.3.67）。
            // 写在这里等于让「语言清单」有两份，加一门语言要改两处还会错位。
            this._cboLang.Location = new System.Drawing.Point(105, 79);
            this._cboLang.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this._cboLang.Name = "_cboLang";
            this._cboLang.Size = new System.Drawing.Size(223, 25);
            this._cboLang.TabIndex = 5;
            this._cboLang.SelectedIndexChanged += new System.EventHandler(this.cboLang_SelectedIndexChanged);
            // 
            // _chkMaster
            // 
            this._chkMaster.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._chkMaster.AutoSize = true;
            this._chkMaster.Location = new System.Drawing.Point(3, 3);
            this._chkMaster.Margin = new System.Windows.Forms.Padding(3, 3, 8, 3);
            this._chkMaster.Name = "_chkMaster";
            this._chkMaster.Size = new System.Drawing.Size(99, 21);
            this._chkMaster.TabIndex = 0;
            this._chkMaster.Text = "全部效果总开关";
            this._chkMaster.UseVisualStyleBackColor = true;
            this._chkMaster.CheckedChanged += new System.EventHandler(this.chkMaster_CheckedChanged);
            // 
            // _lblMasterSt
            // 
            this._lblMasterSt.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._lblMasterSt.AutoSize = true;
            this._lblMasterSt.Location = new System.Drawing.Point(113, 5);
            this._lblMasterSt.Margin = new System.Windows.Forms.Padding(3, 0, 6, 0);
            this._lblMasterSt.Name = "_lblMasterSt";
            this._lblMasterSt.Size = new System.Drawing.Size(100, 17);
            this._lblMasterSt.TabIndex = 1;
            this._lblMasterSt.Text = "运行中";
            // 
            // _barMid
            // 
            this._barMid.Controls.Add(this._midTlp);
            this._barMid.Dock = System.Windows.Forms.DockStyle.Top;
            this._barMid.Location = new System.Drawing.Point(0, 46);
            this._barMid.Name = "_barMid";
            this._barMid.Size = new System.Drawing.Size(1200, 238);
            this._barMid.TabIndex = 1;
            // 
            // _midTlp
            // 
            this._midTlp.ColumnCount = 3;
            this._midTlp.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this._midTlp.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this._midTlp.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this._midTlp.Controls.Add(this._ledCard, 0, 0);
            this._midTlp.Controls.Add(this._devCard, 1, 0);
            this._midTlp.Controls.Add(this._simCard, 2, 0);
            this._midTlp.Dock = System.Windows.Forms.DockStyle.Fill;
            this._midTlp.Location = new System.Drawing.Point(0, 0);
            this._midTlp.Name = "_midTlp";
            this._midTlp.Padding = new System.Windows.Forms.Padding(12, 10, 12, 4);
            this._midTlp.RowCount = 1;
            this._midTlp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._midTlp.Size = new System.Drawing.Size(1200, 238);
            this._midTlp.TabIndex = 0;
            // 
            // _ledCard
            // 
            this._ledCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this._ledCard.Location = new System.Drawing.Point(15, 13);
            this._ledCard.Name = "_ledCard";
            this._ledCard.Size = new System.Drawing.Size(464, 218);
            this._ledCard.TabIndex = 0;
            this._ledCard.TabStop = false;
            this._ledCard.Text = "LED 面板预览（两排 4+4 方形灯）";
            // 
            // _devCard
            // 
            this._devCard.Controls.Add(this._devHost);
            this._devCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this._devCard.Location = new System.Drawing.Point(485, 13);
            this._devCard.Name = "_devCard";
            this._devCard.Size = new System.Drawing.Size(343, 218);
            this._devCard.TabIndex = 1;
            this._devCard.TabStop = false;
            this._devCard.Text = "设置";
            // 
            // _flSetOpts
            // 
            this._flSetOpts.ColumnCount = 2;
            this._flSetOpts.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this._flSetOpts.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._flSetOpts.Controls.Add(this._chkMaster, 0, 0);
            this._flSetOpts.Controls.Add(this._lblMasterSt, 1, 0);
            this._flSetOpts.Controls.Add(this._chkLedLabel, 0, 1);
            this._flSetOpts.Controls.Add(this._chkMinimizeToTray, 0, 2);
            this._flSetOpts.Controls.Add(this._lblLang, 0, 3);
            this._flSetOpts.Controls.Add(this._cboLang, 1, 3);
            this._flSetOpts.Dock = System.Windows.Forms.DockStyle.Fill;
            this._flSetOpts.Location = new System.Drawing.Point(6, 63);
            this._flSetOpts.Margin = new System.Windows.Forms.Padding(0);
            this._flSetOpts.Name = "_flSetOpts";
            this._flSetOpts.RowCount = 4;
            this._flSetOpts.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this._flSetOpts.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this._flSetOpts.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this._flSetOpts.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this._flSetOpts.SetColumnSpan(this._chkLedLabel, 2);
            this._flSetOpts.SetColumnSpan(this._chkMinimizeToTray, 2);
            this._flSetOpts.Size = new System.Drawing.Size(331, 104);
            this._flSetOpts.TabIndex = 3;
            // 
            // _devHost
            // 
            this._devHost.ColumnCount = 2;
            this._devHost.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 104F));
            this._devHost.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._devHost.Controls.Add(this._lblDevName, 0, 0);
            this._devHost.Controls.Add(this._lblDevStatus, 1, 0);
            this._devHost.Controls.Add(this._devBtnRow, 0, 1);
            this._devHost.Controls.Add(this._flSetOpts, 0, 2);
            this._devHost.Controls.Add(this._lblDevHint, 0, 3);
            this._devHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this._devHost.Location = new System.Drawing.Point(3, 19);
            this._devHost.Name = "_devHost";
            this._devHost.Padding = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this._devHost.RowCount = 4;
            this._devHost.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this._devHost.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this._devHost.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 104F));
            this._devHost.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._devHost.SetColumnSpan(this._devBtnRow, 2);
            this._devHost.SetColumnSpan(this._flSetOpts, 2);
            this._devHost.SetColumnSpan(this._lblDevHint, 2);
            this._devHost.Size = new System.Drawing.Size(337, 196);
            this._devHost.TabIndex = 0;
            // 
            // _lblDevName
            // 
            this._lblDevName.AutoEllipsis = true;
            this._lblDevName.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblDevName.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this._lblDevName.Location = new System.Drawing.Point(9, 7);
            this._lblDevName.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this._lblDevName.Name = "_lblDevName";
            this._lblDevName.Size = new System.Drawing.Size(98, 26);
            this._lblDevName.TabIndex = 0;
            this._lblDevName.Text = "G940 油门";
            this._lblDevName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblDevStatus
            // 
            this._lblDevStatus.AutoEllipsis = true;
            this._lblDevStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblDevStatus.Location = new System.Drawing.Point(101, 7);
            this._lblDevStatus.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this._lblDevStatus.Name = "_lblDevStatus";
            this._lblDevStatus.Size = new System.Drawing.Size(233, 26);
            this._lblDevStatus.TabIndex = 1;
            this._lblDevStatus.Text = "● 正在检测…";
            this._lblDevStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // 
            // _devBtnRow
            // 
            this._devBtnRow.ColumnCount = 3;
            this._devBtnRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 104F));
            this._devBtnRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 104F));
            this._devBtnRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._devBtnRow.Controls.Add(this._btnReconnect, 0, 0);
            this._devBtnRow.Controls.Add(this._btnDiag, 1, 0);
            this._devBtnRow.Dock = System.Windows.Forms.DockStyle.Fill;
            this._devBtnRow.Location = new System.Drawing.Point(6, 57);
            this._devBtnRow.Margin = new System.Windows.Forms.Padding(0);
            this._devBtnRow.Name = "_devBtnRow";
            this._devBtnRow.RowCount = 1;
            this._devBtnRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._devBtnRow.Size = new System.Drawing.Size(331, 32);
            this._devBtnRow.TabIndex = 2;
            // 
            // _btnReconnect
            // 
            this._btnReconnect.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnReconnect.Location = new System.Drawing.Point(0, 3);
            this._btnReconnect.Margin = new System.Windows.Forms.Padding(0, 3, 6, 3);
            this._btnReconnect.Name = "_btnReconnect";
            this._btnReconnect.Size = new System.Drawing.Size(82, 26);
            this._btnReconnect.TabIndex = 0;
            this._btnReconnect.Text = "断开";
            this._btnReconnect.UseVisualStyleBackColor = true;
            this._btnReconnect.Click += new System.EventHandler(this.btnReconnect_Click);
            // 
            // _btnDiag
            // 
            this._btnDiag.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnDiag.Location = new System.Drawing.Point(88, 3);
            this._btnDiag.Margin = new System.Windows.Forms.Padding(0, 3, 6, 3);
            this._btnDiag.Name = "_btnDiag";
            this._btnDiag.Size = new System.Drawing.Size(82, 26);
            this._btnDiag.TabIndex = 1;
            this._btnDiag.Text = "设备诊断";
            this._btnDiag.UseVisualStyleBackColor = true;
            this._btnDiag.Click += new System.EventHandler(this.btnDiag_Click);
            // 
            // _lblDevHint
            // 
            // 1.3.33 起这一行不再是「⚠ N 条 · 点击查看」（那个集中清单连同 IssuesForm 一起取消了），
            // 改为「保存状态」行：正常灰字"改动已自动保存"，存不下时红字报原因。
            // 名字保留 _lblDevHint，改名的收益抵不上 Designer 里改五处的成本。
            this._lblDevHint.AutoEllipsis = true;
            this._lblDevHint.Cursor = System.Windows.Forms.Cursors.Default;
            this._lblDevHint.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblDevHint.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this._lblDevHint.Location = new System.Drawing.Point(9, 97);
            this._lblDevHint.Margin = new System.Windows.Forms.Padding(3, 4, 3, 0);
            this._lblDevHint.Name = "_lblDevHint";
            this._lblDevHint.Size = new System.Drawing.Size(325, 95);
            this._lblDevHint.TabIndex = 4;
            this._lblDevHint.Text = "改动已自动保存";
            this._lblDevHint.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // _simCard
            // 
            this._simCard.Controls.Add(this._simHost);
            this._simCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this._simCard.Location = new System.Drawing.Point(834, 13);
            this._simCard.Name = "_simCard";
            this._simCard.Size = new System.Drawing.Size(351, 218);
            this._simCard.TabIndex = 2;
            this._simCard.TabStop = false;
            this._simCard.Text = "MSFS2024（SimConnect）";
            // 
            // _simHost
            // 
            this._simHost.ColumnCount = 1;
            this._simHost.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._simHost.Controls.Add(this._chkSim, 0, 0);
            this._simHost.Controls.Add(this._lblSimStatus, 0, 1);
            this._simHost.Controls.Add(this._simBtnRow, 0, 2);
            this._simHost.Controls.Add(this._lblSimData, 0, 3);
            this._simHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this._simHost.Location = new System.Drawing.Point(3, 19);
            this._simHost.Name = "_simHost";
            this._simHost.Padding = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this._simHost.RowCount = 4;
            this._simHost.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this._simHost.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this._simHost.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 56F));
            this._simHost.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._simHost.Size = new System.Drawing.Size(345, 196);
            this._simHost.TabIndex = 0;
            // 
            // _chkSim
            // 
            this._chkSim.Dock = System.Windows.Forms.DockStyle.Fill;
            this._chkSim.Location = new System.Drawing.Point(9, 7);
            this._chkSim.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this._chkSim.Name = "_chkSim";
            this._chkSim.Size = new System.Drawing.Size(333, 26);
            this._chkSim.TabIndex = 0;
            this._chkSim.Text = "启用 SimConnect 联动";
            this._chkSim.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._chkSim.UseVisualStyleBackColor = true;
            // 
            // _lblSimStatus
            // 
            this._lblSimStatus.AutoEllipsis = true;
            this._lblSimStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblSimStatus.Location = new System.Drawing.Point(9, 33);
            this._lblSimStatus.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this._lblSimStatus.Name = "_lblSimStatus";
            this._lblSimStatus.Size = new System.Drawing.Size(333, 24);
            this._lblSimStatus.TabIndex = 1;
            this._lblSimStatus.Text = "● 未连接";
            this._lblSimStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _simBtnRow
            // 
            this._simBtnRow.ColumnCount = 3;
            this._simBtnRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 124F));
            this._simBtnRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 96F));
            this._simBtnRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._simBtnRow.Controls.Add(this._btnSimApply, 0, 0);
            this._simBtnRow.Controls.Add(this._btnSimCheck, 1, 0);
            this._simBtnRow.Controls.Add(this._btnSimData, 2, 0);
            this._simBtnRow.Controls.Add(this._chkSceneLog, 0, 1);
            this._simBtnRow.Dock = System.Windows.Forms.DockStyle.Fill;
            this._simBtnRow.Location = new System.Drawing.Point(6, 57);
            this._simBtnRow.Margin = new System.Windows.Forms.Padding(0);
            this._simBtnRow.Name = "_simBtnRow";
            this._simBtnRow.RowCount = 2;
            this._simBtnRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this._simBtnRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this._simBtnRow.SetColumnSpan(this._chkSceneLog, 3);
            this._simBtnRow.Size = new System.Drawing.Size(339, 56);
            this._simBtnRow.TabIndex = 2;
            // 
            // _btnSimApply
            // 
            this._btnSimApply.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnSimApply.Location = new System.Drawing.Point(0, 3);
            this._btnSimApply.Margin = new System.Windows.Forms.Padding(0, 3, 6, 3);
            this._btnSimApply.Name = "_btnSimApply";
            this._btnSimApply.Size = new System.Drawing.Size(118, 26);
            this._btnSimApply.TabIndex = 0;
            this._btnSimApply.Text = "应用并连接";
            this._btnSimApply.UseVisualStyleBackColor = true;
            this._btnSimApply.Click += new System.EventHandler(this.btnSimApply_Click);
            // 
            // _btnSimCheck
            // 
            this._btnSimCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnSimCheck.Location = new System.Drawing.Point(100, 3);
            this._btnSimCheck.Margin = new System.Windows.Forms.Padding(0, 3, 6, 3);
            this._btnSimCheck.Name = "_btnSimCheck";
            this._btnSimCheck.Size = new System.Drawing.Size(104, 26);
            this._btnSimCheck.TabIndex = 1;
            this._btnSimCheck.Text = "SDK 检测";
            this._btnSimCheck.UseVisualStyleBackColor = true;
            this._btnSimCheck.Click += new System.EventHandler(this.btnSimCheck_Click);
            // 
            // _btnSimData
            // 
            this._btnSimData.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnSimData.Location = new System.Drawing.Point(182, 3);
            this._btnSimData.Name = "_btnSimData";
            this._btnSimData.Size = new System.Drawing.Size(90, 26);
            this._btnSimData.TabIndex = 2;
            this._btnSimData.Text = "实时数据…";
            this._btnSimData.UseVisualStyleBackColor = true;
            this._btnSimData.Click += new System.EventHandler(this.btnSimData_Click);
            // 
            // _chkSceneLog
            // 
            this._chkSceneLog.AutoEllipsis = true;
            this._chkSceneLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this._chkSceneLog.Location = new System.Drawing.Point(3, 32);
            this._chkSceneLog.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this._chkSceneLog.Name = "_chkSceneLog";
            this._chkSceneLog.Size = new System.Drawing.Size(336, 24);
            this._chkSceneLog.TabIndex = 3;
            this._chkSceneLog.Text = "记录场景状态（写到 diag 文件夹）";
            this._chkSceneLog.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._chkSceneLog.UseVisualStyleBackColor = true;
            this._chkSceneLog.CheckedChanged += new System.EventHandler(this.chkSceneLog_CheckedChanged);
            // 
            // _lblSimData
            // 
            this._lblSimData.AutoEllipsis = true;
            this._lblSimData.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblSimData.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this._lblSimData.Location = new System.Drawing.Point(9, 94);
            this._lblSimData.Margin = new System.Windows.Forms.Padding(3, 2, 3, 0);
            this._lblSimData.Name = "_lblSimData";
            this._lblSimData.Size = new System.Drawing.Size(333, 98);
            this._lblSimData.TabIndex = 3;
            this._lblSimData.Text = "连接后显示空速/高度/发动机等状态。";
            // 
            // _tabs
            // 
            this._tabs.Controls.Add(this._pageButtons);
            this._tabs.Controls.Add(this._pageAlert);
            this._tabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tabs.Location = new System.Drawing.Point(0, 284);
            this._tabs.Name = "_tabs";
            this._tabs.SelectedIndex = 0;
            this._tabs.Size = new System.Drawing.Size(1200, 518);
            this._tabs.TabIndex = 2;
            // 
            // _pageButtons
            // 
            this._pageButtons.Controls.Add(this._dgv);
            this._pageButtons.Controls.Add(this._tlpFoot1);
            this._pageButtons.Controls.Add(this._tlpPreset1);
            this._pageButtons.Location = new System.Drawing.Point(4, 26);
            this._pageButtons.Name = "_pageButtons";
            this._pageButtons.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this._pageButtons.Size = new System.Drawing.Size(1192, 488);
            this._pageButtons.TabIndex = 0;
            this._pageButtons.Text = "按键模式（按钮 → 灯光）";
            this._pageButtons.UseVisualStyleBackColor = true;
            // 
            // _dgv
            // 
            this._dgv.AllowUserToAddRows = false;
            this._dgv.AllowUserToDeleteRows = false;
            this._dgv.AllowUserToOrderColumns = false;
            this._dgv.AllowUserToResizeRows = false;
            this._dgv.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.None;
            this._dgv.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this._dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colBEnabled,
            this.colBBtn,
            this.colBName,
            this.colBTrigger,
            this.colBEffect,
            this.colBColor,
            this.colBSpeed,
            this.colBTarget});
            this._dgv.ContextMenuStrip = this._ctxBinding;
            this._dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this._dgv.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this._dgv.Location = new System.Drawing.Point(12, 48);
            this._dgv.MultiSelect = false;
            this._dgv.Name = "_dgv";
            this._dgv.RowHeadersVisible = true;
            this._dgv.RowHeadersWidth = 36;
            this._dgv.RowTemplate.Height = 26;
            this._dgv.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this._dgv.Size = new System.Drawing.Size(1168, 402);
            this._dgv.TabIndex = 1;
            this._dgv.CellBeginEdit += new System.Windows.Forms.DataGridViewCellCancelEventHandler(this.dgv_CellBeginEdit);
            this._dgv.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_CellEndEdit);
            this._dgv.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_CellValueChanged);
            this._dgv.RowPostPaint += new System.Windows.Forms.DataGridViewRowPostPaintEventHandler(this.dgv_RowPostPaint);
            this._dgv.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.dgv_RowsAdded);
            this._dgv.RowsRemoved += new System.Windows.Forms.DataGridViewRowsRemovedEventHandler(this.dgv_RowsRemoved);
            // 
            // colBEnabled
            // 
            this.colBEnabled.FillWeight = 8F;
            this.colBEnabled.HeaderText = "启用";
            this.colBEnabled.MinimumWidth = 40;
            this.colBEnabled.Name = "colBEnabled";
            this.colBEnabled.Width = 48;
            // 
            // colBBtn
            // 
            this.colBBtn.FillWeight = 12F;
            this.colBBtn.HeaderText = "油门按钮#";
            this.colBBtn.MinimumWidth = 80;
            this.colBBtn.Name = "colBBtn";
            this.colBBtn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colBBtn.Width = 90;
            // 
            // colBName
            // 
            this.colBName.FillWeight = 12F;
            this.colBName.HeaderText = "按钮名";
            this.colBName.MinimumWidth = 80;
            this.colBName.Name = "colBName";
            this.colBName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colBName.Width = 100;
            // 
            // colBTrigger
            // 
            this.colBTrigger.FillWeight = 11F;
            this.colBTrigger.HeaderText = "触发";
            this.colBTrigger.Items.AddRange(new object[] {
            "按下持续",
            "按下切换"});
            this.colBTrigger.MinimumWidth = 80;
            this.colBTrigger.Name = "colBTrigger";
            this.colBTrigger.Width = 92;
            // 
            // colBEffect
            // 
            this.colBEffect.FillWeight = 14F;
            this.colBEffect.HeaderText = "效果";
            this.colBEffect.Items.AddRange(new object[] {
            "关灯",
            "常亮",
            "慢闪",
            "双闪",
            "一长一短",
            "快闪",
            "心跳",
            "环行·顺时针",
            "环行·逆时针",
            "双灯追",
            "三灯追",
            "中心扩散",
            "边缘收拢",
            "两点对扫",
            "双子·对称转",
            "颜色循环",
            "随机彩灯",
            "二进制计数",
            "绕环填充",
            "奇偶跳"});
            this.colBEffect.MinimumWidth = 110;
            this.colBEffect.Name = "colBEffect";
            this.colBEffect.Width = 120;
            // 
            // colBColor
            // 
            this.colBColor.FillWeight = 9F;
            this.colBColor.HeaderText = "颜色";
            this.colBColor.Items.AddRange(new object[] {
            "关",
            "绿",
            "琥珀",
            "红"});
            this.colBColor.MinimumWidth = 60;
            this.colBColor.Name = "colBColor";
            this.colBColor.Width = 68;
            // 
            // colBSpeed
            // 
            this.colBSpeed.FillWeight = 9F;
            this.colBSpeed.HeaderText = "速度";
            this.colBSpeed.MinimumWidth = 55;
            this.colBSpeed.Name = "colBSpeed";
            this.colBSpeed.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colBSpeed.Width = 60;
            // 
            // colBTarget
            // 
            this.colBTarget.FillWeight = 10F;
            this.colBTarget.HeaderText = "目标";
            this.colBTarget.Items.AddRange(new object[] {
            "全部",
            "P1",
            "P2",
            "P3",
            "P4",
            "P5",
            "P6",
            "P7",
            "P8"});
            this.colBTarget.MinimumWidth = 64;
            this.colBTarget.Name = "colBTarget";
            this.colBTarget.Width = 72;
            // 
            // _tlpFoot1
            // 
            this._tlpFoot1.ColumnCount = 2;
            // 与 _tlpFoot2 同比例：右列左边界落在同一处，两页页脚才对得齐。
            this._tlpFoot1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 46F));
            this._tlpFoot1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 54F));
            this._tlpFoot1.Controls.Add(this._lblCapture, 0, 0);
            this._tlpFoot1.Controls.Add(this._lblPresetInfo1, 1, 0);
            this._tlpFoot1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._tlpFoot1.Location = new System.Drawing.Point(12, 450);
            this._tlpFoot1.Name = "_tlpFoot1";
            this._tlpFoot1.RowCount = 1;
            this._tlpFoot1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._tlpFoot1.Size = new System.Drawing.Size(1168, 30);
            this._tlpFoot1.TabIndex = 2;
            // 
            // _lblCapture
            // 
            this._lblCapture.AutoEllipsis = true;
            this._lblCapture.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblCapture.Location = new System.Drawing.Point(0, 0);
            this._lblCapture.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this._lblCapture.Name = "_lblCapture";
            this._lblCapture.Size = new System.Drawing.Size(527, 30);
            this._lblCapture.TabIndex = 0;
            this._lblCapture.Text = "选中一行后可编辑；「油门按钮#」列填 0 起的编号。";
            this._lblCapture.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblPresetInfo1
            // 
            // 属性与 _lblSimHint2 逐项对齐：同字号、同对齐、同 Dock，
            // 两页页脚右侧因此长得一样——只改一处的话，字号差 0.5F 都能看出来。
            this._lblPresetInfo1.AutoEllipsis = true;
            this._lblPresetInfo1.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblPresetInfo1.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this._lblPresetInfo1.Location = new System.Drawing.Point(540, 0);
            this._lblPresetInfo1.Margin = new System.Windows.Forms.Padding(0);
            this._lblPresetInfo1.Name = "_lblPresetInfo1";
            this._lblPresetInfo1.Size = new System.Drawing.Size(628, 30);
            this._lblPresetInfo1.TabIndex = 1;
            this._lblPresetInfo1.Text = "右键表格可操作 · 双击可编辑";
            this._lblPresetInfo1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // _ctxBinding
            // 
            // 三段式，与 _ctxAlert 同骨架：捕获（读设备）── 新增 ── 删除。
            // 不留图片边距（不设 ShowImageMargin），与 _ctxAlert、管理菜单一致。
            this._ctxBinding.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._miCapture,
            this._miBindSepTop,
            this._miAddBind,
            this._miBindSepMid,
            this._miDelBind});
            this._ctxBinding.Name = "_ctxBinding";
            this._ctxBinding.Size = new System.Drawing.Size(200, 104);
            this._ctxBinding.Opening += new System.ComponentModel.CancelEventHandler(this.ctxBinding_Opening);
            // 
            // _miCapture
            // 
            this._miCapture.Name = "_miCapture";
            this._miCapture.Size = new System.Drawing.Size(199, 24);
            this._miCapture.Text = "捕获油门按键";
            this._miCapture.Click += new System.EventHandler(this.miCaptureBinding_Click);
            // 
            // _miBindSepTop
            // 
            this._miBindSepTop.Name = "_miBindSepTop";
            this._miBindSepTop.Size = new System.Drawing.Size(196, 6);
            // 
            // _miAddBind
            // 
            this._miAddBind.Name = "_miAddBind";
            this._miAddBind.Size = new System.Drawing.Size(199, 24);
            this._miAddBind.Text = "新增按键绑定";
            this._miAddBind.Click += new System.EventHandler(this.miAddBinding_Click);
            // 
            // _miBindSepMid
            // 
            this._miBindSepMid.Name = "_miBindSepMid";
            this._miBindSepMid.Size = new System.Drawing.Size(196, 6);
            // 
            // _miDelBind
            // 
            this._miDelBind.Name = "_miDelBind";
            this._miDelBind.Size = new System.Drawing.Size(199, 24);
            this._miDelBind.Text = "删除本条绑定";
            this._miDelBind.Click += new System.EventHandler(this.miDelBinding_Click);
            // 
            // _tlpPreset1
            // 
            this._tlpPreset1.ColumnCount = 5;
            this._tlpPreset1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 62F));
            this._tlpPreset1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 260F));
            this._tlpPreset1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 96F));
            this._tlpPreset1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 96F));
            this._tlpPreset1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._tlpPreset1.Controls.Add(this._lblPresetTitle1, 0, 0);
            this._tlpPreset1.Controls.Add(this._cboPreset1, 1, 0);
            this._tlpPreset1.Controls.Add(this._btnManage1, 2, 0);
            this._tlpPreset1.Controls.Add(this._lblChip1, 3, 0);
            this._tlpPreset1.Controls.Add(this._flBg, 0, 1);
            this._tlpPreset1.Dock = System.Windows.Forms.DockStyle.Top;
            this._tlpPreset1.Location = new System.Drawing.Point(12, 8);
            this._tlpPreset1.Name = "_tlpPreset1";
            this._tlpPreset1.RowCount = 2;
            this._tlpPreset1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this._tlpPreset1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this._tlpPreset1.SetColumnSpan(this._flBg, 5);
            this._tlpPreset1.SetColumnSpan(this._lblChip1, 2);
            this._tlpPreset1.Size = new System.Drawing.Size(1168, 80);
            this._tlpPreset1.TabIndex = 0;
            // 
            // _lblPresetTitle1
            // 
            this._lblPresetTitle1.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblPresetTitle1.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this._lblPresetTitle1.Location = new System.Drawing.Point(3, 0);
            this._lblPresetTitle1.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this._lblPresetTitle1.Name = "_lblPresetTitle1";
            this._lblPresetTitle1.Size = new System.Drawing.Size(56, 40);
            this._lblPresetTitle1.TabIndex = 0;
            this._lblPresetTitle1.Text = "配置";
            this._lblPresetTitle1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cboPreset1
            // 
            this._cboPreset1.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cboPreset1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cboPreset1.DropDownWidth = 420;
            this._cboPreset1.Location = new System.Drawing.Point(119, 6);
            this._cboPreset1.Margin = new System.Windows.Forms.Padding(3, 6, 6, 6);
            this._cboPreset1.Name = "_cboPreset1";
            this._cboPreset1.Size = new System.Drawing.Size(181, 25);
            this._cboPreset1.TabIndex = 1;
            // 
            // 
            // _btnManage1
            // 
            this._btnManage1.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnManage1.Location = new System.Drawing.Point(501, 4);
            this._btnManage1.Margin = new System.Windows.Forms.Padding(3, 4, 6, 4);
            this._btnManage1.Name = "_btnManage1";
            this._btnManage1.Size = new System.Drawing.Size(87, 32);
            this._btnManage1.TabIndex = 4;
            this._btnManage1.Text = "管理 ▾";
            this._btnManage1.UseVisualStyleBackColor = true;
            this._btnManage1.Click += new System.EventHandler(this.btnManage_Click);
            // 
            // _lblChip1
            // 
            this._lblChip1.AutoEllipsis = true;
            this._lblChip1.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblChip1.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this._lblChip1.Location = new System.Drawing.Point(600, 0);
            this._lblChip1.Margin = new System.Windows.Forms.Padding(6, 0, 3, 0);
            this._lblChip1.Name = "_lblChip1";
            this._lblChip1.Size = new System.Drawing.Size(565, 40);
            this._lblChip1.TabIndex = 4;
            this._lblChip1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _flBg
            // 
            this._flBg.Controls.Add(this._lblDef);
            this._flBg.Controls.Add(this._cboBgEffect);
            this._flBg.Controls.Add(this._lblBgColor);
            this._flBg.Controls.Add(this._cboBgColor);
            this._flBg.Controls.Add(this._lblSpd);
            this._flBg.Controls.Add(this._cboBgSpeed);
            this._flBg.Controls.Add(this._lblBgHint);
            this._flBg.Dock = System.Windows.Forms.DockStyle.Fill;
            this._flBg.Location = new System.Drawing.Point(3, 43);
            this._flBg.Name = "_flBg";
            this._flBg.Size = new System.Drawing.Size(1162, 34);
            this._flBg.TabIndex = 5;
            this._flBg.WrapContents = false;
            // 
            // _lblBgHint
            // 
            this._lblBgHint.AutoSize = true;
            this._lblBgHint.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this._lblBgHint.Location = new System.Drawing.Point(489, 10);
            this._lblBgHint.Margin = new System.Windows.Forms.Padding(3, 10, 3, 0);
            this._lblBgHint.Name = "_lblBgHint";
            this._lblBgHint.Size = new System.Drawing.Size(386, 16);
            this._lblBgHint.TabIndex = 6;
            this._lblBgHint.Text = "无触发时持续显示 · 只在按键模式生效 · 载入配置会连带设置底灯";
            // 
            // _pageAlert
            // 
            this._pageAlert.Controls.Add(this._dgvAlert);
            this._pageAlert.Controls.Add(this._flLegend);
            this._pageAlert.Controls.Add(this._tlpFoot2);
            this._pageAlert.Controls.Add(this._tlpPreset2);
            this._pageAlert.Location = new System.Drawing.Point(4, 26);
            this._pageAlert.Name = "_pageAlert";
            this._pageAlert.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this._pageAlert.Size = new System.Drawing.Size(1192, 488);
            this._pageAlert.TabIndex = 1;
            this._pageAlert.Text = "告警模式（MSFS2024 系统状态 → 灯光）";
            this._pageAlert.UseVisualStyleBackColor = true;
            // 
            // _dgvAlert
            // 
            this._dgvAlert.AllowUserToAddRows = false;
            this._dgvAlert.AllowUserToDeleteRows = false;
            this._dgvAlert.AllowUserToOrderColumns = false;
            this._dgvAlert.AllowUserToResizeRows = false;
            this._dgvAlert.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.None;
            this._dgvAlert.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._dgvAlert.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this._dgvAlert.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colAEnabled,
            this.colALed,
            this.colARule,
            this.colAColor,
            this.colAEffect,
            this.colASpeed,
            this.colACond,
            this.colARaw});
            this._dgvAlert.Dock = System.Windows.Forms.DockStyle.Fill;
            this._dgvAlert.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this._dgvAlert.Location = new System.Drawing.Point(12, 48);
            this._dgvAlert.MultiSelect = false;
            this._dgvAlert.ContextMenuStrip = this._ctxAlert;
            this._dgvAlert.Name = "_dgvAlert";
            this._dgvAlert.RowHeadersVisible = true;
            this._dgvAlert.RowHeadersWidth = 36;
            this._dgvAlert.RowTemplate.Height = 26;
            this._dgvAlert.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this._dgvAlert.Size = new System.Drawing.Size(1168, 402);
            this._dgvAlert.TabIndex = 1;
            this._dgvAlert.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvSim_CellDoubleClick);
            this._dgvAlert.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvSim_CellValueChanged);
            this._dgvAlert.RowPostPaint += new System.Windows.Forms.DataGridViewRowPostPaintEventHandler(this.dgv_RowPostPaint);
            // 
            // colAEnabled
            // 
            this.colAEnabled.FillWeight = 6F;
            this.colAEnabled.HeaderText = "启用";
            this.colAEnabled.MinimumWidth = 44;
            this.colAEnabled.Name = "colAEnabled";
            this.colAEnabled.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colAEnabled.Width = 48;
            // 
            // colALed
            // 
            this.colALed.FillWeight = 6F;
            this.colALed.HeaderText = "灯位";
            this.colALed.MinimumWidth = 50;
            this.colALed.Name = "colALed";
            this.colALed.ReadOnly = true;
            this.colALed.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colALed.Width = 54;
            // 
            // colARule
            // 
            this.colARule.FillWeight = 30F;
            this.colARule.HeaderText = "系统 · 规则";
            this.colARule.MinimumWidth = 180;
            this.colARule.Name = "colARule";
            this.colARule.ReadOnly = true;
            this.colARule.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colARule.Width = 220;
            // 
            // colAColor
            // 
            this.colAColor.FillWeight = 9F;
            this.colAColor.HeaderText = "颜色";
            this.colAColor.Items.AddRange(new object[] {
            "关",
            "绿",
            "琥珀",
            "红"});
            this.colAColor.MinimumWidth = 66;
            this.colAColor.Name = "colAColor";
            this.colAColor.Width = 70;
            // 
            // colAEffect
            // 
            this.colAEffect.FillWeight = 12F;
            this.colAEffect.HeaderText = "效果";
            this.colAEffect.Items.AddRange(new object[] {
            "关灯",
            "常亮",
            "慢闪",
            "双闪",
            "一长一短",
            "快闪",
            "心跳"});
            this.colAEffect.MinimumWidth = 96;
            this.colAEffect.Name = "colAEffect";
            this.colAEffect.Width = 104;
            // 
            // colASpeed
            // 
            this.colASpeed.FillWeight = 7F;
            this.colASpeed.HeaderText = "速度";
            this.colASpeed.MinimumWidth = 52;
            this.colASpeed.Name = "colASpeed";
            this.colASpeed.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colASpeed.Width = 56;
            // 
            // colACond
            // 
            this.colACond.FillWeight = 32F;
            this.colACond.HeaderText = "触发条件";
            this.colACond.MinimumWidth = 200;
            this.colACond.Name = "colACond";
            this.colACond.ReadOnly = true;
            this.colACond.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colACond.Width = 240;
            // 
            // colARaw
            // 
            this.colARaw.HeaderText = "原始值";
            this.colARaw.MinimumWidth = 60;
            this.colARaw.Name = "colARaw";
            this.colARaw.ReadOnly = true;
            this.colARaw.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colARaw.Width = 120;
            // 
            // _tlpFoot2
            // 
            this._tlpFoot2.ColumnCount = 2;
            this._tlpFoot2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 46F));
            this._tlpFoot2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 54F));
            this._tlpFoot2.Controls.Add(this._lblAlertCounts, 0, 0);
            this._tlpFoot2.Controls.Add(this._lblSimHint2, 1, 0);
            this._tlpFoot2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._tlpFoot2.Location = new System.Drawing.Point(12, 450);
            this._tlpFoot2.Name = "_tlpFoot2";
            this._tlpFoot2.RowCount = 1;
            this._tlpFoot2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._tlpFoot2.Size = new System.Drawing.Size(1168, 30);
            this._tlpFoot2.TabIndex = 2;
            // 
            // _lblAlertCounts
            // 
            this._lblAlertCounts.AutoEllipsis = true;
            this._lblAlertCounts.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblAlertCounts.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this._lblAlertCounts.Location = new System.Drawing.Point(0, 0);
            this._lblAlertCounts.Margin = new System.Windows.Forms.Padding(0);
            this._lblAlertCounts.Name = "_lblAlertCounts";
            this._lblAlertCounts.Size = new System.Drawing.Size(537, 30);
            this._lblAlertCounts.TabIndex = 0;
            this._lblAlertCounts.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblSimHint2
            // 
            this._lblSimHint2.AutoEllipsis = true;
            this._lblSimHint2.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblSimHint2.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this._lblSimHint2.Location = new System.Drawing.Point(540, 0);
            this._lblSimHint2.Margin = new System.Windows.Forms.Padding(0);
            this._lblSimHint2.Name = "_lblSimHint2";
            this._lblSimHint2.Size = new System.Drawing.Size(628, 30);
            this._lblSimHint2.TabIndex = 1;
            this._lblSimHint2.Text = "勾选即生效 · 双击「触发条件」可改条件";
            this._lblSimHint2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // _tlpPreset2
            // 
            // 单行 4 列：配置 / 下拉 / 管理菜单 / 状态徽标。
            // 1.3.44 移除「机型 + 阈值」整行（阈值机制已删除），配置与管理上移一行。
            this._tlpPreset2.ColumnCount = 4;
            this._tlpPreset2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 62F));
            this._tlpPreset2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 260F));
            this._tlpPreset2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 96F));
            this._tlpPreset2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._tlpPreset2.Controls.Add(this._lblPresetTitle2, 0, 0);
            this._tlpPreset2.Controls.Add(this._cboPreset2, 1, 0);
            this._tlpPreset2.Controls.Add(this._btnManage2, 2, 0);
            this._tlpPreset2.Controls.Add(this._lblChip2, 3, 0);
            this._tlpPreset2.Dock = System.Windows.Forms.DockStyle.Top;
            this._tlpPreset2.Location = new System.Drawing.Point(12, 8);
            this._tlpPreset2.Name = "_tlpPreset2";
            this._tlpPreset2.RowCount = 1;
            this._tlpPreset2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this._tlpPreset2.Size = new System.Drawing.Size(1168, 40);
            this._tlpPreset2.TabIndex = 0;
            // 
            // _lblPresetTitle2
            // 
            this._lblPresetTitle2.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblPresetTitle2.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this._lblPresetTitle2.Location = new System.Drawing.Point(3, 0);
            this._lblPresetTitle2.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this._lblPresetTitle2.Name = "_lblPresetTitle2";
            this._lblPresetTitle2.Size = new System.Drawing.Size(56, 40);
            this._lblPresetTitle2.TabIndex = 8;
            this._lblPresetTitle2.Text = "配置";
            this._lblPresetTitle2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cboPreset2
            // 
            this._cboPreset2.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cboPreset2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cboPreset2.DropDownWidth = 420;
            this._cboPreset2.Location = new System.Drawing.Point(51, 6);
            this._cboPreset2.Margin = new System.Windows.Forms.Padding(3, 6, 6, 6);
            this._cboPreset2.Name = "_cboPreset2";
            this._cboPreset2.Size = new System.Drawing.Size(191, 28);
            this._cboPreset2.TabIndex = 9;
            // 
            // 
            // _btnManage2
            // 
            this._btnManage2.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnManage2.Location = new System.Drawing.Point(251, 4);
            this._btnManage2.Margin = new System.Windows.Forms.Padding(3, 4, 6, 4);
            this._btnManage2.Name = "_btnManage2";
            this._btnManage2.Size = new System.Drawing.Size(87, 32);
            this._btnManage2.TabIndex = 12;
            this._btnManage2.Text = "管理 ▾";
            this._btnManage2.UseVisualStyleBackColor = true;
            this._btnManage2.Click += new System.EventHandler(this.btnManage_Click);
            // 
            // _ctxAlert
            // 
            this._ctxAlert.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._miAudit,
            this._miEditCond,
            this._miSepTop,
            this._miAddRule,
            this._miSepMid,
            this._miDelRule});
            this._ctxAlert.Name = "_ctxAlert";
            this._ctxAlert.Size = new System.Drawing.Size(220, 132);
            this._ctxAlert.Opening += new System.ComponentModel.CancelEventHandler(this.ctxAlert_Opening);
            // 
            // _miAudit
            // 
            this._miAudit.Name = "_miAudit";
            this._miAudit.Size = new System.Drawing.Size(220, 24);
            this._miAudit.Text = "检查列表规则…";
            this._miAudit.Click += new System.EventHandler(this.miAudit_Click);
            // 
            // _miEditCond
            // 
            this._miEditCond.Name = "_miEditCond";
            this._miEditCond.Size = new System.Drawing.Size(220, 24);
            this._miEditCond.Text = "编辑本条规则的条件…";
            this._miEditCond.Click += new System.EventHandler(this.miEditCond_Click);
            // 
            // _miSepTop
            // 
            this._miSepTop.Name = "_miSepTop";
            this._miSepTop.Size = new System.Drawing.Size(217, 6);
            // 
            // _miAddRule
            // 
            this._miAddRule.Name = "_miAddRule";
            this._miAddRule.Size = new System.Drawing.Size(220, 24);
            this._miAddRule.Text = "新增规则…";
            this._miAddRule.Click += new System.EventHandler(this.btnAddRule_Click);
            // 
            // _miSepMid
            // 
            this._miSepMid.Name = "_miSepMid";
            this._miSepMid.Size = new System.Drawing.Size(217, 6);
            // 
            // _miDelRule
            // 
            this._miDelRule.Name = "_miDelRule";
            this._miDelRule.Size = new System.Drawing.Size(220, 24);
            this._miDelRule.Text = "删除本条规则";
            this._miDelRule.Click += new System.EventHandler(this.btnDelRule_Click);
            // 
            // _lblChip2
            // 
            this._lblChip2.AutoEllipsis = true;
            this._lblChip2.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblChip2.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this._lblChip2.Location = new System.Drawing.Point(347, 4);
            this._lblChip2.Margin = new System.Windows.Forms.Padding(6, 0, 3, 0);
            this._lblChip2.Name = "_lblChip2";
            this._lblChip2.Size = new System.Drawing.Size(350, 40);
            this._lblChip2.TabIndex = 12;
            this._lblChip2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _flLegend
            // 
            this._flLegend.Controls.Add(this._lblLgOk);
            this._flLegend.Controls.Add(this._lblLgWarn);
            this._flLegend.Controls.Add(this._lblLgErr);
            this._flLegend.Dock = System.Windows.Forms.DockStyle.Top;
            this._flLegend.Location = new System.Drawing.Point(12, 20);
            this._flLegend.Name = "_flLegend";
            this._flLegend.Padding = new System.Windows.Forms.Padding(2, 4, 2, 2);
            this._flLegend.Size = new System.Drawing.Size(1168, 26);
            this._flLegend.TabIndex = 0;
            this._flLegend.WrapContents = false;
            // 
            // _lblLgOk
            // 
            this._lblLgOk.AutoSize = true;
            this._lblLgOk.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this._lblLgOk.Location = new System.Drawing.Point(2, 4);
            this._lblLgOk.Margin = new System.Windows.Forms.Padding(0, 0, 18, 0);
            this._lblLgOk.Name = "_lblLgOk";
            this._lblLgOk.Size = new System.Drawing.Size(58, 17);
            this._lblLgOk.TabIndex = 0;
            this._lblLgOk.Text = "● 正常";
            // 
            // _lblLgWarn
            // 
            this._lblLgWarn.AutoSize = true;
            this._lblLgWarn.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this._lblLgWarn.Location = new System.Drawing.Point(78, 4);
            this._lblLgWarn.Margin = new System.Windows.Forms.Padding(0, 0, 18, 0);
            this._lblLgWarn.Name = "_lblLgWarn";
            this._lblLgWarn.Size = new System.Drawing.Size(58, 17);
            this._lblLgWarn.TabIndex = 1;
            this._lblLgWarn.Text = "● 留意";
            // 
            // _lblLgErr
            // 
            this._lblLgErr.AutoSize = true;
            this._lblLgErr.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this._lblLgErr.Location = new System.Drawing.Point(154, 4);
            this._lblLgErr.Margin = new System.Windows.Forms.Padding(0);
            this._lblLgErr.Name = "_lblLgErr";
            this._lblLgErr.Size = new System.Drawing.Size(58, 17);
            this._lblLgErr.TabIndex = 2;
            this._lblLgErr.Text = "● 处置";
            // 
            // _statusBar
            // 
            this._statusBar.Controls.Add(this._statusTlp);
            this._statusBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._statusBar.Location = new System.Drawing.Point(0, 802);
            this._statusBar.Name = "_statusBar";
            this._statusBar.Size = new System.Drawing.Size(1200, 26);
            this._statusBar.TabIndex = 3;
            // 
            // _statusTlp
            // 
            this._statusTlp.ColumnCount = 4;
            this._statusTlp.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this._statusTlp.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this._statusTlp.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this._statusTlp.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._statusTlp.Controls.Add(this._lblPressed, 0, 0);
            this._statusTlp.Controls.Add(this._lblMode, 1, 0);
            this._statusTlp.Controls.Add(this._lblSource, 2, 0);
            this._statusTlp.Controls.Add(this._lblPollErr, 3, 0);
            this._statusTlp.Dock = System.Windows.Forms.DockStyle.Fill;
            this._statusTlp.Location = new System.Drawing.Point(0, 0);
            this._statusTlp.Name = "_statusTlp";
            this._statusTlp.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this._statusTlp.RowCount = 1;
            this._statusTlp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._statusTlp.Size = new System.Drawing.Size(1200, 26);
            this._statusTlp.TabIndex = 0;
            // 
            // _lblPressed
            // 
            this._lblPressed.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._lblPressed.AutoSize = true;
            this._lblPressed.Location = new System.Drawing.Point(13, 5);
            this._lblPressed.Margin = new System.Windows.Forms.Padding(3, 0, 16, 0);
            this._lblPressed.Name = "_lblPressed";
            this._lblPressed.Size = new System.Drawing.Size(68, 17);
            this._lblPressed.TabIndex = 0;
            this._lblPressed.Text = "当前按下: -";
            // 
            // _lblMode
            // 
            this._lblMode.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._lblMode.AutoSize = true;
            this._lblMode.Location = new System.Drawing.Point(100, 4);
            this._lblMode.Margin = new System.Windows.Forms.Padding(3, 0, 20, 0);
            this._lblMode.Name = "_lblMode";
            this._lblMode.Size = new System.Drawing.Size(231, 17);
            this._lblMode.TabIndex = 1;
            this._lblMode.Text = "当前模式：按键模式（按钮 → 灯光）";
            // 
            // _lblSource
            // 
            this._lblSource.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._lblSource.AutoSize = true;
            this._lblSource.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this._lblSource.Location = new System.Drawing.Point(354, 4);
            this._lblSource.Margin = new System.Windows.Forms.Padding(3, 0, 16, 0);
            this._lblSource.Name = "_lblSource";
            this._lblSource.Size = new System.Drawing.Size(90, 17);
            this._lblSource.TabIndex = 2;
            this._lblSource.Text = "● 数据源：—";
            // 
            // _lblPollErr
            // 
            this._lblPollErr.AutoEllipsis = true;
            this._lblPollErr.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblPollErr.Location = new System.Drawing.Point(460, 0);
            this._lblPollErr.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this._lblPollErr.Name = "_lblPollErr";
            this._lblPollErr.Size = new System.Drawing.Size(727, 26);
            this._lblPollErr.TabIndex = 2;
            this._lblPollErr.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 828);
            this.Controls.Add(this._tabs);
            this.Controls.Add(this._statusBar);
            this.Controls.Add(this._barMid);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.MinimumSize = new System.Drawing.Size(1024, 520);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            // 版本号不写在这里：运行期由 MainForm 构造函数用 AppInfo.Title 覆盖，
            // 唯一来源是 csproj 的 <Version>，出包不必再改这一行。
            this.Text = "G940LED面板";
            this._barMid.ResumeLayout(false);
            this._midTlp.ResumeLayout(false);
            this._devCard.ResumeLayout(false);
            this._devHost.ResumeLayout(false);
            this._devBtnRow.ResumeLayout(false);
            this._flSetOpts.ResumeLayout(false);
            this._flSetOpts.PerformLayout();
            this._simCard.ResumeLayout(false);
            this._simHost.ResumeLayout(false);
            this._simBtnRow.ResumeLayout(false);
            this._tabs.ResumeLayout(false);
            this._pageButtons.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._dgv)).EndInit();
            this._tlpFoot1.ResumeLayout(false);
            this._flBg.ResumeLayout(false);
            this._flBg.PerformLayout();
            this._tlpPreset1.ResumeLayout(false);
            this._pageAlert.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._dgvAlert)).EndInit();
            this._tlpFoot2.ResumeLayout(false);
            this._tlpPreset2.ResumeLayout(false);
            this._statusBar.ResumeLayout(false);
            this._statusTlp.ResumeLayout(false);
            this._statusTlp.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}
