using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using WebAppEF.Models;
using WebAppEF.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using GestioneClienti.Repositories;
using GestioneClienti.Services;

var builder = WebApplication.CreateBuilder(args);

// Configurazione Serilog per logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("C:\\LogsProgetto\\app-log-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .MinimumLevel.Debug()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Host.UseSerilog();

// Configurazione DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configurazione CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWithCredentials", builder =>
    {
        builder.WithOrigins("http://localhost:5000")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
    });
});

// Configurazione autenticazione Cookie (cookie authentication)
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




// Iniezione repository
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IOrdiniRepository, OrdiniRepository>();
builder.Services.AddHttpClient<IGeocodingService, NominatimGeocodingService>();
builder.Services.AddScoped<RecaptchaService>();

// Configurazione EmailSettings
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// Configurazione EmailSender con logging
builder.Services.AddTransient<IEmailSender, EmailSender>(provider =>
{
    var emailSettings = provider.GetRequiredService<IOptions<EmailSettings>>().Value;
    var logger = provider.GetRequiredService<ILogger<EmailSender>>();
    return new EmailSender(emailSettings, logger);
});

// Configurazione Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WebAppEF API",
        Version = "v1",
        Description = "API per la gestione dei clienti e ordini.",
        Contact = new OpenApiContact { Name = "Carlo", Email = "supporto@webapief.com", Url = new Uri("http://localhost:5000/") }
    });
});

// Configurazione MVC e Razor Pages
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddRazorPages();

var app = builder.Build();

// Applica le migrazioni e seeding del database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();

        // Cancella tutti i clienti esistenti
        if (context.Clienti.Any())
        {
            context.Clienti.RemoveRange(context.Clienti);
            context.SaveChanges(); // Salva la cancellazione prima di ripopolare
        }

        // Riopopola la tabella Clienti con nuovi dati Bogus
        context.Clienti.AddRange(DataSeeder.GeneraClienti(50));

        if (!context.Ordini.Any())
        {
            context.Ordini.AddRange(new OrdiniFaker().GenerateOrders(100));
        }

        context.SaveChanges(); // Salva l'inserimento dei nuovi clienti e degli ordini
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

app.UseCors("AllowWithCredentials");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();

app.MapControllers();
app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Middleware per logging delle richieste
app.Use(async (context, next) =>
{
    Log.Information($"Request: {context.Request.Method} {context.Request.Path}");
    await next();
    Log.Information($"Response: {context.Response.StatusCode}");
});

app.Run();

