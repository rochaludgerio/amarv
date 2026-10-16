using SQLite;
using maui_amarv.Models;

namespace maui_amarv.Helpers
{
    public class BackupHelper
    {
        private readonly SQLiteAsyncConnection _database;

        public BackupHelper(SQLiteAsyncConnection database)
        {
            _database = database;
        }

        // ==========================================
        // SALVAR BACKUP
        // ==========================================

        public Task<int> Salvar(Backup backup)
        {
            return _database.InsertAsync(backup);
        }

        // ==========================================
        // LISTAR BACKUPS
        // ==========================================

        public Task<List<Backup>> Listar()
        {
            return _database
                .Table<Backup>()
                .OrderByDescending(b => b.DataBackup)
                .ToListAsync();
        }

        // ==========================================
        // BUSCAR O ÚLTIMO BACKUP
        // ==========================================

        public Task<Backup> Ultimo()
        {
            return _database
                .Table<Backup>()
                .OrderByDescending(b => b.DataBackup)
                .FirstOrDefaultAsync();
        }

        // ==========================================
        // ATUALIZAR BACKUP
        // ==========================================

        public Task<int> Atualizar(Backup backup)
        {
            return _database.UpdateAsync(backup);
        }

        // ==========================================
        // EXCLUIR BACKUP
        // ==========================================

        public Task<int> Excluir(Backup backup)
        {
            return _database.DeleteAsync(backup);
        }

        // ==========================================
        // EXCLUIR TODOS OS BACKUPS
        // ==========================================

        public Task<int> ExcluirTodos()
        {
            return _database
                .Table<Backup>()
                .DeleteAsync();
        }
    }
}