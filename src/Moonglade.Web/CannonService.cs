namespace Moonglade.Web;

// https://anduin.aiursoft.cn/post/2020/10/14/fire-and-forget-in-aspnet-core-with-dependency-alive
// GPT lied to me about fire and forget in ASP.NET, please don't believe IHostedService and BackgroundService, only this works
public class CannonService(
    ILogger<CannonService> logger,
    IServiceScopeFactory scopeFactory)
{
    public void FireAsync<T>(Func<T, Task> bullet, Action<Exception> handler = null)
    {
        logger.LogInformation("Fired a new async action.");
        Task.Run(async () =>
        {
            using var scope = scopeFactory.CreateScope();
            var dependency = scope.ServiceProvider.GetRequiredService<T>();
            try
            {
                await bullet(dependency);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Cannon crashed!");
                handler?.Invoke(e);
            }
            finally
            {
                dependency = default;
            }
        });
    }
}