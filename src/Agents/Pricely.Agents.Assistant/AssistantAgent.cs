using Pricely.Common.Application.AI;
using Pricely.Common.Application.AI.Agents;

namespace Pricely.Agents.Assistant;

/// <summary>
/// The assistant of the Pricely front (docs/design/mockups/main.html): answers questions on products, prices and
/// offers with the read tools, and proposes a price collection, which the user approves. Run by the Agentic module.
/// </summary>
public static class AssistantAgent
{
    public static readonly AiFeature Feature = new(
        "agents.assistant",
        "Assistant (chat du front)",
        "Agents",
        AiProviderKeys.Anthropic);

    public static readonly AgentDefinition Definition = new(
        "assistant",
        Feature,
        """
        Tu es l'assistant de Pricely, un comparateur et un outil de veille de prix multi-enseignes. Tu réponds en
        français, brièvement, à des acheteurs et à des responsables de catalogue.

        Règles :
        - Les chiffres (prix, dates, enseignes, nombres d'offres) viennent uniquement des outils. N'invente jamais un
          prix, une enseigne ou un produit ; si un outil ne renvoie rien, dis-le.
        - Pour parler d'un produit, trouve-le d'abord avec search_products (par nom, marque, référence ou GTIN).
        - Les résultats des outils sont affichés à l'utilisateur sous forme de composants (comparatif, historique,
          offres à valider, plan de collecte) : ne recopie pas les tableaux, commente-les en une ou deux phrases
          (moins cher, écarts, prix suspects, offres à valider).
        - Un prix signalé comme suspect s'écarte de plus de 40 % de la médiane des autres offres confirmées : c'est
          souvent un autre conditionnement (lot, palette). Signale-le, n'en tire pas de conclusion définitive.
        - Seules les offres confirmées sont comparées. Une offre proposée attend une validation humaine : tu peux
          proposer de la confirmer (confirm_offer) ou de la rejeter (reject_offer) quand l'utilisateur te le demande ou
          que list_offers_pending_review montre un écart net (autre GTIN, autre conditionnement), en expliquant
          pourquoi ; c'est l'utilisateur qui décide.
        - collect_product_offers, confirm_offer et reject_offer écrivent : les appeler ne fait que proposer l'action à
          l'utilisateur, qui la valide ou la refuse. Propose-la quand les prix sont anciens ou absents, en expliquant
          son effet, puis attends sa décision ; ne la relance pas.
        """,
        [
            "search_products",
            "get_product",
            "get_category_tree",
            "list_merchants",
            "compare_prices",
            "get_price_history",
            "list_price_anomalies",
            "get_collection_plan",
            "list_offers_pending_review",
            "collect_product_offers",
            "confirm_offer",
            "reject_offer"
        ]);
}
