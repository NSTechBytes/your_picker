using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Rainmeter;

namespace YourPicker
{
    // CUSTOM COLOR WHEEL CONTROL
    public class ColorWheelControl : Control
    {

        private Bitmap colorWheel;
        private int wheelSize = 200; // Reduced from 300.
        private bool isDragging = false;

        // Selected hue and saturation.
        public double SelectedHue { get; private set; } = 0.0;
        public double SelectedSaturation { get; private set; } = 0.0;

        // Returns the full-brightness color based on the wheel selection.
        public Color SelectedColor
        {
            get { return ColorFromHSV(SelectedHue, SelectedSaturation, 1.0); }
        }

        public event EventHandler ColorChanged;

        public ColorWheelControl()
        {
            // Enable double buffering.
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            this.UpdateStyles();

            this.Width = wheelSize;
            this.Height = wheelSize;
            CreateColorWheel();

            this.MouseDown += ColorWheelControl_MouseDown;
            this.MouseMove += ColorWheelControl_MouseMove;
            this.MouseUp += ColorWheelControl_MouseUp;
        }

        private void ColorWheelControl_MouseDown(object sender, MouseEventArgs e)
        {
            isDragging = true;
            UpdateColorFromPoint(e.Location);
        }

        private void ColorWheelControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
                UpdateColorFromPoint(e.Location);
        }

        private void ColorWheelControl_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        private void UpdateColorFromPoint(Point point)
        {
            int centerX = this.Width / 2;
            int centerY = this.Height / 2;
            int dx = point.X - centerX;
            int dy = point.Y - centerY;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance <= wheelSize / 2)
            {
                double angle = Math.Atan2(dy, dx);
                if (angle < 0)
                    angle += 2 * Math.PI;
                SelectedHue = angle * 180 / Math.PI; // 0-360
                SelectedSaturation = distance / (wheelSize / 2); // 0-1

                ColorChanged?.Invoke(this, EventArgs.Empty);
                Invalidate(); // Redraw to update the indicator.
            }
        }

        // External update of selection.
        public void SetSelection(double hue, double saturation)
        {
            SelectedHue = hue;
            SelectedSaturation = saturation;
            Invalidate();
        }

        private void CreateColorWheel()
        {
            colorWheel = new Bitmap(wheelSize, wheelSize);
            int radius = wheelSize / 2;
            for (int x = 0; x < wheelSize; x++)
            {
                for (int y = 0; y < wheelSize; y++)
                {
                    int dx = x - radius;
                    int dy = y - radius;
                    double distance = Math.Sqrt(dx * dx + dy * dy);
                    if (distance <= radius)
                    {
                        double angle = Math.Atan2(dy, dx);
                        if (angle < 0)
                            angle += 2 * Math.PI;
                        double hue = angle * 180 / Math.PI;
                        double saturation = distance / radius;
                        // Generate color with full brightness.
                        Color c = ColorFromHSV(hue, saturation, 1.0);
                        colorWheel.SetPixel(x, y, c);
                    }
                    else
                    {
                        colorWheel.SetPixel(x, y, Color.Transparent);
                    }
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (colorWheel != null)
                e.Graphics.DrawImage(colorWheel, 0, 0);
            // Draw a bold border around the color wheel.
            using (Pen borderPen = new Pen(Color.Black, 4))
                e.Graphics.DrawEllipse(borderPen, 0, 0, wheelSize - 1, wheelSize - 1);

            int radius = wheelSize / 2;
            int indicatorSize = 10;
            int selectedX = radius + (int)(SelectedSaturation * radius * Math.Cos(SelectedHue * Math.PI / 180));
            int selectedY = radius + (int)(SelectedSaturation * radius * Math.Sin(SelectedHue * Math.PI / 180));
            Rectangle indicatorRect = new Rectangle(selectedX - indicatorSize / 2, selectedY - indicatorSize / 2, indicatorSize, indicatorSize);
            using (SolidBrush brush = new SolidBrush(SelectedColor))
            {
                e.Graphics.FillEllipse(brush, indicatorRect);
            }
            // Draw a white border around the indicator.
            using (Pen indicatorPen = new Pen(Color.White, 2))
            {
                e.Graphics.DrawEllipse(indicatorPen, indicatorRect);
            }
        }

        // Helper method: Convert HSV values to a Color.
        public static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);
            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));
            switch (hi)
            {
                case 0: return Color.FromArgb(v, t, p);
                case 1: return Color.FromArgb(q, v, p);
                case 2: return Color.FromArgb(p, v, t);
                case 3: return Color.FromArgb(p, q, v);
                case 4: return Color.FromArgb(t, p, v);
                default: return Color.FromArgb(v, p, q);
            }
        }
    }

    public class MagnifierForm : Form
    {
        private Timer timer;
        public int ZoomFactor { get; set; } = 4;
        public int CaptureSize { get; set; } = 40;
        private Bitmap magnifiedImage;

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
            this.Size = new Size(CaptureSize * ZoomFactor, CaptureSize * ZoomFactor);

            // Create a circular region for a round magnifier.
            GraphicsPath gp = new GraphicsPath();
            gp.AddEllipse(new Rectangle(0, 0, this.Width, this.Height));
            this.Region = new Region(gp);

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
                e.Graphics.DrawImage(magnifiedImage, 0, 0, this.Width, this.Height);
                // Draw a cross in the center of the magnifier.
                int centerX = this.Width / 2;
                int centerY = this.Height / 2;
                using (Pen crossPen = new Pen(Color.White, 2))
                {
                    e.Graphics.DrawLine(crossPen, 0, centerY, this.Width, centerY);
                    e.Graphics.DrawLine(crossPen, centerX, 0, centerX, this.Height);
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

        // DESKTOP COLOR PICKER FORM (DPI-Aware) with Circular Magnifier.
        public class DesktopColorPickerForm : Form
    {
        public Color PickedColor { get; private set; }
        private MagnifierForm magnifier; // For the magnified preview.

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("gdi32.dll")]
        private static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);
        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("user32.dll")]
        private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext);
        private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

        public DesktopColorPickerForm()
        {
            // Set thread DPI awareness.
            SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            // Remove the crosshair; default cursor is used.
            this.Opacity = 0.01; // Nearly invisible.
            this.ShowInTaskbar = false;
            this.KeyDown += DesktopColorPickerForm_KeyDown;
            this.MouseDown += DesktopColorPickerForm_MouseDown;

            // Create and show the circular magnifier.
            magnifier = new MagnifierForm();
            magnifier.Show();
        }

        private void DesktopColorPickerForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                CloseMagnifier();
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        private void DesktopColorPickerForm_MouseDown(object sender, MouseEventArgs e)
        {
            CloseMagnifier();
            this.Hide();
            Point screenPoint = Cursor.Position;
            IntPtr hdc = GetDC(IntPtr.Zero);
            uint pixel = GetPixel(hdc, screenPoint.X, screenPoint.Y);
            ReleaseDC(IntPtr.Zero, hdc);
            int r = (int)(pixel & 0x000000FF);
            int g_val = (int)((pixel & 0x0000FF00) >> 8);
            int b = (int)((pixel & 0x00FF0000) >> 16);
            PickedColor = Color.FromArgb(r, g_val, b);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void CloseMagnifier()
        {
            if (magnifier != null)
            {
                magnifier.Close();
                magnifier = null;
            }
        }
    }

    // FIXED-SIZE COLOR PICKER FORM with Dark Mode and RGB/HSV sliders.
    // (The RGB and HSV adjustment boxes remain unchanged.)
    public class ColorPickerForm : Form
    {
        private ColorWheelControl colorWheel;
        private TrackBar brightnessBar;
        private TrackBar alphaBar;
        private Panel previewPanel;
        private Label hexLabel;
        private Label rgbLabel;
        private Label hexCopyLabel;
        private Label rgbCopyLabel;
        private Button desktopPickButton;
        private Button okButton;
        private Button cancelButton;

        // Slider controls for manual RGB and HSV adjustments.
        private GroupBox groupBoxRGB;
        private GroupBox groupBoxHSV;
        private TrackBar trackBar_R;
        private TrackBar trackBar_G;
        private TrackBar trackBar_B;
        private TrackBar trackBar_H;
        private TrackBar trackBar_S;
        private TrackBar trackBar_V;

        private bool darkMode;
        private bool isUpdatingSliders = false; // Prevent recursive updates.
        public Color SelectedColor { get; private set; }

        // Constructor accepts a darkMode parameter.
        public ColorPickerForm(bool darkMode)
        {
            this.darkMode = darkMode;
            // Adjusted form size to reduce overall height.
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(480, 480); // Overall height reduced.
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            this.UpdateStyles();
            this.Text = "YourPicker";

            if (this.darkMode)
            {
                this.BackColor = ColorTranslator.FromHtml("#0d1117");
                this.ForeColor = ColorTranslator.FromHtml("#c9d1d9");
            }

            // Color wheel.
            colorWheel = new ColorWheelControl { Location = new Point(20, 10) };
            colorWheel.ColorChanged += (s, e) => { UpdateColorFromWheel(); RefreshUI(); };
            this.Controls.Add(colorWheel);

            // Brightness slider.
            Label brightnessLabel = new Label { Text = "Brightness", Location = new Point(340, 0), AutoSize = true };
            if (this.darkMode)
                brightnessLabel.ForeColor = ColorTranslator.FromHtml("#c9d1d9");
            this.Controls.Add(brightnessLabel);
            brightnessBar = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 100,
                TickFrequency = 10,
                Orientation = Orientation.Vertical,
                Location = new Point(340, 10),
                Height = 200 // Reduced from 300.
            };
            brightnessBar.ValueChanged += (s, e) => { if (!isUpdatingSliders) { UpdateColorFromWheel(); RefreshUI(); } };
            if (this.darkMode)
            {
                brightnessBar.BackColor = ColorTranslator.FromHtml("#0d1117");
                brightnessBar.ForeColor = ColorTranslator.FromHtml("#c9d1d9");
            }
            this.Controls.Add(brightnessBar);

            // Opacity slider.
            Label alphaLabel = new Label { Text = "Opacity", Location = new Point(400, 0), AutoSize = true };
            if (this.darkMode)
                alphaLabel.ForeColor = ColorTranslator.FromHtml("#c9d1d9");
            this.Controls.Add(alphaLabel);
            alphaBar = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 100,
                TickFrequency = 10,
                Orientation = Orientation.Vertical,
                Location = new Point(400, 10),
                Height = 200 // Reduced from 300.
            };
            alphaBar.ValueChanged += (s, e) => { if (!isUpdatingSliders) { UpdateColorFromWheel(); RefreshUI(); } };
            if (this.darkMode)
            {
                alphaBar.BackColor = ColorTranslator.FromHtml("#0d1117");
                alphaBar.ForeColor = ColorTranslator.FromHtml("#c9d1d9");
            }
            this.Controls.Add(alphaBar);

            // Preview panel.
            previewPanel = new Panel { Location = new Point(20, 220), Size = new Size(300, 20), BorderStyle = BorderStyle.FixedSingle };
            if (this.darkMode)
                previewPanel.BackColor = ColorTranslator.FromHtml("#161b22");
            this.Controls.Add(previewPanel);

            // HEX label.
            hexLabel = new Label { Location = new Point(20, 245), Size = new Size(300, 20), TextAlign = ContentAlignment.MiddleLeft };
            if (this.darkMode)
                hexLabel.ForeColor = ColorTranslator.FromHtml("#c9d1d9");
            this.Controls.Add(hexLabel);
            hexCopyLabel = new Label
            {
                Text = "📋",
                Location = new Point(340, 245),
                AutoSize = true,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI Emoji", 12)
            };
            if (this.darkMode)
                hexCopyLabel.ForeColor = ColorTranslator.FromHtml("#c9d1d9");
            hexCopyLabel.Click += HexCopyLabel_Click;
            this.Controls.Add(hexCopyLabel);

            // RGB label.
            rgbLabel = new Label { Location = new Point(20, 270), Size = new Size(300, 20), TextAlign = ContentAlignment.MiddleLeft };
            if (this.darkMode)
                rgbLabel.ForeColor = ColorTranslator.FromHtml("#c9d1d9");
            this.Controls.Add(rgbLabel);
            rgbCopyLabel = new Label
            {
                Text = "📋",
                Location = new Point(340, 270),
                AutoSize = true,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI Emoji", 12)
            };
            if (this.darkMode)
                rgbCopyLabel.ForeColor = ColorTranslator.FromHtml("#c9d1d9");
            rgbCopyLabel.Click += RgbCopyLabel_Click;
            this.Controls.Add(rgbCopyLabel);

            // Group Box for RGB adjustments.
            groupBoxRGB = new GroupBox
            {
                Text = "RGB Adjustments",
                Location = new Point(20, 300),
                Size = new Size(200, 130)
            };
            if (this.darkMode)
            {
                groupBoxRGB.BackColor = ColorTranslator.FromHtml("#0d1117");
                groupBoxRGB.ForeColor = ColorTranslator.FromHtml("#c9d1d9");
            }
            // TrackBar for R.
            Label labelR = new Label { Text = "R:", Location = new Point(10, 20), AutoSize = true };
            trackBar_R = new TrackBar
            {
                Orientation = Orientation.Horizontal,
                Minimum = 0,
                Maximum = 255,
                TickFrequency = 5,
                Location = new Point(40, 15),
                Width = 140
            };
            trackBar_R.ValueChanged += TrackBarRGB_ValueChanged;
            if (this.darkMode)
            {
                trackBar_R.BackColor = ColorTranslator.FromHtml("#0d1117");
                trackBar_R.ForeColor = ColorTranslator.FromHtml("#c9d1d9");
            }
            groupBoxRGB.Controls.Add(labelR);
            groupBoxRGB.Controls.Add(trackBar_R);

            // TrackBar for G.
            Label labelG = new Label { Text = "G:", Location = new Point(10, 60), AutoSize = true };
            trackBar_G = new TrackBar
            {
                Orientation = Orientation.Horizontal,
                Minimum = 0,
                Maximum = 255,
                TickFrequency = 5,
                Location = new Point(40, 55),
                Width = 140
            };
            trackBar_G.ValueChanged += TrackBarRGB_ValueChanged;
            if (this.darkMode)
            {
                trackBar_G.BackColor = ColorTranslator.FromHtml("#0d1117");
                trackBar_G.ForeColor = ColorTranslator.FromHtml("#c9d1d9");
            }
            groupBoxRGB.Controls.Add(labelG);
            groupBoxRGB.Controls.Add(trackBar_G);

            // TrackBar for B.
            Label labelB = new Label { Text = "B:", Location = new Point(10, 100), AutoSize = true };
            trackBar_B = new TrackBar
            {
                Orientation = Orientation.Horizontal,
                Minimum = 0,
                Maximum = 255,
                TickFrequency = 5,
                Location = new Point(40, 95),
                Width = 140
            };
            trackBar_B.ValueChanged += TrackBarRGB_ValueChanged;
            if (this.darkMode)
            {
                trackBar_B.BackColor = ColorTranslator.FromHtml("#0d1117");
                trackBar_B.ForeColor = ColorTranslator.FromHtml("#c9d1d9");
            }
            groupBoxRGB.Controls.Add(labelB);
            groupBoxRGB.Controls.Add(trackBar_B);

            this.Controls.Add(groupBoxRGB);

            // Group Box for HSV adjustments.
            groupBoxHSV = new GroupBox
            {
                Text = "HSV Adjustments",
                Location = new Point(230, 300),
                Size = new Size(230, 130)
            };
            if (this.darkMode)
            {
                groupBoxHSV.BackColor = ColorTranslator.FromHtml("#0d1117");
                groupBoxHSV.ForeColor = ColorTranslator.FromHtml("#c9d1d9");
            }
            // TrackBar for H.
            Label labelH = new Label { Text = "H:", Location = new Point(10, 20), AutoSize = true };
            trackBar_H = new TrackBar
            {
                Orientation = Orientation.Horizontal,
                Minimum = 0,
                Maximum = 360,
                TickFrequency = 10,
                Location = new Point(40, 15),
                Width = 160
            };
            trackBar_H.ValueChanged += TrackBarHSV_ValueChanged;
            if (this.darkMode)
            {
                trackBar_H.BackColor = ColorTranslator.FromHtml("#0d1117");
                trackBar_H.ForeColor = ColorTranslator.FromHtml("#c9d1d9");
            }
            groupBoxHSV.Controls.Add(labelH);
            groupBoxHSV.Controls.Add(trackBar_H);

            // TrackBar for S.
            Label labelS = new Label { Text = "S:", Location = new Point(10, 60), AutoSize = true };
            trackBar_S = new TrackBar
            {
                Orientation = Orientation.Horizontal,
                Minimum = 0,
                Maximum = 100,
                TickFrequency = 5,
                Location = new Point(40, 55),
                Width = 160
            };
            trackBar_S.ValueChanged += TrackBarHSV_ValueChanged;
            if (this.darkMode)
            {
                trackBar_S.BackColor = ColorTranslator.FromHtml("#0d1117");
                trackBar_S.ForeColor = ColorTranslator.FromHtml("#c9d1d9");
            }
            groupBoxHSV.Controls.Add(labelS);
            groupBoxHSV.Controls.Add(trackBar_S);

            // TrackBar for V.
            Label labelV = new Label { Text = "V:", Location = new Point(10, 100), AutoSize = true };
            trackBar_V = new TrackBar
            {
                Orientation = Orientation.Horizontal,
                Minimum = 0,
                Maximum = 100,
                TickFrequency = 5,
                Location = new Point(40, 95),
                Width = 160
            };
            trackBar_V.ValueChanged += TrackBarHSV_ValueChanged;
            if (this.darkMode)
            {
                trackBar_V.BackColor = ColorTranslator.FromHtml("#0d1117");
                trackBar_V.ForeColor = ColorTranslator.FromHtml("#c9d1d9");
            }
            groupBoxHSV.Controls.Add(labelV);
            groupBoxHSV.Controls.Add(trackBar_V);

            this.Controls.Add(groupBoxHSV);

            // "Pick Desktop" button.
            desktopPickButton = new Button { Text = "Pick Desktop", Location = new Point(20, 440), Size = new Size(90, 25) };
            if (this.darkMode)
            {
                desktopPickButton.FlatStyle = FlatStyle.Flat;
                desktopPickButton.BackColor = ColorTranslator.FromHtml("#161b22");
                desktopPickButton.ForeColor = ColorTranslator.FromHtml("#c9d1d9");
            }
            desktopPickButton.Click += DesktopPickButton_Click;
            this.Controls.Add(desktopPickButton);

            // OK and Cancel buttons.
            okButton = new Button { Text = "OK", Location = new Point(120, 440), Size = new Size(60, 25) };
            if (this.darkMode)
            {
                okButton.FlatStyle = FlatStyle.Flat;
                okButton.BackColor = ColorTranslator.FromHtml("#161b22");
                okButton.ForeColor = ColorTranslator.FromHtml("#c9d1d9");
            }
            okButton.Click += OkButton_Click;
            this.Controls.Add(okButton);
            cancelButton = new Button { Text = "Cancel", Location = new Point(190, 440), Size = new Size(60, 25) };
            if (this.darkMode)
            {
                cancelButton.FlatStyle = FlatStyle.Flat;
                cancelButton.BackColor = ColorTranslator.FromHtml("#161b22");
                cancelButton.ForeColor = ColorTranslator.FromHtml("#c9d1d9");
            }
            cancelButton.Click += CancelButton_Click;
            this.Controls.Add(cancelButton);

            // Initialize SelectedColor and update the UI.
            UpdateColorFromWheel();
            RefreshUI();
        }

        // Called when the color wheel, brightness, or alpha sliders change.
        private void UpdateColorFromWheel()
        {
            double brightness = brightnessBar.Value / 100.0;
            Color baseColor = ColorWheelControl.ColorFromHSV(colorWheel.SelectedHue, colorWheel.SelectedSaturation, brightness);
            int alpha = (int)(alphaBar.Value / 100.0 * 255);
            SelectedColor = Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);
        }

        // Refresh all UI elements (preview panel, labels, and slider positions).
        private void RefreshUI()
        {
            isUpdatingSliders = true;

            // Update RGB sliders.
            trackBar_R.Value = SelectedColor.R;
            trackBar_G.Value = SelectedColor.G;
            trackBar_B.Value = SelectedColor.B;

            // Convert current color to HSV.
            double h, s, v;
            RgbToHsv(SelectedColor, out h, out s, out v);
            trackBar_H.Value = (int)Math.Round(h);
            trackBar_S.Value = (int)Math.Round(s * 100);
            trackBar_V.Value = (int)Math.Round(v * 100);

            // Update brightness slider and color wheel.
            brightnessBar.Value = (int)Math.Round(v * 100);
            colorWheel.SetSelection(h, s);

            // Update preview panel and labels.
            previewPanel.BackColor = SelectedColor;
            previewPanel.Invalidate(); // Force repaint.
            string hexColor;
            string rgbColor;
            if (SelectedColor.A == 255)
            {
                hexColor = $"{SelectedColor.R:X2}{SelectedColor.G:X2}{SelectedColor.B:X2}";
                rgbColor = $"{SelectedColor.R},{SelectedColor.G},{SelectedColor.B}";
            }
            else
            {
                hexColor = $"{SelectedColor.R:X2}{SelectedColor.G:X2}{SelectedColor.B:X2}{SelectedColor.A:X2}";
                rgbColor = $"{SelectedColor.R},{SelectedColor.G},{SelectedColor.B},{SelectedColor.A}";
            }
            hexLabel.Text = $"HEX: {hexColor}";
            rgbLabel.Text = $"RGB: {rgbColor}";
            if (Plugin.gReturnValue.ToUpper() == "RGB")
                Plugin.UpdateLastColor(rgbColor);
            else
                Plugin.UpdateLastColor(hexColor);

            isUpdatingSliders = false;
        }

        // Converts an RGB color to HSV.
        private void RgbToHsv(Color color, out double hue, out double saturation, out double value)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;
            if (delta == 0)
                hue = 0;
            else if (max == r)
                hue = 60 * (((g - b) / delta) % 6);
            else if (max == g)
                hue = 60 * (((b - r) / delta) + 2);
            else
                hue = 60 * (((r - g) / delta) + 4);
            if (hue < 0)
                hue += 360;
            saturation = (max == 0) ? 0 : delta / max;
            value = max;
        }

        // Event handler for changes in any of the RGB sliders.
        private void TrackBarRGB_ValueChanged(object sender, EventArgs e)
        {
            if (!isUpdatingSliders)
            {
                isUpdatingSliders = true;
                int r = trackBar_R.Value;
                int g = trackBar_G.Value;
                int b = trackBar_B.Value;
                int alpha = (int)(alphaBar.Value / 100.0 * 255);
                SelectedColor = Color.FromArgb(alpha, r, g, b);
                double h, s, v;
                RgbToHsv(SelectedColor, out h, out s, out v);
                trackBar_H.Value = (int)Math.Round(h);
                trackBar_S.Value = (int)Math.Round(s * 100);
                trackBar_V.Value = (int)Math.Round(v * 100);
                brightnessBar.Value = (int)Math.Round(v * 100);
                colorWheel.SetSelection(h, s);
                RefreshUI();
                isUpdatingSliders = false;
            }
        }

        // Event handler for changes in any of the HSV sliders.
        private void TrackBarHSV_ValueChanged(object sender, EventArgs e)
        {
            if (!isUpdatingSliders)
            {
                isUpdatingSliders = true;
                double h = trackBar_H.Value;
                double s = trackBar_S.Value / 100.0;
                double v = trackBar_V.Value / 100.0;
                Color baseColor = ColorWheelControl.ColorFromHSV(h, s, v);
                int alpha = (int)(alphaBar.Value / 100.0 * 255);
                SelectedColor = Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);
                trackBar_R.Value = SelectedColor.R;
                trackBar_G.Value = SelectedColor.G;
                trackBar_B.Value = SelectedColor.B;
                brightnessBar.Value = (int)Math.Round(v * 100);
                colorWheel.SetSelection(h, s);
                RefreshUI();
                isUpdatingSliders = false;
            }
        }

        private void HexCopyLabel_Click(object sender, EventArgs e)
        {
            string hexColor = hexLabel.Text.Replace("HEX: ", "");
            Clipboard.SetText(hexColor);
            MessageBox.Show("Copied " + hexColor + " to clipboard.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void RgbCopyLabel_Click(object sender, EventArgs e)
        {
            string rgbText = rgbLabel.Text;
            Clipboard.SetText(rgbText);
            MessageBox.Show("Copied " + rgbText + " to clipboard.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DesktopPickButton_Click(object sender, EventArgs e)
        {
            using (DesktopColorPickerForm dpForm = new DesktopColorPickerForm())
            {
                if (dpForm.ShowDialog() == DialogResult.OK)
                {
                    Color picked = dpForm.PickedColor;
                    double hue, saturation, value;
                    RgbToHsv(picked, out hue, out saturation, out value);
                    colorWheel.SetSelection(hue, saturation);
                    brightnessBar.Value = (int)(value * 100);
                    alphaBar.Value = (int)(picked.A / 255.0 * 100);
                    UpdateColorFromWheel();
                    RefreshUI();
                }
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }

    // PLUGIN CLASS for Rainmeter integration.
    public static class Plugin
    {
        public static string gReturnValue = "Hex"; // Default format.
        public static string gFinishAction = "";
        public static string gLastColor = "";
        public static string myName = "";
        public static bool gDarkMode = false; // Dark mode flag.
        public static IntPtr gRainmeter = IntPtr.Zero;

        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            data = IntPtr.Zero;
            gRainmeter = rm;
        }

        [DllExport]
        public static void Finalize(IntPtr data)
        {
        }

        [DllExport]
        public static void Reload(IntPtr data, IntPtr rm, ref double maxValue)
        {
            Rainmeter.API api = new Rainmeter.API(rm);
            gReturnValue = api.ReadString("ReturnValue", "Hex");
            myName = api.GetMeasureName();
            gFinishAction = api.ReadString("OnFinishAction", "");
            // Read DarkMode setting from Rainmeter. If DarkMode=1 then enable dark mode.
            gDarkMode = api.ReadInt("DarkMode", 0) == 1;
            maxValue = 1.0;
        }

        [DllExport]
        public static double Update(IntPtr data)
        {
            return 0.0;
        }

        // Launch the color picker or magnifier in a separate STA thread.
        [DllExport]
        public static void ExecuteBang(IntPtr data, IntPtr args)
        {
            string arguments = Marshal.PtrToStringUni(args);
            if (arguments.Equals("-cp", StringComparison.OrdinalIgnoreCase))
            {
                System.Threading.Thread t = new System.Threading.Thread(() =>
                {
                    // Launch the full color picker GUI.
                    ColorPickerForm picker = new ColorPickerForm(gDarkMode);
                    Application.Run(picker);
                    if (picker.DialogResult == DialogResult.OK)
                    {
                        Color selected = picker.SelectedColor;
                        if (gReturnValue.ToUpper() == "RGB")
                        {
                            gLastColor = $"{selected.R},{selected.G},{selected.B}";
                        }
                        else
                        {
                            if (selected.A < 255)
                                gLastColor = $"{selected.R:X2}{selected.G:X2}{selected.B:X2}{selected.A:X2}";
                            else
                                gLastColor = $"{selected.R:X2}{selected.G:X2}{selected.B:X2}";
                        }
                        Rainmeter.API api = new Rainmeter.API(gRainmeter);
                        api.Execute("[!UpdateMeasure MeasureYourPicker]");
                        if (!string.IsNullOrEmpty(gFinishAction))
                        {
                            try
                            {
                                api.Execute(gFinishAction);
                            }
                            catch { }
                        }
                    }
                });
                t.SetApartmentState(System.Threading.ApartmentState.STA);
                t.Start();
            }
            else if (arguments.Equals("-mp", StringComparison.OrdinalIgnoreCase))
            {
                // In -mp mode, show only the magnifier (DesktopColorPickerForm) to select a color.
                System.Threading.Thread t = new System.Threading.Thread(() =>
                {
                    using (DesktopColorPickerForm dpForm = new DesktopColorPickerForm())
                    {
                        Application.Run(dpForm);
                        if (dpForm.DialogResult == DialogResult.OK)
                        {
                            Color selected = dpForm.PickedColor;
                            string newColor;
                            if (gReturnValue.ToUpper() == "RGB")
                            {
                                newColor = $"{selected.R},{selected.G},{selected.B}";
                            }
                            else
                            {
                                if (selected.A < 255)
                                    newColor = $"{selected.R:X2}{selected.G:X2}{selected.B:X2}{selected.A:X2}";
                                else
                                    newColor = $"{selected.R:X2}{selected.G:X2}{selected.B:X2}";
                            }
                            // First update the plugin's value.
                            Plugin.UpdateLastColor(newColor);
                            Rainmeter.API api = new Rainmeter.API(gRainmeter);
                            // Update the measure.
                            api.Execute($"!UpdateMeasure \"{myName}\"");
                            // Then execute on finish action.
                            if (!string.IsNullOrEmpty(gFinishAction))
                            {
                                try
                                {
                                    api.Execute(gFinishAction);
                                }
                                catch { }
                            }
                        }
                    }
                });
                t.SetApartmentState(System.Threading.ApartmentState.STA);
                t.Start();
            }
        }

        // Returns the current color as a string.
        [DllExport]
        public static IntPtr GetString(IntPtr data)
        {
            return Marshal.StringToHGlobalUni(gLastColor);
        }

        public static void UpdateLastColor(string newColor)
        {
            gLastColor = newColor;
        }
    }
}
