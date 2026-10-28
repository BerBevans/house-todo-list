namespace HouseTodoList.Web;

/// <summary>
/// The composition root: the only type in this project that may use HouseTodoList.Adapters.
/// </summary>
internal static class Program
{
    public static Task Main(string[] args) => BuildApp(args).RunAsync();

    /// <summary>Registers the services and builds the app. Unit tests call this directly.</summary>
    internal static WebApplication BuildApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        return builder.Build();
    }
}
