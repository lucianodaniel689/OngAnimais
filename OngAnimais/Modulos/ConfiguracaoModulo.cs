using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using OngAnimais.Common.Armazenamento;
using OngAnimais.Common.Auditoria;
using OngAnimais.Common.Exceptions;
using OngAnimais.Data;
using OngAnimais.DTOs;
using OngAnimais.Enums;
using OngAnimais.Models;

namespace OngAnimais.Modulos
{
    /// <summary>
    /// Módulo responsável pelas configurações da ONG (tabela 'configuracoes_ong', linha única
    /// id = 1): leitura pública do que o site precisa, leitura completa e atualização pelo painel.
    /// </summary>
    /// <remarks>
    /// <para><b>Permissão (defesa em profundidade):</b> o controller já exige
    /// <c>[Authorize(Roles = Admin)]</c>, mas o perfil vem do JWT, que vale até expirar (~60 min).
    /// Por isso <see cref="ObterAsync"/> e <see cref="AtualizarAsync"/> também conferem <b>no
    /// banco</b> que o operador ainda existe, está ativo e é Admin — um Admin rebaixado ou
    /// desativado perde o acesso na hora, não só quando o token vencer. Na atualização a
    /// checagem roda dentro da transação, com a linha do operador travada (<c>FOR SHARE</c>):
    /// um rebaixamento simultâneo espera esta gravação terminar, nunca a "atravessa".</para>
    /// </remarks>
    public class ConfiguracaoModulo
    {
        private readonly DbConnectionFactory _dbFactory;
        private readonly ContextoOperador _contexto;
        private readonly AuditoriaModulo _auditoria;
        private readonly IArmazenamentoArquivos _armazenamento;
        private readonly ILogger<ConfiguracaoModulo> _logger;

        /// <summary>
        /// Subpasta de <c>wwwroot/uploads</c> com os arquivos de identidade visual (logo e ícones).
        /// </summary>
        private const string SubpastaMarca = "marca";

        /// <summary>
        /// ID fixo da linha única de configuração (CHECK no banco).
        /// </summary>
        private const int IdConfiguracao = 1;

        /// <summary>
        /// Status aceitos como destino do animal devolvido (espelha o CHECK do banco).
        /// </summary>
        private static readonly StatusAnimal[] StatusDevolucaoPermitidos =
            [StatusAnimal.EmTratamento, StatusAnimal.Disponivel];

        /// <summary>
        /// Serialização das colunas JSONB (camelCase, igual ao contrato da API; omite
        /// <c>rota</c>/<c>ancora</c> nulas).
        /// </summary>
        private static readonly JsonSerializerOptions OpcoesJson = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private const string SqlColunas = @"
            id, nome_ong, logo_url, logo_larguras, favicon_url, icone_app_url, imagem_padrao_url, paleta,
            whatsapp, instagram, email_contato, endereco, horario_atendimento, texto_contato, mensagem_whatsapp,
            hero_titulo, hero_subtitulo, quantidade_destaque,
            passos_adocao::text AS passos_adocao, formas_ajudar::text AS formas_ajudar,
            doacao_pix_chave, doacao_texto,
            especies_habilitadas, especies_com_porte, exibir_castrado, exibir_vermifugado,
            aceitar_candidaturas, aviso_candidaturas_pausadas, multiplas_candidaturas,
            recusar_pendentes_ao_concluir, status_animal_devolvido, telefone_obrigatorio,
            texto_consentimento, exibir_whatsapp_animal,
            modulo_vacinas, dias_alerta_vacina, updated_at";

        private const string SqlSelect = "SELECT " + SqlColunas + " FROM configuracoes_ong WHERE id = @Id";

        /// <summary>
        /// Rótulos legíveis dos campos para o detalhe da auditoria. <c>MostrarValor</c> = falso
        /// para textos longos/listas JSON (registra só que mudou, sem copiar o texto inteiro).
        /// </summary>
        private static readonly (string Rotulo, bool MostrarValor, Func<ConfiguracaoOngModel, string?> Valor)[] CamposAuditados =
        [
            ("nome da ONG", true, c => c.NomeOng),
            ("imagem padrão", false, c => c.ImagemPadraoUrl),
            ("paleta de cores", true, c => c.Paleta),
            ("WhatsApp", true, c => c.Whatsapp),
            ("Instagram", true, c => c.Instagram),
            ("email de contato", true, c => c.EmailContato),
            ("endereço", true, c => c.Endereco),
            ("horário de atendimento", true, c => c.HorarioAtendimento),
            ("texto de contato", false, c => c.TextoContato),
            ("mensagem do WhatsApp", false, c => c.MensagemWhatsapp),
            ("título da página inicial", false, c => c.HeroTitulo),
            ("subtítulo da página inicial", false, c => c.HeroSubtitulo),
            ("animais em destaque", true, c => c.QuantidadeDestaque.ToString()),
            ("passos de adoção", false, c => c.PassosAdocao),
            ("formas de ajudar", false, c => c.FormasAjudar),
            ("chave PIX", true, c => c.DoacaoPixChave),
            ("texto de doação", false, c => c.DoacaoTexto),
            ("espécies atendidas", true, c => string.Join(", ", c.EspeciesHabilitadas)),
            ("espécies com porte", true, c => string.Join(", ", c.EspeciesComPorte)),
            ("selo castrado", true, c => SimNao(c.ExibirCastrado)),
            ("selo vermifugado", true, c => SimNao(c.ExibirVermifugado)),
            ("aceitar candidaturas", true, c => SimNao(c.AceitarCandidaturas)),
            ("aviso de candidaturas pausadas", false, c => c.AvisoCandidaturasPausadas),
            ("várias candidaturas por animal", true, c => SimNao(c.MultiplasCandidaturas)),
            ("recusar pendentes ao concluir", true, c => SimNao(c.RecusarPendentesAoConcluir)),
            ("status do animal devolvido", true, c => c.StatusAnimalDevolvido),
            ("telefone obrigatório", true, c => SimNao(c.TelefoneObrigatorio)),
            ("texto de consentimento", false, c => c.TextoConsentimento),
            ("WhatsApp na página do animal", true, c => SimNao(c.ExibirWhatsappAnimal)),
            ("módulo de vacinas", true, c => SimNao(c.ModuloVacinas)),
            ("dias do alerta de vacina", true, c => c.DiasAlertaVacina.ToString())
        ];

        /// <summary>
        /// Inicializa o módulo com as dependências necessárias.
        /// </summary>
        /// <param name="dbFactory">Fábrica de conexão com o banco.</param>
        /// <param name="contexto">Contexto do operador da requisição atual (claims do JWT).</param>
        /// <param name="auditoria">Módulo de auditoria (registro das alterações e acessos negados).</param>
        /// <param name="armazenamento">Armazenamento de arquivos (logo e ícones gerados a partir dele).</param>
        /// <param name="logger">Logger para falhas ao apagar arquivos de logo antigos.</param>
        public ConfiguracaoModulo(
            DbConnectionFactory dbFactory,
            ContextoOperador contexto,
            AuditoriaModulo auditoria,
            IArmazenamentoArquivos armazenamento,
            ILogger<ConfiguracaoModulo> logger)
        {
            _dbFactory = dbFactory;
            _contexto = contexto;
            _auditoria = auditoria;
            _armazenamento = armazenamento;
            _logger = logger;
        }

        /// <summary>
        /// Obtém as configurações que o site precisa (endpoint público, sem login).
        /// Traz só as formas de ajudar ativas e nenhuma regra interna de adoção.
        /// </summary>
        /// <returns>Configurações públicas, ou null se a linha de configuração não existir.</returns>
        public async Task<ConfiguracaoOngPublicaDto?> ObterPublicaAsync()
        {
            using var connection = _dbFactory.CreateConnection();
            var configuracao = await connection.QueryFirstOrDefaultAsync<ConfiguracaoOngModel>(
                SqlSelect, new { Id = IdConfiguracao });

            if (configuracao is null)
                return null;

            var dto = new ConfiguracaoOngPublicaDto();
            PreencherPublica(dto, configuracao, somenteFormasAtivas: true);
            return dto;
        }

        /// <summary>
        /// Obtém a configuração completa para a tela de edição do painel (exclusivo de Admin —
        /// revalida o perfil do operador no banco, além do <c>[Authorize]</c> do controller).
        /// </summary>
        /// <returns>Configuração completa, ou null se a linha de configuração não existir.</returns>
        /// <exception cref="AcessoNegadoException">Operador não é (mais) um Admin ativo.</exception>
        public async Task<ConfiguracaoOngRespostaDto?> ObterAsync()
        {
            using var connection = _dbFactory.CreateConnection();

            await GarantirOperadorAdminAsync(connection, transacao: null);

            var configuracao = await connection.QueryFirstOrDefaultAsync<ConfiguracaoOngModel>(
                SqlSelect, new { Id = IdConfiguracao });

            return configuracao is null ? null : MapearParaResposta(configuracao);
        }

        /// <summary>
        /// Atualiza a configuração inteira da ONG (exclusivo de Admin). Na mesma transação:
        /// revalida o perfil do operador no banco, trava a linha de configuração, grava e
        /// registra na auditoria quais campos mudaram.
        /// </summary>
        /// <param name="dto">Nova configuração completa.</param>
        /// <returns>Configuração atualizada, ou null se a linha de configuração não existir.</returns>
        /// <exception cref="AcessoNegadoException">Operador não é (mais) um Admin ativo.</exception>
        /// <exception cref="RegraDeNegocioException">Alguma regra da configuração foi violada.</exception>
        public async Task<ConfiguracaoOngRespostaDto?> AtualizarAsync(ConfiguracaoOngUpdateDto dto)
        {
            using var connection = _dbFactory.CreateConnection();
            using var transacao = connection.BeginTransaction();

            try
            {
                // 1) Permissão primeiro: quem não é Admin não chega nem a ver erros de validação.
                await GarantirOperadorAdminAsync(connection, transacao);

                // 2) Regras que o DataAnnotations não cobre (+ normalização: vazio → nulo).
                var parametros = ValidarENormalizar(dto);

                // 3) Estado anterior travado (FOR UPDATE): duas edições simultâneas não se
                //    misturam e o detalhe da auditoria compara com o valor realmente substituído.
                var antes = await connection.QueryFirstOrDefaultAsync<ConfiguracaoOngModel>(
                    SqlSelect + " FOR UPDATE", new { Id = IdConfiguracao }, transacao);

                if (antes is null)
                {
                    transacao.Rollback();
                    return null;
                }

                var depois = await connection.QueryFirstAsync<ConfiguracaoOngModel>(@"
                    UPDATE configuracoes_ong SET
                        nome_ong                      = @NomeOng,
                        imagem_padrao_url             = @ImagemPadraoUrl,
                        paleta                        = @Paleta,
                        whatsapp                      = @Whatsapp,
                        instagram                     = @Instagram,
                        email_contato                 = @EmailContato,
                        endereco                      = @Endereco,
                        horario_atendimento           = @HorarioAtendimento,
                        texto_contato                 = @TextoContato,
                        mensagem_whatsapp             = @MensagemWhatsapp,
                        hero_titulo                   = @HeroTitulo,
                        hero_subtitulo                = @HeroSubtitulo,
                        quantidade_destaque           = @QuantidadeDestaque,
                        passos_adocao                 = CAST(@PassosAdocao AS jsonb),
                        formas_ajudar                 = CAST(@FormasAjudar AS jsonb),
                        doacao_pix_chave              = @DoacaoPixChave,
                        doacao_texto                  = @DoacaoTexto,
                        especies_habilitadas          = @EspeciesHabilitadas,
                        especies_com_porte            = @EspeciesComPorte,
                        exibir_castrado               = @ExibirCastrado,
                        exibir_vermifugado            = @ExibirVermifugado,
                        aceitar_candidaturas          = @AceitarCandidaturas,
                        aviso_candidaturas_pausadas   = @AvisoCandidaturasPausadas,
                        multiplas_candidaturas        = @MultiplasCandidaturas,
                        recusar_pendentes_ao_concluir = @RecusarPendentesAoConcluir,
                        status_animal_devolvido       = @StatusAnimalDevolvido,
                        telefone_obrigatorio          = @TelefoneObrigatorio,
                        texto_consentimento           = @TextoConsentimento,
                        exibir_whatsapp_animal        = @ExibirWhatsappAnimal,
                        modulo_vacinas                = @ModuloVacinas,
                        dias_alerta_vacina            = @DiasAlertaVacina,
                        updated_at                    = NOW()
                    WHERE id = @Id
                    RETURNING " + SqlColunas,
                    parametros, transacao);

                var detalhes = MontarDetalhesAlteracao(antes, depois);
                if (detalhes is not null)
                    await _auditoria.RegistrarAsync(connection, transacao,
                        CategoriaAuditoria.Configuracao, AcoesAuditoria.ConfiguracaoAtualizada, IdConfiguracao, detalhes);

                transacao.Commit();
                return MapearParaResposta(depois);
            }
            catch
            {
                transacao.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Troca o logo da ONG (exclusivo de Admin): grava a imagem (versões WebP + favicon e ícone
        /// de atalho em PNG), atualiza os 4 campos de logo numa transação, audita e só depois do
        /// commit apaga os arquivos do logo anterior. Se o banco falhar, apaga os arquivos novos.
        /// </summary>
        /// <param name="arquivo">Imagem enviada.</param>
        /// <returns>Configuração atualizada, ou null se a linha de configuração não existir.</returns>
        /// <exception cref="AcessoNegadoException">Operador não é (mais) um Admin ativo.</exception>
        /// <exception cref="RegraDeNegocioException">Arquivo inválido (tipo, tamanho, resolução).</exception>
        public async Task<ConfiguracaoOngRespostaDto?> AtualizarLogoAsync(IFormFile arquivo)
        {
            // Permissão antes de gravar qualquer arquivo em disco (revalidada de novo na transação).
            using (var verificacao = _dbFactory.CreateConnection())
                await GarantirOperadorAdminAsync(verificacao, transacao: null);

            var logo = await _armazenamento.SalvarLogoAsync(arquivo, SubpastaMarca);

            ConfiguracaoOngModel? antes, depois;
            try
            {
                (antes, depois) = await GravarLogoAsync(
                    logo.Url, logo.Larguras, logo.FaviconUrl, logo.IconeAppUrl, AcoesAuditoria.LogoAtualizado);
            }
            catch
            {
                // Banco não gravou (ou acesso negado na revalidação): os arquivos novos sobrariam órfãos.
                ExcluirArquivosSemFalhar(logo.Url);
                throw;
            }

            if (depois is null)
            {
                ExcluirArquivosSemFalhar(logo.Url);
                return null;
            }

            ExcluirArquivosSemFalhar(antes?.LogoUrl);
            return MapearParaResposta(depois);
        }

        /// <summary>
        /// Remove o logo da ONG (exclusivo de Admin): o site volta ao símbolo e ao favicon padrão.
        /// Os arquivos só são apagados depois do commit.
        /// </summary>
        /// <returns>Configuração atualizada, ou null se a linha de configuração não existir.</returns>
        /// <exception cref="AcessoNegadoException">Operador não é (mais) um Admin ativo.</exception>
        public async Task<ConfiguracaoOngRespostaDto?> RemoverLogoAsync()
        {
            var (antes, depois) = await GravarLogoAsync(null, [], null, null, AcoesAuditoria.LogoRemovido);
            if (depois is null)
                return null;

            ExcluirArquivosSemFalhar(antes?.LogoUrl);
            return MapearParaResposta(depois);
        }

        /// <summary>
        /// Apaga os arquivos de um logo (todas as versões e ícones, pelo GUID) depois que o banco já
        /// foi atualizado. Falha de disco aqui não desfaz a operação nem vira 500 — sobra no máximo
        /// um arquivo órfão, inofensivo; o banco (o que o site usa) já está certo.
        /// </summary>
        private void ExcluirArquivosSemFalhar(string? url)
        {
            if (url is null)
                return;
            try
            {
                _armazenamento.Excluir(url);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "Não foi possível apagar os arquivos do logo {Url}: {Message}", url, ex.Message);
            }
        }

        /// <summary>
        /// Grava os campos de logo numa transação (revalida o Admin, trava a linha, audita) e devolve
        /// o estado anterior e o novo. <c>(null, null)</c> se a linha de configuração não existir.
        /// Sem logo antes e sem logo agora (remover o que não existe) não audita nada.
        /// </summary>
        private async Task<(ConfiguracaoOngModel? Antes, ConfiguracaoOngModel? Depois)> GravarLogoAsync(
            string? logoUrl, int[] larguras, string? faviconUrl, string? iconeAppUrl, string acao)
        {
            using var connection = _dbFactory.CreateConnection();
            using var transacao = connection.BeginTransaction();
            try
            {
                await GarantirOperadorAdminAsync(connection, transacao);

                var antes = await connection.QueryFirstOrDefaultAsync<ConfiguracaoOngModel>(
                    SqlSelect + " FOR UPDATE", new { Id = IdConfiguracao }, transacao);
                if (antes is null)
                {
                    transacao.Rollback();
                    return (null, null);
                }

                var depois = await connection.QueryFirstAsync<ConfiguracaoOngModel>(@"
                    UPDATE configuracoes_ong SET
                        logo_url      = @LogoUrl,
                        logo_larguras = @LogoLarguras,
                        favicon_url   = @FaviconUrl,
                        icone_app_url = @IconeAppUrl,
                        updated_at    = NOW()
                    WHERE id = @Id
                    RETURNING " + SqlColunas,
                    new
                    {
                        Id = IdConfiguracao,
                        LogoUrl = logoUrl,
                        // INTEGER[] nativo (o Dapper não expande arrays com o Npgsql).
                        LogoLarguras = larguras,
                        FaviconUrl = faviconUrl,
                        IconeAppUrl = iconeAppUrl
                    },
                    transacao);

                if (antes.LogoUrl is not null || logoUrl is not null)
                    await _auditoria.RegistrarAsync(connection, transacao,
                        CategoriaAuditoria.Configuracao, acao, IdConfiguracao,
                        logoUrl is null ? "Logo removido (site volta ao símbolo padrão)." : "Logo enviado (favicon e ícone de atalho gerados).");

                transacao.Commit();
                return (antes, depois);
            }
            catch
            {
                transacao.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Confere no banco que o operador autenticado existe, está ativo e tem perfil Admin.
        /// Com transação, trava a linha do operador (<c>FOR SHARE</c>) até o fim dela. Se a
        /// checagem falhar, registra o evento de segurança e lança <see cref="AcessoNegadoException"/>
        /// (vira 404 no middleware, mesma resposta de quem nem passou pelo <c>[Authorize]</c>).
        /// </summary>
        /// <param name="connection">Conexão aberta.</param>
        /// <param name="transacao">Transação em andamento, ou null em leituras.</param>
        private async Task GarantirOperadorAdminAsync(IDbConnection connection, IDbTransaction? transacao)
        {
            var operadorId = _contexto.UsuarioId;
            string? motivo = null;

            if (operadorId is null)
            {
                motivo = "token sem identificação do operador";
            }
            else
            {
                var sql = transacao is null
                    ? "SELECT id, perfil, ativo FROM usuarios WHERE id = @Id"
                    : "SELECT id, perfil, ativo FROM usuarios WHERE id = @Id FOR SHARE";

                var operador = await connection.QueryFirstOrDefaultAsync<UsuarioModel>(
                    sql, new { Id = operadorId }, transacao);

                if (operador is null)
                    motivo = "conta do operador não existe mais";
                else if (!operador.Ativo)
                    motivo = "conta do operador está desativada";
                else if (operador.Perfil != PerfilUsuario.Admin)
                    motivo = $"perfil atual do operador é {operador.Perfil}";
            }

            if (motivo is null)
                return;

            // Auditoria em conexão própria e tolerante a falha: a transação do chamador vai ser
            // desfeita, e um erro ao auditar não pode trocar o 404 por um 500.
            await _auditoria.RegistrarSeguroAsync(
                CategoriaAuditoria.Seguranca, AcoesAuditoria.AcessoAdminNegado, operadorId,
                $"Acesso às configurações da ONG recusado: {motivo}.");

            throw new AcessoNegadoException($"Configurações da ONG: {motivo} (usuário id {operadorId?.ToString() ?? "-"}).");
        }

        /// <summary>
        /// Valida as regras que o DataAnnotations não cobre e monta os parâmetros do UPDATE
        /// já normalizados (textos aparados, vazio → nulo, "@" fora do Instagram, espécies
        /// sem repetição, listas serializadas para JSONB).
        /// </summary>
        /// <param name="dto">Configuração recebida.</param>
        /// <returns>Objeto de parâmetros para o Dapper.</returns>
        /// <exception cref="RegraDeNegocioException">Alguma regra foi violada.</exception>
        private static object ValidarENormalizar(ConfiguracaoOngUpdateDto dto)
        {
            var emailContato = Normalizar(dto.EmailContato);
            if (emailContato is not null && !new EmailAddressAttribute().IsValid(emailContato))
                throw new RegraDeNegocioException("Email de contato inválido.");

            if (!StatusDevolucaoPermitidos.Contains(dto.StatusAnimalDevolvido!.Value))
                throw new RegraDeNegocioException(
                    "O animal devolvido só pode voltar como \"Em tratamento\" (triagem) ou \"Disponível\".");

            var passos = dto.PassosAdocao!
                .Select(p => new PassoAdocaoDto
                {
                    Icone = p.Icone.Trim(),
                    Titulo = p.Titulo.Trim(),
                    Descricao = p.Descricao.Trim()
                })
                .ToList();

            var formas = new List<FormaAjudarDto>();
            foreach (var forma in dto.FormasAjudar!)
            {
                var rota = Normalizar(forma.Rota);
                var ancora = Normalizar(forma.Ancora);

                // O card precisa de exatamente um destino: o front usa a rota se houver, senão a âncora.
                if ((rota is null) == (ancora is null))
                    throw new RegraDeNegocioException(
                        $"O card \"{forma.Titulo.Trim()}\" deve ter exatamente um link: uma rota do site " +
                        "(ex.: \"/adotar\") ou uma âncora da página (ex.: \"#contato\").");

                formas.Add(new FormaAjudarDto
                {
                    Icone = forma.Icone.Trim(),
                    Titulo = forma.Titulo.Trim(),
                    Descricao = forma.Descricao.Trim(),
                    RotuloAcao = forma.RotuloAcao.Trim(),
                    Rota = rota,
                    Ancora = ancora,
                    Ativo = forma.Ativo
                });
            }

            return new
            {
                Id = IdConfiguracao,
                NomeOng = dto.NomeOng.Trim(),
                ImagemPadraoUrl = Normalizar(dto.ImagemPadraoUrl),
                Paleta = dto.Paleta!.Value.ToString(),
                Whatsapp = Normalizar(dto.Whatsapp),
                Instagram = Normalizar(dto.Instagram)?.TrimStart('@'),
                EmailContato = emailContato,
                Endereco = Normalizar(dto.Endereco),
                HorarioAtendimento = Normalizar(dto.HorarioAtendimento),
                TextoContato = Normalizar(dto.TextoContato),
                MensagemWhatsapp = Normalizar(dto.MensagemWhatsapp),
                HeroTitulo = dto.HeroTitulo.Trim(),
                HeroSubtitulo = dto.HeroSubtitulo.Trim(),
                QuantidadeDestaque = dto.QuantidadeDestaque!.Value,
                PassosAdocao = JsonSerializer.Serialize(passos, OpcoesJson),
                FormasAjudar = JsonSerializer.Serialize(formas, OpcoesJson),
                DoacaoPixChave = Normalizar(dto.DoacaoPixChave),
                DoacaoTexto = Normalizar(dto.DoacaoTexto),
                // Arrays vão como VARCHAR[] nativo (o Dapper não expande arrays com o Npgsql).
                EspeciesHabilitadas = ParaArrayDeEspecies(dto.EspeciesHabilitadas!),
                EspeciesComPorte = ParaArrayDeEspecies(dto.EspeciesComPorte!),
                ExibirCastrado = dto.ExibirCastrado!.Value,
                ExibirVermifugado = dto.ExibirVermifugado!.Value,
                AceitarCandidaturas = dto.AceitarCandidaturas!.Value,
                AvisoCandidaturasPausadas = Normalizar(dto.AvisoCandidaturasPausadas),
                MultiplasCandidaturas = dto.MultiplasCandidaturas!.Value,
                RecusarPendentesAoConcluir = dto.RecusarPendentesAoConcluir!.Value,
                StatusAnimalDevolvido = dto.StatusAnimalDevolvido.Value.ToString(),
                TelefoneObrigatorio = dto.TelefoneObrigatorio!.Value,
                TextoConsentimento = Normalizar(dto.TextoConsentimento),
                ExibirWhatsappAnimal = dto.ExibirWhatsappAnimal!.Value,
                ModuloVacinas = dto.ModuloVacinas!.Value,
                DiasAlertaVacina = dto.DiasAlertaVacina!.Value
            };
        }

        /// <summary>
        /// Converte a lista de espécies para o array gravado no banco: sem repetição e na
        /// ordem do enum (a ordem de envio não importa — evita "mudança" falsa na auditoria).
        /// </summary>
        private static string[] ParaArrayDeEspecies(IEnumerable<EspecieAnimal> especies) =>
            especies.Distinct().Order().Select(e => e.ToString()).ToArray();

        /// <summary>
        /// Apara o texto e converte vazio/só espaços em nulo (o item some do site).
        /// </summary>
        private static string? Normalizar(string? valor) =>
            string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

        private static string SimNao(bool valor) => valor ? "sim" : "não";

        /// <summary>
        /// Descreve quais campos mudaram entre o estado anterior e o novo (para a auditoria).
        /// Retorna null se nada mudou.
        /// </summary>
        private static string? MontarDetalhesAlteracao(ConfiguracaoOngModel antes, ConfiguracaoOngModel depois)
        {
            var mudancas = new List<string>();

            foreach (var (rotulo, mostrarValor, valor) in CamposAuditados)
            {
                var valorAntes = valor(antes);
                var valorDepois = valor(depois);
                if (valorAntes == valorDepois)
                    continue;

                mudancas.Add(mostrarValor
                    ? $"{rotulo}: '{valorAntes ?? "(vazio)"}' → '{valorDepois ?? "(vazio)"}'"
                    : $"{rotulo} alterado");
            }

            return mudancas.Count > 0 ? string.Join("; ", mudancas) : null;
        }

        /// <summary>
        /// Preenche os campos públicos do DTO a partir do Model (desserializa as colunas JSONB e
        /// converte as espécies para o enum).
        /// </summary>
        /// <param name="dto">DTO de destino (público ou a resposta completa de Admin).</param>
        /// <param name="configuracao">Linha de configuração lida do banco.</param>
        /// <param name="somenteFormasAtivas">Verdadeiro na resposta pública (esconde os cards inativos).</param>
        private static void PreencherPublica(ConfiguracaoOngPublicaDto dto, ConfiguracaoOngModel configuracao, bool somenteFormasAtivas)
        {
            var formas = JsonSerializer.Deserialize<List<FormaAjudarDto>>(configuracao.FormasAjudar, OpcoesJson) ?? [];

            dto.NomeOng = configuracao.NomeOng;
            dto.LogoUrl = configuracao.LogoUrl;
            dto.LogoLarguras = configuracao.LogoLarguras;
            dto.FaviconUrl = configuracao.FaviconUrl;
            dto.IconeAppUrl = configuracao.IconeAppUrl;
            dto.ImagemPadraoUrl = configuracao.ImagemPadraoUrl;
            dto.Paleta = Enum.Parse<PaletaCores>(configuracao.Paleta);
            dto.Whatsapp = configuracao.Whatsapp;
            dto.Instagram = configuracao.Instagram;
            dto.EmailContato = configuracao.EmailContato;
            dto.Endereco = configuracao.Endereco;
            dto.HorarioAtendimento = configuracao.HorarioAtendimento;
            dto.TextoContato = configuracao.TextoContato;
            dto.MensagemWhatsapp = configuracao.MensagemWhatsapp;
            dto.HeroTitulo = configuracao.HeroTitulo;
            dto.HeroSubtitulo = configuracao.HeroSubtitulo;
            dto.QuantidadeDestaque = configuracao.QuantidadeDestaque;
            dto.PassosAdocao = JsonSerializer.Deserialize<List<PassoAdocaoDto>>(configuracao.PassosAdocao, OpcoesJson) ?? [];
            dto.FormasAjudar = somenteFormasAtivas ? formas.Where(f => f.Ativo).ToList() : formas;
            dto.DoacaoPixChave = configuracao.DoacaoPixChave;
            dto.DoacaoTexto = configuracao.DoacaoTexto;
            dto.EspeciesHabilitadas = configuracao.EspeciesHabilitadas.Select(Enum.Parse<EspecieAnimal>).ToList();
            dto.EspeciesComPorte = configuracao.EspeciesComPorte.Select(Enum.Parse<EspecieAnimal>).ToList();
            dto.ExibirCastrado = configuracao.ExibirCastrado;
            dto.ExibirVermifugado = configuracao.ExibirVermifugado;
            dto.AceitarCandidaturas = configuracao.AceitarCandidaturas;
            dto.AvisoCandidaturasPausadas = configuracao.AvisoCandidaturasPausadas;
            dto.TelefoneObrigatorio = configuracao.TelefoneObrigatorio;
            dto.TextoConsentimento = configuracao.TextoConsentimento;
            dto.ExibirWhatsappAnimal = configuracao.ExibirWhatsappAnimal;
            dto.ModuloVacinas = configuracao.ModuloVacinas;
            dto.DiasAlertaVacina = configuracao.DiasAlertaVacina;
        }

        /// <summary>
        /// Mapeia o Model para a resposta completa de Admin (todas as formas de ajudar + regras
        /// internas de adoção).
        /// </summary>
        private static ConfiguracaoOngRespostaDto MapearParaResposta(ConfiguracaoOngModel configuracao)
        {
            var dto = new ConfiguracaoOngRespostaDto
            {
                MultiplasCandidaturas = configuracao.MultiplasCandidaturas,
                RecusarPendentesAoConcluir = configuracao.RecusarPendentesAoConcluir,
                StatusAnimalDevolvido = Enum.Parse<StatusAnimal>(configuracao.StatusAnimalDevolvido),
                DataAtualizacao = configuracao.UpdatedAt
            };
            PreencherPublica(dto, configuracao, somenteFormasAtivas: false);
            return dto;
        }
    }
}
