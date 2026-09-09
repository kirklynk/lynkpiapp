using lynkpiapp;
using lynkpiapp.Components;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomCenter;
    config.SnackbarConfiguration.PreventDuplicates = true;
    config.SnackbarConfiguration.VisibleStateDuration = 1000;
});

builder.Services.AddSingleton<HardwareService>();
builder.Services.Configure<List<User>>(builder.Configuration.GetSection("Users"));
builder.Services.Configure<List<Sensor>>(builder.Configuration.GetSection("Sensors"));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

InitHardwareService(app.Services.CreateScope());

app.Run();

void InitHardwareService(IServiceScope scope)
{
    _ = scope.ServiceProvider.GetRequiredService<HardwareService>();

}