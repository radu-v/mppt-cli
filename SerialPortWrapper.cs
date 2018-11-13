namespace mppt_cli
{
    using System;
    using System.IO;
    using System.IO.Ports;
    using System.Text;

    public class SerialPortWrapper : ISerialPortWrapper, IDisposable
    {
        readonly SerialPort serialPort;

        public SerialPortWrapper()
        {
            serialPort = new SerialPort();
        }

        public SerialPortWrapper(SerialPort serialPort)
        {
            this.serialPort = serialPort;
        }

        public event SerialDataReceivedEventHandler DataReceived
        {
            add { serialPort.DataReceived += value; }
            remove { serialPort.DataReceived -= value; }
        }

        public event SerialErrorReceivedEventHandler ErrorReceived
        {
            add { serialPort.ErrorReceived += value; }
            remove { serialPort.ErrorReceived -= value; }
        }

        public event SerialPinChangedEventHandler PinChanged
        {
            add { serialPort.PinChanged += value; }
            remove { serialPort.PinChanged -= value; }
        }

        public Stream BaseStream => serialPort.BaseStream;

        public int BaudRate { get => serialPort.BaudRate; set => serialPort.BaudRate = value; }

        public bool BreakState { get => serialPort.BreakState; set => serialPort.BreakState = value; }

        public int BytesToRead => serialPort.BytesToRead;

        public int BytesToWrite => serialPort.BytesToWrite;

        public bool CDHolding => serialPort.CDHolding;

        public bool CtsHolding => serialPort.CtsHolding;

        public int DataBits { get => serialPort.DataBits; set => serialPort.DataBits = value; }

        public bool DiscardNull { get => serialPort.DiscardNull; set => serialPort.DiscardNull = value; }

        public bool DsrHolding => serialPort.DsrHolding;

        public bool DtrEnable { get => serialPort.DtrEnable; set => serialPort.DtrEnable = value; }

        public Encoding Encoding { get => serialPort.Encoding; set => serialPort.Encoding = value; }

        public Handshake Handshake { get => serialPort.Handshake; set => serialPort.Handshake = value; }

        public bool IsOpen => serialPort.IsOpen;

        public string NewLine { get => serialPort.NewLine; set => serialPort.NewLine = value; }

        public Parity Parity { get => serialPort.Parity; set => serialPort.Parity = value; }

        public byte ParityReplace { get => serialPort.ParityReplace; set => serialPort.ParityReplace = value; }

        public string PortName { get => serialPort.PortName; set => serialPort.PortName = value; }

        public int ReadBufferSize { get => serialPort.ReadBufferSize; set => serialPort.ReadBufferSize = value; }

        public int ReadTimeout { get => serialPort.ReadTimeout; set => serialPort.ReadTimeout = value; }

        public int ReceivedBytesThreshold { get => serialPort.ReceivedBytesThreshold; set => serialPort.ReceivedBytesThreshold = value; }

        public bool RtsEnable { get => serialPort.RtsEnable; set => serialPort.RtsEnable = value; }

        public StopBits StopBits { get => serialPort.StopBits; set => serialPort.StopBits = value; }

        public int WriteBufferSize { get => serialPort.WriteBufferSize; set => serialPort.WriteBufferSize = value; }

        public int WriteTimeout { get => serialPort.WriteTimeout; set => serialPort.WriteTimeout = value; }

        public void Close() => serialPort.Close();

        public void DiscardInBuffer() => serialPort.DiscardInBuffer();

        public void DiscardOutBuffer() => serialPort.DiscardOutBuffer();

        public void Open() => serialPort.Open();

        public int Read(byte[] buffer, int offset, int count) => serialPort.Read(buffer, offset, count);

        public int Read(char[] buffer, int offset, int count) => serialPort.Read(buffer, offset, count);

        public int ReadByte() => serialPort.ReadByte();

        public int ReadChar() => serialPort.ReadChar();

        public string ReadExisting() => serialPort.ReadExisting();

        public string ReadLine() => serialPort.ReadLine();

        public string ReadTo(string value) => serialPort.ReadTo(value);

        public void Write(byte[] buffer, int offset, int count) => serialPort.Write(buffer, offset, count);

        public void Write(char[] buffer, int offset, int count) => serialPort.Write(buffer, offset, count);

        public void Write(string text) => serialPort.Write(text);

        public void WriteLine(string text) => serialPort.WriteLine(text);

        bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    serialPort?.Close();
                    serialPort?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
