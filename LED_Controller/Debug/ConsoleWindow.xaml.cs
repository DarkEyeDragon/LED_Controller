using System;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Windows;

namespace LED_Controller.Debug
{
    public partial class ConsoleWindow : Window
    {
        private SerialPort _serialPort;
        Task task;
        public ConsoleWindow(ref SerialPort serialPort)
        {
            InitializeComponent();
            if (serialPort != null)
            {
                _serialPort = serialPort;
                _serialPort.DataReceived += DataReceived;
            }
        }

        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_serialPort != null)
                AppendText(((SerialPort) sender).ReadLine());
        }

        public void AppendText(string text)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Output.Inlines.Count > 100)
                {
                    Output.Inlines.Remove(Output.Inlines.FirstInline);
                }

                Output.Inlines.Add(text);
                ScrollViewer.ScrollToBottom();
            });
        }
    }
}