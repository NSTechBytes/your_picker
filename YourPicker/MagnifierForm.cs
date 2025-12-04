using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace YourPicker
{
    /// <summary>
    /// A square magnifier form that follows the mouse cursor and displays a zoomed view of the screen.
    /// </summary>
    public class MagnifierForm : Form
    {
        private Timer timer;
        public int ZoomFactor { get; set; } = 3;
        public int CaptureSize { get; set; } = 30;
        private Bitmap magnifiedImage;
        private int squareSize;

        // Import SetWindowPos API.
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        // Constants for SetWindowPos.
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        public MagnifierForm()
        {
            // Enable double buffering.
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

            this.FormBorderStyle = FormBorderStyle.None;
            
            // Calculate square size and set client size only.
            squareSize = CaptureSize * ZoomFactor;
            this.ClientSize = new Size(squareSize, squareSize);
            this.MinimumSize = new Size(squareSize, squareSize);
            this.MaximumSize = new Size(squareSize, squareSize);

            // Square magnifier with border.

            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.Opacity = 0.9;

            // Set the initial location to the current mouse position.
            Point pos = Cursor.Position;
            this.Location = new Point(pos.X + 20, pos.Y + 20);

            timer = new Timer();
            timer.Interval = 100; // Update every 100 ms.
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        // Override OnResize to enforce square dimensions.
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (this.ClientSize.Width != squareSize || this.ClientSize.Height != squareSize)
            {
                this.ClientSize = new Size(squareSize, squareSize);
            }
        }

        // Override CreateParams to enforce topmost behavior.
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000008; // WS_EX_TOPMOST
                return cp;
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Get current mouse position.
            Point pos = Cursor.Position;
            // Position the magnifier slightly offset from the pointer.
            this.Location = new Point(pos.X + 20, pos.Y + 20);

            // Force this window to the topmost position.
            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);

            // Capture a small area around the mouse pointer.
            using (Bitmap bmp = new Bitmap(CaptureSize, CaptureSize))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(pos.X - CaptureSize / 2, pos.Y - CaptureSize / 2, 0, 0, new Size(CaptureSize, CaptureSize));
                }
                if (magnifiedImage != null)
                    magnifiedImage.Dispose();
                magnifiedImage = new Bitmap(bmp, new Size(CaptureSize * ZoomFactor, CaptureSize * ZoomFactor));
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (magnifiedImage != null)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                // Draw the magnified image filling the entire client area.
                e.Graphics.DrawImage(magnifiedImage, 0, 0, this.ClientSize.Width, this.ClientSize.Height);
                // Draw a cross in the center of the magnifier.
                int centerX = this.ClientSize.Width / 2;
                int centerY = this.ClientSize.Height / 2;
                using (Pen crossPen = new Pen(Color.White, 2))
                {
                    e.Graphics.DrawLine(crossPen, 0, centerY, this.ClientSize.Width, centerY);
                    e.Graphics.DrawLine(crossPen, centerX, 0, centerX, this.ClientSize.Height);
                }
                // Draw a border around the magnifier.
                using (Pen borderPen = new Pen(Color.White, 3))
                {
                    e.Graphics.DrawRectangle(borderPen, 1, 1, this.ClientSize.Width - 3, this.ClientSize.Height - 3);
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            timer.Stop();
            if (magnifiedImage != null)
                magnifiedImage.Dispose();
            base.OnFormClosing(e);
        }
    }
}
