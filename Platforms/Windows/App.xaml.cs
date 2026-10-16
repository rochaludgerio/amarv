using Microsoft.UI.Xaml;
using maui_amarv;

namespace maui_amarv.WinUI
{
    public partial class App : MauiWinUIApplication
    {
        public App()
        {
            this.InitializeComponent();
        }

        protected override MauiApp CreateMauiApp()
        {
            return MauiProgram.CreateMauiApp();
        }
    }
}