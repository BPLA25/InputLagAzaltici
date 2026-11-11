using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace InputLagAzaltici
{
    // Yatay Sekmeler İçin Özel Buton (Sistem Sekmesinde Kullanılıyor)
    class TopTabButton : Control
    {
        public bool Selected;
        bool hover;

        public TopTabButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            Height = 34; Cursor = Cursors.Hand; Font = UiKit.FSemibold; BackColor = Color.Transparent;
            TabStop = false;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (Parent != null) using (var b = new SolidBrush(Parent.BackColor)) e.Graphics.FillRectangle(b, ClientRectangle);

            var bg = Selected ? Color.FromArgb(60, UiKit.Accent) : (hover ? UiKit.Surface2 : Color.Transparent);
            if (bg != Color.Transparent) UiKit.FillRound(e.Graphics, new Rectangle(0, 0, Width - 1, Height - 1), 6, bg);
            
            var c = Selected || hover ? UiKit.Text : UiKit.Muted;
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, c, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}