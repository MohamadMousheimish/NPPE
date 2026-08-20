namespace NPPE.E2ETests;

/// <summary>
/// Owns one app instance for the whole E2E class: forces host creation (which starts
/// Kestrel and captures the address) and seeds the premium student + exam once.
/// </summary>
public class E2EFixture : IAsyncLifetime
{
    public E2EWebAppFactory Factory { get; } = new();

    public async Task InitializeAsync()
    {
        _ = Factory.Services;   // triggers CreateHost -> Kestrel starts, ServerAddress set
        await Factory.SeedAsync();
    }

    public Task DisposeAsync()
    {
        Factory.Dispose();
        return Task.CompletedTask;
    }
}
