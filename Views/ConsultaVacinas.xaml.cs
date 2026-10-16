namespace maui_amarv.Views;

public partial class ConsultaVacinas : ContentPage
{
    public ConsultaVacinas()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        collection_vacinas.ItemsSource = await App.Database.ListarRegrasVacinais();
    }

    private async void Button_SUS_Clicked(object sender, EventArgs e)
    {
        collection_vacinas.ItemsSource = await App.Database.ListarRegrasPorTipo("SUS");
    }

    private async void Button_Privado_Clicked(object sender, EventArgs e)
    {
        collection_vacinas.ItemsSource = await App.Database.ListarRegrasPorTipo("Privado");
    }

    private async void Button_Todos_Clicked(object sender, EventArgs e)
    {
        collection_vacinas.ItemsSource = await App.Database.ListarRegrasVacinais();
    }

    private async void Button_Menu_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}