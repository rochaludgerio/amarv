
using SQLite;

namespace maui_amarv.Models
{
    [SQLite.Table("Paciente")]
    public class Paciente
    {
        // Mesmo Id da Pessoa correspondente.
        // Chave primária da tabela Paciente.
        [PrimaryKey]
        public int Id { get; set; }

        // Dados específicos do paciente.
        public DateTime DataNascimento { get; set; }

        public string Sexo { get; set; } = string.Empty;

        public string CartaoSUS { get; set; } = string.Empty;

        public int IdadeAnos { get; set; }

        public int IdadeMesesAdicionais { get; set; }

        public string CondicaoClinica { get; set; } = string.Empty;

        public bool Gestante { get; set; }

        public int SemanasGestacao { get; set; }

        public bool Lactante { get; set; }

        public bool ProfissionalSaude { get; set; }

        public bool Indigena { get; set; }

        public bool AcamadoOuInstitucionalizado { get; set; }

        public bool CriancaAmamentadaMenor6Meses { get; set; }

        public bool Imunossuprimido { get; set; }

        public bool HIV { get; set; }

        public bool Quimioterapia { get; set; }

        public bool Transplantado { get; set; }

        public bool UsoCorticoideOuImunobiologico { get; set; }

        public bool AlergiaGrave { get; set; }

        public bool FebreOuDoencaAguda { get; set; }

        // Propriedades calculadas: não criam colunas no SQLite.
        [Ignore]
        public int IdadeTotalMeses =>
            (IdadeAnos * 12) + IdadeMesesAdicionais;

        [Ignore]
        public string IdadeFormatada =>
            $"{IdadeAnos} ano(s) e {IdadeMesesAdicionais} mês(es)";

        // Operações do paciente.
        public void CadastrarPaciente()
        {
        }

        public void AtualizarPaciente()
        {
        }

        public Paciente ConsultarPaciente()
        {
            return this;
        }
    }
}
