-- =============================================================
-- Script de criação da tabela de auditoria
-- Banco: PostgreSQL
-- Projeto: OngAnimais API — Fase 16 (auditoria e segurança)
-- =============================================================
--
-- Registro imutável de eventos de negócio (quem aprovou uma adoção,
-- quem desativou um funcionário) e de segurança (tentativas de login).
-- A aplicação apenas INSERE nesta tabela — nunca UPDATE ou DELETE.
--
-- usuario_id é NULL em ações públicas (candidatura de adoção, login
-- falho de email desconhecido); usuario_nome é um snapshot do nome no
-- momento do evento, para o histórico sobreviver à exclusão da conta
-- (a FK usa ON DELETE SET NULL pelo mesmo motivo).
-- =============================================================

CREATE TABLE IF NOT EXISTS auditoria (
    id              SERIAL PRIMARY KEY,
    usuario_id      INT,
    usuario_nome    VARCHAR(150),
    categoria       VARCHAR(30)     NOT NULL,
    acao            VARCHAR(60)     NOT NULL,
    entidade_id     INT,
    detalhes        TEXT,
    ip              VARCHAR(45),
    created_at      TIMESTAMP       NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_auditoria_usuario FOREIGN KEY (usuario_id)
        REFERENCES usuarios(id) ON DELETE SET NULL,
    CONSTRAINT chk_auditoria_categoria CHECK (categoria IN ('Seguranca', 'Adocao', 'Animal', 'Usuario'))
);

-- Índices para os filtros da tela de auditoria (categoria, funcionário, período)
CREATE INDEX IF NOT EXISTS idx_auditoria_categoria  ON auditoria (categoria);
CREATE INDEX IF NOT EXISTS idx_auditoria_usuario_id ON auditoria (usuario_id);
CREATE INDEX IF NOT EXISTS idx_auditoria_created_at ON auditoria (created_at);

-- =============================================================
-- Como executar:
--      psql -U postgres -d onganimais_dev -f Scripts/009_criar_tabela_auditoria.sql
-- =============================================================
