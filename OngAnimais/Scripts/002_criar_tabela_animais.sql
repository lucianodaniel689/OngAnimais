-- =============================================================
-- Script de criação da tabela de animais
-- Banco: PostgreSQL
-- Projeto: OngAnimais API
-- =============================================================

CREATE TABLE IF NOT EXISTS animais (
    id                  SERIAL PRIMARY KEY,
    nome                VARCHAR(150)    NOT NULL,
    especie             VARCHAR(50)     NOT NULL,
    sexo                VARCHAR(20)     NOT NULL,
    idade_aproximada    VARCHAR(50),
    porte               VARCHAR(20)     NOT NULL,
    descricao           TEXT,
    status              VARCHAR(30)     NOT NULL DEFAULT 'Disponivel',
    castrado            BOOLEAN         NOT NULL DEFAULT FALSE,
    vermifugado         BOOLEAN         NOT NULL DEFAULT FALSE,
    data_cadastro       TIMESTAMP       NOT NULL DEFAULT NOW(),
    ativo               BOOLEAN         NOT NULL DEFAULT TRUE,
    updated_at          TIMESTAMP       NOT NULL DEFAULT NOW(),

    CONSTRAINT chk_animais_especie CHECK (especie IN ('Cachorro', 'Gato', 'Ave', 'Roedor', 'Reptil', 'Outro')),
    CONSTRAINT chk_animais_sexo    CHECK (sexo    IN ('Macho', 'Femea')),
    CONSTRAINT chk_animais_porte   CHECK (porte   IN ('Pequeno', 'Medio', 'Grande')),
    CONSTRAINT chk_animais_status  CHECK (status  IN ('Disponivel', 'Adotado', 'EmTratamento', 'Reservado'))
);

-- Índices para buscas frequentes
CREATE INDEX IF NOT EXISTS idx_animais_especie ON animais (especie);
CREATE INDEX IF NOT EXISTS idx_animais_status ON animais (status);
