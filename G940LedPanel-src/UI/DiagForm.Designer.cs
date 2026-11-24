namespace G940LedPanel.UI
{
    /// <summary>
    /// 设计器文件 —— 设备诊断窗口。说明文字 / 设备下拉 / 输出框 / 工具条（暂停·导出·复制·关闭），
    /// 可直接在 VS 设计器里调整高度与字体。逻辑在 DiagForm.cs。
    /// </summary>
    partial class DiagForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label _lblTip;
        private System.Windows.Forms.ComboBox _cboScan;
        private System.Windows.Forms.RichTextBox _box;
        private System.Windows.Forms.TableLayoutPanel _barTools;
        private System.Windows.Forms.Button _btnPause;
        private System.Windows.Forms.Button _btnExport;
        private System.Windows.Forms.Button _btnCopy;
        private System.Windows.Forms.Button _btnHidProbe;
        private System.Windows.Forms.Label _lblPaused;
        private System.Windows.Forms.Button _btnClose;

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
            this._lblTip = new System.Windows.Forms.Label();
            this._cboScan = new System.Windows.Forms.ComboBox();
            this._box = new System.Windows.Forms.RichTextBox();
            this._barTools = new System.Windows.Forms.TableLayoutPanel();
            this._btnPause = new System.Windows.Forms.Button();
            this._btnExport = new System.Windows.Forms.Button();
            this._btnCopy = new System.Windows.Forms.Button();
            this._btnHidProbe = new System.Windows.Forms.Button();
            this._lblPaused = new System.Windows.Forms.Label();
            this._btnClose = new System.Windows.Forms.Button();
            this._barTools.SuspendLayout();
            this.SuspendLayout();
            // 
            // _lblTip
            // 
            this._lblTip.Dock = System.Windows.Forms.DockStyle.Top;
            this._lblTip.Location = new System.Drawing.Point(0, 0);
            this._lblTip.Name = "_lblTip";
            this._lblTip.Padding = new System.Windows.Forms.Padding(12, 8, 12, 0);
            this._lblTip.Size = new System.Drawing.Size(860, 70);
            this._lblTip.TabIndex = 0;
            this._lblTip.Text = "选择设备后实时监控：按住【G940 上的按钮】并拨动油门杆，观察数值变化。\r\n下方【原始输入】区直接显示 HID 报告 —— 若 DirectInput 全 0 但原始输入有变化，按钮读取将自动切换成功。\r\n要留下证据点【暂停】锁定画面，再点【导出报告】存成 txt。";
            // 
            // _cboScan
            // 
            this._cboScan.Dock = System.Windows.Forms.DockStyle.Top;
            this._cboScan.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cboScan.Location = new System.Drawing.Point(0, 70);
            this._cboScan.Name = "_cboScan";
            this._cboScan.Size = new System.Drawing.Size(860, 25);
            this._cboScan.TabIndex = 1;
            this._cboScan.SelectedIndexChanged += new System.EventHandler(this.cboScan_SelectedIndexChanged);
            // 
            // _box
            // 
            this._box.BackColor = System.Drawing.Color.White;
            this._box.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._box.Dock = System.Windows.Forms.DockStyle.Fill;
            this._box.Font = new System.Drawing.Font("Consolas", 9F);
            this._box.Location = new System.Drawing.Point(0, 95);
            this._box.Name = "_box";
            this._box.ReadOnly = true;
            this._box.Size = new System.Drawing.Size(860, 545);
            this._box.TabIndex = 2;
            this._box.Text = "";
            this._box.WordWrap = false;
            // 
            // _barTools
            // 
            this._barTools.ColumnCount = 6;
            this._barTools.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 96F));
            this._barTools.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 104F));
            this._barTools.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 82F));
            this._barTools.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 96F));
            this._barTools.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._barTools.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 96F));
            this._barTools.Controls.Add(this._btnPause, 0, 0);
            this._barTools.Controls.Add(this._btnExport, 1, 0);
            this._barTools.Controls.Add(this._btnCopy, 2, 0);
            this._barTools.Controls.Add(this._btnHidProbe, 3, 0);
            this._barTools.Controls.Add(this._lblPaused, 4, 0);
            this._barTools.Controls.Add(this._btnClose, 5, 0);
            this._barTools.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._barTools.Location = new System.Drawing.Point(0, 640);
            this._barTools.Name = "_barTools";
            this._barTools.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this._barTools.RowCount = 1;
            this._barTools.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._barTools.Size = new System.Drawing.Size(860, 40);
            this._barTools.TabIndex = 3;
            // 
            // _btnPause
            // 
            this._btnPause.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnPause.Location = new System.Drawing.Point(11, 7);
            this._btnPause.Name = "_btnPause";
            this._btnPause.Size = new System.Drawing.Size(90, 26);
            this._btnPause.TabIndex = 0;
            this._btnPause.Text = "暂停";
            this._btnPause.UseVisualStyleBackColor = true;
            this._btnPause.Click += new System.EventHandler(this.btnPause_Click);
            // 
            // _btnExport
            // 
            this._btnExport.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnExport.Location = new System.Drawing.Point(107, 7);
            this._btnExport.Name = "_btnExport";
            this._btnExport.Size = new System.Drawing.Size(98, 26);
            this._btnExport.TabIndex = 1;
            this._btnExport.Text = "导出报告";
            this._btnExport.UseVisualStyleBackColor = true;
            this._btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // _btnCopy
            // 
            this._btnCopy.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnCopy.Location = new System.Drawing.Point(211, 7);
            this._btnCopy.Name = "_btnCopy";
            this._btnCopy.Size = new System.Drawing.Size(76, 26);
            this._btnCopy.TabIndex = 2;
            this._btnCopy.Text = "复制";
            this._btnCopy.UseVisualStyleBackColor = true;
            this._btnCopy.Click += new System.EventHandler(this.btnCopy_Click);
            // 
            // _btnHidProbe
            // 
            this._btnHidProbe.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnHidProbe.Location = new System.Drawing.Point(293, 7);
            this._btnHidProbe.Name = "_btnHidProbe";
            this._btnHidProbe.Size = new System.Drawing.Size(90, 26);
            this._btnHidProbe.TabIndex = 3;
            this._btnHidProbe.Text = "HID 探针";
            this._btnHidProbe.UseVisualStyleBackColor = true;
            this._btnHidProbe.Click += new System.EventHandler(this.btnHidProbe_Click);
            // 
            // _lblPaused
            // 
            this._lblPaused.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._lblPaused.AutoSize = true;
            this._lblPaused.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this._lblPaused.Location = new System.Drawing.Point(389, 11);
            this._lblPaused.Name = "_lblPaused";
            this._lblPaused.Size = new System.Drawing.Size(200, 16);
            this._lblPaused.TabIndex = 4;
            this._lblPaused.Text = "";
            // 
            // _btnClose
            // 
            this._btnClose.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnClose.Location = new System.Drawing.Point(759, 7);
            this._btnClose.Name = "_btnClose";
            this._btnClose.Size = new System.Drawing.Size(90, 26);
            this._btnClose.TabIndex = 5;
            this._btnClose.Text = "关闭";
            this._btnClose.UseVisualStyleBackColor = true;
            this._btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // DiagForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(860, 680);
            this.Controls.Add(this._box);
            this.Controls.Add(this._cboScan);
            this.Controls.Add(this._lblTip);
            this.Controls.Add(this._barTools);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DiagForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "设备诊断（全设备扫描）";
            this._barTools.ResumeLayout(false);
            this._barTools.PerformLayout();
            this.ResumeLayout(false);

        }
    }
}
