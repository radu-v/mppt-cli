using System.Linq;
using System.Threading.Tasks;

namespace MpptCli
{
   using System;
   using System.Collections.Generic;
   using System.Diagnostics;
   using System.IO;
   using System.IO.Ports;
   using System.Text.Json;
   using System.Threading;
   using Serilog;

   public sealed class ProtocolController
   {
      static string key;
      readonly ILogger logger;
      bool responding;

      private ProtocolController(ISerialPortWrapper serialPort, ILogger logger)
      {
         CommPort = serialPort;

         this.logger = logger;

         ParametersList = new List<McmParameter>();
      }

      public static async Task<ProtocolController> CreateAsync(ISerialPortWrapper serialPort, ILogger logger)
      {
         var protocolController = new ProtocolController(serialPort, logger);
         protocolController.ConfigureCommPort();
         protocolController.LoadDefaultParametersList();
         await protocolController.LoadParametersListAsync();
         await protocolController.SaveParametersListAsync();

         return protocolController;
      }

      public ISerialPortWrapper CommPort { get; }

      public List<McmParameter> ParametersList { get; set; }

      static bool FindPredicate_Parameter(McmParameter p) => p.Command == key;

      public bool CheckConnected()
      {
         responding = false;

         if (CommPort.IsOpen)
         {
            SendCommand("ECHO=OFF");
            var param = GetParameter("VER");
            param.SetValue(string.Empty);
            ReadParameter(param);
         }

         return responding;
      }

      void ConfigureCommPort()
      {
         Trace.Assert(CommPort != null);
         logger.Verbose(nameof(ConfigureCommPort));
         var parity = Settings.Default.Parity;
         var bits = (StopBits)Enum.Parse(typeof(StopBits), Settings.Default.StopBits.ToString());
         CommPort.PortName = Settings.Default.CommPort;
         CommPort.BaudRate = Settings.Default.BaudRate;
         CommPort.Parity = parity;
         CommPort.DataBits = Settings.Default.DataBits;
         CommPort.StopBits = bits;
         CommPort.WriteBufferSize = 0x800;
         CommPort.ReadBufferSize = 0x2800;
      }

      bool GetCommandResponse(int timeout, out string response)
      {
         var flag = false;
         response = string.Empty;
         var stopwatch = Stopwatch.StartNew();
         while (stopwatch.ElapsedMilliseconds < timeout + 250)
         {
            if (CommPort.BytesToRead > 0)
            {
               response += CommPort.ReadExisting();
               if (response.StartsWith("\r\n", StringComparison.Ordinal))
               {
                  response = response.TrimStart('\r', '\n');
               }

               if (!response.Contains("\r\n", StringComparison.Ordinal))
               {
                  continue;
               }

               flag = true;
               break;
            }

            Thread.CurrentThread.Join(200);
         }

         if (!flag)
         {
            logger.Verbose($" *** Cmd Response: Timeout:{stopwatch.ElapsedMilliseconds}msec resp:'{response}'");
            responding = false;
            return false;
         }

         response = response.Replace("OK:", string.Empty, StringComparison.OrdinalIgnoreCase)
             .Replace("\r\n", string.Empty, StringComparison.OrdinalIgnoreCase);
         return true;
      }

      string GetCommPortSettings() => GetCommPortSettings(true);

      string GetCommPortSettings(bool detailedInfo)
      {
         var str = CommPort.PortName;
         if (detailedInfo)
         {
            str = string.Concat(str, ":", CommPort.BaudRate, ",", CommPort.DataBits, ",", Enum.GetName(typeof(Parity), CommPort.Parity), ",", (int)CommPort.StopBits);
         }

         return str;
      }

      public McmParameter GetParameter(string key)
      {
         ProtocolController.key = key;
         var parameter = ParametersList.Find(FindPredicate_Parameter);
         Trace.Assert(parameter != null);
         return parameter;
      }

      void LoadDefaultParametersList()
      {
         ParametersList.Add(new McmParameter("BATV", "Battery Voltage", "Volts", McmParameter.Attribute.ReadOnlyReal));
         ParametersList.Add(new McmParameter("OUTC", "Solar Panel Current", "Amps", McmParameter.Attribute.ReadOnlyReal));
         ParametersList.Add(new McmParameter("LODC", "Load Current", "Amps", McmParameter.Attribute.ReadOnlyReal));
         ParametersList.Add(new McmParameter("IBAT", "Battery Current", "Amps", McmParameter.Attribute.ReadOnlyReal));
         ParametersList.Add(new McmParameter("TEMP", "System Temperature", "\x00b0C", McmParameter.Attribute.ReadOnlyReal));
         ParametersList.Add(new McmParameter("LAH", "Load Capacity", "AmpHour", McmParameter.Attribute.ReadOnlyReal));
         ParametersList.Add(new McmParameter("BATH", "Battery Capacity", "AmpHour", McmParameter.Attribute.ReadOnlyReal));
         ParametersList.Add(new McmParameter("AHT", "AmpHour Running Time", "HH:MM:SS", McmParameter.Attribute.ReadOnlyString));
         ParametersList.Add(new McmParameter("ALM", "Alarm Status", string.Empty, McmParameter.Attribute.ReadOnlyString));
         ParametersList.Add(new McmParameter("VER", "Firmware Version", string.Empty, McmParameter.Attribute.ReadOnlyString));
         ParametersList.Add(new McmParameter("HVER", "Hardware Version", string.Empty, McmParameter.Attribute.ReadOnlyString));
         ParametersList.Add(new McmParameter("MODEL", "Model Number", string.Empty, McmParameter.Attribute.ReadOnlyString));
         ParametersList.Add(new McmParameter("ReadAll", "Vbatt, Ibatt, Iload, Temp, Iout, AHload,AHbatt", string.Empty, McmParameter.Attribute.ReadOnlyString));
         ParametersList.Add(new McmParameter("RESET", "Reset MCM", string.Empty, McmParameter.Attribute.Write));
         ParametersList.Add(new McmParameter("RTC", "Clears AmpHour Timer and Batt/Load Capacity", string.Empty, McmParameter.Attribute.Write));
         ParametersList.Add(new McmParameter("ECHO", "Echo On/Off", string.Empty, McmParameter.Attribute.Write));
         ParametersList.Add(new McmParameter("RLC", "Remote Load Control ", McmParameter.Attribute.String | McmParameter.Attribute.OnOff, new[] { "LVD", "DDC", "LSC" }));
         ParametersList.Add(new McmParameter("LVD", "Low Voltage Disconnect Threshold", "Volts", McmParameter.Attribute.ReadWriteRange, 8.0, 53.0));
         ParametersList.Add(new McmParameter("DDC", "Dawn to Dusk Load Disconnect Time", "Hours", McmParameter.Attribute.ReadWriteRange, 1.0, 16.0));
         ParametersList.Add(new McmParameter("LSC", "Direct Control Of Load", string.Empty, McmParameter.Attribute.ReadWriteOnOff));
         ParametersList.Add(new McmParameter("FBR", "Flat Battery Recovery Mode Enable/Disable", string.Empty, McmParameter.Attribute.ReadWriteReal | McmParameter.Attribute.OnOff));
         ParametersList.Add(new McmParameter("OVP", "Output Voltage Programming Enable/Disable ", "V", McmParameter.Attribute.ReadWriteOnOffRange, 8.0, 58.0));
         ParametersList.Add(new McmParameter("OVT", "Over Temperature Alarm Threshold", "\x00b0C", McmParameter.Attribute.ReadWriteOnOffRange, 35.0, 75.0));
         ParametersList.Add(new McmParameter("BUV", "Battery Undervoltage Alarm Threshold", "Volts", McmParameter.Attribute.ReadWriteOnOffRange, 10.0, 50.0));
         ParametersList.Add(new McmParameter("BAH", "Battery AmpHour Alarm Threshold", "AmpHours", McmParameter.Attribute.ReadWriteOnOffRange, -50.0, -1000.0));
         ParametersList.Add(new McmParameter("TIME", "Set/Read System Time (YYYY,MM,DD,HH,MM)", string.Empty, McmParameter.Attribute.ReadWriteString));
      }

      async Task LoadParametersListAsync()
      {
         if (!File.Exists("parameterList.json")) return;

         List<McmParameter> loadedParameterList;

         await using (Stream stream = File.Open("parameterList.json", FileMode.Open))
         {
            loadedParameterList = await JsonSerializer.DeserializeAsync<List<McmParameter>>(stream);
         }

         foreach (var param in loadedParameterList)
         {
            var existing = ParametersList.FindIndex(p => p.Command == param.Command);

            if (existing < 0)
            {
               ParametersList.Add(param);
            }
            else
            {
               ParametersList[existing] = param;
            }
         }
      }

      public bool Open()
      {
         Trace.Assert(CommPort != null);
         try
         {
            if (CommPort.IsOpen)
            {
               CommPort.Close();
            }

            logger.Verbose($"Protocol.Open:{GetCommPortSettings()}");
            Trace.Assert(!CommPort.IsOpen);
            CommPort.Open();
            logger.Verbose($"- {(CommPort.IsOpen ? "OPEN" : "NOT OPENED")}");
            SendCommand("ECHO=OFF");
         }
         catch (Exception exception)
         {
            logger.Error($"Could Not Open {GetCommPortSettings()} \n{exception.Message}\nPlease Check Comm Settings.");
         }

         return CommPort.IsOpen;
      }

      public bool ReadParameter(McmParameter param)
      {
         Trace.Assert((param.Attrib & McmParameter.Attribute.Read) == McmParameter.Attribute.Read);
         SendReadCommand(param.Command);

         var commandResponse = GetCommandResponse(500, out var str);
         if (commandResponse)
         {
            commandResponse = param.TryParseCommandResponse(str);
         }

         responding = commandResponse;

         return commandResponse;
      }

      Task SaveParametersListAsync()
      {
         using Stream stream = File.Open("parameterList.json", FileMode.Create);

         return JsonSerializer.SerializeAsync(stream, ParametersList, new JsonSerializerOptions() { WriteIndented = true });
      }

      void SendCommand(string cmd)
      {
         CommPort.ReadExisting();
         logger.Verbose($"Sent Cmd:{cmd}");
         cmd += "\r";
         CommPort.Write(cmd);
      }

      void SendReadCommand(string cmd)
      {
         CommPort.ReadExisting();
         logger.Verbose($"Sent ReadCmd:{cmd}	");
         cmd += "?\r";
         CommPort.Write(cmd);
      }
   }
}