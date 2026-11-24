namespace G940LedPanel.UI
{
    partial class PickDeviceForm
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
            this._lstDevices = new System.Windows.Forms.ListBox();
            this._lblHint = new System.Windows.Forms.Label();
            this._btnOk = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // _lblPrompt
            // 
            this._lblPrompt.Location = new System.Drawing.Point(16, 14);
            this._lblPrompt.Name = "_lblPrompt";
            this._lblPrompt.Size = new System.Drawing.Size(448, 66);
            this._lblPrompt.TabIndex = 0;
            this._lblPrompt.Text = "程序没认出 G940 油门。下面是从系统里枚举到的控制器，\r\n请选名字里带 Throttle 的那一个：";
            // 
            // _lstDevices
            // 
            this._lstDevices.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this._lstDevices.IntegralHeight = false;
            this._lstDevices.ItemHeight = 20;
            this._lstDevices.Location = new System.Drawing.Point(16, 86);
            this._lstDevices.Name = "_lstDevices";
            this._lstDevices.Size = new System.Drawing.Size(448, 168);
            this._lstDevices.TabIndex = 1;
            this._lstDevices.DoubleClick += new System.EventHandler(this.lstDevices_DoubleClick);
            // 
            // _lblHint
            // 
            this._lblHint.AutoEllipsis = true;
            this._lblHint.Location = new System.Drawing.Point(16, 260);
            this._lblHint.Name = "_lblHint";
            this._lblHint.Size = new System.Drawing.Size(448, 22);
            this._lblHint.TabIndex = 2;
            this._lblHint.Text = "这个选择会记住，以后启动不再问。";
            this._lblHint.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _btnOk
            // 
            this._btnOk.Location = new System.Drawing.Point(296, 290);
            this._btnOk.Name = "_btnOk";
            this._btnOk.Size = new System.Drawing.Size(80, 30);
            this._btnOk.TabIndex = 3;
            this._btnOk.Text = "确定";
            this._btnOk.UseVisualStyleBackColor = true;
            this._btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // _btnCancel
            // 
            this._btnCancel.Location = new System.Drawing.Point(384, 290);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Size = new System.Drawing.Size(80, 30);
            this._btnCancel.TabIndex = 4;
            this._btnCancel.Text = "取消";
            this._btnCancel.UseVisualStyleBackColor = true;
            this._btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // PickDeviceForm
            // 
            this.AcceptButton = this._btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(480, 336);
            this.Controls.Add(this._lblPrompt);
            this.Controls.Add(this._lstDevices);
            this.Controls.Add(this._lblHint);
            this.Controls.Add(this._btnOk);
            this.Controls.Add(this._btnCancel);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PickDeviceForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "选择 G940 油门";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label _lblPrompt;
        private System.Windows.Forms.ListBox _lstDevices;
        private System.Windows.Forms.Label _lblHint;
        private System.Windows.Forms.Button _btnOk;
        private System.Windows.Forms.Button _btnCancel;
    }
}
