using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace InputLagAzaltici
{
    class Card : Panel
    {
        public string Title = "";
        public Color CardColor = UiKit.Surface;
        public int Radius = 10;

        public Card()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Padding = new Padding(16, 40, 16, 12);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            UiKit.FillRound(e.Graphics, r, Radius, CardColor);
            UiKit.DrawRound(e.Graphics, r, Radius, UiKit.Border);
            if (!string.IsNullOrEmpty(Title))
            {
                TextRenderer.DrawText(e.Graphics, Title, UiKit.FontSemibold, new Point(16, 12), UiKit.Text);
                using (var p = new Pen(UiKit.Border, 1))
                    e.Graphics.DrawLine(p, 16, 34, Width - 16, 34);
            }
            base.OnPaint(e);
        }
    }
}