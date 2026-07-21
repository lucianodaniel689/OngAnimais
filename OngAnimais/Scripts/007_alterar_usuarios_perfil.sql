-- =============================================================
-- Script de alteração da tabela de usuários: perfil de acesso
-- Banco: PostgreSQL
-- Projeto: OngAnimais API — Fase 14 (perfis Admin × Comum)
-- =============================================================
--
-- Adiciona a coluna 'perfil' (Admin | Comum). Idempotente: seguro
-- reexecutar em bancos novos (001 já cria a coluna) ou existentes.
-- =============================================================

ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS perfil VARCHAR(20) NOT NULL DEFAULT 'Comum';

-- CHECK constraint (ADD CONSTRAINT não suporta IF NOT EXISTS — usa DO block)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_usuarios_perfil') THEN
        ALTER TABLE usuarios
            ADD CONSTRAINT chk_usuarios_perfil CHECK (perfil IN ('Admin', 'Comum'));
    END IF;
END$$;

-- Garante que sempre exista pelo menos um Admin em bancos já populados:
-- promove o usuário ativo mais antigo, caso nenhum admin exista ainda.
UPDATE usuarios
SET perfil = 'Admin', updated_at = NOW()
WHERE id = (SELECT id FROM usuarios WHERE ativo = TRUE ORDER BY id LIMIT 1)
  AND NOT EXISTS (SELECT 1 FROM usuarios WHERE perfil = 'Admin');

-- =============================================================
-- Como executar:
--      psql -U postgres -d onganimais_dev -f Scripts/007_alterar_usuarios_perfil.sql
-- =============================================================
