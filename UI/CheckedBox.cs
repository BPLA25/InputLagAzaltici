using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace InputLagAzaltici
{
    class CheckedBox : CheckBox
    {
        bool hover;

        public CheckedBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Font = UiKit.FontBase;
            Cursor = Cursors.Hand;
            Height = 24;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnCheckedChanged(EventArgs e) { Invalidate(); base.OnCheckedChanged(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Arka planı parent'ın rengine boya (siyah şerit sorununu çözer)
            if (Parent != null)
            {
                using (var bg = new SolidBrush(Parent.BackColor))
                    e.Graphics.FillRectangle(bg, ClientRectangle);
            }
            else
            {
                e.Graphics.Clear(UiKit.Bg);
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            
            // Kutucuğu çiz
            var box = new Rectangle(2, (Height - 16) / 2, 16, 16);
            UiKit.DrawCheck(e.Graphics, box, Checked, Enabled);

            // Yazı rengi ve kalınlığı
            Color c = !Enabled ? UiKit.Muted : (Checked ? UiKit.Text : (hover ? UiKit.Text : UiKit.Muted));
            Font f = Checked && Enabled ? UiKit.FontBold : UiKit.FontBase;
            
            // Yazıyı çiz
            TextRenderer.DrawText(e.Graphics, Text, f, new Rectangle(24, 0, Width - 24, Height), c,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }
    }
}