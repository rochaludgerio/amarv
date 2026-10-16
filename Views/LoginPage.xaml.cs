
using System;
using Microsoft.Maui.Controls;
using maui_amarv.Models;

namespace maui_amarv.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void Button_Entrar_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(entry_login.Text))
            {
                await DisplayAlert(
                    "Atenção",
                    "Digite o login.",
                    "OK");

                return;
            }

            if (string.IsNullOrWhiteSpace(entry_senha.Text))
            {
                await DisplayAlert(
                    "Atenção",
                    "Digite a senha.",
                    "OK");

                return;
            }

            string login = entry_login.Text.Trim();
            string senha = entry_senha.Text.Trim();

            // Inicializar o banco existente.
            await App.Database.InicializarAsync();

            // Buscar usuário.
            UsuarioDoSistema? usuario =
                await App.Database.BuscarUsuario(
                    login,
                    senha);

            if (usuario == null)
            {
                await DisplayAlert(
                    "Erro",
                    "Login ou senha inválidos.",
                    "OK");

                return;
            }

            // Abrir menu usando a mesma instância do banco.
            await Navigation.PushAsync(
                new MenuPrincipal(usuario, App.Database));
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Ops",
                ex.Message,
                "OK");
        }
    }
}
