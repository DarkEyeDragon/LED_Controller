using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Ports;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LED_Controller.Debug;
using LED_Controller.Math;
using LED_Controller.Serial;
using LED_Controller.Utils;
using Color = System.Drawing.Color;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace LED_Controller
{
    public partial class MainWindow : Window
    {
        public enum Modes
        {
            MostFrequent,
            Borders,
            Manual,
            None
        }

        public Modes LedMode { get; set; }

        private ColorPreview colorPreview;

        private Algorithm borderAlgo;

        private readonly Graphics _gfxScreenshot;
        private readonly int _screenHeight = Screen.PrimaryScreen.Bounds.Height;
        private readonly int _screenWidth = Screen.PrimaryScreen.Bounds.Width;
        private readonly BackgroundWorker _worker = new BackgroundWorker();
        private Bitmap _bmpCurrentColor = new Bitmap(30, 30);
        private Bitmap _bmpScreenshot;
        private SolidBrush _brush;
        private readonly int[] _colorInts = new int[220];

        private SerialCon _serialPort;

        private Graphics gfx;

        private readonly DispatcherTimer timer = new DispatcherTimer();

        //Console window
        private ConsoleWindow console;

        private int _currentColor;
        Stopwatch stopwatch = new Stopwatch();

        public MainWindow()
        {
            InitializeComponent();
            var ports = SerialPort.GetPortNames();
            ComboBoxCom.Items.Add("--None--");
            ComboBoxCom.Text = "Select COM port";
            foreach (var port in ports)
            {
                ComboBoxCom.Items.Add(port);
            }

            ComboBoxCom.SelectedIndex = 0;
            timer.Tick += dispacherTimer_Tick;
            timer.Interval = new TimeSpan(0);
            _bmpScreenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
                Screen.PrimaryScreen.Bounds.Height,
                PixelFormat.Format32bppArgb);
            _gfxScreenshot = Graphics.FromImage(_bmpScreenshot);
            _worker.DoWork += worker_DoWork;
            _worker.RunWorkerCompleted += worker_completed;

            borderAlgo = new Algorithm(500, 10, _bmpScreenshot);
            colorPreview = new ColorPreview(ImageMostFrequent);

        }

        //Triggers whenever the background worker completes their task.
        private void worker_completed(object sender, RunWorkerCompletedEventArgs e)
        {            
            SetColorPreview();
        }

        //Tell the background worker what to do.
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            CaptureScreen();
            byte[] colorBytes;
            switch (LedMode)
            {
                case Modes.MostFrequent:
                    Color color = Color.FromArgb(_currentColor);
                    colorBytes = new[] {color.R, color.G, color.B};
                    _currentColor = MostFrequent(_colorInts, _colorInts.Length);
                    SendSerialData(colorBytes);
                    break;
                case Modes.Borders:
                    borderAlgo.Bitmap = _bmpScreenshot;
                    List<int> list = borderAlgo.BordersPrecise(22, 19, 5);
                    foreach (var i in list)
                    {
                        Color c = Color.FromArgb(i);
                        SendSerialData(new[] {c.R, c.G, c.B});
                    }

                    _currentColor = list[0];
                    //SendSerialData(colorBytes);
                    break;
                case Modes.None:
                    break;
            }
        }

        //Trigger the worker every timer tick.
        private void dispacherTimer_Tick(object sender, EventArgs e)
        {
            stopwatch.Restart();
            if (!_worker.IsBusy)
            {
                _worker.RunWorkerAsync();
            }
        }

        public void CaptureScreen()
        {
            // Take the screenshot from the upper left corner to the right bottom corner.
            _gfxScreenshot.CopyFromScreen(Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y, 0, 0,
                Screen.PrimaryScreen.Bounds.Size, CopyPixelOperation.SourceCopy);


            var total = 0;
            for (var x = 0; x < _screenWidth; x += 100)
            for (var y = 0; y < _screenHeight; y += 100)
            {
                //Console.WriteLine(bmp_Screenshot.GetPixel(x, y));
                _colorInts[total] = _bmpScreenshot.GetPixel(x, y).ToArgb();
                total++;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (timer.IsEnabled)
            {
                timer.Stop();
                ComboBoxCom.IsEnabled = true;
                RealTimeButton.Content = "Start Most Freq";
                BordersButton.IsEnabled = true;
                LedMode = Modes.None;
                ButtonSetColor.IsEnabled = true;

            }
            else
            {
                LedMode = Modes.MostFrequent;
                timer.Start();
                ComboBoxCom.IsEnabled = false;
                RealTimeButton.Content = "Stop Most Freq";
                BordersButton.IsEnabled = false;
                ButtonSetColor.IsEnabled = false;
                Mode.Text = "Most frequent";
            }
        }

        private void BordersButton_Click(object sender, RoutedEventArgs e)
        {
            if (timer.IsEnabled)
            {
                timer.Stop();
                ComboBoxCom.IsEnabled = true;
                BordersButton.Content = "Start Border modus";
                RealTimeButton.IsEnabled = true;
                LedMode = Modes.None;
                ButtonSetColor.IsEnabled = true;
            }
            else
            {
                LedMode = Modes.Borders;
                timer.Start();
                ComboBoxCom.IsEnabled = false;
                BordersButton.Content = "Stop Border modus";
                RealTimeButton.IsEnabled = false;
                ButtonSetColor.IsEnabled = false;
                Mode.Text = "Border";
            }
        }

        //Calculate the most frequent pixel in the screenshot
        private static int MostFrequent(int[] arr, int n)
        {
            // Insert all elements in hash 
            var hp =
                new Dictionary<int, int>();

            for (var i = 0; i < n; i++)
            {
                var key = arr[i];
                if (hp.ContainsKey(key))
                {
                    var freq = hp[key];
                    freq++;
                    hp[key] = freq;
                }
                else
                {
                    hp.Add(key, 1);
                }
            }

            // find max frequency. 
            int min_count = 0, res = -1;

            foreach (var pair in hp)
                if (min_count < pair.Value)
                {
                    res = pair.Key;
                    min_count = pair.Value;
                }

            return res;
        }

        //Send the data over the serial bus
        public void SendSerialData(byte[] dataBytes)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                stopwatch.Stop();
                System.Diagnostics.Debug.WriteLine(stopwatch.ElapsedMilliseconds);
                _serialPort?.Write(dataBytes);
            }
        }

        private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ComboBoxCom.SelectedItem.ToString().StartsWith("--"))
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.Close();
                    _serialPort.Dispose();
                }

                Status.Text = "Status: Disposed connection.";
            }
            else
            {
                _serialPort = new SerialCon(ComboBoxCom.SelectedItem.ToString());
                _serialPort.Open();
                _serialPort.ReadTimeout = 500;
                _serialPort.WriteTimeout = 24;
                _serialPort.BaudRate = 115200;
                _serialPort.DataReceived += DataReceived;
                Status.Text = $"Status: Connected to {ComboBoxCom.SelectedItem}";
            }
        }

        private byte b;

        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (console != null && console.IsInitialized)
            {
                var s = (SerialPort) sender;
                if (_serialPort == null) return;
                lock (console.BufferList)
                {
                    switch (console.Mode)
                    {
                        case ConsoleWindow.ConsoleModes.ReadByte:
                            b = (byte) s.ReadByte();
                            console.BufferList.Add(b.ToString());
                            break;
                        case ConsoleWindow.ConsoleModes.NewLine:
                            console.BufferList.Add(s.ReadLine());
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        private void ButtonDebug_OnClick(object sender, RoutedEventArgs e)
        {
            if (console == null || !console.IsVisible)
            {
                console = new ConsoleWindow();
                console.Show();
            }
        }

        private void ButtonSetColor_Click(object sender, RoutedEventArgs e)
        {
            var color = Color.FromArgb((byte)SliderRed.Value, (byte)SliderGreen.Value, (byte)SliderBlue.Value);
            _currentColor = color.ToArgb();
            byte[] colorBytes = {color.R, color.G, color.B, color.R, color.G, color.B, color.R, color.G, color.B, color.R, color.G, color.B};
            SetColorPreview();
            SendSerialData(colorBytes);
            Mode.Text = "Manual";
        }

        private void SliderRed_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            LabelRedValue.Content = e.NewValue;
        }

        private void SliderGreen_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            LabelGreenValue.Content = e.NewValue;
        }

        private void SliderBlue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            LabelBlueValue.Content = e.NewValue;
        }

        public void SetColorPreview()
        {
            colorPreview.Color = Color.FromArgb(_currentColor);
            colorPreview.Update();
            ImageMostFrequent.Source = colorPreview.ImageSource;
            Color color = Color.FromArgb(_currentColor);
            ColorMostFrequent.Text = $"({color.R}, {color.G}, {color.B})";
        }
    }
}