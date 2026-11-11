using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace InputLagAzaltici
{
    class SidebarButton : Control
    {
        public string Icon = "";
        public bool Selected;
        bool hover;
        public event EventHandler Activate;

        public SidebarButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            Height = 42; Cursor = Cursors.Hand; Font = UiKit.FontSemibold; BackColor = Color.Transparent;
            TabStop = false;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnClick(EventArgs e) { if (Activate != null) Activate(this, e); base.OnClick(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (Parent != null) using (var b = new SolidBrush(Parent.BackColor)) e.Graphics.FillRectangle(b, ClientRectangle);
            
            var r = new Rectangle(8, 4, Width - 16, Height - 8);
            var bg = Selected ? Color.FromArgb(60, UiKit.Accent) : (hover ? UiKit.Surface2 : Color.Transparent);
            if (bg != Color.Transparent) UiKit.FillRound(e.Graphics, r, 6, bg);
            if (Selected) using (var b = new SolidBrush(UiKit.Accent)) e.Graphics.FillRectangle(b, 4, r.Y + 6, 3, r.Height - 12);
            
            TextRenderer.DrawText(e.Graphics, Icon, UiKit.FontMono10, new Rectangle(16, 4, 30, Height - 8), Selected ? UiKit.Text : UiKit.Muted, TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(52, 4, Width - 60, Height - 8), Selected || hover ? UiKit.Text : UiKit.Muted, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }
    }
}