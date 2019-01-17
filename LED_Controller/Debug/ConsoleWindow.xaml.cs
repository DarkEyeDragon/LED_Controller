using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace LED_Controller.Debug
{
    public partial class ConsoleWindow : Window
    {
        public enum ConsoleModes
        {
            ReadByte,
            NewLine
        }


        public ConsoleModes Mode { get; set; }

        public List<string> BufferList { get; set; }
        private readonly DispatcherTimer _timer = new DispatcherTimer();

        public ConsoleWindow()
        {
            InitializeComponent();
            BufferList = new List<string>();
            foreach (var consoleMode in System.Enum.GetValues(typeof(ConsoleModes)))
            {
                ComboBoxMode.Items.Add(consoleMode);
            }

            ComboBoxMode.SelectedIndex = 0;
            _timer.Interval = new TimeSpan(0, 0, 0, 0, 80);
            _timer.Tick += PopQueue;
            _timer.Start();
        }

        private void PopQueue(object sender, EventArgs e)
        {
            lock (BufferList)
            {
                foreach (var queueString in BufferList)
                {
                    AppendText(queueString);
                }

                BufferList.Clear();
            }
        }

        private void AppendText(string text)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Output.Inlines.Count > 100)
                {
                    Output.Inlines.Remove(Output.Inlines.FirstInline);
                }

                Output.Inlines.Add(text);
                ScrollViewer.ScrollToBottom();
                //if (Application.Current.MainWindow != null) ;
                //((MainWindow) Application.Current.MainWindow).BufferStatus.Value = Convert.ToInt16(text) * 100 / 64;
            });
        }

        private void ComboBoxMode_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBoxMode.SelectedItem is ConsoleModes modes)
            {
                Mode = modes;
            }
        }
    }
}