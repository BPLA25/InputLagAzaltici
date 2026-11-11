using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace InputLagAzaltici
{
    class FlatButton : Button
    {
        public Color BaseColor = UiKit.Surface2;
        public Color HoverColor = UiKit.Border;
        public Color TextColor = UiKit.Text;
        public int Radius = 6;
        bool hover, down;

        public FlatButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            FlatStyle = FlatStyle.Flat; 
            FlatAppearance.BorderSize = 0;
            FlatAppearance.MouseDownBackColor = Color.Transparent;
            FlatAppearance.MouseOverBackColor = Color.Transparent;
            Cursor = Cursors.Hand; 
            BackColor = Color.Transparent;
            Font = UiKit.FontBase; 
            Height = 32;
            
            // Sarsılmayı ve odak çerçevesini (kesik çizgiler) yok et
            TabStop = false;
            SetStyle(ControlStyles.Selectable, false);
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            // Arka planı parent'ın rengiyle doldur (hayalet kenarlıkları önler)
            if (Parent != null) using (var b = new SolidBrush(Parent.BackColor)) e.Graphics.FillRectangle(b, ClientRectangle);
            
            Color c = down ? ControlPaint.Dark(BaseColor, 0.1f) : (hover ? HoverColor : BaseColor);
            UiKit.FillRound(e.Graphics, new Rectangle(0, 0, Width - 1, Height - 1), Radius, c);
            
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(0, 0, Width, Height),
                TextColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}