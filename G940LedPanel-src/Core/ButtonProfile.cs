using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace G940LedPanel.Core
{
    /// <summary>一条“油门按钮 → LED 效果”绑定。</summary>
    public class ButtonBinding
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("button")]
        public int ButtonIndex { get; set; }

        [JsonPropertyName("buttonName")]
        public string ButtonName { get; set; } = "";

        /// <summary>hold=按下期间持续；toggle=按一下开/再按关。默认切换，和新增行的默认值一致。</summary>
        [JsonPropertyName("trigger")]
        public string Trigger { get; set; } = "toggle";

        [JsonPropertyName("effect")]
        public string EffectId { get; set; } = "solid";

        [JsonPropertyName("color")]
        public int Color { get; set; } = (int)LedColor.Green;

        [JsonPropertyName("speed")]
        public double Speed { get; set; } = 1.0;

        /// <summary>-1=全部灯，0..7=单个灯 P1..P8。</summary>
        [JsonPropertyName("targetLed")]
        public int TargetLed { get; set; } = -1;

        /// <summary>
        /// 运行时状态：toggle 型绑定现在是不是"开着"。
        /// 只活在内存里，不写配置文件——重启后所有 toggle 都该是关的。
        /// </summary>
        [JsonIgnore]
        public bool RuntimeOn { get; set; }

        public ButtonBinding Clone()
        {
            return new ButtonBinding
            {
                Enabled = Enabled,
                ButtonIndex = ButtonIndex,
                ButtonName = ButtonName,
                Trigger = Trigger,
                EffectId = EffectId,
                Color = Color,
                Speed = Speed,
                TargetLed = TargetLed
            };
        }
    }

    /// <summary>
    /// 一套按键配置 = 一个文件（buttons\名字.json）。
    ///
    /// 原来叫 <c>SimplePreset</c>（"简单模式预设"），那是早期叫法——界面早就不叫"简单模式"了，
    /// 现在叫"按键模式"，所以整个类型跟着改名，避免"文件夹叫 buttons、代码里叫 Simple"。
    /// </summary>
    public sealed class ButtonProfile
    {
        public const string KindValue = "g940.button";
        public const int FormatVersion = 1;

        [JsonPropertyName("kind")]
        public string Kind { get; set; } = KindValue;

        [JsonPropertyName("version")]
        public int Version { get; set; } = FormatVersion;

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        /// <summary>油门按钮 → 灯光 的绑定表。</summary>
        [JsonPropertyName("bindings")]
        public List<ButtonBinding> Bindings { get; set; } = new List<ButtonBinding>();

        /// <summary>底灯：按键模式下无触发时持续显示的效果。</summary>
        [JsonPropertyName("background")]
        public BackgroundStyle Background { get; set; } = new BackgroundStyle();

        // ---------------------------------------------------------------- 运行时字段

        [JsonIgnore]
        public string FilePath { get; set; } = "";

        [JsonIgnore]
        public bool IsBuiltIn { get; set; }

        public ButtonProfile Clone()
        {
            var c = new ButtonProfile
            {
                Kind = Kind,
                Version = Version,
                Name = Name,
                FilePath = FilePath,
                IsBuiltIn = IsBuiltIn,
                Bindings = new List<ButtonBinding>(),
                Background = Background != null ? Background.Clone() : new BackgroundStyle()
            };
            if (Bindings != null)
                foreach (var b in Bindings)
                    if (b != null) c.Bindings.Add(b.Clone());
            return c;
        }

        /// <summary>把另一套配置的内容搬进来（名字与身份不动）。</summary>
        public void CopyFrom(ButtonProfile other)
        {
            if (other == null) return;
            Bindings = new List<ButtonBinding>();
            if (other.Bindings != null)
                foreach (var b in other.Bindings)
                    if (b != null) Bindings.Add(b.Clone());
            Background = other.Background != null ? other.Background.Clone() : new BackgroundStyle();
        }
    }

    /// <summary>底灯（无触发时持续显示）。</summary>
    public sealed class BackgroundStyle
    {
        /// <summary>"off" = 关灯。</summary>
        [JsonPropertyName("effect")]
        public string EffectId { get; set; } = "off";

        [JsonPropertyName("color")]
        public int Color { get; set; } = (int)LedColor.Green;

        [JsonPropertyName("speed")]
        public double Speed { get; set; } = 1.0;

        public BackgroundStyle Clone()
        {
            return new BackgroundStyle { EffectId = EffectId, Color = Color, Speed = Speed };
        }
    }
}
