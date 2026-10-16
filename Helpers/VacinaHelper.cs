using SQLite;
using maui_amarv.Models;

namespace maui_amarv.Helpers
{
    public class VacinaHelper
    {
        private readonly SQLiteAsyncConnection _database;

        public VacinaHelper(SQLiteAsyncConnection database)
        {
            _database = database;
        }

        public async Task<List<Vacina>> Listar()
        {
            return await _database
                .Table<Vacina>()
                .ToListAsync();
        }

        public async Task<Vacina?> BuscarPorId(int id)
        {
            return await _database
                .FindAsync<Vacina>(id);
        }

        public async Task<int> Salvar(Vacina vacina)
        {
            if (vacina.Id == 0)
                return await _database.InsertAsync(vacina);

            return await _database.UpdateAsync(vacina);
        }

        public async Task<int> Excluir(Vacina vacina)
        {
            return await _database.DeleteAsync(vacina);
        }
    }
}
