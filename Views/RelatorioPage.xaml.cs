using maui_amarv.Models;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using System.Net;
using System.Text;

namespace maui_amarv.Views;

public partial class RelatoriosPage : ContentPage
{
    private readonly UsuarioDoSistema usuarioLogado;
    private string textoRelatorio = string.Empty;

    private List<Paciente> pacientesDoRelatorio = new();

    private Dictionary<int, Pessoa> pessoasPorId = new();

    public RelatoriosPage(UsuarioDoSistema usuario)
    {
        InitializeComponent();

        usuarioLogado = usuario;

        ConfigurarTelaPorPerfil();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            var pessoas = await App.Database.Listar<Pessoa>();

            pessoasPorId = pessoas
                .GroupBy(p => p.Id)
                .ToDictionary(g => g.Key, g => g.First());

            pck_paciente.ItemsSource =
                await App.Database.ListarPacientes();
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erro",
                $"Não foi possível carregar os dados: {ex.Message}",
                "OK");
        }
    }

    // =====================================================
    // LOCALIZAR PESSOA PELO ID DO PACIENTE
    // =====================================================

    private Pessoa? ObterPessoa(int pacienteId)
    {
        pessoasPorId.TryGetValue(
            pacienteId,
            out Pessoa? pessoa);

        return pessoa;
    }

    // =====================================================
    // CONFIGURAÇÃO DA TELA POR PERFIL
    // =====================================================

    private void ConfigurarTelaPorPerfil()
    {
        lbl_perfil.Text =
            "Perfil: " + usuarioLogado.Perfil;

        if (usuarioLogado.Perfil == "Usuario")
        {
            area_usuario.IsVisible = true;
            area_admin.IsVisible = false;
        }
        else if (usuarioLogado.Perfil == "Administrador")
        {
            area_usuario.IsVisible = false;
            area_admin.IsVisible = true;
        }
        else
        {
            area_usuario.IsVisible = false;
            area_admin.IsVisible = false;
        }
    }

    // =====================================================
    // FILTRO DE PACIENTES
    // =====================================================

    private async Task<List<Paciente>> ObterPacientesFiltrados()
    {
        List<Paciente> pacientes =
            await App.Database.ListarPacientes();

        string filtroRua =
            entry_filtro_rua.Text ?? string.Empty;

        string filtroMae =
            entry_filtro_mae.Text ?? string.Empty;

        string filtroIdadeTexto =
            entry_filtro_idade.Text ?? string.Empty;

      
if (!string.IsNullOrWhiteSpace(filtroRua))
        {
            pacientes = pacientes
                .Where(p =>
                {
                    Pessoa? pessoa = ObterPessoa(p.Id);

                    return pessoa != null &&
                           !string.IsNullOrWhiteSpace(pessoa.Endereco) &&
                           pessoa.Endereco.Contains(
                               filtroRua,
                               StringComparison.OrdinalIgnoreCase);
                })
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(filtroMae))
        {
            pacientes = pacientes
                .Where(p =>
                {
                    Pessoa? pessoa = ObterPessoa(p.Id);

                    return pessoa != null &&
                           !string.IsNullOrWhiteSpace(pessoa.Nome_da_Mae) &&
                           pessoa.Nome_da_Mae.Contains(
                               filtroMae,
                               StringComparison.OrdinalIgnoreCase);
                })
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(filtroIdadeTexto))
        {
            if (int.TryParse(filtroIdadeTexto, out int idade))
            {
                pacientes = pacientes
                    .Where(p => p.IdadeAnos == idade)
                    .ToList();
            }
            else
            {
                return new List<Paciente>();
            }
        }

        return pacientes;
    }

    // =====================================================
    // RELATÓRIO INDIVIDUAL DO PACIENTE
    // =====================================================

    private async void Button_RelatorioPaciente_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            if (pck_paciente.SelectedItem is not Paciente paciente)
            {
                await DisplayAlert(
                    "Atenção",
                    "Selecione um paciente.",
                    "OK");

                return;
            }

            Pessoa? pessoa = ObterPessoa(paciente.Id);

            if (pessoa == null)
            {
                await DisplayAlert(
                    "Atenção",
                    "Não foi encontrada a pessoa vinculada a este paciente.",
                    "OK");

                return;
            }

            var historico =
                await App.Database.BuscarHistoricoPaciente(
                    paciente.Id);

            var analise =
                new maui_amarv.Services.AnaliseEsquemaVacinal(
                    App.Database);

            string resultadoAnalise =
                await analise.AnalisarPacienteAsync(
                    paciente,
                    historico);

            textoRelatorio =
                "RELATÓRIO DE SITUAÇÃO VACINAL DO PACIENTE\n\n" +

                "Nome: " +
                pessoa.Nome +
                "\n" +

               "Nomeda mãe: " +
               pessoa.Nome_da_Mae +
                "\n"+

                "Idade: " +
                paciente.IdadeFormatada +
                "\n" +

                "Endereço: " +
                pessoa.Endereco +
                "\n" +

                "Telefone: " +
                pessoa.Telefone +
                "\n" +

                "Cartão SUS: " +
                paciente.CartaoSUS +
                "\n\n" +

                resultadoAnalise;

            lbl_relatorio.Text =
                textoRelatorio;

            pacientesDoRelatorio =
                new List<Paciente> { paciente };

            RelatorioVacinalPaciente relatorio =
                new RelatorioVacinalPaciente();

            relatorio.GerarRelatorioVacinalPaciente(
                paciente,
                pessoa);

            relatorio.Conteudo =
                textoRelatorio;

            await App.Database
                .SalvarRelatorioVacinalPaciente(
                    relatorio);
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
    // RELATÓRIO DE PACIENTES COM PENDÊNCIAS
    // =====================================================

    private async void Button_Pendentes_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            if (usuarioLogado.Perfil != "Administrador")
            {
                await DisplayAlert(
                    "Acesso negado",
                    "Relatório administrativo permitido apenas para administrador.",
                    "OK");

                return;
            }

            pacientesDoRelatorio = new List<Paciente>();

            List<string> lista = new();

            var pacientes = await ObterPacientesFiltrados();

            foreach (Paciente paciente in pacientes)
            {
                Pessoa? pessoa = ObterPessoa(paciente.Id);

                if (pessoa == null)
                    continue;

                var regras =
                    await App.Database.ListarRegrasPorIdade(
                        paciente.IdadeTotalMeses);

                var historico =
                    await App.Database.BuscarHistoricoPaciente(
                        paciente.Id);

                int pendencias =
                    CalcularPendencias(regras, historico);

                if (pendencias > 0)
                {
                    pacientesDoRelatorio.Add(paciente);

                    lista.Add(
                        "Paciente: " + pessoa.Nome + "\n" +
                        "Nome da mãe: " + pessoa.Nome_da_Mae + "\n" +
                        "Idade: " + paciente.IdadeFormatada + "\n" +
                        "Endereço: " + pessoa.Endereco + "\n" +
                        "Telefone: " + pessoa.Telefone + "\n" +
                        "Pendências: " + pendencias + "\n");
                }
            }

            textoRelatorio =
                "RELATÓRIO DE PACIENTES COM VACINAS ATRASADAS/PENDENTES\n\n" +
                "Total encontrado: " +
                pacientesDoRelatorio.Count +
                "\n\n" +
                string.Join(
                    "\n----------------------\n",
                    lista);

            if (pacientesDoRelatorio.Count == 0)
                textoRelatorio += "Nenhum paciente encontrado.";

            lbl_relatorio.Text = textoRelatorio;

            RelatorioPacienteComVacinasAtrasadas relatorio =
                new RelatorioPacienteComVacinasAtrasadas();

            relatorio.GerarRelatorioPacientesAtrasados(
                pacientesDoRelatorio.Count);

            relatorio.Conteudo = textoRelatorio;

            await App.Database.SalvarRelatorioPacientesAtrasados(
                relatorio);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
        }
    }

    // =====================================================
    // RELATÓRIO DE PACIENTES REGULARES
    // =====================================================

    private async void Button_Regulares_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            if (usuarioLogado.Perfil != "Administrador")
            {
                await DisplayAlert(
                    "Acesso negado",
                    "Relatório administrativo permitido apenas para administrador.",
                    "OK");

                return;
            }

            pacientesDoRelatorio = new List<Paciente>();

            List<string> lista = new();

            var pacientes = await ObterPacientesFiltrados();

            foreach (Paciente paciente in pacientes)
            {
                Pessoa? pessoa = ObterPessoa(paciente.Id);

                if (pessoa == null)
                    continue;

                var regras =
                    await App.Database.ListarRegrasPorIdade(
                        paciente.IdadeTotalMeses);

                var historico =
                    await App.Database.BuscarHistoricoPaciente(
                        paciente.Id);

                int pendencias =
                    CalcularPendencias(regras, historico);

                if (pendencias == 0)
                {
                    pacientesDoRelatorio.Add(paciente);

                    lista.Add(
                        "Paciente: " + pessoa.Nome + "\n" +
                        "Nome da mãe: " + pessoa.Nome_da_Mae + "\n" +
                        "Idade: " + paciente.IdadeFormatada + "\n" +
                        "Endereço: " + pessoa.Endereco + "\n" +
                        "Telefone: " + pessoa.Telefone + "\n" +
                        "Situação: Regular\n");
                }


                textoRelatorio =
                    "RELATÓRIO DE PACIENTES EM SITUAÇÃO REGULAR\n\n" +
                    "Total encontrado: " +
                    pacientesDoRelatorio.Count +
                    "\n\n" +
                    string.Join(
                        "\n----------------------\n",
                        lista);

                if (pacientesDoRelatorio.Count == 0)
                    textoRelatorio += "Nenhum paciente encontrado.";

                lbl_relatorio.Text = textoRelatorio;

                RelatorioPacientesEmSituacaoRegular relatorio =
                    new RelatorioPacientesEmSituacaoRegular();

                relatorio.GerarRelatorioSituacaoRegular(
                    pacientesDoRelatorio.Count);

                relatorio.Conteudo = textoRelatorio;

                await App.Database.SalvarRelatorioPacientesRegulares(
                    relatorio);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
        }
    }

    // =====================================================
    // RELATÓRIO GERAL
    // =====================================================

    private async void Button_Geral_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            if (usuarioLogado.Perfil != "Administrador")
            {
                await DisplayAlert(
                    "Acesso negado",
                    "Relatório administrativo permitido apenas para administrador.",
                    "OK");

                return;
            }

            pacientesDoRelatorio =
                await ObterPacientesFiltrados();

            int totalPacientes = pacientesDoRelatorio.Count;
            int totalPendentes = 0;
            int totalRegulares = 0;

            foreach (Paciente paciente in pacientesDoRelatorio)
            {
                var regras =
                    await App.Database.ListarRegrasPorIdade(
                        paciente.IdadeTotalMeses);

                var historico =
                    await App.Database.BuscarHistoricoPaciente(
                        paciente.Id);

                int pendencias =
                    CalcularPendencias(regras, historico);

                if (pendencias > 0)
                    totalPendentes++;
                else
                    totalRegulares++;
            }

            textoRelatorio =
                "RELATÓRIO GERAL ADMINISTRATIVO\n\n" +
                "Data: " +
                DateTime.Now.ToString("dd/MM/yyyy HH:mm") +
                "\n\n" +
                "Filtro rua: " + entry_filtro_rua.Text +
                "\n" +
                "Filtro idade: " + entry_filtro_idade.Text +
                "\n" +
                "Filtro nome da mãe: " + entry_filtro_mae.Text +
                "\n\n" +
                "Total de pacientes encontrados: " +
                totalPacientes +
                "\n" +
                "Pacientes com pendências/atrasos: " +
                totalPendentes +
                "\n" +
                "Pacientes em situação regular: " +
                totalRegulares +
                "\n";

            lbl_relatorio.Text = textoRelatorio;

            Relatorio relatorio = new Relatorio
            {
                DataGeracao = DateTime.Now,
                Periodo = "Geral",
                Conteudo = textoRelatorio
            };

            await App.Database.SalvarRelatorio(relatorio);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
        }
    }

    // =====================================================
    // CALCULAR PENDÊNCIAS
    // =====================================================

    private int CalcularPendencias(
        List<RegraVacinal> regras,
        List<HistoricoVacinal> historico)
    {
        int pendencias = 0;

        foreach (RegraVacinal regra in regras)
        {
            HistoricoVacinal? registro =
                historico.FirstOrDefault(
                    h => h.RegraVacinalId == regra.Id);

            int dosesTomadas =
                registro?.DosesTomadas ?? 0;

            if (dosesTomadas < regra.TotalDoses)
                pendencias++;
        }

        return pendencias;
    }

    // =====================================================
    // EXPORTAR CSV
    // =====================================================

    private async void Button_Excel_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            if (pacientesDoRelatorio.Count == 0)
            {
                await DisplayAlert(
                    "Atenção",
                    "Gere um relatório primeiro.",
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
                "relatorio_amarv_" +
                DateTime.Now.ToString("yyyyMMdd_HHmmss") +
                ".csv");

            StringBuilder csv = new();

            csv.AppendLine(
                "Nome;Nome da Mae;Idade;Endereco;Telefone;CPF;Cartao SUS;Condicao Clinica");

            foreach (Paciente paciente in pacientesDoRelatorio)
            {
                Pessoa? pessoa = ObterPessoa(paciente.Id);

                if (pessoa == null)
                    continue;

                
                 csv.AppendLine(
    PrepararCsv(pessoa.Nome) + ";" +
    PrepararCsv(pessoa.Nome_da_Mae) + ";" +
    PrepararCsv(paciente.IdadeFormatada) + ";" +
    PrepararCsv(pessoa.Endereco) + ";" +
    PrepararCsv(pessoa.Telefone) + ";" +
    PrepararCsv(pessoa.Cpf) + ";" +
    PrepararCsv(paciente.CartaoSUS) + ";" +
    PrepararCsv(paciente.CondicaoClinica));

            }

            File.WriteAllText(
                caminhoArquivo,
                csv.ToString(),
                Encoding.UTF8);

            await Launcher.Default.OpenAsync(
                new OpenFileRequest
                {
                    File = new ReadOnlyFile(caminhoArquivo)
                });

            await DisplayAlert(
                "Exportado",
                "Arquivo CSV exportado em Documentos > AmarV.",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
        }
    }

    // =====================================================
    // PREPARAR CSV
    // =====================================================

    private string PrepararCsv(string? valor)
    {
        if (string.IsNullOrEmpty(valor))
            return "";

        valor = valor.Replace(";", ",")
                     .Replace("\n", " ")
                     .Replace("\r", " ");

        return "\"" + valor.Replace("\"", "\"\"") + "\"";
    }

    // =====================================================
    // COMPARTILHAR
    // =====================================================

    private async void Button_Compartilhar_Clicked(
        object sender,
        EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(textoRelatorio))
        {
            await DisplayAlert(
                "Atenção",
                "Gere um relatório primeiro.",
                "OK");

            return;
        }

        await Share.Default.RequestAsync(
            new ShareTextRequest
            {
                Title = "Relatório Amar V",
                Text = textoRelatorio
            });
    }

    // =====================================================
    // IMPRIMIR
    // =====================================================

    private async void Button_Imprimir_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(textoRelatorio))
            {
                await DisplayAlert(
                    "Atenção",
                    "Gere um relatório primeiro.",
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
                "relatorio_amarv.html");

            string html = @"
<html>
<head>
<meta charset='utf-8'>
<title>Relatório Amar V</title>
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
    font-size: 16px;
}
</style>
</head>
<body>
<h1>Amar V - Relatório</h1>
<pre>" +
                WebUtility.HtmlEncode(textoRelatorio) +
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
                "O relatório foi salvo em Documentos > AmarV. Abra o arquivo e use Ctrl + P para imprimir.",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
        }
    }

    // =====================================================
    // VOLTAR AO MENU
    // =====================================================

    private async void Button_Menu_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }
}