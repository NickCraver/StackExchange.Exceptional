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
            // rollupSeconds is 0 so a new file is always generated, for demonstration purposes
            ErrorStore.Setup("Samples.Console", new JSONErrorStore(new ErrorStoreSettings
            {
                Path = "Errors",
                RollupPeriod = null
            }));
            // How to do it with no roll-up
            //ErrorStore.Setup("Samples.Console", new JSONErrorStore(path: "Errors"));

            // Example of a code-only email setup, alternatively this can be in the App.config
            EmailNotifier.Setup(new EmailSettings
            {
                SMTPHost = "localhost", // Use Papercut here for testing: https://github.com/ChangemakerStudios/Papercut
                FromAddress = "exceptions@site.com",
                FromDisplayName = "Bob the Builder",
                ToAddress = "dont.use@thisadress.com"
            });

            // Optional: for logging all unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += ExceptionalHandler;

            // Normally we wouldn't want to .GetAwaiter().GetResult(), but async Main is only on a the latest platforms at the moment
            DisplayExceptionStats().GetAwaiter().GetResult();
            PauseForInput();

            try
            {
                throw new Exception("Just a try/catch test");
            }
            catch (Exception ex)
            {
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

        // Optional, for logging all unhandled exceptions on the way out
        private static void ExceptionalHandler(object sender, UnhandledExceptionEventArgs e)
        {
            // e.ExceptionObject may not be an exception, refer to http://www.ecma-international.org/publications/files/ECMA-ST/ECMA-335.pdf
            // section 10.5, CLS Rule 40 if you're curious on why this check needs to happen
            (e.ExceptionObject as Exception)?.LogNoContext();
        }

        private static async Task DisplayExceptionStats()
        {
            WriteLine(ErrorStore.Default.Name + " for " + ErrorStore.Default.Name);
            var count = await ErrorStore.Default.GetCountAsync().ConfigureAwait(false);
            WriteLine("Exceptions in the log: " + count.ToString());

            var errors = await ErrorStore.Default.GetAllAsync().ConfigureAwait(false);

            if (errors.Count == 0) return;

            var last = errors[0];
            WriteLine($"Latest: {last.Message} on {last.CreationDate.ToString()}");
        }

        private static void PauseForInput()
        {
            WriteLine("Press any key to continue...");
            ReadLine();
        }
    }
}
