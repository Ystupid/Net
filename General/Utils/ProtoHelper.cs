using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.General.Utils
{
    public class ProtoHelper
    {
        public static byte[] Serialize<T>(T t)
        {
            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, t);
                return stream.ToArray();
            }
        }

        public static T Deserialize<T>(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            {
                return Serializer.Deserialize<T>(stream);
            }
        }
    }
}
