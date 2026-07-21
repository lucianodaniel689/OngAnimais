/**
 * Dados de contato da ONG usados em vários pontos do site (seção de contato da
 * home, botão de WhatsApp na página do animal, etc.).
 *
 * ⚠️ Dados de exemplo (placeholder) — substituir pelos reais da ONG antes de
 * publicar. Centralizados aqui pra trocar em um lugar só.
 */

/** Número de WhatsApp em formato internacional (só dígitos), usado em links `wa.me`. */
export const WHATSAPP_ONG = '5511999999999';

/** Mesmo número, formatado pra exibição. */
export const WHATSAPP_ONG_FORMATADO = '(11) 99999-9999';

/** Perfil do Instagram (com @). */
export const INSTAGRAM_ONG = '@onganimais';

/** URL do perfil do Instagram. */
export const INSTAGRAM_ONG_URL = 'https://instagram.com/onganimais';

/** Endereço físico do abrigo, em uma linha. */
export const ENDERECO_ONG = 'Rua das Acácias, 245 — Jardim Primavera, São Paulo/SP';

/** Horário de atendimento presencial. */
export const HORARIO_ONG = 'Terça a domingo, das 9h às 17h';

/** Monta um link `wa.me` com mensagem pré-preenchida. */
export function linkWhatsapp(mensagem: string): string {
  return `https://wa.me/${WHATSAPP_ONG}?text=${encodeURIComponent(mensagem)}`;
}
