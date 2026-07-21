-- =============================================================
-- Script de criação da tabela de fotos
-- Banco: PostgreSQL
-- Projeto: OngAnimais API
-- Galeria de fotos por animal.
-- =============================================================

CREATE TABLE IF NOT EXISTS fotos (
    id                  SERIAL PRIMARY KEY,
    animal_id           INT             NOT NULL,
    url                 VARCHAR(500)    NOT NULL,
    descricao           VARCHAR(255),
    principal           BOOLEAN         NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMP       NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMP       NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_fotos_animal FOREIGN KEY (animal_id) REFERENCES animais(id)
);

-- Índice para buscar a galeria de um animal
CREATE INDEX IF NOT EXISTS idx_fotos_animal_id ON fotos (animal_id);
