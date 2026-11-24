using System;
using System.Collections.Generic;

namespace G940LedPanel.Core
{
    /// <summary>
    /// 从 DirectInput 枚举结果里挑出「G940 油门」。
    ///
    /// 为什么单独抽出来：一套 G940 在 DirectInput 里是**三个设备**
    /// （Joystick / Throttle / Pedals），三者名字都含 "G940"。所以
    /// 「含 G940」会命中三个、「含 Throttle」会命中外家油门（Warthog、
    /// T.16000M 也叫 Throttle），只有**两个条件同时表达**才唯一：
    /// 精确等于 "Logitech G940 Throttle"。
    ///
    /// 为什么不用 GUID 记设备：GUID 是「机器 + USB 口」的指纹，换台电脑
    /// 必然失效，而且失效是静默的（配置里躺着一串没用的字符）。名字是
    /// 型号标识，同型号同驱动在全世界都可复现，拷配置过去仍然有效。
    ///
    /// 为什么两个名字字段都比：DIDEVICEINSTANCEW 里 tszInstanceName 在
    /// 偏移 40、tszProductName 在 560，本项目历史上把这两个标反过。
    /// 与其赌「名字装在哪个字段」，不如两个都比——外家设备不会精确等于
    /// 这个字符串，比两个也不会误中。
    /// </summary>
    public static class DevicePicker
    {
        /// <summary>
        /// 出厂默认的设备名。程序只拿这个字符串和 DirectInput 报出的名字精确比对，
        /// 不加载任何厂商软件。对不上就认不出，用户可从列表里手选一次，结果记进配置。
        /// </summary>
        public const string DefaultName = "Logitech G940 Throttle";

        /// <summary>弹框里用来把「像油门」的设备排到前面的关键字（只影响排序，不参与选中判定）。</summary>
        public const string ThrottleHint = "Throttle";

        /// <summary>
        /// 设备的「权威名字」：有产品名就用产品名，空则退回实例名。
        ///
        /// 存进配置、弹框里显示、日志里打印，全部走这一个函数。
        /// 如果"存的时候用一个字段、比的时候用另一个"，就会出现存了却比不中
        /// 的怪事——把「哪个才是真名」这件事关在一个地方，才不会有第二处判断。
        /// </summary>
        public static string NameOf(DeviceInfo d)
        {
            if (d == null) return "";
            if (!string.IsNullOrEmpty(d.ProductName)) return d.ProductName.Trim();
            if (!string.IsNullOrEmpty(d.InstanceName)) return d.InstanceName.Trim();
            return "";
        }

        /// <summary>
        /// 找名字精确等于 want 的设备，找不到返回 null。
        ///
        /// 比较前 Trim + 忽略大小写：DirectInput 给的字符串尾部可能带空格，
        /// 大小写也不保证统一。除了这两点，不做任何模糊匹配——
        /// 名字对不上就应该响亮地失败（调用方会弹框让用户选），
        /// 而不是"含 Throttle 就算"，那会把外家油门悄悄认成 G940。
        /// </summary>
        public static DeviceInfo Find(List<DeviceInfo> list, string want)
        {
            if (list == null || string.IsNullOrEmpty(want)) return null;
            string w = want.Trim();
            foreach (var d in list)
            {
                if (d == null) continue;
                if (Eq(d.ProductName, w) || Eq(d.InstanceName, w)) return d;
            }
            return null;
        }

        /// <summary>
        /// 弹框列表的排序：名字里含 "Throttle" 的排前面，其余保持枚举原序。
        ///
        /// 只影响"先看到谁"，不参与选中判定。用户在一堆 Logitech G940
        /// 里挑带灯的那个时，第一眼看到的就是该选的，比任何提示文字都管用。
        /// </summary>
        public static List<DeviceInfo> OrderForPick(List<DeviceInfo> list)
        {
            var hits = new List<DeviceInfo>();
            var rest = new List<DeviceInfo>();
            if (list != null)
            {
                foreach (var d in list)
                {
                    if (d == null) continue;
                    string n = NameOf(d);
                    if (n.IndexOf(ThrottleHint, StringComparison.OrdinalIgnoreCase) >= 0) hits.Add(d);
                    else rest.Add(d);
                }
            }
            hits.AddRange(rest);
            return hits;
        }

        private static bool Eq(string a, string b)
        {
            if (a == null) return false;
            return string.Equals(a.Trim(), b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
