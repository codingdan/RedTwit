using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public interface ILinkCache
    {
        Task<bool> CacheLink(Link link);

        Task<Link> GetLink(string uri);
    }
}
