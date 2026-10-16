using maui_amarv.Models;

namespace maui_amarv.Views;

public partial class BackupPage : ContentPage
{
    private readonly UsuarioDoSistema usuarioLogado;

    public BackupPage(UsuarioDoSistema usuario)
    {
        InitializeComponent();

        usuarioLogado = usuario;

        lbl_usuario.Text =
            $"Usuário: {usuarioLogado.Nome} | Perfil: {usuarioLogado.Perfil}";
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        bool administrador =
            usuarioLogado?.Perfil?.Trim().Equals(
                "Administrador",
                StringComparison.OrdinalIgnoreCase) == true;

        btn_backup.IsEnabled = administrador;
        btn_recuperar.IsEnabled = administrador;

        if (!administrador)
        {
            lbl_status.Text =
                "Backup disponível somente para o administrador.";
        }
    }

    // ============================================================
    // REALIZAR BACKUP
    // ============================================================

    private async void Button_Backup_Clicked(object sender, EventArgs e)
    {
        try
        {
            bool administrador =
                usuarioLogado?.Perfil?.Trim().Equals(
                    "Administrador",
                    StringComparison.OrdinalIgnoreCase) == true;

            if (!administrador)
            {
                await DisplayAlert(
                    "Acesso negado",
                    "Somente o administrador pode realizar backup.",
                    "OK");

                return;
            }

            string pasta = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments),
                "AmarV");

            if (!Directory.Exists(pasta))
            {
                Directory.CreateDirectory(pasta);
            }

            string banco = Path.Combine(
                pasta,
                "amarv.db3");

            if (!File.Exists(banco))
            {
                await DisplayAlert(
                    "Erro",
                    "Banco de dados não encontrado.",
                    "OK");

                return;
            }

            string nomeBackup =
                $"backup_amarv_{DateTime.Now:yyyyMMdd_HHmmss}.db3";

            string caminhoBackup =
                Path.Combine(
                    pasta,
                    nomeBackup);

            // ====================================================
            // CRIA A CÓPIA FÍSICA DO BANCO
            // ====================================================

            File.Copy(
                banco,
                caminhoBackup,
                true);

            // ====================================================
            // CRIA O REGISTRO DO BACKUP
            // ====================================================

            Backup backup = new Backup
            {
                DataBackup = DateTime.Now,
                Status = "Backup realizado",
                LocalArquivo = caminhoBackup
            };

            // Executa a rotina existente da classe Backup
            backup.RealizarBackup();

            // Garante o caminho correto
            backup.LocalArquivo = caminhoBackup;

            // ====================================================
            // CORREÇÃO:
            // O BancoDados possui BackupHelper, não Backup
            // ====================================================

            await App.Database.BackupHelper.Salvar(backup);

            // ====================================================
            // ATUALIZA O STATUS NA TELA
            // ====================================================

            lbl_status.Text =
                $"Último backup: {backup.DataBackup:dd/MM/yyyy HH:mm}" +
                $"\nArquivo: {nomeBackup}";

            await DisplayAlert(
                "Backup realizado",
                $"Backup salvo com sucesso.\n\n" +
                $"Data: {backup.DataBackup:dd/MM/yyyy}\n" +
                $"Horário: {backup.DataBackup:HH:mm}\n\n" +
                "Local: Documentos > AmarV",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erro no Backup",
                $"Não foi possível realizar o backup.\n\n" +
                ex.Message,
                "OK");
        }
    }

    // ============================================================
    // RECUPERAR BACKUP
    // ============================================================

    private async void Button_Recuperar_Clicked(object sender, EventArgs e)
    {
        try
        {
            bool administrador =
                usuarioLogado?.Perfil?.Trim().Equals(
                    "Administrador",
                    StringComparison.OrdinalIgnoreCase) == true;

            if (!administrador)
            {
                await DisplayAlert(
                    "Acesso negado",
                    "Somente o administrador pode recuperar um backup.",
                    "OK");

                return;
            }

            string pasta = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments),
                "AmarV");

            if (!Directory.Exists(pasta))
            {
                await DisplayAlert(
                    "Backup",
                    "A pasta de backups não foi encontrada.",
                    "OK");

                return;
            }

            // ====================================================
            // LOCALIZA TODOS OS BACKUPS
            // ====================================================

            string[] arquivos = Directory.GetFiles(
                pasta,
                "backup_amarv_*.db3");

            if (arquivos.Length == 0)
            {
                await DisplayAlert(
                    "Backup",
                    "Nenhum backup encontrado.",
                    "OK");

                return;
            }

            // ====================================================
            // ORDENA DO MAIS RECENTE PARA O MAIS ANTIGO
            // ====================================================

            Array.Sort(arquivos);
            Array.Reverse(arquivos);

            // ====================================================
            // CRIA A LISTA MOSTRANDO SOMENTE DATA E HORA
            // ====================================================

            List<string> opcoesBackup = new List<string>();

            foreach (string arquivo in arquivos)
            {
                DateTime dataBackup =
                    File.GetLastWriteTime(arquivo);

                string opcao =
                    $"{dataBackup:dd/MM/yyyy} às {dataBackup:HH:mm}";

                opcoesBackup.Add(opcao);
            }

            // ====================================================
            // MOSTRA OS BACKUPS PARA O ADMINISTRADOR
            // ====================================================

            string backupSelecionado =
                await DisplayActionSheet(
                    "Selecione o backup para recuperar",
                    "Cancelar",
                    null,
                    opcoesBackup.ToArray());

            if (string.IsNullOrWhiteSpace(backupSelecionado) ||
                backupSelecionado == "Cancelar")
            {
                return;
            }

            // ====================================================
            // IDENTIFICA O ARQUIVO CORRESPONDENTE
            // ====================================================

            int indiceSelecionado =
                opcoesBackup.IndexOf(backupSelecionado);

            if (indiceSelecionado < 0 ||
                indiceSelecionado >= arquivos.Length)
            {
                await DisplayAlert(
                    "Erro",
                    "Não foi possível identificar o backup selecionado.",
                    "OK");

                return;
            }

            string caminhoBackup =
                arquivos[indiceSelecionado];

            // ====================================================
            // CONFIRMA A RESTAURAÇÃO
            // ====================================================

            bool confirmar =
                await DisplayAlert(
                    "Confirmar recuperação",
                    $"Deseja restaurar o backup de:\n\n" +
                    $"{backupSelecionado}\n\n" +
                    "Os dados atuais do aplicativo serão substituídos.",
                    "Restaurar",
                    "Cancelar");

            if (!confirmar)
            {
                return;
            }

            string banco = Path.Combine(
                pasta,
                "amarv.db3");

            // ====================================================
            // FAZ BACKUP DO BANCO ATUAL
            // ANTES DA RESTAURAÇÃO
            // ====================================================

            if (File.Exists(banco))
            {
                string nomeBackupSeguranca =
                    $"backup_antes_restauracao_{DateTime.Now:yyyyMMdd_HHmmss}.db3";

                string caminhoBackupSeguranca =
                    Path.Combine(
                        pasta,
                        nomeBackupSeguranca);

                File.Copy(
                    banco,
                    caminhoBackupSeguranca,
                    true);
            }

            // ====================================================
            // RESTAURA O BACKUP SELECIONADO
            // ====================================================

            File.Copy(
                caminhoBackup,
                banco,
                true);

            // ====================================================
            // ATUALIZA O STATUS
            // ====================================================

            lbl_status.Text =
                $"Backup recuperado:\n{backupSelecionado}";

            await DisplayAlert(
                "Backup recuperado",
                "O backup foi restaurado com sucesso.\n\n" +
                "É recomendado fechar e abrir novamente o aplicativo " +
                "para que todos os dados restaurados sejam carregados.",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erro na recuperação",
                $"Não foi possível recuperar o backup.\n\n" +
                ex.Message,
                "OK");
        }
    }

    // ============================================================
    // VOLTAR AO MENU
    // ============================================================

    private async void Button_Menu_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}