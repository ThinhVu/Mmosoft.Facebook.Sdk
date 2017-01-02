using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mmosoft.Facebook.Sdk.Utilities
{
    public static class HtmlHelper
    {
        static HtmlDocument _htmlDoc = new HtmlDocument();      

        /// <summary>
        /// Load DOM method. This method get url and download html content then parse to DOM object
        /// using HtmlAgilityPack library.
        /// </summary>
        /// <param name="url">Url want to get DOM</param>
        /// <param name="cookieContainer">cookie you want to pass for this method</param>        
        /// <returns>DOM object parsed from html content</returns>
        public static HtmlNode BuildDom(string content)
        {                        
            _htmlDoc.LoadHtml(content);

            return _htmlDoc.DocumentNode;
        }

        /// <summary>
        /// Build payload from input collection
        /// </summary>
        /// <param name="inputNodes">input node</param>
        /// <param name="additionKeyValuePair">addition key value pair</param>
        /// <returns>payload string</returns>
        public static string BuildPayload(IEnumerable<HtmlNode> inputNodes, string additionKeyValuePair)
        {
            var inputs = new List<string>();

            foreach (HtmlNode node in inputNodes)
            {
                if (node.GetAttributeValue("type", string.Empty) != "hidden") continue;

                var name = node.GetAttributeValue("name", null);
                var value = node.GetAttributeValue("value", null);

                inputs.Add(name + "=" + value);
            }

            if (!string.IsNullOrWhiteSpace(additionKeyValuePair))
                inputs.Add(additionKeyValuePair);

            return string.Join("&", inputs);
        }
    }
}
