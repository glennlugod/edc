using EDCApp.Components;
using EDCApp.Services;

var builder = WebApplication.CreateBuilder(args);

// OIDC Auth with Enhanced Configuration
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi(new[] { $"{builder.Configuration["Dataverse:Url"]}/.default" })
    .AddInMemoryTokenCaches();

builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI();

builder.Services.AddServerSideBlazor()
    .AddMicrosoftIdentityConsentHandler();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add authorization services with more granular policies
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("RequireAuthenticatedUser", policy => 
        policy.RequireAuthenticatedUser());
});

// Add Trial Service
builder.Services.AddScoped<TrialService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// Map authentication endpoints
app.MapRazorPages();
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
