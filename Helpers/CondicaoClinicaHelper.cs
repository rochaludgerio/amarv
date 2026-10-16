using SQLite;
using maui_amarv.Models;

namespace maui_amarv.Helpers
{
    public class CondicaoClinicaHelper
    {
        private readonly SQLiteAsyncConnection _database;

        public CondicaoClinicaHelper(SQLiteAsyncConnection database)
        {
            _database = database;
        }

        public async Task<List<CondicaoClinica>> Listar()
        {
            return await _database
                .Table<CondicaoClinica>()
                .ToListAsync();
        }

        public async Task<CondicaoClinica?> BuscarPorId(int id)
        {
            return await _database
                .FindAsync<CondicaoClinica>(id);
        }

        public async Task<int> Salvar(CondicaoClinica condicao)
        {
            if (condicao.Id == 0)
                return await _database.InsertAsync(condicao);

            return await _database.UpdateAsync(condicao);
        }

        public async Task<int> Excluir(CondicaoClinica condicao)
        {
            return await _database.DeleteAsync(condicao);
        }

        public async Task<List<CondicaoClinica>> BuscarPorPaciente(
            int pacienteId)
        {
            /*
             * O modelo atual de CondicaoClinica informado
             * anteriormente não possui PacienteId.
             *
             * Por isso mantemos o comportamento atual do BancoDados.
             */
            return await _database
                .Table<CondicaoClinica>()
                .ToListAsync();
        }
    }
}