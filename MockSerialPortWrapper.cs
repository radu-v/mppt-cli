namespace mppt_cli
{
    using System.IO;
    using System.IO.Ports;
    using System.Text;

    public class MockSerialPortWrapper : ISerialPortWrapper
    {
        bool isOpen;


        public event SerialDataReceivedEventHandler DataReceived;

        public event SerialErrorReceivedEventHandler ErrorReceived;

        public event SerialPinChangedEventHandler PinChanged;

        public Stream BaseStream => throw new System.NotImplementedException();

        public int BaudRate { get; set; }

        public bool BreakState { get; set; }

        public int BytesToRead => throw new System.NotImplementedException();

        public int BytesToWrite => throw new System.NotImplementedException();

        public bool CDHolding => throw new System.NotImplementedException();

        public bool CtsHolding => throw new System.NotImplementedException();

        public int DataBits { get; set; }

        public bool DiscardNull { get; set; }

        public bool DsrHolding => throw new System.NotImplementedException();

        public bool DtrEnable { get; set; }

        public Encoding Encoding { get; set; }

        public Handshake Handshake { get; set; }

        public bool IsOpen => isOpen;

        public string NewLine { get; set; }

        public Parity Parity { get; set; }

        public byte ParityReplace { get; set; }

        public string PortName { get; set; }

        public int ReadBufferSize { get; set; }

        public int ReadTimeout { get; set; }

        public int ReceivedBytesThreshold { get; set; }

        public bool RtsEnable { get; set; }

        public StopBits StopBits { get; set; }

        public int WriteBufferSize { get; set; }

        public int WriteTimeout { get; set; }

        public void Close() => throw new System.NotImplementedException();

        public void DiscardInBuffer() => throw new System.NotImplementedException();

        public void DiscardOutBuffer() => throw new System.NotImplementedException();

        public void Open()
        {
            isOpen = true;
        }

        public int Read(byte[] buffer, int offset, int count) => throw new System.NotImplementedException();

        public int Read(char[] buffer, int offset, int count) => throw new System.NotImplementedException();

        public int ReadByte() => throw new System.NotImplementedException();

        public int ReadChar() => throw new System.NotImplementedException();

        public string ReadExisting() => throw new System.NotImplementedException();

        public string ReadLine() => throw new System.NotImplementedException();

        public string ReadTo(string value) => throw new System.NotImplementedException();

        public void Write(byte[] buffer, int offset, int count) => throw new System.NotImplementedException();

        public void Write(char[] buffer, int offset, int count) => throw new System.NotImplementedException();

        public void Write(string text) => throw new System.NotImplementedException();

        public void WriteLine(string text) => throw new System.NotImplementedException();
    }
}
