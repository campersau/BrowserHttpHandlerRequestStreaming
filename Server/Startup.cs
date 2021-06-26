using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllersWithViews();
            services.AddRazorPages();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                var receivers = new ConcurrentDictionary<string, ConcurrentDictionary<HttpContext, TaskCompletionSource>>();

                endpoints.Map("/send", async httpContext =>
                {
                    if (httpContext.Request.Query.TryGetValue("channel", out var channel))
                    {
                        try
                        {
                            var pipeReader = httpContext.Request.BodyReader;

                            while (true)
                            {
                                var readResult = await pipeReader.ReadAsync();
                                var buffer = readResult.Buffer;

                                if (receivers.TryGetValue(channel, out var channelReceivers))
                                {
                                    foreach (var (receiver, tcs) in channelReceivers)
                                    {
                                        if (buffer.IsSingleSegment)
                                        {
                                            await receiver.Response.BodyWriter.WriteAsync(buffer.First);
                                        }
                                        else
                                        {
                                            foreach (var b in buffer)
                                            {
                                                await receiver.Response.BodyWriter.WriteAsync(b);
                                            }
                                        }
                                    }
                                }

                                pipeReader.AdvanceTo(buffer.End, buffer.End);

                                if (readResult.IsCompleted)
                                {
                                    break;
                                }
                            }
                        }
                        catch
                        {
                            // ignore
                        }
                        finally
                        {
                            if (receivers.TryRemove(channel, out var channelReceivers))
                            {
                                foreach (var (receiver, tcs) in channelReceivers)
                                {
                                    await receiver.Response.BodyWriter.CompleteAsync();
                                    tcs.SetResult();
                                }
                            }
                        }
                        return;
                    }

                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                });
                endpoints.Map("/receive", httpContext =>
                {
                    if (httpContext.Request.Query.TryGetValue("channel", out var channel))
                    {
                        httpContext.Response.ContentType = "text/plain";

                        var channelReceivers = receivers.GetOrAdd(channel, _ => new ConcurrentDictionary<HttpContext, TaskCompletionSource>());
                        var tcs = new TaskCompletionSource();

                        channelReceivers.TryAdd(httpContext, tcs);

                        httpContext.RequestAborted.Register(() =>
                        {
                            channelReceivers.TryRemove(httpContext, out var tcs);
                            if (channelReceivers.IsEmpty)
                            {
                                receivers.TryRemove(channel, out _);
                            }
                            tcs.SetResult();
                        });

                        return tcs.Task;
                    }

                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

                    return Task.CompletedTask;
                });

                endpoints.Map("/upload", async httpContext =>
                {
                    var form = await httpContext.Request.ReadFormAsync();

                    var file = form.Files[0];

                    await httpContext.Response.WriteAsync($"File uploaded: {file.FileName} {file.Length}bytes");
                });

                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
