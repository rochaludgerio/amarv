
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using maui_amarv.Data;
using maui_amarv.Helpers;
using maui_amarv.Models;

namespace maui_amarv.Views
{
    public partial class ConsultaPacientes : ContentPage
    {
        private readonly BancoDados _banco;
        private List<PacienteConsultaItem> _pacientes = new();

        public ConsultaPacientes(BancoDados banco)
        {
            InitializeComponent();

            _banco = banco ?? throw new ArgumentNullException(nameof(banco));
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CarregarPacientesAsync();
        }

        private async Task CarregarPacientesAsync()
        {
            try
            {
                var pacientes = await _banco.PacienteHelper.Listar();
                var pessoas = await _banco.ListarPessoasAsync();

                _pacientes = (
                    from paciente in pacientes
                    join pessoa in pessoas
                        on paciente.Id equals pessoa.Id
                    orderby pessoa.Nome
                    select new PacienteConsultaItem
                    {
                        Id = paciente.Id,
                        Nome = pessoa.Nome,
                        Cpf = pessoa.Cpf,
                        Telefone = pessoa.Telefone,
                        Endereco = pessoa.Endereco,
                        NomeMae = pessoa.Nome_da_Mae,
                        DataNascimento = paciente.DataNascimento,
                        IdadeAnos = paciente.IdadeAnos,
                        Sexo = paciente.Sexo,
                        CartaoSUS = paciente.CartaoSUS,
                        Rua = pessoa.Endereco
                    }).ToList();

                listaPacientes.ItemsSource = _pacientes;
                FiltrarPacientes(searchPaciente.Text);
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Erro",
                    $"Não foi possível carregar os pacientes.\n{ex.Message}",
                    "OK");
            }
        }

        private void OnPesquisaTextoChanged(
            object sender,
            TextChangedEventArgs e)
        {
            FiltrarPacientes(e.NewTextValue);
        }

        private void OnPesquisarClicked(object sender, EventArgs e)
        {
            FiltrarPacientes(searchPaciente.Text);
        }

        private void FiltrarPacientes(string? texto)
        {
            IEnumerable<PacienteConsultaItem> resultado = _pacientes;

            if (!string.IsNullOrWhiteSpace(texto))
            {
                texto = texto.Trim();

                resultado = _pacientes.Where(p =>
                    (p.Nome?.Contains(
                        texto,
                        StringComparison.OrdinalIgnoreCase) ?? false)
                    ||
                    (p.Cpf?.Contains(
                        texto,
                        StringComparison.OrdinalIgnoreCase) ?? false));
            }

            listaPacientes.ItemsSource = resultado.ToList();
        }

        private async void OnPacienteSelecionado(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault()
                is not PacienteConsultaItem paciente)
            {
                return;
            }

            listaPacientes.SelectedItem = null;

            string dados =
                $"Nome: {paciente.Nome}\n" +
                $"CPF: {paciente.Cpf}\n" +
                $"Telefone: {paciente.Telefone}\n" +
                $"Endereço: {paciente.Endereco}\n" +
                $"Data de nascimento: {paciente.DataNascimento:dd/MM/yyyy}\n" +
                $"Idade: {paciente.IdadeAnos} anos\n" +
                $"Sexo: {paciente.Sexo}\n" +
                $"Cartão SUS: {paciente.CartaoSUS}\n" +
                $"Nome da mãe: {paciente.NomeMae}";

            await DisplayAlert("Dados do paciente", dados, "Fechar");
        }

        private async void OnVoltarClicked(object sender, EventArgs e)
        {
            if (Navigation.NavigationStack.Count > 1)
            {
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert(
                    "Navegação",
                    "Não há uma página anterior na pilha de navegação.",
                    "OK");
            }
        }

        private class PacienteConsultaItem
        {
            public int Id { get; set; }
            public string Nome { get; set; } = string.Empty;
            public string Cpf { get; set; } = string.Empty;
            public string Telefone { get; set; } = string.Empty;
            public string Endereco { get; set; } = string.Empty;
            public string NomeMae { get; set; } = string.Empty;
            public DateTime DataNascimento { get; set; }
            public int IdadeAnos { get; set; }
            public string Sexo { get; set; } = string.Empty;
            public string CartaoSUS { get; set; } = string.Empty;
            public string Rua { get; set; } = string.Empty;
        }
    }
}

