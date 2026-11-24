using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using G940LedPanel.Core;

namespace G940LedPanel.UI
{
    /// <summary>
    /// 「从列表里选一个设备」的对话框。
    ///
    /// 只在一种情况下出现：用户点了「连接」，系统枚举到了控制器，但没有一个
    /// 名字精确等于配置里的设备名（默认 "Logitech G940 Throttle"）。
    ///
    /// 为什么不自动挑一个：一套 G940 在 DirectInput 里有三个设备
    /// （Joystick / Throttle / Pedals），名字都带 "Logitech G940"，
    /// 只有油门那个的按钮才是这块面板的。选错的表现是"按钮读的是别的设备"，
    /// 用户会以为设备坏了。宁可问一次，也不替他猜。
    ///
    /// 注意灯不受这个选择影响：灯走独立 HID 通道，由 G940Hid 自己按
    /// VID 046D / PID C287 找接口并试写，与这里选哪个设备无关。
    /// 这里选的只是**按钮来源**。
    ///
    /// 为什么启动时不弹：把程序目录拷到一台没接设备的电脑上看配置，
    /// 一开机就被弹框拦住不合理。启动只静默失败并写状态词，用户想用时
    /// 自然会点「连接」——那时才是该问的时候。
    ///
    /// 界面在 PickDeviceForm.Designer.cs，本文件只放逻辑。
    /// </summary>
    public partial class PickDeviceForm : Form
    {
        /// <summary>用户选中的设备；取消或没选时为 null。</summary>
        public DeviceInfo Result { get; private set; }

        /// <summary>供 VS 设计器实例化用（设计器只解析 InitializeComponent）。</summary>
        public PickDeviceForm()
            : this(null)
        {
        }

        public PickDeviceForm(List<DeviceInfo> devices)
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            ApplyLang();

            // 含 "Throttle" 的排前面：用户在一堆 G940 里挑带灯的那个时，
            // 第一眼看到的就是该选的，比任何提示文字都管用。
            foreach (var d in DevicePicker.OrderForPick(devices))
                _lstDevices.Items.Add(d);

            if (_lstDevices.Items.Count > 0) _lstDevices.SelectedIndex = 0;
            else _btnOk.Enabled = false;
        }

        /// <summary>Designer 里那份中文是"设计时预览值"，运行时以语言包为准。</summary>
        private void ApplyLang()
        {
            Text = L.T("pick.form");
            _lblPrompt.Text = L.T("pick.prompt");
            _lblHint.Text = L.T("pick.hint");
            _btnOk.Text = L.T("common.ok");
            _btnCancel.Text = L.T("common.cancel");
        }

        /// <summary>
        /// 弹出选择框并返回选中的设备，取消返回 null。
        ///
        /// owner 不可见时（主窗体收在托盘）不传 owner：以隐藏窗体为 owner
        /// 弹窗可能出现在屏幕边缘，无 owner 才会居中于当前屏幕。
        /// </summary>
        public static DeviceInfo Ask(Form owner, List<DeviceInfo> devices)
        {
            using (var f = new PickDeviceForm(devices))
            {
                var r = (owner != null && owner.Visible) ? f.ShowDialog(owner) : f.ShowDialog();
                return r == DialogResult.OK ? f.Result : null;
            }
        }

        private void lstDevices_DoubleClick(object sender, EventArgs e)
        {
            Accept();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            Accept();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void Accept()
        {
            var pick = _lstDevices.SelectedItem as DeviceInfo;
            if (pick == null) return;   // 一项都没选：不关窗，让用户先选
            Result = pick;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
