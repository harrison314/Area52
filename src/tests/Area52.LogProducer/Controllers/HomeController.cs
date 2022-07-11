using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Area52.LogProducer.Models;

namespace Area52.LogProducer.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> logger;

    public HomeController(ILogger<HomeController> logger)
    {
        this.logger = logger;
    }

    public IActionResult Index()
    {
        this.logger.LogTrace("Entering to Index.");
        return View();
    }

    public IActionResult Privacy()
    {
        this.logger.LogTrace("Entering to Privacy.");
        return View();
    }

    public IActionResult ExecError()
    {
        this.logger.LogError(new Exception("Some Exception"),
            "Message exception with host {host}.", this.HttpContext.Request.Host);
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
