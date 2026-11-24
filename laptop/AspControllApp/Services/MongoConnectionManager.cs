using AspControllApp.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AspControllApp.Services;

public sealed class MongoConnectionManager : IDisposable
{
    private const string PasswordPlaceholder = "<password>";

    private readonly IOptionsMonitor<MongoDbOptions> _optionsMonitor;
    private readonly ILogger<MongoConnectionManager> _logger;
    private readonly object _sync = new();
    private MongoClient? _client;

    public MongoConnectionManager(IOptionsMonitor<MongoDbOptions> optionsMonitor, ILogger<MongoConnectionManager> logger)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public bool IsConnected
    {
        get
        {
            lock (_sync)
            {
                return _client is not null;
            }
        }
    }

    public string? LastError { get; private set; }

    public async Task<bool> ConnectAsync(string? password, CancellationToken cancellationToken = default)
    {
        var options = _optionsMonitor.CurrentValue;
        if (string.IsNullOrWhiteSpace(options.DatabaseName))
        {
            LastError = "Database name is not configured. Set MongoDb:DatabaseName in secrets/mongodb.json or secrets/.env.";
            return false;
        }

        MongoUrl mongoUrl;
        try
        {
            mongoUrl = BuildMongoUrl(options.ConnectionString, password);
        }
        catch (InvalidOperationException ex)
        {
            LastError = ex.Message;
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to build MongoDB connection URL from configuration.");
            LastError = "MongoDB connection configuration is invalid.";
            return false;
        }

        try
        {
            var client = new MongoClient(mongoUrl);
            var pingDatabase = client.GetDatabase(options.DatabaseName);
            await pingDatabase.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1),
                cancellationToken: cancellationToken);

            lock (_sync)
            {
                _client?.Dispose();
                _client = client;
            }

            LastError = null;
            _logger.LogInformation("Successfully connected to MongoDB database {DatabaseName}.", options.DatabaseName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MongoDB connection attempt failed.");
            LastError = "Could not connect to MongoDB. Check configuration, credentials, and network access.";
            return false;
        }
    }

    public void Disconnect()
    {
        lock (_sync)
        {
            _client?.Dispose();
            _client = null;
        }

        LastError = null;
    }

    public IMongoCollection<T>? GetCollection<T>(string collectionName)
    {
        MongoClient? client;
        lock (_sync)
        {
            client = _client;
        }

        if (client is null)
        {
            return null;
        }

        var databaseName = _optionsMonitor.CurrentValue.DatabaseName;
        return client.GetDatabase(databaseName).GetCollection<T>(collectionName);
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _client?.Dispose();
            _client = null;
        }
    }

    private static MongoUrl BuildMongoUrl(string configuredConnectionString, string? password)
    {
        if (string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            throw new InvalidOperationException(
                "MongoDB connection string is not configured. Add it to secrets/mongodb.json or secrets/.env.");
        }

        if (configuredConnectionString.Contains(PasswordPlaceholder, StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Enter the database password to connect.");
            }

            var parseValue = configuredConnectionString.Replace(
                PasswordPlaceholder,
                "PASSWORD_PLACEHOLDER",
                StringComparison.Ordinal);
            var urlBuilder = new MongoUrlBuilder(parseValue)
            {
                Password = password
            };

            return urlBuilder.ToMongoUrl();
        }

        return MongoUrl.Create(configuredConnectionString);
    }
}
