using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Pricely.Common.Application.FileHelpers;
public sealed class XmlHelper
{
    public static string SerializeToXml<T>(T obj)
    {
        // Spécifier aucun espace de noms pour la sérialisation
        var ns = new XmlSerializerNamespaces();
        ns.Add("", "");  // Indiquer qu'il n'y a pas de namespaces

        var settings = new System.Xml.XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 };

        using var ms = new MemoryStream();

        using var streamWriter = new StreamWriter(ms, Encoding.UTF8);


        using var xmlWriter = System.Xml.XmlWriter.Create(streamWriter, settings);

        // Écriture de la balise XML standard
        xmlWriter.WriteStartDocument();

        // Ajout de la déclaration DOCTYPE
        xmlWriter.WriteDocType("cXML", null, "http://xml.cxml.org/schemas/cXML/1.2.024/cXML.dtd", null);

        var serializer = new XmlSerializer(typeof(T));

        serializer.Serialize(xmlWriter, obj, ns);

        xmlWriter.Flush();

        ms.Position = 0;
        using var reader = new StreamReader(ms);

        string xmlOutput = reader.ReadToEnd();

        return xmlOutput;
    }
}
