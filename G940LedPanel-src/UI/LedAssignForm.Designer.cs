namespace G940LedPanel.UI
{
    partial class LedAssignForm
    {
        /// <summary>必需的设计器变量。</summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>清理正在使用的所有资源。</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        private void InitializeComponent()
        {
            this._lblTitle = new System.Windows.Forms.Label();
            this._list = new System.Windows.Forms.ListBox();
            this._lblHint = new System.Windows.Forms.Label();
            this._btnOk = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // _lblTitle
            // 
            this._lblTitle.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F, System.Drawing.FontStyle.Bold);
            this._lblTitle.Location = new System.Drawing.Point(16, 14);
            this._lblTitle.Name = "_lblTitle";
            this._lblTitle.Size = new System.Drawing.Size(388, 26);
            this._lblTitle.TabIndex = 0;
            this._lblTitle.Text = "P1 · 当前：电源";
            this._lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _list
            // 
            this._list.FormattingEnabled = true;
            this._list.ItemHeight = 17;
            this._list.Location = new System.Drawing.Point(16, 48);
            this._list.Name = "_list";
            this._list.Size = new System.Drawing.Size(388, 216);
            this._list.TabIndex = 1;
            this._list.DoubleClick += new System.EventHandler(this.list_DoubleClick);
            // 
            // _lblHint
            // 
            this._lblHint.Location = new System.Drawing.Point(16, 272);
            this._lblHint.Name = "_lblHint";
            this._lblHint.Size = new System.Drawing.Size(388, 44);
            this._lblHint.TabIndex = 2;
            this._lblHint.Text = "标「现挂 Pn」的系统会从那颗灯换过来；\r\n选「（空）」让这颗灯空出来。";
            // 
            // _btnOk
            // 
            this._btnOk.Location = new System.Drawing.Point(222, 322);
            this._btnOk.Name = "_btnOk";
            this._btnOk.Size = new System.Drawing.Size(88, 30);
            this._btnOk.TabIndex = 3;
            this._btnOk.Text = "确定";
            this._btnOk.UseVisualStyleBackColor = true;
            this._btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // _btnCancel
            // 
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.Location = new System.Drawing.Point(316, 322);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Size = new System.Drawing.Size(88, 30);
            this._btnCancel.TabIndex = 4;
            this._btnCancel.Text = "取消";
            this._btnCancel.UseVisualStyleBackColor = true;
            this._btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // LedAssignForm
            // 
            this.AcceptButton = this._btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(420, 366);
            this.Controls.Add(this._lblTitle);
            this.Controls.Add(this._list);
            this.Controls.Add(this._lblHint);
            this.Controls.Add(this._btnOk);
            this.Controls.Add(this._btnCancel);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LedAssignForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "灯位配置";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label _lblTitle;
        private System.Windows.Forms.ListBox _list;
        private System.Windows.Forms.Label _lblHint;
        private System.Windows.Forms.Button _btnOk;
        private System.Windows.Forms.Button _btnCancel;
    }
}
