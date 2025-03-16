# YourPicker

YourPicker is a Rainmeter plugin that provides a custom color picker with a modern, VSCode-like interface. It features a high-resolution color wheel, brightness and opacity sliders, a desktop color picker, and supports continuous updates of the selected color in either RGB or Hex format. The plugin also supports executing a finish action (such as logging) when the color selection is complete.

---

## Features

- **Custom Color Wheel:**  
  A high-resolution (300×300) color wheel that allows for intuitive hue and saturation selection with a visual indicator.

- **Brightness & Opacity Sliders:**  
  Vertical sliders enable you to adjust brightness and opacity separately.

- **Desktop Color Picker:**  
  Pick a color directly from your desktop using a DPI-aware, nearly invisible full-screen form.

- **Configurable Output:**  
  Return the selected color as either a comma‑separated RGB string (e.g., `120,120,120`) or as a Hex string (e.g., `FFFFFF` or `FFFFFFC8` when alpha isn’t full).

- **Continuous Update:**  
  The plugin continuously updates the output value as you adjust the color, so your Rainmeter skin always displays the current selection.

- **Finish Action Support:**  
  Execute a Rainmeter bang (e.g., log a message) after a color is selected using the OnFinishAction setting.

- **Non‑Blocking UI:**  
  The color picker runs in a separate instance (STA thread), allowing Rainmeter’s main window to remain accessible.

---

## Requirements

- **Rainmeter:** Version 4.0 or later  
- **.NET Framework:** 4.5 or later  
- **Visual Studio:** For building the project  
- **DllExport Tool:** For exporting functions from your DLL (e.g., [UnmanagedExports](https://github.com/3F/DllExport))  
- **Rainmeter API:** Ensure you have access to the Rainmeter API assembly

---

## Installation & Deployment

1. **Clone or Download the Repository:**

   ```bash
   git clone https://github.com/yourusername/YourPicker.git
   ```

2. **Open the Project in Visual Studio:**

   Open the solution file in Visual Studio.

3. **Install the DllExport Package:**

   Install a DllExport tool (such as [UnmanagedExports](https://github.com/3F/DllExport)) via NuGet to ensure that the exported functions are visible to Rainmeter.

4. **Build the Project:**

   Build the project in Visual Studio to produce the `YourPicker.dll`.

5. **Deploy the DLL:**

   Copy `YourPicker.dll` into your Rainmeter plugins folder, typically located at:
   ```
   %USERPROFILE%\Documents\Rainmeter\Plugins
   ```

---

## Usage

In your Rainmeter skin’s INI file, reference the plugin and configure the desired output format and finish action.

**Example Skin Snippet:**

```ini
[MeasureYourPicker]
Measure=Plugin
Plugin=YourPicker.dll
ReturnValue=RGB
OnFinishAction=[!Log "Finish"]

[MeterButton]
Meter=String
MeasureName=MeasureYourPicker
X=10
Y=10
Text="Open Color Picker"
LeftMouseUpAction=[!CommandMeasure MeasureYourPicker  "-cp"]
```

- **ReturnValue:**  
  Set to `RGB` to return a comma-separated RGB value (e.g., `120,120,120`), or to `Hex` to return a hex string (e.g., `FFFFFF`).

- **OnFinishAction:**  
  Specify any Rainmeter bang to be executed after the color selection is complete (e.g., `[!Log "Finish"]`).

---

## How It Works

- **Color Wheel & Sliders:**  
  The plugin provides a custom color wheel control that lets you select hue and saturation. Brightness and opacity are adjusted with vertical sliders. As you adjust these controls, the plugin continuously updates the output value.

- **Desktop Color Picker:**  
  A separate, DPI-aware full-screen form (with nearly invisible opacity) lets you pick a color from anywhere on your desktop without interfering with your desktop appearance.

- **Plugin Exports:**  
  The plugin exports standard Rainmeter plugin functions (`Initialize`, `Finalize`, `Reload`, `Update`, `ExecuteBang`, `GetString`) using DllExport. It stores the last selected color in a global variable that is returned when Rainmeter queries the measure.

- **Separate Instance:**  
  The color picker UI is launched in its own STA thread, ensuring that the Rainmeter window remains available.

---

## Contributing

Contributions, bug reports, and feature requests are welcome! Feel free to fork this repository and submit pull requests.

---

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

---

## Acknowledgments

- [Rainmeter](https://www.rainmeter.net/) for providing an amazing customization platform.
- [UnmanagedExports](https://github.com/3F/DllExport) for enabling DllExport in .NET.
- Community contributions and inspirations from various Rainmeter skin developers.

---

Happy customizing!