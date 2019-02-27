using HtmlAgilityPack;
using System.Collections.Generic;
using System.Net;

namespace Mmosoft.Facebook.Utils
{
    public static class HtmlHelper
    {        
        /// <summary>
        /// Parse input string to DOM object.
        /// </summary>
        /// <param name="content">Input string</param>
        /// <returns>DOM object</returns>
        public static HtmlNode BuildDom(string content)
        {
            var htmlDoc = new HtmlDocument();         
            htmlDoc.LoadHtml(content);
            return htmlDoc.DocumentNode;
        }
        
        /// <summary>
        /// Build payload from input collection
        /// </summary>
        /// <param name="inputNodes">input node</param>
        /// <param name="additionKeyValuePair">addition key value pair</param>
        /// <returns>payload string</returns>
        public static string CreatePayload(IEnumerable<HtmlNode> inputNodes, string additionKeyValuePair)
        {
            var inputs = new List<string>();

            foreach (HtmlNode node in inputNodes)
            {
                if (node.GetAttributeValue("type", string.Empty) != "hidden")
                    continue;

                var name = WebUtility.UrlDecode(node.GetAttributeValue("name", string.Empty));
                var value = WebUtility.UrlDecode(node.GetAttributeValue("value", string.Empty));
                
                if (name != string.Empty && value != string.Empty)
                    inputs.Add(name + "=" + value);
            }

            if (!string.IsNullOrWhiteSpace(additionKeyValuePair))
                inputs.Add(additionKeyValuePair);

            return string.Join("&", inputs);
        }
    }
}