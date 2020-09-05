using System;
using System.Windows.Forms;

namespace TCSynchronize
{
    static class Program
    {
        private static CustomApplicationContext customApplicationContext;

        [STAThread]
        static void Main()
        {
            Logger.initialize("log.txt");

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                customApplicationContext = new CustomApplicationContext();

                Application.Run(customApplicationContext);
            }
            catch (Exception e)
            {
                Logger.log(Logger.Level.Info, e);
            }
        }
    }
}
