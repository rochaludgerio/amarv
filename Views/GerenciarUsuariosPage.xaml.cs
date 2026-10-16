using maui_amarv.Models;

namespace maui_amarv.Views;

public partial class GerenciarUsuariosPage : ContentPage
{
    private UsuarioDoSistema usuarioSelecionado;

    public GerenciarUsuariosPage()
    {
        InitializeComponent();

        ConfigurarToqueAtivo();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await CarregarUsuarios();
    }

    private void ConfigurarToqueAtivo()
    {
        var tap = new TapGestureRecognizer();

        tap.Tapped += (s, e) =>
        {
            chk_ativo.IsChecked = !chk_ativo.IsChecked;
        };

        linha_ativo.GestureRecognizers.Add(tap);
    }

    private async Task CarregarUsuarios()
    {
        collection_usuarios.ItemsSource =
            await App.Database.ListarUsuarios();
    }

    private async void Button_Salvar_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (!ValidarCampos())
                return;

            UsuarioDoSistema usuario = new UsuarioDoSistema
            {
                Nome = entry_nome.Text,
                Login = entry_login.Text,
                Senha = entry_senha.Text,
                Perfil = pck_perfil.SelectedItem?.ToString(),
                Status = chk_ativo.IsChecked ? "Ativo" : "Inativo",
                Ativo = chk_ativo.IsChecked
            };

            await App.Database.SalvarUsuario(usuario);

            await DisplayAlert(
                "Sucesso",
                "Usuário cadastrado com sucesso.",
                "OK");

            LimparCampos();

            await CarregarUsuarios();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ops", ex.Message, "OK");
        }
    }

    private async void Button_Atualizar_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (usuarioSelecionado == null)
            {
                await DisplayAlert(
                    "Atenção",
                    "Clique em EDITAR no usuário da lista primeiro.",
                    "OK");

                return;
            }

            if (!ValidarCampos())
                return;

            usuarioSelecionado.Nome = entry_nome.Text;
            usuarioSelecionado.Login = entry_login.Text;
            usuarioSelecionado.Senha = entry_senha.Text;
            usuarioSelecionado.Perfil = pck_perfil.SelectedItem?.ToString();
            usuarioSelecionado.Status =
                chk_ativo.IsChecked ? "Ativo" : "Inativo";
            usuarioSelecionado.Ativo = chk_ativo.IsChecked;

            await App.Database.AtualizarUsuario(usuarioSelecionado);

            await DisplayAlert(
                "Sucesso",
                "Usuário atualizado com sucesso.",
                "OK");

            LimparCampos();

            usuarioSelecionado = null;

            await CarregarUsuarios();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ops", ex.Message, "OK");
        }
    }

    private void Button_Editar_Item_Clicked(object sender, EventArgs e)
    {
        Button botao = sender as Button;

        if (botao == null)
            return;

        UsuarioDoSistema usuario =
            botao.CommandParameter as UsuarioDoSistema;

        if (usuario == null)
            return;

        usuarioSelecionado = usuario;

        PreencherCampos(usuarioSelecionado);
    }

    private async void Button_Ativar_Item_Clicked(object sender, EventArgs e)
    {
        try
        {
            Button botao = sender as Button;

            if (botao == null)
                return;

            UsuarioDoSistema usuario =
                botao.CommandParameter as UsuarioDoSistema;

            if (usuario == null)
                return;

            usuario.Ativo = true;
            usuario.Status = "Ativo";

            await App.Database.AtualizarUsuario(usuario);

            await DisplayAlert(
                "Sucesso",
                "Usuário ativado com sucesso.",
                "OK");

            await CarregarUsuarios();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ops", ex.Message, "OK");
        }
    }

    private async void Button_Desativar_Item_Clicked(object sender, EventArgs e)
    {
        try
        {
            Button botao = sender as Button;

            if (botao == null)
                return;

            UsuarioDoSistema usuario =
                botao.CommandParameter as UsuarioDoSistema;

            if (usuario == null)
                return;

            bool confirmar = await DisplayAlert(
                "Confirmar",
                "Deseja desativar este usuário?",
                "Sim",
                "Não");

            if (!confirmar)
                return;

            usuario.Ativo = false;
            usuario.Status = "Inativo";

            await App.Database.AtualizarUsuario(usuario);

            await DisplayAlert(
                "Sucesso",
                "Usuário desativado com sucesso.",
                "OK");

            await CarregarUsuarios();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ops", ex.Message, "OK");
        }
    }

    private async void Button_Deletar_Item_Clicked(object sender, EventArgs e)
    {
        try
        {
            Button botao = sender as Button;

            if (botao == null)
                return;

            UsuarioDoSistema usuario =
                botao.CommandParameter as UsuarioDoSistema;

            if (usuario == null)
                return;

            bool confirmar = await DisplayAlert(
                "Confirmar exclusão",
                "Deseja realmente deletar o usuário "
                + usuario.Nome
                + "?",
                "Sim",
                "Não");

            if (!confirmar)
                return;

            await App.Database.ExcluirUsuario(usuario);

            await DisplayAlert(
                "Sucesso",
                "Usuário deletado com sucesso.",
                "OK");

            if (usuarioSelecionado != null &&
                usuarioSelecionado.Id == usuario.Id)
            {
                usuarioSelecionado = null;

                LimparCampos();
            }

            await CarregarUsuarios();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ops", ex.Message, "OK");
        }
    }

    private void PreencherCampos(UsuarioDoSistema usuario)
    {
        entry_nome.Text = usuario.Nome;
        entry_login.Text = usuario.Login;
        entry_senha.Text = usuario.Senha;

        if (usuario.Perfil == "Administrador")
        {
            pck_perfil.SelectedIndex = 0;
        }
        else if (usuario.Perfil == "Usuario")
        {
            pck_perfil.SelectedIndex = 1;
        }
        else
        {
            pck_perfil.SelectedIndex = -1;
        }

        chk_ativo.IsChecked = usuario.Ativo;
    }

    private bool ValidarCampos()
    {
        if (string.IsNullOrWhiteSpace(entry_nome.Text))
        {
            DisplayAlert(
                "Atenção",
                "Digite o nome do usuário.",
                "OK");

            return false;
        }

        if (string.IsNullOrWhiteSpace(entry_login.Text))
        {
            DisplayAlert(
                "Atenção",
                "Digite o login.",
                "OK");

            return false;
        }

        if (string.IsNullOrWhiteSpace(entry_senha.Text))
        {
            DisplayAlert(
                "Atenção",
                "Digite a senha.",
                "OK");

            return false;
        }

        if (pck_perfil.SelectedItem == null)
        {
            DisplayAlert(
                "Atenção",
                "Selecione o perfil.",
                "OK");

            return false;
        }

        return true;
    }

    private void Button_Limpar_Clicked(object sender, EventArgs e)
    {
        LimparCampos();

        usuarioSelecionado = null;
    }

    private void LimparCampos()
    {
        entry_nome.Text = "";
        entry_login.Text = "";
        entry_senha.Text = "";

        pck_perfil.SelectedIndex = -1;

        chk_ativo.IsChecked = true;
    }

    private async void Button_Menu_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}