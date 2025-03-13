using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PuppeteerSharp;
using WebAppEF.Repositories;
using WebAppEF.Entities;
using WebAppEF.Models;
using WebAppEF.ViewModel;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

#pragma warning disable CS0618 // Type or member is obsolete
await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
#pragma warning restore CS0618 // Type or member is obsolete

builder.Services.AddSingleton(serviceProvider =>
{
    return Puppeteer.LaunchAsync(new LaunchOptions
    {
        Headless = true,
        Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
    }).Result;
});

// configurazione serilog
Log.Logger = new LoggerConfiguration()
.WriteTo.File(@"C:\LogsProgetto\app-log-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .MinimumLevel.Warning()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Host.UseSerilog();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// DI PER LE REPOSITORY
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IOrdiniRepository, OrdiniRepository>();

builder.Services.AddSingleton<BrowserFetcher>();
builder.Services.AddSingleton<IWebScraperService, PuppeteerWebScraperService>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();

// PERSONALIZZAZIONE SWAGGER
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WebAppEF API",
        Version = "v1",
        Description = "API per la gestione dei clienti e ordini.",
        Contact = new OpenApiContact
        {
            Name = "Carlo",
            Email = "supporto@webapief.com",
            Url = new Uri("http://localhost:5000/")
        },
        // License = new OpenApiLicense
        // {
        //     Name = "Licenza MIT",
        //     Url = new Uri("https://opensource.org/licenses/MIT")
        // }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();

        // Seeding clienti
        if (!context.Clienti.Any())
        {
            var clientiFittizi = DataSeeder.GeneraClienti(50);
            context.Clienti.AddRange(clientiFittizi);
            context.SaveChanges();
        }

        // Seeding ordini
        if (!context.Ordini.Any())
        {
            var ordiniFaker = new OrdiniFaker();
            var ordiniFittizi = ordiniFaker.GenerateOrders(100);
            context.Ordini.AddRange(ordiniFittizi);
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        Log.Error($"Errore durante il seeding del database: {ex.Message}");
    }
}

// Configura la pipeline di gestione delle richieste
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Swagger solo in ambiente di sviluppo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebAppEF API v1");
        c.RoutePrefix = "swagger"; // Swagger disponibile su /swagger
    });
}

app.UseRouting();
app.UseAuthorization();

// Mappa i controller e le Razor Pages
app.MapControllers();
app.MapRazorPages();

// Configura la route predefinita per i controller
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();