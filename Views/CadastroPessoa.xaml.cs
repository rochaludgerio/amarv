
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using maui_amarv.Models;

namespace maui_amarv.Views
{
    public partial class CadastroPessoa : ContentPage
    {
        private Pessoa? _pessoaSelecionada;
        private bool _atualizandoLista;
        private bool _modoEdicao;

        public CadastroPessoa()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CarregarPessoas();
        }

        // Carrega as pessoas cadastradas no SQLite.
        private async Task CarregarPessoas()
        {
            try
            {
                _atualizandoLista = true;

                var pessoas = await App.Database.Listar<Pessoa>();

                pck_pessoas.ItemsSource = pessoas
                    .OrderBy(p => p.Nome)
                    .ToList();

                if (_pessoaSelecionada != null)
                {
                    var selecionada = pessoas.FirstOrDefault(
                        p => p.Id == _pessoaSelecionada.Id);

                    if (selecionada != null)
                    {
                        pck_pessoas.SelectedItem = selecionada;
                        _pessoaSelecionada = selecionada;
                    }
                    else
                    {
                        _pessoaSelecionada = null;
                        pck_pessoas.SelectedItem = null;
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Erro",
                    $"Não foi possível carregar as pessoas: {ex.Message}",
                    "OK");
            }
            finally
            {
                _atualizandoLista = false;
                AtualizarBotoes();
            }
        }

        // Carrega os dados da pessoa selecionada.
        private void Pck_Pessoas_SelectedIndexChanged(
            object sender, EventArgs e)
        {
            if (_atualizandoLista)
                return;

            _pessoaSelecionada =
                pck_pessoas.SelectedItem as Pessoa;

            _modoEdicao = false;

            if (_pessoaSelecionada == null)
            {
                LimparCampos();
                AtualizarBotoes();
                return;
            }

            entry_nome.Text =
                _pessoaSelecionada.Nome ?? string.Empty;

            entry_nome_mae.Text =
                _pessoaSelecionada.Nome_da_Mae ?? string.Empty;

            entry_cpf.Text =
                _pessoaSelecionada.Cpf ?? string.Empty;

            entry_telefone.Text =
                _pessoaSelecionada.Telefone ?? string.Empty;

            entry_endereco.Text =
                _pessoaSelecionada.Endereco ?? string.Empty;

            AtualizarBotoes();
        }

        // Habilita a edição da pessoa selecionada.
        private async void Button_Editar_Clicked(
            object sender, EventArgs e)
        {
            if (_pessoaSelecionada == null)
            {
                await DisplayAlert(
                    "Atenção",
                    "Selecione uma pessoa cadastrada para editar.",
                    "OK");
                return;
            }

            _modoEdicao = true;
            AtualizarBotoes();

            await DisplayAlert(
                "Editar Pessoa",
                "Altere os dados desejados e clique em Salvar Alterações.",
                "OK");
        }

        // Cadastra uma pessoa ou atualiza um cadastro existente.
        private async void Button_Salvar_Clicked(
            object sender, EventArgs e)
        {
            string nome =
                entry_nome.Text?.Trim() ?? string.Empty;

            string nomeMae =
                entry_nome_mae.Text?.Trim() ?? string.Empty;

            string cpf = new string(
                (entry_cpf.Text ?? string.Empty)
                    .Where(char.IsDigit)
                    .ToArray());

            string telefone =
                entry_telefone.Text?.Trim() ?? string.Empty;

            string endereco =
                entry_endereco.Text?.Trim() ?? string.Empty;

            // Validação do nome.
            if (string.IsNullOrWhiteSpace(nome))
            {
                await DisplayAlert(
                    "Atenção",
                    "O nome completo é obrigatório.",
                    "OK");

                entry_nome.Focus();
                return;
            }

            // Validação do CPF.
            if (cpf.Length != 11)
            {
                await DisplayAlert(
                    "Atenção",
                    "O CPF é obrigatório e deve conter 11 dígitos.",
                    "OK");

                entry_cpf.Focus();
                return;
            }

            bool editando =
                _modoEdicao && _pessoaSelecionada != null;

            // Impede salvar por cima de uma pessoa selecionada
            // sem entrar primeiro no modo de edição.
            if (_pessoaSelecionada != null && !editando)
            {
                await DisplayAlert(
                    "Atenção",
                    "Para alterar uma pessoa existente, clique em Editar Pessoa.",
                    "OK");

                return;
            }

            var pessoa = new Pessoa
            {
                Id = editando
                    ? _pessoaSelecionada!.Id
                    : 0,

                Nome = nome,
                Nome_da_Mae = nomeMae,
                Cpf = cpf,
                Telefone = telefone,
                Endereco = endereco
            };

            try
            {
                btn_salvar.IsEnabled = false;

                int resultado;

                if (editando)
                {
                    // Atualiza o registro existente, preservando o ID.
                    resultado =
                        await App.Database.EditarPessoa(pessoa);
                }
                else
                {
                    // Cria um novo registro.
                    resultado =
                        await App.Database.SalvarPessoa(pessoa);
                }

                if (resultado <= 0)
                {
                    await DisplayAlert(
                        "Aviso",
                        "Não foi possível concluir a operação.",
                        "OK");

                    return;
                }

                await DisplayAlert(
                    "Sucesso",
                    editando
                        ? "Pessoa atualizada com sucesso."
                        : $"Pessoa cadastrada com sucesso. ID: {resultado}",
                    "OK");

                _pessoaSelecionada = null;
                _modoEdicao = false;

                _atualizandoLista = true;
                pck_pessoas.SelectedItem = null;
                _atualizandoLista = false;

                LimparCampos();

                await CarregarPessoas();
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Erro ao salvar",
                    ex.Message,
                    "OK");
            }
            finally
            {
                btn_salvar.IsEnabled = true;
                AtualizarBotoes();
            }
        }

        // Limpa os campos para iniciar um novo cadastro.
        private void Button_Nova_Pessoa_Clicked(
            object sender, EventArgs e)
        {
            _pessoaSelecionada = null;
            _modoEdicao = false;

            _atualizandoLista = true;
            pck_pessoas.SelectedItem = null;
            _atualizandoLista = false;

            LimparCampos();
            AtualizarBotoes();
        }

        // Exclui a pessoa selecionada após confirmação.
        private async void Button_Excluir_Clicked(
            object sender, EventArgs e)
        {
            if (_pessoaSelecionada == null)
            {
                await DisplayAlert(
                    "Atenção",
                    "Selecione uma pessoa para excluir.",
                    "OK");

                return;
            }

            bool confirmar = await DisplayAlert(
                "Confirmar exclusão",
                $"Deseja realmente excluir a pessoa {_pessoaSelecionada.Nome}?",
                "Excluir",
                "Cancelar");

            if (!confirmar)
                return;

            try
            {
                btn_excluir.IsEnabled = false;

                await App.Database.ExcluirPessoa(
                    _pessoaSelecionada);

                await DisplayAlert(
                    "Sucesso",
                    "Pessoa excluída com sucesso.",
                    "OK");

                _pessoaSelecionada = null;
                _modoEdicao = false;

                _atualizandoLista = true;
                pck_pessoas.SelectedItem = null;
                _atualizandoLista = false;

                LimparCampos();

                await CarregarPessoas();
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Não foi possível excluir",
                    ex.Message,
                    "OK");
            }
            finally
            {
                AtualizarBotoes();
            }
        }

        // Limpa todos os campos.
        private void LimparCampos()
        {
            entry_nome.Text = string.Empty;
            entry_nome_mae.Text = string.Empty;
            entry_cpf.Text = string.Empty;
            entry_telefone.Text = string.Empty;
            entry_endereco.Text = string.Empty;

            if (btn_salvar != null)
                btn_salvar.Text = "Cadastrar Pessoa";
        }

        // Atualiza o estado dos botões.
        private void AtualizarBotoes()
        {
            if (btn_editar == null ||
                btn_excluir == null ||
                btn_salvar == null)
            {
                return;
            }

            bool selecionada =
                _pessoaSelecionada != null;

            btn_editar.IsEnabled =
                selecionada && !_modoEdicao;

            btn_excluir.IsEnabled =
                selecionada && !_modoEdicao;

            btn_salvar.Text = _modoEdicao
                ? "Salvar Alterações"
                : "Cadastrar Pessoa";
        }
    }
}
