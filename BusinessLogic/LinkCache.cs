using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic
{
    public class LinkCache : ILinkCache
    {
        private const string LinkKeyFormat = "cachedLink:{0}";
        public async Task<bool> CacheLink(Link link)
        {
            var redisDb = Dependencies.Instance.Value.GetRedisDatabase();

            var serializedLink = SerializationUtils.Serialize(link);

            return await redisDb.StringSetAsync(string.Format(LinkKeyFormat, link.Uri), serializedLink);
        }

        public async Task<Link> GetLink(string uri)
        {
            var redisDb = Dependencies.Instance.Value.GetRedisDatabase();

            var cacheValue = await redisDb.StringGetAsync(string.Format(LinkKeyFormat, uri));

            if (cacheValue.IsNull)
            {
                return null;
            }
            else
            {
                Link link = SerializationUtils.Deserialize<Link>((byte[])cacheValue);
                return link;
            }
        }
    }
}
