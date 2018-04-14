using System;
using StackExchange.Exceptional;
using StackExchange.Exceptional.Stores;
using StackExchange.Exceptional.Notifiers;
using static System.Console;
using System.Threading.Tasks;

namespace Samples.Console
{
    public static class Program
    {
        private static void Main()
        {
            // Example of code-only setup, alternatively this can be in the App.config
            // RollupPeriod is null so a new file is always generated, for demonstration purposes
            Exceptional.Configure(settings =>
            {
                settings.DefaultStore = new JSONErrorStore(new ErrorStoreSettings
                {
                    ApplicationName = "Samples.Console",
                    Path = "Errors",
                    RollupPeriod = null
                });

                // Example of a code-only email setup, alternatively this can be in the App.config
                settings.Register(new EmailNotifier(new EmailSettings
                {
                    SMTPHost = "localhost", // Use Papercut here for testing: https://github.com/ChangemakerStudios/Papercut
                    FromAddress = "exceptions@site.com",
                    FromDisplayName = "Bob the Builder",
                    ToAddress = "dont.use@thisadress.com"
                }));
            });

            // How to do it with normal roll-up
            //Exceptional.Configure(new ExceptionalSettings() { DefaultStore = new JSONErrorStore("Errors") });

            // Optional: for logging all unhandled exceptions
            Exceptional.ObserveAppDomainUnhandledExceptions();

            // Normally we wouldn't want to .GetAwaiter().GetResult(), but async Main is only on a the latest platforms at the moment
            DisplayExceptionStats().GetAwaiter().GetResult();
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

            DisplayExceptionStats().GetAwaiter().GetResult();
            PauseForInput();

            WriteLine("This next one will crash the program, but will be logged on the way out...");
            PauseForInput();

            // one not explicitly caught, will be logged by ExceptionHandler
            throw new Exception("I am an exception thrown on exit");
        }

        private static async Task DisplayExceptionStats()
        {
            var settings = Exceptional.Settings;
            WriteLine(settings.DefaultStore.Name + " for " + settings.DefaultStore.Name);
            var count = await settings.DefaultStore.GetCountAsync().ConfigureAwait(false);
            WriteLine("Exceptions in the log: " + count.ToString());

            var errors = await settings.DefaultStore.GetAllAsync().ConfigureAwait(false);

            if (errors.Count == 0) return;

            var last = errors[0];
            WriteLine($"Latest: {last.Message} on {last.CreationDate.ToString()}");
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
