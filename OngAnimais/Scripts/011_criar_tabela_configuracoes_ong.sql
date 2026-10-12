-- =============================================================
-- Script de criação da tabela de configurações da ONG
-- Banco: PostgreSQL
-- Projeto: OngAnimais API — painel de configuração (customização por ONG)
-- =============================================================
--
-- Guarda o que cada ONG pode customizar sem mexer em código: identidade
-- (nome, logo, contato), conteúdo da página pública, espécies atendidas,
-- regras de adoção e módulos do painel.
--
-- LINHA ÚNICA: a ONG tem uma configuração só. Por isso o `id` NÃO é SERIAL
-- (exceção à convenção) — é fixo em 1 e o CHECK impede uma segunda linha.
-- A aplicação só faz SELECT/UPDATE de `WHERE id = 1`, nunca INSERT/DELETE.
--
-- Os valores iniciais (INSERT no fim) reproduzem exatamente o comportamento
-- de antes desta tabela existir — rodar o script não muda nada no site até
-- um Admin editar pelo painel. Os dados de contato iniciais são os mesmos
-- PLACEHOLDERS que estavam em `frontend/.../ong.config.ts`.
--
-- Fora daqui de propósito (infraestrutura, fica no appsettings.json com
-- quem hospeda): JWT, CORS, rate limit, limites de upload e telemetria.
--
-- Idempotente: pode rodar em banco novo ou existente.
-- =============================================================

CREATE TABLE IF NOT EXISTS configuracoes_ong (
    id                              INT             PRIMARY KEY DEFAULT 1,

    -- ---------- Identidade ----------
    nome_ong                        VARCHAR(150)    NOT NULL,
    logo_url                        VARCHAR(500),               -- NULL = usa o símbolo padrão (🐾)
    imagem_padrao_url               VARCHAR(500),               -- foto de reserva (animal sem foto / vitrine vazia)

    -- ---------- Contato (NULL = item não aparece no site) ----------
    whatsapp                        VARCHAR(15),                -- só dígitos, formato internacional (ex.: 5511999999999)
    instagram                       VARCHAR(30),                -- usuário sem "@" (a URL é montada pelo front)
    email_contato                   VARCHAR(255),
    endereco                        VARCHAR(255),
    horario_atendimento             VARCHAR(150),
    texto_contato                   TEXT,                       -- parágrafo da seção "Venha nos conhecer"
    mensagem_whatsapp               VARCHAR(500),               -- mensagem pré-preenchida do link wa.me

    -- ---------- Página pública ----------
    hero_titulo                     VARCHAR(150)    NOT NULL,
    hero_subtitulo                  TEXT            NOT NULL,
    quantidade_destaque             INT             NOT NULL DEFAULT 6,   -- animais no carrossel da home
    -- [{ "icone", "titulo", "descricao" }] — os passos de "Como adotar", na ordem
    passos_adocao                   JSONB           NOT NULL DEFAULT '[]',
    -- [{ "icone", "titulo", "descricao", "rotuloAcao", "rota"?, "ancora"?, "ativo" }] — cards de "Formas de ajudar"
    formas_ajudar                   JSONB           NOT NULL DEFAULT '[]',
    doacao_pix_chave                VARCHAR(150),               -- NULL = card de doação não aparece
    doacao_texto                    TEXT,                       -- dados bancários / instruções de doação

    -- ---------- Animais ----------
    -- Subconjunto do enum EspecieAnimal (CHECK abaixo). Espécie desativada some dos
    -- selects de cadastro/filtro; animais já cadastrados com ela continuam existindo.
    especies_habilitadas            VARCHAR(20)[]   NOT NULL DEFAULT '{Cachorro,Gato,Ave,Roedor,Reptil,Outro}',
    -- Espécies em que o campo "porte" faz sentido (ex.: ONG de gatos deixa vazio).
    especies_com_porte              VARCHAR(20)[]   NOT NULL DEFAULT '{Cachorro,Gato,Ave,Roedor,Reptil,Outro}',
    exibir_castrado                 BOOLEAN         NOT NULL DEFAULT TRUE,   -- selo na página pública
    exibir_vermifugado              BOOLEAN         NOT NULL DEFAULT TRUE,

    -- ---------- Adoção ----------
    aceitar_candidaturas            BOOLEAN         NOT NULL DEFAULT TRUE,   -- FALSE = POST /api/adocao recusa (adoções pausadas)
    aviso_candidaturas_pausadas     TEXT,                                    -- mensagem exibida quando pausado
    multiplas_candidaturas          BOOLEAN         NOT NULL DEFAULT TRUE,   -- FALSE = a 1ª candidatura já reserva o animal
    recusar_pendentes_ao_concluir   BOOLEAN         NOT NULL DEFAULT TRUE,   -- auto-recusa das demais ao concluir
    status_animal_devolvido         VARCHAR(30)     NOT NULL DEFAULT 'EmTratamento',
    telefone_obrigatorio            BOOLEAN         NOT NULL DEFAULT TRUE,
    texto_consentimento             TEXT,                                    -- aviso LGPD no form (NULL = sem aviso)
    exibir_whatsapp_animal          BOOLEAN         NOT NULL DEFAULT TRUE,   -- botão "Falar no WhatsApp" na página do animal

    -- ---------- Módulos do painel ----------
    modulo_vacinas                  BOOLEAN         NOT NULL DEFAULT TRUE,
    dias_alerta_vacina              INT             NOT NULL DEFAULT 30,     -- janela "próximas doses" do dashboard

    created_at                      TIMESTAMP       NOT NULL DEFAULT NOW(),
    updated_at                      TIMESTAMP       NOT NULL DEFAULT NOW(),

    CONSTRAINT chk_configuracoes_ong_linha_unica    CHECK (id = 1),
    CONSTRAINT chk_configuracoes_ong_whatsapp       CHECK (whatsapp IS NULL OR whatsapp ~ '^[0-9]{10,15}$'),
    CONSTRAINT chk_configuracoes_ong_destaque       CHECK (quantidade_destaque BETWEEN 1 AND 20),
    CONSTRAINT chk_configuracoes_ong_passos         CHECK (jsonb_typeof(passos_adocao) = 'array'),
    CONSTRAINT chk_configuracoes_ong_formas         CHECK (jsonb_typeof(formas_ajudar) = 'array'),
    CONSTRAINT chk_configuracoes_ong_especies       CHECK (
        cardinality(especies_habilitadas) >= 1
        AND especies_habilitadas <@ ARRAY['Cachorro', 'Gato', 'Ave', 'Roedor', 'Reptil', 'Outro']::VARCHAR(20)[]
    ),
    CONSTRAINT chk_configuracoes_ong_especies_porte CHECK (
        especies_com_porte <@ ARRAY['Cachorro', 'Gato', 'Ave', 'Roedor', 'Reptil', 'Outro']::VARCHAR(20)[]
    ),
    CONSTRAINT chk_configuracoes_ong_devolvido      CHECK (status_animal_devolvido IN ('EmTratamento', 'Disponivel')),
    CONSTRAINT chk_configuracoes_ong_alerta_vacina  CHECK (dias_alerta_vacina BETWEEN 1 AND 365)
);

-- Linha inicial = comportamento atual do site (ON CONFLICT: não sobrescreve o que o Admin já editou).
INSERT INTO configuracoes_ong (
    id, nome_ong,
    whatsapp, instagram, endereco, horario_atendimento, texto_contato, mensagem_whatsapp,
    hero_titulo, hero_subtitulo, passos_adocao, formas_ajudar
) VALUES (
    1,
    'OngAnimais',
    '5511999999999',
    'onganimais',
    'Rua das Acácias, 245 — Jardim Primavera, São Paulo/SP',
    'Terça a domingo, das 9h às 17h',
    'Atendemos com hora marcada — chame no WhatsApp antes de aparecer, assim garantimos alguém disponível pra te receber.',
    'Olá! Vim pelo site da OngAnimais.',
    'Adote um amigo para a vida toda',
    'Todos já foram resgatados, estão com a saúde em dia e esperam por um lar. Escolha, converse com a nossa equipe e leve seu novo companheiro pra casa.',
    '[
        { "icone": "search",   "titulo": "Conheça",        "descricao": "Navegue pelos animais disponíveis e veja fotos, idade e temperamento de cada um." },
        { "icone": "favorite", "titulo": "Solicite",       "descricao": "Encontrou um match? Preencha o formulário com seus dados na página do animal." },
        { "icone": "home",     "titulo": "Leve para casa", "descricao": "Nossa equipe entra em contato para combinar os próximos passos da adoção." }
    ]',
    '[
        { "icone": "pets",               "titulo": "Adote",           "descricao": "Já é possível agora: veja quem está disponível e comece pela página do animal.",          "rotuloAcao": "Ver animais disponíveis",    "rota": "/adotar",     "ativo": true },
        { "icone": "volunteer_activism", "titulo": "Apadrinhe",       "descricao": "Contribua mensalmente com ração e cuidados de um animal, mesmo sem levá-lo pra casa.",    "rotuloAcao": "Falar sobre apadrinhamento", "ancora": "#contato", "ativo": true },
        { "icone": "groups",             "titulo": "Seja voluntário", "descricao": "Passeios, banho e tosa, transporte ou apoio em feiras de adoção — toda ajuda conta.",      "rotuloAcao": "Quero ser voluntário",       "ancora": "#contato", "ativo": true },
        { "icone": "campaign",           "titulo": "Divulgue",        "descricao": "Compartilhe o perfil de um animal com quem você conhece — não custa nada e alcança longe.", "rotuloAcao": "Escolher quem divulgar",     "rota": "/adotar",     "ativo": true }
    ]'
)
ON CONFLICT (id) DO NOTHING;

-- Auditoria: nova categoria 'Configuracao' (alterações feitas pelo painel). O 009 já nasce com
-- ela em banco novo; em banco existente o CHECK antigo precisa ser recriado (DROP + ADD é
-- idempotente — ADD CONSTRAINT não suporta IF NOT EXISTS).
ALTER TABLE auditoria DROP CONSTRAINT IF EXISTS chk_auditoria_categoria;
ALTER TABLE auditoria ADD CONSTRAINT chk_auditoria_categoria
    CHECK (categoria IN ('Seguranca', 'Adocao', 'Animal', 'Usuario', 'Configuracao'));

-- =============================================================
-- Como executar:
--      psql -U postgres -d onganimais_dev -f Scripts/011_criar_tabela_configuracoes_ong.sql
-- =============================================================
