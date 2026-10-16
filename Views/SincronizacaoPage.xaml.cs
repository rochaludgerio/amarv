using maui_amarv.Models;

namespace maui_amarv.Views;

public partial class SincronizacaoPage : ContentPage
{
    private UsuarioDoSistema usuarioLogado;

    public SincronizacaoPage(UsuarioDoSistema usuario)
    {
        InitializeComponent();

        usuarioLogado = usuario;
    }

    private async void Button_Sincronizar_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            if (usuarioLogado == null)
            {
                await DisplayAlert(
                    "Erro",
                    "Usuário não identificado.",
                    "OK");

                return;
            }

            await Navigation.PushAsync(
                new SincronizacaoBackup(usuarioLogado));
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erro",
                ex.Message,
                "OK");
        }
    }

    private async void Button_Menu_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erro",
                ex.Message,
                "OK");
        }
    }
}