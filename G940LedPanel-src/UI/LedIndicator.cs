using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace G940LedPanel.UI
{
    /// <summary>
    /// LED 面板预览控件：正方形灯，两排 4+4 与真实 G940 上的 8 颗灯一一对应。
    ///
    /// 两个刻意的设计：
    /// 1. **灯是正方形、灭灯是淡黑**——纯黑和深色面板糊在一起，看不出这里有个键；
    ///    淡黑保留轮廓，又一眼能和亮着的彩色区分开。
    /// 2. **文字在灯体上**，不在灯下面——按键模式写 P1..P8，告警模式写系统名（电源/发动机…），
    ///    和真机面板丝印的位置一致；灯下再挂一行字反而挤掉灯的高度。
    /// </summary>
    public class LedIndicator : Control
    {
        private byte _color;
        private string _caption = "";
        private bool _showLabel = true;

        public int LedIndex;

        /// <summary>灯体上的主标签：按键模式 P1..P8，告警模式系统名。</summary>
        public string Caption
        {
            get { return _caption; }
            set
            {
                if (_caption == value) return;
                _caption = value ?? "";
                Invalidate();
            }
        }

        /// <summary>是否显示灯体文字（顶栏「显示灯位文字」）。</summary>
        public bool ShowLabel
        {
            get { return _showLabel; }
            set
            {
                if (_showLabel == value) return;
                _showLabel = value;
                Invalidate();
            }
        }

        public bool Pressed;

        public byte LedColorValue
        {
            get { return _color; }
            set
            {
                // 值没变就不重画：引擎 40Hz 推帧，无条件 Invalidate 会让 8 个控件
                // 每秒重画 320 次，配合父容器擦背景就是肉眼可见的闪。
                if (_color == value) return;
                _color = value;
                Invalidate();
            }
        }

        public LedIndicator()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Opaque, true);
            Size = new Size(72, 72);
            Cursor = Cursors.Hand;
        }

        // ---- 字体缓存：每帧 new Font 会漏 GDI 句柄，几十秒就能把进程拖垮 ----
        private Font _f85, _f75, _f65;

        private Font FontFor(string text, int side)
        {
            if (_f85 == null) _f85 = new Font(Font.FontFamily, 8.5F, FontStyle.Bold);
            if (_f75 == null) _f75 = new Font(Font.FontFamily, 7.5F, FontStyle.Bold);
            if (_f65 == null) _f65 = new Font(Font.FontFamily, 6.5F, FontStyle.Bold);

            int avail = side - 8;
            if (avail < 12) return _f65;
            using (var g = CreateGraphics())
            {
                if (g.MeasureString(text, _f85).Width <= avail) return _f85;
                if (g.MeasureString(text, _f75).Width <= avail) return _f75;
            }
            return _f65;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 整块铺底（Opaque 必须自己填满客户区，否则父容器每次擦背景都会透出来）
            Color bg = BackColor;
            if (bg == Color.Transparent) bg = Parent != null ? Parent.BackColor : Color.FromArgb(13, 22, 38);
            using (var bgBrush = new SolidBrush(bg))
                g.FillRectangle(bgBrush, ClientRectangle);

            // 正方形灯体：取宽高较小值，居中，最大 64px（再大就不像按键了）
            int side = Math.Min(Width, Height) - 12;
            if (side > 64) side = 64;
            if (side < 10) return;
            var body = new Rectangle((Width - side) / 2, (Height - side) / 2, side, side);

            bool lit = _color != 0;

            if (!lit)
            {
                // 灭灯：淡黑（不是纯黑）。纯黑会和深色面板糊成一片，看不出这里有个键；
                // 淡黑仍能看出方形灯体轮廓，又明显区别于"亮着"的彩色。
                using (var b = new LinearGradientBrush(
                    new Rectangle(body.X, body.Y, body.Width, body.Height),
                    Color.FromArgb(82, 89, 104), Color.FromArgb(46, 52, 64), 90F))
                using (var p = new Pen(Color.FromArgb(126, 136, 154), 1.2f))
                {
                    var rr = Rounded(body, 4);
                    g.FillPath(b, rr);
                    g.DrawPath(p, rr);
                }
                using (var sh = new Pen(Color.FromArgb(30, 34, 42), 1f))
                    g.DrawLine(sh, body.X + 3, body.Bottom - 3, body.Right - 4, body.Bottom - 3);
            }
            else
            {
                Color c;
                switch (_color)
                {
                    case 1: c = LedPalette.Green; break;
                    case 2: c = LedPalette.Amber; break;
                    case 3: c = LedPalette.Red; break;
                    default: c = LedPalette.Dark; break;
                }

                // 外圈辉光（两层，只有点亮时才画）
                using (var glow1 = new SolidBrush(Color.FromArgb(38, c)))
                    g.FillPath(glow1, Rounded(Rectangle.Inflate(body, 7, 7), 9));
                using (var glow2 = new SolidBrush(Color.FromArgb(70, c)))
                    g.FillPath(glow2, Rounded(Rectangle.Inflate(body, 3, 3), 7));

                using (var b = new SolidBrush(c))
                using (var p = new Pen(Color.FromArgb(150, 255, 255, 255), 1.2f))
                {
                    var rr = Rounded(body, 4);
                    g.FillPath(b, rr);
                    g.DrawPath(p, rr);
                }
            }

            // 按下时外圈描一道浅蓝，表示"这颗键此刻是按住状态"
            if (Pressed)
            {
                using (var pp = new Pen(Color.FromArgb(90, 200, 235), 1.6f))
                    g.DrawPath(pp, Rounded(Rectangle.Inflate(body, 2, 2), 6));
            }

            // 灯体文字：灭灯深灰、亮灯白色（亮灯时先画一层暗影，闪烁时也读得清）
            if (_showLabel && _caption.Length > 0)
            {
                Font f = FontFor(_caption, side);
                var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                var rect = new RectangleF(body.X, body.Y, body.Width, body.Height);
                if (lit)
                {
                    using (var shadow = new SolidBrush(Color.FromArgb(110, 0, 0, 0)))
                        g.DrawString(_caption, f, shadow,
                            new RectangleF(body.X + 1, body.Y + 1, body.Width, body.Height), sf);
                    using (var white = new SolidBrush(Color.White))
                        g.DrawString(_caption, f, white, rect, sf);
                }
                else
                {
                    // 淡黑灯体上要用浅灰字，深灰字会糊掉
                    using (var ink = new SolidBrush(Color.FromArgb(168, 178, 196)))
                        g.DrawString(_caption, f, ink, rect, sf);
                }
                sf.Dispose();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_f85 != null) { _f85.Dispose(); _f85 = null; }
                if (_f75 != null) { _f75.Dispose(); _f75 = null; }
                if (_f65 != null) { _f65.Dispose(); _f65 = null; }
            }
            base.Dispose(disposing);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            if (_f85 != null) { _f85.Dispose(); _f85 = null; }
            if (_f75 != null) { _f75.Dispose(); _f75 = null; }
            if (_f65 != null) { _f65.Dispose(); _f65 = null; }
            Invalidate();
        }

        private static GraphicsPath Rounded(Rectangle r, int rad)
        {
            var p = new GraphicsPath();
            if (r.Width <= rad * 2 || r.Height <= rad * 2) { p.AddRectangle(r); return p; }
            int d = rad * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}
