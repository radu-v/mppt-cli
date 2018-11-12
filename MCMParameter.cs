namespace mppt_cli
{
    using System;
    using System.Diagnostics;
    using System.Globalization;

    [Serializable]
    public class McmParameter
    {
        double valueReal;
        string valueString;

        public McmParameter(string command, Attribute attrib)
        {
            Command = command;
            Description = string.Empty;
            Units = string.Empty;
            Attrib = attrib;
            Minimum = 0.0;
            Maximum = 0.0;
            SetValue(0.0);
            SetValue(string.Empty);
        }

        public McmParameter(string command, string description, string units)
        {
            Command = command;
            Description = description;
            Units = units;
            Attrib = Attribute.ReadOnlyReal;
            SetValue(0.0);
        }

        public McmParameter(string command, string description, Attribute attrib, string[] validValuesList)
        {
            Command = command;
            Description = description;
            ValidValuesList = validValuesList;
            Attrib = Attribute.List | Attribute.ReadWrite | attrib;
        }

        public McmParameter(string command, string description, string units, Attribute attrib)
        {
            Trace.Assert((attrib & Attribute.ReadWriteRange) != Attribute.ReadWriteRange);
            Command = command;
            Description = description;
            Units = units;
            Attrib = attrib;
        }

        public McmParameter(string command, string description, string units, double value)
        {
            Command = command;
            Description = description;
            Units = units;
            Attrib = Attribute.ReadWriteReal;
            Minimum = 0.0;
            Maximum = 0.0;
            SetValue(value);
        }

        public McmParameter(string command, string description, string units, string value)
        {
            Command = command;
            Description = description;
            Units = units;
            Attrib = Attribute.ReadWriteString;
            SetValue(value);
        }

        public McmParameter(string command, string description, string units, Attribute attrib, double min, double max)
        {
            Trace.Assert((attrib & Attribute.Range) == Attribute.Range);
            Trace.Assert((attrib & Attribute.List) != Attribute.List);
            Command = command;
            Description = description;
            Units = units;
            Attrib = attrib | Attribute.Real;
            Minimum = min;
            Maximum = max;
        }

        public string GetFormattedValue()
        {
            if (Command == "TIME")
            {
                if (valueString == null)
                {
                    return null;
                }

                var strArray = valueString.Split(',');
                return strArray[2] + "/" + strArray[1] + "/" + strArray[0] + " " + strArray[3] + ":" + strArray[4];
            }

            if ((Attrib & Attribute.String) != Attribute.String && (Attrib & Attribute.Real) == Attribute.Real)
            {
                return valueReal.ToString("f2", CultureInfo.CurrentCulture);
            }

            return valueString;
        }

        public string GetStringValue()
        {
            if ((Attrib & Attribute.Real) == Attribute.Real)
            {
                return valueReal.ToString("f2", CultureInfo.CurrentCulture);
            }

            Trace.Assert((Attrib & Attribute.String) == Attribute.String, "MCMParameter.GetStringValue():Attrib != Attribute.String)");
            return valueString;
        }

        public double GetValue()
        {
            Trace.Assert((Attrib & Attribute.Real) == Attribute.Real, "MCMParameter.SetValue():Attrib != Attribute.Real)");
            return valueReal;
        }

        static bool GotList(string tmp)
        {
            var num = 0;
            var index = -1;
            while (true)
            {
                index = tmp.IndexOf(',', index + 1);
                if (index < 0)
                {
                    break;
                }

                num++;
            }

            return num > 1;
        }

        static bool GotOnOff(string tmp) => tmp.Contains("ON/OFF", StringComparison.OrdinalIgnoreCase);

        static bool GotRange(string tmp) =>
            tmp.IndexOfAny(new[] { '(', ')' }) != -1;

        public void SetValue(double value)
        {
            Trace.Assert((Attrib & Attribute.Real) == Attribute.Real, "MCMParameter.SetValue():Attrib != Attribute.Real)");
            valueReal = value;
        }

        public void SetValue(string value)
        {
            Trace.Assert((Attrib & Attribute.String) == Attribute.String, "MCMParameter.SetValue():Attrib != Attribute.String)");
            if ((Attrib & Attribute.Real) == Attribute.Real)
            {
                var num = double.Parse(value, CultureInfo.CurrentCulture);
                SetValue(num);
            }
            else
            {
                valueString = value;
            }
        }

        public override string ToString() => Command;

        public bool TryParseCommandResponse(string response)
        {
            var flag = false;
            var message = $"Cmd:'{Command}' Decoding Resp:'{response}' ";
            Response = response;
            var strArray = response.Split('=');
            if (strArray.Length != 2)
            {
                Trace.WriteLine(message + "No '=' separator found - should not happen");
                return false;
            }

            var str = strArray[0].ToUpper(CultureInfo.CurrentCulture);
            if (str != Command.ToUpper(CultureInfo.CurrentCulture))
            {
                Trace.WriteLine(message + $"Resp:'{str}' Not For our cmd:'{Command}'- should not happen");
                return false;
            }

            str = strArray[1].ToUpper(CultureInfo.CurrentCulture);
            if ((Attrib & Attribute.OnOff) == Attribute.OnOff)
            {
                flag = true;
                if (str.StartsWith("ON", StringComparison.Ordinal))
                {
                    Enabled = true;
                }
                else if (str.StartsWith("OFF", StringComparison.Ordinal))
                {
                    Enabled = false;
                }
                else
                {
                    flag = false;
                }

                var index = str.IndexOf(',', 1);
                if (index != -1)
                {
                    str = str.Substring(index + 1);
                }
            }

            if ((Attrib & Attribute.Real) == Attribute.Real)
            {
                if (double.TryParse(str, out var num2))
                {
                    SetValue(num2);
                    message += $"Value:{num2}";
                    flag = true;
                }
            }
            else if ((Attrib & Attribute.String) == Attribute.String)
            {
                SetValue(str);
                message += $"Value:'{str}'";
                flag = true;
            }
            else if ((Attrib & Attribute.OnOff) == Attribute.OnOff)
            {
                Enabled = str.Contains("ON", StringComparison.Ordinal) ? true : false;

                message += $"Enabled:'{str}'";
                flag = true;
            }
            else
            {
                message += $"Value:'{str}'. Cmd Parameter with Unexpected Attribute:{Attrib.ToString()} - should not happen";
            }

            Trace.WriteLine(message);
            return flag;
        }

        public bool TryParseRangeResponse(string response)
        {
            var flag = false;
            response = response.ToUpper(CultureInfo.CurrentCulture);
            Response = response;
            Trace.Write($"Cmd:'{Command}' Decoding Range Resp:'{response} ");
            var strArray = response.Split('=');
            if (strArray.Length != 2)
            {
                Trace.WriteLine("No '=' separator found - should not happen");
                return false;
            }

            if (strArray[0] != Command.ToUpper(CultureInfo.CurrentCulture))
            {
                Trace.WriteLine($"Resp:'{strArray[0]}' Not For our cmd:'{Command}'- should not happen");
                return false;
            }

            var tmp = strArray[1];
            if (GotOnOff(tmp))
            {
                Attrib |= Attribute.OnOff;
                Trace.Write("Got ON/OFF. ");
                var index = tmp.IndexOf(',', 1);
                if (index != -1)
                {
                    tmp = tmp.Substring(index + 1);
                }

                flag = true;
            }

            if (GotRange(tmp) || tmp.IndexOf('-', 1) != -1)
            {
                Attrib |= Attribute.Range;
                var index = tmp.IndexOf('(', StringComparison.Ordinal);
                var length = tmp.IndexOf(')', StringComparison.Ordinal);
                if (index == -1 || length == -1)
                {
                    index = tmp.IndexOf('-', 1);
                    Trace.Assert(index != -1);
                    if (index == -1)
                    {
                        return false;
                    }

                    index = -1;
                    length = tmp.Length;
                }

                var str2 = tmp.Substring(index + 1, length - (index + 1));
                index = str2.IndexOf('-', 1);
                if (index != -1)
                {
                    var str3 = str2.Substring(0, index);
                    var str4 = str2.Substring(index + 1);
                    Trace.Write($"Got Range:{str3}-{str4}");
                    Minimum = double.Parse(str3, CultureInfo.CurrentCulture);
                    Maximum = double.Parse(str4, CultureInfo.CurrentCulture);
                }
                else
                {
                    ValidValuesList = str2.Split(',');
                }

                return true;
            }

            if (GotList(tmp))
            {
                flag = true;
            }

            return flag;
        }

        public string Command { get; set; }

        public string Description { get; set; }

        public string Units { get; set; }

        public Attribute Attrib { get; set; }

        public double Minimum { get; set; }

        public double Maximum { get; set; }

        public string[] ValidValuesList { get; set; }

        public string Response { get; set; }

        public bool Enabled { get; set; }

        [Flags]
        public enum Attribute
        {
            None = 0,
            Read = 1,
            Write = 2,
            OnOff = 4,
            Range = 8,
            List = 0x10,
            String = 0x20,
            Real = 0x40,
            ReadOnly = 1,
            ReadOnlyReal = 0x41,
            ReadOnlyString = 0x21,
            WriteOnly = 2,
            ReadWrite = 3,
            ReadWriteString = 0x23,
            ReadWriteReal = 0x43,
            ReadWriteOnOff = 7,
            ReadWriteRange = 11,
            ReadWriteOnOffRange = 15
        }
    }
}