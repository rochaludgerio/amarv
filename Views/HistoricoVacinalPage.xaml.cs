
using maui_amarv.Models;
using System.Globalization;
using System.Text;

namespace maui_amarv.Views
{
    public partial class HistoricoVacinalPage : ContentPage
    {
        private readonly Dictionary<RegraVacinal, Picker> pickers = new();
        private readonly Dictionary<RegraVacinal, DatePicker> datas = new();

        private Paciente? pacienteSelecionado;
        private List<Pessoa> pessoas = new();

        public HistoricoVacinalPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await CarregarPacientes();
        }

        // ============================================================
        // CARREGAR PACIENTES
        // ============================================================

        private async Task CarregarPacientes()
        {
            try
            {
                var pacientes = await App.Database.Listar<Paciente>();

                // Atualiza o campo da classe, utilizado também ao salvar.
                pessoas = await App.Database.Listar<Pessoa>();

                var pacientesExibicao = pacientes
                    .Select(p => new
                    {
                        Paciente = p,
                        Nome = pessoas.FirstOrDefault(x => x.Id == p.Id)?.Nome
                            ?? $"Paciente ID {p.Id}"
                    })
                    .OrderBy(x => x.Nome)
                    .ToList();

                pck_paciente.ItemsSource = null;
                pck_paciente.ItemDisplayBinding = new Binding("Nome");
                pck_paciente.ItemsSource = pacientesExibicao;
                pck_paciente.SelectedItem = null;

                pacienteSelecionado = null;
                lbl_idade.Text = "Selecione um paciente.";

                layout_vacinas.Children.Clear();
                pickers.Clear();
                datas.Clear();
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Erro",
                    $"Não foi possível carregar os pacientes.\n\n{ex.Message}",
                    "OK");
            }
        }

        // ============================================================
        // SELECIONAR PACIENTE
        // ============================================================

        private async void pck_paciente_SelectedIndexChanged(
            object sender, EventArgs e)
        {
            try
            {
                var selecionado = pck_paciente.SelectedItem;

                if (selecionado == null)
                {
                    pacienteSelecionado = null;
                    lbl_idade.Text = "Selecione um paciente.";
                    layout_vacinas.Children.Clear();
                    pickers.Clear();
                    datas.Clear();
                    return;
                }

                var tipo = selecionado.GetType();

                pacienteSelecionado =
                    tipo.GetProperty("Paciente")?.GetValue(selecionado)
                    as Paciente;

                if (pacienteSelecionado == null)
                {
                    await DisplayAlert(
                        "Atenção",
                        "Não foi possível identificar o paciente selecionado.",
                        "OK");
                    return;
                }

                string nome = tipo.GetProperty("Nome")?
                    .GetValue(selecionado)?.ToString()
                    ?? $"Paciente ID {pacienteSelecionado.Id}";

                int idadeMeses = ObterIdadeMesesPaciente(pacienteSelecionado);

                lbl_idade.Text =
                    $"Paciente: {nome}\n" +
                    $"Idade: {FormatarIdadeCompleta(idadeMeses)}";

                await CarregarVacinasDoPaciente();
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Erro",
                    $"Não foi possível carregar o histórico vacinal.\n\n{ex.Message}",
                    "OK");
            }
        }

        // ============================================================
        // CARREGAR VACINAS
        // ============================================================

        private async Task CarregarVacinasDoPaciente()
        {
            layout_vacinas.Children.Clear();
            pickers.Clear();
            datas.Clear();

            if (pacienteSelecionado == null)
                return;

            try
            {
                int idadeMeses = ObterIdadeMesesPaciente(pacienteSelecionado);

                var historico = await App.Database
                    .BuscarHistoricoPaciente(pacienteSelecionado.Id);

                var todasRegras = await App.Database.Listar<RegraVacinal>();

                string grupo = pacienteSelecionado.Gestante
                    ? "Gestante"
                    : ObterGrupoEtario(idadeMeses);

                var regrasSUS = RemoverRegrasDuplicadas(
                    todasRegras
                        .Where(r => Normalizar(r.TipoCalendario) == "sus")
                        .Where(r => r.NumeroDose > 0)
                        .Where(r => RegraCompativelComPaciente(
                            r, pacienteSelecionado, grupo, idadeMeses, historico))
                        .ToList());

                var regrasPrivadas = RemoverRegrasDuplicadas(
                    todasRegras
                        .Where(r =>
                        {
                            string tipo = Normalizar(r.TipoCalendario);
                            return tipo == "rede privada"
                                || tipo == "privada"
                                || tipo == "rede_privada";
                        })
                        .Where(r => r.NumeroDose > 0)
                        .Where(r => RegraCompativelComPaciente(
                            r, pacienteSelecionado, grupo, idadeMeses, historico))
                        .ToList());

                AdicionarTituloSecao("VACINAS DO SUS", Colors.DarkGreen);

                if (regrasSUS.Count == 0)
                {
                    layout_vacinas.Children.Add(
                        CriarMensagemSecao(
                            "Nenhuma vacina do SUS foi encontrada para a idade e condições informadas."));
                }
                else
                {
                    foreach (var regra in regrasSUS)
                        AdicionarCampoDose(regra, historico);
                }

                layout_vacinas.Children.Add(new BoxView
                {
                    HeightRequest = 2,
                    Color = Colors.LightGray,
                    Margin = new Thickness(0, 20)
                });

                AdicionarTituloSecao(
                    "VACINAS DA REDE PRIVADA", Colors.DarkBlue);

                layout_vacinas.Children.Add(new Label
                {
                    Text = "Vacinas disponíveis na iniciativa privada. " +
                           "Essas vacinas não são consideradas pendências do calendário SUS.",
                    FontSize = 13,
                    TextColor = Colors.Gray,
                    Margin = new Thickness(0, 0, 0, 10)
                });

                if (regrasPrivadas.Count == 0)
                {
                    layout_vacinas.Children.Add(
                        CriarMensagemSecao(
                            "Nenhuma vacina da rede privada foi encontrada para a idade e condições informadas."));
                }
                else
                {
                    foreach (var regra in regrasPrivadas)
                        AdicionarCampoDose(regra, historico);
                }

                AdicionarRegistrosForaDasRegras(
                    historico, regrasSUS.Concat(regrasPrivadas).ToList());
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Erro",
                    $"Erro ao carregar as vacinas:\n\n{ex.Message}",
                    "OK");
            }
        }

        private List<RegraVacinal> RemoverRegrasDuplicadas(
            List<RegraVacinal> regras)
        {
            return regras
                .GroupBy(r =>
                    $"{Normalizar(r.NomeVacina)}|{r.NumeroDose}|{Normalizar(r.TipoCalendario)}")
                .Select(g => g.First())
                .OrderBy(r => r.NomeVacina)
                .ThenBy(r => r.NumeroDose)
                .ToList();
        }

        private void AdicionarTituloSecao(string titulo, Color cor)
        {
            layout_vacinas.Children.Add(new Label
            {
                Text = titulo,
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                TextColor = cor,
                Margin = new Thickness(0, 10, 0, 8)
            });
        }

        private Label CriarMensagemSecao(string texto)
        {
            return new Label
            {
                Text = texto,
                FontSize = 15,
                TextColor = Colors.Gray,
                Margin = new Thickness(10, 10)
            };
        }

        // ============================================================
        // CAMPOS DE DOSE
        // ============================================================

        private void AdicionarCampoDose(
            RegraVacinal regra,
            List<HistoricoVacinal> historico)
        {
            var registro = historico.FirstOrDefault(h =>
                h.RegraVacinalId == regra.Id &&
                h.DosesTomadas > 0);

            bool doseRegistrada = registro != null;

            DateTime dataAplicacao = DateTime.Today;

            if (registro != null)
            {
                if (registro.DataAplicacao != default)
                    dataAplicacao = registro.DataAplicacao;
                else if (registro.DataUltimaAplicacao != default)
                    dataAplicacao = registro.DataUltimaAplicacao;
            }

            if (dataAplicacao.Date > DateTime.Today)
                dataAplicacao = DateTime.Today;

            var frame = new Border
            {
                Stroke = Colors.LightGray,
                StrokeThickness = 1,
                Padding = 12,
                Margin = new Thickness(0, 5),
                BackgroundColor = Colors.White
            };

            var layout = new VerticalStackLayout { Spacing = 8 };

            string tipoCalendario = Normalizar(regra.TipoCalendario);
            bool eSUS = tipoCalendario == "sus";

            layout.Children.Add(new Label
            {
                Text = eSUS ? "SUS" : "REDE PRIVADA",
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = eSUS ? Colors.DarkGreen : Colors.DarkBlue
            });

            layout.Children.Add(new Label
            {
                Text = $"{regra.NomeVacina} — Dose {regra.NumeroDose}",
                FontSize = 17,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.Black
            });

            if (!string.IsNullOrWhiteSpace(regra.GrupoEtario))
                layout.Children.Add(new Label
                {
                    Text = $"Grupo: {regra.GrupoEtario}",
                    FontSize = 12,
                    TextColor = Colors.Gray
                });

            if (!string.IsNullOrWhiteSpace(regra.GrupoEspecial))
                layout.Children.Add(new Label
                {
                    Text = $"Condição: {regra.GrupoEspecial}",
                    FontSize = 12,
                    TextColor = Colors.DarkSlateGray
                });

            if (!string.IsNullOrWhiteSpace(regra.Observacao))
                layout.Children.Add(new Label
                {
                    Text = regra.Observacao,
                    FontSize = 13,
                    TextColor = Colors.Gray
                });

            if (!string.IsNullOrWhiteSpace(regra.IntervaloEntreDoses))
                layout.Children.Add(new Label
                {
                    Text = $"Intervalo recomendado: {regra.IntervaloEntreDoses}",
                    FontSize = 13,
                    TextColor = Colors.DarkSlateGray
                });

            if (!string.IsNullOrWhiteSpace(regra.Periodicidade))
                layout.Children.Add(new Label
                {
                    Text = $"Periodicidade: {regra.Periodicidade}",
                    FontSize = 13,
                    TextColor = Colors.DarkSlateGray
                });

            var picker = new Picker
            {
                Title = "Selecione a situação",
                FontSize = 15
            };

            picker.Items.Add("Não registrada");
            picker.Items.Add("Registrada");
            picker.SelectedIndex = doseRegistrada ? 1 : 0;

            layout.Children.Add(picker);

            var datePicker = new DatePicker
            {
                Date = dataAplicacao,
                Format = "dd/MM/yyyy",
                IsVisible = doseRegistrada,
                MaximumDate = DateTime.Today
            };

            layout.Children.Add(datePicker);


            picker.SelectedIndexChanged += (s, e) =>
            {
                datePicker.IsVisible = picker.SelectedIndex == 1;

                if (datePicker.Date.HasValue &&
                    datePicker.Date.Value.Date > DateTime.Today)
                {
                    datePicker.Date = DateTime.Today;
                }
            };

            pickers[regra] = picker;
            datas[regra] = datePicker;

            frame.Content = layout;
            layout_vacinas.Children.Add(frame);
        }

        // ============================================================
        // SALVAR HISTÓRICO VACINAL — CORRIGIDO
        // ============================================================

        private async void Button_Salvar_Clicked(
            object sender, EventArgs e)
        {
            if (pacienteSelecionado == null)
            {
                await DisplayAlert(
                    "Atenção",
                    "Selecione um paciente antes de salvar.",
                    "OK");
                return;
            }

            if (pickers.Count == 0)
            {
                await DisplayAlert(
                    "Atenção",
                    "Não existem doses disponíveis para salvar. " +
                    "Selecione um paciente e aguarde o carregamento das vacinas.",
                    "OK");
                return;
            }

            try
            {
                // Atualiza a lista de pessoas antes de obter o nome.
                pessoas = await App.Database.Listar<Pessoa>();

                string nomePaciente = pessoas
                    .FirstOrDefault(p => p.Id == pacienteSelecionado.Id)?.Nome
                    ?? $"Paciente ID {pacienteSelecionado.Id}";

                var historicoAtual = await App.Database
                    .BuscarHistoricoPaciente(pacienteSelecionado.Id);

                // Valida todas as datas antes de alterar o banco.
                foreach (var item in pickers)
                {
                    if (item.Value.SelectedIndex != 1)
                        continue;

                    if (!datas.TryGetValue(item.Key, out DatePicker? datePicker)
                        || datePicker == null)
                    {
                        await DisplayAlert(
                            "Erro",
                            $"Não foi possível obter a data da vacina {item.Key.NomeVacina}.",
                            "OK");
                        return;
                    }

                    if (datePicker.Date.HasValue &&
    datePicker.Date.Value.Date > DateTime.Today)
                    {
                        await DisplayAlert(
                            "Data inválida",
                            $"A data da vacina {item.Key.NomeVacina}, " +
                            $"dose {item.Key.NumeroDose}, não pode ser futura.",
                            "OK");
                        return;
                    }
                }

                int quantidadeSalva = 0;
                int quantidadeExcluida = 0;

                foreach (var item in pickers)
                {
                    RegraVacinal regra = item.Key;
                    Picker picker = item.Value;

                    bool doseRegistrada = picker.SelectedIndex == 1;

                    // Localiza o registro correspondente a esta regra.
                    var registros = historicoAtual
                        .Where(h =>
                            h.RegraVacinalId == regra.Id &&
                            h.DosesTomadas > 0)
                        .ToList();

                    var registro = registros.FirstOrDefault();

                    if (!doseRegistrada)
                    {
                        // Exclui somente os registros desta dose/regra.
                        foreach (var registroExcluir in registros)
                        {
                            await App.Database
                                .ExcluirHistoricoVacinalAsync(registroExcluir);

                            historicoAtual.Remove(registroExcluir);
                            quantidadeExcluida++;
                        }

                        continue;
                    }

                    DateTime dataAplicacao =
    datas.TryGetValue(regra, out DatePicker? dp) && dp != null
        ? dp.Date.Value.Date
        : DateTime.Today;

                    if (registro != null)
                    {
                        registro.PacienteId = pacienteSelecionado.Id;
                        registro.NomePaciente = nomePaciente;
                        registro.RegraVacinalId = regra.Id;
                        registro.NomeVacina = regra.NomeVacina;

                        // O modelo atual representa uma dose por registro.
                        registro.DosesTomadas = 1;
                        registro.DataAplicacao = dataAplicacao;
                        registro.DataUltimaAplicacao = dataAplicacao;

                        if (registro.DataRegistro == default)
                            registro.DataRegistro = DateTime.Now;

                        if (string.IsNullOrWhiteSpace(registro.Observacao))
                            registro.Observacao = regra.Observacao;

                        await App.Database.SalvarHistoricoVacinal(registro);
                    }
                    else
                    {
                        var novoRegistro = new HistoricoVacinal
                        {
                            PacienteId = pacienteSelecionado.Id,
                            NomePaciente = nomePaciente,
                            RegraVacinalId = regra.Id,
                            NomeVacina = regra.NomeVacina,
                            DosesTomadas = 1,
                            DataAplicacao = dataAplicacao,
                            DataUltimaAplicacao = dataAplicacao,
                            DataRegistro = DateTime.Now,
                            LocalAplicacao = string.Empty,
                            Lote = string.Empty,
                            ProfissionalResponsavel = string.Empty,
                            Observacao = regra.Observacao
                        };

                        await App.Database.SalvarHistoricoVacinal(novoRegistro);

                        // Evita criar outra cópia durante este processamento.
                        historicoAtual.Add(novoRegistro);
                    }

                    quantidadeSalva++;
                }

                // A mensagem só aparece depois de processar todas as doses.
                await DisplayAlert(
                    "Sucesso",
                    "Histórico vacinal atualizado com sucesso.\n\n" +
                    $"Paciente: {nomePaciente}\n" +
                    $"Doses registradas/atualizadas: {quantidadeSalva}\n" +
                    $"Doses removidas: {quantidadeExcluida}",
                    "OK");

                // Navega somente depois de concluir o salvamento.
                await Navigation.PushAsync(new AnaliseVacinalPage());
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Erro ao salvar",
                    "Não foi possível concluir o salvamento do histórico vacinal.\n\n" +
                    $"Detalhes: {ex.Message}",
                    "OK");
            }
        }

        // ============================================================
        // REGISTROS ANTIGOS
        // ============================================================

        private void AdicionarRegistrosForaDasRegras(
            List<HistoricoVacinal> historico,
            List<RegraVacinal> regrasAtuais)
        {
            if (historico == null || historico.Count == 0)
                return;

            var idsRegrasAtuais = regrasAtuais.Select(r => r.Id).ToHashSet();

            var registrosAntigos = historico
                .Where(h => h.DosesTomadas > 0 &&
                            !idsRegrasAtuais.Contains(h.RegraVacinalId))
                .OrderBy(h => h.NomeVacina)
                .ThenBy(h => h.DataAplicacao)
                .ToList();

            if (registrosAntigos.Count == 0)
                return;

            layout_vacinas.Children.Add(new BoxView
            {
                HeightRequest = 1,
                Color = Colors.LightGray,
                Margin = new Thickness(0, 20, 0, 10)
            });

            layout_vacinas.Children.Add(new Label
            {
                Text = "REGISTROS ANTIGOS ENCONTRADOS",
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.DarkOrange,
                Margin = new Thickness(0, 5, 0, 5)
            });

            layout_vacinas.Children.Add(new Label
            {
                Text = "Os registros abaixo estão salvos no histórico, " +
                       "mas não fazem parte das regras atualmente apresentadas " +
                       "para este paciente. Verifique antes de excluir.",
                FontSize = 13,
                TextColor = Colors.Gray,
                Margin = new Thickness(0, 0, 0, 10)
            });

            foreach (var registro in registrosAntigos)
                AdicionarRegistroAntigo(registro);
        }

        private void AdicionarRegistroAntigo(HistoricoVacinal registro)
        {
            var frame = new Border
            {
                Stroke = Colors.LightGray,
                StrokeThickness = 1,
                Padding = 12,
                Margin = new Thickness(0, 5),
                BackgroundColor = Colors.FloralWhite
            };

            var layout = new VerticalStackLayout { Spacing = 8 };

            layout.Children.Add(new Label
            {
                Text = $"{registro.NomeVacina} — {registro.DosesTomadas} dose(s)",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.Black
            });

            DateTime data = registro.DataAplicacao != default
                ? registro.DataAplicacao
                : registro.DataUltimaAplicacao;

            if (data != default)
                layout.Children.Add(new Label
                {
                    Text = $"Data da aplicação: {data:dd/MM/yyyy}",
                    FontSize = 13,
                    TextColor = Colors.Gray
                });

            layout.Children.Add(new Label
            {
                Text = "Este registro não está associado às regras atualmente apresentadas.",
                FontSize = 12,
                TextColor = Colors.DarkOrange
            });

            var botaoExcluir = new Button
            {
                Text = "Excluir este registro",
                FontSize = 14,
                BackgroundColor = Colors.LightCoral,
                TextColor = Colors.Black
            };

            botaoExcluir.Clicked += async (sender, e) =>
                await ExcluirRegistroAntigoAsync(registro);

            layout.Children.Add(botaoExcluir);

            frame.Content = layout;
            layout_vacinas.Children.Add(frame);
        }

        private async Task ExcluirRegistroAntigoAsync(
            HistoricoVacinal registro)
        {
            try
            {
                bool confirmar = await DisplayAlert(
                    "Confirmar exclusão",
                    $"Deseja excluir o registro:\n\n{registro.NomeVacina}\n" +
                    $"Dose(s): {registro.DosesTomadas}",
                    "Excluir",
                    "Cancelar");

                if (!confirmar)
                    return;

                await App.Database.ExcluirHistoricoVacinalAsync(registro);

                await DisplayAlert(
                    "Sucesso",
                    "Registro removido do histórico vacinal.",
                    "OK");

                await CarregarVacinasDoPaciente();
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Erro",
                    $"Não foi possível excluir o registro.\n\n{ex.Message}",
                    "OK");
            }
        }

        // ============================================================
        // COMPATIBILIDADE DAS REGRAS
        // ============================================================

        private bool RegraCompativelComPaciente(
            RegraVacinal regra,
            Paciente paciente,
            string grupo,
            int idadeMeses,
            List<HistoricoVacinal> historico)
        {
            if (paciente == null)
                return false;

            if (paciente.Gestante)
            {
                if (Normalizar(regra.GrupoEtario) != "gestante")
                    return false;

                if (regra.SemanaGestacionalMinima > 0 &&
                    paciente.SemanasGestacao < regra.SemanaGestacionalMinima)
                    return false;
            }
            else
            {
                string grupoRegra = Normalizar(regra.GrupoEtario);

                bool grupoCompativel =
                    grupoRegra == Normalizar(grupo) ||
                    grupoRegra == "todos" ||
                    grupoRegra == "geral" ||
                    string.IsNullOrWhiteSpace(grupoRegra);

                if (!grupoCompativel)
                    return false;
            }

            if (!GrupoEspecialAtendido(regra.GrupoEspecial, paciente))
                return false;

            if (idadeMeses < regra.IdadeMinimaMeses)
                return false;

            if (regra.IdadeMaximaMeses > 0 &&
                idadeMeses > regra.IdadeMaximaMeses)
                return false;

            // Mantém a regra original de pré-requisito.
            // A lógica de sequência deverá ser ajustada conforme
            // a estrutura definitiva das doses no banco.
            if (regra.NumeroDose > 1)
            {
                bool tomouDoseAnterior = historico != null &&
                    historico.Any(h =>
                        Normalizar(h.NomeVacina) == Normalizar(regra.NomeVacina) &&
                        h.DosesTomadas >= regra.NumeroDose - 1);

                if (!tomouDoseAnterior)
                    return false;
            }

            return true;
        }

        private bool GrupoEspecialAtendido(
            string? grupo,
            Paciente paciente)
        {
            string g = Normalizar(grupo ?? string.Empty);

            if (string.IsNullOrWhiteSpace(g) ||
                g == "geral" || g == "todos" || g == "nenhuma")
                return true;

            if (g.Contains("gestante"))
                return paciente.Gestante;

            if (g.Contains("lactante") &&
                (g.Contains("bebe") || g.Contains("menor") ||
                 g.Contains("6 meses")))
                return paciente.CriancaAmamentadaMenor6Meses;

            if (g.Contains("lactante"))
                return paciente.Lactante;

            if (g.Contains("profissional") && g.Contains("saude"))
                return paciente.ProfissionalSaude;

            if (g.Contains("indigena"))
                return paciente.Indigena;

            if (g.Contains("acamado") || g.Contains("institucionalizado") ||
                g.Contains("institucional"))
                return paciente.AcamadoOuInstitucionalizado;

            if (g.Contains("imunossuprimido") || g.Contains("imunossupressao"))
                return paciente.Imunossuprimido;

            if (g.Contains("hiv"))
                return paciente.HIV;

            if (g.Contains("quimioterapia"))
                return paciente.Quimioterapia;

            if (g.Contains("transplantado") || g.Contains("transplante"))
                return paciente.Transplantado;

            if (g.Contains("corticoide") || g.Contains("imunobiologico"))
                return paciente.UsoCorticoideOuImunobiologico;

            if (g.Contains("alergia") || g.Contains("anafilaxia"))
                return paciente.AlergiaGrave;

            if (g.Contains("febre") || g.Contains("infeccao") ||
                g.Contains("doenca aguda"))
                return paciente.FebreOuDoencaAguda;

            if (g.Contains("grupo especial"))
            {
                return paciente.Gestante ||
                       paciente.Lactante ||
                       paciente.CriancaAmamentadaMenor6Meses ||
                       paciente.ProfissionalSaude ||
                       paciente.Indigena ||
                       paciente.AcamadoOuInstitucionalizado ||
                       paciente.Imunossuprimido ||
                       paciente.HIV ||
                       paciente.Quimioterapia ||
                       paciente.Transplantado ||
                       paciente.UsoCorticoideOuImunobiologico ||
                       paciente.AlergiaGrave ||
                       paciente.FebreOuDoencaAguda;
            }

            return false;
        }

        // ============================================================
        // IDADE
        // ============================================================

        private int ObterIdadeMesesPaciente(Paciente paciente)
        {
            if (paciente.DataNascimento != default)
                return CalcularIdadeEmMeses(paciente.DataNascimento);

            return Math.Max(0, paciente.IdadeTotalMeses);
        }

        private int CalcularIdadeEmMeses(DateTime nascimento)
        {
            DateTime hoje = DateTime.Today;

            int meses = (hoje.Year - nascimento.Year) * 12
                        + hoje.Month - nascimento.Month;

            if (hoje.Day < nascimento.Day)
                meses--;

            return Math.Max(0, meses);
        }

        private string FormatarIdadeCompleta(int idadeMeses)
        {
            int anos = idadeMeses / 12;
            int meses = idadeMeses % 12;

            return $"{anos} ano(s) e {meses} mês(es)";
        }

        private string ObterGrupoEtario(int idadeMeses)
        {
            if (idadeMeses < 120)
                return "Criança";

            if (idadeMeses < 300)
                return "Adolescente/Jovem";

            if (idadeMeses < 720)
                return "Adulto";

            return "Idoso";
        }

        // ============================================================
        // NORMALIZAÇÃO
        // ============================================================

        private static string Normalizar(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            string normalizado = texto.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();

            foreach (char caractere in normalizado)
            {
                UnicodeCategory categoria =
                    CharUnicodeInfo.GetUnicodeCategory(caractere);

                if (categoria != UnicodeCategory.NonSpacingMark)
                    builder.Append(caractere);
            }

            return builder.ToString()
                .Normalize(NormalizationForm.FormC)
                .Trim()
                .ToLowerInvariant();
        }

        // ============================================================
        // VOLTAR AO MENU
        // ============================================================

        private async void Button_Menu_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (Navigation.NavigationStack.Count > 1)
                    await Navigation.PopAsync();
                else
                    await DisplayAlert(
                        "Navegação",
                        "Não há uma página anterior na pilha de navegação.",
                        "OK");
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
}