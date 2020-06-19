namespace MpptCli
{
   using System;
   using System.IO;
   using System.IO.Ports;
   using System.Text;

   public class MockSerialPortWrapper : ISerialPortWrapper
   {
      string currentBuffer = string.Empty;

      public event SerialDataReceivedEventHandler DataReceived;

      public event SerialErrorReceivedEventHandler ErrorReceived;

      public event SerialPinChangedEventHandler PinChanged;

      public Stream BaseStream => throw new NotImplementedException(nameof(BaseStream));

      public int BaudRate { get; set; }

      public bool BreakState { get; set; }

      public int BytesToRead => currentBuffer.Length;

      public int BytesToWrite => throw new NotImplementedException(nameof(BytesToWrite));

      public bool CDHolding => throw new NotImplementedException(nameof(CDHolding));

      public bool CtsHolding => throw new NotImplementedException(nameof(CtsHolding));

      public int DataBits { get; set; }

      public bool DiscardNull { get; set; }

      public bool DsrHolding => throw new NotImplementedException(nameof(DsrHolding));

      public bool DtrEnable { get; set; }

      public Encoding Encoding { get; set; }

      public Handshake Handshake { get; set; }

      public bool IsOpen { get; private set; }

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

      public void Close() => IsOpen = false;

      public void DiscardInBuffer() => throw new NotImplementedException(nameof(DiscardInBuffer));

      public void DiscardOutBuffer() => throw new NotImplementedException(nameof(DiscardOutBuffer));

      public void Open()
      {
         IsOpen = true;
      }

      public int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException(nameof(Read));

      public int Read(char[] buffer, int offset, int count) => throw new NotImplementedException(nameof(Read));

      public int ReadByte() => throw new NotImplementedException(nameof(ReadByte));

      public int ReadChar() => throw new NotImplementedException(nameof(ReadChar));

      public string ReadExisting()
      {
         var t = currentBuffer;
         currentBuffer = string.Empty;
         return t;
      }

      public string ReadLine() => throw new NotImplementedException(nameof(ReadLine));

      public string ReadTo(string value) => throw new NotImplementedException(nameof(ReadTo));

      public void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException(nameof(Write));

      public void Write(char[] buffer, int offset, int count) => throw new NotImplementedException(nameof(Write));

      public void Write(string text)
      {
         ProcessInput(text);
      }

      public void WriteLine(string text) => throw new NotImplementedException(nameof(WriteLine));

      void ProcessInput(string text)
      {
         var (cmd, value) = ParseCommand(text);

         if (cmd.Equals("ECHO", StringComparison.OrdinalIgnoreCase))
         {
         }
         else if (cmd.Equals("VER?", StringComparison.OrdinalIgnoreCase))
         {
            currentBuffer += "\r\nOK:VER=1.0\r\n";
         }
         else if (cmd.Equals("READALL?", StringComparison.OrdinalIgnoreCase))
         {
            currentBuffer += "\r\nOK:READALL=124,-300,320,18,666,-3,2\r\n";
         }
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
