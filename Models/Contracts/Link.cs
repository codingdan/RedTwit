using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Models
{
    public enum LinkType
    {
        Generic=1,
        YouTube=2,
        Imgur=3,
    }

    [DataContract]
    [ProtoInclude(101, typeof(YoutubeLink))]
    public class Link
    {
        [DataMember(Order = 1)]
        public string Uri { get; set; }

        [DataMember(Order = 2)]
        public LinkType Type { get; set; }

        [DataMember(Order = 3)]
        public string Name { get; set; }

        [DataMember(Order = 4)]
        public string ImageUri { get; set; }

        [DataMember(Order = 5)]
        public string Description { get; set; }

        private string ClientStringFormat = "<a href='{0}'>{1}</a><br>"; //<img src='{2}' style='width:304px;height:228px'>";
        public virtual string ClientString
        {
            get
            {
                return string.Format(ClientStringFormat, this.Uri, this.Name);
            }
        }
    }

    [DataContract]
    public class YoutubeLink : Link
    {
        private string iframeFormat = "<iframe id='player' type='text/html' width='640' height='390' src='http://www.youtube.com/embed/{0}?enablejsapi=1&origin=http://example.com' frameborder='0'></iframe><br>";

        [DataMember(Order = 1)]
        public string YoutubeId { get; set; }

        public override string ClientString
        {
            get
            {
                return string.Format(iframeFormat, YoutubeId);
            }
        }
    }
}