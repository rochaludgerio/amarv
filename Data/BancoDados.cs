
using SQLite;
using maui_amarv.Models;
using maui_amarv.Helpers;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace maui_amarv.Data
{
    public class BancoDados
    {
        private readonly SQLiteAsyncConnection _database;

        // ============================================================
        // HELPERS EXISTENTES
        // ============================================================

        public PacienteHelper PacienteHelper { get; private set; }
        public HistoricoVacinalHelper HistoricoVacinalHelper { get; private set; }
        public SincronizacaoHelper SincronizacaoHelper { get; private set; }
        public BackupHelper BackupHelper { get; private set; }

        // ============================================================
        // DEMAIS HELPERS
        // ============================================================

        public UsuarioHelper UsuarioHelper { get; private set; }
        public SessaoHelper SessaoHelper { get; private set; }
        public CondicaoClinicaHelper CondicaoClinicaHelper { get; private set; }
        public VacinaHelper VacinaHelper { get; private set; }
        public DoseHelper DoseHelper { get; private set; }
        public RegraVacinalHelper RegraVacinalHelper { get; private set; }
        public AnaliseEsquemaVacinalHelper AnaliseEsquemaVacinalHelper { get; private set; }
        public RelatorioHelper RelatorioHelper { get; private set; }
        public DadosIniciaisHelper DadosIniciaisHelper { get; private set; }

        // ============================================================
        // LISTAR PESSOAS
        // ============================================================

        public Task<List<Pessoa>> ListarPessoasAsync()
        {
            return _database.Table<Pessoa>().ToListAsync();
        }

        // ============================================================
        // COMPATIBILIDADE
        // ============================================================

        public BackupHelper Backup => BackupHelper;

        // ============================================================
        // CONTROLE DA INICIALIZAÇÃO
        // ============================================================

        private Task? _inicializacaoBanco;

        // ============================================================
        // CONSTRUTOR
        // ============================================================

        public BancoDados(string caminhoBanco)
        {
            _database = new SQLiteAsyncConnection(caminhoBanco);

            PacienteHelper = new PacienteHelper(_database);
            HistoricoVacinalHelper = new HistoricoVacinalHelper(_database);
            SincronizacaoHelper = new SincronizacaoHelper(_database);
            BackupHelper = new BackupHelper(_database);

            UsuarioHelper = new UsuarioHelper(_database);
            SessaoHelper = new SessaoHelper(_database);
            CondicaoClinicaHelper = new CondicaoClinicaHelper(_database);
            VacinaHelper = new VacinaHelper(_database);
            DoseHelper = new DoseHelper(_database);
            RegraVacinalHelper = new RegraVacinalHelper(_database);

            AnaliseEsquemaVacinalHelper =
                new AnaliseEsquemaVacinalHelper(_database);

            RelatorioHelper = new RelatorioHelper(_database);
            DadosIniciaisHelper = new DadosIniciaisHelper(_database);
        }

        // ============================================================
        // INICIALIZAÇÃO DO BANCO
        // ============================================================

        public Task InicializarAsync()
        {
            return _inicializacaoBanco ??= CriarTabelasAsync();
        }

        // ============================================================
        // GARANTIR COLUNA NOMEPACIENTE
        // ============================================================

        private async Task GarantirColunaNomePacienteAsync()
        {
            try
            {
                var colunas =
                    await _database.QueryAsync<ColunaSQLite>(
                        "PRAGMA table_info('AnaliseEsquemaVacinal');");

                bool existe = colunas.Any(c =>
                    string.Equals(
                        c.Name,
                        "NomePaciente",
                        StringComparison.OrdinalIgnoreCase));

                if (!existe)
                {
                    await _database.ExecuteAsync(
                        "ALTER TABLE AnaliseEsquemaVacinal " +
                        "ADD COLUMN NomePaciente TEXT NOT NULL DEFAULT '';");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Erro ao verificar NomePaciente: {ex}");
            }
        }

        // ============================================================
        // EXIBIR ESTRUTURA DAS TABELAS
        // ============================================================

        private async Task ExibirEstruturaTabelasAsync()
        {
            try
            {
                string[] tabelas =
                {
                    "Pessoa",
                    "Paciente",
                    "UsuarioDoSistema",
                    "AdministradorDoSistema",
                    "Sessao",
                    "HistoricoVacinal"
                };

                foreach (string tabela in tabelas)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"========== ESTRUTURA: {tabela} ==========");

                    var colunas =
                        await _database.QueryAsync<ColunaSQLite>(
                            $"PRAGMA table_info(\"{tabela}\");");

                    foreach (var coluna in colunas)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"Coluna: {coluna.Name} | " +
                            $"Tipo: {coluna.Type} | " +
                            $"PK: {coluna.PrimaryKey} | " +
                            $"Obrigatória: {coluna.NotNull}");
                    }

                    var chaves =
                        await _database.QueryAsync<ChaveEstrangeiraSQLite>(
                            $"PRAGMA foreign_key_list(\"{tabela}\");");

                    foreach (var chave in chaves)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"FK: {chave.From} -> " +
                            $"{chave.Table}.{chave.To}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Erro ao exibir a estrutura das tabelas: {ex}");
            }
        }

        // ============================================================
        // CRIAÇÃO DAS TABELAS E DIAGNÓSTICO
        // ============================================================

        private async Task CriarTabelasAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine(
                    "### DIAGNÓSTICO: entrou em CriarTabelasAsync ###");

                // 1. Habilitar chaves estrangeiras nesta conexão.

                await _database.ExecuteAsync(
                    "PRAGMA foreign_keys = ON;");

                int foreignKeysInicial =
                    await _database.ExecuteScalarAsync<int>(
                        "PRAGMA foreign_keys;");

                System.Diagnostics.Debug.WriteLine(
                    $"SQLite: foreign_keys inicial = {foreignKeysInicial}");

                // 2. Tabelas principais.

                await _database.CreateTableAsync<Pessoa>();
                await _database.CreateTableAsync<UsuarioDoSistema>();
                await _database.CreateTableAsync<AdministradorDoSistema>();
                await _database.CreateTableAsync<Paciente>();

                // 3. Demais tabelas.

                await _database.CreateTableAsync<Sessao>();

                await _database.CreateTableAsync<CondicaoClinica>();
                await _database.CreateTableAsync<Vacina>();
                await _database.CreateTableAsync<Dose>();

                await _database.CreateTableAsync<CalendarioVacinal>();
                await _database.CreateTableAsync<CalendarioVacinalSUS>();
                await _database.CreateTableAsync<CalendarioVacinalRedePrivada>();

                await _database.CreateTableAsync<RegraVacinal>();

                await _database.CreateTableAsync<HistoricoVacinal>();
                await _database.CreateTableAsync<AnaliseEsquemaVacinal>();

                await _database.CreateTableAsync<Relatorio>();
                await _database.CreateTableAsync<RelatorioVacinalPaciente>();
                await _database.CreateTableAsync<RelatorioPacientesEmSituacaoRegular>();
                await _database.CreateTableAsync<RelatorioPacienteComVacinasAtrasadas>();

                await _database.CreateTableAsync<Sincronizacao>();
                await _database.CreateTableAsync<Backup>();

                System.Diagnostics.Debug.WriteLine(
                    "### DIAGNÓSTICO: criação das tabelas concluída ###");

                // 4. Ajustes existentes.

                await GarantirColunaNomePacienteAsync();

                // Mantém o método de limpeza já existente no projeto.
                await LimparHistoricosSemDose();

                // 5. Listar tabelas reais do SQLite.

                var tabelas =
                    await _database.QueryAsync<TabelaSQLite>(
                        @"SELECT name
                          FROM sqlite_master
                          WHERE type = 'table'
                          ORDER BY name;");

                System.Diagnostics.Debug.WriteLine(
                    "========== TABELAS REAIS DO SQLITE ==========");

                foreach (var tabela in tabelas)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Tabela encontrada: {tabela.Name}");
                }

                // 6. Exibir colunas e chaves estrangeiras.

                await ExibirEstruturaTabelasAsync();

                System.Diagnostics.Debug.WriteLine(
                    "### DIAGNÓSTICO: terminou ExibirEstruturaTabelasAsync ###");

                // 7. Verificar integridade referencial.

                int foreignKeysAtivas =
                    await _database.ExecuteScalarAsync<int>(
                        "PRAGMA foreign_keys;");

                System.Diagnostics.Debug.WriteLine(
                    $"### DIAGNÓSTICO: foreign_keys = {foreignKeysAtivas} ###");

                var violacoes =
                    await _database.QueryAsync<ResultadoIntegridadeSQLite>(
                        "PRAGMA foreign_key_check;");

                System.Diagnostics.Debug.WriteLine(
                    $"SQLite: violações de chaves estrangeiras = {violacoes.Count}");

                foreach (var violacao in violacoes)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Violação: tabela={violacao.Table}, " +
                        $"registro={violacao.RowId}, " +
                        $"tabela referenciada={violacao.Parent}, " +
                        $"FK={violacao.ForeignKeyId}");
                }

                // 8. Carregar usuários iniciais.

                await UsuarioHelper.CarregarUsuariosIniciais();

                System.Diagnostics.Debug.WriteLine(
                    "### DIAGNÓSTICO: carregamento de usuários concluído ###");

                // 9. Verificar versão do calendário.

                const string versaoCalendario = "2026.7";

                string versaoAtual = Preferences.Get(
                    "AmarV_Calendario_Versao",
                    "");

                int totalRegras = await _database
                    .Table<RegraVacinal>()
                    .CountAsync();

                System.Diagnostics.Debug.WriteLine(
                    $"Calendário: versão salva = '{versaoAtual}'");

                System.Diagnostics.Debug.WriteLine(
                    $"Calendário: regras existentes = {totalRegras}");

                if (versaoAtual != versaoCalendario || totalRegras == 0)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "### DIAGNÓSTICO: carregando regras iniciais ###");

                    // Mantém o método de carregamento existente.
                    await CarregarRegrasIniciais();

                    Preferences.Set(
                        "AmarV_Calendario_Versao",
                        versaoCalendario);
                }

                System.Diagnostics.Debug.WriteLine(
                    "### TESTE: CriarTabelasAsync chegou ao final ###");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "### ERRO AO INICIALIZAR O BANCO SQLITE ###");

                System.Diagnostics.Debug.WriteLine(ex.ToString());

                throw;
            }
        }

        // ============================================================
        // CLASSE AUXILIAR: TABELAS DO SQLITE
        // CORRIGE O ERRO CS0246 DE TabelaSQLite
        // ============================================================

        private class TabelaSQLite
        {
            [Column("name")]
            public string Name { get; set; } = string.Empty;
        }

        // ============================================================
        // CLASSE AUXILIAR: RESULTADO DA VERIFICAÇÃO DE INTEGRIDADE
        // ============================================================

        private class ResultadoIntegridadeSQLite
        {
            [Column("table")]
            public string Table { get; set; } = string.Empty;

            [Column("rowid")]
            public long? RowId { get; set; }

            [Column("parent")]
            public string Parent { get; set; } = string.Empty;

            [Column("fkid")]
            public int ForeignKeyId { get; set; }
        }

        // ============================================================
        // CLASSE AUXILIAR: CHAVES ESTRANGEIRAS
        // ============================================================

        private class ChaveEstrangeiraSQLite
        {
            [Column("id")]
            public int Id { get; set; }

            [Column("seq")]
            public int Seq { get; set; }

            [Column("table")]
            public string Table { get; set; } = string.Empty;

            [Column("from")]
            public string From { get; set; } = string.Empty;

            [Column("to")]
            public string To { get; set; } = string.Empty;
        }

        // ============================================================
        // CLASSE AUXILIAR: COLUNAS DO SQLITE
        // ============================================================

        private class ColunaSQLite
        {
            [Column("cid")]
            public int Cid { get; set; }

            [Column("name")]
            public string Name { get; set; } = string.Empty;

            [Column("type")]
            public string Type { get; set; } = string.Empty;

            [Column("notnull")]
            public int NotNull { get; set; }

            [Column("dflt_value")]
            public string? DefaultValue { get; set; }

            [Column("pk")]
            public int PrimaryKey { get; set; }
        }


// ============================================================
// USUÁRIOS
// ============================================================

public async Task<UsuarioDoSistema?> BuscarUsuario(
            string login,
            string senha)
        {
            return await UsuarioHelper.BuscarUsuario(
                login,
                senha);
        }

        public async Task<UsuarioDoSistema?> BuscarUsuarioDoSistema(
            string login,
            string senha)
        {
            return await UsuarioHelper.BuscarUsuarioDoSistema(
                login,
                senha);
        }

        public async Task<List<UsuarioDoSistema>> ListarUsuarios()
        {
            return await UsuarioHelper.Listar();
        }

        public async Task CarregarUsuariosIniciais()
        {
            await UsuarioHelper.CarregarUsuariosIniciais();
        }

        public async Task<int> SalvarUsuario(
            UsuarioDoSistema usuario)
        {
            return await UsuarioHelper.Salvar(usuario);
        }

        public async Task<int> AtualizarUsuario(
            UsuarioDoSistema usuario)
        {
            return await UsuarioHelper.Salvar(usuario);
        }

        public async Task<int> ExcluirUsuario(
            UsuarioDoSistema usuario)
        {
            return await UsuarioHelper.Excluir(usuario);
        }

        // ============================================================
        // SESSÃO
        // ============================================================

        public async Task<int> SalvarSessao(Sessao sessao)
        {
            return await SessaoHelper.Salvar(sessao);
        }

        public async Task<Sessao?> ObterSessaoAtual()
        {
            return await SessaoHelper.ObterAtual();
        }

        public async Task LimparSessoes()
        {
            await SessaoHelper.Limpar();
        }


        // ============================================================
        // PEssoa salvar
        // ============================================================

        public async Task<int> SalvarPessoa(Pessoa pessoa)
        {
            if (pessoa == null)
                throw new ArgumentNullException(nameof(pessoa));

            if (string.IsNullOrWhiteSpace(pessoa.Nome))
                throw new InvalidOperationException("O nome é obrigatório.");

            string cpf = new string(
                (pessoa.Cpf ?? string.Empty)
                .Where(char.IsDigit)
                .ToArray());

            if (cpf.Length != 11)
                throw new InvalidOperationException(
                    "O CPF deve conter 11 dígitos.");

            // Impede o cadastro de duas pessoas com o mesmo CPF.
            var pessoas = await _database.Table<Pessoa>().ToListAsync();

            var existente = pessoas.FirstOrDefault(p =>
                new string((p.Cpf ?? string.Empty)
                    .Where(char.IsDigit)
                    .ToArray()) == cpf);

            if (existente != null && existente.Id != pessoa.Id)
                throw new InvalidOperationException(
                    "Já existe uma pessoa cadastrada com esse CPF.");

            pessoa.Cpf = cpf;

            if (pessoa.Id > 0)
            {
                await _database.UpdateAsync(pessoa);
                return pessoa.Id;
            }

            await _database.InsertAsync(pessoa);

            return pessoa.Id;
        }


        // ============================================================
        // excluir pessoa
        // ============================================================

        public async Task ExcluirPessoa(Pessoa pessoa)
        {
            if (pessoa == null || pessoa.Id <= 0)
            {
                throw new InvalidOperationException(
                    "Selecione uma pessoa válida para excluir.");
            }

            // Impede excluir uma pessoa que esteja cadastrada
            // também como paciente.
            var paciente = await _database
                .Table<Paciente>()
                .Where(p => p.Id == pessoa.Id)
                .FirstOrDefaultAsync();

            if (paciente != null)
            {
                throw new InvalidOperationException(
                    "Esta pessoa está vinculada a um paciente. " +
                    "Remova ou trate esse vínculo antes de excluir.");
            }

            await _database.DeleteAsync(pessoa);
        }

        // ============================================================
        // editar pessoa
        // ============================================================

     
public async Task<int> EditarPessoa(Pessoa pessoa)
        {
            if (pessoa == null)
                throw new ArgumentNullException(nameof(pessoa));

            if (pessoa.Id <= 0)
                throw new ArgumentException(
                    "O ID da pessoa é inválido.");

            if (string.IsNullOrWhiteSpace(pessoa.Nome))
                throw new ArgumentException(
                    "O nome da pessoa é obrigatório.");

            string cpf = new string(
                (pessoa.Cpf ?? string.Empty)
                    .Where(char.IsDigit)
                    .ToArray());

            if (cpf.Length != 11)
                throw new ArgumentException(
                    "O CPF deve conter 11 dígitos.");

            int resultado = 0;

            Action<SQLiteConnection> action = conn =>
            {
                // Localiza a pessoa pelo ID original.
                Pessoa? pessoaBanco = conn.Find<Pessoa>(pessoa.Id);

                if (pessoaBanco == null)
                    throw new InvalidOperationException(
                        "Pessoa não encontrada para edição.");

                // Verifica se o CPF já pertence a outra pessoa.
                var pessoasBanco = conn.Table<Pessoa>().ToList();

                bool cpfDuplicado = pessoasBanco.Any(p =>
                    p.Id != pessoa.Id &&
                    new string(
                        (p.Cpf ?? string.Empty)
                            .Where(char.IsDigit)
                            .ToArray()) == cpf);

                if (cpfDuplicado)
                    throw new InvalidOperationException(
                        "Já existe outra pessoa cadastrada com este CPF.");

                // Atualiza os dados sem alterar a chave primária.
                pessoaBanco.Nome = pessoa.Nome.Trim();
                pessoaBanco.Cpf = cpf;

                pessoaBanco.Nome_da_Mae =
                    pessoa.Nome_da_Mae?.Trim() ?? string.Empty;

                pessoaBanco.Telefone =
                    pessoa.Telefone?.Trim() ?? string.Empty;

                pessoaBanco.Endereco =
                    pessoa.Endereco?.Trim() ?? string.Empty;

                // Executa a atualização e guarda o resultado.
                resultado = conn.Update(pessoaBanco);
            };

            // Executa a atualização dentro da transação SQLite.
            await _database.RunInTransactionAsync(action);

            // Retorna a quantidade de registros atualizados.
            return resultado;
        }

        // ============================================================
        // PACIENTES
        // ============================================================

        public async Task<List<Paciente>> ListarPacientes()
        {
            return await PacienteHelper.Listar();
        }

        public async Task<Paciente?> BuscarPaciente(int id)
        {
            return await _database
                .Table<Paciente>()
                .Where(p => p.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<int> SalvarPaciente(
            Paciente paciente,
            Pessoa pessoa)
        {
            if (paciente == null)
                throw new ArgumentNullException(nameof(paciente));

            if (pessoa == null)
                throw new ArgumentNullException(nameof(pessoa));

            if (pessoa.Id <= 0)
                throw new ArgumentException(
                    "Selecione uma pessoa previamente cadastrada.");

            if (string.IsNullOrWhiteSpace(pessoa.Nome))
                throw new ArgumentException(
                    "O nome da pessoa é obrigatório.");

            string cpf = new string(
                (pessoa.Cpf ?? string.Empty)
                    .Where(char.IsDigit)
                    .ToArray());

            if (cpf.Length != 11)
                throw new ArgumentException(
                    "O CPF deve conter 11 dígitos.");

            // O paciente utiliza o mesmo ID da pessoa.
            if (paciente.Id != 0 && paciente.Id != pessoa.Id)
                throw new InvalidOperationException(
                    "O ID do paciente deve ser igual ao ID da pessoa.");

            paciente.Id = pessoa.Id;


            await _database.RunInTransactionAsync(conn =>
            {
                // Confirma que a pessoa existe no banco.
                Pessoa? pessoaBanco = conn.Find<Pessoa>(pessoa.Id);

                if (pessoaBanco == null)
                {
                    throw new InvalidOperationException(
                        "A pessoa selecionada não foi encontrada no banco de dados.");
                }

                if (string.IsNullOrWhiteSpace(pessoaBanco.Nome))
                {
                    throw new InvalidOperationException(
                        "O nome da pessoa cadastrada é obrigatório.");
                }

                string cpfBanco = new string(
                    (pessoaBanco.Cpf ?? string.Empty)
                        .Where(char.IsDigit)
                        .ToArray());

                if (cpfBanco.Length != 11)
                {
                    throw new InvalidOperationException(
                        "O CPF da pessoa cadastrada deve conter 11 dígitos.");
                }

                // Impede CPF duplicado em outra pessoa.
                var pessoasBanco = conn.Table<Pessoa>().ToList();

                bool cpfDuplicado = pessoasBanco.Any(p =>
                    p.Id != pessoa.Id &&
                    new string(
                        (p.Cpf ?? string.Empty)
                            .Where(char.IsDigit)
                            .ToArray()) == cpfBanco);

                if (cpfDuplicado)
                {
                    throw new InvalidOperationException(
                        "Já existe outra pessoa cadastrada com este CPF.");
                }

                // O ID do paciente é o mesmo ID da pessoa.
                paciente.Id = pessoaBanco.Id;

                Paciente? pacienteBanco =
                    conn.Find<Paciente>(pessoaBanco.Id);

                if (pacienteBanco == null)
                {
                    conn.Insert(paciente);
                }
                else
                {
                    conn.Update(paciente);
                }
            });

            return paciente.Id;
        }

        public async Task<int> EditarPaciente(
            Paciente paciente,
            Pessoa pessoa)
        {
            if (paciente == null)
                throw new ArgumentNullException(nameof(paciente));

            if (pessoa == null)
                throw new ArgumentNullException(nameof(pessoa));

            if (paciente.Id <= 0 || paciente.Id != pessoa.Id)
            {
                throw new ArgumentException(
                    "O ID do paciente deve ser igual ao ID da pessoa.");
            }

            var existente = await BuscarPaciente(paciente.Id);

            if (existente == null)
            {
                throw new InvalidOperationException(
                    "O paciente não foi encontrado para edição.");
            }

            // SalvarPaciente já faz a atualização quando o ID existe.
            return await SalvarPaciente(paciente, pessoa);
        }

        public async Task ExcluirPacienteComHistorico(int idPaciente)
        {
            if (idPaciente <= 0)
                throw new ArgumentException("ID de paciente inválido.");

            var paciente = await BuscarPaciente(idPaciente);

            if (paciente == null)
                throw new InvalidOperationException(
                    "Paciente não encontrado.");

            // Exclui o histórico antes do paciente.
            var historicos =
                await BuscarHistoricoPaciente(idPaciente);

            foreach (var historico in historicos)
            {
                await ExcluirHistoricoVacinal(historico);
            }

            // Remove somente o registro de Paciente.
            // Não remove Pessoa.
            await ExcluirPaciente(paciente);
        }


        public async Task<List<Paciente>> ConsultarPacientes()
        {
            return await _database
                .Table<Paciente>()
                .ToListAsync();
        }


        public async Task<int> ExcluirPaciente(Paciente paciente)
        {
            return await PacienteHelper.Excluir(paciente);
        }

        // ============================================================
        // LISTAR GENÉRICO
        // ============================================================

        public async Task<List<T>> Listar<T>()
            where T : new()
        {
            return await _database
                .Table<T>()
                .ToListAsync();
        }

        // ============================================================
        // RELATÓRIOS
        // ============================================================

        public async Task<int> SalvarRelatorioVacinalPaciente(
            RelatorioVacinalPaciente relatorio)
        {
            if (relatorio.Id == 0)
                return await _database.InsertAsync(relatorio);

            return await _database.UpdateAsync(relatorio);
        }

        public async Task<int> SalvarRelatorioPacientesRegulares(
            RelatorioPacientesEmSituacaoRegular relatorio)
        {
            if (relatorio.Id == 0)
                return await _database.InsertAsync(relatorio);

            return await _database.UpdateAsync(relatorio);
        }

        public async Task<int> SalvarRelatorioPacientesAtrasados(
            RelatorioPacienteComVacinasAtrasadas relatorio)
        {
            if (relatorio.Id == 0)
                return await _database.InsertAsync(relatorio);

            return await _database.UpdateAsync(relatorio);
        }

        // ============================================================
        // CONDIÇÕES CLÍNICAS
        // ============================================================

        public async Task<List<CondicaoClinica>>
            ListarCondicoesClinicas()
        {
            return await CondicaoClinicaHelper.Listar();
        }

        public async Task<int> SalvarCondicaoClinica(
            CondicaoClinica condicao)
        {
            return await CondicaoClinicaHelper.Salvar(condicao);
        }

        public async Task<CondicaoClinica?>
            BuscarCondicaoClinica(int id)
        {
            return await CondicaoClinicaHelper.BuscarPorId(id);
        }

        public async Task<int> ExcluirCondicaoClinica(
            CondicaoClinica condicao)
        {
            return await CondicaoClinicaHelper.Excluir(condicao);
        }

        public async Task<List<CondicaoClinica>>
            BuscarCondicoesPaciente(int pacienteId)
        {
            return await CondicaoClinicaHelper
                .BuscarPorPaciente(pacienteId);
        }

        // ============================================================
        // VACINAS
        // ============================================================

        public async Task<List<Vacina>> ListarVacinas()
        {
            return await VacinaHelper.Listar();
        }

        public async Task<int> SalvarVacina(Vacina vacina)
        {
            return await VacinaHelper.Salvar(vacina);
        }

        public async Task<Vacina?> BuscarVacina(int id)
        {
            return await VacinaHelper.BuscarPorId(id);
        }

        public async Task<int> ExcluirVacina(Vacina vacina)
        {
            return await VacinaHelper.Excluir(vacina);
        }

        // ============================================================
        // DOSES
        // ============================================================

        public async Task<List<Dose>> ListarDoses()
        {
            return await DoseHelper.Listar();
        }

        public async Task<int> SalvarDose(Dose dose)
        {
            return await DoseHelper.Salvar(dose);
        }

        public async Task<Dose?> BuscarDose(int id)
        {
            return await DoseHelper.BuscarPorId(id);
        }

        public async Task<int> ExcluirDose(Dose dose)
        {
            return await DoseHelper.Excluir(dose);
        }

        // ============================================================
        // HISTÓRICO VACINAL
        // ============================================================

        public async Task<List<HistoricoVacinal>>
            ListarHistoricoVacinal()
        {
            return await _database
                .Table<HistoricoVacinal>()
                .ToListAsync();
        }

        public async Task<List<HistoricoVacinal>>
            BuscarHistoricoPaciente(int pacienteId)
        {
            return await _database
                .Table<HistoricoVacinal>()
                .Where(h => h.PacienteId == pacienteId)
                .ToListAsync();
        }

        // ============================================================
        // SALVAR HISTÓRICO
        // ============================================================

        public async Task<int> SalvarHistoricoVacinal(
            HistoricoVacinal historico)
        {
            if (historico == null)
                return 0;

            // Uma dose com valor zero não é um registro válido.
            if (historico.DosesTomadas <= 0)
            {
                if (historico.Id > 0)
                    return await _database.DeleteAsync(historico);

                return 0;
            }

            if (historico.DataRegistro == default)
            {
                historico.DataRegistro = DateTime.Now;
            }

            if (historico.DataAplicacao == default)
            {
                if (historico.DataUltimaAplicacao != default)
                {
                    historico.DataAplicacao =
                        historico.DataUltimaAplicacao;
                }
                else
                {
                    historico.DataAplicacao =
                        DateTime.Today;
                }
            }

            if (historico.DataUltimaAplicacao == default)
            {
                historico.DataUltimaAplicacao =
                    historico.DataAplicacao;
            }

            // IMPORTANTE:
            // Uma linha do histórico representa uma dose.
            historico.DosesTomadas = 1;

            return await HistoricoVacinalHelper
                .Salvar(historico);
        }

        // ============================================================
        // EXCLUIR HISTÓRICO
        // ============================================================

        public async Task<int> ExcluirHistoricoVacinal(
            HistoricoVacinal historico)
        {
            if (historico == null)
                return 0;

            return await _database.DeleteAsync(historico);
        }

        public async Task<int> ExcluirHistoricoVacinalAsync(
            HistoricoVacinal historico)
        {
            return await ExcluirHistoricoVacinal(historico);
        }

        // ============================================================
        // BUSCAR HISTÓRICO PELO ID
        // ============================================================

        public async Task<HistoricoVacinal?>
            BuscarHistoricoVacinal(int id)
        {
            return await _database
                .Table<HistoricoVacinal>()
                .Where(h => h.Id == id)
                .FirstOrDefaultAsync();
        }

        // ============================================================
        // NOVO:
        // EXCLUIR HISTÓRICO DE UMA VACINA DE UM PACIENTE
        //
        // A busca é feita primeiro no SQLite e a comparação é feita
        // em memória para evitar problemas de tradução do LINQ.
        // ============================================================

        public async Task<int>
            ExcluirHistoricoPorPacienteEVacina(
                int pacienteId,
                string nomeVacina)
        {
            if (pacienteId <= 0 ||
                string.IsNullOrWhiteSpace(nomeVacina))
            {
                return 0;
            }

            var historicos =
                await BuscarHistoricoPaciente(pacienteId);

            string nomeNormalizado =
                NormalizarVacina(nomeVacina);

            var registros =
                historicos
                    .Where(h =>
                        NormalizarVacina(h.NomeVacina) ==
                        nomeNormalizado)
                    .ToList();

            int quantidade = 0;

            foreach (var registro in registros)
            {
                quantidade +=
                    await _database.DeleteAsync(registro);
            }

            return quantidade;
        }

        // ============================================================
        // NORMALIZAR NOME DE VACINA
        // ============================================================

        private static string NormalizarVacina(
            string? nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return string.Empty;

            string texto =
                nome.Trim()
                    .ToLowerInvariant()
                    .Replace("-", " ")
                    .Replace("_", " ");

            return texto
                .Replace(" ", "");
        }

        // ============================================================
        // LIMPAR REGISTROS COM ZERO DOSE
        // ============================================================

        public async Task LimparHistoricosSemDose()
        {
            try
            {
                await _database.ExecuteAsync(
                    "DELETE FROM HistoricoVacinal " +
                    "WHERE DosesTomadas <= 0");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Erro ao limpar históricos sem dose: {ex.Message}");
            }
        }

        // ============================================================
        // LIMPAR TODO O HISTÓRICO
        // ============================================================

        public async Task LimparHistorico()
        {
            await _database
                .DeleteAllAsync<HistoricoVacinal>();
        }

        // ============================================================
        // REGRAS VACINAIS
        // ============================================================

        public async Task<List<RegraVacinal>>
            ListarRegrasVacinais()
        {
            return await _database
                .Table<RegraVacinal>()
                .ToListAsync();
        }

        public async Task<List<RegraVacinal>>
            ListarRegrasPorTipo(string tipoCalendario)
        {
            return await _database
                .Table<RegraVacinal>()
                .Where(r =>
                    r.TipoCalendario == tipoCalendario)
                .ToListAsync();
        }

        public async Task<List<RegraVacinal>>
            ListarRegrasPorIdade(int idadeEmMeses)
        {
            return await _database
                .Table<RegraVacinal>()
                .Where(r =>
                    r.IdadeMinimaMeses <= idadeEmMeses &&
                    r.IdadeMaximaMeses >= idadeEmMeses)
                .ToListAsync();
        }

        public async Task<int> SalvarRegraVacinal(
            RegraVacinal regra)
        {
            if (regra.Id == 0)
                return await _database.InsertAsync(regra);

            return await _database.UpdateAsync(regra);
        }

        public async Task<int> AtualizarRegraVacinal(
            RegraVacinal regra)
        {
            return await _database.UpdateAsync(regra);
        }

        public async Task<RegraVacinal?>
            BuscarRegraVacinal(int id)
        {
            return await _database
                .Table<RegraVacinal>()
                .Where(r => r.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<int> ExcluirRegraVacinal(
            RegraVacinal regra)
        {
            return await _database.DeleteAsync(regra);
        }

        public async Task RecarregarRegrasVacinais()
        {
            await _database
                .DeleteAllAsync<RegraVacinal>();

            await CarregarRegrasIniciais();

            Preferences.Set(
                "AmarV_Calendario_Versao",
                "2026.1");
        }

        // ============================================================
        // CARREGAR REGRAS INICIAIS
        // ============================================================

        public async Task CarregarRegrasIniciais()
        {
            var regras = new List<RegraVacinal>();

            void Adicionar(
                string vacina,
                int dose,
                int total,
                int min,
                int max,
                string grupo,
                string indicacao,
                string contra =
                    "Anafilaxia a componente ou dose anterior.",
                string especial = "",
                string intervalo = "",
                string periodicidade = "",
                int semana = 0,
                string calendario = "SUS",
                string fonte =
                    "Calendário Nacional de Vacinação 2026",
                string observacao = "")
            {
                regras.Add(
                    new RegraVacinal
                    {
                        NomeVacina = vacina,
                        NumeroDose = dose,
                        TotalDoses = total,

                        DosesNecessariasCrianca = total,
                        DosesNecessariasAdulto = total,

                        IdadeMinimaMeses = min,
                        IdadeMaximaMeses = max,
                        IdadeReferenciaMeses = min,

                        GrupoEtario = grupo,
                        GrupoEspecial = especial,

                        SemanaGestacionalMinima = semana,

                        IntervaloEntreDoses = intervalo,
                        Periodicidade = periodicidade,

                        TipoCalendario = calendario,
                        FonteCalendario = fonte,

                        // Via usada pelo controle operacional do atendimento.
                        // COVID-19 deve ser conferida conforme o produto cadastrado.
                        ViaAdministracao = ObterViaAdministracao(vacina),

                        Indicacao = indicacao,
                        Contraindicacao = contra,

                        Observacao = observacao
                    });
            }

            // Define a via de administração das regras iniciais.
            // A via deve ser revisada quando houver produto/apresentação diferente.
            string ObterViaAdministracao(string nomeVacina)
            {
                string nome = nomeVacina.Trim().ToLowerInvariant();

                if (nome == "bcg") return "ID";
                if (nome == "rotavírus") return "Oral";
                if (nome == "febre amarela" || nome == "scr" ||
                    nome == "varicela" || nome == "dengue") return "SC";

                if (nome == "hepatite b" || nome == "pentavalente" ||
                    nome == "vip" || nome.StartsWith("pneumocócica") ||
                    nome.StartsWith("meningocócica") || nome == "influenza" ||
                    nome == "covid-19" || nome == "hepatite a" ||
                    nome == "dt" || nome == "dtp" || nome == "dtpa" || nome == "hpv" ||
                    nome == "vvsr") return "IM";

                // Não inventar uma via para vacinas não reconhecidas.
                return string.Empty;
            }

            /// ========================================================
            // CRIANÇA
            // ========================================================

            Adicionar(
                "Hepatite B", 1, 1, 0, 119,
                "Criança",
                "1 dose ao nascer.");

            Adicionar(
                "BCG", 1, 1, 0, 59,
                "Criança",
                "Dose única ao nascer.",
                "Imunodeficiência grave e outras contraindicações específicas devem ser avaliadas.");

            for (int d = 1; d <= 3; d++)
            {
                Adicionar(
                    "Pentavalente",
                    d,
                    3,
                    d == 1 ? 2 :
                    d == 2 ? 4 : 6,
                    83,
                    "Criança",
                    "Esquema primário aos 2, 4 e 6 meses.",
                    intervalo: d > 1 ? "60 dias" : "");
            }

            // Resgate para crianças de 7 a 9 anos (84 a 119 meses) sem esquema de DTP/Penta
            for (int d = 1; d <= 3; d++)
            {
                Adicionar(
                    "dT",
                    d,
                    3,
                    84, // Idade Mínima: 7 anos completos (84 meses)
                    119, // Idade Máxima da infância (até 9 anos e 11 meses)
                    "Criança",
                    d == 1
                        ? "1ª dose de dT para atualização vacinal (substitui DTP/Penta a partir dos 7 anos)."
                        : $"Dose {d} de dT para atualização do esquema vacinal.",
                    intervalo: d > 1 ? "60 dias" : "");
            }

            for (int d = 1; d <= 3; d++)
            {
                Adicionar(
                    "VIP",
                    d,
                    3,
                    d == 1 ? 2 :
                    d == 2 ? 4 : 6,
                    83,
                    "Criança",
                    "Esquema primário aos 2, 4 e 6 meses.",
                    intervalo: d > 1 ? "60 dias" : "");
            }

            Adicionar(
                "Rotavírus", 1, 2, 2, 11,
                "Criança",
                "1ª dose aos 2 meses, observando o limite máximo de 11 meses e 29 dias.",
                intervalo: "60 dias");

            Adicionar(
                "Rotavírus", 2, 2, 4, 23,
                "Criança",
                "2ª dose aos 4 meses, observando o limite máximo de 23 meses e 29 dias.",
                intervalo: "60 dias");

            Adicionar(
                "Pneumocócica 20-valente", 1, 1, 2, 23,
                "Criança",
                "1ª dose aos 2 meses.");

            Adicionar(
                "Pneumocócica 10-valente", 2, 2, 4, 23,
                "Criança",
                "2ª dose aos 4 meses.");

            Adicionar(
                "Pneumocócica 20-valente", 3, 3, 12, 59,
                "Criança",
                "Dose de reforço aos 12 meses.");

            Adicionar(
                "Meningocócica C", 1, 2, 3, 59,
                "Criança",
                "1ª dose aos 3 meses (resgate até 59 meses).");

            Adicionar(
                "Meningocócica C", 2, 2, 5, 59,
                "Criança",
                "2ª dose aos 5 meses (resgate até 59 meses).");

            Adicionar(
                "Meningocócica ACWY", 1, 1, 12, 59,
                "Criança",
                "1 dose aos 12 meses (resgate até 59 meses).");

            Adicionar(
                "Influenza", 1, 1, 6, 71,
                "Criança",
                "1 dose anual para crianças de 6 meses a menores de 6 anos.",
                periodicidade: "Anual");

            for (int d = 1; d <= 3; d++)
            {
                Adicionar(
                    "COVID-19",
                    d,
                    3,
                    d == 1 ? 6 :
                    d == 2 ? 7 : 9,
                    59,
                    "Criança",
                    "Esquema de 3 doses; observar o esquema vigente para a vacina utilizada.",
                    intervalo: d > 1 ? "30 dias" : "");
            }

            Adicionar(
                "Febre Amarela", 1, 2, 9, 119,
                "Criança",
                "1ª dose aos 9 meses.",
                observacao:
                    "Para viajantes, observar antecedência mínima de 10 dias.");

            Adicionar(
                "Febre Amarela", 2, 2, 48, 119,
                "Criança",
                "Dose de reforço aos 4 anos.");

            Adicionar(
                "SCR", 1, 2, 12, 119,
                "Criança",
                "1ª dose aos 12 meses.");

            Adicionar(
                "SCR", 2, 2, 15, 119,
                "Criança",
                "2ª dose aos 15 meses.",
                intervalo: "90 dias");

            // Rotina padrão: 1ª dose aos 15 meses
            Adicionar(
                "Varicela", 1, 2, 15, 59,
                "Criança",
                "1ª dose aos 15 meses.");

            // 2ª dose aos 4 anos (reforço)
            Adicionar(
                "Varicela", 2, 2, 48, 59,
                "Criança",
                "2ª dose aos 4 anos.");

            // Regra de resgate/atualização para crianças maiores sem histórico completo
            Adicionar(
                "Varicela", 1, 1, 60, 119,
                "Criança",
                "Dose única de resgate para varicela (suscetíveis dos 5 aos 9 anos).");

            Adicionar(
                "Hepatite A", 1, 1, 15, 59,
                "Criança",
                "Dose única aos 15 meses (resgate até 59 meses).");

            Adicionar(
                "DTP", 1, 2, 15, 47,
                "Criança",
                "1º reforço aos 15 meses.");

            Adicionar(
                "DTP", 2, 2, 48, 83,
                "Criança",
                "2º reforço aos 4 anos (limite até 83 meses).");

            Adicionar(
                "VIP", 4, 4, 15, 47,
                "Criança",
                "1º reforço aos 15 meses.");

            Adicionar(
                "VIP", 5, 5, 48, 83,
                "Criança",
                "2º reforço aos 4 anos (limite até 83 meses).");

            Adicionar(
                "HPV", 1, 1, 108, 179,
                "Criança",
                "1 dose conforme estratégia vigente (9 a 14 anos).");

            // ========================================================
            // ADOLESCENTE / JOVEM
            // ========================================================

            Adicionar(
                "HPV", 1, 1, 120, 299,
                "Adolescente/Jovem",
                "1 dose conforme histórico.");

            Adicionar(
                "Dengue", 1, 2, 120, 179,
                "Adolescente/Jovem",
                "1ª dose.",
                intervalo: "3 meses");

            Adicionar(
                "Dengue", 2, 2, 120, 179,
                "Adolescente/Jovem",
                "2ª dose.",
                intervalo: "3 meses");

            Adicionar(
                "Meningocócica ACWY", 1, 1, 132, 179,
                "Adolescente/Jovem",
                "1 dose conforme faixa etária e histórico.");

            for (int d = 1; d <= 3; d++)
            {
                Adicionar(
                    "Hepatite B",
                    d,
                    3,
                    120,
                    299,
                    "Adolescente/Jovem",
                    "3 doses conforme histórico vacinal.",
                    intervalo: d > 1
                        ? "Conforme histórico vacinal"
                        : "");
            }

            Adicionar(
                "Febre Amarela", 1, 1, 120, 299,
                "Adolescente/Jovem",
                "1 dose conforme histórico, residência e situação epidemiológica.");

            Adicionar(
                "SCR", 1, 2, 120, 299,
                "Adolescente/Jovem",
                "2 doses conforme histórico.");

            Adicionar(
                "SCR", 2, 2, 120, 299,
                "Adolescente/Jovem",
                "2ª dose conforme histórico.",
                intervalo: "90 dias");

            Adicionar(
                "dT", 1, 3, 120, 299,
                "Adolescente/Jovem",
                "Atualização do esquema conforme histórico.");

            Adicionar(
                "dT", 2, 3, 120, 299,
                "Adolescente/Jovem",
                "Atualização do esquema conforme histórico.",
                intervalo: "60 dias");

            Adicionar(
                "dT", 3, 3, 120, 299,
                "Adolescente/Jovem",
                "Completar o esquema de 3 doses.",
                intervalo: "60 dias",
                periodicidade: "10 anos");

            // ========================================================
            // ADULTO
            // ========================================================

            for (int d = 1; d <= 3; d++)
            {
                Adicionar(
                    "Hepatite B",
                    d,
                    3,
                    300,
                    719,
                    "Adulto",
                    "3 doses conforme histórico vacinal.");
            }

            Adicionar(
                "dT", 1, 3, 300, 719,
                "Adulto",
                "3 doses conforme histórico vacinal.");

            Adicionar(
                "dT", 2, 3, 300, 719,
                "Adulto",
                "3 doses conforme histórico vacinal.",
                intervalo: "60 dias");

            Adicionar(
                "dT", 3, 3, 300, 719,
                "Adulto",
                "Após esquema completo, reforço a cada 10 anos; antecipar para 5 anos em situação de risco.",
                periodicidade: "10 anos");

            Adicionar(
                "Febre Amarela", 1, 1, 300, 719,
                "Adulto",
                "1 dose conforme histórico, residência, viagem e situação epidemiológica.",
                observacao:
                    "Viajantes devem observar antecedência mínima de 10 dias.");

            Adicionar(
                "SCR", 1, 2, 300, 359,
                "Adulto",
                "Até 29 anos: 2 doses conforme histórico.");

            Adicionar(
                "SCR", 2, 2, 300, 359,
                "Adulto",
                "Até 29 anos: 2 doses conforme histórico.",
                intervalo: "90 dias");

            Adicionar(
                "SCR", 1, 1, 360, 719,
                "Adulto",
                "De 30 a 59 anos: 1 dose conforme histórico.");

            // ========================================================
            // IDOSO
            // ========================================================

            for (int d = 1; d <= 3; d++)
            {
                Adicionar(
                    "Hepatite B",
                    d,
                    3,
                    720,
                    9999,
                    "Idoso",
                    "3 doses conforme histórico vacinal.");
            }

            Adicionar(
                "dT", 1, 3, 720, 9999,
                "Idoso",
                "3 doses conforme histórico vacinal.");

            Adicionar(
                "dT", 2, 3, 720, 9999,
                "Idoso",
                "3 doses conforme histórico vacinal.",
                intervalo: "60 dias");

            Adicionar(
                "dT", 3, 3, 720, 9999,
                "Idoso",
                "Após esquema completo, reforço a cada 10 anos; antecipar para 5 anos em situação de risco.",
                periodicidade: "10 anos");

            Adicionar(
                "Febre Amarela", 1, 1, 720, 9999,
                "Idoso",
                "Somente em casos excepcionais para não vacinados em alto risco, após avaliação individual.");

            Adicionar(
                "Influenza", 1, 1, 720, 9999,
                "Idoso",
                "1 dose anual por temporada.",
                periodicidade: "Anual");

            Adicionar(
                "COVID-19", 1, 1, 720, 9999,
                "Idoso",
                "Dose conforme estratégia vigente.",
                periodicidade: "Semestral");

            // ========================================================
            // GESTANTE
            // ========================================================

            Adicionar(
                "Hepatite B", 1, 3, 120, 9999,
                "Gestante",
                "3 doses conforme histórico vacinal.");

            Adicionar(
                "Hepatite B", 2, 3, 120, 9999,
                "Gestante",
                "3 doses conforme histórico vacinal.",
                intervalo: "Conforme histórico");

            Adicionar(
                "Hepatite B", 3, 3, 120, 9999,
                "Gestante",
                "3 doses conforme histórico vacinal.",
                intervalo: "Conforme histórico");

            Adicionar(
                "dT", 1, 3, 120, 9999,
                "Gestante",
                "3 doses conforme histórico vacinal.");

            Adicionar(
                "dT", 2, 3, 120, 9999,
                "Gestante",
                "3 doses conforme histórico vacinal.",
                intervalo: "60 dias");

            Adicionar(
                "dT", 3, 3, 120, 9999,
                "Gestante",
                "3 doses conforme histórico vacinal.",
                intervalo: "60 dias");

            Adicionar(
                "Influenza", 1, 1, 120, 9999,
                "Gestante",
                "1 dose por temporada.",
                periodicidade: "Anual");

            Adicionar(
                "COVID-19", 1, 1, 120, 9999,
                "Gestante",
                "1 dose a cada gestação.");

            Adicionar(
                "dTpa", 1, 1, 120, 9999,
                "Gestante",
                "1 dose conforme semana gestacional.",
                semana: 20);

            Adicionar(
                "VVSR", 1, 1, 120, 9999,
                "Gestante",
                "1 dose conforme semana gestacional.",
                semana: 28);

            // ========================================================
            // REDE PRIVADA
            // ========================================================

            Adicionar(
                "Meningocócica B", 1, 2, 0, 9999,
                "Todos",
                "Disponível na rede privada conforme indicação e produto utilizado.",
                calendario: "Rede Privada",
                fonte: "Calendário da Rede Privada");

            Adicionar(
                "Meningocócica ACWY", 1, 1, 0, 9999,
                "Todos",
                "Disponível na rede privada conforme indicação e produto utilizado.",
                calendario: "Rede Privada",
                fonte: "Calendário da Rede Privada");

            Adicionar(
                "Pneumocócica 13", 1, 1, 0, 9999,
                "Todos",
                "Disponível na rede privada conforme indicação e produto utilizado.",
                calendario: "Rede Privada",
                fonte: "Calendário da Rede Privada");

            // ========================================================
            // SALVAR
            // ========================================================

            await RegraVacinalHelper.Limpar();

            foreach (var regra in regras)
            {
                await RegraVacinalHelper.Salvar(regra);
            }
        }

        // ============================================================
        // ANÁLISE
        // ============================================================

        public async Task<int> SalvarAnaliseVacinal(
            AnaliseEsquemaVacinal analise)
        {
            return await AnaliseEsquemaVacinalHelper
                .Salvar(analise);
        }

        public async Task<List<AnaliseEsquemaVacinal>>
            ListarAnalisesVacinais()
        {
            return await AnaliseEsquemaVacinalHelper
                .Listar();
        }

        public async Task<AnaliseEsquemaVacinal?>
            BuscarAnaliseVacinal(int id)
        {
            return await AnaliseEsquemaVacinalHelper
                .BuscarPorId(id);
        }

        public async Task<List<AnaliseEsquemaVacinal>>
            BuscarAnalisesPaciente(int pacienteId)
        {
            return await AnaliseEsquemaVacinalHelper
                .BuscarPorPaciente(pacienteId);
        }

        public async Task LimparAnalises()
        {
            await AnaliseEsquemaVacinalHelper.Limpar();
        }

        public async Task LimparAnalise()
        {
            await _database
                .DeleteAllAsync<AnaliseEsquemaVacinal>();
        }

        // ============================================================
        // RELATÓRIOS
        // ============================================================

        public async Task<List<Relatorio>>
            ListarRelatorios()
        {
            return await RelatorioHelper.Listar();
        }

        public async Task<int> SalvarRelatorio(
            Relatorio relatorio)
        {
            return await RelatorioHelper.Salvar(relatorio);
        }

        public async Task<List<RelatorioVacinalPaciente>>
            ListarRelatoriosVacinais()
        {
            return await RelatorioHelper.ListarVacinais();
        }

        public async Task<List<RelatorioPacienteComVacinasAtrasadas>>
            ListarRelatoriosPacientesAtrasados()
        {
            return await RelatorioHelper
                .ListarPacientesAtrasados();
        }

        public async Task<List<RelatorioPacientesEmSituacaoRegular>>
            ListarRelatoriosPacientesRegulares()
        {
            return await RelatorioHelper
                .ListarPacientesRegulares();
        }

        // ============================================================
        // SINCRONIZAÇÃO
        // ============================================================

        public async Task<List<Sincronizacao>>
            ListarSincronizacoes()
        {
            return await SincronizacaoHelper.Listar();
        }

        // ============================================================
        // BACKUP
        // ============================================================

        public async Task<List<Backup>>
            ListarBackups()
        {
            return await BackupHelper.Listar();
        }

        public async Task<int> SalvarBackup(
            Backup backup)
        {
            return await BackupHelper.Salvar(backup);
        }

        // ============================================================
        // SUBSTITUIR DADOS DA SINCRONIZAÇÃO
        // ============================================================

        public async Task SubstituirDados<T>(
            IEnumerable<T>? dados)
            where T : new()
        {
            if (dados == null)
                return;

            var lista = dados.ToList();

            if (lista.Count == 0)
                return;

            foreach (var item in lista)
            {
                try
                {
                    await _database
                        .InsertOrReplaceAsync(item);
                }
                catch
                {
                    try
                    {
                        await _database
                            .UpdateAsync(item);
                    }
                    catch
                    {
                        // Continua com os demais registros.
                    }
                }
            }
        }
    }
}