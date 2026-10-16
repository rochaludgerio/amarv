
using Microsoft.Maui.Controls;
using maui_amarv.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace maui_amarv.Views
{
    public partial class CadastroPaciente : ContentPage
    {
        private Pessoa? pessoaSelecionada;
        private List<Pessoa> pessoas = new();
        private Paciente? pacienteSelecionado;
        private bool atualizandoCondicoes;
        private bool modoEdicao;

        public CadastroPaciente()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CarregarPessoas();
        }

        // =====================================================
        // CARREGAR PESSOAS
        // =====================================================

        private async Task CarregarPessoas()
        {
            try
            {
                pessoas = await App.Database.Listar<Pessoa>();

                pck_pessoa.ItemsSource = null;
                pck_pessoa.ItemsSource = pessoas
                    .OrderBy(p => p.Nome)
                    .ToList();

                pck_pessoa.ItemDisplayBinding =
                    new Binding(nameof(Pessoa.Nome));

                if (pessoaSelecionada != null)
                {
                    pessoaSelecionada = pessoas.FirstOrDefault(
                        p => p.Id == pessoaSelecionada.Id);

                    pck_pessoa.SelectedItem = pessoaSelecionada;
                }

                if (pessoas.Count == 0)
                {
                    pessoaSelecionada = null;
                    LimparCamposPessoa();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Erro",
                    $"Não foi possível carregar as pessoas.\n{ex.Message}",
                    "OK");
            }
        }

        // =====================================================
        // SELECIONAR PESSOA
        // =====================================================

        private async void Pck_Pessoa_SelectedIndexChanged(
            object sender, EventArgs e)
        {
            pessoaSelecionada = pck_pessoa.SelectedItem as Pessoa;

            if (pessoaSelecionada == null)
            {
                LimparCamposPessoa();
                return;
            }

            entry_nome.Text = pessoaSelecionada.Nome ?? string.Empty;
            entry_cpf.Text = pessoaSelecionada.Cpf ?? string.Empty;
            entry_nome_mae.Text =
                pessoaSelecionada.Nome_da_Mae ?? string.Empty;
            entry_telefone.Text =
                pessoaSelecionada.Telefone ?? string.Empty;
            entry_endereco.Text =
                pessoaSelecionada.Endereco ?? string.Empty;

            // Ao selecionar outra pessoa, sai do modo de edição.
            pacienteSelecionado = null;
            modoEdicao = false;

            try
            {
                var paciente = await App.Database.BuscarPaciente(
                    pessoaSelecionada.Id);

                if (paciente != null)
                {
                    await CarregarDadosPaciente(paciente);
                }
                else
                {
                    await LimparCamposPaciente();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Erro",
                    $"Não foi possível consultar o paciente.\n{ex.Message}",
                    "OK");
            }
        }

        private void LimparCamposPessoa()
        {
            entry_nome.Text = string.Empty;
            entry_cpf.Text = string.Empty;
            entry_nome_mae.Text = string.Empty;
            entry_telefone.Text = string.Empty;
            entry_endereco.Text = string.Empty;
        }

        // =====================================================
        // CARREGAR DADOS DO PACIENTE PARA EDIÇÃO
        // =====================================================

        private async Task CarregarDadosPaciente(Paciente paciente)
        {
            pacienteSelecionado = paciente;

            pck_sexo.SelectedItem = paciente.Sexo;
            entry_cartao_sus.Text = paciente.CartaoSUS;
            entry_idade_anos.Text = paciente.IdadeAnos.ToString();
            entry_idade_meses.Text =
                paciente.IdadeMesesAdicionais.ToString();

            chk_gestante.IsChecked = paciente.Gestante;
            entry_semanas_gestacao.Text =
                paciente.SemanasGestacao > 0
                    ? paciente.SemanasGestacao.ToString()
                    : string.Empty;

            chk_lactante.IsChecked = paciente.Lactante;
            chk_lactante_bebe.IsChecked =
                paciente.CriancaAmamentadaMenor6Meses;
            chk_profissional_saude.IsChecked =
                paciente.ProfissionalSaude;
            chk_indigena.IsChecked = paciente.Indigena;
            chk_acamado.IsChecked =
                paciente.AcamadoOuInstitucionalizado;
            chk_imunossuprimido.IsChecked =
                paciente.Imunossuprimido;
            chk_hiv.IsChecked = paciente.HIV;
            chk_quimioterapia.IsChecked = paciente.Quimioterapia;
            chk_transplante.IsChecked = paciente.Transplantado;
            chk_corticoide.IsChecked =
                paciente.UsoCorticoideOuImunobiologico;
            chk_alergia.IsChecked = paciente.AlergiaGrave;
            chk_febre.IsChecked = paciente.FebreOuDoencaAguda;

            chk_nenhuma.IsChecked =
                paciente.CondicaoClinica == "Nenhuma";

            entry_semanas_gestacao.IsEnabled = paciente.Gestante;

            await Task.CompletedTask;
        }

        // =====================================================
        // BOTÃO EDITAR
        // =====================================================

        private async void Button_EditarPaciente_Clicked(
            object sender, EventArgs e)
        {
            if (pessoaSelecionada == null)
            {
                await DisplayAlert(
                    "Atenção",
                    "Selecione uma pessoa cadastrada.",
                    "OK");
                return;
            }

            try
            {
                var paciente = await App.Database.BuscarPaciente(
                    pessoaSelecionada.Id);

                if (paciente == null)
                {
                    await DisplayAlert(
                        "Atenção",
                        "Esta pessoa ainda não possui cadastro de paciente. " +
                        "Utilize o botão de cadastro.",
                        "OK");
                    return;
                }

                await CarregarDadosPaciente(paciente);
                modoEdicao = true;

                await DisplayAlert(
                    "Editar paciente",
                    "Altere os dados e clique em Salvar.",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Erro",
                    $"Não foi possível iniciar a edição.\n{ex.Message}",
                    "OK");
            }
        }

        // =====================================================
        // CONDIÇÕES CLÍNICAS
        // =====================================================

        private void Condicao_CheckedChanged(
            object sender, CheckedChangedEventArgs e)
        {
            if (atualizandoCondicoes)
                return;

            atualizandoCondicoes = true;

            try
            {
                if (sender == chk_nenhuma && e.Value)
                {
                    chk_gestante.IsChecked = false;
                    chk_lactante.IsChecked = false;
                    chk_lactante_bebe.IsChecked = false;
                    chk_profissional_saude.IsChecked = false;
                    chk_indigena.IsChecked = false;
                    chk_acamado.IsChecked = false;
                    chk_imunossuprimido.IsChecked = false;
                    chk_hiv.IsChecked = false;
                    chk_quimioterapia.IsChecked = false;
                    chk_transplante.IsChecked = false;
                    chk_corticoide.IsChecked = false;
                    chk_alergia.IsChecked = false;
                    chk_febre.IsChecked = false;

                    entry_semanas_gestacao.Text = string.Empty;
                }
                else if (sender != chk_nenhuma && e.Value)
                {
                    chk_nenhuma.IsChecked = false;
                }

                bool gestante = chk_gestante.IsChecked;
                entry_semanas_gestacao.IsEnabled = gestante;

                if (!gestante)
                    entry_semanas_gestacao.Text = string.Empty;
            }
            finally
            {
                atualizandoCondicoes = false;
            }
        }

        // =====================================================
        // CADASTRAR OU EDITAR PACIENTE
        // =====================================================

        private async void Button_Clicked(
            object sender, EventArgs e)
        {
            try
            {
                if (pessoaSelecionada == null)
                {
                    await DisplayAlert(
                        "Atenção",
                        "Selecione uma pessoa cadastrada.",
                        "OK");
                    return;
                }

                string cpf = new string(
                    (pessoaSelecionada.Cpf ?? string.Empty)
                    .Where(char.IsDigit)
                    .ToArray());

                if (string.IsNullOrWhiteSpace(pessoaSelecionada.Nome))
                {
                    await DisplayAlert(
                        "Atenção",
                        "O nome da pessoa é obrigatório.",
                        "OK");
                    return;
                }

                if (cpf.Length != 11)
                {
                    await DisplayAlert(
                        "Atenção",
                        "O CPF deve conter 11 dígitos.",
                        "OK");
                    return;
                }

                if (pck_sexo.SelectedItem == null)
                {
                    await DisplayAlert(
                        "Atenção",
                        "Selecione o sexo do paciente.",
                        "OK");
                    return;
                }

                if (!int.TryParse(entry_idade_anos.Text, out int idadeAnos)
                    || idadeAnos < 0 || idadeAnos > 130)
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
                            entry_idade_meses.Text, out idadeMeses)
                        || idadeMeses < 0 || idadeMeses > 11))
                {
                    await DisplayAlert(
                        "Atenção",
                        "Os meses adicionais devem estar entre 0 e 11.",
                        "OK");
                    return;
                }

                int semanasGestacao = 0;

                if (chk_gestante.IsChecked
                    && (!int.TryParse(
                            entry_semanas_gestacao.Text,
                            out semanasGestacao)
                        || semanasGestacao < 1
                        || semanasGestacao > 45))
                {
                    await DisplayAlert(
                        "Atenção",
                        "Informe as semanas de gestação, de 1 a 45.",
                        "OK");
                    return;
                }

                var condicoes = new List<string>();

                if (chk_gestante.IsChecked) condicoes.Add("Gestante");
                if (chk_lactante.IsChecked) condicoes.Add("Lactante");
                if (chk_lactante_bebe.IsChecked)
                    condicoes.Add("Lactante com bebê menor de 6 meses");
                if (chk_profissional_saude.IsChecked)
                    condicoes.Add("Profissional de saúde");
                if (chk_indigena.IsChecked) condicoes.Add("Pessoa indígena");
                if (chk_acamado.IsChecked)
                    condicoes.Add("Acamado/institucionalizado");
                if (chk_imunossuprimido.IsChecked)
                    condicoes.Add("Imunossuprimido/imunodeficiência");
                if (chk_hiv.IsChecked) condicoes.Add("HIV");
                if (chk_quimioterapia.IsChecked)
                    condicoes.Add("Quimioterapia");
                if (chk_transplante.IsChecked)
                    condicoes.Add("Transplantado");
                if (chk_corticoide.IsChecked)
                    condicoes.Add("Uso de corticoide ou imunobiológico");
                if (chk_alergia.IsChecked)
                    condicoes.Add("Alergia grave/anafilaxia");
                if (chk_febre.IsChecked)
                    condicoes.Add("Febre, infecção ou doença aguda");
                if (chk_nenhuma.IsChecked)
                    condicoes.Add("Nenhuma");

                var paciente = new Paciente
                {
                    Id = pessoaSelecionada.Id,
                    DataNascimento = DateTime.Today
                        .AddYears(-idadeAnos)
                        .AddMonths(-idadeMeses),
                    Sexo = pck_sexo.SelectedItem.ToString()
                        ?? string.Empty,
                    CartaoSUS =
                        entry_cartao_sus.Text?.Trim() ?? string.Empty,
                    IdadeAnos = idadeAnos,
                    IdadeMesesAdicionais = idadeMeses,
                    CondicaoClinica = string.Join("; ", condicoes),
                    Gestante = chk_gestante.IsChecked,
                    SemanasGestacao = semanasGestacao,
                    Lactante = chk_lactante.IsChecked,
                    CriancaAmamentadaMenor6Meses =
                        chk_lactante_bebe.IsChecked,
                    ProfissionalSaude =
                        chk_profissional_saude.IsChecked,
                    Indigena = chk_indigena.IsChecked,
                    AcamadoOuInstitucionalizado =
                        chk_acamado.IsChecked,
                    Imunossuprimido = chk_imunossuprimido.IsChecked,
                    HIV = chk_hiv.IsChecked,
                    Quimioterapia = chk_quimioterapia.IsChecked,
                    Transplantado = chk_transplante.IsChecked,
                    UsoCorticoideOuImunobiologico =
                        chk_corticoide.IsChecked,
                    AlergiaGrave = chk_alergia.IsChecked,
                    FebreOuDoencaAguda = chk_febre.IsChecked
                };

                int idPaciente;

                if (modoEdicao && pacienteSelecionado != null)
                {
                    paciente.Id = pacienteSelecionado.Id;

                    idPaciente = await App.Database.EditarPaciente(
                        paciente, pessoaSelecionada);
                }
                else
                {
                    // Não cria duplicidade para uma pessoa que já
                    // possui registro na tabela Paciente.
                    var existente = await App.Database.BuscarPaciente(
                        pessoaSelecionada.Id);

                    if (existente != null)
                    {
                        await DisplayAlert(
                            "Atenção",
                            "Esta pessoa já possui cadastro de paciente. " +
                            "Clique em Editar Paciente para alterar os dados.",
                            "OK");
                        return;
                    }

                    idPaciente = await App.Database.SalvarPaciente(
                        paciente, pessoaSelecionada);
                }

                if (idPaciente <= 0)
                {
                    await DisplayAlert(
                        "Erro",
                        "Não foi possível salvar o paciente.",
                        "OK");
                    return;
                }

                modoEdicao = false;
                pacienteSelecionado = null;

                await DisplayAlert(
                    "Sucesso",
                    "Cadastro do paciente salvo com sucesso.",
                    "OK");

                await Navigation.PushAsync(
                    new HistoricoVacinalPage());
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Erro ao salvar",
                    ex.Message,
                    "OK");
            }
        }

        // =====================================================
        // EXCLUIR PACIENTE
        // =====================================================

        private async void Button_ExcluirPaciente_Clicked(
            object sender, EventArgs e)
        {
            try
            {
                if (pessoaSelecionada == null)
                {
                    await DisplayAlert(
                        "Atenção",
                        "Selecione uma pessoa cadastrada.",
                        "OK");
                    return;
                }

                var paciente = await App.Database.BuscarPaciente(
                    pessoaSelecionada.Id);

                if (paciente == null)
                {
                    await DisplayAlert(
                        "Atenção",
                        "Esta pessoa não possui cadastro de paciente.",
                        "OK");
                    return;
                }

                bool confirmar = await DisplayAlert(
                    "Confirmar exclusão",
                    $"Deseja excluir o cadastro de paciente de " +
                    $"{pessoaSelecionada.Nome}? " +
                    "Os históricos vacinais relacionados também serão excluídos. " +
                    "Os dados pessoais serão preservados.",
                    "Excluir",
                    "Cancelar");

                if (!confirmar)
                    return;

                await App.Database.ExcluirPacienteComHistorico(
                    paciente.Id);

                pacienteSelecionado = null;
                modoEdicao = false;

                await LimparCamposPaciente();

                await DisplayAlert(
                    "Sucesso",
                    "Cadastro de paciente excluído. " +
                    "Os dados pessoais foram preservados.",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Erro ao excluir",
                    ex.Message,
                    "OK");
            }
        }

        // =====================================================
        // LIMPAR CAMPOS DO PACIENTE
        // =====================================================

        private async Task LimparCamposPaciente()
        {
            pck_sexo.SelectedItem = null;
            entry_cartao_sus.Text = string.Empty;
            entry_idade_anos.Text = string.Empty;
            entry_idade_meses.Text = string.Empty;
            entry_semanas_gestacao.Text = string.Empty;

            atualizandoCondicoes = true;

            try
            {
                chk_nenhuma.IsChecked = false;
                chk_gestante.IsChecked = false;
                chk_lactante.IsChecked = false;
                chk_lactante_bebe.IsChecked = false;
                chk_profissional_saude.IsChecked = false;
                chk_indigena.IsChecked = false;
                chk_acamado.IsChecked = false;
                chk_imunossuprimido.IsChecked = false;
                chk_hiv.IsChecked = false;
                chk_quimioterapia.IsChecked = false;
                chk_transplante.IsChecked = false;
                chk_corticoide.IsChecked = false;
                chk_alergia.IsChecked = false;
                chk_febre.IsChecked = false;
            }
            finally
            {
                atualizandoCondicoes = false;
            }

            entry_semanas_gestacao.IsEnabled = false;
            await Task.CompletedTask;
        }

        // =====================================================
        // CONSULTAR PESSOAS
        // =====================================================

        private async void Button_Consultar_Clicked(
            object sender, EventArgs e)
        {
            try
            {
                await CarregarPessoas();

                await DisplayAlert(
                    "Consulta de pessoas",
                    $"Foram encontradas {pessoas.Count} pessoa(s).",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Erro",
                    ex.Message,
                    "OK");
            }
        }

        // =====================================================
        // VOLTAR AO MENU
        // =====================================================

        private async void Button_Menu_Clicked(
            object sender, EventArgs e)
        {
            try
            {
                if (Navigation.NavigationStack.Count > 1)
                    await Navigation.PopToRootAsync();
                else
                    await DisplayAlert(
                        "Menu",
                        "Você já está na página inicial.",
                        "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Erro",
                    $"Não foi possível retornar ao menu.\n{ex.Message}",
                    "OK");
            }
        }
    }
}
