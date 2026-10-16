using maui_amarv.Data;
using maui_amarv.Views;

namespace maui_amarv
{
    public partial class App : Application
    {
        private static BancoDados? database;

        public static BancoDados Database
        {
            get
            {
                if (database == null)
                {
                    string pasta = Path.Combine(
                        Environment.GetFolderPath(
                            Environment.SpecialFolder.MyDocuments),
                        "AmarV");

                    if (!Directory.Exists(pasta))
                    {
                        Directory.CreateDirectory(pasta);
                    }

                    string caminho = Path.Combine(
                        pasta,
                        "amarv.db3");

                    database = new BancoDados(caminho);
                }

                return database;
            }
        }

        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(
            IActivationState activationState)
        {
            var window =
                new Window(
                    new NavigationPage(
                        new LoginPage()));

            window.Width = 420;
            window.Height = 800;

            return window;
        }
    }
}