using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OData.Mongo.Helpers
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMongoClient(this IServiceCollection services, IConfiguration configuration)
        {
            var client = SetupMongoClient(configuration);
            services.AddSingleton<IMongoClient>(client);

            return services;
        }

        private static IMongoClient SetupMongoClient(IConfiguration configuration)
        {
            var settings = new MongoClientSettings
            {
                Credential =
                    MongoCredential.CreateCredential(configuration["Mongo:AdminDatabase"], configuration["Mongo:Username"],
                        configuration["Mongo:Password"]),
                MaxConnectionIdleTime = TimeSpan.FromMinutes(1),
                UseSsl = false,
                //setting WriteConcern to majority, to get casual consistency (ReadConcern is majority by default)
                WriteConcern = new WriteConcern("majority", wTimeout: TimeSpan.FromSeconds(5), journal: true)
            };

            string server = configuration["Mongo:Server"];

            if (server.IndexOf(',') == -1)
            {
                int port = 27017;
                if (server.IndexOf(':') != -1)
                {
                    int.TryParse(server.Split(':')[1], out port);
                    settings.Server = (new MongoServerAddress(server.Split(':')[0], port));
                }
                else
                {
                    settings.Server = (new MongoServerAddress(server, port));
                }
            }
            else
            {
                // There is an array of servers, so we're dealing with a cluster
                List<MongoServerAddress> serverAddresses = new List<MongoServerAddress>();
                foreach (string serverAddress in server.Split(','))
                {
                    int port = 27017;
                    if (serverAddress.IndexOf(':') != -1)
                    {
                        int.TryParse(serverAddress.Split(':')[1], out port);
                    }

                    serverAddresses.Add(new MongoServerAddress(serverAddress, port));
                }

                settings.Servers = serverAddresses;
            }

            return new MongoClient(settings);
        }
    }
}
