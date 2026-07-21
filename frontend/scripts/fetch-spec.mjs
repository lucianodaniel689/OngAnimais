// Baixa o OpenAPI (swagger.json) da API em execução e salva na raiz do projeto.
// A URL pode ser sobrescrita via variável de ambiente SPEC_URL.
// Uso: npm run api:spec   (a API precisa estar rodando — dotnet run)
import { writeFileSync } from 'node:fs';

const url = process.env.SPEC_URL ?? 'http://localhost:5049/swagger/v1/swagger.json';

try {
  const resp = await fetch(url);
  if (!resp.ok) {
    throw new Error(`HTTP ${resp.status} ${resp.statusText}`);
  }
  const spec = await resp.text();
  writeFileSync('swagger.json', spec);
  console.log(`✓ swagger.json atualizado a partir de ${url} (${spec.length} bytes)`);
} catch (erro) {
  console.error(`✗ Não foi possível baixar o spec de ${url}`);
  console.error(`  ${erro.message}`);
  console.error('  A API está rodando? (dotnet run --project ../OngAnimais/OngAnimais/OngAnimais.csproj)');
  process.exit(1);
}
