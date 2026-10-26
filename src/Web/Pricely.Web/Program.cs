using Pricely.Web.Api;
using Pricely.Web.Authentication;
using Pricely.Web.Components;
using Pricely.Web.Components.Shared;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddPricelyAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddPricelyApi(builder.Configuration);
builder.Services.AddScoped<ProductLookup>();

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapAuthenticationEndpoints();

// Every page needs a signed-in user: the first request of an anonymous visitor goes to Keycloak.
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .RequireAuthorization();

await app.RunAsync();
