# Design du front Pricely

Maquettes de référence du front Blazor. Ce sont des pages HTML statiques, sans dépendance, avec les styles en ligne : on les ouvre dans un navigateur, on ne les copie pas telles quelles dans les composants. Elles fixent l'intention (contenu, hiérarchie, états), pas le markup.

Les données affichées sont fictives (enseignes, marques, GTIN). Les montants en `[COÛT]`, `[PRIX]`, `[N]` et `[DATE]` sont des emplacements à brancher sur l'API, pas des valeurs.

## Écrans

| Maquette | Écran | Données (requêtes, endpoints) |
|---|---|---|
| [`mockups/main.html`](mockups/main.html) | Assistant : chat, réponses typées, action proposée | agent interne (étape 4), tools MCP `search_products`, `compare_prices`, `list_price_anomalies` |
| [`mockups/product.html`](mockups/product.html) | Fiche produit : offres, historique, prix signalés | `GetProduct`, `compare_prices`, `GetPriceHistory`, `GetPriceAnomalies`, `CollectProductOffersCommand` |
| [`mockups/review.html`](mockups/review.html) | Offres à valider : référence vs page observée | offres `Proposed`, `Confirm` / `Reject` |
| [`mockups/profiles.html`](mockups/profiles.html) | Profils de collecte par typologie | `GetProductCategories`, `GetEffectiveCollectionProfileQuery` |
| [`mockups/ai-usage.html`](mockups/ai-usage.html) | IA et coûts : usage, cibles, catalogue | `aiengine/usage/*`, `aiengine/features`, `aiengine/models` |
| [`mockups/mobile.html`](mockups/mobile.html) | Assistant en 390 px | idem Assistant |

## Principes

- **L'assistant répond avec des composants typés**, pas seulement du texte : comparatif de prix, liste d'offres, plan de collecte. Chaque résultat de tool a son composant Blazor ; le texte du modèle n'est qu'un commentaire autour.
- **Action proposée avant toute écriture.** Un tool non `ReadOnly` (ADR-009) s'affiche comme une carte « Action proposée » qui décrit l'effet (produit, enseignes, conséquences). Rien ne s'exécute avant « Valider » ; le bouton envoie directement la commande à l'API, sans repasser par le modèle. Après décision, la carte garde une trace (« Validée par toi », « Refusée — rien n'a été modifié »).
- **Lot d'actions** (ADR-028). Les écritures proposées dans une même réponse s'affichent en une carte « Lot proposé » : une ligne par action, dans l'ordre d'exécution, avec ce qu'elle fera (produit, marque, GTIN, typologie ; enseigne et site ; prix et page). « Valider le lot » décide tout en une fois, « Refuser le lot » ne change rien, « Retirer du lot » refuse une ligne avant de décider le reste. Les boutons restent inactifs tant que l'assistant écrit (le lot n'est pas fini). Après décision, chaque ligne garde son issue (ajouté, déjà présent, offre confirmée ou à valider, échec et sa raison). Une action proposée seule garde sa carte habituelle.
- **Les outils utilisés sont visibles** (pastilles sous la question) : l'utilisateur sait d'où viennent les chiffres.
- **Jamais de logique métier dans les composants.** Prix suspect, mode de rapprochement, profil effectif : tout vient des réponses de l'API (`PriceAnomalyPolicy`, `OfferMatchingPolicy`, `CollectionProfileResolver`).
- **Hériter se voit.** Un profil hérité porte le badge « Héritées de <typologie> » ; l'aperçu du prompt effectif montre chaque bloc d'instructions avec sa typologie d'origine, de la racine à la feuille.

## Tokens

| Rôle | Valeur |
|---|---|
| Fond | `#F4F5F2` |
| Surface | `#FFFFFF`, surface secondaire `#F9FAF7` |
| Bordures | `#DDE1DA` (cartes), `#E8ECE6` (séparateurs), `#C7CDC4` (champs, boutons secondaires) |
| Texte | `#17201B`, secondaire `#56605A` |
| Barre latérale | `#17201B`, élément actif `#2A3630` |
| Accent (action, confirmé, moins cher) | `#0B5D6B`, fond `#EEF5F6` / `#DCEBEE`, texte sur fond clair `#07404A` |
| Attention (en attente, suspect) | `#A84B0C`, fond `#FDF1E7`, bordure `#E9B98F`, texte `#7A3608`, badge `#F2B27A` |
| Séries de graphique | `#0B5D6B`, `#8A6FB8`, `#B4762F` (plus un style de trait par série) |

- Typographie : IBM Plex Sans (400, 500, 600) ; **IBM Plex Mono pour les prix, GTIN, références et noms de tools**. Prix alignés à droite.
- Rayons : 8 px (boutons, champs), 12 px (cartes), pastilles en 999 px.
- Une couleur n'est **jamais** le seul signal : toujours un libellé ou une icône (« Prix suspect », « ✓ identique », « ● À valider »).
- Cibles tactiles de 44 px minimum. Focus visible, `aria-current` sur la navigation, libellés sur les champs.

## Responsive

- Desktop : barre latérale de navigation ; panneau de contexte à droite du chat (produit en cours, outils, consommation).
- Mobile : en-tête avec menu et raccourci « Offres à valider » (badge), comparatif compacté en liste, saisie en bas d'écran. Les tableaux défilent horizontalement plutôt que de déborder.
