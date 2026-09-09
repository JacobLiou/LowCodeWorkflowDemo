using Microsoft.EntityFrameworkCore;
using WorkflowRulesDemo.Web.Components;
using WorkflowRulesDemo.Web.Data;
using WorkflowRulesDemo.Web.Engine;
using WorkflowRulesDemo.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddBootstrapBlazor();

var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=workflowrules.db";
builder.Services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddSingleton<RuleEngine>();
builder.Services.AddSingleton<WorkflowEngine>();
builder.Services.AddScoped<RuleService>();
builder.Services.AddScoped<WorkflowService>();
builder.Services.AddScoped<SeedDataService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
    db.Database.EnsureCreated();

    var seed = scope.ServiceProvider.GetRequiredService<SeedDataService>();
    await seed.EnsureSeedDataAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
