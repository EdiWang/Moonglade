using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace WilderMinds.MetaWeblog
{
  public static class MetaWeblogExtensions
  {
    public static IApplicationBuilder UseMetaWeblog(this IApplicationBuilder builder, string path)
    {
      return builder.UseMiddleware<MetaWeblogMiddleware>(path);
    }

    public static IServiceCollection AddMetaWeblog<TImplementation>(this IServiceCollection coll) where TImplementation : class, IMetaWeblogProvider
    {
      return coll.AddScoped<IMetaWeblogProvider, TImplementation>()
        .AddScoped<MetaWeblogService>();
    }

  }
}
