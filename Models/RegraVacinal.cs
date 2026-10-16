using SQLite;

namespace maui_amarv.Models
{
    public class RegraVacinal
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // =========================================================
        // IDENTIFICAÇÃO
        // =========================================================

        public string NomeVacina { get; set; } = string.Empty;

        public string Fabricante { get; set; } = string.Empty;

        public string TipoVacina { get; set; } = string.Empty;

        // =========================================================
        // DOSES
        // =========================================================

        public int NumeroDose { get; set; }

        public int TotalDoses { get; set; }

        public int DosesNecessariasCrianca { get; set; }

        public int DosesNecessariasAdulto { get; set; }

        // =========================================================
        // IDADE
        // =========================================================

        public int IdadeMinimaMeses { get; set; }

        public int IdadeMaximaMeses { get; set; }

        public int IdadeReferenciaMeses { get; set; }

        // =========================================================
        // GRUPO ETÁRIO
        // =========================================================

        public string GrupoEtario { get; set; } = string.Empty;

        // =========================================================
        // GRUPO ESPECIAL
        // =========================================================

        public string GrupoEspecial { get; set; } = "Geral";

        // =========================================================
        // GESTAÇÃO
        // =========================================================

        public int SemanaGestacionalMinima { get; set; }

        // =========================================================
        // INTERVALOS
        // =========================================================

        public string IntervaloEntreDoses { get; set; } = string.Empty;

        public string Periodicidade { get; set; } = string.Empty;

        // =========================================================
        // ADMINISTRAÇÃO
        // =========================================================

        public string ViaAdministracao { get; set; } = string.Empty;

        public string LocalAplicacao { get; set; } = string.Empty;

        // =========================================================
        // CALENDÁRIO
        // =========================================================

        public string TipoCalendario { get; set; } = string.Empty;

        public string FonteCalendario { get; set; } = string.Empty;

        // =========================================================
        // INDICAÇÃO
        // =========================================================

        public string Indicacao { get; set; } = string.Empty;

        // =========================================================
        // CONTRAINDICAÇÃO
        // =========================================================

        public string Contraindicacao { get; set; } = string.Empty;

        // =========================================================
        // EVENTOS ADVERSOS
        // =========================================================

        public string EventosAdversos { get; set; } = string.Empty;

        // =========================================================
        // OBSERVAÇÃO
        // =========================================================

        public string Observacao { get; set; } = string.Empty;

        // =========================================================
        // RESUMO
        // =========================================================

        public string Resumo
        {
            get
            {
                string vacina = NomeVacina ?? "Vacina";

                string dose = NumeroDose > 0
                    ? $"Dose {NumeroDose}"
                    : "Dose";

                string grupo = string.IsNullOrWhiteSpace(GrupoEtario)
                    ? "Geral"
                    : GrupoEtario;

                return $"{vacina} - {dose} - {grupo}";
            }
        }
    }
}