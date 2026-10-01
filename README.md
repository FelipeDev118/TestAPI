# TestAPI — Monitor de Disponibilidade de APIs

[![CI](https://github.com/FelipeDev118/TestAPI/actions/workflows/ci.yml/badge.svg)](https://github.com/FelipeDev118/TestAPI/actions/workflows/ci.yml)

Informe uma URL e o servidor testa se ela está no ar, devolvendo um semáforo (verde/vermelho) com o código HTTP ou o motivo da falha.

**Stack:** C# · ASP.NET Core · Docker (multi-stage)

## Como funciona

- `POST /api/monitor` com `{ "nome": "...", "url": "..." }`
- Verificações em paralelo (`Task.WhenAll`), timeout de 4s por `CancellationToken` e um `HttpClient` compartilhado para não esgotar sockets.
- Redirecionamento (3xx) conta como "no ar" e mostra o destino — sem segui-lo.

## Segurança: proteção contra SSRF

Como é o **servidor** que faz a requisição, a URL informada é validada antes (`Services/ValidadorDeAlvo.cs`):

- só `http`/`https`;
- o host é **resolvido** e todos os IPs são conferidos — recusa loopback, redes privadas (10/8, 172.16/12, 192.168/16, CGNAT), link-local e o endpoint de metadados da nuvem (`169.254.169.254`), inclusive quando escondidos atrás de um domínio ou em notação decimal;
- redirecionamento automático desligado, para uma URL pública não redirecionar para um alvo interno.

Recusas voltam **HTTP 400 com o motivo**.

## Rodar

```bash
docker build -t testapi .
docker run -p 5000:5000 testapi      # http://localhost:5000
./scripts/smoke-test.sh               # em outro terminal
```

## CI

A cada push o GitHub Actions compila a imagem de produção, sobe o container e roda `scripts/smoke-test.sh`: página, 10 vetores de SSRF/validação e o caminho feliz.
