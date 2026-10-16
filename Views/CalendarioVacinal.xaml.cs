namespace maui_amarv.Views;

public partial class CalendarioVacinal : ContentPage
{
    public CalendarioVacinal()
    {
        InitializeComponent();
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