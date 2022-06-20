using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Globalization;
using System.Threading;

namespace SPX_Weight
{
    /// <summary>
    /// App.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class App : Application
    {
        Mutex mMutex;

        public static void lnaugeChange(string type)
        {
            CultureInfo culture = new CultureInfo(type);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary()
            {
                Source = new Uri(string.Format("{0}.xaml", type), UriKind.Relative)
            });
        }

        public App()
        {
          
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            bool isNewInstance = false;

            mMutex = new System.Threading.Mutex(true, "SPX_Weight_Mutex", out isNewInstance);            
            if (!isNewInstance)
            {
                MessageBox.Show("Already an instance is running");
                App.Current.Shutdown();                
            }
            else
            {
                mMutex.ReleaseMutex();
            }
        }

    }
}
