namespace MpptCli
{
   using System;
   using System.Diagnostics;
   using System.Globalization;

   public class McmParameter
   {
      double valueReal;
      string valueString;

      public McmParameter()
      { }

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
         ReadOnlyReal = 0x41,
         ReadOnlyString = 0x21,
         ReadWrite = 3,
         ReadWriteString = 0x23,
         ReadWriteReal = 0x43,
         ReadWriteOnOff = 7,
         ReadWriteRange = 11,
         ReadWriteOnOffRange = 15
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
            Enabled = str.Contains("ON", StringComparison.Ordinal);

            message += $"Enabled:'{str}'";
            flag = true;
         }
         else
         {
            message += $"Value:'{str}'. Cmd Parameter with Unexpected Attribute:{Attrib} - should not happen";
         }

         Trace.WriteLine(message);
         return flag;
      }
   }
}