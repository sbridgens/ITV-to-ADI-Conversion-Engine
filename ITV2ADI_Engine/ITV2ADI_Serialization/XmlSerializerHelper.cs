using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ITV2ADI_Engine.ITV2ADI_Serialization
{
    public class XmlSerializerHelper<T>
    {
        public Type _type;

        public XmlSerializerHelper()
        {
            _type = typeof(T);
        }


        public void Save(string path, object obj)
        {
            using (TextWriter textWriter = new StreamWriter(path))
            {
                XmlSerializer serializer = new XmlSerializer(_type);
                serializer.Serialize(textWriter, obj);
                textWriter.Close();
            }

        }

        public T Read(string path)
        {
            T result;
            using (TextReader textReader = new StringReader(path))
            {
                XmlSerializer deserializer = new XmlSerializer(_type);
                result = (T)deserializer.Deserialize(textReader);
            }
            return result;
        }
    }
}
