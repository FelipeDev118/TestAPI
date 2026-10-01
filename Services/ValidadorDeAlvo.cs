using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TestAPI.Models;

namespace TestAPI.Services
{
    /// <summary>
    /// Decide se uma URL pode ser testada pelo nosso servidor (defesa contra SSRF).
    /// Lança ArgumentException com a mensagem que vai para o usuário (HTTP 400).
    /// </summary>
    /// <remarks>
    /// SSRF (Server-Side Request Forgery): o atacante usa o NOSSO servidor para alcançar
    /// endereços que ele não alcança de fora — a rede interna, ou o endpoint de metadados
    /// da nuvem (169.254.169.254), que entrega credenciais da máquina.
    ///
    /// Comparar o texto da URL ("localhost", "127.0.0.1") não basta: um domínio público
    /// pode apontar para 127.0.0.1, e "http://2130706433" também é 127.0.0.1. Por isso
    /// o host é RESOLVIDO e cada IP resultante é conferido.
    ///
    /// Limitação conhecida: entre esta checagem e a conexão do HttpClient o DNS é resolvido
    /// de novo (DNS rebinding). Fechar isso exige validar no momento da conexão
    /// (SocketsHttpHandler.ConnectCallback), disponível a partir do .NET 5.
    /// </remarks>
    public static class ValidadorDeAlvo
    {
        public static async Task ValidarAsync(ApiFoco api)
        {
            if (string.IsNullOrWhiteSpace(api.Nome))
            {
                throw new ArgumentException("O nome da API não pode estar vazio.");
            }
            api.Nome = api.Nome.Trim();

            if (string.IsNullOrWhiteSpace(api.Url))
            {
                throw new ArgumentException("A URL da API não pode estar vazia.");
            }
            api.Url = api.Url.Trim();

            // Só URL web absoluta: bloqueia file://, ftp://, javascript: e caminhos locais
            bool urlValida = Uri.TryCreate(api.Url, UriKind.Absolute, out Uri uri)
                             && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
            if (!urlValida)
            {
                throw new ArgumentException("A URL fornecida é inválida ou usa um protocolo inseguro. Use apenas HTTP ou HTTPS.");
            }

            IPAddress[] enderecos;
            try
            {
                enderecos = IPAddress.TryParse(uri.DnsSafeHost, out IPAddress literal)
                    ? new[] { literal }
                    : await Dns.GetHostAddressesAsync(uri.DnsSafeHost);
            }
            catch (SocketException)
            {
                throw new ArgumentException($"Não foi possível resolver o endereço '{uri.Host}'.");
            }

            // Basta UM IP interno para recusar: o HttpClient pode escolher qualquer um deles
            if (enderecos.Length == 0 || enderecos.Any(EhEnderecoInterno))
            {
                throw new ArgumentException("Por motivos de segurança, não é permitido testar endereços locais, de rede interna ou de metadados da nuvem.");
            }
        }

        private static bool EhEnderecoInterno(IPAddress ip)
        {
            if (ip.IsIPv4MappedToIPv6)
            {
                ip = ip.MapToIPv4();
            }

            if (IPAddress.IsLoopback(ip))
            {
                return true;
            }

            if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                byte[] b6 = ip.GetAddressBytes();
                return ip.Equals(IPAddress.IPv6None)          // ::
                       || ip.IsIPv6LinkLocal                  // fe80::/10
                       || ip.IsIPv6SiteLocal                  // fec0::/10
                       || (b6[0] & 0xFE) == 0xFC;             // fc00::/7 (rede privada IPv6)
            }

            byte[] b = ip.GetAddressBytes();
            return b[0] == 0                                  // 0.0.0.0/8
                   || b[0] == 10                              // 10.0.0.0/8
                   || (b[0] == 100 && b[1] >= 64 && b[1] <= 127)  // 100.64.0.0/10 (CGNAT)
                   || (b[0] == 169 && b[1] == 254)            // 169.254.0.0/16 (link-local, metadados da nuvem)
                   || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)   // 172.16.0.0/12
                   || (b[0] == 192 && b[1] == 168)            // 192.168.0.0/16
                   || b[0] >= 224;                            // multicast e reservados
        }
    }
}
