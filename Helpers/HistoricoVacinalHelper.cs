using SQLite;
using maui_amarv.Models;

namespace maui_amarv.Helpers
{
    public class HistoricoVacinalHelper
    {
        private readonly SQLiteAsyncConnection _database;

    public HistoricoVacinalHelper(SQLiteAsyncConnection database)
        {
            _database = database;
        }

        // =====================================================
        // SALVAR
        // =====================================================

        public async Task<int> Salvar(HistoricoVacinal historico)
        {
            return await _database.InsertAsync(historico);
        }

        // =====================================================
        // ATUALIZAR
        // =====================================================

        public async Task<int> Atualizar(HistoricoVacinal historico)
        {
            return await _database.UpdateAsync(historico);
        }

        // =====================================================
        // LISTAR POR PACIENTE
        // =====================================================

        public Task<List<HistoricoVacinal>> ListarPorPaciente(int pacienteId)
        {
            return _database.Table<HistoricoVacinal>()
                            .Where(h => h.PacienteId == pacienteId)
                            .ToListAsync();
        }

        // =====================================================
        // LISTAR TODOS
        // =====================================================

        public Task<List<HistoricoVacinal>> ListarTodos()
        {
            return _database.Table<HistoricoVacinal>()
                            .ToListAsync();
        }

        // =====================================================
        // LISTAR POR PACIENTE E VACINA
        // =====================================================

        public async Task<HistoricoVacinal> BuscarPorPacienteEVacina(
            int pacienteId,
            string nomeVacina)
        {
            var historicos = await _database.Table<HistoricoVacinal>()
                .Where(h =>
                    h.PacienteId == pacienteId &&
                    h.NomeVacina == nomeVacina)
                .ToListAsync();

            return historicos
                .OrderByDescending(h => h.DataUltimaAplicacao)
                .FirstOrDefault();
        }

        // =====================================================
        // EXCLUIR HISTÓRICO DO PACIENTE
        // =====================================================

        public Task<int> ExcluirPorPaciente(int pacienteId)
        {
            return _database.Table<HistoricoVacinal>()
                            .Where(h => h.PacienteId == pacienteId)
                            .DeleteAsync();
        }

        // =====================================================
        // EXCLUIR UMA VACINA DO HISTÓRICO
        // =====================================================

        public async Task<int> ExcluirPorPacienteEVacina(
            int pacienteId,
            string nomeVacina)
        {
            var registros = await _database.Table<HistoricoVacinal>()
                .Where(h =>
                    h.PacienteId == pacienteId &&
                    h.NomeVacina == nomeVacina)
                .ToListAsync();

            int quantidade = 0;

            foreach (var registro in registros)
            {
                quantidade += await _database.DeleteAsync(registro);
            }

            return quantidade;
        }
    }
}
