using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace GPlusQuickstartCsharp
{
    public class Dependencies
    {
        private Lazy<ConnectionMultiplexer> redisMultiplexer = new Lazy<ConnectionMultiplexer>(() =>
        {

            return ConnectionMultiplexer.Connect("localhost,allowAdmin=true");
        }, LazyThreadSafetyMode.ExecutionAndPublication);

        public IDatabase GetRedisDatabase()
        {
            return redisMultiplexer.Value.GetDatabase();
        }

        public async Task ClearStore()
        {
            var endpoints = this.redisMultiplexer.Value.GetEndPoints(true);
            foreach (var endpoint in endpoints)
            {
                var server = this.redisMultiplexer.Value.GetServer(endpoint);
                await server.FlushAllDatabasesAsync();
            }
        }

        public static Lazy<Dependencies> Instance = new Lazy<Dependencies>(() =>
            {
                return new Dependencies();
            }, LazyThreadSafetyMode.ExecutionAndPublication);
    }
}