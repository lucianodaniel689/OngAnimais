-- =============================================================
-- 010 — Larguras geradas de cada foto (imagens responsivas, Fase 23)
-- Cada foto é salva em várias larguras ({guid}-{largura}.webp); esta coluna lista as larguras
-- reais (em pixels) que existem em disco, em ordem crescente — o front monta o `srcset` com ela
-- e o navegador escolhe a melhor para a tela. Vazia = foto ainda no formato antigo (o serviço
-- de otimização converte ao iniciar a API).
-- Idempotente: pode rodar em banco existente.
-- =============================================================

ALTER TABLE fotos ADD COLUMN IF NOT EXISTS larguras INTEGER[] NOT NULL DEFAULT '{}';

COMMENT ON COLUMN fotos.larguras IS
    'Larguras (px) disponíveis em disco, crescente: {guid}-{largura}.webp. Vazio = formato antigo.';
