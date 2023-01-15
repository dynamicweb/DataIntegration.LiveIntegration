using System.IO;
using System.Text;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Extensions
{
    /// <summary>
    /// Helper class to write UTF8 XML documents. This class cannot be inherited.
    /// </summary>
    /// <seealso cref="StringWriter" />
    public sealed class StringWriterWithEncoding : StringWriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringWriterWithEncoding"/> class.
        /// </summary>
        /// <param name="encoding">The encoding.</param>
        /// <param name="builder">The builder.</param>
        public StringWriterWithEncoding(Encoding encoding, StringBuilder builder) : base(builder)
        {
            Encoding = encoding;
        }

        /// <summary>
        /// Gets the <see cref="T:System.Text.Encoding" /> in which the output is written.
        /// </summary>
        /// <value>The encoding.</value>
        public override Encoding Encoding { get; }
    }
}