namespace MyNewHttpServer;

internal class Program
{
    static async Task Main(string[] args)
    {
        await new Server().RunServer();
    }
}
