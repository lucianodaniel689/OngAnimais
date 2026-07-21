// Helpers de UI sobre os enums gerados pelo openapi-generator.
// Arrays para popular selects e mapas de rótulos amigáveis (com acento).
import {
  CategoriaAuditoria,
  EspecieAnimal,
  PerfilUsuario,
  PorteAnimal,
  SexoAnimal,
  StatusAdocao,
  StatusAnimal,
} from '@app/core/api';

export const ESPECIES: EspecieAnimal[] = Object.values(EspecieAnimal);
export const SEXOS: SexoAnimal[] = Object.values(SexoAnimal);
export const PORTES: PorteAnimal[] = Object.values(PorteAnimal);
export const STATUS_ANIMAL: StatusAnimal[] = Object.values(StatusAnimal);
export const STATUS_ADOCAO: StatusAdocao[] = Object.values(StatusAdocao);
export const PERFIS_USUARIO: PerfilUsuario[] = Object.values(PerfilUsuario);

export const ROTULO_STATUS_ANIMAL: Record<StatusAnimal, string> = {
  [StatusAnimal.Disponivel]: 'Disponível',
  [StatusAnimal.Adotado]: 'Adotado',
  [StatusAnimal.EmTratamento]: 'Em tratamento',
  [StatusAnimal.Reservado]: 'Reservado',
};

export const ROTULO_ESPECIE: Record<EspecieAnimal, string> = {
  [EspecieAnimal.Cachorro]: 'Cachorro',
  [EspecieAnimal.Gato]: 'Gato',
  [EspecieAnimal.Ave]: 'Ave',
  [EspecieAnimal.Roedor]: 'Roedor',
  [EspecieAnimal.Reptil]: 'Réptil',
  [EspecieAnimal.Outro]: 'Outro',
};

export const ROTULO_SEXO: Record<SexoAnimal, string> = {
  [SexoAnimal.Macho]: 'Macho',
  [SexoAnimal.Femea]: 'Fêmea',
};

export const ROTULO_PORTE: Record<PorteAnimal, string> = {
  [PorteAnimal.Pequeno]: 'Pequeno',
  [PorteAnimal.Medio]: 'Médio',
  [PorteAnimal.Grande]: 'Grande',
};

export const ROTULO_PERFIL_USUARIO: Record<PerfilUsuario, string> = {
  [PerfilUsuario.Admin]: 'Administrador',
  [PerfilUsuario.Comum]: 'Comum',
};

export const CATEGORIAS_AUDITORIA: CategoriaAuditoria[] = Object.values(CategoriaAuditoria);

export const ROTULO_CATEGORIA_AUDITORIA: Record<CategoriaAuditoria, string> = {
  [CategoriaAuditoria.Seguranca]: 'Segurança',
  [CategoriaAuditoria.Adocao]: 'Adoção',
  [CategoriaAuditoria.Animal]: 'Animal',
  [CategoriaAuditoria.Usuario]: 'Funcionário',
};

export const ROTULO_STATUS_ADOCAO: Record<StatusAdocao, string> = {
  [StatusAdocao.Pendente]: 'Pendente',
  [StatusAdocao.Aprovada]: 'Aprovada',
  [StatusAdocao.Recusada]: 'Recusada',
  [StatusAdocao.Concluida]: 'Concluída',
  [StatusAdocao.Cancelada]: 'Cancelada',
};
