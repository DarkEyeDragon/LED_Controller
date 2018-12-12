using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            None
        }

        public Modes LedMode { get; set; }


        private Algorithm borderAlgo;

        private readonly Graphics _gfxScreenshot;
        private readonly int _screenHeight = Screen.PrimaryScreen.Bounds.Height;
        private readonly int _screenWidth = Screen.PrimaryScreen.Bounds.Width;
        private readonly BackgroundWorker _worker = new BackgroundWorker();
        private Bitmap _bmpMostFrequent = new Bitmap(30, 30);
        private Bitmap _bmpScreenshot;
        private SolidBrush _brush;
        private readonly int[] _colorInts = new int[220];

        private SerialCon _serialPort;

        private Graphics gfx;

        private readonly DispatcherTimer timer = new DispatcherTimer();

        //Console window
        private ConsoleWindow console;

        private int _mostFrequent;

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
            timer.Interval = new TimeSpan(0, 0, 0, 0, 60);
            _bmpScreenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
                Screen.PrimaryScreen.Bounds.Height,
                PixelFormat.Format32bppArgb);
            _gfxScreenshot = Graphics.FromImage(_bmpScreenshot);
            _worker.DoWork += worker_DoWork;
            _worker.RunWorkerCompleted += worker_completed;

            borderAlgo = new Algorithm(500, 10, _bmpScreenshot);
        }

        //Triggers whenever the background worker completes their task.
        private void worker_completed(object sender, RunWorkerCompletedEventArgs e)
        {
            ImagePreview.Source = ConvertFromImage(_bmpScreenshot);
            ImageMostFrequent.Source = ConvertFromImage(_bmpMostFrequent);
            Color color = Color.FromArgb(_mostFrequent);
            ColorMostFrequent.Text = $"({color.R}, {color.G}, {color.B})";
        }

        //Tell the background worker what to do.
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            CaptureScreen();
            byte[] colorBytes;
            switch (LedMode)
            {
                case Modes.MostFrequent:
                    Color color = Color.FromArgb(_mostFrequent);
                    colorBytes = new[] {color.R, color.G, color.B};
                    _mostFrequent = MostFrequent(_colorInts, _colorInts.Length);
                    SendSerialData(colorBytes);
                    break;
                case Modes.Borders:
                    borderAlgo.Bitmap = _bmpScreenshot;
                    int freqColor = borderAlgo.CalculateBorderColors();
                    colorBytes = new[]
                        {Color.FromArgb(freqColor).R, Color.FromArgb(freqColor).G, Color.FromArgb(freqColor).B};

                    _mostFrequent = freqColor;
                    SendSerialData(colorBytes);
                    break;
                case Modes.None:
                    break;
            }
        }

        //Trigger the worker every timer tick.
        private void dispacherTimer_Tick(object sender, EventArgs e)
        {
            if (!_worker.IsBusy)
            {
                if (LedMode == Modes.None)
                {
                    _bmpScreenshot.Dispose();
                }
                else
                {
                    _worker.RunWorkerAsync();
                }
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

            _bmpMostFrequent = new Bitmap(30, 30);
            using (gfx = Graphics.FromImage(_bmpMostFrequent))
            using (_brush = new SolidBrush(Color.FromArgb(_mostFrequent)))
            {
                gfx.FillRectangle(_brush, 0, 0, 30, 30);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (timer.IsEnabled)
            {
                timer.Stop();
                ComboBoxCom.IsEnabled = true;
                RealTimeButton.Content = "Start Realtime";
                BordersButton.IsEnabled = true;
                ImagePreview.Source = null;
                LedMode = Modes.None;
            }
            else
            {
                LedMode = Modes.MostFrequent;
                timer.Start();
                ComboBoxCom.IsEnabled = false;
                RealTimeButton.Content = "Stop Realtime";
                BordersButton.IsEnabled = false;
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
            }
            else
            {
                LedMode = Modes.Borders;
                timer.Start();
                ComboBoxCom.IsEnabled = false;
                BordersButton.Content = "Stop Border modus";
                RealTimeButton.IsEnabled = false;
            }
        }


        private BitmapImage bitmapImage;

        //Convert Image/Bitmap to ImageSource
        public ImageSource ConvertFromImage(Image image)
        {
            using (var ms = new MemoryStream())
            {
                bitmapImage = new BitmapImage();
                image.Save(ms, ImageFormat.Bmp);
                ms.Seek(0, SeekOrigin.Begin);
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();
                ms.Flush();
                return bitmapImage;
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
    }
}