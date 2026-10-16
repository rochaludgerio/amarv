
using SQLite;
using maui_amarv.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace maui_amarv.Helpers
{
    public class UsuarioHelper
    {
        private readonly SQLiteAsyncConnection _database;

        public UsuarioHelper(SQLiteAsyncConnection database)
        {
            _database = database;
        }

        // ============================================================
        // BUSCAR USUÁRIO PELO LOGIN E SENHA
        // ============================================================

        public async Task<UsuarioDoSistema?> BuscarUsuario(
            string login,
            string senha)
        {
            var usuario = await _database
                .Table<UsuarioDoSistema>()
                .Where(u =>
                    u.Login == login &&
                    u.Senha == senha &&
                    u.Ativo)
                .FirstOrDefaultAsync();

            if (usuario != null)
                await CarregarNomeAsync(usuario);

            return usuario;
        }

        // ============================================================
        // BUSCAR USUÁRIO DO SISTEMA
        // ============================================================

        public async Task<UsuarioDoSistema?> BuscarUsuarioDoSistema(
            string login,
            string senha)
        {
            return await BuscarUsuario(login, senha);
        }

        // ============================================================
        // BUSCAR USUÁRIO PELO ID
        // ============================================================

        public async Task<UsuarioDoSistema?> BuscarPorIdAsync(int id)
        {
            if (id <= 0)
                return null;

            var usuario = await _database
                .Table<UsuarioDoSistema>()
                .Where(u => u.Id == id)
                .FirstOrDefaultAsync();

            if (usuario != null)
                await CarregarNomeAsync(usuario);

            return usuario;
        }

        // ============================================================
        // LISTAR USUÁRIOS
        // ============================================================

        public async Task<List<UsuarioDoSistema>> Listar()
        {
            var usuarios = await _database
                .Table<UsuarioDoSistema>()
                .ToListAsync();

            foreach (var usuario in usuarios)
                await CarregarNomeAsync(usuario);

            return usuarios;
        }

        // ============================================================
        // CARREGAR NOME A PARTIR DA TABELA PESSOA
        // ============================================================

        private async Task CarregarNomeAsync(
            UsuarioDoSistema usuario)
        {
            if (usuario.Id <= 0)
                return;

            var pessoa = await _database
                .Table<Pessoa>()
                .Where(p => p.Id == usuario.Id)
                .FirstOrDefaultAsync();

            usuario.Nome = pessoa?.Nome ?? string.Empty;
        }

        // ============================================================
        // GARANTIR REGISTRO DE PESSOA
        // ============================================================

        private async Task GarantirPessoaAsync(
            UsuarioDoSistema usuario,
            string nomePadrao)
        {
            if (usuario.Id <= 0)
            {
                throw new InvalidOperationException(
                    "O usuário precisa ter um ID válido.");
            }

            var pessoa = await _database
                .Table<Pessoa>()
                .Where(p => p.Id == usuario.Id)
                .FirstOrDefaultAsync();

            if (pessoa == null)
            {
                string nome = !string.IsNullOrWhiteSpace(usuario.Nome)
                    ? usuario.Nome.Trim()
                    : nomePadrao.Trim();

                if (string.IsNullOrWhiteSpace(nome))
                {
                    throw new InvalidOperationException(
                        "O nome da pessoa é obrigatório.");
                }

                pessoa = new Pessoa
                {
                    Id = usuario.Id,
                    Nome = nome
                };

                await _database.InsertAsync(pessoa);
            }
            else
            {
                // Não substitui um nome já cadastrado.
                if (string.IsNullOrWhiteSpace(pessoa.Nome))
                {
                    pessoa.Nome = !string.IsNullOrWhiteSpace(usuario.Nome)
                        ? usuario.Nome.Trim()
                        : nomePadrao.Trim();

                    await _database.UpdateAsync(pessoa);
                }
            }

            usuario.Nome = pessoa.Nome;
        }

        // ============================================================
        // SALVAR USUÁRIO
        // Pessoa.Id = UsuarioDoSistema.Id
        // ============================================================

        public async Task<int> Salvar(
            UsuarioDoSistema usuario)
        {
            if (usuario == null)
                throw new ArgumentNullException(nameof(usuario));

            if (string.IsNullOrWhiteSpace(usuario.Login))
            {
                throw new InvalidOperationException(
                    "O login é obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(usuario.Nome))
            {
                throw new InvalidOperationException(
                    "O nome é obrigatório.");
            }

            var loginNormalizado = usuario.Login.Trim();

            var usuariosExistentes = await _database
                .Table<UsuarioDoSistema>()
                .ToListAsync();

            bool loginDuplicado = usuariosExistentes.Any(u =>
                string.Equals(
                    u.Login?.Trim(),
                    loginNormalizado,
                    StringComparison.OrdinalIgnoreCase)
                && u.Id != usuario.Id);

            if (loginDuplicado)
            {
                throw new InvalidOperationException(
                    "Já existe um usuário com esse login.");
            }

            usuario.Login = loginNormalizado;

            // A gravação é transacional para não deixar Pessoa
            // criada sem o usuário correspondente em caso de falha.
            int idSalvo = 0;

            await _database.RunInTransactionAsync(conn =>
            {
                if (usuario.Id == 0)
                {
                    var pessoa = new Pessoa
                    {
                        Nome = usuario.Nome.Trim()
                    };

                    conn.Insert(pessoa);

                    usuario.Id = pessoa.Id;

                    conn.Insert(usuario);
                }
                else
                {
                    var pessoa = conn.Find<Pessoa>(usuario.Id);

                    if (pessoa == null)
                    {
                        throw new InvalidOperationException(
                            $"Não existe Pessoa com o ID {usuario.Id}. " +
                            "O usuário não foi salvo.");
                    }

                    pessoa.Nome = usuario.Nome.Trim();
                    conn.Update(pessoa);

                    var usuarioExistente =
                        conn.Find<UsuarioDoSistema>(usuario.Id);

                    if (usuarioExistente == null)
                    {
                        // Não cria usuário órfão: a pessoa já existe
                        // e o ID será o mesmo nas duas tabelas.
                        conn.Insert(usuario);
                    }
                    else
                    {
                        conn.Update(usuario);
                    }
                }

                idSalvo = usuario.Id;
            });

            // Mantém o registro específico de administrador.
            if (string.Equals(
                usuario.Perfil,
                "Administrador",
                StringComparison.OrdinalIgnoreCase))
            {
                await GarantirAdministradorAsync(usuario.Id);
            }

            return idSalvo;
        }

        // ============================================================
        // GARANTIR REGISTRO DE ADMINISTRADOR
        // AdministradorDoSistema.Id = UsuarioDoSistema.Id
        // ============================================================

        private async Task GarantirAdministradorAsync(int usuarioId)
        {
            var administrador = await _database
                .Table<AdministradorDoSistema>()
                .Where(a => a.Id == usuarioId)
                .FirstOrDefaultAsync();

            if (administrador == null)
            {
                await _database.InsertAsync(
                    new AdministradorDoSistema
                    {
                        Id = usuarioId,
                        NivelAcesso = "Total"
                    });
            }
        }

        // ============================================================
        // EXCLUIR USUÁRIO
        // Não exclui Pessoa nem Administrador automaticamente.
        // ============================================================

        public async Task<int> Excluir(
            UsuarioDoSistema usuario)
        {
            if (usuario == null)
                throw new ArgumentNullException(nameof(usuario));

            if (usuario.Id <= 0)
                throw new InvalidOperationException(
                    "O ID do usuário é inválido.");

            var administrador = await _database
                .Table<AdministradorDoSistema>()
                .Where(a => a.Id == usuario.Id)
                .FirstOrDefaultAsync();

            if (administrador != null)
            {
                throw new InvalidOperationException(
                    "Este usuário possui registro de administrador. " +
                    "Trate o vínculo de administrador antes de excluir.");
            }

            return await _database.DeleteAsync(usuario);
        }

        // ============================================================
        // CARREGAR USUÁRIOS INICIAIS
        // ============================================================

        public async Task CarregarUsuariosIniciais()
        {
            await CriarUsuarioInicialAsync(
                login: "admin",
                senha: "admin",
                nome: "Administrador",
                perfil: "Administrador");

            await CriarUsuarioInicialAsync(
                login: "usuario",
                senha: "123",
                nome: "Usuário",
                perfil: "Usuario");
        }

        // ============================================================
        // CRIAR OU ATUALIZAR USUÁRIO INICIAL
        // ============================================================

        private async Task CriarUsuarioInicialAsync(
            string login,
            string senha,
            string nome,
            string perfil)
        {
            var usuario = await _database
                .Table<UsuarioDoSistema>()
                .Where(u => u.Login == login)
                .FirstOrDefaultAsync();

            if (usuario == null)
            {
                // Se a pessoa já existir, tenta reutilizar o registro
                // somente quando for possível identificar o vínculo.
                usuario = new UsuarioDoSistema
                {
                    Login = login,
                    Senha = senha,
                    Nome = nome,
                    Perfil = perfil,
                    Status = "Ativo",
                    Ativo = true
                };

                await Salvar(usuario);
            }
            else
            {
                // Preserva a senha e o nome pessoal existentes.
                usuario.Perfil = perfil;
                usuario.Status = "Ativo";
                usuario.Ativo = true;

                await CarregarNomeAsync(usuario);

                if (string.IsNullOrWhiteSpace(usuario.Nome))
                {
                    usuario.Nome = nome;
                    await GarantirPessoaAsync(usuario, nome);
                }

                await _database.UpdateAsync(usuario);
            }

            // Confirma que a pessoa correspondente existe.
            await GarantirPessoaAsync(usuario, nome);

            if (string.Equals(
                perfil,
                "Administrador",
                StringComparison.OrdinalIgnoreCase))
            {
                await GarantirAdministradorAsync(usuario.Id);
            }
        }
    }
}
