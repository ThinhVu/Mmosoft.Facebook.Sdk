using HtmlAgilityPack;
using Mmosoft.Facebook.Sdk.Models.Page;
using Mmosoft.Facebook.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Mmosoft.Facebook.Sdk
{
    public partial class PageEndpoint : FbClient
    {        
        public PageEndpoint(string username, string password) 
            : base(username, password)
        {            
        }

        // ----------- Page ----------- //
        /// <summary>
        /// Send request to like or dislike target page
        /// </summary>
        /// <param name="pageIdOrAlias">Id's target page</param>
        public void LikePage(string pageIdOrAlias)
        {            
            HtmlNode likeAnchor = this.GetLikePageAnchor(pageIdOrAlias);
            if (likeAnchor == null)
                throw new Exception("Page:LikePage:likeAnchor is null");
            var href = likeAnchor.GetAttributeValue("href", string.Empty);
            if (string.IsNullOrWhiteSpace(href))
                throw new Exception("Page:LikePage:href is null");
            _requestHandler.SendGETRequest("http://m.facebook.com" + href);
        }
        /// <summary>
        /// Get page id from page alias
        /// </summary>
        /// <param name="pageAlias"></param>
        /// <returns></returns>
        public string GetPageId(string pageAlias)
        {
            // When we get likeAnchor HtmlNode, 2 case happened
            // 1. likeAnchor is null
            // 2. likeAnchor not null => we can get page Id from like anchor href.
            HtmlNode likeAnchor = this.GetLikePageAnchor(pageAlias);
            if (likeAnchor == null) return "-1";
            var compiledRegex = new Regex(@"fan&amp;id=(?<pid>\d+)");
            Match match = compiledRegex.Match(likeAnchor.GetAttributeValue("href", string.Empty));
            return match.Groups["pid"].Value;
        }
        /// <summary>
        /// Get page reviews
        /// </summary>
        /// <param name="pageIdOrAlias"></param>
        /// <returns></returns>
        public PageReviewInfo GetPageReviewInfo(string pageIdOrAlias)
        {
            var pageId = string.Empty;

            // Checking if pageIdOrAlias is pageAlias or PageId            
            MatchCollection matches = Regex.Matches(pageIdOrAlias, "\\d+");

            // param passed is page alias, we need to get page id from it.
            if (matches.Count != 1)
                pageId = GetPageId(pageIdOrAlias);

            if (pageId.Length == 0)
                throw new ArgumentException("Can not detect page id of " + pageIdOrAlias);

            HtmlNode htmlNode = __BuildDomFromUrl("https://m.facebook.com/page/reviews.php?id=" + pageIdOrAlias);

            if (Localization.PageNotFound.Any(text => htmlNode.InnerHtml.Contains(text)))
                throw new Exception(pageIdOrAlias + " not contains review page");

            // get review nodes -- review node contain user's review
            // TODO : Replace with more safely xpath
            HtmlNodeCollection reviewNodes = htmlNode.SelectNodes("/html/body/div/div/div[2]/div[2]/div[1]/div/div[3]/div/div/div/div");

            // Create page review
            var pageReview = new PageReviewInfo();

            // loop through DOM reviewNodes
            foreach (var reviewNode in reviewNodes)
            {
                // create new instance of review info
                var reviewInfo = new PageReview();

                // Get avatar
                HtmlNode imgAvatarNode = reviewNode.SelectSingleNode("div/div/div[1]/a/div/img");
                if (imgAvatarNode != null)
                    reviewInfo.UserAvatarUrl = WebUtility.HtmlDecode(imgAvatarNode.GetAttributeValue("src", string.Empty));

                // User name and id                
                HtmlNode userNameIdNode = reviewNode.SelectSingleNode("div/div/div[2]/div/div[1]/a[1]");
                if (userNameIdNode != null)
                {
                    // Get urlink and parse
                    string urlLink = userNameIdNode.GetAttributeValue("href", null);
                    if (urlLink != null)
                    {
                        if (urlLink.Contains("/profile.php?id="))
                            reviewInfo.UserId = urlLink.Substring(16); // /profile.php?id=100012141183155
                        else
                            reviewInfo.UserId = urlLink.Substring(1); // /kakarotto.pham.9
                    }

                    HtmlNode nameNode = userNameIdNode.SelectSingleNode("span");
                    if (nameNode != null)
                        reviewInfo.UserDisplayName = WebUtility.HtmlDecode(nameNode.InnerText + string.Empty);
                }

                // Get rate score
                HtmlNode rateScoreNode = reviewNode.SelectSingleNode("div/div/div[2]/div/div[1]/a[2]/img");
                // return -1 indicate that can not recognize value
                if (rateScoreNode != null)
                    reviewInfo.RateScore = int.Parse(rateScoreNode.GetAttributeValue("alt", "-1"), CultureInfo.CurrentCulture);

                // Get fully rate content page
                HtmlNode rateContentNode = reviewNode.SelectSingleNode("div/div/div[2]/div/div[1]/a[2]");
                if (rateContentNode != null)
                {
                    string rateContentAnchorLink = rateContentNode.GetAttributeValue("href", null);
                    if (rateContentAnchorLink != null)
                    {
                        HtmlNode htmlRateContentNode = __BuildDomFromUrl("https://m.facebook.com" + rateContentAnchorLink);
                        // TODO : Replace with more safely
                        HtmlNode contentNode = htmlRateContentNode.SelectSingleNode("html/body/div/div/div[2]/div/div[1]/div/div[1]/div/div[1]/div[2]/p");

                        if (contentNode != null)
                            reviewInfo.Content = contentNode.InnerText;
                    }
                }

                pageReview.Reviews.Add(reviewInfo);
            }

            return pageReview;
        }
        /// <summary>
        /// Get likepage anchor from specified page.
        /// </summary>
        /// <param name="pageAlias">Page alias name</param>
        /// <returns>Anchor element if exist or null if not</returns>
        public HtmlNode GetLikePageAnchor(string pageAlias)
        {
            var pageId = string.Empty;
            HtmlNode htmlNode = __BuildDomFromUrl("https://m.facebook.com/" + pageAlias);
            HtmlNode pagesMbasicContextItemsId = htmlNode.SelectSingleNode("//div[@id='pages_mbasic_context_items_id']");
            if (pagesMbasicContextItemsId != null)
            {
                // This div contain Like, Message, .. action
                HtmlNode actionDiv = pagesMbasicContextItemsId.PreviousSibling;
                if (actionDiv == null) return null;

                HtmlNodeCollection anchors = actionDiv.SelectNodes("//table/tbody/tr/td[1]/a");
                var compiledRegex = new Regex(@"fan&amp;id=(?<pid>\d+)");

                foreach (HtmlNode anchor in anchors)
                {
                    if (anchor == null) continue;

                    if (anchor.InnerText == Localization.Like)
                        return anchor;
                }
            }

            return null;
        }
        public List<PageAlbumModel> GetPageAlbums(string pageAlias)
        {
            // NOTE: Current implementation only extract 1st page of specified page
            string pageAlbumUrl = "https://m.facebook.com/" + pageAlias + "/photos/?tab=albums";
            HtmlNode dom = __BuildDomFromUrl(pageAlbumUrl);
            HtmlNode albumNodeHeader = dom.SelectSingleNode("//div/h3[text()='Albums']");
            // 1st page            
            HtmlNodeCollection albumNodes = __BuildDomFromHtmlContent(albumNodeHeader.NextSibling.InnerHtml).SelectNodes("//a");
            List<PageAlbumModel> models = null;
            if (albumNodes != null || albumNodes.Count > 0)
            {
                models = new List<PageAlbumModel>();

                foreach (var node in albumNodes)
                {
                    PageAlbumModel model = new PageAlbumModel();
                    model.AlbumName = node.InnerText;
                    model.AlbumUrl = WebUtility.HtmlDecode("https://m.facebook.com" + node.GetAttributeValue("href", string.Empty));
                    models.Add(model);
                }
            }
            return models;
        }

        public IEnumerable<PageAlbumImageModel> GetPageAlbumImages(string pageAlias, string albumId)
        {
            string albumUrl = string.Format("https://m.facebook.com/{0}/albums/{1}/", pageAlias, albumId);

            while (albumUrl != null)
            {
                // https://m.facebook.com/FHNChallengingTheImpossible/albums/568853546917720/
                HtmlNode dom = __BuildDomFromUrl(albumUrl);
                HtmlNodeCollection thumbAreaImgs = dom.SelectNodes("//div[@id='thumbnail_area']/a");

                if (thumbAreaImgs != null || thumbAreaImgs.Count > 0)
                {
                    foreach (var thumbItem in thumbAreaImgs)
                    {
                        PageAlbumImageModel img = new PageAlbumImageModel();
                        img.ImageUrl = "https://m.facebook.com" + WebUtility.HtmlDecode(thumbItem.GetAttributeValue("href", string.Empty));
                        img.ThumbImageUrl = WebUtility.HtmlDecode(thumbItem.SelectSingleNode("img").GetAttributeValue("src", string.Empty));
                        yield return img;
                    }
                }

                // more item?
                HtmlNode moreItemAnchorNode = dom.SelectSingleNode("//div[@id='m_more_item']/a");
                if (moreItemAnchorNode != null)
                {
                    albumUrl = "https://m.facebook.com" + moreItemAnchorNode.GetAttributeValue("href", string.Empty);
                }
                else
                {
                    albumUrl = null; // end process
                }
            }
        }

        public string GetPageAlbumImageSource(PageAlbumImageModel model)
        {
            HtmlNode node = __BuildDomFromUrl(model.ImageUrl);
            var imgNode = node.SelectSingleNode("//div[@class='ba']/div[@style='text-align:center;']/img");
            return WebUtility.HtmlDecode(imgNode.GetAttributeValue("src", ""));
        }

        public void CommentToPageAlbumImage(string imageUrl, string commentText)
        {
            HtmlNode node = __BuildDomFromUrl(imageUrl);
            var commentForm = node.SelectSingleNode("//form[starts-with(@action,'/a/comment.php')]");
            List<string> postData = __ExtractHidenInputNodes(commentForm.ParentNode);
            postData.Add("comment_text=" + Uri.EscapeDataString(commentText));
            string postUrl = "https://m.facebook.com" + WebUtility.HtmlDecode(commentForm.GetAttributeValue("action", ""));
            _requestHandler.SendPOSTRequest(postUrl, __CreatePayload(postData));
        }

        public bool LikePhoto(string targetId)
        {
            HtmlNode document = __BuildDomFromUrl(targetId);
            HtmlNode likeAction = document.SelectSingleNode("//div[@id='MPhotoActionbar']/div[starts-with(@id,'actions_')]/table/tbody/tr/td/a[starts-with(@href,'/a/like.php')]");
            if (likeAction != null)
            {
                string likeHref = likeAction.GetAttributeValue("href", null);
                if (likeHref != null)
                {
                    Uri requestUrl = new Uri("https://m.facebook.com" + WebUtility.HtmlDecode(likeHref));
                    HttpWebRequest getRequest = _requestHandler.CreateGETRequest(requestUrl);
                    getRequest.Referer = targetId;
                    getRequest.GetResponse();
                    return true;
                }
            }
            return false;
        } 
    }
}
