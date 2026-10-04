using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace LittleColony.Api.Infrastructure;

/// <summary>Serves a configured WebGL build on the API origin so session cookies remain first party.</summary>
public static class WebGameHosting
{
    /// <summary>Maps existing build files only when a WebClient root was explicitly configured.</summary>
    public static void UseWebGame(this WebApplication app)
    {
        string? configuredPath = app.Configuration["WebClient:RootPath"];
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return;
        }

        string rootPath = Path.GetFullPath(configuredPath, app.Environment.ContentRootPath);
        if (!Directory.Exists(rootPath))
        {
            app.Logger.LogWarning("The configured WebGL build directory does not exist yet.");
            return;
        }

        var files = new PhysicalFileProvider(rootPath);
        app.Lifetime.ApplicationStopped.Register(files.Dispose);
        var types = new FileExtensionContentTypeProvider();
        types.Mappings[".wasm"] = "application/wasm";
        types.Mappings[".data"] = "application/octet-stream";
        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = files,
            ContentTypeProvider = types,
            OnPrepareResponse = context => context.Context.Response.Headers.CacheControl = "no-cache"
        });
    }
}
