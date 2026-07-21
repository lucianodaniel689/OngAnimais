-- =============================================================
-- Script de criação da tabela de adoções
-- Banco: PostgreSQL
-- Projeto: OngAnimais API
-- =============================================================

CREATE TABLE IF NOT EXISTS adocoes (
    id                  SERIAL PRIMARY KEY,
    animal_id           INT             NOT NULL,
    adotante_id         INT             NOT NULL,
    status              VARCHAR(30)     NOT NULL DEFAULT 'Pendente',
    data_solicitacao    TIMESTAMP       NOT NULL DEFAULT NOW(),
    data_conclusao      TIMESTAMP,
    observacoes         TEXT,
    created_at          TIMESTAMP       NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMP       NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_adocoes_animal    FOREIGN KEY (animal_id)   REFERENCES animais(id),
    CONSTRAINT fk_adocoes_adotante  FOREIGN KEY (adotante_id) REFERENCES adotantes(id),
    CONSTRAINT chk_adocoes_status CHECK (status IN ('Pendente', 'Aprovada', 'Recusada', 'Concluida', 'Cancelada'))
);

-- Índices para buscas frequentes
CREATE INDEX IF NOT EXISTS idx_adocoes_animal_id   ON adocoes (animal_id);
CREATE INDEX IF NOT EXISTS idx_adocoes_adotante_id ON adocoes (adotante_id);
CREATE INDEX IF NOT EXISTS idx_adocoes_status      ON adocoes (status);
