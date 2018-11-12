namespace mppt_cli
{
    using System.IO.Ports;

    class Settings
    {
        public static Settings Default { get; } = new Settings
        {
            CommPort = "COM28",
            Parity = Parity.None,
            StopBits = StopBits.One,
            BaudRate = 38400,
            DataBits = 8
        };

        public Parity Parity { get; internal set; }

        public StopBits StopBits { get; internal set; }

        public string CommPort { get; internal set; }

        public int BaudRate { get; internal set; }

        public int DataBits { get; internal set; }
    }
}