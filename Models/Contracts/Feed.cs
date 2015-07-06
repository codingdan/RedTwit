using System.Runtime.Serialization;

namespace Models
{
    [DataContract]
    public class Feed
    {
        [DataMember]
        public Post[] Posts { get; set; }

        [DataMember]
        public int NumPosts { get; set; }
    }
}