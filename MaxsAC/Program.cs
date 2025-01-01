using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class AutoClicker
{
    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(Keys vKey);

    private static bool isAutoClicking = false;
    private static Thread clickThread;

    private static int updateRate = 1;

    private static int debounceTime = 100;

    private static Keys hotkey = Keys.F6;

    private static string configFilePath = "config.json";

    private static string version = "3.2";

    public static async Task Main()
    {
        LoadConfig();

        DisplayMessage("Maxs Auto Clicker V3.0", MessageType.On);
        await CheckLatestVersionAsync();

        DisplayMessage("Use the hotkey to toggle autoclicker. For a list of commands, type 'help'.", MessageType.Keywords);
        Thread keyPressThread = new Thread(KeyPressMonitor);
        keyPressThread.Start();

        CommandListener();
    }

    public static async Task CheckLatestVersionAsync()
    {
        string url = "https://api.github.com/repos/maxplayz7566/maxsac/tags";

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("User-Agent", "MaxsAC/" + version);

            try
            {
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string responseData = await response.Content.ReadAsStringAsync();
                    var jsonArray = JsonConvert.DeserializeObject<JArray>(responseData);
                    string latest = jsonArray[0]["name"].ToString();

                    if (latest != version)
                    {
                        DisplayMessage($"Update available {version} -> {latest}", MessageType.On);
                        DisplayMessage($"Get latest version at: https://github.com/Maxplayz7566/MaxsAC/releases/tag/{latest}", MessageType.On);
                    } else
                    {
                        DisplayMessage($"No updates available", MessageType.On);
                    }
                }
                else
                {
                    DisplayMessage("Unexpected error occurred while sending http reques", MessageType.Off);
                }
            }
            catch (Exception ex)
            {
                DisplayMessage("Exception occurred while sending http request", MessageType.Off);
            }
        }
    }

    private static void KeyPressMonitor()
    {
        while (true)
        {
            if ((GetAsyncKeyState(hotkey) & 0x8000) != 0)
            {
                ToggleAutoClicker();
                Thread.Sleep(debounceTime);
            }

            Thread.Sleep(1);
        }
    }

    private static void ToggleAutoClicker()
    {
        if (isAutoClicking)
        {
            isAutoClicking = false;
            DisplayMessage("Autoclicker OFF", MessageType.Off);
            clickThread?.Abort();
        }
        else
        {
            isAutoClicking = true;
            DisplayMessage("Autoclicker ON", MessageType.On);
            clickThread = new Thread(ClickLoop);
            clickThread.Start();
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

    [DllImport("user32.dll")]
    private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

    private static void MouseEvent(MouseEventFlags value)
    {
        mouse_event((int)value, 0, 0, 0, 0);
    }

    [Flags]
    private enum MouseEventFlags
    {
        LeftDown = 0x02,
        LeftUp = 0x04,
        RightDown = 0x08,
        RightUp = 0x10,
        MiddleDown = 0x20,
        MiddleUp = 0x40
    }

    private static void DisplayMessage(string message, MessageType type)
    {
        switch (type)
        {
            case MessageType.Off:
                Console.ForegroundColor = ConsoleColor.Red;
                break;
            case MessageType.On:
                Console.ForegroundColor = ConsoleColor.Green;
                break;
            case MessageType.Keywords:
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;
            case MessageType.Regular:
            default:
                Console.ResetColor();
                break;
        }

        Console.WriteLine(message);
        Console.ResetColor();
    }

    private static void CommandListener()
    {
        while (true)
        {
            string input = Console.ReadLine()?.ToLower();
            if (string.IsNullOrWhiteSpace(input))
                continue;

            switch (input)
            {
                case "help":
                    DisplayMessage("Commands:", MessageType.Keywords);
                    DisplayMessage("help - Show this message", MessageType.Keywords);
                    DisplayMessage("status - Show the current status of the autoclicker", MessageType.Keywords);
                    DisplayMessage("reset - Resets the configs to the default", MessageType.Keywords);
                    DisplayMessage("setrate [rate] - Set the click rate in milliseconds", MessageType.Keywords);
                    DisplayMessage("setdebounce [time] - Set the debounce time in milliseconds", MessageType.Keywords);
                    DisplayMessage("sethotkey [key] - Set the hotkey for toggling the autoclicker", MessageType.Keywords);
                    break;

                case "status":
                    if (isAutoClicking)
                    {
                        DisplayMessage("Autoclicker ON", MessageType.On);
                    }
                    else
                    {
                        DisplayMessage("Autoclicker OFF", MessageType.Off);
                    }
                    break;

                case string command when command.StartsWith("setrate"):
                    SetUpdateRate(command);
                    break;

                case string command when command.StartsWith("setdebounce"):
                    SetDebounceTime(command);
                    break;

                case string command when command.StartsWith("sethotkey"):
                    SetHotkey(command);
                    break;

                case string command when command.StartsWith("reset"):
                    SetDefaultConfig();
                    break;

                default:
                    DisplayMessage("Unknown command. Type 'help' for a list of commands.", MessageType.Off);
                    break;
            }
        }
    }

    private static void SetUpdateRate(string command)
    {
        var parts = command.Split(' ');
        if (parts.Length == 2 && int.TryParse(parts[1], out int newRate) && newRate > 0)
        {
            updateRate = newRate;
            DisplayMessage($"Update rate set to {updateRate} ms", MessageType.On);

            SaveConfig();
        }
        else
        {
            DisplayMessage("Invalid rate. Please enter a positive integer.", MessageType.Off);
        }
    }

    private static void SetDefaultConfig()
    {
        var config = new Config { UpdateRate = 1, DebounceTime = 100, Hotkey = Keys.F6 };
        string configJson = JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText(configFilePath, configJson);
    }

    private static void SetDebounceTime(string command)
    {
        var parts = command.Split(' ');
        if (parts.Length == 2 && int.TryParse(parts[1], out int newDebounceTime) && newDebounceTime >= 0)
        {
            debounceTime = newDebounceTime;
            DisplayMessage($"Debounce time set to {debounceTime} ms", MessageType.On);

            SaveConfig();
        }
        else
        {
            DisplayMessage("Invalid debounce time. Please enter a non-negative integer.", MessageType.Off);
        }
    }

    private static void SetHotkey(string command)
    {
        var parts = command.Split(' ');
        if (parts.Length == 2 && Enum.TryParse(parts[1], true, out Keys newHotkey))
        {
            hotkey = newHotkey;
            DisplayMessage($"Hotkey set to {hotkey}", MessageType.On);

            SaveConfig();
        }
        else
        {
            DisplayMessage("Invalid hotkey. Please enter a valid key (e.g., F1, F2, etc.).", MessageType.Off);
        }
    }

    private static void LoadConfig()
    {
        if (File.Exists(configFilePath))
        {
            try
            {
                string configJson = File.ReadAllText(configFilePath);
                var config = JsonConvert.DeserializeObject<Config>(configJson);
                updateRate = config?.UpdateRate ?? 1;
                debounceTime = config?.DebounceTime ?? 50;
                hotkey = config?.Hotkey ?? Keys.F6;
            }
            catch (Exception ex)
            {
                DisplayMessage($"Error loading config: {ex.Message}", MessageType.Off);
            }
        }
        else
        {
            SetDefaultConfig();
        }
    }

    private static void SaveConfig()
    {
        var config = new Config { UpdateRate = updateRate, DebounceTime = debounceTime, Hotkey = hotkey };
        string configJson = JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText(configFilePath, configJson);
    }

    private class Config
    {
        public int UpdateRate { get; set; }
        public int DebounceTime { get; set; }
        public Keys Hotkey { get; set; }
    }

    private enum MessageType
    {
        Off,
        On,
        Keywords,
        Regular
    }
}
