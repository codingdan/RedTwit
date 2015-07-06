using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace Models
{
    public class SerializationUtils
    {
        public static byte[] Serialize<T>(T obj)
        {
            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    Serializer.Serialize<T>(stream, obj);

                    return stream.ToArray();
                }
            }
            catch (Exception)
            {
                return new byte[0];
            }
        }

        public static T Deserialize<T>(byte[] serialized)
        {
            try
            {
                using (MemoryStream stream = new MemoryStream(serialized))
                {
                    return Serializer.Deserialize<T>(stream);
                }
            }
            catch (Exception e)
            {
                return default(T);
            }
        }
    }
}