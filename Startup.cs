using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http; // OBRIGATÓRIO: Necessário para usar o SendFileAsync
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TestAPI
{
    /// <summary>
    /// Gerencia a inicialização da aplicação Web, configurando os serviços injetados
    /// e a esteira de Middlewares do pipeline HTTP.
    /// </summary>
    public class Startup
    {
        // BOA PRÁTICA: Evita o uso de 'Magic Strings'. Centralizar o nome da política
        // impede erros de digitação entre o ConfigureServices e o Configure.
        private const string PoliticaCorsFront = "LiberarFrontEndPolicy";

        // 1. CONFIGURAÇÃO DE SERVIÇOS (Injeção de Dependência / IoC Container)
        // Equivalente às classes de @Configuration ou definição de Beans do Spring Boot em Java.
        public void ConfigureServices(IServiceCollection services)
        {
            // Adiciona o suporte para controllers focados em APIs REST (otimiza performance ignorando Views Razor)
            services.AddControllers();

            // ===== SESSÃO SECURITY: POLÍTICA DE CORS =====
            // Permite que o JavaScript contido no navegador interaja com este backend
            // contornando de forma segura as restrições da Same-Origin Policy (SOP).
            services.AddCors(options =>
            {
                options.AddPolicy(PoliticaCorsFront, builder =>
                {
                    builder.AllowAnyOrigin()   // Permite requisições de qualquer origem para fins de testes locais
                           .AllowAnyMethod()   // Habilita GET, POST, OPTIONS, etc.
                           .AllowAnyHeader();  // Aceita Content-Type, Authorization, etc.
                });
            });
        }

        // 2. PIPELINE DE REQUISIÇÃO HTTP (A Esteira de Middlewares)
        // Define a ordem exata em que as requisições HTTP são tratadas pelo servidor Kestrel.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Se o projeto rodar em modo de desenvolvimento, injeta uma página rica de erros (Stack Trace amigável)
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // ===== ENGENHARIA DE ARQUIVOS ESTÁTICOS NA NUVEM =====
            // REGRA DE OURO: UseDefaultFiles DEVE vir obrigatoriamente antes de UseStaticFiles.
            // Avisa ao Kestrel para buscar por arquivos padrão (como 'index.html') quando a URL raiz (/) for chamada.
            app.UseDefaultFiles();

            // Entrega arquivos físicos estáticos da pasta 'wwwroot' (HTML, CSS, JS)
            // Se a requisição pedir pelo index.html, ela para aqui e não sobrecarrega o roteamento lógico.
            app.UseStaticFiles();

            // Middleware 2: Analisa a URL da requisição e escolhe qual rota/controller deve atendê-la
            app.UseRouting();

            // ===== SESSÃO SECURITY: APLICAÇÃO DA POLÍTICA DE CORS =====
            // REGRA DE OURO: Deve vir OBRIGATORIAMENTE após o UseRouting e antes do UseEndpoints.
            app.UseCors(PoliticaCorsFront);

            // Middleware 3: Executa o destino final, enviando a requisição para dentro do MonitorController
            app.UseEndpoints(endpoints =>
            {
                // Mapeia os endpoints dos nossos Controllers (como o /api/monitor)
                endpoints.MapControllers();

                // ===== SOLUÇÃO DEFINITIVA PARA O LINK RAIZ EM PRODUÇÃO (XEQUE-MATE) =====
                // Força o Kestrel a buscar o index.html diretamente do caminho físico do WebRoot (wwwroot)
                // e injetar o conteúdo na resposta HTTP, eliminando o erro 404 de roteamento no Linux.
                endpoints.MapGet("/", async context =>
                {
                    context.Response.ContentType = "text/html";
                    await context.Response.SendFileAsync(System.IO.Path.Combine(env.WebRootPath, "index.html"));
                });

                // Caso o usuário tente acessar qualquer outra rota de subnível inválida,
                // faz o fallback injetando o index.html direto do disco por segurança.
                endpoints.MapFallback(async context =>
                {
                    context.Response.ContentType = "text/html";
                    await context.Response.SendFileAsync(System.IO.Path.Combine(env.WebRootPath, "index.html"));
                });
            });
        }
    }
}