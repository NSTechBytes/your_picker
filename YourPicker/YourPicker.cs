using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Rainmeter;

namespace YourPicker
{
    // CUSTOM COLOR WHEEL CONTROL (SQUARE HUE-SATURATION MAP)
    public class ColorWheelControl : Control
    {

        private Bitmap colorWheel;
        private int wheelSize = 200;
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
            int x = Math.Max(0, Math.Min(point.X, this.Width - 1));
            int y = Math.Max(0, Math.Min(point.Y, this.Height - 1));

            SelectedHue = (double)x / (this.Width - 1) * 360.0;
            SelectedSaturation = 1.0 - ((double)y / (this.Height - 1));

            ColorChanged?.Invoke(this, EventArgs.Empty);
            Invalidate(); // Redraw to update the indicator.
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
            for (int x = 0; x < wheelSize; x++)
            {
                for (int y = 0; y < wheelSize; y++)
                {
                    double hue = (double)x / (wheelSize - 1) * 360.0;
                    double saturation = 1.0 - ((double)y / (wheelSize - 1));
                    
                    // Generate color with full brightness.
                    Color c = ColorFromHSV(hue, saturation, 1.0);
                    colorWheel.SetPixel(x, y, c);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (colorWheel != null)
                e.Graphics.DrawImage(colorWheel, 0, 0);

            int indicatorSize = 10;
            int selectedX = (int)(SelectedHue / 360.0 * (this.Width - 1));
            int selectedY = (int)((1.0 - SelectedSaturation) * (this.Height - 1));
            
            Rectangle indicatorRect = new Rectangle(selectedX - indicatorSize / 2, selectedY - indicatorSize / 2, indicatorSize, indicatorSize);
            
            // Draw a white border around the indicator for visibility on dark colors
            using (Pen indicatorPen = new Pen(Color.White, 2))
            {
                e.Graphics.DrawEllipse(indicatorPen, indicatorRect);
            }
            // Draw a black inner border for visibility on light colors
            using (Pen innerPen = new Pen(Color.Black, 1))
            {
                e.Graphics.DrawEllipse(innerPen, indicatorRect);
            }
        }

        // Helper method: Convert HSV values to a Color.
        public static Color ColorFromHSV(double hue, double saturation, double value)
        {
            return ColorUtils.ColorFromHSV(hue, saturation, value);
        }
    }


    // DESKTOP COLOR PICKER FORM (DPI-Aware) with Square Magnifier.
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

    /// <summary>
    /// Modern color picker form with enhanced visual design
    /// </summary>
    public class ModernColorPickerForm : Form
    {
        private ColorWheelControl colorWheel;
        private ModernSlider brightnessBar;
        private ModernSlider alphaBar;
        private Panel previewPanel;
        private Label hexLabel;
        private Label rgbLabel;
        private Button hexCopyButton;
        private Button rgbCopyButton;
        private Button desktopPickButton;
        private Button okButton;
        private Button cancelButton;

        // Slider controls for manual RGB and HSV adjustments
        private GroupBox groupBoxRGB;
        private GroupBox groupBoxHSV;
        private ModernSlider trackBar_R, trackBar_G, trackBar_B;
        private ModernSlider trackBar_H, trackBar_S, trackBar_V;
        private Label labelRValue, labelGValue, labelBValue;
        private Label labelHValue, labelSValue, labelVValue;

        private bool darkMode;
        private bool isUpdatingSliders = false;
        public Color SelectedColor { get; private set; }

        public ModernColorPickerForm(bool darkMode)
        {
            this.darkMode = darkMode;
            InitializeForm();
            CreateControls();
            UpdateColorFromWheel();
            RefreshUI();
        }

        private void InitializeForm()
        {
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(480, 480);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            this.UpdateStyles();
            this.Text = "YourPicker - Color Selector";
            this.Font = new Font("Segoe UI", 9F);

            if (darkMode)
            {
                this.BackColor = ColorTranslator.FromHtml("#0d1117");
                this.ForeColor = ColorTranslator.FromHtml("#c9d1d9");
            }
            else
            {
                this.BackColor = ColorTranslator.FromHtml("#f6f8fa");
                this.ForeColor = ColorTranslator.FromHtml("#24292f");
            }
        }

        private void CreateControls()
        {
            // Color wheel
            colorWheel = new ColorWheelControl { Location = new Point(15, 15) };
            colorWheel.ColorChanged += (s, e) => { UpdateColorFromWheel(); RefreshUI(); };
            this.Controls.Add(colorWheel);

            // Brightness slider
            var brightnessLabel = CreateLabel("Brightness", new Point(230, 15), true);
            this.Controls.Add(brightnessLabel);
            
            brightnessBar = new ModernSlider
            {
                Location = new Point(245, 40),
                Size = new Size(30, 180),
                Minimum = 0,
                Maximum = 100,
                Value = 100,
                DarkMode = darkMode,
                TrackColor = ColorTranslator.FromHtml("#3aafe6"),
                Orientation = Orientation.Vertical
            };
            brightnessBar.ValueChanged += (s, e) => { if (!isUpdatingSliders) { UpdateColorFromWheel(); RefreshUI(); } };
            this.Controls.Add(brightnessBar);

            // Opacity slider
            var alphaLabel = CreateLabel("Opacity", new Point(310, 15), true);
            this.Controls.Add(alphaLabel);
            
            alphaBar = new ModernSlider
            {
                Location = new Point(325, 40),
                Size = new Size(30, 180),
                Minimum = 0,
                Maximum = 100,
                Value = 100,
                DarkMode = darkMode,
                TrackColor = ColorTranslator.FromHtml("#3aafe6"),
                Orientation = Orientation.Vertical
            };
            alphaBar.ValueChanged += (s, e) => { if (!isUpdatingSliders) { UpdateColorFromWheel(); RefreshUI(); } };
            this.Controls.Add(alphaBar);

            // Preview panel with rounded corners
            previewPanel = new Panel { Location = new Point(375, 15), Size = new Size(90, 200) };
            previewPanel.Paint += PreviewPanel_Paint;
            this.Controls.Add(previewPanel);

            // HEX label and copy button
            hexLabel = new Label 
            { 
                Location = new Point(15, 230), 
                Size = new Size(270, 22), 
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10F)
            };
            if (darkMode) hexLabel.ForeColor = ColorTranslator.FromHtml("#c9d1d9");
            this.Controls.Add(hexLabel);
            
            hexCopyButton = CreateCopyButton(new Point(290, 228));
            hexCopyButton.Click += HexCopyLabel_Click;
            this.Controls.Add(hexCopyButton);

            // RGB label and copy button
            rgbLabel = new Label 
            { 
                Location = new Point(15, 260), 
                Size = new Size(270, 22), 
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10F)
            };
            if (darkMode) rgbLabel.ForeColor = ColorTranslator.FromHtml("#c9d1d9");
            this.Controls.Add(rgbLabel);
            
            rgbCopyButton = CreateCopyButton(new Point(290, 258));
            rgbCopyButton.Click += RgbCopyLabel_Click;
            this.Controls.Add(rgbCopyButton);

            // RGB Group
            CreateRGBGroup();
            
            // HSV Group
            CreateHSVGroup();

            // Buttons
            CreateButtons();
        }

        private void CreateRGBGroup()
        {
            groupBoxRGB = new GroupBox
            {
                Text = "RGB",
                Location = new Point(15, 295),
                Size = new Size(220, 125),
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold)
            };
            if (darkMode)
            {
                groupBoxRGB.BackColor = ColorTranslator.FromHtml("#0d1117");
                groupBoxRGB.ForeColor = ColorTranslator.FromHtml("#c9d1d9");
            }

            // R slider
            var labelR = new Label { Text = "R", Location = new Point(10, 28), AutoSize = true };
            trackBar_R = new ModernSlider
            {
                Location = new Point(30, 23),
                Size = new Size(140, 30),
                Minimum = 0,
                Maximum = 255,
                Value = 0,
                DarkMode = darkMode,
                TrackColor = ColorTranslator.FromHtml("#f85149")
            };
            trackBar_R.ValueChanged += TrackBarRGB_ValueChanged;
            
            labelRValue = new Label { Text = "0", Location = new Point(180, 25), AutoSize = true, Font = new Font("Segoe UI", 8F) };
            trackBar_R.ValueChanged += (s, e) => labelRValue.Text = trackBar_R.Value.ToString();
            
            groupBoxRGB.Controls.AddRange(new Control[] { labelR, trackBar_R, labelRValue });

            // G slider
            var labelG = new Label { Text = "G", Location = new Point(10, 63), AutoSize = true };
            trackBar_G = new ModernSlider
            {
                Location = new Point(30, 58),
                Size = new Size(140, 30),
                Minimum = 0,
                Maximum = 255,
                Value = 0,
                DarkMode = darkMode,
                TrackColor = ColorTranslator.FromHtml("#3fb950")
            };
            trackBar_G.ValueChanged += TrackBarRGB_ValueChanged;
            
            labelGValue = new Label { Text = "0", Location = new Point(180, 60), AutoSize = true, Font = new Font("Segoe UI", 8F) };
            trackBar_G.ValueChanged += (s, e) => labelGValue.Text = trackBar_G.Value.ToString();
            
            groupBoxRGB.Controls.AddRange(new Control[] { labelG, trackBar_G, labelGValue });

            // B slider
            var labelB = new Label { Text = "B", Location = new Point(10, 98), AutoSize = true };
            trackBar_B = new ModernSlider
            {
                Location = new Point(30, 93),
                Size = new Size(140, 30),
                Minimum = 0,
                Maximum = 255,
                Value = 0,
                DarkMode = darkMode,
                TrackColor = ColorTranslator.FromHtml("#1f6feb")
            };
            trackBar_B.ValueChanged += TrackBarRGB_ValueChanged;
            
            labelBValue = new Label { Text = "0", Location = new Point(180, 95), AutoSize = true, Font = new Font("Segoe UI", 8F) };
            trackBar_B.ValueChanged += (s, e) => labelBValue.Text = trackBar_B.Value.ToString();
            
            groupBoxRGB.Controls.AddRange(new Control[] { labelB, trackBar_B, labelBValue });

            this.Controls.Add(groupBoxRGB);
        }

        private void CreateHSVGroup()
        {
            groupBoxHSV = new GroupBox
            {
                Text = "HSV",
                Location = new Point(245, 295),
                Size = new Size(220, 125),
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold)
            };
            if (darkMode)
            {
                groupBoxHSV.BackColor = ColorTranslator.FromHtml("#0d1117");
                groupBoxHSV.ForeColor = ColorTranslator.FromHtml("#c9d1d9");
            }

            // H slider
            var labelH = new Label { Text = "H", Location = new Point(10, 28), AutoSize = true };
            trackBar_H = new ModernSlider
            {
                Location = new Point(30, 23),
                Size = new Size(140, 30),
                Minimum = 0,
                Maximum = 360,
                Value = 0,
                DarkMode = darkMode,
                TrackColor = ColorTranslator.FromHtml("#bc4c00")
            };
            trackBar_H.ValueChanged += TrackBarHSV_ValueChanged;
            
            labelHValue = new Label { Text = "0°", Location = new Point(180, 25), AutoSize = true, Font = new Font("Segoe UI", 8F) };
            trackBar_H.ValueChanged += (s, e) => labelHValue.Text = trackBar_H.Value + "°";
            
            groupBoxHSV.Controls.AddRange(new Control[] { labelH, trackBar_H, labelHValue });

            // S slider
            var labelS = new Label { Text = "S", Location = new Point(10, 63), AutoSize = true };
            trackBar_S = new ModernSlider
            {
                Location = new Point(30, 58),
                Size = new Size(140, 30),
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                DarkMode = darkMode,
                TrackColor = ColorTranslator.FromHtml("#a371f7")
            };
            trackBar_S.ValueChanged += TrackBarHSV_ValueChanged;
            
            labelSValue = new Label { Text = "0%", Location = new Point(180, 60), AutoSize = true, Font = new Font("Segoe UI", 8F) };
            trackBar_S.ValueChanged += (s, e) => labelSValue.Text = trackBar_S.Value + "%";
            
            groupBoxHSV.Controls.AddRange(new Control[] { labelS, trackBar_S, labelSValue });

            // V slider
            var labelV = new Label { Text = "V", Location = new Point(10, 98), AutoSize = true };
            trackBar_V = new ModernSlider
            {
                Location = new Point(30, 93),
                Size = new Size(140, 30),
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                DarkMode = darkMode,
                TrackColor = ColorTranslator.FromHtml("#f0883e")
            };
            trackBar_V.ValueChanged += TrackBarHSV_ValueChanged;
            
            labelVValue = new Label { Text = "0%", Location = new Point(180, 95), AutoSize = true, Font = new Font("Segoe UI", 8F) };
            trackBar_V.ValueChanged += (s, e) => labelVValue.Text = trackBar_V.Value + "%";
            
            groupBoxHSV.Controls.AddRange(new Control[] { labelV, trackBar_V, labelVValue });

            this.Controls.Add(groupBoxHSV);
        }

        private void CreateButtons()
        {
            // Pick from Screen button
            desktopPickButton = new Button 
            { 
                Text = "Pick from Screen", 
                Location = new Point(15, 435), 
                Size = new Size(140, 32),
                Font = new Font("Segoe UI", 9F),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            if (darkMode)
            {
                desktopPickButton.BackColor = ColorTranslator.FromHtml("#238636");
                desktopPickButton.ForeColor = Color.White;
                desktopPickButton.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#2ea043");
            }
            else
            {
                desktopPickButton.BackColor = ColorTranslator.FromHtml("#2da44e");
                desktopPickButton.ForeColor = Color.White;
                desktopPickButton.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#1a7f37");
            }
            desktopPickButton.Click += DesktopPickButton_Click;
            this.Controls.Add(desktopPickButton);

            // OK button
            okButton = new Button 
            { 
                Text = "OK", 
                Location = new Point(305, 435), 
                Size = new Size(75, 32),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            if (darkMode)
            {
                okButton.BackColor = ColorTranslator.FromHtml("#1f6feb");
                okButton.ForeColor = Color.White;
                okButton.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#388bfd");
            }
            else
            {
                okButton.BackColor = ColorTranslator.FromHtml("#0969da");
                okButton.ForeColor = Color.White;
                okButton.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#0550ae");
            }
            okButton.Click += (s, e) => { this.DialogResult = DialogResult.OK; this.Close(); };
            this.Controls.Add(okButton);
            
            // Cancel button
            cancelButton = new Button 
            { 
                Text = "Cancel", 
                Location = new Point(390, 435), 
                Size = new Size(75, 32),
                Font = new Font("Segoe UI", 9F),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            if (darkMode)
            {
                cancelButton.BackColor = ColorTranslator.FromHtml("#21262d");
                cancelButton.ForeColor = ColorTranslator.FromHtml("#c9d1d9");
                cancelButton.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#30363d");
            }
            else
            {
                cancelButton.BackColor = ColorTranslator.FromHtml("#f6f8fa");
                cancelButton.ForeColor = ColorTranslator.FromHtml("#24292f");
                cancelButton.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#d0d7de");
            }
            cancelButton.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.Add(cancelButton);
        }

        private Label CreateLabel(string text, Point location, bool bold = false)
        {
            var label = new Label 
            { 
                Text = text, 
                Location = location, 
                AutoSize = true,
                Font = bold ? new Font("Segoe UI Semibold", 9F, FontStyle.Bold) : new Font("Segoe UI", 9F)
            };
            if (darkMode) label.ForeColor = ColorTranslator.FromHtml("#c9d1d9");
            return label;
        }



        private Button CreateCopyButton(Point location)
        {
            var button = new Button
            {
                Text = "Copy",
                Location = location,
                Size = new Size(55, 26),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8F),
                FlatStyle = FlatStyle.Flat
            };
            
            if (darkMode)
            {
                button.BackColor = ColorTranslator.FromHtml("#21262d");
                button.ForeColor = ColorTranslator.FromHtml("#c9d1d9");
                button.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#30363d");
            }
            else
            {
                button.BackColor = ColorTranslator.FromHtml("#f6f8fa");
                button.ForeColor = ColorTranslator.FromHtml("#24292f");
                button.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#d0d7de");
            }
            
            return button;
        }

        private void PreviewPanel_Paint(object sender, PaintEventArgs e)
        {
            // Clear any region to ensure full rectangle painting
            previewPanel.Region = null;
            
            var rect = new Rectangle(0, 0, previewPanel.Width - 1, previewPanel.Height - 1);
            
            using (var brush = new SolidBrush(SelectedColor))
            {
                e.Graphics.FillRectangle(brush, rect);
            }
            
            using (var pen = new Pen(darkMode ? ColorTranslator.FromHtml("#30363d") : ColorTranslator.FromHtml("#d0d7de"), 2))
            {
                e.Graphics.DrawRectangle(pen, rect);
            }
            
            // Draw outer border (black) for better definition
            using (var outerPen = new Pen(Color.Black, 1))
            {
                e.Graphics.DrawRectangle(outerPen, rect);
            }
        }

        private void UpdateColorFromWheel()
        {
            double brightness = brightnessBar.Value / 100.0;
            Color baseColor = ColorWheelControl.ColorFromHSV(colorWheel.SelectedHue, colorWheel.SelectedSaturation, brightness);
            int alpha = (int)(alphaBar.Value / 100.0 * 255);
            SelectedColor = Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);
        }

        private void RefreshUI(bool updateHSVSliders = true)
        {
            isUpdatingSliders = true;

            trackBar_R.Value = SelectedColor.R;
            trackBar_G.Value = SelectedColor.G;
            trackBar_B.Value = SelectedColor.B;

            if (updateHSVSliders)
            {
                double h, s, v;
                ColorUtils.RgbToHsv(SelectedColor, out h, out s, out v);
                trackBar_H.Value = (int)Math.Round(h);
                trackBar_S.Value = (int)Math.Round(s * 100);
                trackBar_V.Value = (int)Math.Round(v * 100);

                brightnessBar.Value = (int)Math.Round(v * 100);
                colorWheel.SetSelection(h, s);
            }

            previewPanel.Invalidate();
            
            string hexColor = ColorUtils.ColorToHex(SelectedColor);
            string rgbColor = ColorUtils.ColorToRgb(SelectedColor);
            
            hexLabel.Text = $"HEX: {hexColor}";
            rgbLabel.Text = $"RGB: {rgbColor}";
            
            if (Plugin.gReturnValue.ToUpper() == "RGB")
                Plugin.UpdateLastColor(rgbColor);
            else
                Plugin.UpdateLastColor(hexColor);

            isUpdatingSliders = false;
        }

        private void TrackBarRGB_ValueChanged(object sender, EventArgs e)
        {
            if (!isUpdatingSliders)
            {
                isUpdatingSliders = true;
                int alpha = (int)(alphaBar.Value / 100.0 * 255);
                SelectedColor = Color.FromArgb(alpha, trackBar_R.Value, trackBar_G.Value, trackBar_B.Value);
                
                RefreshUI();
                isUpdatingSliders = false;
            }
        }

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
                
                // Update other controls but NOT the HSV sliders we are currently dragging
                brightnessBar.Value = (int)Math.Round(v * 100);
                colorWheel.SetSelection(h, s);
                
                RefreshUI(false);
                isUpdatingSliders = false;
            }
        }

        private void HexCopyLabel_Click(object sender, EventArgs e)
        {
            string hexColor = hexLabel.Text.Replace("HEX: ", "");
            Clipboard.SetText(hexColor);
            MessageBox.Show($"Copied {hexColor} to clipboard.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void RgbCopyLabel_Click(object sender, EventArgs e)
        {
            string rgbText = rgbLabel.Text;
            Clipboard.SetText(rgbText);
            MessageBox.Show($"Copied {rgbText} to clipboard.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DesktopPickButton_Click(object sender, EventArgs e)
        {
            using (DesktopColorPickerForm dpForm = new DesktopColorPickerForm())
            {
                if (dpForm.ShowDialog() == DialogResult.OK)
                {
                    Color picked = dpForm.PickedColor;
                    double hue, saturation, value;
                    ColorUtils.RgbToHsv(picked, out hue, out saturation, out value);
                    colorWheel.SetSelection(hue, saturation);
                    brightnessBar.Value = (int)(value * 100);
                    alphaBar.Value = (int)(picked.A / 255.0 * 100);
                    UpdateColorFromWheel();
                    RefreshUI();
                }
            }
        }
    }
}