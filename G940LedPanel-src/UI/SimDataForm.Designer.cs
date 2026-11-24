namespace G940LedPanel.UI
{
    /// <summary>
    /// 设计器文件 —— MSFS2024 实时数据窗。搜索框 / 两个过滤勾选 / 变量表 / 底部统计与导出。
    /// 逻辑在 SimDataForm.cs。
    /// </summary>
    partial class SimDataForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel _barTop;
        private System.Windows.Forms.TextBox _txtSearch;
        private System.Windows.Forms.CheckBox _chkOnlyAnswered;
        private System.Windows.Forms.CheckBox _chkOnlyChanged;
        private System.Windows.Forms.CheckBox _chkOnTop;
        private System.Windows.Forms.DataGridView _grid;
        private System.Windows.Forms.DataGridViewTextBoxColumn colGroup;
        private System.Windows.Forms.DataGridViewTextBoxColumn colVar;
        private System.Windows.Forms.DataGridViewTextBoxColumn colUnit;
        private System.Windows.Forms.DataGridViewTextBoxColumn colValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAnswered;
        private System.Windows.Forms.DataGridViewTextBoxColumn colChanges;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAge;
        private System.Windows.Forms.TableLayoutPanel _barFoot;
        private System.Windows.Forms.Label _lblStat;
        private System.Windows.Forms.Button _btnExport;
        private System.Windows.Forms.Button _btnCopy;
        private System.Windows.Forms.Button _btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this._barTop = new System.Windows.Forms.TableLayoutPanel();
            this._txtSearch = new System.Windows.Forms.TextBox();
            this._chkOnlyAnswered = new System.Windows.Forms.CheckBox();
            this._chkOnlyChanged = new System.Windows.Forms.CheckBox();
            this._chkOnTop = new System.Windows.Forms.CheckBox();
            this._grid = new System.Windows.Forms.DataGridView();
            this.colGroup = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colVar = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colUnit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAnswered = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colChanges = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAge = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._barFoot = new System.Windows.Forms.TableLayoutPanel();
            this._lblStat = new System.Windows.Forms.Label();
            this._btnExport = new System.Windows.Forms.Button();
            this._btnCopy = new System.Windows.Forms.Button();
            this._btnClose = new System.Windows.Forms.Button();
            this._barTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._grid)).BeginInit();
            this._barFoot.SuspendLayout();
            this.SuspendLayout();
            // 
            // _barTop
            // 
            this._barTop.ColumnCount = 4;
            this._barTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 210F));
            this._barTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this._barTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this._barTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._barTop.Controls.Add(this._txtSearch, 0, 0);
            this._barTop.Controls.Add(this._chkOnlyAnswered, 1, 0);
            this._barTop.Controls.Add(this._chkOnlyChanged, 2, 0);
            this._barTop.Controls.Add(this._chkOnTop, 3, 0);
            this._barTop.Dock = System.Windows.Forms.DockStyle.Top;
            this._barTop.Location = new System.Drawing.Point(0, 0);
            this._barTop.Name = "_barTop";
            this._barTop.Padding = new System.Windows.Forms.Padding(8, 6, 8, 4);
            this._barTop.RowCount = 1;
            this._barTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._barTop.Size = new System.Drawing.Size(880, 38);
            this._barTop.TabIndex = 0;
            // 
            // _txtSearch
            // 
            this._txtSearch.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtSearch.Location = new System.Drawing.Point(11, 9);
            this._txtSearch.Name = "_txtSearch";
            this._txtSearch.Size = new System.Drawing.Size(204, 25);
            this._txtSearch.TabIndex = 0;
            this._txtSearch.TextChanged += new System.EventHandler(this.Filter_Changed);
            // 
            // _chkOnlyAnswered
            // 
            this._chkOnlyAnswered.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._chkOnlyAnswered.AutoSize = true;
            this._chkOnlyAnswered.Location = new System.Drawing.Point(221, 11);
            this._chkOnlyAnswered.Name = "_chkOnlyAnswered";
            this._chkOnlyAnswered.Size = new System.Drawing.Size(99, 21);
            this._chkOnlyAnswered.TabIndex = 1;
            this._chkOnlyAnswered.Text = "只看已应答";
            this._chkOnlyAnswered.UseVisualStyleBackColor = true;
            this._chkOnlyAnswered.CheckedChanged += new System.EventHandler(this.Filter_Changed);
            // 
            // _chkOnlyChanged
            // 
            this._chkOnlyChanged.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._chkOnlyChanged.AutoSize = true;
            this._chkOnlyChanged.Location = new System.Drawing.Point(331, 11);
            this._chkOnlyChanged.Name = "_chkOnlyChanged";
            this._chkOnlyChanged.Size = new System.Drawing.Size(99, 21);
            this._chkOnlyChanged.TabIndex = 2;
            this._chkOnlyChanged.Text = "只看有变化";
            this._chkOnlyChanged.UseVisualStyleBackColor = true;
            this._chkOnlyChanged.CheckedChanged += new System.EventHandler(this.Filter_Changed);
            // 
            // _chkOnTop
            // 
            this._chkOnTop.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._chkOnTop.AutoSize = true;
            this._chkOnTop.Location = new System.Drawing.Point(451, 11);
            this._chkOnTop.Name = "_chkOnTop";
            this._chkOnTop.Size = new System.Drawing.Size(87, 21);
            this._chkOnTop.TabIndex = 3;
            this._chkOnTop.Text = "总在最前";
            this._chkOnTop.UseVisualStyleBackColor = true;
            this._chkOnTop.CheckedChanged += new System.EventHandler(this.chkOnTop_CheckedChanged);
            // 
            // _grid
            // 
            this._grid.AllowUserToAddRows = false;
            this._grid.AllowUserToDeleteRows = false;
            this._grid.AllowUserToResizeRows = false;
            this._grid.BackgroundColor = System.Drawing.Color.White;
            this._grid.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this._grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colGroup,
            this.colVar,
            this.colUnit,
            this.colValue,
            this.colAnswered,
            this.colChanges,
            this.colAge});
            this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._grid.Location = new System.Drawing.Point(0, 38);
            this._grid.MultiSelect = false;
            this._grid.Name = "_grid";
            this._grid.ReadOnly = true;
            this._grid.RowHeadersVisible = false;
            this._grid.RowTemplate.Height = 24;
            this._grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this._grid.Size = new System.Drawing.Size(880, 452);
            this._grid.TabIndex = 1;
            // 
            // colGroup
            // 
            this.colGroup.HeaderText = "分组";
            this.colGroup.Name = "colGroup";
            this.colGroup.ReadOnly = true;
            this.colGroup.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colGroup.Width = 76;
            // 
            // colVar
            // 
            this.colVar.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colVar.HeaderText = "变量";
            this.colVar.Name = "colVar";
            this.colVar.ReadOnly = true;
            this.colVar.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colUnit
            // 
            this.colUnit.HeaderText = "单位";
            this.colUnit.Name = "colUnit";
            this.colUnit.ReadOnly = true;
            this.colUnit.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colUnit.Width = 110;
            // 
            // colValue
            // 
            this.colValue.HeaderText = "当前值";
            this.colValue.Name = "colValue";
            this.colValue.ReadOnly = true;
            this.colValue.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colValue.Width = 90;
            // 
            // colAnswered
            // 
            this.colAnswered.HeaderText = "应答";
            this.colAnswered.Name = "colAnswered";
            this.colAnswered.ReadOnly = true;
            this.colAnswered.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colAnswered.Width = 60;
            // 
            // colChanges
            // 
            this.colChanges.HeaderText = "变化";
            this.colChanges.Name = "colChanges";
            this.colChanges.ReadOnly = true;
            this.colChanges.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colChanges.Width = 56;
            // 
            // colAge
            // 
            this.colAge.HeaderText = "最近更新";
            this.colAge.Name = "colAge";
            this.colAge.ReadOnly = true;
            this.colAge.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colAge.Width = 80;
            // 
            // _barFoot
            // 
            this._barFoot.ColumnCount = 4;
            this._barFoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._barFoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 104F));
            this._barFoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 84F));
            this._barFoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this._barFoot.Controls.Add(this._lblStat, 0, 0);
            this._barFoot.Controls.Add(this._btnExport, 1, 0);
            this._barFoot.Controls.Add(this._btnCopy, 2, 0);
            this._barFoot.Controls.Add(this._btnClose, 3, 0);
            this._barFoot.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._barFoot.Location = new System.Drawing.Point(0, 490);
            this._barFoot.Name = "_barFoot";
            this._barFoot.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this._barFoot.RowCount = 1;
            this._barFoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._barFoot.Size = new System.Drawing.Size(880, 40);
            this._barFoot.TabIndex = 2;
            // 
            // _lblStat
            // 
            this._lblStat.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._lblStat.AutoSize = true;
            this._lblStat.Location = new System.Drawing.Point(11, 11);
            this._lblStat.Name = "_lblStat";
            this._lblStat.Size = new System.Drawing.Size(200, 17);
            this._lblStat.TabIndex = 0;
            this._lblStat.Text = "尚未收到数据";
            // 
            // _btnExport
            // 
            this._btnExport.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnExport.Location = new System.Drawing.Point(601, 7);
            this._btnExport.Name = "_btnExport";
            this._btnExport.Size = new System.Drawing.Size(98, 26);
            this._btnExport.TabIndex = 1;
            this._btnExport.Text = "导出 txt";
            this._btnExport.UseVisualStyleBackColor = true;
            this._btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // _btnCopy
            // 
            this._btnCopy.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnCopy.Location = new System.Drawing.Point(705, 7);
            this._btnCopy.Name = "_btnCopy";
            this._btnCopy.Size = new System.Drawing.Size(78, 26);
            this._btnCopy.TabIndex = 2;
            this._btnCopy.Text = "复制";
            this._btnCopy.UseVisualStyleBackColor = true;
            this._btnCopy.Click += new System.EventHandler(this.btnCopy_Click);
            // 
            // _btnClose
            // 
            this._btnClose.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnClose.Location = new System.Drawing.Point(789, 7);
            this._btnClose.Name = "_btnClose";
            this._btnClose.Size = new System.Drawing.Size(84, 26);
            this._btnClose.TabIndex = 3;
            this._btnClose.Text = "关闭";
            this._btnClose.UseVisualStyleBackColor = true;
            this._btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // SimDataForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(880, 530);
            this.Controls.Add(this._grid);
            this.Controls.Add(this._barTop);
            this.Controls.Add(this._barFoot);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.MinimumSize = new System.Drawing.Size(620, 360);
            this.Name = "SimDataForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "实时数据 — MSFS2024（SimConnect）";
            this._barTop.ResumeLayout(false);
            this._barTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
            this._barFoot.ResumeLayout(false);
            this._barFoot.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
