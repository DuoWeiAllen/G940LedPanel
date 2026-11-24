using System;

namespace G940LedPanel.Core
{
    /// <summary>
    /// 一次设备枚举的结果。
    ///
    /// 单独成文件（原先是放在 DeviceSession.cs 里）：它是纯数据，不碰 DirectInput，
    /// 只有把它和枚举实现分开，离线断言才能引用它而不必链接整个 P/Invoke 层。
    ///
    /// 两个名字的来历见 DeviceSession 的枚举回调：DIDEVICEINSTANCEW 里
    /// tszInstanceName 在偏移 40、tszProductName 在 560。
    /// </summary>
    public class DeviceInfo
    {
        public Guid InstanceGuid;

        /// <summary>产品名（tszProductName）。有些驱动会留空，此时用实例名。</summary>
        public string ProductName = "";

        /// <summary>实例名（tszInstanceName）。同型号插两台时可能带序号。</summary>
        public string InstanceName = "";

        public int ButtonCount;

        public override string ToString()
        {
            // 名字统一走 DevicePicker.NameOf：显示、存配置、匹配判定用的是同一个来源，
            // 否则会出现"列表里显示 A、存进去的是 B、下次匹配 C"这种自相矛盾。
            //
            // 两处文字走 L.T（1.3.66）：这里的结果直接进 PickDeviceForm 的列表
            // （`_lstDevices.Items.Add(d)`，ListBox 走 ToString），英文界面下
            // 原来会在每个设备名后面挂着「[按钮×8]」。白名单原先的理由是
            // "设备名拼接，值来自驱动不进语言包"——名字确实来自驱动，
            // 但按钮数那截是代码拼的中文，理由没对着代码核。
            string name = DevicePicker.NameOf(this);
            if (string.IsNullOrEmpty(name)) name = L.T("dev.devicelist.unnamed");
            return ButtonCount > 0 ? name + "  " + L.T("dev.btncount", ButtonCount) : name;
        }
    }
}
