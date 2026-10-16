using SQLite;
using maui_amarv.Models;

namespace maui_amarv.Helpers
{
    public class PacienteHelper
    {
        private readonly SQLiteAsyncConnection _database;

        public PacienteHelper(SQLiteAsyncConnection database)
        {
            _database = database;
        }

        // INSERT
        public Task<int> Salvar(Paciente paciente)
        {
            return _database.InsertAsync(paciente);
        }

        // SELECT
        public Task<List<Paciente>> Listar()
        {
            return _database.Table<Paciente>().ToListAsync();
        }

        // UPDATE
        public Task<int> Atualizar(Paciente paciente)
        {
            return _database.UpdateAsync(paciente);
        }

        // DELETE
        public Task<int> Excluir(Paciente paciente)
        {
            return _database.DeleteAsync(paciente);
        }
   
        public async Task<int> ExcluirComHistorico(Paciente paciente)
        {
            await _database.Table<HistoricoVacinal>()
                           .Where(h => h.PacienteId == paciente.Id)
                           .DeleteAsync();

            return await _database.DeleteAsync(paciente);
        }
    }
}