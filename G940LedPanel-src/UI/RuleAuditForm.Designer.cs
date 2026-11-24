namespace G940LedPanel.UI
{
    /// <summary>
    /// 设计器文件 —— 规则体检清单窗口。说明文字 / 清单 / 底部（计数 + 关闭），
    /// 可直接在 VS 设计器里调整列宽与字号。逻辑在 RuleAuditForm.cs。
    /// </summary>
    partial class RuleAuditForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label _lblTip;
        private System.Windows.Forms.ListView _list;
        private System.Windows.Forms.ColumnHeader _colLevel;
        private System.Windows.Forms.ColumnHeader _colSys;
        private System.Windows.Forms.ColumnHeader _colRule;
        private System.Windows.Forms.ColumnHeader _colText;
        private System.Windows.Forms.TableLayoutPanel _bar;
        private System.Windows.Forms.Label _lblCount;
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
            this._list = new System.Windows.Forms.ListView();
            this._colLevel = new System.Windows.Forms.ColumnHeader();
            this._colSys = new System.Windows.Forms.ColumnHeader();
            this._colRule = new System.Windows.Forms.ColumnHeader();
            this._colText = new System.Windows.Forms.ColumnHeader();
            this._bar = new System.Windows.Forms.TableLayoutPanel();
            this._lblCount = new System.Windows.Forms.Label();
            this._btnClose = new System.Windows.Forms.Button();
            this._bar.SuspendLayout();
            this.SuspendLayout();
            // 
            // _lblTip
            // 
            this._lblTip.Dock = System.Windows.Forms.DockStyle.Top;
            this._lblTip.Location = new System.Drawing.Point(0, 0);
            this._lblTip.Name = "_lblTip";
            this._lblTip.Padding = new System.Windows.Forms.Padding(12, 8, 12, 0);
            this._lblTip.Size = new System.Drawing.Size(900, 64);
            this._lblTip.TabIndex = 0;
            this._lblTip.Text = "检查同一个系统内，你的规则和内置规则放一起会不会互相压住。\r\n只看两件事：条件能不能同时成立、同时成立时两条规则的输出是否一致。不亮本身不是问题。\r\n每条结论都附一个具体数值，可以复制到条件编辑器的「手动试算」里原样复现。";
            // 
            // _list
            // 
            this._list.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this._colLevel,
            this._colSys,
            this._colRule,
            this._colText});
            this._list.Dock = System.Windows.Forms.DockStyle.Fill;
            this._list.FullRowSelect = true;
            this._list.GridLines = true;
            this._list.HideSelection = false;
            this._list.Location = new System.Drawing.Point(0, 64);
            this._list.MultiSelect = false;
            this._list.Name = "_list";
            this._list.ShowItemToolTips = true;
            this._list.Size = new System.Drawing.Size(900, 456);
            this._list.TabIndex = 1;
            this._list.UseCompatibleStateImageBehavior = false;
            this._list.View = System.Windows.Forms.View.Details;
            // 
            // _colLevel
            // 
            this._colLevel.Text = "级别";
            this._colLevel.Width = 64;
            // 
            // _colSys
            // 
            this._colSys.Text = "系统";
            this._colSys.Width = 92;
            // 
            // _colRule
            // 
            this._colRule.Text = "规则";
            this._colRule.Width = 210;
            // 
            // _colText
            // 
            this._colText.Text = "结论";
            this._colText.Width = 512;
            // 
            // _bar
            // 
            this._bar.ColumnCount = 2;
            this._bar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._bar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 104F));
            this._bar.Controls.Add(this._lblCount, 0, 0);
            this._bar.Controls.Add(this._btnClose, 1, 0);
            this._bar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._bar.Location = new System.Drawing.Point(0, 520);
            this._bar.Name = "_bar";
            this._bar.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this._bar.RowCount = 1;
            this._bar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._bar.Size = new System.Drawing.Size(900, 40);
            this._bar.TabIndex = 2;
            // 
            // _lblCount
            // 
            this._lblCount.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this._lblCount.AutoSize = true;
            this._lblCount.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this._lblCount.Location = new System.Drawing.Point(11, 11);
            this._lblCount.Name = "_lblCount";
            this._lblCount.Size = new System.Drawing.Size(200, 16);
            this._lblCount.TabIndex = 0;
            this._lblCount.Text = "";
            // 
            // _btnClose
            // 
            this._btnClose.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnClose.Location = new System.Drawing.Point(791, 7);
            this._btnClose.Name = "_btnClose";
            this._btnClose.Size = new System.Drawing.Size(98, 26);
            this._btnClose.TabIndex = 1;
            this._btnClose.Text = "关闭";
            this._btnClose.UseVisualStyleBackColor = true;
            this._btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // RuleAuditForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 560);
            this.Controls.Add(this._list);
            this.Controls.Add(this._lblTip);
            this.Controls.Add(this._bar);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RuleAuditForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "规则体检";
            this._bar.ResumeLayout(false);
            this._bar.PerformLayout();
            this.ResumeLayout(false);

        }
    }
}
