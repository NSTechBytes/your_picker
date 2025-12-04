using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace YourPicker
{
    /// <summary>
    /// Modern custom slider control with colored track
    /// </summary>
    public class ModernSlider : Control
    {
        private int minimum = 0;
        private int maximum = 100;
        private int value = 0;
        private bool isDragging = false;
        private Color trackColor = Color.FromArgb(58, 175, 230); // Blue color
        private Color trackBackColor = Color.FromArgb(100, 100, 100); // Gray color
        private Color thumbColor = Color.FromArgb(58, 175, 230);
        private bool darkMode = false;
        private Orientation orientation = Orientation.Horizontal;

        public event EventHandler ValueChanged;

        public int Minimum
        {
            get => minimum;
            set
            {
                minimum = value;
                if (this.value < minimum) this.value = minimum;
                Invalidate();
            }
        }

        public int Maximum
        {
            get => maximum;
            set
            {
                maximum = value;
                if (this.value > maximum) this.value = maximum;
                Invalidate();
            }
        }

        public int Value
        {
            get => value;
            set
            {
                if (this.value != value)
                {
                    this.value = Math.Max(minimum, Math.Min(maximum, value));
                    Invalidate();
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public Color TrackColor
        {
            get => trackColor;
            set { trackColor = value; Invalidate(); }
        }

        public Color TrackBackColor
        {
            get => trackBackColor;
            set { trackBackColor = value; Invalidate(); }
        }

        public Color ThumbColor
        {
            get => thumbColor;
            set { thumbColor = value; Invalidate(); }
        }

        public bool DarkMode
        {
            get => darkMode;
            set
            {
                darkMode = value;
                if (darkMode)
                {
                    this.BackColor = ColorTranslator.FromHtml("#0d1117");
                    trackBackColor = ColorTranslator.FromHtml("#30363d");
                }
                else
                {
                    this.BackColor = ColorTranslator.FromHtml("#f6f8fa");
                    trackBackColor = ColorTranslator.FromHtml("#d0d7de");
                }
                Invalidate();
            }
        }

        public Orientation Orientation
        {
            get => orientation;
            set
            {
                orientation = value;
                Invalidate();
            }
        }

        public ModernSlider()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | 
                         ControlStyles.OptimizedDoubleBuffer | 
                         ControlStyles.ResizeRedraw | 
                         ControlStyles.UserPaint, true);
            this.Size = new Size(200, 30);
            this.Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int trackThickness = 4;
            int thumbSize = 16;

            if (orientation == Orientation.Horizontal)
            {
                PaintHorizontal(g, trackThickness, thumbSize);
            }
            else
            {
                PaintVertical(g, trackThickness, thumbSize);
            }
        }

        private void PaintHorizontal(Graphics g, int trackHeight, int thumbSize)
        {
            int trackY = (Height - trackHeight) / 2;
            int trackWidth = Width - thumbSize;
            int trackX = thumbSize / 2;

            // Calculate thumb position
            float percentage = (float)(value - minimum) / (maximum - minimum);
            int thumbX = trackX + (int)(trackWidth * percentage);

            // Draw background track (right side)
            using (var brush = new SolidBrush(trackBackColor))
            {
                var trackRect = new Rectangle(trackX, trackY, trackWidth, trackHeight);
                g.FillRectangle(brush, trackRect);
            }

            // Draw filled track (left side)
            using (var brush = new SolidBrush(trackColor))
            {
                int filledWidth = thumbX - trackX;
                if (filledWidth > 0)
                {
                    var filledRect = new Rectangle(trackX, trackY, filledWidth, trackHeight);
                    g.FillRectangle(brush, filledRect);
                }
            }

            // Draw thumb (circle)
            using (var brush = new SolidBrush(thumbColor))
            using (var pen = new Pen(darkMode ? ColorTranslator.FromHtml("#0d1117") : Color.White, 2))
            {
                var thumbRect = new Rectangle(thumbX - thumbSize / 2, Height / 2 - thumbSize / 2, thumbSize, thumbSize);
                g.FillEllipse(brush, thumbRect);
                g.DrawEllipse(pen, thumbRect);
            }
        }

        private void PaintVertical(Graphics g, int trackWidth, int thumbSize)
        {
            int trackX = (Width - trackWidth) / 2;
            int trackHeight = Height - thumbSize;
            int trackY = thumbSize / 2;

            // Calculate thumb position (inverted for vertical - top is max, bottom is min)
            float percentage = (float)(value - minimum) / (maximum - minimum);
            int thumbY = trackY + trackHeight - (int)(trackHeight * percentage);

            // Draw background track (top side)
            using (var brush = new SolidBrush(trackBackColor))
            {
                var trackRect = new Rectangle(trackX, trackY, trackWidth, trackHeight);
                g.FillRectangle(brush, trackRect);
            }

            // Draw filled track (bottom side)
            using (var brush = new SolidBrush(trackColor))
            {
                int filledHeight = (trackY + trackHeight) - thumbY;
                if (filledHeight > 0)
                {
                    var filledRect = new Rectangle(trackX, thumbY, trackWidth, filledHeight);
                    g.FillRectangle(brush, filledRect);
                }
            }

            // Draw thumb (circle)
            using (var brush = new SolidBrush(thumbColor))
            using (var pen = new Pen(darkMode ? ColorTranslator.FromHtml("#0d1117") : Color.White, 2))
            {
                var thumbRect = new Rectangle(Width / 2 - thumbSize / 2, thumbY - thumbSize / 2, thumbSize, thumbSize);
                g.FillEllipse(brush, thumbRect);
                g.DrawEllipse(pen, thumbRect);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                UpdateValueFromMouse(e.X, e.Y);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (isDragging)
            {
                UpdateValueFromMouse(e.X, e.Y);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            isDragging = false;
        }

        private void UpdateValueFromMouse(int mouseX, int mouseY)
        {
            int thumbSize = 16;

            if (orientation == Orientation.Horizontal)
            {
                int trackWidth = Width - thumbSize;
                int trackX = thumbSize / 2;
                int relativeX = mouseX - trackX;
                float percentage = Math.Max(0, Math.Min(1, (float)relativeX / trackWidth));
                Value = minimum + (int)((maximum - minimum) * percentage);
            }
            else
            {
                int trackHeight = Height - thumbSize;
                int trackY = thumbSize / 2;
                int relativeY = mouseY - trackY;
                // Inverted for vertical - top is max, bottom is min
                float percentage = 1.0f - Math.Max(0, Math.Min(1, (float)relativeY / trackHeight));
                Value = minimum + (int)((maximum - minimum) * percentage);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }
    }
}

