using SQLite;
using maui_amarv.Models;

namespace maui_amarv.Helpers
{
    public class SincronizacaoHelper
    {
        private readonly SQLiteAsyncConnection _database;

        public SincronizacaoHelper(SQLiteAsyncConnection database)
        {
            _database = database;
        }

        // SALVAR
        public Task<int> Salvar(Sincronizacao sincronizacao)
        {
            return _database.InsertAsync(sincronizacao);
        }

        // LISTAR TODAS AS SINCRONIZAÇÕES
        public Task<List<Sincronizacao>> Listar()
        {
            return _database
                .Table<Sincronizacao>()
                .OrderByDescending(s => s.DataSincronizacao)
                .ToListAsync();
        }

        // BUSCAR A ÚLTIMA SINCRONIZAÇÃO
        public Task<Sincronizacao> Ultima()
        {
            return _database
                .Table<Sincronizacao>()
                .OrderByDescending(s => s.DataSincronizacao)
                .FirstOrDefaultAsync();
        }

        // EXCLUIR
        public Task<int> Excluir(Sincronizacao sincronizacao)
        {
            return _database.DeleteAsync(sincronizacao);
        }

        // EXCLUIR TODAS
        public Task<int> ExcluirTodas()
        {
            return _database
                .Table<Sincronizacao>()
                .DeleteAsync();
        }
    }
}