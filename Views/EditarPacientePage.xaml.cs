using maui_amarv.Models;

namespace maui_amarv.Views;

public partial class EditarPacientePage : ContentPage
{
    private readonly Paciente pacienteAtual;
    private Pessoa? pessoaAtual;

    public EditarPacientePage(Paciente paciente)
    {
        InitializeComponent();

        pacienteAtual = paciente
            ?? throw new ArgumentNullException(nameof(paciente));

        // Os dados pessoais serão carregados em OnAppearing.
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            var pessoas = await App.Database.Listar<Pessoa>();

            pessoaAtual = pessoas.FirstOrDefault(
                p => p.Id == pacienteAtual.Id);

            if (pessoaAtual == null)
            {
                await DisplayAlert(
                    "Erro",
                    "Não foi encontrado o cadastro pessoal deste paciente.",
                    "OK");

                await Navigation.PopAsync();
                return;
            }

            entry_nome.Text = pessoaAtual.Nome;

            entry_idade_anos.Text =
                pacienteAtual.IdadeAnos.ToString();

            entry_idade_meses.Text =
                pacienteAtual.IdadeMesesAdicionais.ToString();

            entry_condicao.Text =
                pacienteAtual.CondicaoClinica;

            chk_profissional_saude.IsChecked =
                pacienteAtual.ProfissionalSaude;

            chk_indigena.IsChecked =
                pacienteAtual.Indigena;

            chk_acamado.IsChecked =
                pacienteAtual.AcamadoOuInstitucionalizado;

            chk_gestante.IsChecked =
                pacienteAtual.Gestante;

            entry_semanas_gestacao.Text =
                pacienteAtual.SemanasGestacao.ToString();

            layout_gestante.IsVisible =
                pacienteAtual.Gestante;

            chk_lactante.IsChecked =
                pacienteAtual.Lactante;

            chk_crianca_menor_6_meses.IsChecked =
                pacienteAtual.CriancaAmamentadaMenor6Meses;

            layout_lactante.IsVisible =
                pacienteAtual.Lactante;
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erro",
                $"Não foi possível carregar o paciente: {ex.Message}",
                "OK");
        }
    }

    private void chk_gestante_CheckedChanged(
        object sender,
        CheckedChangedEventArgs e)
    {
        layout_gestante.IsVisible = e.Value;

        if (!e.Value)
            entry_semanas_gestacao.Text = "0";
    }

    private void chk_lactante_CheckedChanged(
        object sender,
        CheckedChangedEventArgs e)
    {
        layout_lactante.IsVisible = e.Value;

        if (!e.Value)
            chk_crianca_menor_6_meses.IsChecked = false;
    }

    private async void Button_Salvar_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            if (pessoaAtual == null)
            {
                await DisplayAlert(
                    "Erro",
                    "Os dados pessoais do paciente não foram carregados.",
                    "OK");

                return;
            }

            if (string.IsNullOrWhiteSpace(entry_nome.Text))
            {
                await DisplayAlert(
                    "Atenção",
                    "Digite o nome do paciente.",
                    "OK");

                return;
            }

            if (!int.TryParse(entry_idade_anos.Text, out int idadeAnos)
                || idadeAnos < 0)
            {
                await DisplayAlert(
                    "Atenção",
                    "Informe uma idade válida em anos.",
                    "OK");

                return;
            }

            int idadeMeses = 0;

            if (!string.IsNullOrWhiteSpace(entry_idade_meses.Text)
                && (!int.TryParse(
                        entry_idade_meses.Text,
                        out idadeMeses)
                    || idadeMeses < 0
                    || idadeMeses > 11))
            {
                await DisplayAlert(
                    "Atenção",
                    "Os meses adicionais devem estar entre 0 e 11.",
                    "OK");

                return;
            }

            int semanasGestacao = 0;

            if (chk_gestante.IsChecked)
            {
                if (!int.TryParse(
                        entry_semanas_gestacao.Text,
                        out semanasGestacao)
                    || semanasGestacao < 0
                    || semanasGestacao > 42)
                {
                    await DisplayAlert(
                        "Atenção",
                        "As semanas de gestação devem estar entre 0 e 42.",
                        "OK");

                    return;
                }
            }

            // Dados pessoais: tabela Pessoa.
            pessoaAtual.Nome = entry_nome.Text.Trim();

            // Dados específicos: tabela Paciente.
            pacienteAtual.IdadeAnos = idadeAnos;
            pacienteAtual.IdadeMesesAdicionais = idadeMeses;
            pacienteAtual.CondicaoClinica = entry_condicao.Text;
            pacienteAtual.ProfissionalSaude =
                chk_profissional_saude.IsChecked;
            pacienteAtual.Indigena = chk_indigena.IsChecked;
            pacienteAtual.AcamadoOuInstitucionalizado =
                chk_acamado.IsChecked;

            pacienteAtual.Gestante = chk_gestante.IsChecked;
            pacienteAtual.SemanasGestacao = semanasGestacao;
            pacienteAtual.Lactante = chk_lactante.IsChecked;
            pacienteAtual.CriancaAmamentadaMenor6Meses =
                chk_crianca_menor_6_meses.IsChecked;

            // Salvar as duas entidades relacionadas pelo mesmo Id.
            await App.Database.SalvarPaciente(
                pacienteAtual,
                pessoaAtual);

            await DisplayAlert(
                "Sucesso",
                "Cadastro atualizado.",
                "OK");

            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erro",
                $"Não foi possível salvar o cadastro: {ex.Message}",
                "OK");
        }
    }

    private async void Button_Voltar_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }
}