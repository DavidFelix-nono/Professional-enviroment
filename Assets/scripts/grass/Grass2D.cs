using System.Collections.Generic;
using UnityEngine;

// Impede que o mesmo GameObject tenha este script adicionado mais de uma vez.
[DisallowMultipleComponent]
public class Grama2D : MonoBehaviour
{
    // =========================
    // CONFIGURAÇÃO NO INSPECTOR
    // =========================

    [Header("Base / Pivô")]
    [Tooltip("Transform que será rotacionado. Se ficar vazio, usa o próprio transform deste objeto.")]
    public Transform pivoBase;

    [Header("Vento / Movimento Idle")]
    [Tooltip("Ângulo máximo do balanço causado pelo vento.")]
    public float anguloVento = 8f;

    [Tooltip("Velocidade do balanço do vento.")]
    public float velocidadeVento = 2f;

    [Tooltip("Deslocamento aleatório para cada grama não balançar igual às outras.")]
    public float faseAleatoria;

    [Header("Interação")]
    [Tooltip("Distância horizontal máxima em que o corpo ainda influencia a grama.")]
    public float raioInfluencia = 0.8f;

    [Tooltip("Ângulo máximo que a grama pode inclinar ao ser empurrada.")]
    public float anguloEmpurrao = 28f;

    [Tooltip("Velocidade com que a grama se inclina ao ser tocada.")]
    public float velocidadeDobrar = 14f;

    [Tooltip("Velocidade com que a grama volta ao normal.")]
    public float velocidadeRetorno = 9f;

    [Tooltip("Somente objetos nestas layers poderão afetar a grama.")]
    public LayerMask camadasInteracao = ~0;

    [Header("Detecção")]
    [Tooltip("Se ativado, a grama usa o BoxCollider2D trigger já existente no objeto.")]
    public bool usarDeteccaoPorTrigger = true;

    // =========================
    // VARIÁVEIS INTERNAS
    // =========================

    // Lista de rigidbodies que estão atualmente dentro da área da grama.
    private readonly List<Rigidbody2D> corposDentro = new List<Rigidbody2D>();

    // Ângulo extra atual causado pela interação com corpos.
    private float anguloExtraAtual;

    // Variável auxiliar usada pelo SmoothDamp para suavizar a rotação.
    private float velocidadeAngularAtual;

    // Referência guardada do BoxCollider2D do objeto.
    private BoxCollider2D colisorCaixa;

    // =========================
    // MÉTODOS DE CICLO DE VIDA
    // =========================

    private void Reset()
    {
        // Quando o componente é adicionado ou resetado no Inspector,
        // sorteia uma fase aleatória para o vento.
        faseAleatoria = Random.Range(0f, 10f);
    }

    private void Awake()
    {
        // Se nenhum pivô/base foi informado no Inspector,
        // usa o próprio transform do objeto.
        if (pivoBase == null)
            pivoBase = transform;

        // Se a fase ainda estiver praticamente zero,
        // sorteia um valor para variar o vento entre instâncias.
        if (Mathf.Approximately(faseAleatoria, 0f)) 
            faseAleatoria = Random.Range(0f, 10f);

        // Prepara a referência do collider sem alterar tamanho nem offset.
        PrepararTriggerSeNecessario();
    }

    private void Update()
    {
        // Remove referências inválidas da lista, caso algum objeto tenha sido destruído.
        LimparCorposInvalidos();

        // Calcula qual deveria ser o ângulo extra ideal neste frame
        // com base nos corpos que estão tocando a grama.
        float anguloExtraAlvo = CalcularAnguloInteracao();

        // Decide se vamos usar a velocidade de dobrar ou de voltar.
        // Se o ângulo alvo for "mais forte" que o atual, dobra.
        // Senão, retorna.
        float velocidadeAtual =
            Mathf.Abs(anguloExtraAlvo) > Mathf.Abs(anguloExtraAtual)
            ? velocidadeDobrar
            : velocidadeRetorno;

        // Suaviza a transição do ângulo atual até o ângulo alvo.
        // Isso evita movimentos bruscos.
        anguloExtraAtual = Mathf.SmoothDamp(
            anguloExtraAtual,
            anguloExtraAlvo,
            ref velocidadeAngularAtual,
            1f / Mathf.Max(0.01f, velocidadeAtual)
        );

        // Calcula o vento usando uma senoide baseada no tempo.
        float anguloDoVento =
            Mathf.Sin((Time.time + faseAleatoria) * velocidadeVento) * anguloVento;

        // Soma o balanço do vento com o empurrão causado pelos corpos.
        float anguloFinal = anguloDoVento + anguloExtraAtual;

        // Aplica a rotação final no pivô da base.
        // Em 2D, a rotação visível geralmente acontece no eixo Z.
        pivoBase.localRotation = Quaternion.Euler(0f, 0f, anguloFinal);
    }

    // =========================
    // PREPARAÇÃO DO TRIGGER
    // =========================

    private void PrepararTriggerSeNecessario()
    {
        // Se a detecção por trigger estiver desligada,
        // não precisamos fazer nada.
        if (!usarDeteccaoPorTrigger)
            return;

        // Tenta pegar o BoxCollider2D já existente no objeto.
        colisorCaixa = GetComponent<BoxCollider2D>();

        // Se não existir, avisa no console.
        // Não criamos nem alteramos automaticamente,
        // porque agora a ideia é respeitar 100% o collider que você configurou no Inspector.
        if (colisorCaixa == null)
        {
            Debug.LogWarning(
                $"[{name}] O script Grama2D está com detecção por trigger ativada, " +
                $"mas não encontrou BoxCollider2D neste objeto.",
                this
            );
            return;
        }

        // Garante apenas que ele seja trigger.
        // Não mexe em size, offset nem qualquer outra configuração.
        colisorCaixa.isTrigger = true;
    }

    // =========================
    // CÁLCULO DA INTERAÇÃO
    // =========================

    private float CalcularAnguloInteracao()
    {
        // Se não há corpos dentro da grama,
        // não existe inclinação extra.
        if (corposDentro.Count == 0)
            return 0f;

        // Guarda o ângulo mais forte encontrado.
        float anguloMaisForte = 0f;

        // Posição da base da grama no mundo.
        Vector3 origem = pivoBase.position;

        // Percorre todos os corpos dentro da área de detecção.
        for (int i = 0; i < corposDentro.Count; i++)
        {
            // Pega o rigidbody atual da lista.
            Rigidbody2D corpo = corposDentro[i];

            // Se a referência estiver nula, pula para o próximo.
            if (corpo == null)
                continue;

            // Pega o centro de massa do corpo no espaço do mundo.
            Vector2 posicaoCorpo = corpo.worldCenterOfMass;

            // Diferença horizontal entre o corpo e a base da grama.
            float diferencaX = posicaoCorpo.x - origem.x;

            // Valor absoluto dessa diferença.
            float distanciaHorizontal = Mathf.Abs(diferencaX);

            // Se estiver além do raio de influência, ignora este corpo.
            if (distanciaHorizontal > raioInfluencia)
                continue;

            // Normaliza a influência:
            // 1 = muito perto
            // 0 = no limite do raio
            float proximidade = 1f - (distanciaHorizontal / raioInfluencia);

            // Define o lado para onde a grama deve dobrar.
            // Se o corpo está à direita, a grama dobra para a esquerda.
            // Se o corpo está à esquerda, a grama dobra para a direita.
            float direcao = diferencaX >= 0f ? -1f : 1f;

            // Pega a velocidade horizontal do corpo e reduz sua influência.
            float influenciaVelocidade =
                Mathf.Clamp(corpo.linearVelocity.x * 0.15f, -0.35f, 0.35f);

            // Calcula o ângulo base do empurrão.
            float anguloCalculado = direcao * anguloEmpurrao * proximidade;

            // Soma um extra com base na velocidade lateral do corpo.
            anguloCalculado += anguloEmpurrao * influenciaVelocidade * proximidade;

            // Se este ângulo for mais forte que o atual, ele se torna o dominante.
            if (Mathf.Abs(anguloCalculado) > Mathf.Abs(anguloMaisForte))
                anguloMaisForte = anguloCalculado;
        }

        // Retorna o ângulo de interação mais forte encontrado.
        return anguloMaisForte;
    }

    // =========================
    // LIMPEZA DA LISTA
    // =========================

    private void LimparCorposInvalidos()
    {
        // Percorre a lista de trás para frente para remover com segurança.
        for (int i = corposDentro.Count - 1; i >= 0; i--)
        {
            // Se o rigidbody foi destruído ou ficou inválido, remove da lista.
            if (corposDentro[i] == null)
                corposDentro.RemoveAt(i);
        }
    }

    // =========================
    // FILTRO DE LAYER
    // =========================

    private bool CamadaPermitida(GameObject objeto)
    {
        // Verifica se a layer do objeto está dentro da máscara configurada.
        return (camadasInteracao.value & (1 << objeto.layer)) != 0;
    }

    // =========================
    // EVENTOS DE TRIGGER
    // =========================

    private void OnTriggerEnter2D(Collider2D outro)
    {
        // Se a detecção por trigger estiver desligada, sai.
        if (!usarDeteccaoPorTrigger)
            return;

        // Se a layer do objeto não for permitida, ignora.
        if (!CamadaPermitida(outro.gameObject))
            return;

        // Pega o Rigidbody2D associado ao collider que entrou.
        Rigidbody2D corpo = outro.attachedRigidbody;

        // Se não houver rigidbody, ignora.
        if (corpo == null)
            return;

        // Evita adicionar duplicado na lista.
        if (!corposDentro.Contains(corpo))
            corposDentro.Add(corpo);
    }

    private void OnTriggerExit2D(Collider2D outro)
    {
        // Se a detecção por trigger estiver desligada, sai.
        if (!usarDeteccaoPorTrigger)
            return;

        // Pega o Rigidbody2D associado ao collider que saiu.
        Rigidbody2D corpo = outro.attachedRigidbody;

        // Se não houver rigidbody, ignora.
        if (corpo == null)
            return;

        // Remove o corpo da lista.
        corposDentro.Remove(corpo);
    }
}