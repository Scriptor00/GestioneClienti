using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using WebAppEF.Models;
using WebAppEF.Repositories;
using Microsoft.AspNetCore.Mvc;
using WebAppEF.Data;

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

/*
// Configurazione Identity 
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 8;
        options.Password.RequiredUniqueChars = 1;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
*/

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
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "MioProgettoCookie";
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.SlidingExpiration = true;  // Questo fa sì che la scadenza venga rinnovata se l'utente è attivo
    });


// Configurazione autorizzazione
builder.Services.AddAuthorizationBuilder()
                                    // Configurazione admin
                                    .AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"))
                                    // Configurazione user
                                    .AddPolicy("RequireUserRole", policy => policy.RequireRole("User"));

// Iniezione repository
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IOrdiniRepository, OrdiniRepository>();

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

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        DbInitializer.Initialize(context); // Popola il database
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Errore durante il seeding del database.");
    }
}
// Seeding database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
        if (!context.Clienti.Any()) context.Clienti.AddRange(DataSeeder.GeneraClienti(50));
        if (!context.Ordini.Any()) context.Ordini.AddRange(new OrdiniFaker().GenerateOrders(100));
        context.SaveChanges();
    }
    catch (Exception ex)
    {
        Log.Error($"Errore seeding database: {ex.Message}");
    }
}

// Middleware
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

app.Run();
