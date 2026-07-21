-- =============================================================
-- Script de criação da tabela de vacinas
-- Banco: PostgreSQL
-- Projeto: OngAnimais API
-- Histórico de vacinas aplicadas por animal.
-- =============================================================

CREATE TABLE IF NOT EXISTS vacinas (
    id                  SERIAL PRIMARY KEY,
    animal_id           INT             NOT NULL,
    nome                VARCHAR(150)    NOT NULL,
    data_aplicacao      DATE            NOT NULL,
    proxima_dose        DATE,
    veterinario         VARCHAR(150),
    observacoes         TEXT,
    created_at          TIMESTAMP       NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMP       NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_vacinas_animal FOREIGN KEY (animal_id) REFERENCES animais(id)
);

-- Índice para buscar o histórico de vacinas de um animal
CREATE INDEX IF NOT EXISTS idx_vacinas_animal_id ON vacinas (animal_id);
