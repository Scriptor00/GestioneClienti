using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebAppEF.Models;

namespace WebAppEF.Controllers;

public class HomeController(ILogger<HomeController> logger) : Controller
{
    private readonly ILogger<HomeController> _logger = logger;

    public IActionResult Index()
    {
        _logger.LogInformation("Accesso alla home page.");
        return View();
    }

    public IActionResult Privacy()
    {
        _logger.LogInformation("Accesso alla pagina Privacy Policy.");
        ViewData["Title"] = "Privacy Policy";
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        _logger.LogError("Errore riscontrato. RequestId: {RequestId}", requestId);
        
        return View(new ErrorViewModel { RequestId = requestId });
    }
}
