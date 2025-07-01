using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using WebAppEF.Models;
using WebAppEF.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using GestioneClienti.Repositories;
using GestioneClienti.Services;
using Microsoft.AspNetCore.Identity;
using GestioneClienti.Hubs;
using ProgettoStage.Hubs;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using ProgettoStage.Repositories;
using ProgettoStage.Services;
using ProgettoStage.Utilities; // Assicurati che questo namespace esista per DataSeeder e OrdiniFaker
using QuestPDF.Infrastructure;
using QuestPDF.Fluent;
using Microsoft.Extensions.Logging; // Aggiunto per ILogger

// Imposta la licenza di QuestPDF
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
QuestPDF.Settings.EnableDebugging = true; // Utile per il debug dei PDF

var builder = WebApplication.CreateBuilder(args);

// Comando per esporre l'applicazione su porta 80 con Docker (decommenta se necessario)
// builder.WebHost.ConfigureKestrel(options =>
// {
//     options.ListenAnyIP(80);
// });

// Configurazione Serilog per logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("C:\\LogsProgetto\\app-log-.txt", rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .Enrich.FromLogContext()
    .MinimumLevel.Debug() // Imposta il livello di logging minimo
    .CreateLogger();

builder.Logging.ClearProviders(); // Rimuove i provider di logging predefiniti
builder.Host.UseSerilog(); // Utilizza Serilog come provider di logging

// Configurazione Sessione
builder.Services.AddDistributedMemoryCache(); // Necessario per la sessione basata sulla memoria
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // Il cookie di sessione è essenziale per il funzionamento dell'app
});

// Questo permette di salvare oggetti complessi come byte[] in TempData.
builder.Services.AddSingleton<ITempDataProvider, SessionStateTempDataProvider>();

// Configurazione DbContext
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configurazione delle opzioni AziendaInfo
builder.Services.Configure<AziendaInfoOptions>(
    builder.Configuration.GetSection(AziendaInfoOptions.AziendaInfo));

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
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; 
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

// Iniezione repository e servizi
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IOrdiniRepository, OrdiniRepository>();
builder.Services.AddScoped<RecaptchaService>();
builder.Services.AddHttpClient<IGeocodingService, GoogleMapsGeocodingService>();
// builder.Services.AddScoped<ProgettoStage.Services.StripePaymentService>(); // Decommenta se usi Stripe

// Configurazione EmailSettings
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddTransient<IEmailSender, EmailSender>(provider =>
{
    var emailSettings = provider.GetRequiredService<IOptions<EmailSettings>>();
    var logger = provider.GetRequiredService<ILogger<EmailSender>>(); // Inietta ILogger
    var configuration = provider.GetRequiredService<IConfiguration>();
    return new EmailSender(emailSettings, logger, configuration);
});

builder.Services.AddSingleton<ProgettoStage.Services.GestoreDisponibilitaProdotto>();

// Registrazione di GeneratorePdfService 
builder.Services.AddTransient<GeneratorePdfService>(provider => {
    var webHostEnvironment = provider.GetRequiredService<IWebHostEnvironment>();
    var logger = provider.GetRequiredService<ILogger<GeneratorePdfService>>(); 
    var configuration = provider.GetRequiredService<IConfiguration>(); 

    var logoFileName = "logo.jpeg";
    var logoPath = Path.Combine(webHostEnvironment.WebRootPath, "images", logoFileName);

    // Recupera i dati di AziendaInfo dalla configurazione
    var ragioneSociale = configuration["AziendaInfo:RagioneSociale"] ?? "La Tua Azienda S.r.l.";
    var nomeAzienda = configuration["AziendaInfo:NomeAzienda"] ?? "Nome Azienda";

    return new GeneratorePdfService(logoPath, ragioneSociale, nomeAzienda, logger); 
});


// Configurazione Swagger/OpenAPI
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
    // Aggiunto per protezione CSRF
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddSignalR();
builder.Services.AddRazorPages();

var app = builder.Build();

// // Seeding del database (da eseguire solo una volta all'avvio dell'applicazione o in fase di sviluppo)
// using (var scope = app.Services.CreateScope())
// {
//     var services = scope.ServiceProvider;
//     try
//     {
//         var context = services.GetRequiredService<ApplicationDbContext>();
//         context.Database.Migrate(); // Applica le migrazioni pendenti

//         // Esempio di seeding: rimuove i clienti esistenti e ne aggiunge di nuovi
//         // ATTENZIONE: Questo cancellerà i dati esistenti ad ogni avvio.
//         // Usalo solo in ambiente di sviluppo.
// //         if (context.Clienti.Any())
// //         {
// //              context.Clienti.RemoveRange(context.Clienti);
// //              context.SaveChanges();
// //         }
// //         context.Clienti.AddRange(DataSeeder.GeneraClienti(50));

// //         // Seeding dei prodotti (se non ci sono prodotti esistenti)
// //         if (!context.Prodotti.Any())
// //         {
// //             context.Prodotti.AddRange(DataSeeder.GeneraProdotti(20)); // Assumi che GeneraProdotti esista e sia definito in DataSeeder
// //         }

// //         // Seeding degli ordini (solo se non ci sono ordini esistenti)
// //         if (!context.Ordini.Any())
// //         {
// //             var clientiEsistenti = context.Clienti.ToList();
// //             var prodottiEsistenti = context.Prodotti.ToList();
// //             context.Ordini.AddRange(new OrdiniFaker(clientiEsistenti, prodottiEsistenti).GenerateOrders(100));
// //         }

// //         context.SaveChanges();
// //     }
// //     catch (Exception ex)
// //     {
// //         var logger = services.GetRequiredService<ILogger<Program>>();
// //         logger.LogError(ex, "Errore durante il seeding del database");
// //     }
// // }
//     }

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

app.UseHttpsRedirection(); // Reindirizza le richieste HTTP a HTTPS
app.UseCors("AllowWithCredentials"); // Abilita CORS con la policy definita
app.UseRouting(); // Identifica il percorso della richiesta
app.UseSession(); // Deve essere posizionato prima di UseAuthentication e UseAuthorization
app.UseAuthentication(); // Autentica l'utente
app.UseAuthorization(); // Autorizza l'utente in base alle policy definite
app.UseStaticFiles(); // Abilita il servizio di file statici (es. wwwroot)

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

// Mappa i controller MVC e le Razor Pages
app.MapControllers();
app.MapRazorPages();

// Configurazione del routing MVC predefinito
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run(); 