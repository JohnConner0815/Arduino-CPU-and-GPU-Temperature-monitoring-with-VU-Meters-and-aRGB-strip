using System;
using System.IO;
using System.Windows.Forms;

namespace ArduinoTempMonitor
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                Application.Run(new Form1());
            }
            catch (Exception ex)
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
                File.WriteAllText(logPath, "Fatal Error on Startup:\n\n" + ex.ToString());
                MessageBox.Show("A fatal error occurred. The details have been saved to:\n" + logPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}