namespace MpptCli
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Serilog;

    class ConsoleApp
    {
        readonly ProtocolController protocolController;
        readonly ILogger logger;

        public ConsoleApp(ProtocolController protocolController, ILogger logger)
        {
            this.protocolController = protocolController;
            this.logger = logger;
        }

        public async Task<int> OnExecuteAsync()
        {
            logger.Verbose($"Polling controller data from serial port {protocolController.CommPort.PortName}. Press any key to exit.");

            TextWriter csvFile = default;
            try
            {
                csvFile = File.CreateText($"mppt-cli_{DateTime.Now:yyyyMMdd-HHmmss}.csv");
                await csvFile.WriteLineAsync("Timestamp;Volts;Amps;Load Amps;Battery (AHr);Load (AHr);Temperature (C);AHr Run Timer;Alarm").ConfigureAwait(false);

                protocolController.Open();

                while (!Console.KeyAvailable)
                {
                    if (!protocolController.CheckConnected())
                    {
                        logger.Warning($"Serial port {protocolController.CommPort.PortName} is closed");
                    }
                    else
                    {
                        if (ReadAll(out var numArray))
                        {
                            var voltage = (numArray[1] / 10.0d).ToString("f1", CultureInfo.CurrentCulture);
                            var current = (numArray[3] / 10.0d).ToString("f1", CultureInfo.CurrentCulture);
                            var load = (numArray[4] / 10.0d).ToString("f1", CultureInfo.CurrentCulture);
                            var ahBattery = numArray[6].ToString("f1", CultureInfo.CurrentCulture);
                            var ahLoad = numArray[5].ToString("f1", CultureInfo.CurrentCulture);
                            var temperature = numArray[0].ToString("f0", CultureInfo.CurrentCulture);

                            var sb = new StringBuilder($"Volts: {voltage} V; Amps: {current} A; Load Amps: {load} A; Battery: {ahBattery} AHr; Load: {ahLoad} AHr; Temperature: {temperature} \x00b0C");
                            await csvFile.WriteAsync($"{DateTime.Now:G};{voltage};{current};{load};{ahBattery};{ahLoad};{temperature};").ConfigureAwait(false);

                            var ahtParam = protocolController.GetParameter("AHT");
                            if (protocolController.ReadParameter(ahtParam))
                            {
                                var formattedValue = ahtParam.GetFormattedValue();
                                sb.Append("; AHr Run Timer: ").Append(formattedValue);
                                await csvFile.WriteAsync(formattedValue).ConfigureAwait(false);
                            }
                            else
                            {
                                await csvFile.WriteAsync(";").ConfigureAwait(false);
                            }

                            var alarmParameter = protocolController.GetParameter("ALM");
                            if (protocolController.ReadParameter(alarmParameter))
                            {
                                var stringValue = alarmParameter.GetStringValue();
                                sb.Append("; Alarm: ").Append(stringValue);
                                await csvFile.WriteAsync(stringValue).ConfigureAwait(false);
                            }
                            else
                            {
                                await csvFile.WriteAsync(";").ConfigureAwait(false);
                            }

                            await csvFile.WriteLineAsync().ConfigureAwait(false);

                            await csvFile.FlushAsync().ConfigureAwait(false);

                            logger.Information(sb.ToString());
                        }
                    }

                    await Task.Delay(1000).ConfigureAwait(false);
                }
            }
            finally
            {
                csvFile?.Close();
            }

            return 0;
        }

        bool ReadAll(out double[] values)
        {
            values = null;
            var param = protocolController.GetParameter("ReadAll");
            param.SetValue(string.Empty);
            var flag = protocolController.ReadParameter(param);

            if (flag)
            {
                var strArray = param.GetStringValue().Split(',');
                values = new double[strArray.Length];
                Trace.Assert(strArray.Length == 7);

                if (!double.TryParse(strArray[3]?.Trim() ?? "0", out values[0])) { values[0] = 0; }
                if (!double.TryParse(strArray[0]?.Trim() ?? "0", out values[1])) { values[1] = 0; }
                if (!double.TryParse(strArray[4]?.Trim() ?? "0", out values[2])) { values[2] = 0; }
                if (!double.TryParse(strArray[1]?.Trim() ?? "0", out values[3])) { values[3] = 0; }
                if (!double.TryParse(strArray[2]?.Trim() ?? "0", out values[4])) { values[4] = 0; }
                if (!double.TryParse(strArray[5]?.Trim() ?? "0", out values[5])) { values[5] = 0; }
                if (!double.TryParse(strArray[6]?.Trim() ?? "0", out values[6])) { values[6] = 0; }
            }

            return flag;
        }
    }
}