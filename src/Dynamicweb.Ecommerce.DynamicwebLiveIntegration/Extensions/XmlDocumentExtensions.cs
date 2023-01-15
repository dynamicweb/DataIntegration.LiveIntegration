using System.Text;
using System.Xml;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Extensions
{  
  /// <summary>
  /// Extension methods for XmlDocument.
  /// </summary>
  internal static class XmlDocumentExtensions
  {
    /// <summary>
    /// Creates a formatted / indented string from the XML document.
    /// </summary>
    /// <param name="doc">The document to convert.</param>
    /// <returns>System.String.</returns>
    internal static string Beautify(this XmlDocument doc)
    {
      var sb = new StringBuilder();
      StringWriterWithEncoding sw = null;

      try
      {
        sw = new StringWriterWithEncoding(Encoding.UTF8, sb);

        var settings = new XmlWriterSettings
        {
          Indent = true,
          IndentChars = "  ",
          NewLineChars = "\r\n",
          NewLineHandling = NewLineHandling.Replace,
          Encoding = Encoding.UTF8
        };

        using (var writer = XmlWriter.Create(sw, settings))
        {
          sw = null;
          doc.Save(writer);
        }
      }
      finally
      {
        sw?.Dispose();
      }

      return sb.ToString();
    }
  }
}