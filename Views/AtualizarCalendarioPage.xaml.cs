using maui_amarv.Models;

namespace maui_amarv.Views;

public partial class AtualizarCalendarioPage : ContentPage
{
    private RegraVacinal regraSelecionada;

    public AtualizarCalendarioPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await CarregarRegras();
    }

    private async Task CarregarRegras()
    {
        collection_regras.ItemsSource = await App.Database.ListarRegrasVacinais();
    }

    private async void Button_Salvar_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (!ValidarCampos())
                return;

            RegraVacinal regra = new RegraVacinal
            {
                NomeVacina = entry_nome.Text,
                Fabricante = entry_fabricante.Text,
                TipoVacina = entry_tipo_vacina.Text,
                TipoCalendario = pck_tipo.SelectedItem.ToString(),

                IdadeMinimaMeses = Convert.ToInt32(entry_minima.Text),
                IdadeMaximaMeses = Convert.ToInt32(entry_maxima.Text),
                TotalDoses = Convert.ToInt32(entry_doses.Text),

                IntervaloEntreDoses = entry_intervalo.Text,
                ViaAdministracao = entry_via.Text,
                LocalAplicacao = entry_local.Text,

                Indicacao = editor_indicacao.Text,
                Contraindicacao = editor_contraindicacao.Text,
                EventosAdversos = editor_eventos.Text,
                Observacao = editor_observacao.Text
            };

            await App.Database.SalvarRegraVacinal(regra);

            await DisplayAlert("Sucesso", "Vacina cadastrada com sucesso.", "OK");

            LimparCampos();

            await CarregarRegras();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ops", ex.Message, "OK");
        }
    }

    private async void Button_Editar_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (regraSelecionada == null)
            {
                await DisplayAlert("Atenção", "Selecione uma vacina para editar.", "OK");
                return;
            }

            if (!ValidarCampos())
                return;

            regraSelecionada.NomeVacina = entry_nome.Text;
            regraSelecionada.Fabricante = entry_fabricante.Text;
            regraSelecionada.TipoVacina = entry_tipo_vacina.Text;
            regraSelecionada.TipoCalendario = pck_tipo.SelectedItem.ToString();

            regraSelecionada.IdadeMinimaMeses = Convert.ToInt32(entry_minima.Text);
            regraSelecionada.IdadeMaximaMeses = Convert.ToInt32(entry_maxima.Text);
            regraSelecionada.TotalDoses = Convert.ToInt32(entry_doses.Text);

            regraSelecionada.IntervaloEntreDoses = entry_intervalo.Text;
            regraSelecionada.ViaAdministracao = entry_via.Text;
            regraSelecionada.LocalAplicacao = entry_local.Text;

            regraSelecionada.Indicacao = editor_indicacao.Text;
            regraSelecionada.Contraindicacao = editor_contraindicacao.Text;
            regraSelecionada.EventosAdversos = editor_eventos.Text;
            regraSelecionada.Observacao = editor_observacao.Text;

            await App.Database.AtualizarRegraVacinal(regraSelecionada);

            await DisplayAlert("Sucesso", "Vacina atualizada com sucesso.", "OK");

            LimparCampos();

            regraSelecionada = null;
            collection_regras.SelectedItem = null;

            await CarregarRegras();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ops", ex.Message, "OK");
        }
    }

    private void collection_regras_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        regraSelecionada = e.CurrentSelection.FirstOrDefault() as RegraVacinal;

        if (regraSelecionada == null)
            return;

        PreencherCampos(regraSelecionada);
    }

    private void PreencherCampos(RegraVacinal regra)
    {
        entry_nome.Text = regra.NomeVacina;
        entry_fabricante.Text = regra.Fabricante;
        entry_tipo_vacina.Text = regra.TipoVacina;

        if (regra.TipoCalendario == "SUS")
        {
            pck_tipo.SelectedIndex = 0;
        }
        else if (regra.TipoCalendario == "Privado")
        {
            pck_tipo.SelectedIndex = 1;
        }
        else
        {
            pck_tipo.SelectedIndex = -1;
        }

        entry_minima.Text = regra.IdadeMinimaMeses.ToString();
        entry_maxima.Text = regra.IdadeMaximaMeses.ToString();
        entry_doses.Text = regra.TotalDoses.ToString();

        entry_intervalo.Text = regra.IntervaloEntreDoses;
        entry_via.Text = regra.ViaAdministracao;
        entry_local.Text = regra.LocalAplicacao;

        editor_indicacao.Text = regra.Indicacao;
        editor_contraindicacao.Text = regra.Contraindicacao;
        editor_eventos.Text = regra.EventosAdversos;
        editor_observacao.Text = regra.Observacao;
    }

    private async void Button_Excluir_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (regraSelecionada == null)
            {
                await DisplayAlert("Atenção", "Selecione uma vacina para excluir.", "OK");
                return;
            }

            bool confirmar = await DisplayAlert(
                "Confirmar exclusão",
                "Deseja realmente excluir esta vacina?",
                "Sim",
                "Não");

            if (!confirmar)
                return;

            await App.Database.ExcluirRegraVacinal(regraSelecionada);

            await DisplayAlert("Sucesso", "Vacina excluída com sucesso.", "OK");

            LimparCampos();

            regraSelecionada = null;
            collection_regras.SelectedItem = null;

            await CarregarRegras();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ops", ex.Message, "OK");
        }
    }

    private void Button_Limpar_Clicked(object sender, EventArgs e)
    {
        LimparCampos();

        regraSelecionada = null;
        collection_regras.SelectedItem = null;
    }

    private bool ValidarCampos()
    {
        if (string.IsNullOrWhiteSpace(entry_nome.Text))
        {
            DisplayAlert("Atenção", "Digite o nome da vacina.", "OK");
            return false;
        }

        if (pck_tipo.SelectedItem == null)
        {
            DisplayAlert("Atenção", "Selecione o tipo de calendário.", "OK");
            return false;
        }

        if (string.IsNullOrWhiteSpace(entry_minima.Text))
        {
            DisplayAlert("Atenção", "Digite a idade mínima.", "OK");
            return false;
        }

        if (string.IsNullOrWhiteSpace(entry_maxima.Text))
        {
            DisplayAlert("Atenção", "Digite a idade máxima.", "OK");
            return false;
        }

        if (string.IsNullOrWhiteSpace(entry_doses.Text))
        {
            DisplayAlert("Atenção", "Digite a quantidade de doses.", "OK");
            return false;
        }

        return true;
    }

    private void LimparCampos()
    {
        entry_nome.Text = "";
        entry_fabricante.Text = "";
        entry_tipo_vacina.Text = "";
        pck_tipo.SelectedIndex = -1;

        entry_minima.Text = "";
        entry_maxima.Text = "";
        entry_doses.Text = "";

        entry_intervalo.Text = "";
        entry_via.Text = "";
        entry_local.Text = "";

        editor_indicacao.Text = "";
        editor_contraindicacao.Text = "";
        editor_eventos.Text = "";
        editor_observacao.Text = "";
    }

    private async void Button_Menu_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
    private async void Button_RecarregarCalendario_Clicked(object sender, EventArgs e)
    {
        try
        {
            bool confirmar = await DisplayAlert(
            "Recarregar Calendário",
            "Isso apagará somente as regras vacinais atuais e carregará novamente o Calendário 2026.\n\n" +
            "Pacientes, históricos vacinais e outros dados NÃO serão apagados.\n\n" +
            "As alterações manuais feitas nesta tela também serão substituídas pelas regras iniciais.\n\n" +
            "Deseja continuar?",
            "Sim",
            "Não");

            if (!confirmar)
                return;

            await App.Database.RecarregarRegrasVacinais();

            await CarregarRegras();

            await DisplayAlert(
                "Sucesso",
                "Calendário 2026 recarregado com sucesso.",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erro",
                "Não foi possível recarregar o calendário.\n\n" + ex.Message,
                "OK");
        }
    }
}