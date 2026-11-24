using AspControllApp.Models;
using AspControllApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace AspControllApp.Pages;

public class IndexModel : PageModel
{
    private readonly TemperatureAnalyticsService _analyticsService;
    private readonly MongoConnectionManager _connectionManager;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        TemperatureAnalyticsService analyticsService,
        MongoConnectionManager connectionManager,
        ILogger<IndexModel> logger)
    {
        _analyticsService = analyticsService;
        _connectionManager = connectionManager;
        _logger = logger;
    }

    public TemperatureAnalyticsViewModel Analytics { get; private set; } = TemperatureAnalyticsViewModel.Empty();

    public bool IsDatabaseConnected => _connectionManager.IsConnected;

    [BindProperty]
    public string? DatabasePassword { get; set; }

    [TempData]
    public string? ConnectionMessage { get; set; }

    [TempData]
    public bool ConnectionMessageIsError { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAnalyticsAsync(cancellationToken);
    }

    [EnableRateLimiting("connect")]
    public async Task<IActionResult> OnPostConnectAsync(CancellationToken cancellationToken)
    {
        var connected = await _connectionManager.ConnectAsync(DatabasePassword, cancellationToken);
        if (connected)
        {
            ConnectionMessage = "Connected to MongoDB.";
            ConnectionMessageIsError = false;
        }
        else
        {
            ConnectionMessage = _connectionManager.LastError ?? "Could not connect to MongoDB. Check configuration and try again.";
            ConnectionMessageIsError = true;
            _logger.LogWarning("MongoDB connect attempt failed from {RemoteIp}.", HttpContext.Connection.RemoteIpAddress);
        }

        DatabasePassword = null;
        return RedirectToPage();
    }

    public IActionResult OnPostDisconnect()
    {
        _connectionManager.Disconnect();
        ConnectionMessage = "Disconnected from MongoDB.";
        ConnectionMessageIsError = false;
        return RedirectToPage();
    }

    private async Task LoadAnalyticsAsync(CancellationToken cancellationToken)
    {
        try
        {
            Analytics = await _analyticsService.GetLastMinuteAnalyticsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load temperature analytics from MongoDB.");
            Analytics = TemperatureAnalyticsViewModel.Empty();
            if (IsDatabaseConnected)
            {
                ConnectionMessage = "Connected, but loading analytics failed. Check logs and collection configuration.";
                ConnectionMessageIsError = true;
            }
        }
    }
}

