using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json;

class AutoClicker : Form
{
    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(Keys vKey);

    private static bool isAutoClicking = false;
    private static Thread clickThread;
    private static int updateRate = 1;
    private static Keys hotkey = Keys.F6;
    private static string configFilePath = "config.json";
    private static string version = "3.4";

    private NumericUpDown updateRateInput;
    private ComboBox hotkeySelector;
    private Button toggleButton;
    private Button saveButton;
    private Button resetButton;
    private Label statusLabel;

    private KeyboardHook keyboardHook;

    public AutoClicker()
    {
        Text = $"Max's Auto Clicker v{version}";
        Width = 400;
        Height = 250;

        Label updateRateLabel = new Label { Text = "Click Rate (ms):", Top = 20, Left = 20, Width = 100 };
        updateRateInput = new NumericUpDown { Top = 20, Left = 140, Width = 100, Minimum = 1, Maximum = 1000 };

        Label hotkeyLabel = new Label { Text = "Hotkey:", Top = 60, Left = 20, Width = 100 };
        hotkeySelector = new ComboBox { Top = 60, Left = 140, Width = 100 };
        foreach (Keys key in Enum.GetValues(typeof(Keys)))
        {
            hotkeySelector.Items.Add(key);
        }

        toggleButton = new Button { Text = "Start AutoClicker", Top = 100, Left = 20, Width = 150 };
        toggleButton.Click += ToggleAutoClicker;

        saveButton = new Button { Text = "Save Config", Top = 100, Left = 200, Width = 150 };
        saveButton.Click += SaveConfig;

        resetButton = new Button { Text = "Reset Config", Top = 140, Left = 20, Width = 150 };
        resetButton.Click += ResetConfig;

        statusLabel = new Label { Text = "Status: Ready", Top = 180, Left = 20, Width = 350, ForeColor = System.Drawing.Color.Green };

        Controls.Add(updateRateLabel);
        Controls.Add(updateRateInput);
        Controls.Add(hotkeyLabel);
        Controls.Add(hotkeySelector);
        Controls.Add(toggleButton);
        Controls.Add(saveButton);
        Controls.Add(resetButton);
        Controls.Add(statusLabel);

        LoadConfig();

        // Initialize global keyboard hook
        keyboardHook = new KeyboardHook();
        keyboardHook.KeyPressed += OnKeyPressed;
        RegisterHotkey();
    }

    private void RegisterHotkey()
    {
        if (keyboardHook != null)
        {
            keyboardHook.KeyPressed -= OnKeyPressed; // Remove any previous handlers
            keyboardHook.KeyPressed += OnKeyPressed;
        }
    }

    private void LoadConfig()
    {
        if (File.Exists(configFilePath))
        {
            try
            {
                string configJson = File.ReadAllText(configFilePath);
                var config = JsonConvert.DeserializeObject<Config>(configJson);
                updateRate = config?.UpdateRate ?? 1;
                hotkey = config?.Hotkey ?? Keys.F6;

                updateRateInput.Value = updateRate;
                hotkeySelector.SelectedItem = hotkey;

                UpdateStatus("Config loaded successfully.", true);
            }
            catch
            {
                UpdateStatus("Error loading config. Using defaults.", false);
                ResetConfig(null, null);
            }
        }
        else
        {
            ResetConfig(null, null);
        }
    }

    private void SaveConfig(object sender, EventArgs e)
    {
        updateRate = (int)updateRateInput.Value;
        hotkey = (Keys)hotkeySelector.SelectedItem;

        var config = new Config { UpdateRate = updateRate, Hotkey = hotkey };
        string configJson = JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText(configFilePath, configJson);

        RegisterHotkey(); // Update hotkey registration

        UpdateStatus("Configuration saved successfully.", true);
    }

    private void ResetConfig(object sender, EventArgs e)
    {
        updateRate = 1;
        hotkey = Keys.F6;

        updateRateInput.Value = updateRate;
        hotkeySelector.SelectedItem = hotkey;

        SaveConfig(null, null);
        UpdateStatus("Configuration reset to defaults.", true);
    }

    private void ToggleAutoClicker(object sender, EventArgs e)
    {
        ToggleAutoClicker();
    }

    private void ToggleAutoClicker()
    {
        if (isAutoClicking)
        {
            isAutoClicking = false;
            toggleButton.Text = "Start AutoClicker";
            clickThread?.Abort();
            UpdateStatus("AutoClicker stopped.", false);
        }
        else
        {
            isAutoClicking = true;
            toggleButton.Text = "Stop AutoClicker";
            clickThread = new Thread(ClickLoop);
            clickThread.Start();
            UpdateStatus("AutoClicker started.", true);
        }
    }

    private static void ClickLoop()
    {
        while (isAutoClicking)
        {
            MouseEvent(MouseEventFlags.LeftDown | MouseEventFlags.LeftUp);
            Thread.Sleep(updateRate);
        }
    }

    private void UpdateStatus(string message, bool isSuccess)
    {
        statusLabel.Text = $"Status: {message}";
        statusLabel.ForeColor = isSuccess ? System.Drawing.Color.Green : System.Drawing.Color.Red;
    }

    private void OnKeyPressed(object sender, KeyPressedEventArgs e)
    {
        if (e.Key == hotkey)
        {
            ToggleAutoClicker();
        }
    }

    [DllImport("user32.dll")]
    private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

    private static void MouseEvent(MouseEventFlags value)
    {
        mouse_event((int)value, 0, 0, 0, 0);
    }

    private class Config
    {
        public int UpdateRate { get; set; }
        public Keys Hotkey { get; set; }
    }

    [Flags]
    private enum MouseEventFlags
    {
        LeftDown = 0x02,
        LeftUp = 0x04,
    }

    [STAThread]
    public static void Main()
    {
        Application.Run(new AutoClicker());
    }
}
