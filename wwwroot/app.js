/**
 * TestAPI - Motor de Controle JavaScript (Vanilla)
 * Gerencia a captura do formulário, requisições assíncronas via POST e manipulação do DOM.
 */

const API_URL = "/api/monitor";

// Elementos globais do DOM encapsulados
const painelCards = document.getElementById("painel-cards");
const btnEscanear = document.getElementById("btn-escanear");

/**
 * Captura a entrada do usuário, valida os campos e envia a requisição POST para o C#.
 */
async function testarApiUnica() {
    // 1. CAPTURA DE DADOS: Obtém e sanitiza as entradas removendo espaços vazios extras
    const nomeInput = document.getElementById("input-nome").value.trim();
    const urlInput = document.getElementById("input-url").value.trim();

    // 2. VALIDAÇÃO CLIENT-SIDE (Fail-Fast): Evita chamadas inúteis à rede
    if (!nomeInput || !urlInput) {
        alert("Por favor, preencha o Nome e a URL da API antes de realizar o teste.");
        return;
    }

    // Criamos uma ID exclusiva para gerenciar o card temporário de carregamento deste teste
    const idCardTemporario = `card-loading-${Date.now()}`;

    try {
        // 3. UX FEEDBACK: Bloqueia interações duplicadas enquanto processa
        btnEscanear.disabled = true;
        btnEscanear.innerText = "VERIFICANDO INFRAESTRUTURA...";

        // 4. INJEÇÃO TEMPORÁRIA: Cria o card com o semáforo Amarelo (Processando) no topo da lista
        exibirCardCarregamento(nomeInput, urlInput, idCardTemporario);

        // Estrutura o payload exatamente como o modelo do C# (ApiFoco.cs) espera receber
        const payload = {
            nome: nomeInput,
            url: urlInput
        };

        // 5. REQUISIÇÃO HTTP POST: Envia o pacote JSON para o backend
        const resposta = await fetch(API_URL, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(payload)
        });

        // Remove o feedback visual de carregamento específico deste teste
        removerElementoPorId(idCardTemporario);

        // Se o backend rejeitar (HTTP 400 ou 500), extrai o texto explicativo do erro enviado pelo C#
        if (!resposta.ok) {
            const mensagemErroCsharp = await resposta.text();
            throw new Error(mensagemErroCsharp);
        }

        // 6. SUCESSO: Converte o JSON retornado pelo C# em objeto JS
        const apiResultado = await resposta.json();

        // Insere o card definitivo na tela (Verde ou Vermelho)
        renderizarCardDefinitivo(apiResultado);

        // Limpa apenas o campo de URL para o usuário poder digitar a próxima sem precisar apagar manualmente
        document.getElementById("input-url").value = "";

    } catch (erro) {
        // Remove o card temporário caso ocorra alguma falha de rede ou validação
        removerElementoPorId(idCardTemporario);
        
        console.error("Erro na varredura:", erro);
        alert(`Falha no monitoramento: ${erro.message}`);
    } finally {
        // 7. RESTAURAÇÃO DE ESTADO: Devolve o controle ao usuário
        btnEscanear.disabled = false;
        btnEscanear.innerText = "TESTAR ENDPOINT AGORA";
    }
}

/**
 * Cria e injeta um elemento visual de processamento temporário na tela.
 */
function exibirCardCarregamento(nome, url, id) {
    const templateLoading = `
        <div id="${id}" class="card status-processando">
            <h3>${nome}</h3>
            <p>${url}</p>
            <span class="badge-status">PROCESSANDO</span>
        </div>
    `;
    // 'afterbegin' insere o novo elemento sempre no topo da Grid
    painelCards.insertAdjacentHTML("afterbegin", templateLoading);
}

/**
 * Converte os dados processados pelo backend C# em elementos visuais definitivos.
 */
function renderizarCardDefinitivo(api) {
    // Mapeamento explícito do enum de Status do C#: 0 = Aguardando, 1 = Processando, 2 = Aberta (Verde), 3 = Fechada (Vermelho)
    let classeCssStatus = "status-fechada";
    let textoBadge = "FECHADA";

    if (api.status === 2) {
        classeCssStatus = "status-aberta";
        textoBadge = "ABERTA";
    }

    const templateCard = `
        <div class="card ${classeCssStatus}">
            <h3>${api.nome}</h3>
            <p>${api.url}</p>
            <div style="font-family: monospace; font-size: 0.8rem; margin-bottom: 1rem; color: #cbd5e1;">
                > ${api.mensagemRetorno}
            </div>
            <span class="badge-status">${textoBadge}</span>
        </div>
    `;

    painelCards.insertAdjacentHTML("afterbegin", templateCard);
}

/**
 * Auxiliar seguro para remover nós do DOM.
 */
function removerElementoPorId(id) {
    const elemento = document.getElementById(id);
    if (elemento) {
        elemento.remove();
    }
}