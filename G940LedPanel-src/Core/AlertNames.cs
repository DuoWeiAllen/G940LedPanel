using System;
using System.Collections.Generic;

namespace G940LedPanel.Core
{
    /// <summary>
    /// 预置配置的显示名。
    ///
    /// 为什么单独一个类：配置的身份是**文件名**（twin_piston），显示名要能随语言变，
    /// 而文件名不能变。这张表就是两者之间的唯一桥梁。
    ///
    /// 现状（1.3.48）：返回中文常量，给中文界面用。
    /// 第 1 期接 resx 后，这里改成资源引用，**调用点一行都不用动**。
    ///
    /// 注意：用户自建的配置不在这张表里——它们的显示名就是文件里写的名字，不翻译。
    /// </summary>
    public static class AlertNames
    {
        private sealed class Entry
        {
            public string Stem;
            public string Display;
            public int Order;
        }

        // 顺序 = 界面下拉里的固定次序。切语言后不能变，否则用户找不到上次用的那套。
        // 名字只留"这套配置在演示什么"：1.3.49 去掉了「演示①②③」编号与
        // 「（单灯）/（整帧）」这类范围说明——前缀现在统一由下拉的「（内置）」承担，
        // 范围是效果层的事，写在配置名里既长又容易被当成功能限制。
        private static readonly Entry[] All =
        {
            new Entry { Stem = "twin_piston",   Display = "通用双发活塞", Order = 0 },
            new Entry { Stem = "single_piston", Display = "单发活塞",     Order = 1 },
            new Entry { Stem = "jet_regional",  Display = "喷气支线",     Order = 2 },
            new Entry { Stem = "demo_rhythm",   Display = "节奏",         Order = 0 },
            new Entry { Stem = "demo_ring",     Display = "环行动画",     Order = 1 },
            new Entry { Stem = "demo_decor",    Display = "装饰",         Order = 2 }
        };

        private static readonly Dictionary<string, Entry> ByStem = Build();

        private static Dictionary<string, Entry> Build()
        {
            var d = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in All)
                if (!d.ContainsKey(e.Stem)) d.Add(e.Stem, e);
            return d;
        }

        /// <summary>文件名（不含扩展名）→ 显示名。不是预置则返回 null。</summary>
        public static string Of(string stem)
        {
            Entry e;
            return !string.IsNullOrEmpty(stem) && ByStem.TryGetValue(stem, out e) ? e.Display : null;
        }

        /// <summary>下拉里的固定次序。不认识的（用户配置）排在最后。</summary>
        public static int Index(string stem)
        {
            Entry e;
            return !string.IsNullOrEmpty(stem) && ByStem.TryGetValue(stem, out e) ? e.Order : int.MaxValue;
        }

    }
}
