namespace mppt_cli
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Ports;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Threading;
    using Serilog;

    sealed class ProtocolController
    {
        static string key;
        readonly string nL = Environment.NewLine;
        readonly ILogger logger;
        bool responding;

        public ProtocolController(ISerialPortWrapper serialPort, ILogger logger)
        {
            CommPort = serialPort;

            this.logger = logger;

            ConfigureCommPort();
            LoadDefaultParametersList();
            LoadParametersList();
        }

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

        public void Close()
        {
            try
            {
                logger.Verbose($"Protocol.Close:{GetCommPortSettings()}");

                if (CommPort.IsOpen)
                {
                    CommPort.Close();
                }
            }
            catch (Exception exception)
            {
                logger.Verbose($"Could Not Close {GetCommPortSettings()}\n{exception.Message}\nShould not happen.");
            }
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

        static bool FindPredicate_Parameter(McmParameter p) => p.Command == key;

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
                return flag;
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

        public bool IsOpen()
        {
            Trace.Assert(CommPort != null);
            return CommPort.IsOpen;
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
            SaveParametersList();
        }

        void LoadParametersList()
        {
            ParametersList.Clear();
            using (Stream stream = File.Open("data.bin", FileMode.Open))
            {
                var formatter = new BinaryFormatter();
                ParametersList = (List<McmParameter>)formatter.Deserialize(stream);
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
                logger.Verbose($"Could Not Open {GetCommPortSettings()} \n{exception.Message}\nPlease Check Comm Settings.");
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

        public bool ReadParameterRange(McmParameter param)
        {
            Trace.Assert((param.Attrib & McmParameter.Attribute.Range) == McmParameter.Attribute.Range || (param.Attrib & McmParameter.Attribute.List) == McmParameter.Attribute.List);
            SendReadRangeCommand(param.Command);

            var commandResponse = GetCommandResponse(500, out var str);
            if (commandResponse)
            {
                commandResponse = param.TryParseRangeResponse(str);
            }

            responding = commandResponse;

            return commandResponse;
        }

        void SaveParametersList()
        {
            using (Stream stream = File.Open("data.bin", FileMode.Create))
            {
                new BinaryFormatter().Serialize(stream, ParametersList);
            }
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

        void SendReadRangeCommand(string cmd)
        {
            CommPort.ReadExisting();
            logger.Verbose($"Sent ReadRand:{cmd}	");
            cmd += "=?\r";
            CommPort.Write(cmd);
        }

        void SendWriteCommand(McmParameter param)
        {
            CommPort.ReadExisting();
            logger.Verbose($"Sent WriteCmd:{param.Command}={param.GetValue()}	");
            var text = string.Concat(param.Command, "=", param.GetValue(), nL);
            CommPort.Write(text);
        }

        internal void SendWriteCommand(string cmd, string value)
        {
            CommPort.ReadExisting();
            logger.Verbose($"Sent WriteCmd:{cmd}={value}	");
            if (value.Length > 0)
            {
                cmd = cmd + "=" + value + "\r";
            }
            else
            {
                cmd += "\r";
            }

            CommPort.Write(cmd);
        }

        bool WriteParameter(McmParameter param)
        {
            var commandResponse = true;
            var str = string.Empty;
            Trace.Assert((param.Attrib & McmParameter.Attribute.Write) == McmParameter.Attribute.Write);
            if ((param.Attrib & McmParameter.Attribute.ReadWriteOnOff) == McmParameter.Attribute.ReadWriteOnOff)
            {
                str = param.Enabled ? "ON," : "OFF,";
            }

            str += param.GetStringValue();
            SendWriteCommand(param.Command, str);

            if ((param.Attrib & McmParameter.Attribute.Read) == McmParameter.Attribute.Read)
            {
                commandResponse = GetCommandResponse(500, out var str2);
                if (commandResponse)
                {
                    commandResponse = param.TryParseCommandResponse(str2);
                }

                responding = commandResponse;
            }

            return commandResponse;
        }

        bool WriteParameter(McmParameter param, DateTime time)
        {
            var str = time.ToLocalTime().ToString("yyyy,MM,dd,HH,mm");
            Trace.Assert(param.Command == "TIME");
            param.SetValue(str);
            return WriteParameter(param);
        }

        bool WriteParameter(McmParameter param, string value)
        {
            Trace.Assert((param.Attrib & McmParameter.Attribute.Write) == McmParameter.Attribute.Write);
            param.SetValue(value);
            return WriteParameter(param);
        }

        public bool WriteParameter(McmParameter param, bool enabled, string value)
        {
            Trace.Assert((param.Attrib & McmParameter.Attribute.Write) == McmParameter.Attribute.Write);
            Trace.Assert((param.Attrib & McmParameter.Attribute.OnOff) == McmParameter.Attribute.OnOff);
            param.Enabled = enabled;
            param.SetValue(value);

            return WriteParameter(param);
        }

        bool IsConnected
        {
            get
            {
                if (!CommPort.IsOpen)
                {
                    responding = false;
                }

                return responding;
            }
        }

        public ISerialPortWrapper CommPort { get; }

        public List<McmParameter> ParametersList { get; set; } = new List<McmParameter>();
    }
}