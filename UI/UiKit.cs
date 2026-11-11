using System.Drawing;
using System.Drawing.Drawing2D;

namespace InputLagAzaltici
{
    static class UiKit
    {
        public static Color Bg = Color.FromArgb(10, 10, 12);
        public static Color Surface = Color.FromArgb(18, 18, 22);
        public static Color Surface2 = Color.FromArgb(28, 28, 34);
        public static Color Border = Color.FromArgb(45, 45, 55);
        public static Color Text = Color.FromArgb(245, 245, 250);
        public static Color Muted = Color.FromArgb(140, 140, 155);
        
        public static Color Accent = Color.FromArgb(88, 166, 255);
        public static Color AccentDark = Color.FromArgb(58, 120, 200);
        
        public static Color Ok = Color.FromArgb(63, 185, 80);
        public static Color Warn = Color.FromArgb(210, 153, 34);
        public static Color Err = Color.FromArgb(248, 81, 73);
        public static Color Purple = Color.FromArgb(163, 113, 247);

        public static readonly Font FontBase = new Font("Segoe UI", 9F);
        public static readonly Font FontBold = new Font("Segoe UI", 9F, FontStyle.Bold);
        public static readonly Font FontSemibold = new Font("Segoe UI Semibold", 9.5f);
        public static readonly Font FontMono = new Font("Consolas", 9.5f);
        public static readonly Font FontMono10 = new Font("Consolas", 10F);
        public static readonly Font FontBig = new Font("Segoe UI Semibold", 28f);
        public static readonly Font FontSmall = new Font("Segoe UI", 8f);

        public static Font FBase => FontBase;
        public static Font FBold => FontBold;
        public static Font FSemibold => FontSemibold;
        public static Font FMono => FontMono;
        public static Font FMono10 => FontMono10;
        public static Font FBig => FontBig;
        public static Font FSmall => FontSmall;

        public static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            var p = new GraphicsPath();
            int d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void FillRound(Graphics g, Rectangle r, int radius, Color c)
        { using (var p = RoundRect(r, radius)) using (var b = new SolidBrush(c)) g.FillPath(b, p); }

        public static void DrawRound(Graphics g, Rectangle r, int radius, Color c, float w = 1f)
        { using (var p = RoundRect(r, radius)) using (var pen = new Pen(c, w)) g.DrawPath(pen, p); }

        public static void DrawCheck(Graphics g, Rectangle box, bool chk, bool en)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var b = new Rectangle(box.X, box.Y, 16, 16);
            if (!en) { FillRound(g, b, 3, Color.FromArgb(40, 40, 48)); DrawRound(g, b, 3, Color.FromArgb(60, 60, 70)); return; }
            if (chk)
            {
                FillRound(g, b, 3, Accent);
                using (var pen = new Pen(Color.White, 2f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawLines(pen, new[] {
                        new Point(b.X + 3, b.Y + 8),
                        new Point(b.X + 6, b.Y + 11),
                        new Point(b.X + 13, b.Y + 5)
                    });
                }
            }
            else { FillRound(g, b, 3, Surface2); DrawRound(g, b, 3, Muted); }
        }
    }
}