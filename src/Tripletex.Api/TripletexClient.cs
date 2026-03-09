using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tripletex.Api.Authentication;
using Tripletex.Api.Handlers;
using Tripletex.Api.Generated;
using Tripletex.Api.Models;
using Tripletex.Api.Operations;

namespace Tripletex.Api;

public sealed class TripletexClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly HttpClient? _authFreeClient;
    private readonly SessionTokenProvider _tokenProvider;
    private readonly bool _ownsHttpClient;

    public TimesheetOperations Timesheet { get; }
    public InvoiceOperations Invoice { get; }
    public EmployeeOperations Employee { get; }
    public ProjectOperations Project { get; }
    public CustomerOperations Customer { get; }
    public SupplierOperations Supplier { get; }
    public ActivityOperations Activity { get; }
    public ExpenseOperations Expense { get; }
    public ExpenseAttachmentOperations ExpenseAttachment { get; }

    public TripletexClient(TripletexOptions options, ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        loggerFactory ??= NullLoggerFactory.Instance;
        var logger = loggerFactory.CreateLogger<TripletexClient>();

        _authFreeClient = new HttpClient { BaseAddress = new Uri(options.BaseUrl) };

        _tokenProvider = new SessionTokenProvider(
            options.ConsumerToken,
            options.EmployeeToken,
            options.BaseUrl,
            options.SessionLifetime,
            _authFreeClient,
            loggerFactory.CreateLogger<SessionTokenProvider>());

        var pathRewriter = new PathRewriteHandler(GeneratedPathMappings.Mappings) { InnerHandler = new HttpClientHandler() };
        var rateLimiter = new RateLimitHandler(options.MaxRetries, options.RetryBaseDelay, logger) { InnerHandler = pathRewriter };
        var errorHandler = new ErrorHandler { InnerHandler = rateLimiter };
        var authHandler = new BasicAuthHandler(_tokenProvider) { InnerHandler = errorHandler };

        _httpClient = new HttpClient(authHandler)
        {
            BaseAddress = new Uri(options.BaseUrl)
        };
        _ownsHttpClient = true;

        Timesheet = new TimesheetOperations(_httpClient);
        Invoice = new InvoiceOperations(_httpClient);
        Employee = new EmployeeOperations(_httpClient);
        Project = new ProjectOperations(_httpClient);
        Customer = new CustomerOperations(_httpClient);
        Supplier = new SupplierOperations(_httpClient);
        Activity = new ActivityOperations(_httpClient);
        Expense = new ExpenseOperations(_httpClient);
        ExpenseAttachment = new ExpenseAttachmentOperations(_httpClient);
    }

    internal TripletexClient(HttpClient httpClient, SessionTokenProvider tokenProvider)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
        _ownsHttpClient = false;

        Timesheet = new TimesheetOperations(_httpClient);
        Invoice = new InvoiceOperations(_httpClient);
        Employee = new EmployeeOperations(_httpClient);
        Project = new ProjectOperations(_httpClient);
        Customer = new CustomerOperations(_httpClient);
        Supplier = new SupplierOperations(_httpClient);
        Activity = new ActivityOperations(_httpClient);
        Expense = new ExpenseOperations(_httpClient);
        ExpenseAttachment = new ExpenseAttachmentOperations(_httpClient);
    }

    public void Dispose()
    {
        _tokenProvider.Dispose();
        _authFreeClient?.Dispose();
        if (_ownsHttpClient)
            _httpClient.Dispose();
    }
}
