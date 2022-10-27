using Microsoft.Extensions.Configuration;
using System;
using StackExchange.Exceptional;
using StackExchange.Exceptional.Stores;
using static System.Console;
using System.Threading.Tasks;

namespace Samples.NetCoreConsole
{
    internal static class Program
    {
        private static async Task Main()
        {
            // Example of setting things up from appsettings.json config
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            var exceptionalSettings = config.GetSection("Exceptional").Get<ExceptionalSettings>();
            Exceptional.Configure(exceptionalSettings);

            // Example of code-only setup, alternatively this can be in the appsettings.json (or any config format) as shown above
            // RollupPeriod is null so a new file is always generated, for demonstration purposes
            //Exceptional.Configure(settings =>
            //{
            //    settings.DefaultStore = new JSONErrorStore(new ErrorStoreSettings
            //    {
            //        ApplicationName = "Samples.ConsoleNetCore",
            //        Path = "Errors",
            //        RollupPeriod = null
            //    });
            //});

            // How to do it with normal roll-up
            //Exceptional.Configure(new ExceptionalSettings() { DefaultStore = new JSONErrorStore("Errors") });

            // Optional: for logging all unhandled exceptions
            Exceptional.ObserveAppDomainUnhandledExceptions();

            await DisplayExceptionStatsAsync();
            PauseForInput();

            try
            {
                throw new Exception("Just a try/catch test");
            }
            catch (Exception ex)
            {
                ex.AddLogData("Example string", DateTime.UtcNow.ToString())
                  .AddLogData("User Id", "You could fetch a user/account Id here, etc.")
                  .AddLogData("Links get linkified", "https://www.google.com");

                // logged, but caught so we don't crash
                ex.LogNoContext();
            }

            await DisplayExceptionStatsAsync();
            PauseForInput();

            WriteLine("This next one will crash the program, but will be logged on the way out...");
            PauseForInput();

            // one not explicitly caught, will be logged by ExceptionHandler
            throw new Exception("I am an exception thrown on exit");
        }

        private static async Task DisplayExceptionStatsAsync()
        {
            var settings = Exceptional.Settings;
            WriteLine(settings.DefaultStore.Name + " for " + settings.DefaultStore.Name);
            var count = await settings.DefaultStore.GetCountAsync();
            WriteLine("Exceptions in the log: " + count.ToString());

            var errors = await settings.DefaultStore.GetAllAsync();

            if (errors.Count == 0) return;

            var last = errors[0];
            WriteLine($"Latest: {last.Message} on {last.CreationDate}");
            foreach (var customData in last.CustomData)
                WriteLine($"    CustomData: '{customData.Key}': '{customData.Value}'");
        }

        private static void PauseForInput()
        {
            WriteLine("Press any key to continue...");
            ReadLine();
        }
    }
}
