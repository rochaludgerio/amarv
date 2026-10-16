using SQLite;
using maui_amarv.Models;

namespace maui_amarv.Helpers
{
    public class RegraVacinalHelper
    {
        private readonly SQLiteAsyncConnection _database;

        public RegraVacinalHelper(SQLiteAsyncConnection database)
        {
            _database = database;
        }

        public async Task<List<RegraVacinal>> Listar()
        {
            return await _database
                .Table<RegraVacinal>()
                .ToListAsync();
        }

        public async Task<RegraVacinal?> BuscarPorId(int id)
        {
            return await _database
                .FindAsync<RegraVacinal>(id);
        }

        public async Task<int> Salvar(RegraVacinal regra)
        {
            if (regra.Id == 0)
                return await _database.InsertAsync(regra);

            return await _database.UpdateAsync(regra);
        }

        public async Task<int> Excluir(RegraVacinal regra)
        {
            return await _database.DeleteAsync(regra);
        }

        public async Task Limpar()
        {
            await _database.DeleteAllAsync<RegraVacinal>();
        }

        public async Task InserirTodas(
            IEnumerable<RegraVacinal> regras)
        {
            foreach (var regra in regras)
            {
                await _database.InsertAsync(regra);
            }
        }

        public async Task Recarregar(
            Func<Task> carregarRegrasIniciais)
        {
            await Limpar();

            await carregarRegrasIniciais();
        }
    }
}