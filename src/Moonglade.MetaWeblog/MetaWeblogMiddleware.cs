using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace WilderMinds.MetaWeblog
{
  public class MetaWeblogMiddleware
  {
    private ILogger _logger;
    private readonly RequestDelegate _next;
    private string _urlEndpoint;

    public MetaWeblogMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, string urlEndpoint)
    {
      _next = next;
      _logger = loggerFactory.CreateLogger<MetaWeblogMiddleware>();
      _urlEndpoint = urlEndpoint;
    }

    public async Task Invoke(HttpContext context, MetaWeblogService service)
    {
      if (context.Request.Method == "POST" &&
        context.Request.Path.StartsWithSegments(_urlEndpoint) && 
        context.Request != null && 
        context.Request.ContentType.ToLower().Contains("text/xml"))
      {
        try
        {
          context.Response.ContentType = "text/xml";
          var rdr = new StreamReader(context.Request.Body);
          var xml = await rdr.ReadToEndAsync();
          _logger.LogInformation($"Request XMLRPC: {xml}");
          string result = await service.InvokeAsync(xml);
          _logger.LogInformation($"Result XMLRPC: {result}");
          await context.Response.WriteAsync(result, Encoding.UTF8);
        }
        catch (Exception ex)
        {
          _logger.LogError($"Failed to read the content: {ex}");
        }
        return;
      }

      // Continue On
      await _next.Invoke(context);
    }
  }
}