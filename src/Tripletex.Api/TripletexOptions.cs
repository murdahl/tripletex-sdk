namespace Tripletex.Api;

public sealed class TripletexOptions
{
    public string ConsumerToken { get; set; } = string.Empty;
    public string EmployeeToken { get; set; } = string.Empty;
    public TripletexEnvironment Environment { get; set; } = TripletexEnvironment.Production;
    public TimeSpan SessionLifetime { get; set; } = TimeSpan.FromHours(24);
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    public string BaseUrl => Environment switch
    {
        TripletexEnvironment.Test => "https://api-test.tripletex.tech/v2/",
        TripletexEnvironment.Production => "https://tripletex.no/v2/",
        _ => throw new ArgumentOutOfRangeException(nameof(Environment))
    };
}

public enum TripletexEnvironment
{
    Production,
    Test
}
