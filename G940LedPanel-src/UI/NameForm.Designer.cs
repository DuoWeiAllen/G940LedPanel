namespace G940LedPanel.UI
{
    partial class NameForm
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
            this._lblPrompt = new System.Windows.Forms.Label();
            this._txt = new System.Windows.Forms.TextBox();
            this._lblHint = new System.Windows.Forms.Label();
            this._btnOk = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // _lblPrompt
            // 
            this._lblPrompt.Location = new System.Drawing.Point(16, 14);
            this._lblPrompt.Name = "_lblPrompt";
            this._lblPrompt.Size = new System.Drawing.Size(400, 20);
            this._lblPrompt.TabIndex = 0;
            this._lblPrompt.Text = "配置名称";
            this._lblPrompt.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _txt
            // 
            this._txt.Location = new System.Drawing.Point(16, 38);
            this._txt.Name = "_txt";
            this._txt.Size = new System.Drawing.Size(400, 25);
            this._txt.TabIndex = 1;
            this._txt.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txt_KeyDown);
            // 
            // _lblHint
            // 
            this._lblHint.AutoEllipsis = true;
            this._lblHint.Location = new System.Drawing.Point(16, 68);
            this._lblHint.Name = "_lblHint";
            this._lblHint.Size = new System.Drawing.Size(400, 40);
            this._lblHint.TabIndex = 2;
            this._lblHint.Text = "名字会用作文件名，建议用能一眼看懂用途的词。";
            // 
            // _btnOk
            // 
            this._btnOk.Location = new System.Drawing.Point(246, 112);
            this._btnOk.Name = "_btnOk";
            this._btnOk.Size = new System.Drawing.Size(80, 30);
            this._btnOk.TabIndex = 3;
            this._btnOk.Text = "确定";
            this._btnOk.UseVisualStyleBackColor = true;
            this._btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // _btnCancel
            // 
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.Location = new System.Drawing.Point(336, 112);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Size = new System.Drawing.Size(80, 30);
            this._btnCancel.TabIndex = 4;
            this._btnCancel.Text = "取消";
            this._btnCancel.UseVisualStyleBackColor = true;
            this._btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // NameForm
            // 
            this.AcceptButton = this._btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(432, 158);
            this.Controls.Add(this._lblPrompt);
            this.Controls.Add(this._txt);
            this.Controls.Add(this._lblHint);
            this.Controls.Add(this._btnOk);
            this.Controls.Add(this._btnCancel);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "NameForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "名称";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label _lblPrompt;
        private System.Windows.Forms.TextBox _txt;
        private System.Windows.Forms.Label _lblHint;
        private System.Windows.Forms.Button _btnOk;
        private System.Windows.Forms.Button _btnCancel;
    }
}
