namespace mppt_cli
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

        async Task<int> OnExecuteAsync()
        {
            logger.Verbose($"Polling controller data from serial port {protocolController.CommPort.PortName}. Press any key to exit.");

            TextWriter csvFile = default;
            try
            {
                csvFile = File.CreateText($"mppt-cli_{DateTime.Now:yyyyMMdd-HHmmss}.csv");
                await csvFile.WriteLineAsync("Timestamp;Level;Volts;Amps;Load Amps;Battery;Load;Temperature;AHr Run Timer;Alarm").ConfigureAwait(false);

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
                            var voltage = (numArray[1] / 10.0d).ToString("f1", CultureInfo.CurrentCulture) + " V";
                            var current = (numArray[3] / 10.0d).ToString("f1", CultureInfo.CurrentCulture) + " A";
                            var load = (numArray[4] / 10.0d).ToString("f1", CultureInfo.CurrentCulture) + " A";
                            var ahBattery = numArray[6].ToString("f1", CultureInfo.CurrentCulture) + " AHr";
                            var ahLoad = numArray[5].ToString("f1", CultureInfo.CurrentCulture) + " AHr";
                            var temperature = numArray[0].ToString("f0", CultureInfo.CurrentCulture) + " \x00b0C";

                            var sb = new StringBuilder($"Volts: {voltage}; Amps: {current}; Load Amps: {load}; Battery: {ahBattery}; Load: {ahLoad}; Temperature: {temperature}");
                            await csvFile.WriteAsync($"{voltage};{current};{load};{ahBattery};{ahLoad};{temperature};").ConfigureAwait(false);

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

                            sb.AppendLine();

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

                double.TryParse(strArray[3]?.Trim() ?? "0", out values[0]);
                double.TryParse(strArray[0]?.Trim() ?? "0", out values[1]);
                double.TryParse(strArray[4]?.Trim() ?? "0", out values[2]);
                double.TryParse(strArray[1]?.Trim() ?? "0", out values[3]);
                double.TryParse(strArray[2]?.Trim() ?? "0", out values[4]);
                double.TryParse(strArray[5]?.Trim() ?? "0", out values[5]);
                double.TryParse(strArray[6]?.Trim() ?? "0", out values[6]);
            }

            return flag;
        }
    }
}