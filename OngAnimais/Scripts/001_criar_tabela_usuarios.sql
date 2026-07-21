-- =============================================================
-- Script de criação da tabela de usuários
-- Banco: PostgreSQL
-- Projeto: OngAnimais API
-- =============================================================

CREATE TABLE IF NOT EXISTS usuarios (
    id              SERIAL PRIMARY KEY,
    nome            VARCHAR(150)    NOT NULL,
    email           VARCHAR(255)    NOT NULL UNIQUE,
    senha_hash      VARCHAR(255)    NOT NULL,
    perfil          VARCHAR(20)     NOT NULL DEFAULT 'Comum'
                    CONSTRAINT chk_usuarios_perfil CHECK (perfil IN ('Admin', 'Comum')),
    ativo           BOOLEAN         NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMP       NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMP       NOT NULL DEFAULT NOW()
);

-- Índice para buscas por email (login)
CREATE INDEX IF NOT EXISTS idx_usuarios_email ON usuarios (email);

-- =============================================================
-- Como executar:
-- 1. Certifique-se de que o PostgreSQL está rodando
-- 2. Crie o banco de dados (caso não exista):
--      CREATE DATABASE onganimais;
-- 3. Conecte ao banco:
--      \c onganimais
-- 4. Execute este script:
--      \i caminho/para/001_criar_tabela_usuarios.sql
--
-- Ou via terminal:
--      psql -U postgres -d onganimais -f Scripts/001_criar_tabela_usuarios.sql
-- =============================================================
