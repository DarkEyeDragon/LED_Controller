using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using LED_Controller.Serial;
using Color = System.Drawing.Color;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace LED_Controller
{
    public partial class MainWindow : Window
    {
        private readonly Graphics _gfxScreenshot;
        private readonly int SCREEN_HEIGHT = Screen.PrimaryScreen.Bounds.Height;
        private readonly int SCREEN_WIDTH = Screen.PrimaryScreen.Bounds.Width;
        private readonly BackgroundWorker worker = new BackgroundWorker();
        private Bitmap bmp_mostFrequent = new Bitmap(30, 30);
        private readonly Bitmap bmp_Screenshot;
        private SolidBrush brush;
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

            timer.Tick += dispacherTimer_Tick;
            timer.Interval = new TimeSpan(0, 0, 0, 0, 60);
            bmp_Screenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
                Screen.PrimaryScreen.Bounds.Height,
                PixelFormat.Format32bppArgb);
            _gfxScreenshot = Graphics.FromImage(bmp_Screenshot);
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_completed;
        }

        //Triggers whenever the background worker completes their task.
        private void worker_completed(object sender, RunWorkerCompletedEventArgs e)
        {
            ImagePreview.Source = ConvertFromImage(bmp_Screenshot);
            ImageMostFrequent.Source = ConvertFromImage(bmp_mostFrequent);
            Color color = Color.FromArgb(_mostFrequent);
            ColorMostFrequent.Text = $"({color.R}, {color.G}, {color.B})";
        }

        //Tell the background worker what to do.
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            CaptureScreen();
            Color color = Color.FromArgb(_mostFrequent);
            byte[] colorBytes = {color.R, color.G, color.B};
            SendSerialData(colorBytes);
        }

        //Trigger the worker every timer tick.
        private void dispacherTimer_Tick(object sender, EventArgs e)
        {
            if (!worker.IsBusy) worker.RunWorkerAsync();
        }

        public void CaptureScreen()
        {
            // Take the screenshot from the upper left corner to the right bottom corner.
            _gfxScreenshot.CopyFromScreen(Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y, 0, 0,
                Screen.PrimaryScreen.Bounds.Size, CopyPixelOperation.SourceCopy);


            var total = 0;
            for (var x = 0; x < SCREEN_WIDTH; x += 100)
            for (var y = 0; y < SCREEN_HEIGHT; y += 100)
            {
                //Console.WriteLine(bmp_Screenshot.GetPixel(x, y));
                _colorInts[total] = bmp_Screenshot.GetPixel(x, y).ToArgb();
                total++;
            }

            _mostFrequent = MostFrequent(_colorInts, _colorInts.Length);
            bmp_mostFrequent = new Bitmap(30, 30);
            using (gfx = Graphics.FromImage(bmp_mostFrequent))
            using (brush = new SolidBrush(Color.FromArgb(_mostFrequent)))
            {
                gfx.FillRectangle(brush, 0, 0, 30, 30);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (timer.IsEnabled)
            {
                timer.Stop();
                ComboBoxCom.IsEnabled = true;
                RealTimeButton.Content = "Start Realtime";
            }
            else
            {
                timer.Start();
                ComboBoxCom.IsEnabled = false;
                RealTimeButton.Content = "Stop Realtime";
            }
        }

        //Convert Image/Bitmap to ImageSource
        public ImageSource ConvertFromImage(Image image)
        {
            using (var ms = new MemoryStream())
            {
                var bitmapImage = new BitmapImage();
                image.Save(ms, ImageFormat.Bmp);
                ms.Seek(0, SeekOrigin.Begin);
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();
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
                            b = (byte)s.ReadByte();
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