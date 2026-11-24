namespace G940LedPanel.UI
{
    partial class NewRuleForm
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
            this._lblWarn = new System.Windows.Forms.Label();
            this._lblSys = new System.Windows.Forms.Label();
            this._cboSys = new System.Windows.Forms.ComboBox();
            this._lblName = new System.Windows.Forms.Label();
            this._txtName = new System.Windows.Forms.TextBox();
            this._lblPos = new System.Windows.Forms.Label();
            this._cboPos = new System.Windows.Forms.ComboBox();
            this._lblColor = new System.Windows.Forms.Label();
            this._cboColor = new System.Windows.Forms.ComboBox();
            this._lblEffect = new System.Windows.Forms.Label();
            this._cboEffect = new System.Windows.Forms.ComboBox();
            this._lblSpeed = new System.Windows.Forms.Label();
            this._cboSpeed = new System.Windows.Forms.ComboBox();
            this._lblCondTitle = new System.Windows.Forms.Label();
            this._lblCond = new System.Windows.Forms.Label();
            this._btnCond = new System.Windows.Forms.Button();
            this._btnOk = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // _lblWarn
            // 
            this._lblWarn.AutoEllipsis = true;
            this._lblWarn.Location = new System.Drawing.Point(16, 8);
            this._lblWarn.Name = "_lblWarn";
            this._lblWarn.Size = new System.Drawing.Size(492, 22);
            this._lblWarn.TabIndex = 0;
            this._lblWarn.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblSys
            // 
            this._lblSys.Location = new System.Drawing.Point(16, 40);
            this._lblSys.Name = "_lblSys";
            this._lblSys.Size = new System.Drawing.Size(90, 22);
            this._lblSys.TabIndex = 1;
            this._lblSys.Text = "所属系统";
            this._lblSys.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cboSys
            // 
            this._cboSys.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cboSys.Location = new System.Drawing.Point(112, 36);
            this._cboSys.Name = "_cboSys";
            this._cboSys.Size = new System.Drawing.Size(396, 25);
            this._cboSys.TabIndex = 2;
            this._cboSys.SelectedIndexChanged += new System.EventHandler(this.cboSys_SelectedIndexChanged);
            // 
            // _lblName
            // 
            this._lblName.Location = new System.Drawing.Point(16, 80);
            this._lblName.Name = "_lblName";
            this._lblName.Size = new System.Drawing.Size(90, 22);
            this._lblName.TabIndex = 3;
            this._lblName.Text = "规则名称";
            this._lblName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _txtName
            // 
            this._txtName.Location = new System.Drawing.Point(112, 76);
            this._txtName.Name = "_txtName";
            this._txtName.Size = new System.Drawing.Size(396, 25);
            this._txtName.TabIndex = 4;
            // 
            // _lblPos
            // 
            this._lblPos.Location = new System.Drawing.Point(16, 120);
            this._lblPos.Name = "_lblPos";
            this._lblPos.Size = new System.Drawing.Size(90, 22);
            this._lblPos.TabIndex = 5;
            this._lblPos.Text = "插入位置";
            this._lblPos.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cboPos
            // 
            this._cboPos.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cboPos.Location = new System.Drawing.Point(112, 116);
            this._cboPos.Name = "_cboPos";
            this._cboPos.Size = new System.Drawing.Size(396, 25);
            this._cboPos.TabIndex = 6;
            // 
            // _lblColor
            // 
            this._lblColor.Location = new System.Drawing.Point(16, 160);
            this._lblColor.Name = "_lblColor";
            this._lblColor.Size = new System.Drawing.Size(90, 22);
            this._lblColor.TabIndex = 7;
            this._lblColor.Text = "灯色";
            this._lblColor.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cboColor
            // 
            this._cboColor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cboColor.Location = new System.Drawing.Point(112, 156);
            this._cboColor.Name = "_cboColor";
            this._cboColor.Size = new System.Drawing.Size(150, 25);
            this._cboColor.TabIndex = 8;
            // 
            // _lblEffect
            // 
            this._lblEffect.Location = new System.Drawing.Point(16, 200);
            this._lblEffect.Name = "_lblEffect";
            this._lblEffect.Size = new System.Drawing.Size(90, 22);
            this._lblEffect.TabIndex = 9;
            this._lblEffect.Text = "效果";
            this._lblEffect.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cboEffect
            // 
            this._cboEffect.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cboEffect.Location = new System.Drawing.Point(112, 196);
            this._cboEffect.Name = "_cboEffect";
            this._cboEffect.Size = new System.Drawing.Size(150, 25);
            this._cboEffect.TabIndex = 10;
            // 
            // _lblSpeed
            // 
            this._lblSpeed.Location = new System.Drawing.Point(16, 240);
            this._lblSpeed.Name = "_lblSpeed";
            this._lblSpeed.Size = new System.Drawing.Size(90, 22);
            this._lblSpeed.TabIndex = 11;
            this._lblSpeed.Text = "速度";
            this._lblSpeed.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cboSpeed
            // 
            this._cboSpeed.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cboSpeed.Location = new System.Drawing.Point(112, 236);
            this._cboSpeed.Name = "_cboSpeed";
            this._cboSpeed.Size = new System.Drawing.Size(150, 25);
            this._cboSpeed.TabIndex = 12;
            // 
            // _lblCondTitle
            // 
            this._lblCondTitle.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this._lblCondTitle.Location = new System.Drawing.Point(16, 282);
            this._lblCondTitle.Name = "_lblCondTitle";
            this._lblCondTitle.Size = new System.Drawing.Size(492, 20);
            this._lblCondTitle.TabIndex = 13;
            this._lblCondTitle.Text = "触发条件（必填）";
            this._lblCondTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblCond
            // 
            this._lblCond.AutoEllipsis = true;
            this._lblCond.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lblCond.Location = new System.Drawing.Point(16, 306);
            this._lblCond.Name = "_lblCond";
            this._lblCond.Padding = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this._lblCond.Size = new System.Drawing.Size(360, 70);
            this._lblCond.TabIndex = 14;
            this._lblCond.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _btnCond
            // 
            this._btnCond.Location = new System.Drawing.Point(388, 306);
            this._btnCond.Name = "_btnCond";
            this._btnCond.Size = new System.Drawing.Size(120, 32);
            this._btnCond.TabIndex = 15;
            this._btnCond.Text = "编辑条件…";
            this._btnCond.UseVisualStyleBackColor = true;
            this._btnCond.Click += new System.EventHandler(this.btnCond_Click);
            // 
            // _btnOk
            // 
            this._btnOk.Location = new System.Drawing.Point(330, 396);
            this._btnOk.Name = "_btnOk";
            this._btnOk.Size = new System.Drawing.Size(80, 30);
            this._btnOk.TabIndex = 16;
            this._btnOk.Text = "确定";
            this._btnOk.UseVisualStyleBackColor = true;
            this._btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // _btnCancel
            // 
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.Location = new System.Drawing.Point(428, 396);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Size = new System.Drawing.Size(80, 30);
            this._btnCancel.TabIndex = 17;
            this._btnCancel.Text = "取消";
            this._btnCancel.UseVisualStyleBackColor = true;
            this._btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // NewRuleForm
            // 
            this.AcceptButton = this._btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(524, 440);
            this.Controls.Add(this._lblWarn);
            this.Controls.Add(this._lblSys);
            this.Controls.Add(this._cboSys);
            this.Controls.Add(this._lblName);
            this.Controls.Add(this._txtName);
            this.Controls.Add(this._lblPos);
            this.Controls.Add(this._cboPos);
            this.Controls.Add(this._lblColor);
            this.Controls.Add(this._cboColor);
            this.Controls.Add(this._lblEffect);
            this.Controls.Add(this._cboEffect);
            this.Controls.Add(this._lblSpeed);
            this.Controls.Add(this._cboSpeed);
            this.Controls.Add(this._lblCondTitle);
            this.Controls.Add(this._lblCond);
            this.Controls.Add(this._btnCond);
            this.Controls.Add(this._btnOk);
            this.Controls.Add(this._btnCancel);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "NewRuleForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "新增告警规则";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label _lblWarn;
        private System.Windows.Forms.Label _lblSys;
        private System.Windows.Forms.ComboBox _cboSys;
        private System.Windows.Forms.Label _lblName;
        private System.Windows.Forms.TextBox _txtName;
        private System.Windows.Forms.Label _lblPos;
        private System.Windows.Forms.ComboBox _cboPos;
        private System.Windows.Forms.Label _lblColor;
        private System.Windows.Forms.ComboBox _cboColor;
        private System.Windows.Forms.Label _lblEffect;
        private System.Windows.Forms.ComboBox _cboEffect;
        private System.Windows.Forms.Label _lblSpeed;
        private System.Windows.Forms.ComboBox _cboSpeed;
        private System.Windows.Forms.Label _lblCondTitle;
        private System.Windows.Forms.Label _lblCond;
        private System.Windows.Forms.Button _btnCond;
        private System.Windows.Forms.Button _btnOk;
        private System.Windows.Forms.Button _btnCancel;
    }
}
