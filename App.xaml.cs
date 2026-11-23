using System;
using System.Linq;
using System.Windows;

namespace SortingLabApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            if (e.Args.Contains("--test") || e.Args.Contains("-t"))
            {
                try
                {
                    VerificationTests.RunAllTests();
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[ОШИБКА ТЕСТОВ]: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                    Console.ResetColor();
                    Environment.Exit(1);
                }
                return;
            }

            base.OnStartup(e);
        }
    }
}
