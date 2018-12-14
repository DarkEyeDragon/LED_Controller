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
            StopBits = StopBits.Two;
            ReadTimeout = 500;
            WriteTimeout = 500;
            SerialPort = this;
        }
        public void Write(byte[] bytes)
        {
            if (SerialPort.IsOpen)
            {
                Write(bytes, 0, bytes.Length);
            }
        }
    }
}
