namespace MpptCli
{
   using System.IO.Ports;
   using System.Threading.Tasks;
   using Microsoft.Extensions.DependencyInjection;
   using Serilog;

   static class Program
   {
      static async Task<int> Main()
      {
         Log.Logger = new LoggerConfiguration()
             .WriteTo.RollingFile("mppt-cli.log", Serilog.Events.LogEventLevel.Information)
             .WriteTo.Console()
             .CreateLogger();

         var serviceProvider = ConfigureServices();
         var protocolController = await ProtocolController.CreateAsync(serviceProvider.GetRequiredService<ISerialPortWrapper>(), Log.Logger);
         var app = new ConsoleApp(protocolController, Log.Logger);

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
             .AddSingleton<ConsoleApp>()
             .AddSingleton(_ => Log.Logger)
             .BuildServiceProvider();
      }
   }
}
