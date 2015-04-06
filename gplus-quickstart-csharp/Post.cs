using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GPlusQuickstartCsharp
{
    public class Post
    {
        public string PosterName { get; set; }

        public string PostBody { get; set; }

        public Post(string posterName, string postBody)
        {
            this.PosterName = posterName;
            this.PostBody = postBody;
        }
    }
}