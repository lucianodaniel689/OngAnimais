-- =============================================================
-- Script MESTRE de criação do banco + todas as tabelas
-- Banco: PostgreSQL
-- Projeto: OngAnimais API
-- =============================================================
--
-- Este script cria o banco de desenvolvimento (caso não exista),
-- conecta-se a ele e cria todas as tabelas na ordem correta
-- (respeitando as chaves estrangeiras).
--
-- Como executar (a partir da pasta Scripts/):
--      psql -U postgres -f 000_criar_banco_e_tabelas.sql
--
-- Observação: os comandos \gexec, \c e \i abaixo são meta-comandos
-- do psql — execute este arquivo pelo psql, não por outra ferramenta
-- que não os interprete.
-- =============================================================

-- 1) Cria o banco somente se ainda não existir.
--    (PostgreSQL não suporta CREATE DATABASE IF NOT EXISTS; usamos \gexec.)
SELECT 'CREATE DATABASE onganimais_dev'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'onganimais_dev')\gexec

-- 2) Conecta ao banco recém-criado.
\c onganimais_dev

-- 3) Cria as tabelas na ordem de dependência (FKs).
--    Cada script usa IF NOT EXISTS, então é seguro reexecutar.
\i 001_criar_tabela_usuarios.sql
\i 002_criar_tabela_animais.sql
\i 003_criar_tabela_adotantes.sql
\i 004_criar_tabela_adocoes.sql
\i 005_criar_tabela_vacinas.sql
\i 006_criar_tabela_fotos.sql

-- 4) Alterações incrementais (idempotentes — seguras em banco novo ou existente).
\i 007_alterar_usuarios_perfil.sql
\i 008_alterar_animais_saude.sql

-- 5) Tabelas adicionadas em fases posteriores.
\i 009_criar_tabela_auditoria.sql

\echo '============================================================='
\echo 'Banco onganimais_dev e tabelas criados/atualizados com sucesso.'
\echo '============================================================='
