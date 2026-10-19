# Estabilização do ProspectAI

## Objetivo
Tornar o projeto confiável para uso individual sem serviços pagos, preservando um caminho seguro para futura comercialização.

## Decisões
- Autenticação local somente quando ativada explicitamente por configuração; Supabase permanece como opção futura.
- OpenStreetMap é a fonte gratuita padrão. Apify e Google Places só operam quando configurados.
- Dados ausentes nunca são estimados. Provedores sintéticos e seed demonstrativo ficam desativados no fluxo normal.
- PostgreSQL e SQLite devem oferecer os mesmos filtros básicos.

## Tarefas
- [x] Especificar com testes o comportamento de autenticação local, cache e busca sem dados inventados.
- [x] Restringir provedores sintéticos e remover avaliações fabricadas do OpenStreetMap.
- [x] Corrigir cache, limites e deduplicação da busca.
- [x] Implementar autenticação local explícita, sessão persistente e proteção das páginas.
- [x] Tornar filtros compatíveis com SQLite/PostgreSQL e cobri-los com integração.
- [x] Desativar seed demonstrativo por padrão e tornar a exportação Excel portátil.
- [x] Atualizar configuração e documentação de execução.
- [x] Executar formatação, compilação e todos os testes.

## Concluído quando
- [x] Nenhuma busca normal retorna contato ou avaliação fabricados.
- [x] Login local rejeita credenciais incorretas e sobrevive à atualização da página.
- [x] Páginas privadas exigem autenticação.
- [x] Build sem avisos e testes aprovados.

## Gatilho de revisão
Ao iniciar a comercialização, tornar Supabase obrigatório e revisar isolamento multiusuário, políticas de banco e limites de consumo.
