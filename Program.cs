namespace MpptCli
{
   using System.IO.Ports;
   using System.Threading.Tasks;
   using Microsoft.Extensions.DependencyInjection;
   using Serilog;

   static class Program
   {
      static async Task<int> Main(string[] args)
      {
         const string LogFormat = "{Timestamp:yyyy MM dd HH:mm:ss};{Level};{Message};{Exception}";

         Log.Logger = new LoggerConfiguration()
             .WriteTo.RollingFile("mppt-cli.log", /*outputTemplate: LogFormat,*/ restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)
             .WriteTo.Console()
             .CreateLogger();

         var serviceProvider = ConfigureServices();

         var app = new ConsoleApp(serviceProvider.GetRequiredService<ProtocolController>(), Log.Logger);

         return await app.OnExecuteAsync();
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
