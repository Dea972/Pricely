using Microsoft.AspNetCore.Components;
using Pricely.Web.Api;
using Pricely.Web.Components.Shared;

namespace Pricely.Web.Components.Pages;

/// <summary>
/// The collection profile of each typology (docs/design/mockups/profiles.html): the tree with the typologies that define
/// their own profile, then for one typology its merchants (own or inherited) and instructions, and the effective prompt
/// block by block. The inheritance is resolved by the API (<c>CollectionProfileResolver</c>), never here.
/// </summary>
public partial class ProfilesPage
{
    private IReadOnlyList<(ProductCategoryDto Category, int Depth)>? _tree;
    private Dictionary<Guid, CollectionProfileSummaryDto> _profiles = [];
    private IReadOnlyList<ProductCategoryDto> _categories = [];
    private IReadOnlyList<MerchantDto> _merchants = [];
    private ProductCategoryDto? _category;
    private string? _categoryPath;
    private EffectiveCollectionProfileDto? _effective;
    private Guid? _loadedCategoryId;

    /// <summary>Null while the typology inherits its merchants; the edited list otherwise.</summary>
    private List<Guid>? _ownMerchants;
    private string? _ownInstructions;
    private string _merchantToAdd = string.Empty;
    private bool _saving;
    private bool _saved;

    [Parameter]
    public Guid? CategoryId { get; set; }

    [Inject]
    private PricelyApiClient Api { get; set; } = default!;

    private IEnumerable<MerchantDto> OtherMerchants =>
        _merchants.Where(m => m.IsActive && _ownMerchants?.Contains(m.Id) != true);

    protected override async Task OnInitializedAsync()
    {
        _categories = await CallAsync(() => Api.GetCategoriesAsync(CancellationToken.None)) ?? [];
        _merchants = await CallAsync(() => Api.GetMerchantsAsync(CancellationToken.None)) ?? [];
        await LoadProfilesAsync();
        _tree = CategoryTree.DepthFirst(_categories);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (CategoryId == _loadedCategoryId)
        {
            return;
        }

        _loadedCategoryId = CategoryId;
        _saved = false;
        _effective = null;
        _category = CategoryId is Guid id ? _categories.FirstOrDefault(c => c.Id == id) : null;
        _categoryPath = _category is null ? null : CategoryPath.Build(_categories, _category.Id);

        if (_category is not null)
        {
            await LoadEffectiveAsync(_category.Id);
        }
    }

    private async Task LoadProfilesAsync()
    {
        IReadOnlyList<CollectionProfileSummaryDto>? profiles = await CallAsync(() => Api.GetCollectionProfilesAsync(CancellationToken.None));
        _profiles = (profiles ?? []).ToDictionary(p => p.ProductCategoryId);
    }

    private async Task LoadEffectiveAsync(Guid categoryId)
    {
        _effective = await CallAsync(() => Api.GetEffectiveCollectionProfileAsync(categoryId, CancellationToken.None));
        _ownMerchants = _effective is { OwnPreferredMerchantIds.Count: > 0 } ? [.. _effective.OwnPreferredMerchantIds] : null;
        _ownInstructions = _effective?.OwnInstructions;
    }

    private void DefineOwnMerchants() => _ownMerchants = [.. _effective?.PreferredMerchantIds ?? []];

    private void InheritMerchants() => _ownMerchants = null;

    private void AddMerchant()
    {
        if (_ownMerchants is not null && Guid.TryParse(_merchantToAdd, out Guid merchantId) && !_ownMerchants.Contains(merchantId))
        {
            _ownMerchants.Add(merchantId);
        }

        _merchantToAdd = string.Empty;
    }

    private void Move(int index, int offset)
    {
        if (_ownMerchants is null || index + offset < 0 || index + offset >= _ownMerchants.Count)
        {
            return;
        }

        (_ownMerchants[index], _ownMerchants[index + offset]) = (_ownMerchants[index + offset], _ownMerchants[index]);
    }

    private async Task SaveAsync()
    {
        if (_category is null || _saving)
        {
            return;
        }

        _saving = true;
        _saved = false;
        Problem = null;

        try
        {
            Guid categoryId = _category.Id;
            var profile = new CollectionProfileRequest(_ownMerchants ?? [], string.IsNullOrWhiteSpace(_ownInstructions) ? null : _ownInstructions.Trim());

            if (await CallAsync(async () => { await Api.DefineCollectionProfileAsync(categoryId, profile, CancellationToken.None); return profile; }) is not null)
            {
                _saved = true;
                await LoadProfilesAsync();
                await LoadEffectiveAsync(categoryId);
            }
        }
        finally
        {
            _saving = false;
        }
    }

    private string NameOf(Guid categoryId) => _categories.FirstOrDefault(c => c.Id == categoryId)?.Name ?? "Typologie inconnue";

    private string MerchantName(Guid merchantId) => _merchants.FirstOrDefault(m => m.Id == merchantId)?.Name ?? "Enseigne inconnue";

    private string MerchantState(Guid merchantId) =>
        _merchants.FirstOrDefault(m => m.Id == merchantId) switch
        {
            { IsActive: true } => "active",
            { IsActive: false } => "inactive (ignorée)",
            _ => string.Empty
        };
}
