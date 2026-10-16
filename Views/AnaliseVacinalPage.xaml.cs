
using System.Net;
using System.Text;
using maui_amarv.Models;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace maui_amarv.Views;

public partial class AnaliseVacinalPage : ContentPage
{
    private string textoAnalise = string.Empty;

    private bool carregandoPacientes;

    // Paciente contém os dados específicos do paciente.
    // Pessoa contém o nome e os demais dados pessoais.
    // O Id é compartilhado entre as duas tabelas.
    private sealed class PacienteExibicao
    {
        public Paciente Paciente { get; set; } = null!;

        public string Nome { get; set; } = string.Empty;
    }

    public AnaliseVacinalPage()
    {
        InitializeComponent();
    }

    // =====================================================
    // CARREGAR PACIENTES
    // =====================================================

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await CarregarPacientes();
    }

    private async Task CarregarPacientes()
    {
        try
        {
            carregandoPacientes = true;

            // Carrega os pacientes e as pessoas separadamente.
            var pacientes =
                await App.Database.Listar<Paciente>();

            var pessoas =
                await App.Database.Listar<Pessoa>();

            // Relaciona Paciente.Id com Pessoa.Id.
            var pacientesExibicao = pacientes
                .Select(paciente =>
                {
                    var pessoa = pessoas.FirstOrDefault(
                        p => p.Id == paciente.Id);

                    return new PacienteExibicao
                    {
                        Paciente = paciente,
                        Nome = pessoa != null &&
                               !string.IsNullOrWhiteSpace(pessoa.Nome)
                            ? pessoa.Nome.Trim()
                            : $"Paciente ID {paciente.Id}"
                    };
                })
                .OrderBy(p => p.Nome)
                .ToList();

            // Configura o Picker para mostrar o nome,
            // e não o nome da classe ou o identificador.
            pck_paciente.SelectedItem = null;
            pck_paciente.ItemsSource = null;
            pck_paciente.ItemDisplayBinding =
                new Binding(nameof(PacienteExibicao.Nome));

            pck_paciente.ItemsSource = pacientesExibicao;

            textoAnalise = string.Empty;
            lbl_analise.Text = string.Empty;

            if (pacientesExibicao.Count == 0)
            {
                await DisplayAlert(
                    "Atenção",
                    "Nenhum paciente foi encontrado no banco de dados.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erro",
                $"Não foi possível carregar os pacientes.\n\n{ex.Message}",
                "OK");
        }
        finally
        {
            carregandoPacientes = false;
        }
    }

    private async void pck_paciente_SelectedIndexChanged(
        object sender,
        EventArgs e)
    {
        if (carregandoPacientes)
            return;

        await GerarAnalise();
    }

    // =====================================================
    // GERAR ANÁLISE VACINAL
    // =====================================================

    private async Task GerarAnalise()
    {
        try
        {
            if (pck_paciente.SelectedItem
                is not PacienteExibicao selecionado)
            {
                textoAnalise = string.Empty;
                lbl_analise.Text = string.Empty;
                return;
            }

            Paciente paciente = selecionado.Paciente;
            string nomePaciente = selecionado.Nome;

            // -------------------------------------------------
            // CONFIRMAR A PESSOA VINCULADA AO PACIENTE
            // Pessoa.Id = Paciente.Id
            // -------------------------------------------------

            var pessoas =
                await App.Database.Listar<Pessoa>();

            Pessoa? pessoa = pessoas.FirstOrDefault(
                p => p.Id == paciente.Id);

            if (pessoa == null)
            {
                await DisplayAlert(
                    "Atenção",
                    "Não foi encontrada a pessoa vinculada a este paciente. " +
                    "Verifique se os registros das tabelas Pessoa e Paciente " +
                    "possuem o mesmo Id.",
                    "OK");

                textoAnalise = string.Empty;
                lbl_analise.Text = string.Empty;
                return;
            }

            // Usa o nome atualizado do cadastro.
            if (!string.IsNullOrWhiteSpace(pessoa.Nome))
                nomePaciente = pessoa.Nome.Trim();

            // -------------------------------------------------
            // CARREGAR REGRAS VACINAIS
            // -------------------------------------------------

            var regras =
                await App.Database.ListarRegrasVacinais();

            // -------------------------------------------------
            // CARREGAR HISTÓRICO VACINAL
            // -------------------------------------------------

            var historico =
                await App.Database.BuscarHistoricoPaciente(
                    paciente.Id);

            // -------------------------------------------------
            // REALIZAR A ANÁLISE
            // -------------------------------------------------

            var servicoAnalise =
                new maui_amarv.Services.AnaliseEsquemaVacinal(
                    App.Database);

            string resultado =
                await servicoAnalise.AnalisarPacienteAsync(
                    paciente,
                    historico);

            // Garante que o nome do paciente apareça
            // no início do resultado apresentado.
            string cabecalho =
                $"PACIENTE: {nomePaciente}\n" +
                $"DATA DA ANÁLISE: {DateTime.Now:dd/MM/yyyy HH:mm}\n\n";

            textoAnalise = cabecalho + (resultado ?? string.Empty);

            // -------------------------------------------------
            // EXIBIR O RESULTADO
            // -------------------------------------------------

            lbl_analise.Text = textoAnalise;

            // -------------------------------------------------
            // SALVAR A ANÁLISE NO BANCO
            // -------------------------------------------------

            var registroAnalise =
                new maui_amarv.Models.AnaliseEsquemaVacinal
                {
                    PacienteId = paciente.Id,
                    NomePaciente = nomePaciente,
                    Resultado = textoAnalise,
                    DataAnalise = DateTime.Now,
                    Status = "Concluída"
                };

            await App.Database.SalvarAnaliseVacinal(
                registroAnalise);
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erro na análise",
                $"Não foi possível gerar ou salvar a análise.\n\n{ex.Message}",
                "OK");
        }
    }

    // =====================================================
    // COMPARTILHAR ANÁLISE
    // =====================================================

    private async void Button_Compartilhar_Clicked(
        object sender,
        EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(textoAnalise))
        {
            await DisplayAlert(
                "Atenção",
                "Gere uma análise primeiro.",
                "OK");

            return;
        }

        await Share.Default.RequestAsync(
            new ShareTextRequest
            {
                Title = "Análise Vacinal Amar V",
                Text = textoAnalise
            });
    }

    // =====================================================
    // GERAR ARQUIVO PARA IMPRESSÃO
    // =====================================================

    private async void Button_Imprimir_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(textoAnalise))
            {
                await DisplayAlert(
                    "Atenção",
                    "Gere uma análise primeiro.",
                    "OK");

                return;
            }

            string pasta = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments),
                "AmarV");

            Directory.CreateDirectory(pasta);

            string caminhoArquivo = Path.Combine(
                pasta,
                $"analise_vacinal_{DateTime.Now:yyyyMMdd_HHmmss}.html");

            string html = @"
<!DOCTYPE html>
<html lang='pt-BR'>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Análise Vacinal Amar V</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            margin: 40px;
            color: #222;
        }

        h1 {
            color: #0A4A9E;
            text-align: center;
        }

        pre {
            font-family: Arial, sans-serif;
            white-space: pre-wrap;
            overflow-wrap: break-word;
            font-size: 14px;
            line-height: 1.5;
        }

        @media print {
            body {
                margin: 15mm;
            }
        }
    </style>
</head>
<body>
    <h1>Amar V - Análise Vacinal</h1>
    <pre>" +
                WebUtility.HtmlEncode(textoAnalise) +
                @"</pre>
</body>
</html>";

            File.WriteAllText(
                caminhoArquivo,
                html,
                Encoding.UTF8);

            await Launcher.Default.OpenAsync(
                new OpenFileRequest
                {
                    File = new ReadOnlyFile(caminhoArquivo)
                });

            await DisplayAlert(
                "Relatório gerado",
                "O arquivo HTML foi salvo em Documentos > AmarV. " +
                "Abra-o e utilize a opção de impressão disponível no dispositivo.",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erro ao gerar relatório",
                ex.Message,
                "OK");
        }
    }

    // =====================================================
    // VOLTAR AO MENU
    // =====================================================

    private async void Button_Menu_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            if (Navigation.NavigationStack.Count > 1)
            {
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert(
                    "Navegação",
                    "Não há uma página anterior na pilha de navegação.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erro",
                $"Não foi possível voltar ao menu.\n\n{ex.Message}",
                "OK");
        }
    }
}