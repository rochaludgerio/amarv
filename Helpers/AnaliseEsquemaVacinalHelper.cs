using SQLite;
using maui_amarv.Models;

namespace maui_amarv.Helpers
{
    public class AnaliseEsquemaVacinalHelper
    {
        private readonly SQLiteAsyncConnection _database;

        public AnaliseEsquemaVacinalHelper(
            SQLiteAsyncConnection database)
        {
            _database = database;
        }

        public async Task<int> Salvar(
            AnaliseEsquemaVacinal analise)
        {
            if (analise == null)
                return 0;

            if (analise.DataAnalise == default)
                analise.DataAnalise = DateTime.Now;

            if (analise.Id == 0)
                return await _database.InsertAsync(analise);

            return await _database.UpdateAsync(analise);
        }

        public async Task<List<AnaliseEsquemaVacinal>> Listar()
        {
            return await _database
                .Table<AnaliseEsquemaVacinal>()
                .ToListAsync();
        }

        public async Task<AnaliseEsquemaVacinal?> BuscarPorId(int id)
        {
            return await _database
                .FindAsync<AnaliseEsquemaVacinal>(id);
        }

        public async Task<List<AnaliseEsquemaVacinal>>
            BuscarPorPaciente(int pacienteId)
        {
            return await _database
                .Table<AnaliseEsquemaVacinal>()
                .Where(a => a.PacienteId == pacienteId)
                .ToListAsync();
        }

        public async Task Limpar()
        {
            await _database
                .DeleteAllAsync<AnaliseEsquemaVacinal>();
        }
    }
}