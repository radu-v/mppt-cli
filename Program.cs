namespace mppt_cli
{
    using System.IO.Ports;
    using McMaster.Extensions.CommandLineUtils;
    using Microsoft.Extensions.DependencyInjection;
    using Serilog;

    static class Program
    {
        static int Main(string[] args)
        {
            const string LogFormat = "{Timestamp:yyyy MM dd HH:mm:ss};{Level};{Message};{Exception}";

            Log.Logger = new LoggerConfiguration()
                .WriteTo.RollingFile("mppt-cli.log", /*outputTemplate: LogFormat,*/ restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)
                .WriteTo.Console()
                .CreateLogger();

            var serviceProvider = ConfigureServices();

            using (var app = new CommandLineApplication<ConsoleApp>(false))
            {
                app.Conventions
                    .UseDefaultConventions()
                    .UseVersionOptionAttribute()
                    .UseConstructorInjection(serviceProvider);

                try
                {
                    return app.Execute(args);
                }
                catch (CommandParsingException e)
                {
                    app.Error.WriteLine(e.Message);
                    app.ShowHint();
                }

                return 1;
            }
        }

        static ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<SerialPort>()
                #if DEBUG
                .AddSingleton<ISerialPortWrapper, MockSerialPortWrapper>()
                #else
                .AddSingleton<ISerialPortWrapper, SerialPortWrapper>()
                #endif
                .AddSingleton<ProtocolController>()
                .AddSingleton<ConsoleApp>()
                .AddSingleton(_ => Log.Logger)
                .BuildServiceProvider();
        }
    }
}
