namespace G940LedPanel.UI
{
    /// <summary>
    /// 设计器文件 —— 试算面板：逐条列出条件里每个叶子的左右值、比较符与三态结果。
    /// 只是显示，不含逻辑；填充数据见 CondProbePanel.cs 的 Show()。
    /// </summary>
    partial class CondProbePanel
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.ListView _lst;
        private System.Windows.Forms.ColumnHeader _colCond;
        private System.Windows.Forms.ColumnHeader _colLeft;
        private System.Windows.Forms.ColumnHeader _colCmp;
        private System.Windows.Forms.ColumnHeader _colRight;
        private System.Windows.Forms.ColumnHeader _colResult;
        private System.Windows.Forms.Label _lblEmpty;

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
            this._lst = new System.Windows.Forms.ListView();
            this._colCond = new System.Windows.Forms.ColumnHeader();
            this._colLeft = new System.Windows.Forms.ColumnHeader();
            this._colCmp = new System.Windows.Forms.ColumnHeader();
            this._colRight = new System.Windows.Forms.ColumnHeader();
            this._colResult = new System.Windows.Forms.ColumnHeader();
            this._lblEmpty = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // _lst
            // 
            this._lst.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this._colCond,
            this._colLeft,
            this._colCmp,
            this._colRight,
            this._colResult});
            this._lst.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lst.FullRowSelect = true;
            this._lst.GridLines = false;
            this._lst.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this._lst.HideSelection = false;
            this._lst.Location = new System.Drawing.Point(0, 22);
            this._lst.MultiSelect = false;
            this._lst.Name = "_lst";
            this._lst.Size = new System.Drawing.Size(430, 176);
            this._lst.TabIndex = 1;
            this._lst.UseCompatibleStateImageBehavior = false;
            this._lst.View = System.Windows.Forms.View.Details;
            // 
            // _colCond
            // 
            this._colCond.Text = "条件";
            this._colCond.Width = 190;
            // 
            // _colLeft
            // 
            this._colLeft.Text = "当前值";
            this._colLeft.Width = 70;
            // 
            // _colCmp
            // 
            this._colCmp.Text = "比较";
            this._colCmp.Width = 56;
            // 
            // _colRight
            // 
            this._colRight.Text = "门槛";
            this._colRight.Width = 62;
            // 
            // _colResult
            // 
            this._colResult.Text = "结果";
            this._colResult.Width = 60;
            // 
            // _lblEmpty
            // 
            this._lblEmpty.Dock = System.Windows.Forms.DockStyle.Top;
            this._lblEmpty.Location = new System.Drawing.Point(0, 0);
            this._lblEmpty.Name = "_lblEmpty";
            this._lblEmpty.AutoEllipsis = true;
            this._lblEmpty.Size = new System.Drawing.Size(430, 44);
            this._lblEmpty.TabIndex = 0;
            this._lblEmpty.Text = "试算：未接入数据时全部显示“无数据”。";
            this._lblEmpty.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // CondProbePanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this._lst);
            this.Controls.Add(this._lblEmpty);
            this.Name = "CondProbePanel";
            this.Size = new System.Drawing.Size(430, 198);
            this.ResumeLayout(false);

        }
    }
}
