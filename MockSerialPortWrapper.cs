using System;
using System.Diagnostics;

namespace mppt_cli
{
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Ports;
    using System.Text;

    public class MockSerialPortWrapper : ISerialPortWrapper
    {
        bool isOpen;
        string currentBuffer = string.Empty;
        bool echo;

        readonly List<string> cmds = new List<string>();
        readonly Dictionary<string, string> settings = new Dictionary<string, string>();

        public event SerialDataReceivedEventHandler DataReceived;

        public event SerialErrorReceivedEventHandler ErrorReceived;

        public event SerialPinChangedEventHandler PinChanged;

        public Stream BaseStream => throw new System.NotImplementedException(nameof(BaseStream));

        public int BaudRate { get; set; }

        public bool BreakState { get; set; }

        public int BytesToRead => currentBuffer.Length;

        public int BytesToWrite => throw new System.NotImplementedException(nameof(BytesToWrite));

        public bool CDHolding => throw new System.NotImplementedException(nameof(CDHolding));

        public bool CtsHolding => throw new System.NotImplementedException(nameof(CtsHolding));

        public int DataBits { get; set; }

        public bool DiscardNull { get; set; }

        public bool DsrHolding => throw new System.NotImplementedException(nameof(DsrHolding));

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

        public void Close() => isOpen = false;

        public void DiscardInBuffer() => throw new System.NotImplementedException(nameof(DiscardInBuffer));

        public void DiscardOutBuffer() => throw new System.NotImplementedException(nameof(DiscardOutBuffer));

        public void Open()
        {
            isOpen = true;
        }

        public int Read(byte[] buffer, int offset, int count) => throw new System.NotImplementedException(nameof(Read));

        public int Read(char[] buffer, int offset, int count) => throw new System.NotImplementedException(nameof(Read));

        public int ReadByte() => throw new System.NotImplementedException(nameof(ReadByte));

        public int ReadChar() => throw new System.NotImplementedException(nameof(ReadChar));

        public string ReadExisting()
        {
            var t = currentBuffer;
            currentBuffer = string.Empty;
            return t;
        }

        public string ReadLine() => throw new System.NotImplementedException(nameof(ReadLine));

        public string ReadTo(string value) => throw new System.NotImplementedException(nameof(ReadTo));

        public void Write(byte[] buffer, int offset, int count) => throw new System.NotImplementedException(nameof(Write));

        public void Write(char[] buffer, int offset, int count) => throw new System.NotImplementedException(nameof(Write));

        public void Write(string text)
        {
            ProcessInput(text);
        }

        public void WriteLine(string text) => throw new System.NotImplementedException(nameof(WriteLine));

        void ProcessInput(string text)
        {
            var (cmd, value) = ParseCommand(text);

            if (cmd.Equals("ECHO", StringComparison.OrdinalIgnoreCase))
            {
                echo = value.Equals("ON", StringComparison.OrdinalIgnoreCase);
            }
            else if (cmd.Equals("VER?", StringComparison.OrdinalIgnoreCase))
            {
                currentBuffer += "\r\nOK:VER=1.0\r\n";
            }
            else if (cmd.Equals("READALL?", StringComparison.OrdinalIgnoreCase))
            {
                currentBuffer += "\r\nOK:READALL=124,-300,320,18,666,-3,2\r\n";
            }

            //settings[cmd] = value;
        }

        static (string Cmd, string Value) ParseCommand(string input)
        {
            var span = input.AsSpan();
            var eqPos = span.IndexOf('=');

            if (eqPos < 0)
            {
                return (input.Trim(), null);
            }

            var cmd = span.Slice(0, eqPos).Trim();
            var value = span.Slice(eqPos + 1).Trim();

            return (cmd.ToString(), value.ToString());
        }
    }
}
