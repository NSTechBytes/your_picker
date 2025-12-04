using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Rainmeter;

namespace YourPicker
{
    /// <summary>
    /// Plugin class for Rainmeter integration.
    /// Handles all Rainmeter API interactions and color picker invocation.
    /// </summary>
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

        /// <summary>
        /// Launch the color picker or magnifier in a separate STA thread.
        /// </summary>
        [DllExport]
        public static void ExecuteBang(IntPtr data, IntPtr args)
        {
            string arguments = Marshal.PtrToStringUni(args);
            if (arguments.Equals("-cp", StringComparison.OrdinalIgnoreCase))
            {
                System.Threading.Thread t = new System.Threading.Thread(() =>
                {
                    // Launch the modern color picker GUI.
                    ModernColorPickerForm picker = new ModernColorPickerForm(gDarkMode);
                    Application.Run(picker);
                    if (picker.DialogResult == DialogResult.OK)
                    {
                        Color selected = picker.SelectedColor;
                        if (gReturnValue.ToUpper() == "RGB")
                            gLastColor = ColorUtils.ColorToRgb(selected);
                        else
                            gLastColor = ColorUtils.ColorToHex(selected);
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
                                newColor = ColorUtils.ColorToRgb(selected);
                            else
                                newColor = ColorUtils.ColorToHex(selected);
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

        /// <summary>
        /// Returns the current color as a string.
        /// </summary>
        [DllExport]
        public static IntPtr GetString(IntPtr data)
        {
            return Marshal.StringToHGlobalUni(gLastColor);
        }

        /// <summary>
        /// Updates the last selected color.
        /// </summary>
        public static void UpdateLastColor(string newColor)
        {
            gLastColor = newColor;
        }
    }
}
