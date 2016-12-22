using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Musync
{
    static class Program
    {
        /// <summary>
        /// Mutex for single instance of Musync at a time
        /// </summary>
        private static Mutex instanceMutex;

        /// <summary>
        /// App name
        /// </summary>
        private static string appName = "Musync";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Ensure this is the only instance
            bool firstInstance;
            instanceMutex = new Mutex(true, appName, out firstInstance);
            if (!firstInstance)
            {
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Musync());
        }
    }
}
