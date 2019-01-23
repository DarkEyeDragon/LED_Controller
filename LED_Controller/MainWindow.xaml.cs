using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Threading;
using LED_Controller.Debug;
using LED_Controller.Math;
using LED_Controller.Serial;
using LED_Controller.Utils;

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

        private readonly Bitmap _bmpScreenshot;
        private readonly int[] _colorInts = new int[220];

        private readonly Graphics _gfxScreenshot;
        private readonly int _screenHeight = Screen.PrimaryScreen.Bounds.Height;
        private readonly int _screenWidth = Screen.PrimaryScreen.Bounds.Width;
        private readonly BackgroundWorker _worker = new BackgroundWorker();

        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private Bitmap _bmpCurrentColor = new Bitmap(30, 30);

        private int _currentColor;

        private SerialCon _serialPort;

        private byte _dataReceivedByte;

        private readonly Algorithm _borderAlgo;

        private readonly ColorPreview _colorPreview;

        //Console window
        private ConsoleWindow _console;
        private readonly Stopwatch _stopwatch = new Stopwatch();

        public MainWindow()
        {
            InitializeComponent();
            var ports = SerialPort.GetPortNames();
            ComboBoxCom.Items.Add("--None--");
            ComboBoxCom.Text = "Select COM port";
            foreach (var port in ports) ComboBoxCom.Items.Add(port);

            ComboBoxCom.SelectedIndex = 0;
            _timer.Tick += dispacherTimer_Tick;
            _timer.Interval = new TimeSpan(0);
            _bmpScreenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
                Screen.PrimaryScreen.Bounds.Height,
                PixelFormat.Format32bppArgb);
            _gfxScreenshot = Graphics.FromImage(_bmpScreenshot);
            _worker.DoWork += worker_DoWork;
            _worker.RunWorkerCompleted += worker_completed;

            _borderAlgo = new Algorithm(5000, 10, _bmpScreenshot);
            _colorPreview = new ColorPreview(ImageMostFrequent);
        }

        public Modes LedMode { get; set; }

        //Triggers whenever the background worker completes their task.
        private void worker_completed(object sender, RunWorkerCompletedEventArgs e)
        {
            SetColorPreview();
        }

        //Tell the background worker what to do.
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            CaptureScreen();
            switch (LedMode)
            {
                case Modes.MostFrequent:
                    var color = Color.FromArgb(_currentColor);
                    var colorBytes = new[] {color.R, color.G, color.B};
                    _currentColor = MostFrequent(_colorInts, _colorInts.Length);
                    SendSerialData(colorBytes);
                    break;
                case Modes.Borders:
                    _borderAlgo.Bitmap = _bmpScreenshot;
                    var list = _borderAlgo.BordersPrecise(22, 19, 80);
                    foreach (var i in list)
                    {
                        var c = Color.FromArgb(i);
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
            _stopwatch.Restart();
            if (!_worker.IsBusy) _worker.RunWorkerAsync();
        }

        private void CaptureScreen()
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
            if (_timer.IsEnabled)
            {
                _timer.Stop();
                ComboBoxCom.IsEnabled = true;
                RealTimeButton.Content = "Most Frequent";
                BordersButton.IsEnabled = true;
                LedMode = Modes.None;
                ButtonSetColor.IsEnabled = true;
            }
            else
            {
                LedMode = Modes.MostFrequent;
                _timer.Start();
                ComboBoxCom.IsEnabled = false;
                RealTimeButton.Content = "Stop";
                BordersButton.IsEnabled = false;
                ButtonSetColor.IsEnabled = false;
                Mode.Text = "Most frequent";
            }
        }

        private void BordersButton_Click(object sender, RoutedEventArgs e)
        {
            if (_timer.IsEnabled)
            {
                _timer.Stop();
                ComboBoxCom.IsEnabled = true;
                BordersButton.Content = "Border";
                RealTimeButton.IsEnabled = true;
                LedMode = Modes.None;
                ButtonSetColor.IsEnabled = true;
            }
            else
            {
                LedMode = Modes.Borders;
                _timer.Start();
                ComboBoxCom.IsEnabled = false;
                BordersButton.Content = "Stop";
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
            int minCount = 0, res = -1;

            foreach (var pair in hp)
                if (minCount < pair.Value)
                {
                    res = pair.Key;
                    minCount = pair.Value;
                }

            return res;
        }

        //Send the data over the serial bus
        public void SendSerialData(byte[] dataBytes)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _stopwatch.Stop();
                _serialPort?.Write(dataBytes);
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_console != null && _console.IsInitialized)
            {
                var s = (SerialPort) sender;
                if (_serialPort == null) return;
                lock (_console.BufferList)
                {
                    switch (_console.Mode)
                    {
                        case ConsoleWindow.ConsoleModes.ReadByte:
                            _dataReceivedByte = (byte) s.ReadByte();
                            _console.BufferList.Add(_dataReceivedByte.ToString());
                            break;
                        case ConsoleWindow.ConsoleModes.NewLine:
                            _console.BufferList.Add(s.ReadLine());
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        private void ButtonDebug_OnClick(object sender, RoutedEventArgs e)
        {
            if (_console == null || !_console.IsVisible)
            {
                _console = new ConsoleWindow();
                _console.Show();
            }
        }

        private void ButtonSetColor_Click(object sender, RoutedEventArgs e)
        {
            var color = Color.FromArgb((byte) SliderRed.Value, (byte) SliderGreen.Value, (byte) SliderBlue.Value);
            _currentColor = color.ToArgb();
            byte[] colorBytes =
            {
                color.R, color.G, color.B, color.R, color.G, color.B, color.R, color.G, color.B, color.R, color.G,
                color.B
            };
            SetColorPreview();
            SendSerialData(colorBytes);
            Mode.Text = "Manual";
            ColorPicker.SelectedColor = System.Windows.Media.Color.FromRgb((byte)SliderRed.Value, (byte)SliderGreen.Value, (byte)SliderBlue.Value);
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
            _colorPreview.Color = Color.FromArgb(_currentColor);
            _colorPreview.Update();
            ImageMostFrequent.Source = _colorPreview.ImageSource;
            var color = Color.FromArgb(_currentColor);
            ColorMostFrequent.Text = $"({color.R}, {color.G}, {color.B})";
        }

        private void ColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<System.Windows.Media.Color?> e)
        {
            System.Windows.Media.Color mColor = (System.Windows.Media.Color)e.NewValue;
            SliderRed.Value = mColor.R;
            SliderGreen.Value = mColor.G;
            SliderBlue.Value = mColor.B;
        }
    }
}