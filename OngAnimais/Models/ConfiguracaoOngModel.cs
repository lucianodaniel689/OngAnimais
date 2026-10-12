namespace OngAnimais.Models
{
    /// <summary>
    /// Representa a tabela 'configuracoes_ong' no banco de dados — linha única (id = 1)
    /// com tudo o que a ONG pode customizar pelo painel: identidade, contato, conteúdo da
    /// página pública, espécies atendidas, regras de adoção e módulos do painel.
    /// </summary>
    /// <remarks>
    /// Colunas JSONB (<see cref="PassosAdocao"/>, <see cref="FormasAjudar"/>) chegam como
    /// string JSON — o <c>ConfiguracaoModulo</c> converte para os DTOs tipados. Colunas
    /// VARCHAR[] de espécies chegam como <c>string[]</c> (o Npgsql lê arrays nativamente).
    /// </remarks>
    public class ConfiguracaoOngModel
    {
        /// <summary>
        /// Identificador da linha — sempre 1 (CHECK no banco garante linha única).
        /// </summary>
        public int Id { get; set; }

        // ---------- Identidade ----------

        /// <summary>
        /// Nome da ONG exibido no cabeçalho, rodapé e títulos das páginas.
        /// Mapeado para a coluna 'nome_ong' no banco.
        /// </summary>
        public string NomeOng { get; set; } = string.Empty;

        /// <summary>
        /// URL da maior versão do logo. Nulo = símbolo padrão. Mapeado para a coluna 'logo_url' no banco.
        /// </summary>
        public string? LogoUrl { get; set; }

        /// <summary>
        /// Larguras (px) das versões WebP do logo. Mapeado para a coluna 'logo_larguras' (INTEGER[]).
        /// </summary>
        public int[] LogoLarguras { get; set; } = [];

        /// <summary>
        /// Ícone da aba do navegador (PNG 64px). Mapeado para a coluna 'favicon_url' no banco.
        /// </summary>
        public string? FaviconUrl { get; set; }

        /// <summary>
        /// Ícone de atalho na tela inicial do celular (PNG 180px).
        /// Mapeado para a coluna 'icone_app_url' no banco.
        /// </summary>
        public string? IconeAppUrl { get; set; }

        /// <summary>
        /// Paleta de cores do site (valor de <c>PaletaCores</c>). Mapeado para a coluna 'paleta' no banco.
        /// </summary>
        public string Paleta { get; set; } = string.Empty;

        /// <summary>
        /// URL da foto de reserva (animal sem foto / vitrine vazia).
        /// Mapeado para a coluna 'imagem_padrao_url' no banco.
        /// </summary>
        public string? ImagemPadraoUrl { get; set; }

        // ---------- Contato ----------

        /// <summary>
        /// WhatsApp em formato internacional, só dígitos (ex.: 5511999999999).
        /// </summary>
        public string? Whatsapp { get; set; }

        /// <summary>
        /// Usuário do Instagram, sem "@".
        /// </summary>
        public string? Instagram { get; set; }

        /// <summary>
        /// Email público de contato. Mapeado para a coluna 'email_contato' no banco.
        /// </summary>
        public string? EmailContato { get; set; }

        /// <summary>
        /// Endereço do abrigo, em uma linha.
        /// </summary>
        public string? Endereco { get; set; }

        /// <summary>
        /// Horário de atendimento. Mapeado para a coluna 'horario_atendimento' no banco.
        /// </summary>
        public string? HorarioAtendimento { get; set; }

        /// <summary>
        /// Parágrafo da seção de contato da home. Mapeado para a coluna 'texto_contato' no banco.
        /// </summary>
        public string? TextoContato { get; set; }

        /// <summary>
        /// Mensagem pré-preenchida dos links do WhatsApp.
        /// Mapeado para a coluna 'mensagem_whatsapp' no banco.
        /// </summary>
        public string? MensagemWhatsapp { get; set; }

        // ---------- Página pública ----------

        /// <summary>
        /// Título principal da home. Mapeado para a coluna 'hero_titulo' no banco.
        /// </summary>
        public string HeroTitulo { get; set; } = string.Empty;

        /// <summary>
        /// Subtítulo da home. Mapeado para a coluna 'hero_subtitulo' no banco.
        /// </summary>
        public string HeroSubtitulo { get; set; } = string.Empty;

        /// <summary>
        /// Quantidade de animais no carrossel da home (1–20).
        /// Mapeado para a coluna 'quantidade_destaque' no banco.
        /// </summary>
        public int QuantidadeDestaque { get; set; }

        /// <summary>
        /// Passos de "Como adotar" — JSON (array). Mapeado para a coluna 'passos_adocao' (JSONB).
        /// </summary>
        public string PassosAdocao { get; set; } = "[]";

        /// <summary>
        /// Cards de "Formas de ajudar" — JSON (array). Mapeado para a coluna 'formas_ajudar' (JSONB).
        /// </summary>
        public string FormasAjudar { get; set; } = "[]";

        /// <summary>
        /// Chave PIX para doações. Nulo = card de doação não aparece.
        /// Mapeado para a coluna 'doacao_pix_chave' no banco.
        /// </summary>
        public string? DoacaoPixChave { get; set; }

        /// <summary>
        /// Instruções de doação / dados bancários. Mapeado para a coluna 'doacao_texto' no banco.
        /// </summary>
        public string? DoacaoTexto { get; set; }

        // ---------- Animais ----------

        /// <summary>
        /// Espécies atendidas pela ONG (valores de <c>EspecieAnimal</c>).
        /// Mapeado para a coluna 'especies_habilitadas' (VARCHAR[]) no banco.
        /// </summary>
        public string[] EspeciesHabilitadas { get; set; } = [];

        /// <summary>
        /// Espécies em que o campo "porte" é usado (valores de <c>EspecieAnimal</c>).
        /// Mapeado para a coluna 'especies_com_porte' (VARCHAR[]) no banco.
        /// </summary>
        public string[] EspeciesComPorte { get; set; } = [];

        /// <summary>
        /// Exibe o selo "castrado" na página pública. Mapeado para a coluna 'exibir_castrado'.
        /// </summary>
        public bool ExibirCastrado { get; set; }

        /// <summary>
        /// Exibe o selo "vermifugado" na página pública. Mapeado para a coluna 'exibir_vermifugado'.
        /// </summary>
        public bool ExibirVermifugado { get; set; }

        // ---------- Adoção ----------

        /// <summary>
        /// Se o site aceita novas candidaturas (falso = adoções pausadas).
        /// Mapeado para a coluna 'aceitar_candidaturas' no banco.
        /// </summary>
        public bool AceitarCandidaturas { get; set; }

        /// <summary>
        /// Aviso exibido quando as candidaturas estão pausadas.
        /// Mapeado para a coluna 'aviso_candidaturas_pausadas' no banco.
        /// </summary>
        public string? AvisoCandidaturasPausadas { get; set; }

        /// <summary>
        /// Se o animal aceita várias candidaturas pendentes (falso = a 1ª já reserva).
        /// Mapeado para a coluna 'multiplas_candidaturas' no banco.
        /// </summary>
        public bool MultiplasCandidaturas { get; set; }

        /// <summary>
        /// Se as demais candidaturas pendentes são recusadas ao concluir uma adoção.
        /// Mapeado para a coluna 'recusar_pendentes_ao_concluir' no banco.
        /// </summary>
        public bool RecusarPendentesAoConcluir { get; set; }

        /// <summary>
        /// Status do animal devolvido ('EmTratamento' ou 'Disponivel').
        /// Mapeado para a coluna 'status_animal_devolvido' no banco.
        /// </summary>
        public string StatusAnimalDevolvido { get; set; } = string.Empty;

        /// <summary>
        /// Se o telefone é obrigatório no formulário de candidatura.
        /// Mapeado para a coluna 'telefone_obrigatorio' no banco.
        /// </summary>
        public bool TelefoneObrigatorio { get; set; }

        /// <summary>
        /// Texto de consentimento (LGPD) exibido no formulário de candidatura.
        /// Mapeado para a coluna 'texto_consentimento' no banco.
        /// </summary>
        public string? TextoConsentimento { get; set; }

        /// <summary>
        /// Exibe o botão "Falar no WhatsApp" na página do animal.
        /// Mapeado para a coluna 'exibir_whatsapp_animal' no banco.
        /// </summary>
        public bool ExibirWhatsappAnimal { get; set; }

        // ---------- Módulos do painel ----------

        /// <summary>
        /// Se o módulo de vacinas está ligado. Mapeado para a coluna 'modulo_vacinas' no banco.
        /// </summary>
        public bool ModuloVacinas { get; set; }

        /// <summary>
        /// Janela (dias) do alerta de próximas doses no dashboard.
        /// Mapeado para a coluna 'dias_alerta_vacina' no banco.
        /// </summary>
        public int DiasAlertaVacina { get; set; }

        /// <summary>
        /// Data da última alteração. Mapeado para a coluna 'updated_at' no banco.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
