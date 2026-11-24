using System;
using System.Collections.Generic;
using System.Text;

namespace G940LedPanel.Core
{
    /// <summary>描述符里解析出的一条 Feature 报告。</summary>
    public class HidFeatureReport
    {
        /// <summary>报告 ID（描述符里没有 Report ID 项时为 0）。</summary>
        public int ReportId { get; set; }

        /// <summary>该报告所有 Feature 字段占用的总 bit 数。</summary>
        public int BitCount { get; set; }

        /// <summary>字段个数（各 Report Count 之和）。</summary>
        public int FieldCount { get; set; }

        /// <summary>纯数据字节数（不含报告 ID）。</summary>
        public int DataBytes { get { return (BitCount + 7) / 8; } }

        /// <summary>实际收发时要给的字节数。有报告 ID 时首字节是 ID，没有则数据从首字节开始。</summary>
        public int TotalBytes { get { return ReportId == 0 ? DataBytes : DataBytes + 1; } }
    }

    /// <summary>HID 报告描述符的解析结果（只关心 Usage Page 与 Feature 报告）。</summary>
    public class HidDescriptorInfo
    {
        public int UsagePage { get; set; }
        public List<HidFeatureReport> Features { get; private set; }
        public string Error { get; set; }

        public HidDescriptorInfo()
        {
            UsagePage = -1;
            Features = new List<HidFeatureReport>();
            Error = "";
        }

        public HidFeatureReport FindFeature(int reportId)
        {
            for (int i = 0; i < Features.Count; i++)
                if (Features[i].ReportId == reportId) return Features[i];
            return null;
        }

        /// <summary>一行摘要，供诊断窗口直接显示。</summary>
        public string Summary()
        {
            if (Error.Length > 0) return Error;
            if (Features.Count == 0) return "描述符里没有 Feature 报告";

            var sb = new StringBuilder();
            for (int i = 0; i < Features.Count; i++)
            {
                HidFeatureReport f = Features[i];
                if (i > 0) sb.Append("；");
                sb.Append("ID=").Append(f.ReportId)
                  .Append(' ').Append(f.FieldCount).Append(" 字段 / ")
                  .Append(f.BitCount).Append(" bit")
                  .Append("（数据 ").Append(f.DataBytes).Append(" 字节 + ID = ")
                  .Append(f.TotalBytes).Append(" 字节）");
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// HID 报告描述符解析（纯函数，无平台依赖，可离线断言）。
    ///
    /// 只解析本项目关心的部分：Usage Page、Report ID、Feature 报告的长度。
    /// 不追求完整实现（Push/Pop、Delimiter、局部用途栈一律忽略），因为 G940 的描述符很简单。
    ///
    /// 描述符格式（HID 1.11 §6.2.2）：
    ///   每项首字节 = bTag(4-7) | bType(2-3) | bSize(0-1)
    ///   bSize: 0→0 字节, 1→1, 2→2, 3→4
    ///   bType: 0=Main, 1=Global, 2=Local
    ///   Global 里 tag=0 是 Usage Page、tag=7 是 Report Size、tag=8 是 Report ID、tag=9 是 Report Count
    ///   Main 里 tag=0xB 是 Feature（另 0x8=Input、0x9=Output）
    /// </summary>
    public static class HidDescriptor
    {
        public static HidDescriptorInfo Parse(byte[] desc)
        {
            var info = new HidDescriptorInfo();
            if (desc == null || desc.Length == 0)
            {
                info.Error = "描述符为空";
                return info;
            }

            int usagePage = -1;
            int reportId = 0;
            int reportSize = 0;
            int reportCount = 0;
            var map = new Dictionary<int, HidFeatureReport>();
            var order = new List<int>();

            int i = 0;
            while (i < desc.Length)
            {
                byte b = desc[i];
                int sizeCode = b & 0x03;
                int size = sizeCode == 3 ? 4 : sizeCode;
                int type = (b >> 2) & 0x03;
                int tag = (b >> 4) & 0x0F;

                // 数据不够说明描述符被截断：保留已解析出的部分，标个错误
                if (i + 1 + size > desc.Length)
                {
                    info.Error = "描述符在第 " + i + " 字节处截断";
                    break;
                }

                int val = 0;
                for (int k = 0; k < size; k++)
                    val |= desc[i + 1 + k] << (8 * k);
                i += 1 + size;

                if (type == 1) // Global
                {
                    // Usage Page 记第一个：那是应用集合的主用法页（如 0x01 通用桌面），
                    // 后面切到 0xFF00 厂商页只是数据字段的用途，不能代表设备类别。
                    if (tag == 0) { if (usagePage < 0) usagePage = val; }
                    else if (tag == 7) reportSize = val;
                    else if (tag == 8) reportId = val;
                    else if (tag == 9) reportCount = val;
                }
                else if (type == 0 && tag == 0x0B) // Main: Feature
                {
                    HidFeatureReport f;
                    if (!map.TryGetValue(reportId, out f))
                    {
                        f = new HidFeatureReport();
                        f.ReportId = reportId;
                        map[reportId] = f;
                        order.Add(reportId);
                    }
                    f.BitCount += reportSize * reportCount;
                    f.FieldCount += reportCount;
                }
            }

            for (int k = 0; k < order.Count; k++)
                info.Features.Add(map[order[k]]);

            info.UsagePage = usagePage;
            return info;
        }

        /// <summary>把字节数组排成 "03 FF 00" 这样的十六进制，供诊断窗口显示。</summary>
        public static string Hex(byte[] data)
        {
            if (data == null || data.Length == 0) return "(空)";
            var sb = new StringBuilder(data.Length * 3);
            for (int i = 0; i < data.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(data[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
}
