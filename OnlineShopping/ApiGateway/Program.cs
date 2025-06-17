using Yarp.ReverseProxy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromMemory(new[]
    {
        new Yarp.ReverseProxy.Configuration.RouteConfig
        {
            RouteId = "orders_route",
            Match = new Yarp.ReverseProxy.Configuration.RouteMatch
            {
                Path = "/orders/{**catchall}"
            },
            ClusterId = "orders_cluster",
            Transforms = new[]
            {
                new Dictionary<string, string> { ["PathRemovePrefix"] = "/orders" }
            }
        },
        new Yarp.ReverseProxy.Configuration.RouteConfig
        {
            RouteId = "payments_route",
            Match = new Yarp.ReverseProxy.Configuration.RouteMatch
            {
                Path = "/payments/{**catchall}"
            },
            ClusterId = "payments_cluster",
            Transforms = new[]
            {
                new Dictionary<string, string> { ["PathRemovePrefix"] = "/payments" }
            }
        }
    },
    new[]
    {
        new Yarp.ReverseProxy.Configuration.ClusterConfig
        {
            ClusterId = "orders_cluster",
            Destinations = new Dictionary<string, Yarp.ReverseProxy.Configuration.DestinationConfig>
            {
                ["orders"] = new Yarp.ReverseProxy.Configuration.DestinationConfig { Address = "http://orders-service:8082/" }
            }
        },
        new Yarp.ReverseProxy.Configuration.ClusterConfig
        {
            ClusterId = "payments_cluster",
            Destinations = new Dictionary<string, Yarp.ReverseProxy.Configuration.DestinationConfig>
            {
                ["payments"] = new Yarp.ReverseProxy.Configuration.DestinationConfig { Address = "http://payments-service:8080/" }
            }
        }
    });

builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
});

var app = builder.Build();

app.MapReverseProxy();
app.MapControllers();

if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();
