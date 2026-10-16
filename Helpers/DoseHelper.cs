using SQLite;
using maui_amarv.Models;

namespace maui_amarv.Helpers
{
    public class DoseHelper
    {
        private readonly SQLiteAsyncConnection _database;

        public DoseHelper(SQLiteAsyncConnection database)
        {
            _database = database;
        }

        public async Task<List<Dose>> Listar()
        {
            return await _database
                .Table<Dose>()
                .ToListAsync();
        }

        public async Task<Dose?> BuscarPorId(int id)
        {
            return await _database
                .FindAsync<Dose>(id);
        }

        public async Task<int> Salvar(Dose dose)
        {
            if (dose.Id == 0)
                return await _database.InsertAsync(dose);

            return await _database.UpdateAsync(dose);
        }

        public async Task<int> Excluir(Dose dose)
        {
            return await _database.DeleteAsync(dose);
        }
    }
}