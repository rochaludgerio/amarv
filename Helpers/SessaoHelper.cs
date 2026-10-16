using SQLite;
using maui_amarv.Models;

namespace maui_amarv.Helpers
{
    public class SessaoHelper
    {
        private readonly SQLiteAsyncConnection _database;

        public SessaoHelper(SQLiteAsyncConnection database)
        {
            _database = database;
        }

        public async Task<int> Salvar(Sessao sessao)
        {
            return await _database.InsertAsync(sessao);
        }

        public async Task<Sessao?> ObterAtual()
        {
            return await _database
                .Table<Sessao>()
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();
        }

        public async Task Limpar()
        {
            await _database.DeleteAllAsync<Sessao>();
        }
    }
}