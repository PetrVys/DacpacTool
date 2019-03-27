using System;
using System.Linq;
using System.IO.Packaging;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Security.Cryptography;

namespace DacpacTool
{
    public static class ProcessDacpac
    {
        public static void Process(string fileName, Action<XElement, string, XmlWriter> processElement)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);

            using (var package = Package.Open(fileName, FileMode.Open, FileAccess.ReadWrite))
            {
                UnpackAndProcessModel(package, tempPath, processElement);
                UnpackAndProcessOrigin(package, tempPath);
                PackFile(package, tempPath, "Origin.xml");
                PackFile(package, tempPath, "model.xml");
                Directory.Delete(tempPath, true);
            }
        }

        private static void PackFile(Package package, string path, string fileName)
        {
            var uri = PackUriHelper.CreatePartUri(new Uri("/" + fileName, UriKind.Relative));
            if (package.PartExists(uri))
            {
                package.DeletePart(uri);
            }
            PackagePart part = package.CreatePart(uri, "", CompressionOption.Normal);
            using (var src = new FileStream(Path.Combine(path, fileName), FileMode.Open, FileAccess.Read))
            using (var dest = part.GetStream())
            {
                src.CopyTo(dest);
            }
        }

        private static void UnpackAndProcessModel(Package package, string path, Action<XElement, string, XmlWriter> processElement)
        {
            var part = package.GetPart(PackUriHelper.CreatePartUri(new Uri("/model.xml", UriKind.Relative)));
            var targetFilePath = Path.Combine(path, "model.xml");
            var readerSettings = new XmlReaderSettings() { CheckCharacters = false, IgnoreWhitespace = false };
            var writerSettings = new XmlWriterSettings() { CloseOutput = true, CheckCharacters = false, Indent = false };

            using (var source = part.GetStream(FileMode.Open, FileAccess.Read))
            using (var source2 = part.GetStream(FileMode.Open, FileAccess.Read))
            using (var target = File.OpenWrite(targetFilePath))
            using (var reader = XmlReader.Create(source, readerSettings))
            using (var navigableReader = new NavigableReader(source2))
            using (var writer = XmlWriter.Create(target, writerSettings))
            {
                var xmlInfo = (IXmlLineInfo)reader;
                while (reader.Read())
                {
                    while (reader.NodeType == XmlNodeType.Element && reader.Name == "Element")
                    {
                        navigableReader.NavigateTo(xmlInfo.LineNumber, xmlInfo.LinePosition);
                        XElement e = XNode.ReadFrom(reader) as XElement;
                        var elementString = navigableReader.ReadUntil(xmlInfo.LineNumber, xmlInfo.LinePosition);
                        processElement(e, elementString, writer);
                    }
                    CopySingleXmlNode(reader, writer);
                }
            }
        }

        private static void UnpackAndProcessOrigin(Package package, string path)
        {
            var part = package.GetPart(PackUriHelper.CreatePartUri(new Uri("/Origin.xml", UriKind.Relative)));
            var targetFilePath = Path.Combine(path, "Origin.xml");

            using (var source = part.GetStream(FileMode.Open, FileAccess.Read))
            using (var target = File.OpenWrite(targetFilePath))
            using (var reader = XmlReader.Create(source))
            using (var writer = XmlWriter.Create(target))
            {
                var checksum = GetChecksum(Path.Combine(path, "model.xml"));
                var content = XElement.Load(reader);
                XNamespace ns = "http://schemas.microsoft.com/sqlserver/dac/Serialization/2012/02";
                var chksumAttr = content.Descendants(ns + "Checksum").Where(e => e.Attribute("Uri").Value == "/model.xml").Single();
                chksumAttr.SetValue(checksum);
                content.WriteTo(writer);
            }
        }

        //https://blogs.msdn.microsoft.com/mfussell/2005/02/12/combining-the-xmlreader-and-xmlwriter-classes-for-simple-streaming-transformations/
        private static void CopySingleXmlNode(XmlReader reader, XmlWriter writer)
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    writer.WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                    writer.WriteAttributes(reader, true);
                    if (reader.IsEmptyElement)
                    {
                        writer.WriteEndElement();
                    }
                    break;
                case XmlNodeType.Text:
                    writer.WriteString(reader.Value);
                    break;
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    writer.WriteWhitespace(reader.Value);
                    break;
                case XmlNodeType.CDATA:
                    writer.WriteCData(reader.Value);
                    break;
                case XmlNodeType.EntityReference:
                    writer.WriteEntityRef(reader.Name);
                    break;
                case XmlNodeType.XmlDeclaration:
                case XmlNodeType.ProcessingInstruction:
                    writer.WriteProcessingInstruction(reader.Name, reader.Value);
                    break;
                case XmlNodeType.DocumentType:
                    writer.WriteDocType(reader.Name, reader.GetAttribute("PUBLIC"), reader.GetAttribute("SYSTEM"), reader.Value);
                    break;
                case XmlNodeType.Comment:
                    writer.WriteComment(reader.Value);
                    break;
                case XmlNodeType.EndElement:
                    writer.WriteFullEndElement();
                    break;
            }
        }

        private static string GetChecksum(string fileName)
        {
            using (FileStream stream = File.OpenRead(fileName))
            {
                var sha = new SHA256Managed();
                byte[] checksum = sha.ComputeHash(stream);
                return BitConverter.ToString(checksum).Replace("-", string.Empty).ToUpperInvariant();
            }
        }
    }
}
