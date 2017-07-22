using System;
using StackExchange.Exceptional;
using StackExchange.Exceptional.Email;
using StackExchange.Exceptional.Stores;

namespace Samples.Console
{
    public static class Program
    {
        private static void Main()
        {
            // Example of code-only setup, alteratively this can be in the App.config
            // rollupSeconds is 0 so a new file is always generated, for demonstration purposes
            ErrorStore.Setup("Samples.Console", new JSONErrorStore(new ErrorStoreSettings
            {
                Path = "Errors",
                RollupPeriod = null
            }));
            // How to do it with no rollup
            //ErrorStore.Setup("Samples.Console", new JSONErrorStore(path: "Errors"));

            // Example of a code-only email setup, alteratively this can be in the App.config

            ErrorEmailer.Setup(new EmailSettings
            {
                FromAddress = "exceptions@site.com",
                FromDisplayName = "Bob the Builder",
                ToAddress = "dont.use@thisadress.com"
            });

            // Optional: for logging all unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += ExceptionalHandler;

            DisplayExceptionStats();
            PauseForInput();

            try
            {
                throw new Exception("Just a try/catch test");
            }
            catch (Exception ex)
            {
                // logged, but caught so we don't crash
                ex.LogWithoutContext();
            }

            DisplayExceptionStats();
            PauseForInput();

            System.Console.WriteLine("This next one will crash the program, but will be logged on the way out...");
            PauseForInput();

            // one not explicitly caught, will be logged by ExceptionHandler
            throw new Exception("I am an exception thrown on exit");
        }

        // Optional, for logging all unhanled exceptions on the way out
        private static void ExceptionalHandler(object sender, UnhandledExceptionEventArgs e)
        {
            // e.ExceptionObject may not be an exception, refer to http://www.ecma-international.org/publications/files/ECMA-ST/ECMA-335.pdf
            // section 10.5, CLS Rule 40 if you're curious on why this check needs to happen
            var exception = e.ExceptionObject as Exception;

            exception?.LogWithoutContext();
        }

        private static void DisplayExceptionStats()
        {
            System.Console.WriteLine(ErrorStore.Default.Name + " for " + ErrorStore.Default.Name);
            var count = ErrorStore.Default.GetCount();
            System.Console.WriteLine("Exceptions in the log: " + count.ToString());

            var errors = ErrorStore.Default.GetAll();

            if (errors.Count == 0) return;

            var last = errors[0];
            System.Console.WriteLine($"Latest: {last.Message} on {last.CreationDate.ToString()}");
        }

        private static void PauseForInput()
        {
            System.Console.WriteLine("Press any key to continue...");
            System.Console.ReadLine();
        }
    }
}
