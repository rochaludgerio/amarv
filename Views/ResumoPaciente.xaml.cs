namespace maui_amarv.Views;

public partial class ResumoPaciente : ContentPage
{
    public ResumoPaciente()
    {
        InitializeComponent();
    }

    private async void Button_Analise_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AnaliseVacinalPage());
    }

    private async void Button_Voltar_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void Button_Menu_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}