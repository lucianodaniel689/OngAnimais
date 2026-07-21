-- =============================================================
-- Script de criação da tabela de adotantes
-- Banco: PostgreSQL
-- Projeto: OngAnimais API
-- =============================================================
--
-- Representa quem solicita a adoção de um animal pelo site público.
-- Diferente de 'usuarios' (login exclusivo dos funcionários da ONG),
-- adotante NÃO tem conta/senha — é só um registro de contato para o
-- funcionário retornar a solicitação.
-- =============================================================

CREATE TABLE IF NOT EXISTS adotantes (
    id              SERIAL PRIMARY KEY,
    nome            VARCHAR(150)    NOT NULL,
    email           VARCHAR(255)    NOT NULL,
    telefone        VARCHAR(20)     NOT NULL,
    created_at      TIMESTAMP       NOT NULL DEFAULT NOW()
);

-- Índice para localizar solicitações anteriores do mesmo adotante.
CREATE INDEX IF NOT EXISTS idx_adotantes_email ON adotantes (email);
