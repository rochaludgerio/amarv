using SQLite;
using maui_amarv.Models;

namespace maui_amarv.Helpers
{
    public class DadosIniciaisHelper
    {
        private readonly SQLiteAsyncConnection _database;

        public DadosIniciaisHelper(SQLiteAsyncConnection database)
        {
            _database = database;
        }

        public async Task CarregarUsuarios()
        {
            var usuarios = await _database
                .Table<UsuarioDoSistema>()
                .ToListAsync();

            if (!usuarios.Any(u => u.Login == "admin"))
            {
                await _database.InsertAsync(
                    new UsuarioDoSistema
                    {
                        Login = "admin",
                        Senha = "admin",
                        Nome = "Administrador",
                        Perfil = "Administrador",
                        Ativo = true
                    });
            }

            if (!usuarios.Any(u => u.Login == "usuario"))
            {
                await _database.InsertAsync(
                    new UsuarioDoSistema
                    {
                        Login = "usuario",
                        Senha = "123",
                        Nome = "Usuário",
                        Perfil = "Usuario",
                        Ativo = true
                    });
            }
        }

        public async Task CarregarRegras(
            IEnumerable<RegraVacinal> regras)
        {
            foreach (var regra in regras)
            {
                await _database.InsertAsync(regra);
            }
        }
    }
}