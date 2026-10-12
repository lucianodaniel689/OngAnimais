-- =============================================================
-- 012 — Aparência do site nas configurações da ONG (Fase 24)
-- Banco: PostgreSQL
-- =============================================================
--
-- paleta          : paleta de cores escolhida (enum PaletaCores — os tons ficam no frontend).
--                   'Kraft' = a identidade original do sistema.
-- logo_larguras   : larguras (px) das versões WebP do logo ({guid}-{largura}.webp), igual a
--                   fotos.larguras — o front monta o srcset. logo_url (script 011) aponta para a maior.
-- favicon_url     : ícone da aba do navegador (PNG quadrado 64px, fundo transparente),
--                   gerado a partir do logo enviado.
-- icone_app_url   : ícone de atalho na tela inicial do celular (PNG quadrado 180px).
--
-- O logo deixa de ser editado pelo PUT da configuração: só muda pelo upload
-- (POST/DELETE /api/configuracao/logo), que grava os 4 campos de logo juntos.
--
-- Idempotente: pode rodar em banco novo ou existente.
-- =============================================================

ALTER TABLE configuracoes_ong ADD COLUMN IF NOT EXISTS paleta        VARCHAR(30)  NOT NULL DEFAULT 'Kraft';
ALTER TABLE configuracoes_ong ADD COLUMN IF NOT EXISTS logo_larguras INTEGER[]    NOT NULL DEFAULT '{}';
ALTER TABLE configuracoes_ong ADD COLUMN IF NOT EXISTS favicon_url   VARCHAR(500);
ALTER TABLE configuracoes_ong ADD COLUMN IF NOT EXISTS icone_app_url VARCHAR(500);

-- ADD CONSTRAINT não suporta IF NOT EXISTS — DROP + ADD mantém o script idempotente.
ALTER TABLE configuracoes_ong DROP CONSTRAINT IF EXISTS chk_configuracoes_ong_paleta;
ALTER TABLE configuracoes_ong ADD CONSTRAINT chk_configuracoes_ong_paleta
    CHECK (paleta IN ('Kraft', 'Floresta', 'Oceano', 'Lavanda', 'Terra'));

-- =============================================================
-- Como executar:
--      psql -U postgres -d onganimais_dev -f Scripts/012_alterar_configuracoes_ong_aparencia.sql
-- =============================================================
