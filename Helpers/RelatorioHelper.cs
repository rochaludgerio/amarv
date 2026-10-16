using SQLite;
using maui_amarv.Models;

namespace maui_amarv.Helpers
{
    public class RelatorioHelper
    {
        private readonly SQLiteAsyncConnection _database;

        public RelatorioHelper(SQLiteAsyncConnection database)
        {
            _database = database;
        }

        public async Task<List<Relatorio>> Listar()
        {
            return await _database
                .Table<Relatorio>()
                .ToListAsync();
        }

        public async Task<Relatorio?> BuscarPorId(int id)
        {
            return await _database
                .FindAsync<Relatorio>(id);
        }

        public async Task<int> Salvar(Relatorio relatorio)
        {
            if (relatorio.Id == 0)
                return await _database.InsertAsync(relatorio);

            return await _database.UpdateAsync(relatorio);
        }

        public async Task<int> Excluir(Relatorio relatorio)
        {
            return await _database.DeleteAsync(relatorio);
        }

        public async Task<List<RelatorioVacinalPaciente>>
            ListarVacinais()
        {
            return await _database
                .Table<RelatorioVacinalPaciente>()
                .ToListAsync();
        }

        public async Task<List<RelatorioPacienteComVacinasAtrasadas>>
            ListarPacientesAtrasados()
        {
            return await _database
                .Table<RelatorioPacienteComVacinasAtrasadas>()
                .ToListAsync();
        }

        public async Task<List<RelatorioPacientesEmSituacaoRegular>>
            ListarPacientesRegulares()
        {
            return await _database
                .Table<RelatorioPacientesEmSituacaoRegular>()
                .ToListAsync();
        }
    }
}