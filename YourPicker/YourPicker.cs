using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Rainmeter;

namespace YourPicker
{
    // CUSTOM COLOR WHEEL CONTROL
    public class ColorWheelControl : Control
    {
        private Bitmap colorWheel;
        private int wheelSize = 300; // Smoother wheel.
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
            using (Pen pen = new Pen(Color.Black, 2))
                e.Graphics.DrawEllipse(pen, 0, 0, wheelSize - 1, wheelSize - 1);

            int radius = wheelSize / 2;
            int indicatorSize = 10;
            int selectedX = radius + (int)(SelectedSaturation * radius * Math.Cos(SelectedHue * Math.PI / 180));
            int selectedY = radius + (int)(SelectedSaturation * radius * Math.Sin(SelectedHue * Math.PI / 180));
            Rectangle indicatorRect = new Rectangle(selectedX - indicatorSize / 2, selectedY - indicatorSize / 2, indicatorSize, indicatorSize);
            using (SolidBrush brush = new SolidBrush(SelectedColor))
            {
                e.Graphics.FillEllipse(brush, indicatorRect);
            }
            using (Pen pen = new Pen(Color.White, 2))
            {
                e.Graphics.DrawEllipse(pen, indicatorRect);
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

    // DESKTOP COLOR PICKER FORM (DPI-Aware)
    public class DesktopColorPickerForm : Form
    {
        public Color PickedColor { get; private set; }

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
            this.Cursor = Cursors.Cross;
            this.Opacity = 0.01; // Nearly invisible.
            this.ShowInTaskbar = false;
            this.KeyDown += DesktopColorPickerForm_KeyDown;
            this.MouseDown += DesktopColorPickerForm_MouseDown;
        }

        private void DesktopColorPickerForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        private void DesktopColorPickerForm_MouseDown(object sender, MouseEventArgs e)
        {
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
    }

    // FIXED-SIZE COLOR PICKER FORM (Launched in Separate STA Thread)
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

        public Color SelectedColor { get; private set; }

        public ColorPickerForm()
        {
            // Fixed-size, non-resizable.
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(480, 440);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            this.UpdateStyles();
            this.Text = "YourPicker";

            // Color wheel.
            colorWheel = new ColorWheelControl { Location = new Point(20, 10) };
            colorWheel.ColorChanged += OnColorChanged;
            this.Controls.Add(colorWheel);

            // Brightness slider.
            Label brightnessLabel = new Label { Text = "Brightness", Location = new Point(340, 0), AutoSize = true };
            this.Controls.Add(brightnessLabel);
            brightnessBar = new TrackBar { Minimum = 0, Maximum = 100, Value = 100, TickFrequency = 10, Orientation = Orientation.Vertical, Location = new Point(340, 10), Height = 300 };
            brightnessBar.ValueChanged += OnColorChanged;
            this.Controls.Add(brightnessBar);

            // Opacity slider.
            Label alphaLabel = new Label { Text = "Opacity", Location = new Point(400, 0), AutoSize = true };
            this.Controls.Add(alphaLabel);
            alphaBar = new TrackBar { Minimum = 0, Maximum = 100, Value = 100, TickFrequency = 10, Orientation = Orientation.Vertical, Location = new Point(400, 10), Height = 300 };
            alphaBar.ValueChanged += OnColorChanged;
            this.Controls.Add(alphaBar);

            // Preview panel.
            previewPanel = new Panel { Location = new Point(20, 320), Size = new Size(300, 20), BorderStyle = BorderStyle.FixedSingle };
            this.Controls.Add(previewPanel);

            // HEX label.
            hexLabel = new Label { Location = new Point(20, 345), Size = new Size(300, 20), TextAlign = ContentAlignment.MiddleLeft };
            this.Controls.Add(hexLabel);
            hexCopyLabel = new Label { Text = "📋", Location = new Point(340, 345), AutoSize = true, Cursor = Cursors.Hand, Font = new Font("Segoe UI Emoji", 12) };
            hexCopyLabel.Click += HexCopyLabel_Click;
            this.Controls.Add(hexCopyLabel);

            // RGB label.
            rgbLabel = new Label { Location = new Point(20, 370), Size = new Size(300, 20), TextAlign = ContentAlignment.MiddleLeft };
            this.Controls.Add(rgbLabel);
            rgbCopyLabel = new Label { Text = "📋", Location = new Point(340, 370), AutoSize = true, Cursor = Cursors.Hand, Font = new Font("Segoe UI Emoji", 12) };
            rgbCopyLabel.Click += RgbCopyLabel_Click;
            this.Controls.Add(rgbCopyLabel);

            // "Pick Desktop" button.
            desktopPickButton = new Button { Text = "Pick Desktop", Location = new Point(20, 400), Size = new Size(90, 25) };
            desktopPickButton.Click += DesktopPickButton_Click;
            this.Controls.Add(desktopPickButton);

            // OK and Cancel buttons.
            okButton = new Button { Text = "OK", Location = new Point(120, 400), Size = new Size(60, 25) };
            okButton.Click += OkButton_Click;
            this.Controls.Add(okButton);
            cancelButton = new Button { Text = "Cancel", Location = new Point(190, 400), Size = new Size(60, 25) };
            cancelButton.Click += CancelButton_Click;
            this.Controls.Add(cancelButton);

            UpdatePreview();
        }

        private void OnColorChanged(object sender, EventArgs e)
        {
            UpdatePreview();
        }

        // Update preview and continuously update the shared global value.
        private void UpdatePreview()
        {
            double brightness = brightnessBar.Value / 100.0;
            Color baseColor = ColorWheelControl.ColorFromHSV(colorWheel.SelectedHue, colorWheel.SelectedSaturation, brightness);
            int alpha = (int)(alphaBar.Value / 100.0 * 255);
            SelectedColor = Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);
            previewPanel.BackColor = SelectedColor;

            string hexColor;
            string rgbColor;
            if (alpha == 255)
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
            rgbLabel.Text = $"RGB:{rgbColor}";
            if (Plugin.gReturnValue.ToUpper() == "RGB")
                Plugin.UpdateLastColor(rgbColor);
            else
                Plugin.UpdateLastColor(hexColor);
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
                    UpdatePreview();
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

    // PLUGIN CLASS
    public static class Plugin
    {
        public static string gReturnValue = "Hex"; // Default format.
        public static string gFinishAction = "";
        public static string gLastColor = "";
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
            gFinishAction = api.ReadString("OnFinishAction", "");
            maxValue = 1.0;
        }

        [DllExport]
        public static double Update(IntPtr data)
        {
            return 0.0;
        }

        // Launch the picker in a separate STA thread.
        [DllExport]
        public static void ExecuteBang(IntPtr data, IntPtr args)
        {
            string arguments = Marshal.PtrToStringUni(args);
            if (arguments.Equals("-cp", StringComparison.OrdinalIgnoreCase))
            {
                System.Threading.Thread t = new System.Threading.Thread(() =>
                {
                    ColorPickerForm picker = new ColorPickerForm();
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
                        if (!string.IsNullOrEmpty(gFinishAction))
                        {
                            try
                            {
                                new Rainmeter.API(gRainmeter).Execute(gFinishAction);
                            }
                            catch { }
                        }
                    }
                });
                t.SetApartmentState(System.Threading.ApartmentState.STA);
                t.Start();
            }
        }

        // Continuously returns the current color value.
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
