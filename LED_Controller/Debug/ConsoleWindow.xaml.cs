using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace LED_Controller.Debug
{
    public partial class ConsoleWindow : Window
    {


        private SerialPort _serialPort;

        //TODO Change to Concurrent list (https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentqueue-1?view=netframework-4.7.2)
        private List<string> bufferStrings = new List<string>();

        public ConsoleWindow(ref SerialPort serialPort)
        {
            InitializeComponent();
            if (serialPort != null)
            {
                _serialPort = serialPort;
                _serialPort.DataReceived += DataReceived;
            }
        }

        private void PopQueue(object sender, EventArgs e)
        {
            lock (bufferStrings)
            {
                foreach (var queueString in bufferStrings)
                {
                    AppendText(queueString);
                }
                bufferStrings.Clear();
            }
        }

        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            lock (bufferStrings)
            {
                if (_serialPort != null)
                {
                    bufferStrings.Add(((SerialPort)sender).ReadLine());
                    //AppendText(((SerialPort) sender).ReadLine());
                }
            }
            
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

        private void ConsoleWindow_OnClosed(object sender, EventArgs e)
        {
            _serialPort = null;
        }
    }
}