using System.Collections.Generic;

namespace Mmosoft.Facebook.Sdk.Models
{
    public class PageReviewInfo
    {
        public string PageId { get; set; }
        public List<PageReview> Reviews { get; private set; }

        public PageReviewInfo()
        {
            PageId = string.Empty;
            Reviews = new List<PageReview>();
        }
    }

    public class PageReview
    {
        public string UserId { get; set; }
        public string UserAvatarUrl { get; set; }
        public string UserDisplayName { get; set; }
        public string Content { get; set; }
        public int RateScore { get; set; }

        public PageReview()
        {
            UserId = string.Empty;
            UserDisplayName = string.Empty;
            UserAvatarUrl = string.Empty;
            Content = string.Empty;
            RateScore = -1;
        }
    }
}
