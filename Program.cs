namespace MIDIPCPortal
{
    using Raylib_cs;
    using Melanchall.DryWetMidi.Core;
    using Melanchall.DryWetMidi.Multimedia;
    using System.Net;
    using System.Text.Json;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Linq;
    class Program
    {
        static Button SelectDeviceButton = new Button(new Rectangle(10, 10, 200, 50), "Select Device", Color.Blue);
        static Button BindNoteToLaunchAppButton = new Button(new Rectangle(10, 70, 200, 50), "Bind Note to App?", Color.Blue);
        static TextBox textBox = new TextBox(new Rectangle(225, 30, 500, 50), "Click Here and Type...", Color.Black);
        static bool isEnteringPath = false;
        static bool selectingDevice = false;
        static InputDevice? selectedDevice;
        static string appPath = "";
        static double startTime = Raylib.GetTime();
        private static Dictionary<string, string> EventAppPaths = new Dictionary<string, string>();
        private static Dictionary<InputDevice, Button> devices = new Dictionary<InputDevice, Button>();

        private class SavedData
        {
            public string DeviceName { get; set; }
            public Dictionary<string, string> EventAppPaths { get; set; }
        }

        static void Main(string[] args)
        {
            Raylib.InitWindow(800, 450, "MIDI PC Portal");
            Raylib.SetTargetFPS(60);

            LoadData();

            while (!Raylib.WindowShouldClose())
            {
                Update();
                Draw();
            }

            Raylib.CloseWindow();
        }

        static void LoadData()
        {
            if (File.Exists("data.json"))
            {
                string json = File.ReadAllTextAsync("data.json").Result;
                if (!string.IsNullOrEmpty(json))
                {
                    SavedData? data = JsonSerializer.Deserialize<SavedData>(json);
                    if (data != null)
                    {
                        EventAppPaths = data.EventAppPaths;
                        selectedDevice = InputDevice.GetAll().FirstOrDefault(device => device.Name == data.DeviceName);
                    }
                }
            }
        }

        static void Update()
        {
            if (SelectDeviceButton.IsClicked())
            {
                selectingDevice = true;
                GetDevices();
            }

            if (BindNoteToLaunchAppButton.IsClicked())
            {
                isEnteringPath = true;
            }

            if (selectingDevice && devices.Count > 0)
            {
                foreach (var device in devices.Values)
                {
                    device.Update();
                }
            }

            if (Raylib.GetTime() - startTime > 5 && selectedDevice != null)
            {
                SaveData().Wait();
                startTime = Raylib.GetTime();
            }
        }

        static void Draw()
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.White);

            SelectDeviceButton.Draw();
            BindNoteToLaunchAppButton.Draw();

            if (isEnteringPath)
            {
                textBox.Draw();
                if (textBox.IsClicked() || textBox.Typing) textBox.EnterInput();
                if (textBox.Submitted)
                {
                    appPath = textBox.Text;
                    textBox.Submitted = false;
                    isEnteringPath = false;
                }
            }

            if (selectingDevice && devices.Count > 0)
            {
                foreach (var device in devices.Values)
                {
                    device.Draw();
                }
            }

            if (selectedDevice != null)
            {
                Raylib.DrawText($"Selected Device: {selectedDevice.Name}", 500, 10, 20, Color.Black);
                if (selectedDevice.IsListeningForEvents && !string.IsNullOrEmpty(appPath))
                {
                    Raylib.DrawText("Press the Note on the MIDI Device to Launch the App", 10, 200, 20, Color.Black);
                }
                else if (selectedDevice.IsListeningForEvents && string.IsNullOrEmpty(appPath))
                {
                    Raylib.DrawText("Please Enter the App Path to Bind to the Note", 10, 200, 20, Color.Black);
                }
                ConnectAndListenToDevice();
            }

            if (!string.IsNullOrEmpty(appPath) && !isEnteringPath)
            {
                Raylib.DrawText($"App Path: {appPath}", 500, 30, 20, Color.Black);
            }

            Raylib.EndDrawing();
        }

        private static async Task SaveData()
        {
            SavedData data = new SavedData
            {
                DeviceName = selectedDevice?.Name,
                EventAppPaths = EventAppPaths
            };
            string json = JsonSerializer.Serialize(data);
            await File.WriteAllTextAsync("data.json", json);
        }

        static void GetDevices()
        {
            if (InputDevice.GetAll().Count > 0 && devices.Count == 0)
            {
                foreach (var device in InputDevice.GetAll())
                {
                    if (!devices.ContainsKey(device))
                    {
                        devices.Add(device, new Button(new Rectangle(10, 100 + (devices.Count + 1) * 60, 600, 50), device.Name, Color.Blue));
                        devices[device].OnClicked = () =>
                        {
                            selectedDevice = device;
                            selectingDevice = false;
                        };
                    }
                }
            }
        }

        static void ConnectAndListenToDevice()
        {
            selectedDevice.Connect();
            if (selectedDevice.IsEnabled && !selectedDevice.IsListeningForEvents)
            {
                selectedDevice.EventReceived += (sender, e) =>
                {
                    if (e.Event.EventType == MidiEventType.NoteOn)
                    {
                        string? eventName = e.Event.ToString()?.Split(',')[0]?.Replace("(", "").Replace(")", "");
                        if (eventName == null) return;
                        if (!EventAppPaths.ContainsKey(eventName) && !string.IsNullOrEmpty(appPath))
                        {
                            EventAppPaths.Add(eventName, appPath);
                            appPath = "";
                        }
                        else
                        {
                            System.Diagnostics.Process.Start(EventAppPaths[eventName]);
                        }
                    }
                };
                selectedDevice.StartEventsListening();
            }
        }
    }

    class Button
    {
        public Rectangle Rect { get; }
        public string Text { get; }
        public Color Color { get; }
        public Action OnClicked { get; set; } = () => { };

        public Button(Rectangle rect, string text, Color color)
        {
            Rect = rect;
            Text = text;
            Color = color;
        }

        public void Draw()
        {
            Raylib.DrawRectangleRec(Rect, Color);
            Raylib.DrawText(Text, (int)Rect.X + 10, (int)Rect.Y + 10, 20, Color.Black);
        }

        public void Update()
        {
            if (IsClicked())
            {
                OnClicked();
            }
        }

        public bool IsClicked()
        {
            return Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), Rect) && Raylib.IsMouseButtonPressed(MouseButton.Left);
        }
    }

    class TextBox
    {
        public Rectangle Rect { get; }
        public string Text { get; private set; }
        public Color Color { get; }
        public Color TextColor { get; set; } = Color.RayWhite;
        public bool Typing { get; private set; } = false;
        private bool longBackspace = false;
        private double backspaceStartTime = 0;
        private bool clearedText = false;
        public bool Submitted { get; set; } = false;

        public TextBox(Rectangle rect, string text, Color color)
        {
            Rect = rect;
            Text = text;
            Color = color;
        }

        public void Draw()
        {
            Raylib.DrawRectangleRec(Rect, Color);
            Raylib.DrawText(Text, (int)Rect.X + 10, (int)Rect.Y + 10, 20, Typing ? TextColor : Color.Gray);
        }

        public bool IsClicked()
        {
            if (Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), Rect) && Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                Typing = true;
            }
            if (!Typing && !Submitted)
            {
                Text = "Click Here and Type...";
                clearedText = false;
            }
            else if (Typing && !clearedText)
            {
                Text = "";
                clearedText = true;
            }
            return Typing;
        }

        public unsafe void EnterInput()
        {
            if (Typing)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.Backspace))
                {
                    if (Text.Length > 0)
                    {
                        Text = Text.Substring(0, Text.Length - 1);
                        backspaceStartTime = Raylib.GetTime();
                        longBackspace = false;
                    }
                }
                else if (Raylib.IsKeyDown(KeyboardKey.Backspace))
                {
                    if (Raylib.GetTime() - backspaceStartTime > 0.5 && Text.Length > 0)
                    {
                        longBackspace = true;
                        Text = Text.Substring(0, Text.Length - 1);
                    }
                }
                else if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                {
                    Typing = false;
                    Submitted = true;
                }
                else if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                {
                    Typing = false;
                }
                else if (Raylib.IsMouseButtonDown(MouseButton.Right) || (Raylib.IsKeyDown(KeyboardKey.LeftControl) && Raylib.IsKeyPressed(KeyboardKey.V)))
                {
                    sbyte* clipboardText = Raylib.GetClipboardText();
                    Text = new string(clipboardText);
                }
                else
                {
                    int key = Raylib.GetCharPressed();
                    if (key > 0)
                    {
                        int* utf8size = stackalloc int[1];
                        sbyte* charPressed = Raylib.CodepointToUTF8(key, utf8size);
                        if (*utf8size == 1)
                        {
                            Text += new string(charPressed);
                        }
                    }
                }
            }
        }
    }
}
