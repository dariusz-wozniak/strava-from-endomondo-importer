using Microsoft.Extensions.DependencyInjection;

var options = Parser.Default.ParseArguments<Options>(args).Value;

var services = new ServiceCollection();

services.AddAuthentication()
        .AddStrava(o =>
        {
            o.ClientId = options.ClientId;
            o.ClientSecret = options.ClientSecret;
        });

Console.ReadKey();