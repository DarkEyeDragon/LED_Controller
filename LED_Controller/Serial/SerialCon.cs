using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LED_Controller.Serial
{
    class SerialCon : SerialPort
    {
        public SerialPort SerialPort { get; set; }

        public SerialCon(string portName) : base(portName)
        {
            BaudRate = 115200;
            ReadTimeout = 50500;
            WriteTimeout = 50500;
            SerialPort = this;
        }
        public void Write(byte[] bytes)
        {
            if (SerialPort.IsOpen)
            {
                try
                {
                    Write(bytes, 0, bytes.Length);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
