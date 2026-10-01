#!/usr/bin/env bash
# Smoke test contra o container no ar. Uso: scripts/smoke-test.sh [URL_BASE]
# Confere o caminho feliz e, principalmente, que cada vetor de SSRF volta 400 com motivo.
set -u
BASE="${1:-http://localhost:5000}"
falhas=0

# Espera o Kestrel subir (até 30s)
for _ in $(seq 1 30); do
  curl -fs -o /dev/null "$BASE/" && break
  sleep 1
done

checar() { # descricao, status esperado, status obtido
  if [ "$2" = "$3" ]; then echo "  ok    $1 -> $3"; else echo "  FALHA $1 -> esperado $2, veio $3"; falhas=$((falhas+1)); fi
}

post() { # nome, url -> imprime o status HTTP
  curl -s -o /tmp/smoke-corpo -w '%{http_code}' -X POST "$BASE/api/monitor" \
    -H 'Content-Type: application/json' -d "{\"nome\":\"$1\",\"url\":\"$2\"}"
}

echo "Página e roteamento"
checar "GET /"                    200 "$(curl -s -o /dev/null -w '%{http_code}' "$BASE/")"

echo "SSRF e validação (devem ser recusados com 400)"
checar "loopback por nome"         400 "$(post a http://localhost:5000)"
checar "loopback por IP"           400 "$(post a http://127.0.0.1)"
checar "loopback IPv6"             400 "$(post a 'http://[::1]')"
checar "loopback em decimal"       400 "$(post a http://2130706433)"
checar "metadados da nuvem"        400 "$(post a http://169.254.169.254/latest/meta-data/)"
checar "rede privada 10/8"         400 "$(post a http://10.0.0.1)"
checar "rede privada 192.168/16"   400 "$(post a http://192.168.0.1)"
checar "domínio que aponta p/ 127" 400 "$(post a http://localtest.me)"
checar "protocolo ftp"             400 "$(post a ftp://example.com)"
checar "nome vazio"                400 "$(post '' https://example.com)"
# A mensagem precisa chegar ao usuário (antes vinha 500 com corpo vazio)
checar "mensagem no corpo do 400"  sim "$(post a http://127.0.0.1 >/dev/null; grep -q 'segurança' /tmp/smoke-corpo && echo sim || echo nao)"

echo "Caminho feliz"
checar "URL pública"               200 "$(post exemplo https://example.com)"

echo
if [ "$falhas" -gt 0 ]; then echo "$falhas verificação(ões) falharam"; exit 1; fi
echo "Todas as verificações passaram"
