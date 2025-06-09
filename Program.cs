using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using WebAppEF.Models; // Assicurati che questo sia il namespace corretto per ApplicationDbContext e OrdiniFaker
using WebAppEF.Repositories; // Assicurati che questo sia il namespace corretto per IOrdiniRepository
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using GestioneClienti.Repositories; // Assicurati che questo sia il namespace corretto per ICustomerRepository
using GestioneClienti.Services; // Assicurati che questo sia il namespace corretto per RecaptchaService, IEmailSender, EmailSender, IGeocodingService, GoogleMapsGeocodingService
using Microsoft.AspNetCore.Identity;
using GestioneClienti.Hubs; // Assicurati che questo sia il namespace corretto per ChatHub
using ProgettoStage.Hubs; // Assicurati che questo sia il namespace corretto per DisponibilitaHub
using Microsoft.AspNetCore.Mvc.ViewFeatures; // Aggiungi questo using per ITempDataProvider e SessionStateTempDataProvider

var builder = WebApplication.CreateBuilder(args);

// comando da usare per esporre l'applicazione su porta 80 con Docker
// builder.WebHost.ConfigureKestrel(options =>
// {
//     options.ListenAnyIP(80); 
// });   


// Configurazione Serilog per logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("C:\\LogsProgetto\\app-log-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .MinimumLevel.Debug()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Host.UseSerilog();

// Configurazione Sessione (già presente, ma l'ordine è importante, e aggiungiamo il TempData provider)
builder.Services.AddDistributedMemoryCache(); // Necessario per la sessione basata sulla memoria
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Puoi regolare il timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ********************************************************************************
// SOLUZIONE DELL'ERRORE: Forza TempData a usare la sessione anziché i cookie per la serializzazione
// Questo permette di salvare oggetti complessi come byte[] in TempData.
builder.Services.AddSingleton<ITempDataProvider, SessionStateTempDataProvider>();
// ********************************************************************************


// Configurazione DbContext
// *** CAMBIATO DA AddDbContext A AddDbContextFactory PER RISOLVERE L'ERRORE SINGLETON/SCOPED ***
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configurazione lockout (tentativi errati di login)
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
});

// Configurazione CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWithCredentials", policyBuilder =>
    {
        policyBuilder.WithOrigins("http://localhost:5000")
                     .AllowAnyHeader()
                     .AllowAnyMethod()
                     .AllowCredentials();
    });
});

// Configurazione autenticazione Cookie
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.Name = "CookieProgramma";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Cambia a .Always in produzione con HTTPS
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
});

// Configurazione autorizzazione
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireUserRole", policy => policy.RequireRole("User"));
});

// Iniezione repository
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IOrdiniRepository, OrdiniRepository>();
builder.Services.AddScoped<RecaptchaService>();
builder.Services.AddHttpClient<IGeocodingService, GoogleMapsGeocodingService>();
// builder.Services.AddScoped<ProgettoStage.Services.StripePaymentService>();

// Configurazione EmailSettings
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddTransient<IEmailSender, EmailSender>(provider =>
{
    var emailSettings = provider.GetRequiredService<IOptions<EmailSettings>>();
    var logger = provider.GetRequiredService<ILogger<EmailSender>>();
    var configuration = provider.GetRequiredService<IConfiguration>(); 
    return new EmailSender(emailSettings, logger, configuration); 
});

// Registrazione del servizio GestoreDisponibilitaProdotto come Singleton
// Questo assicura che ci sia un'unica istanza del servizio, essenziale per la gestione della disponibilità globale.
builder.Services.AddSingleton<ProgettoStage.Services.GestoreDisponibilitaProdotto>();

// Configurazione Swagger
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
        }
    });
});

// Configurazione MVC e Razor Pages
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddSignalR();
builder.Services.AddRazorPages();

var app = builder.Build();


// Seeding del database (da eseguire solo una volta all'avvio dell'applicazione o in fase di sviluppo)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate(); // Applica le migrazioni pendenti

        // Esempio di seeding: rimuove i clienti esistenti e ne aggiunge di nuovi
        if (context.Clienti.Any())
        {
            context.Clienti.RemoveRange(context.Clienti);
            context.SaveChanges();
        }
        context.Clienti.AddRange(DataSeeder.GeneraClienti(50));

        // Seeding degli ordini (solo se non ci sono ordini esistenti)
        if (!context.Ordini.Any())
        {
            context.Ordini.AddRange(new OrdiniFaker().GenerateOrders(100));
        }
        
        // Seeding dei prodotti (aggiungi la tua logica di seeding per i prodotti se non l'hai già)
        // Esempio:
        // if (!context.Prodotti.Any())
        // {
        //     context.Prodotti.AddRange(DataSeeder.GeneraProdotti(20)); // Assumi che GeneraProdotti esista
        // }


        context.SaveChanges();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Errore durante il seeding del database");
    }
}

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Mostra i dettagli degli errori in sviluppo
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebAppEF API v1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection(); // Aggiungi questo per forzare HTTPS in produzione
app.UseCors("AllowWithCredentials");
app.UseRouting();
app.UseSession(); // Deve essere posizionato prima di UseAuthentication e UseAuthorization
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();

// Mapping degli Hub SignalR
app.MapHub<ChatHub>("/chatHub");
app.MapHub<DisponibilitaHub>("/disponibilitaHub");

// Middleware di logging delle richieste (opzionale, per debug)
app.Use(async (context, next) =>
{
    Log.Information($"Request: {context.Request.Method} {context.Request.Path}");
    await next();
    Log.Information($"Response: {context.Response.StatusCode}");
});

app.MapControllers();
app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();