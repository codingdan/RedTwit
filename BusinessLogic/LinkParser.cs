using HtmlAgilityPack;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace BusinessLogic
{
    public static class LinkParser
    {
        public const string youtubeRegexPattern = @"(?:youtu\.be\/|youtube.com\/(?:watch\?.*\bv=|embed\/|v\/)|ytimg\.com\/vi\/)(.+?)(?:[^-a-zA-Z0-9]|$)";
        public const int youtubeIdIndex = 1;

        public static Regex youtubeRegex = new Regex(youtubeRegexPattern);

        public static async Task<Link> ParseLink(string uri)
        {
            // Check if this uri is cached
            var cachedLink = await Dependencies.Instance.Value.GetLinkCache().GetLink(uri);
            if (cachedLink != null)
            {
                return cachedLink;
            }

            Link link = null;

            // Check if its a youtube link
            var youtubeMatch = youtubeRegex.Match(uri);

            if (youtubeMatch.Success)
            {
                var matchedId = youtubeMatch.Groups[youtubeIdIndex].Value;

                link = new YoutubeLink()
                {
                    Uri = uri,
                    YoutubeId = matchedId,
                    Type = LinkType.YouTube,
                };
            }
            else
            {
                link = ScrapeLink(uri);
            }

            // Cache this link
            await Dependencies.Instance.Value.GetLinkCache().CacheLink(link);

            return link;
        }

        public static Link ScrapeLink(string uri)
        {
            try
            {
                // Download the html
                HtmlWeb htmlWebPage = new HtmlWeb();
                HtmlDocument htmlDoc = htmlWebPage.Load(MakeUriString(uri));

                // Get the title of the page
                var pageTitle = htmlDoc.DocumentNode.Descendants("title").FirstOrDefault();
                // Parse it

                return new Link()
                {
                    Type = LinkType.Generic,
                    Uri = uri,
                    Name = pageTitle.InnerText,
                };
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static bool hasProto(String url)
        {
            return Regex.IsMatch(url, "^\\w+://");
        }

        public static string MakeUriString(string uri)
        {
            return (hasProto(uri) ? "" : "http://") + uri;
        }
    }
}