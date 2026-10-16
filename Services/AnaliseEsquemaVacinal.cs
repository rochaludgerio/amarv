using maui_amarv.Data;
using maui_amarv.Models;
using System.Globalization;
using System.Text;

namespace maui_amarv.Services
{
    public class AnaliseEsquemaVacinal
    {
        private readonly BancoDados _bancoDados;

        public AnaliseEsquemaVacinal(BancoDados bancoDados)
        {
            _bancoDados = bancoDados;
        }

        // =========================================================
        // MÉTODO PRINCIPAL
        // =========================================================

        public async Task<string> AnalisarPacienteAsync(
            Paciente paciente,
            List<HistoricoVacinal>? historico = null)
        {
            if (paciente == null)
                return "Paciente não informado.";

            // -----------------------------------------------------
            // HISTÓRICO
            // -----------------------------------------------------

            historico ??=
                await ObterHistoricoPaciente(paciente.Id);

            historico = historico
                .Where(h =>
                    h.PacienteId == paciente.Id &&
                    h.DosesTomadas > 0)
                .ToList();

            // -----------------------------------------------------
            // IDADE
            // -----------------------------------------------------

            int idadeMeses =
                ObterIdadeMesesPaciente(paciente);

            string idade =
                ObterIdadeFormatadaPaciente(
                    paciente,
                    idadeMeses);

            // -----------------------------------------------------
            // GRUPO ETÁRIO
            // -----------------------------------------------------

            string grupo =
                paciente.Gestante
                    ? "Gestante"
                    : ObterGrupoEtario(idadeMeses);

            // =====================================================
            // LIMITES OPERACIONAIS
            // =====================================================

            const int limiteSC = 2;

            int limiteIM =
                idadeMeses <= 24
                    ? 2
                    : 4;

            // IMPORTANTE:
            // Esses contadores são compartilhados entre SUS
            // e Rede Privada durante TODA esta análise.
            int aplicacoesSC = 0;
            int aplicacoesIM = 0;
            int aplicacoesOral = 0;

            // =====================================================
            // REGRAS
            // =====================================================

            var todasRegras =
                await _bancoDados.Listar<RegraVacinal>();

            // -----------------------------------------------------
            // SUS
            // -----------------------------------------------------

            var regrasSUS =
                todasRegras
                    .Where(EhSUS)
                    .ToList();

            var aplicaveisSUS =
                ObterRegrasAplicaveis(
                    regrasSUS,
                    idadeMeses,
                    paciente);

            // -----------------------------------------------------
            // REDE PRIVADA
            // -----------------------------------------------------

            var regrasPrivadas =
                todasRegras
                    .Where(EhRedePrivada)
                    .ToList();

            var aplicaveisPrivadas =
                ObterRegrasAplicaveis(
                    regrasPrivadas,
                    idadeMeses,
                    paciente);

            // =====================================================
            // RESULTADOS SEPARADOS
            // =====================================================

            var pendentesSUS =
                new List<ResultadoVacinal>();

            var atrasadasSUS =
                new List<ResultadoVacinal>();

            var recuperacaoSUS =
                new List<string>();

            var regularesSUS =
                new List<string>();

            var pendentesPrivadas =
                new List<ResultadoVacinal>();

            var atrasadasPrivadas =
                new List<ResultadoVacinal>();

            var recuperacaoPrivadas =
                new List<string>();

            var regularesPrivadas =
                new List<string>();

            // =====================================================
            // ANALISAR SUS
            // =====================================================

            AnalisarCalendario(
                "SUS",
                aplicaveisSUS,
                historico,
                idadeMeses,
                grupo,
                paciente,
                pendentesSUS,
                atrasadasSUS,
                regularesSUS,
                recuperacaoSUS,
                ref aplicacoesSC,
                ref aplicacoesIM,
                ref aplicacoesOral,
                limiteSC,
                limiteIM);

            // =====================================================
            // ANALISAR REDE PRIVADA
            // =====================================================

            // NÃO zerar os contadores aqui.
            //
            // Assim, se o SUS já consumiu:
            //
            // SC = 2
            // IM = 2
            //
            // a rede privada não poderá consumir novamente 2 + 2.
            AnalisarCalendario(
                "Rede Privada",
                aplicaveisPrivadas,
                historico,
                idadeMeses,
                grupo,
                paciente,
                pendentesPrivadas,
                atrasadasPrivadas,
                regularesPrivadas,
                recuperacaoPrivadas,
                ref aplicacoesSC,
                ref aplicacoesIM,
                ref aplicacoesOral,
                limiteSC,
                limiteIM);

            // =====================================================
            // TODAS AS REGRAS PARA ALERTAS
            // =====================================================

            var regrasAplicaveisTodas =
                aplicaveisSUS
                    .Concat(aplicaveisPrivadas)
                    .ToList();

            var alertas =
                GerarAlertasDeSeguranca(
                    paciente,
                    regrasAplicaveisTodas);

            var contraindicacoes =
                GerarContraindicacoesEPrecaucoes(
                    paciente,
                    regrasAplicaveisTodas);

            // =====================================================
            // MONTAR RESULTADO
            // =====================================================

            var resultado =
                new StringBuilder();

            resultado.AppendLine(
                "ANÁLISE DO ESQUEMA VACINAL");

            resultado.AppendLine(
                "========================================");

            var pessoas = await _bancoDados.Listar<Pessoa>();

            var pessoa = pessoas.FirstOrDefault(
                p => p.Id == paciente.Id);

            resultado.AppendLine(
                $"Paciente: {pessoa?.Nome ?? "Nome não informado"}");

            resultado.AppendLine(
                $"Idade: {idade}");

            resultado.AppendLine(
                $"Grupo: {grupo}");

            if (paciente.Gestante)
            {
                resultado.AppendLine(
                    $"Gestação: {paciente.SemanasGestacao} semana(s)");
            }

            // =====================================================
            // LIMITE
            // =====================================================

            resultado.AppendLine();

            resultado.AppendLine(
                "LIMITE OPERACIONAL DO ATENDIMENTO");

            resultado.AppendLine(
                "----------------------------------------");

            resultado.AppendLine(
                $"Subcutânea (SC): {aplicacoesSC}/{limiteSC}");

            resultado.AppendLine(
                $"Intramuscular (IM): {aplicacoesIM}/{limiteIM}");

            resultado.AppendLine(
                $"Oral: {aplicacoesOral} — sem limite operacional");

            resultado.AppendLine(
                "Os limites SC e IM são compartilhados entre " +
                "SUS e Rede Privada no mesmo atendimento.");

            resultado.AppendLine(
                "Observação: trata-se de uma regra operacional " +
                "configurada no aplicativo.");

            // =====================================================
            // HISTÓRICO
            // =====================================================

            resultado.AppendLine();

            resultado.AppendLine(
                "HISTÓRICO VACINAL");

            var historicoValido =
                historico
                    .Where(h =>
                        h.DosesTomadas > 0)
                    .GroupBy(h =>
                        NormalizarVacina(
                            h.NomeVacina))
                    .Select(g => new
                    {
                        Nome =
                            g.First().NomeVacina,

                        Doses =
                            g.Sum(x =>
                                Math.Max(
                                    0,
                                    x.DosesTomadas)),

                        Ultima =
                            ObterUltimaDataAplicacao(
                                g.ToList())
                    })
                    .Where(x =>
                        x.Doses > 0)
                    .OrderBy(x =>
                        x.Nome)
                    .ToList();

            if (historicoValido.Count == 0)
            {
                resultado.AppendLine(
                    "Paciente não possui histórico vacinal " +
                    "registrado no sistema.");
            }
            else
            {
                foreach (var item in historicoValido)
                {
                    if (item.Ultima != default)
                    {
                        resultado.AppendLine(
                            $"- {item.Nome}: " +
                            $"{item.Doses} dose(s) — " +
                            $"última aplicação: " +
                            $"{item.Ultima:dd/MM/yyyy}");
                    }
                    else
                    {
                        resultado.AppendLine(
                            $"- {item.Nome}: " +
                            $"{item.Doses} dose(s)");
                    }
                }
            }

            // =====================================================
            // SITUAÇÃO VACINAL — SUS
            // =====================================================

            resultado.AppendLine();

            resultado.AppendLine(
                "SITUAÇÃO VACINAL — SUS");

            var situacaoSUS =
                RemoverDuplicadosResultado(
                    pendentesSUS
                        .Concat(atrasadasSUS)
                        .ToList());

            if (situacaoSUS.Count == 0)
            {
                resultado.AppendLine(
                    "Nenhuma dose pendente ou atrasada " +
                    "identificada no calendário SUS.");
            }
            else
            {
                foreach (var item in situacaoSUS)
                {
                    resultado.AppendLine(
                        $"- {item.Regra.NomeVacina} — " +
                        $"dose {item.Regra.NumeroDose}: " +
                        $"{item.Motivo}");
                }
            }

            // =====================================================
            // SITUAÇÃO VACINAL — REDE PRIVADA
            // =====================================================

            resultado.AppendLine();

            resultado.AppendLine(
                "SITUAÇÃO VACINAL — REDE PRIVADA");

            var situacaoPrivada =
                RemoverDuplicadosResultado(
                    pendentesPrivadas
                        .Concat(atrasadasPrivadas)
                        .ToList());

            if (situacaoPrivada.Count == 0)
            {
                resultado.AppendLine(
                    "Nenhuma dose pendente ou atrasada " +
                    "identificada na rede privada.");
            }
            else
            {
                foreach (var item in situacaoPrivada)
                {
                    resultado.AppendLine(
                        $"- {item.Regra.NomeVacina} — " +
                        $"dose {item.Regra.NumeroDose}: " +
                        $"{item.Motivo}");
                }
            }

            // =====================================================
            // PRÓXIMAS VACINAS
            // =====================================================

            resultado.AppendLine();

            resultado.AppendLine(
                "PRÓXIMAS VACINAS");

            var proximas =
                pendentesSUS
                    .Concat(pendentesPrivadas)
                    .Where(p =>
                        p.Motivo.StartsWith(
                            "DOSE FUTURA",
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();

            var proximasUnicas =
                RemoverDuplicadosResultado(
                    proximas);

            if (proximasUnicas.Count == 0)
            {
                resultado.AppendLine(
                    "Nenhuma dose futura identificada " +
                    "nas regras aplicáveis.");
            }
            else
            {
                foreach (var item in proximasUnicas)
                {
                    resultado.AppendLine(
                        $"- [{item.Calendario}] " +
                        $"{item.Regra.NomeVacina} — " +
                        $"dose {item.Regra.NumeroDose}: " +
                        $"indicada a partir de " +
                        $"{FormatarIdadeMeses(
                            item.Regra.IdadeMinimaMeses)}.");
                }
            }

            // =====================================================
            // RECUPERAÇÃO — SUS
            // =====================================================

            resultado.AppendLine();

            resultado.AppendLine(
                "ESQUEMA DE RECUPERAÇÃO — SUS");

            var recuperacaoSUSUnica =
                recuperacaoSUS
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();

            if (recuperacaoSUSUnica.Count == 0)
            {
                resultado.AppendLine(
                    "Nenhuma orientação de recuperação " +
                    "identificada no SUS.");
            }
            else
            {
                foreach (var item in recuperacaoSUSUnica)
                {
                    resultado.AppendLine(
                        $"- {item}");
                }
            }

            // =====================================================
            // RECUPERAÇÃO — PRIVADA
            // =====================================================

            resultado.AppendLine();

            resultado.AppendLine(
                "ESQUEMA DE RECUPERAÇÃO — REDE PRIVADA");

            var recuperacaoPrivadaUnica =
                recuperacaoPrivadas
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();

            if (recuperacaoPrivadaUnica.Count == 0)
            {
                resultado.AppendLine(
                    "Nenhuma orientação de recuperação " +
                    "identificada na rede privada.");
            }
            else
            {
                foreach (var item in recuperacaoPrivadaUnica)
                {
                    resultado.AppendLine(
                        $"- {item}");
                }
            }

            // =====================================================
            // ESQUEMAS REGULARES
            // =====================================================

            if (regularesSUS.Count > 0 ||
                regularesPrivadas.Count > 0)
            {
                resultado.AppendLine();

                resultado.AppendLine(
                    "ESQUEMAS REGULARES");

                foreach (var item in regularesSUS
                    .Distinct())
                {
                    resultado.AppendLine(
                        $"[SUS] {item}");
                }

                foreach (var item in regularesPrivadas
                    .Distinct())
                {
                    resultado.AppendLine(
                        $"[Rede Privada] {item}");
                }
            }

            // =====================================================
            // ALERTAS
            // =====================================================

            resultado.AppendLine();

            resultado.AppendLine(
                "ALERTAS DE SEGURANÇA");

            if (alertas.Count == 0)
            {
                resultado.AppendLine(
                    "Nenhum alerta específico identificado " +
                    "pelos dados informados.");
            }
            else
            {
                foreach (var alerta in alertas)
                {
                    resultado.AppendLine(
                        $"- {alerta}");
                }
            }

            // =====================================================
            // CONTRAINDICAÇÕES
            // =====================================================

            resultado.AppendLine();

            resultado.AppendLine(
                "CONTRAINDICAÇÕES / PRECAUÇÕES");

            if (contraindicacoes.Count == 0)
            {
                resultado.AppendLine(
                    "Nenhuma contraindicação ou precaução " +
                    "específica identificada.");
            }
            else
            {
                foreach (var alerta in contraindicacoes)
                {
                    resultado.AppendLine(
                        $"- {alerta}");
                }
            }

            // =====================================================
            // VACINAS SUS
            // =====================================================

            resultado.AppendLine();

            resultado.AppendLine(
                "VACINAS DO SUS CONSIDERADAS");

            var vacinasSUS =
                aplicaveisSUS
                    .Where(r => r.IdadeMaximaMeses <= 0 || idadeMeses <= r.IdadeMaximaMeses) // <-- Filtra regras acima da idade do paciente
                    .GroupBy(r =>
                        NormalizarVacina(
                            r.NomeVacina))
                    .Where(g => g.Any(r => r.IdadeMaximaMeses <= 0 || idadeMeses <= r.IdadeMaximaMeses)) // <-- Garante que pelo menos uma dose válida pertença à faixa etária
                    .Select(g =>
                        g.First().NomeVacina)
                    .OrderBy(x => x)
                    .ToList();

            if (vacinasSUS.Count == 0)
            {
                resultado.AppendLine(
                    "Nenhuma regra SUS encontrada.");
            }
            else
            {
                foreach (var vacina in vacinasSUS)
                {
                    resultado.AppendLine(
                        $"- {vacina}");
                }
            }

            // =====================================================
            // VACINAS PRIVADAS
            // =====================================================

            resultado.AppendLine();

            resultado.AppendLine(
                "VACINAS DA REDE PRIVADA CONSIDERADAS");

            var vacinasPrivadas =
                aplicaveisPrivadas
                    .Where(r => r.IdadeMaximaMeses <= 0 || idadeMeses <= r.IdadeMaximaMeses) // <-- Filtra regras acima da idade do paciente
                    .GroupBy(r =>
                        NormalizarVacina(
                            r.NomeVacina))
                    .Where(g => g.Any(r => r.IdadeMaximaMeses <= 0 || idadeMeses <= r.IdadeMaximaMeses)) // <-- Garante que pelo menos uma dose válida pertença à faixa etária
                    .Select(g =>
                        g.First().NomeVacina)
                    .OrderBy(x => x)
                    .ToList();
            // =====================================================
            // OBSERVAÇÃO
            // =====================================================

            resultado.AppendLine();

            resultado.AppendLine(
                "OBSERVAÇÃO");

            resultado.AppendLine(
                "A análise é um apoio à regularização " +
                "vacinal e não substitui a avaliação " +
                "do serviço de saúde.");

            resultado.AppendLine(
                "Condições clínicas especiais podem exigir " +
                "avaliação em CRIE/RIE.");

            resultado.AppendLine(
                "O limite de aplicações por via apresentado " +
                "nesta análise corresponde à regra operacional " +
                "configurada no aplicativo e não deve ser " +
                "interpretado isoladamente como protocolo " +
                "nacional de vacinação.");

            return resultado.ToString();
        }

        // =========================================================
        // ANALISAR CALENDÁRIO
        // =========================================================

        private void AnalisarCalendario(
            string calendario,
            List<RegraVacinal> aplicaveis,
            List<HistoricoVacinal> historico,
            int idadeMeses,
            string grupo,
            Paciente paciente,
            List<ResultadoVacinal> pendentes,
            List<ResultadoVacinal> atrasadas,
            List<string> regulares,
            List<string> recuperacao,
            ref int aplicacoesSC,
            ref int aplicacoesIM,
            ref int aplicacoesOral,
            int limiteSC,
            int limiteIM)
        {
            if (aplicaveis == null ||
                aplicaveis.Count == 0)
            {
                return;
            }

            var dosesPorVacina =
                ObterDosesTomadas(historico);

            foreach (var grupoVacina in aplicaveis
                .GroupBy(r =>
                    NormalizarVacina(
                        r.NomeVacina)))
            {
                var regrasVacina =
                    grupoVacina
                        .OrderBy(r =>
                            r.NumeroDose)
                        .ToList();

                if (regrasVacina.Count == 0)
                    continue;

                string chaveVacina =
                    grupoVacina.Key;

                // -------------------------------------------------
                // VACINAS PERIÓDICAS
                //
                // São tratadas exclusivamente por
                // AjustarPeriodicas para não consumir
                // o limite duas vezes.
                // -------------------------------------------------

                bool possuiPeriodicidade =
                    regrasVacina.Any(r =>
                        EhPeriodicidadeValida(
                            r.Periodicidade));

                if (possuiPeriodicidade &&
                    chaveVacina != "dt")
                {
                    continue;
                }

                int dosesTomadasVacina =
                    dosesPorVacina.TryGetValue(
                        chaveVacina,
                        out int quantidadeDoses)
                        ? quantidadeDoses
                        : 0;

                int totalDosesNecessarias =
                    ObterTotalDosesNecessarias(
                        regrasVacina,
                        grupo,
                        paciente);

                if (totalDosesNecessarias <= 0)
                {
                    totalDosesNecessarias =
                        regrasVacina
                            .Where(r =>
                                r.NumeroDose > 0)
                            .Select(r =>
                                r.NumeroDose)
                            .DefaultIfEmpty(1)
                            .Max();
                }

                // =================================================
                // dT
                // =================================================

                if (chaveVacina == "dt")
                {
                    AnalisarDuplaAdulto(
                        calendario,
                        regrasVacina,
                        historico,
                        dosesTomadasVacina,
                        idadeMeses,
                        pendentes,
                        atrasadas,
                        regulares,
                        recuperacao,
                        ref aplicacoesSC,
                        ref aplicacoesIM,
                        ref aplicacoesOral,
                        limiteSC,
                        limiteIM);

                    continue;
                }

                // =================================================
                // ESQUEMA COMPLETO
                // =================================================

                if (dosesTomadasVacina >=
                    totalDosesNecessarias)
                {
                    if (!possuiPeriodicidade)
                    {
                        regulares.Add(
                            $"{regrasVacina[0].NomeVacina}: " +
                            $"esquema completo com " +
                            $"{dosesTomadasVacina} dose(s) registrada(s).");
                    }

                    continue;
                }

                // =================================================
                // PRÓXIMA DOSE
                // =================================================
                int proximaDose =
                                    dosesTomadasVacina + 1;

                RegraVacinal? regraProxima =
                    regrasVacina
                        .FirstOrDefault(r =>
                            r.NumeroDose ==
                            proximaDose);

                if (regraProxima == null)
                    continue;

                // =================================================
                // GESTAÇÃO
                // =================================================

                if (!RegraPodeSerAplicadaNaGestacao(
                    regraProxima,
                    paciente))
                {
                    continue;
                }

                string descricaoIntervalo =
                    string.IsNullOrWhiteSpace(
                        regraProxima.IntervaloEntreDoses)
                        ? string.Empty
                        : $" Intervalo mínimo informado: " +
                          $"{regraProxima.IntervaloEntreDoses}.";

                // =================================================
                // DOSE FUTURA
                // =================================================

                if (!EstaNaFaixaEtaria(
                    regraProxima,
                    idadeMeses))
                {
                    if (idadeMeses <
                        regraProxima.IdadeMinimaMeses)
                    {
                        pendentes.Add(
                            CriarResultado(
                                calendario,
                                regraProxima,
                                "DOSE FUTURA — ainda não " +
                                "atingiu a idade mínima."));
                    }

                    continue;
                }

                // =================================================
                // NENHUMA DOSE
                // =================================================

                if (dosesTomadasVacina == 0)
                {
                    bool liberada =
                        PodeAplicarNoAtendimento(
                            regraProxima,
                            idadeMeses,
                            ref aplicacoesSC,
                            ref aplicacoesIM,
                            ref aplicacoesOral,
                            limiteSC,
                            limiteIM);

                    if (liberada)
                    {
                        pendentes.Add(
                            CriarResultado(
                                calendario,
                                regraProxima,
                                "LIBERADA PARA ESTE " +
                                "ATENDIMENTO — nenhuma dose " +
                                "registrada no histórico."));

                        AdicionarRecuperacao(
                            recuperacao,
                            regraProxima,
                            totalDosesNecessarias,
                            descricaoIntervalo);
                    }
                    else
                    {
                        AdicionarBloqueioPorLimite(
                            calendario,
                            regraProxima,
                            pendentes,
                            recuperacao);
                    }

                    continue;
                }

                // =================================================
                // ESQUEMA INCOMPLETO
                // =================================================

                bool intervaloVencido =
                    IntervaloJaVencido(
                        historico,
                        regraProxima);

                // -------------------------------------------------
                // IMPORTANTE:
                //
                // Se o intervalo ainda NÃO venceu, a dose é apenas
                // pendente. Não consome vaga SC/IM.
                // -------------------------------------------------

                if (!intervaloVencido &&
                    !string.IsNullOrWhiteSpace(
                        regraProxima.IntervaloEntreDoses))
                {
                    pendentes.Add(
                        CriarResultado(
                            calendario,
                            regraProxima,
                            "PENDENTE — esquema incompleto; " +
                            "o intervalo mínimo para a próxima " +
                            "dose ainda não foi atingido."));

                    continue;
                }

                bool liberadaIncompleta =
                    PodeAplicarNoAtendimento(
                        regraProxima,
                        idadeMeses,
                        ref aplicacoesSC,
                        ref aplicacoesIM,
                        ref aplicacoesOral,
                        limiteSC,
                        limiteIM);

                if (!liberadaIncompleta)
                {
                    AdicionarBloqueioPorLimite(
                        calendario,
                        regraProxima,
                        pendentes,
                        recuperacao);

                    continue;
                }

                if (intervaloVencido)
                {
                    atrasadas.Add(
                        CriarResultado(
                            calendario,
                            regraProxima,
                            "LIBERADA PARA ESTE ATENDIMENTO — " +
                            "ATRASADA — esquema incompleto e " +
                            "intervalo para a próxima dose " +
                            "já foi atingido."));
                }
                else
                {
                    pendentes.Add(
                        CriarResultado(
                            calendario,
                            regraProxima,
                            "LIBERADA PARA ESTE ATENDIMENTO — " +
                            "PENDENTE — esquema incompleto."));
                }

                recuperacao.Add(
                    $"{regraProxima.NomeVacina}: " +
                    $"realizar a {proximaDose}ª dose " +
                    $"para completar o esquema." +
                    descricaoIntervalo);
            }

            // =====================================================
            // VACINAS PERIÓDICAS
            // =====================================================

            AjustarPeriodicas(
                calendario,
                aplicaveis,
                historico,
                pendentes,
                atrasadas,
                regulares,
                recuperacao,
                idadeMeses,
                ref aplicacoesSC,
                ref aplicacoesIM,
                ref aplicacoesOral,
                limiteSC,
                limiteIM);
        }

        // =========================================================
        // CONTROLE DO LIMITE
        // =========================================================

        private bool PodeAplicarNoAtendimento(
            RegraVacinal regra,
            int idadeMeses,
            ref int aplicacoesSC,
            ref int aplicacoesIM,
            ref int aplicacoesOral,
            int limiteSC,
            int limiteIM)
        {
            string via =
                NormalizarViaAdministracao(
                    regra.ViaAdministracao);

            // -----------------------------------------------------
            // ORAL
            // -----------------------------------------------------

            if (via == "oral")
            {
                aplicacoesOral++;
                return true;
            }

            // -----------------------------------------------------
            // SC
            // -----------------------------------------------------

            if (via == "sc")
            {
                if (aplicacoesSC >= limiteSC)
                    return false;

                aplicacoesSC++;
                return true;
            }

            // -----------------------------------------------------
            // IM
            // -----------------------------------------------------

            if (via == "im")
            {
                if (aplicacoesIM >= limiteIM)
                    return false;

                aplicacoesIM++;
                return true;
            }

            // -----------------------------------------------------
            // VIA INTRADÉRMICA (ID) OU OUTRA VIA CONHECIDA
            // -----------------------------------------------------

            // ID não consome os contadores operacionais SC/IM.
            // A via precisa estar explicitamente cadastrada.
            if (via == "id")
                return true;

            // Via vazia/desconhecida nunca deve ser liberada como se
            // estivesse validada. O chamador exibirá o motivo correto.
            return false;
        }

        // =========================================================
        // CRIAR RESULTADO
        // =========================================================

        private ResultadoVacinal CriarResultado(
            string calendario,
            RegraVacinal regra,
            string motivo)
        {
            return new ResultadoVacinal
            {
                Regra = regra,
                Motivo = motivo,
                Calendario = calendario
            };
        }

        // =========================================================
        // BLOQUEIO POR LIMITE
        // =========================================================

        private void AdicionarBloqueioPorLimite(
            string calendario,
            RegraVacinal regra,
            List<ResultadoVacinal> pendentes,
            List<string> recuperacao)
        {
            string via = NormalizarViaAdministracao(regra.ViaAdministracao);

            // Diferencia cadastro incompleto de limite realmente atingido.
            if (string.IsNullOrWhiteSpace(via))
            {
                const string motivo =
                    "NÃO LIBERADA — via de administração não configurada. " +
                    "Revise o cadastro da vacina antes de definir a aplicação.";

                pendentes.Add(CriarResultado(calendario, regra, motivo));
                recuperacao.Add(
                    $"{regra.NomeVacina}: configurar e validar a via de administração.");
                return;
            }

            // Via conhecida, mas que não participa dos contadores SC/IM.
            if (via == "id")
            {
                pendentes.Add(CriarResultado(
                    calendario,
                    regra,
                    "NÃO LIBERADA — revisar a regra de aplicação e o cadastro da vacina."));
                recuperacao.Add(
                    $"{regra.NomeVacina}: revisar a regra operacional para a via intradérmica.");
                return;
            }

            string nomeVia = via switch
            {
                "sc" => "subcutânea (SC)",
                "im" => "intramuscular (IM)",
                "oral" => "oral",
                _ => "via de administração"
            };

            pendentes.Add(CriarResultado(
                calendario,
                regra,
                $"PENDENTE — limite operacional de aplicações da {nomeVia} " +
                "atingido neste atendimento. Programar para o próximo atendimento."));

            recuperacao.Add(
                $"{regra.NomeVacina}: programar a dose para o próximo atendimento " +
                $"devido ao limite operacional da {nomeVia}.");
        }

        // =========================================================
        // RECUPERAÇÃO
        // =========================================================

        private void AdicionarRecuperacao(
            List<string> recuperacao,
            RegraVacinal regra,
            int totalDosesNecessarias,
            string descricaoIntervalo)
        {
            if (totalDosesNecessarias > 1)
            {
                recuperacao.Add(
                    $"{regra.NomeVacina}: " +
                    $"iniciar esquema de " +
                    $"{totalDosesNecessarias} dose(s)." +
                    descricaoIntervalo);
            }
            else
            {
                recuperacao.Add(
                    $"{regra.NomeVacina}: " +
                    "realizar a dose indicada " +
                    "conforme o calendário configurado." +
                    descricaoIntervalo);
            }
        }

        // =========================================================
        // NORMALIZAR VIA
        // =========================================================

        private string NormalizarViaAdministracao(
            string? via)
        {
            if (string.IsNullOrWhiteSpace(via))
                return string.Empty;

            string texto =
                RemoverAcentos(via)
                    .Trim()
                    .ToLowerInvariant()
                    .Replace(".", " ")
                    .Replace("-", " ")
                    .Replace("_", " ");

            while (texto.Contains("  "))
            {
                texto =
                    texto.Replace("  ", " ");
            }

            // -----------------------------------------------------
            // INTRAMUSCULAR
            // -----------------------------------------------------

            if (texto == "im" ||
                texto == "i m" ||
                texto == "i.m" ||
                texto.Contains("intramuscular"))
            {
                return "im";
            }

            // -----------------------------------------------------
            // SUBCUTÂNEA
            // -----------------------------------------------------

            if (texto == "sc" ||
                texto == "s c" ||
                texto.Contains("subcutanea"))
            {
                return "sc";
            }

            // -----------------------------------------------------
            // INTRADÉRMICA
            // -----------------------------------------------------

            if (texto == "id" || texto.Contains("intradermica"))
                return "id";

            // -----------------------------------------------------
            // ORAL
            // -----------------------------------------------------

            if (texto == "oral" ||
                texto == "vo" ||
                texto == "v o" ||
                texto.Contains("via oral"))
            {
                return "oral";
            }

            return string.Empty;
        }

        // =========================================================
        // ANÁLISE DA dT
        // =========================================================

        private void AnalisarDuplaAdulto(
            string calendario,
            List<RegraVacinal> regrasVacina,
            List<HistoricoVacinal> historico,
            int dosesTomadas,
            int idadeMeses,
            List<ResultadoVacinal> pendentes,
            List<ResultadoVacinal> atrasadas,
            List<string> regulares,
            List<string> recuperacao,
            ref int aplicacoesSC,
            ref int aplicacoesIM,
            ref int aplicacoesOral,
            int limiteSC,
            int limiteIM)
        {
            if (regrasVacina.Count == 0)
                return;

            RegraVacinal? regraDose1 =
                regrasVacina.FirstOrDefault(r =>
                    r.NumeroDose == 1);

            RegraVacinal? regraDose2 =
                regrasVacina.FirstOrDefault(r =>
                    r.NumeroDose == 2);

            RegraVacinal? regraDose3 =
                regrasVacina.FirstOrDefault(r =>
                    r.NumeroDose == 3);

            const int totalBasico = 3;

            // =====================================================
            // NENHUMA DOSE
            // =====================================================

            if (dosesTomadas <= 0)
            {
                if (regraDose1 != null &&
                    EstaNaFaixaEtaria(
                        regraDose1,
                        idadeMeses))
                {
                    bool liberada =
                        PodeAplicarNoAtendimento(
                            regraDose1,
                            idadeMeses,
                            ref aplicacoesSC,
                            ref aplicacoesIM,
                            ref aplicacoesOral,
                            limiteSC,
                            limiteIM);

                    if (liberada)
                    {
                        pendentes.Add(
                            CriarResultado(
                                calendario,
                                regraDose1,
                                "LIBERADA PARA ESTE ATENDIMENTO — " +
                                "nenhuma dose registrada no histórico."));

                        recuperacao.Add(
                            "dT: iniciar esquema básico de 3 doses.");
                    }
                    else
                    {
                        AdicionarBloqueioPorLimite(
                            calendario,
                            regraDose1,
                            pendentes,
                            recuperacao);
                    }
                }

                return;
            }

            // =====================================================
            // UMA DOSE
            // =====================================================

            if (dosesTomadas == 1)
            {
                if (regraDose2 != null)
                {
                    bool intervaloVencido =
                        IntervaloJaVencido(
                            historico,
                            regraDose2);

                    if (!intervaloVencido &&
                        !string.IsNullOrWhiteSpace(
                            regraDose2.IntervaloEntreDoses))
                    {
                        pendentes.Add(
                            CriarResultado(
                                calendario,
                                regraDose2,
                                "PENDENTE — segunda dose do " +
                                "esquema básico da dT; o intervalo " +
                                "mínimo ainda não foi atingido."));

                        return;
                    }

                    bool liberada =
                        PodeAplicarNoAtendimento(
                            regraDose2,
                            idadeMeses,
                            ref aplicacoesSC,
                            ref aplicacoesIM,
                            ref aplicacoesOral,
                            limiteSC,
                            limiteIM);

                    if (!liberada)
                    {
                        AdicionarBloqueioPorLimite(
                            calendario,
                            regraDose2,
                            pendentes,
                            recuperacao);

                        return;
                    }

                    if (intervaloVencido)
                    {
                        atrasadas.Add(
                            CriarResultado(
                                calendario,
                                regraDose2,
                                "LIBERADA PARA ESTE ATENDIMENTO — " +
                                "ATRASADA — segunda dose do " +
                                "esquema básico da dT já pode ser realizada."));
                    }
                    else
                    {
                        pendentes.Add(
                            CriarResultado(
                                calendario,
                                regraDose2,
                                "LIBERADA PARA ESTE ATENDIMENTO — " +
                                "PENDENTE — segunda dose do " +
                                "esquema básico da dT."));
                    }

                    recuperacao.Add(
                        "dT: realizar a 2ª dose do esquema básico.");
                }

                return;
            }

            // =====================================================
            // DUAS DOSES
            // =====================================================

            if (dosesTomadas == 2)
            {
                if (regraDose3 != null)
                {
                    bool intervaloVencido =
                        IntervaloJaVencido(
                            historico,
                            regraDose3);

                    if (!intervaloVencido &&
                        !string.IsNullOrWhiteSpace(
                            regraDose3.IntervaloEntreDoses))
                    {
                        pendentes.Add(
                            CriarResultado(
                                calendario,
                                regraDose3,
                                "PENDENTE — terceira dose do " +
                                "esquema básico da dT; o intervalo " +
                                "mínimo ainda não foi atingido."));

                        return;
                    }

                    bool liberada =
                        PodeAplicarNoAtendimento(
                            regraDose3,
                            idadeMeses,
                            ref aplicacoesSC,
                            ref aplicacoesIM,
                            ref aplicacoesOral,
                            limiteSC,
                            limiteIM);

                    if (!liberada)
                    {
                        AdicionarBloqueioPorLimite(
                            calendario,
                            regraDose3,
                            pendentes,
                            recuperacao);

                        return;
                    }

                    if (intervaloVencido)
                    {
                        atrasadas.Add(
                            CriarResultado(
                                calendario,
                                regraDose3,
                                "LIBERADA PARA ESTE ATENDIMENTO — " +
                                "ATRASADA — terceira dose do " +
                                "esquema básico da dT já pode ser realizada."));
                    }
                    else
                    {
                        pendentes.Add(
                            CriarResultado(
                                calendario,
                                regraDose3,
                                "LIBERADA PARA ESTE ATENDIMENTO — " +
                                "PENDENTE — terceira dose do " +
                                "esquema básico da dT."));
                    }

                    recuperacao.Add(
                        "dT: realizar a 3ª dose para " +
                        "completar o esquema básico.");
                }

                return;
            }

            // =====================================================
            // TRÊS OU MAIS DOSES
            // =====================================================

            if (dosesTomadas >= totalBasico)
            {
                DateTime ultimaDt =
                    ObterUltimaAplicacaoVacina(
                        historico,
                        "dt");

                if (ultimaDt == default)
                {
                    if (regraDose3 != null)
                    {
                        regulares.Add(
                            "- dT: esquema básico com " +
                            $"{dosesTomadas} dose(s) registrada(s). " +
                            "A data da última aplicação não foi " +
                            "informada para calcular o próximo reforço.");
                    }

                    return;
                }

                DateTime proximoReforco =
                    ultimaDt.AddYears(10);

                if (proximoReforco <= DateTime.Today)
                {
                    if (regraDose3 != null)
                    {
                        bool liberada =
                            PodeAplicarNoAtendimento(
                                regraDose3,
                                idadeMeses,
                                ref aplicacoesSC,
                                ref aplicacoesIM,
                                ref aplicacoesOral,
                                limiteSC,
                                limiteIM);

                        if (liberada)
                        {
                            atrasadas.Add(
                                CriarResultado(
                                    calendario,
                                    regraDose3,
                                    $"LIBERADA PARA ESTE ATENDIMENTO — " +
                                    $"ATRASADA — esquema básico completo; " +
                                    $"última aplicação em " +
                                    $"{ultimaDt:dd/MM/yyyy}. " +
                                    $"O reforço de dT venceu em " +
                                    $"{proximoReforco:dd/MM/yyyy}."));

                            recuperacao.Add(
                                "dT: realizar reforço, pois já " +
                                "se passaram 10 anos desde a " +
                                "última dose registrada.");
                        }
                        else
                        {
                            AdicionarBloqueioPorLimite(
                                calendario,
                                regraDose3,
                                pendentes,
                                recuperacao);
                        }
                    }
                }
                else
                {
                    regulares.Add(
                        $"- dT: esquema básico completo " +
                        $"com {dosesTomadas} dose(s). " +
                        $"Última aplicação em " +
                        $"{ultimaDt:dd/MM/yyyy}; " +
                        $"próximo reforço previsto para " +
                        $"{proximoReforco:dd/MM/yyyy}.");
                }
            }
        }

        // =========================================================
        // REGRAS APLICÁVEIS
        // =========================================================

        private List<RegraVacinal> ObterRegrasAplicaveis(
            List<RegraVacinal> regras,
            int idadeMeses,
            Paciente paciente)
        {
            bool gestante =
                paciente.Gestante;

            string grupo =
                ObterGrupoEtario(idadeMeses);

            return regras
                .Where(r =>
                {
                    if (gestante)
                    {
                        if (!string.Equals(
                            r.GrupoEtario,
                            "Gestante",
                            StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        bool grupoCompativel =
                            string.Equals(
                                r.GrupoEtario,
                                grupo,
                                StringComparison.OrdinalIgnoreCase)
                            ||
                            string.Equals(
                                r.GrupoEtario,
                                "Todos",
                                StringComparison.OrdinalIgnoreCase)
                            ||
                            string.IsNullOrWhiteSpace(
                                r.GrupoEtario);

                        if (!grupoCompativel)
                            return false;
                    }

                    if (!GrupoEspecialAtendido(
                        r.GrupoEspecial,
                        paciente))
                    {
                        return false;
                    }

                    bool eReforcoDt =
                        NormalizarVacina(
                            r.NomeVacina) == "dt" &&
                        !string.IsNullOrWhiteSpace(
                            r.Periodicidade);

                    bool periodica =
                        EhPeriodicidadeValida(
                            r.Periodicidade) ||
                        eReforcoDt;

                    if (!periodica)
                    {
                        if (idadeMeses <
                            r.IdadeMinimaMeses)
                        {
                            return false;
                        }
                    }

                    // O teto etário máximo DEVE ser respeitado para todas as vacinas (inclusive as periódicas)
                    if (r.IdadeMaximaMeses > 0 &&
                        idadeMeses >
                        r.IdadeMaximaMeses)
                    {
                        return false;
                    }

                    if (r.SemanaGestacionalMinima > 0)
                    {
                        if (!gestante)
                            return false;

                        if (paciente.SemanasGestacao <
                            r.SemanaGestacionalMinima)
                        {
                            return false;
                        }
                    }

                    return true;
                })
                .ToList();
        }

        // =========================================================
        // GRUPO ESPECIAL
        // =========================================================

        private bool GrupoEspecialAtendido(
            string? grupo,
            Paciente paciente)
        {
            string g =
                RemoverAcentos(
                    grupo ?? string.Empty)
                    .Trim()
                    .ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(g) ||
                g == "geral" ||
                g == "todos")
            {
                return true;
            }

            if (g.Contains("gestante"))
                return paciente.Gestante;

            if (g.Contains("profissional"))
                return paciente.ProfissionalSaude;

            if (g.Contains("indigena"))
                return paciente.Indigena;

            if (g.Contains("grupo especial"))
            {
                return paciente.ProfissionalSaude ||
                       paciente.Indigena ||
                       paciente.AcamadoOuInstitucionalizado;
            }

            if (g.Contains("acamado") ||
                g.Contains("institucional"))
            {
                return paciente.AcamadoOuInstitucionalizado;
            }

            return false;
        }

        // =========================================================
        // TOTAL DE DOSES
        // =========================================================

        private int ObterTotalDosesNecessarias(
            List<RegraVacinal> regras,
            string grupo,
            Paciente paciente)
        {
            if (regras == null ||
                regras.Count == 0)
            {
                return 0;
            }

            int totalPorRegra =
                regras
                    .Where(r =>
                        r.TotalDoses > 0)
                    .Select(r =>
                        r.TotalDoses)
                    .DefaultIfEmpty(0)
                    .Max();

            int totalPorGrupo;

            if (grupo.Equals(
                "Adulto",
                StringComparison.OrdinalIgnoreCase))
            {
                totalPorGrupo =
                    regras
                        .Where(r =>
                            r.DosesNecessariasAdulto > 0)
                        .Select(r =>
                            r.DosesNecessariasAdulto)
                        .DefaultIfEmpty(0)
                        .Max();
            }
            else
            {
                totalPorGrupo =
                    regras
                        .Where(r =>
                            r.DosesNecessariasCrianca > 0)
                        .Select(r =>
                            r.DosesNecessariasCrianca)
                        .DefaultIfEmpty(0)
                        .Max();
            }

            int maiorNumeroDose =
                regras
                    .Where(r =>
                        r.NumeroDose > 0)
                    .Select(r =>
                        r.NumeroDose)
                    .DefaultIfEmpty(0)
                    .Max();

            return Math.Max(
                maiorNumeroDose,
                Math.Max(
                    totalPorRegra,
                    totalPorGrupo));
        }

        // =========================================================
        // FAIXA ETÁRIA
        // =========================================================

        private bool EstaNaFaixaEtaria(
            RegraVacinal regra,
            int idadeMeses)
        {
            if (idadeMeses <
                regra.IdadeMinimaMeses)
            {
                return false;
            }

            if (regra.IdadeMaximaMeses > 0 &&
                idadeMeses >
                regra.IdadeMaximaMeses)
            {
                return false;
            }

            return true;
        }

        // =========================================================
        // GESTAÇÃO
        // =========================================================

        private bool RegraPodeSerAplicadaNaGestacao(
            RegraVacinal regra,
            Paciente paciente)
        {
            if (!paciente.Gestante)
                return true;

            if (string.Equals(
                regra.GrupoEtario,
                "Gestante",
                StringComparison.OrdinalIgnoreCase))
            {
                if (regra.SemanaGestacionalMinima > 0 &&
                    paciente.SemanasGestacao <
                    regra.SemanaGestacionalMinima)
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        // =========================================================
        // INTERVALO
        // =========================================================

        private bool IntervaloJaVencido(
            List<HistoricoVacinal> historico,
            RegraVacinal regra)
        {
            if (string.IsNullOrWhiteSpace(
                regra.IntervaloEntreDoses))
            {
                return true;
            }

            DateTime ultimaAplicacao =
                historico
                    .Where(h =>
                        h.DosesTomadas > 0 &&
                        NormalizarVacina(
                            h.NomeVacina) ==
                        NormalizarVacina(
                            regra.NomeVacina))
                    .Select(h =>
                        h.DataUltimaAplicacao != default
                            ? h.DataUltimaAplicacao
                            : h.DataAplicacao)
                    .Where(d =>
                        d != default)
                    .OrderByDescending(d => d)
                    .FirstOrDefault();

            if (ultimaAplicacao == default)
                return false;

            int diasMinimos =
                ExtrairDiasIntervalo(
                    regra.IntervaloEntreDoses);

            if (diasMinimos <= 0)
                return true;

            DateTime dataPossivel =
                ultimaAplicacao.Date
                    .AddDays(diasMinimos);

            return dataPossivel <=
                   DateTime.Today;
        }

        // =========================================================
        // EXTRAIR DIAS
        // =========================================================

        private int ExtrairDiasIntervalo(
            string intervalo)
        {
            if (string.IsNullOrWhiteSpace(
                intervalo))
            {
                return 0;
            }

            string texto =
                RemoverAcentos(intervalo)
                    .ToLowerInvariant()
                    .Trim();

            if (texto.Contains("dia"))
            {
                return ExtrairPrimeiroNumero(texto);
            }

            if (texto.Contains("semana"))
            {
                return
                    ExtrairPrimeiroNumero(texto) * 7;
            }

            if (texto.Contains("mes"))
            {
                return
                    ExtrairPrimeiroNumero(texto) * 30;
            }

            if (texto.Contains("ano"))
            {
                return
                    ExtrairPrimeiroNumero(texto) * 365;
            }

            return 0;
        }

        // =========================================================
        // EXTRAIR PRIMEIRO NÚMERO
        // =========================================================

        private int ExtrairPrimeiroNumero(
            string texto)
        {
            var numero =
                new StringBuilder();

            foreach (char c in texto)
            {
                if (char.IsDigit(c))
                {
                    numero.Append(c);
                }
                else if (numero.Length > 0)
                {
                    break;
                }
            }

            if (int.TryParse(
                numero.ToString(),
                out int resultado))
            {
                return resultado;
            }

            return 0;
        }

        // =========================================================
        // ALERTAS
        // =========================================================

        private List<string> GerarAlertasDeSeguranca(
            Paciente paciente,
            List<RegraVacinal> regras)
        {
            var alertas =
                new List<string>();

            if (paciente.Lactante &&
                paciente.CriancaAmamentadaMenor6Meses &&
                regras.Any(r =>
                    NormalizarVacina(
                        r.NomeVacina) ==
                    "febre amarela"))
            {
                alertas.Add(
                    "FEBRE AMARELA + LACTAÇÃO: " +
                    "para lactante que amamenta criança " +
                    "menor de 6 meses, a vacinação deve " +
                    "ser avaliada pelo serviço de saúde.");
            }

            if (paciente.Gestante &&
                regras.Any(r =>
                    NormalizarVacina(
                        r.NomeVacina) ==
                    "febre amarela"))
            {
                alertas.Add(
                    "FEBRE AMARELA NA GESTAÇÃO: " +
                    "a indicação deve ser avaliada " +
                    "individualmente pelo serviço de saúde.");
            }

            if (paciente.FebreOuDoencaAguda)
            {
                alertas.Add(
                    "FEBRE OU DOENÇA AGUDA INFORMADA: " +
                    "a situação clínica deve ser avaliada " +
                    "antes da vacinação.");
            }

            if (TemCondicaoImunologica(paciente))
            {
                alertas.Add(
                    "CONDIÇÃO CLÍNICA ESPECIAL: " +
                    "há condição associada à imunidade " +
                    "informada. A indicação e a segurança " +
                    "de algumas vacinas devem ser avaliadas " +
                    "pelo serviço de saúde, podendo envolver " +
                    "CRIE/RIE.");
            }

            if (paciente.AlergiaGrave)
            {
                alertas.Add(
                    "ALERGIA GRAVE INFORMADA: " +
                    "verificar componentes da vacina e " +
                    "histórico de reação antes da administração.");
            }

            return alertas
                .Distinct()
                .ToList();
        }

        // =========================================================
        // CONTRAINDICAÇÕES
        // =========================================================

        private List<string>
            GerarContraindicacoesEPrecaucoes(
                Paciente paciente,
                List<RegraVacinal> regras)
        {
            var resultado =
                new List<string>();

            if (paciente.AlergiaGrave)
            {
                resultado.Add(
                    "Alergia grave: verificar " +
                    "contraindicação por componente ou " +
                    "reação anterior antes da aplicação.");
            }

            if (TemCondicaoImunologica(paciente))
            {
                foreach (var regra in regras
                    .Where(r =>
                        !string.IsNullOrWhiteSpace(
                            r.Contraindicacao)))
                {
                    string vacina =
                        NormalizarVacina(
                            regra.NomeVacina);

                    if (vacina is
                        "bcg" or
                        "rotavirus" or
                        "scr" or
                        "varicela" or
                        "febre amarela" or
                        "dengue")
                    {
                        resultado.Add(
                            $"{regra.NomeVacina}: condição " +
                            "imunológica informada exige " +
                            "avaliação específica antes " +
                            "da vacinação.");
                    }
                }
            }

            if (paciente.AlergiaGrave)
            {
                foreach (var regra in regras
                    .Where(r =>
                        !string.IsNullOrWhiteSpace(
                            r.Contraindicacao)))
                {
                    resultado.Add(
                        $"{regra.NomeVacina}: " +
                        $"{regra.Contraindicacao}");
                }
            }

            return resultado
                .Distinct()
                .ToList();
        }

        // =========================================================
        // CONDIÇÃO IMUNOLÓGICA
        // =========================================================

        private bool TemCondicaoImunologica(
            Paciente paciente)
        {
            return
                paciente.Imunossuprimido ||
                paciente.HIV ||
                paciente.Quimioterapia ||
                paciente.Transplantado ||
                paciente.UsoCorticoideOuImunobiologico;
        }

        // =========================================================
        // VACINAS PERIÓDICAS
        // =========================================================

        private void AjustarPeriodicas(
            string calendario,
            List<RegraVacinal> aplicaveis,
            List<HistoricoVacinal> historico,
            List<ResultadoVacinal> pendentes,
            List<ResultadoVacinal> atrasadas,
            List<string> regulares,
            List<string> recuperacao,
            int idadeMeses,
            ref int aplicacoesSC,
            ref int aplicacoesIM,
            ref int aplicacoesOral,
            int limiteSC,
            int limiteIM)
        {
            foreach (var regra in aplicaveis
                .Where(r =>
                    EhPeriodicidadeValida(
                        r.Periodicidade)))
            {
                if (NormalizarVacina(
                        regra.NomeVacina) == "dt")
                {
                    continue;
                }

                if (!EstaNaFaixaEtaria(regra, idadeMeses))
                {
                    continue;
                }

                var ultimo =
                    historico
                        .Where(h =>
                            h.DosesTomadas > 0 &&
                            NormalizarVacina(
                                h.NomeVacina) ==
                            NormalizarVacina(
                                regra.NomeVacina))
                        .Select(h =>
                            h.DataUltimaAplicacao != default
                                ? h.DataUltimaAplicacao
                                : h.DataAplicacao)
                        .Where(d =>
                            d != default)
                        .OrderByDescending(d => d)
                        .FirstOrDefault();

                // -------------------------------------------------
                // SEM HISTÓRICO
                // -------------------------------------------------

                if (ultimo == default)
                {
                    bool liberada =
                        PodeAplicarNoAtendimento(
                            regra,
                            idadeMeses,
                            ref aplicacoesSC,
                            ref aplicacoesIM,
                            ref aplicacoesOral,
                            limiteSC,
                            limiteIM);

                    if (liberada)
                    {
                        pendentes.Add(
                            CriarResultado(
                                calendario,
                                regra,
                                "LIBERADA PARA ESTE ATENDIMENTO — " +
                                "não há registro de aplicação."));
                    }
                    else
                    {
                        AdicionarBloqueioPorLimite(
                            calendario,
                            regra,
                            pendentes,
                            recuperacao);
                    }

                    continue;
                }

                int dias =
                    ObterDiasPeriodicidade(
                        regra.Periodicidade);

                if (dias <= 0)
                    continue;

                DateTime proximaData =
                    ultimo.Date.AddDays(dias);

                // -------------------------------------------------
                // ATRASADA
                // -------------------------------------------------

                if (proximaData <= DateTime.Today)
                {
                    bool liberada =
                        PodeAplicarNoAtendimento(
                            regra,
                            idadeMeses,
                            ref aplicacoesSC,
                            ref aplicacoesIM,
                            ref aplicacoesOral,
                            limiteSC,
                            limiteIM);

                    if (liberada)
                    {
                        atrasadas.Add(
                            CriarResultado(
                                calendario,
                                regra,
                                $"LIBERADA PARA ESTE ATENDIMENTO — " +
                                $"ATRASADA — última aplicação " +
                                $"em {ultimo:dd/MM/yyyy}; " +
                                $"próxima prevista para " +
                                $"{proximaData:dd/MM/yyyy}."));
                    }
                    else
                    {
                        AdicionarBloqueioPorLimite(
                            calendario,
                            regra,
                            pendentes,
                            recuperacao);
                    }
                }
                else
                {
                    regulares.Add(
                        $"- {regra.NomeVacina}: " +
                        $"última aplicação em " +
                        $"{ultimo:dd/MM/yyyy}; " +
                        $"próxima prevista para " +
                        $"{proximaData:dd/MM/yyyy}.");
                }
            }
        }

        // =========================================================
        // PERIODICIDADE
        // =========================================================

        private bool EhPeriodicidadeValida(
            string? periodicidade)
        {
            if (string.IsNullOrWhiteSpace(
                periodicidade))
            {
                return false;
            }

            string p =
                RemoverAcentos(periodicidade)
                    .Trim()
                    .ToLowerInvariant();

            return
                p.Contains("anual") ||
                p.Contains("semestral") ||
                p.Contains("trimestral") ||
                p.Contains("mensal");
        }

        private int ObterDiasPeriodicidade(
            string periodicidade)
        {
            string p =
                RemoverAcentos(periodicidade)
                    .Trim()
                    .ToLowerInvariant();

            if (p.Contains("anual"))
                return 365;

            if (p.Contains("semestral"))
                return 180;

            if (p.Contains("trimestral"))
                return 90;

            if (p.Contains("mensal"))
                return 30;

            return 0;
        }

        // =========================================================
        // HISTÓRICO DE DOSES
        // =========================================================

        private Dictionary<string, int>
            ObterDosesTomadas(
                List<HistoricoVacinal> historico)
        {
            var resultado =
                new Dictionary<string, int>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (var item in historico)
            {
                if (item.DosesTomadas <= 0)
                    continue;

                string vacina =
                    NormalizarVacina(
                        item.NomeVacina);

                if (string.IsNullOrWhiteSpace(vacina))
                    continue;

                int doses =
                    Math.Max(
                        0,
                        item.DosesTomadas);

                if (doses <= 0)
                    continue;

                if (resultado.TryGetValue(
                    vacina,
                    out int atual))
                {
                    resultado[vacina] =
                        atual + doses;
                }
                else
                {
                    resultado[vacina] =
                        doses;
                }
            }

            return resultado;
        }

        // =========================================================
        // ÚLTIMA APLICAÇÃO
        // =========================================================

        private DateTime ObterUltimaAplicacaoVacina(
            List<HistoricoVacinal> historico,
            string nomeVacina)
        {
            return historico
                .Where(h =>
                    h.DosesTomadas > 0 &&
                    NormalizarVacina(
                        h.NomeVacina) ==
                    NormalizarVacina(
                        nomeVacina))
                .Select(h =>
                    h.DataUltimaAplicacao != default
                        ? h.DataUltimaAplicacao
                        : h.DataAplicacao)
                .Where(d =>
                    d != default)
                .OrderByDescending(d => d)
                .FirstOrDefault();
        }

        private DateTime ObterUltimaDataAplicacao(
            List<HistoricoVacinal> historicos)
        {
            return historicos
                .Where(h =>
                    h.DosesTomadas > 0)
                .Select(h =>
                    h.DataUltimaAplicacao != default
                        ? h.DataUltimaAplicacao
                        : h.DataAplicacao)
                .Where(d =>
                    d != default)
                .OrderByDescending(d => d)
                .FirstOrDefault();
        }

        // =========================================================
        // HISTÓRICO DO PACIENTE
        // =========================================================

        private async Task<List<HistoricoVacinal>>
            ObterHistoricoPaciente(
                int pacienteId)
        {
            return
                (await _bancoDados
                    .Listar<HistoricoVacinal>())
                .Where(h =>
                    h.PacienteId == pacienteId &&
                    h.DosesTomadas > 0)
                .ToList();
        }

        // =========================================================
        // IDADE
        // =========================================================

        private int ObterIdadeMesesPaciente(
            Paciente paciente)
        {
            if (paciente.IdadeAnos > 0 ||
                paciente.IdadeMesesAdicionais > 0)
            {
                return Math.Max(
                    0,
                    paciente.IdadeTotalMeses);
            }

            if (paciente.DataNascimento != default)
            {
                return CalcularIdadeEmMeses(
                    paciente.DataNascimento);
            }

            return 0;
        }

        private string ObterIdadeFormatadaPaciente(
            Paciente paciente,
            int idadeMeses)
        {
            if (paciente.IdadeAnos > 0 ||
                paciente.IdadeMesesAdicionais > 0)
            {
                return paciente.IdadeFormatada;
            }

            if (paciente.DataNascimento != default)
            {
                return CalcularIdadeFormatada(
                    paciente.DataNascimento);
            }

            return FormatarIdadeMeses(
                idadeMeses);
        }

        private int CalcularIdadeEmMeses(
            DateTime nascimento)
        {
            DateTime hoje =
                DateTime.Today;

            int meses =
                (hoje.Year - nascimento.Year) * 12 +
                hoje.Month -
                nascimento.Month;

            if (hoje.Day <
                nascimento.Day)
            {
                meses--;
            }

            return Math.Max(
                0,
                meses);
        }

        private string CalcularIdadeFormatada(
            DateTime nascimento)
        {
            DateTime hoje =
                DateTime.Today;

            int anos =
                hoje.Year -
                nascimento.Year;

            if (nascimento.Date >
                hoje.AddYears(-anos))
            {
                anos--;
            }

            DateTime aniversario =
                nascimento.AddYears(anos);

            int meses =
                (hoje.Year -
                 aniversario.Year) * 12 +
                hoje.Month -
                aniversario.Month;

            if (hoje.Day <
                aniversario.Day)
            {
                meses--;
            }

            return
                $"{Math.Max(0, anos)} ano(s) e " +
                $"{Math.Max(0, meses)} mês(es)";
        }

        // =========================================================
        // GRUPO ETÁRIO
        // =========================================================

        private string ObterGrupoEtario(
            int idadeMeses)
        {
            if (idadeMeses < 120)
                return "Criança";

            if (idadeMeses < 300)
                return "Adolescente/Jovem";

            if (idadeMeses < 720)
                return "Adulto";

            return "Idoso";
        }

        private string FormatarIdadeMeses(
            int meses)
        {
            if (meses < 12)
            {
                return
                    $"{meses} mês(es)";
            }

            return
                $"{meses / 12} ano(s)";
        }

        // =========================================================
        // REMOVER DUPLICADOS
        // =========================================================

        private List<ResultadoVacinal>
            RemoverDuplicadosResultado(
                List<ResultadoVacinal> lista)
        {
            return lista
                .GroupBy(r =>
                    $"{r.Calendario}|" +
                    $"{NormalizarVacina(
                        r.Regra.NomeVacina)}|" +
                    $"{r.Regra.NumeroDose}")
                .Select(g =>
                    g.First())
                .ToList();
        }

        // =========================================================
        // SUS
        // =========================================================

        private bool EhSUS(
            RegraVacinal regra)
        {
            string tipo =
                NormalizarTipoCalendario(
                    regra.TipoCalendario);

            return tipo == "sus";
        }

        // =========================================================
        // REDE PRIVADA
        // =========================================================

        private bool EhRedePrivada(
            RegraVacinal regra)
        {
            string tipo =
                NormalizarTipoCalendario(
                    regra.TipoCalendario);

            return
                tipo == "rede privada" ||
                tipo == "privada" ||
                tipo == "redeprivada";
        }

        // =========================================================
        // TIPO CALENDÁRIO
        // =========================================================

        private string NormalizarTipoCalendario(
            string? tipo)
        {
            if (string.IsNullOrWhiteSpace(tipo))
                return string.Empty;

            string texto =
                RemoverAcentos(tipo)
                    .Trim()
                    .ToLowerInvariant()
                    .Replace("_", " ")
                    .Replace("-", " ");

            while (texto.Contains("  "))
            {
                texto =
                    texto.Replace("  ", " ");
            }

            return texto;
        }

        // =========================================================
        // NORMALIZAÇÃO DA VACINA
        // =========================================================

        private string NormalizarVacina(
            string? nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return string.Empty;

            string t =
                RemoverAcentos(nome)
                    .Trim()
                    .ToLowerInvariant()
                    .Replace("-", " ")
                    .Replace("_", " ");

            while (t.Contains("  "))
                t = t.Replace("  ", " ");

            if (t.Contains("hepatite b"))
                return "hepatite b";

            if (t.Contains("bcg"))
                return "bcg";

            if (t.Contains("pentavalente") ||
                t.Contains("penta"))
                return "pentavalente";

            if (t.Contains("vip") ||
                t.Contains("poliomielite inativada"))
                return "vip";

            if (t.Contains("rotavirus"))
                return "rotavirus";

            if (t.Contains("pneumococica 20"))
                return "pneumococica 20";

            if (t.Contains("pneumococica 10"))
                return "pneumococica 10";

            if (t.Contains("pneumococica 13"))
                return "pneumococica 13";

            if (t.Contains("pneumococica 23"))
                return "pneumococica 23";

            if (t.Contains("meningococica acwy"))
                return "meningococica acwy";

            if (t.Contains("meningococica c"))
                return "meningococica c";

            if (t.Contains("meningococica b"))
                return "meningococica b";

            if (t.Contains("influenza"))
                return "influenza";

            if (t.Contains("covid"))
                return "covid 19";

            if (t.Contains("febre amarela"))
                return "febre amarela";

            if (t.Contains("triviral") ||
                t.Contains("triplice viral") ||
                t == "scr")
                return "scr";

            if (t.Contains("dtpa"))
                return "dtpa";

            if (t == "dt" ||
                t.Contains("difteria e tetano"))
                return "dt";

            if (t.Contains("hpv"))
                return "hpv";

            if (t.Contains("dengue"))
                return "dengue";

            if (t.Contains("varicela"))
                return "varicela";

            if (t.Contains("hepatite a"))
                return "hepatite a";

            if (t.Contains("vssr") ||
                t.Contains("virus sincicial"))
                return "vssr";

            if (t.Contains("dtp"))
                return "dtp";

            return t;
        }

        // =========================================================
        // REMOVER ACENTOS
        // =========================================================

        private string RemoverAcentos(
            string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            string normalizado =
                texto.Normalize(
                    NormalizationForm.FormD);

            var sb =
                new StringBuilder();

            foreach (char c in normalizado)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c)
                    != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb
                .ToString()
                .Normalize(
                    NormalizationForm.FormC);
        }

 
// =========================================================
// NOME
// =========================================================

private async Task<string> ObterNomePacienteAsync(Paciente paciente)
        {
            if (paciente == null)
                return "Paciente";

            var pessoas = await _bancoDados.Listar<Pessoa>();

            Pessoa? pessoa = pessoas.FirstOrDefault(
                p => p.Id == paciente.Id);

            return string.IsNullOrWhiteSpace(pessoa?.Nome)
                ? "Paciente"
                : pessoa.Nome;
        }

        // =========================================================
        // RESULTADO
        // =========================================================

        private class ResultadoVacinal
        {
            public RegraVacinal Regra { get; set; } = null!;

            public string Motivo { get; set; } =
                string.Empty;

            public string Calendario { get; set; } =
                string.Empty;
        }
    }
}