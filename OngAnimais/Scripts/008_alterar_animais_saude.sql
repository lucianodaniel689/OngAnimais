-- =============================================================
-- Script de alteração da tabela de animais: dados de saúde
-- Banco: PostgreSQL
-- Projeto: OngAnimais API — Fase 14 (castrado / vermifugado)
-- =============================================================
--
-- Adiciona as colunas 'castrado' e 'vermifugado'. Idempotente:
-- seguro reexecutar em bancos novos (002 já cria as colunas) ou existentes.
-- =============================================================

ALTER TABLE animais ADD COLUMN IF NOT EXISTS castrado    BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE animais ADD COLUMN IF NOT EXISTS vermifugado BOOLEAN NOT NULL DEFAULT FALSE;

-- =============================================================
-- Como executar:
--      psql -U postgres -d onganimais_dev -f Scripts/008_alterar_animais_saude.sql
-- =============================================================
