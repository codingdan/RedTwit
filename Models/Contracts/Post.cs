using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Models
{
    [DataContract]
    public class Post
    {
        [DataMember(Order = 1)]
        public DateTime PostTime { get; set; }

        [DataMember(Order = 2)]
        public long PostOwnerId { get; set; }

        [DataMember(Order = 3)]
        public string PostBody { get; set; }

        [DataMember(Order = 4)]
        public string PostType { get; set; }

        [DataMember(Order = 5)]
        public ulong PostId { get; set; }

        [DataMember(Order = 6)]
        public string PostOwnerName { get; set; }

        public Post(long postOwner, string postBody, string postType)
        {
            this.PostOwnerId = postOwner;
            this.PostBody = postBody;
            this.PostType = postType;

            this.PostTime = DateTime.UtcNow;
        }

        public Post() { }
    }
}