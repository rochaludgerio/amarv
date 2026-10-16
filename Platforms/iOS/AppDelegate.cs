using Foundation;
using maui_amarv;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace MauiApp1
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp()
        {
            return MauiProgram.CreateMauiApp();
        }
    }
}