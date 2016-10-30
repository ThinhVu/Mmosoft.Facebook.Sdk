using System.Collections.Generic;

namespace Mmosoft.Facebook.Sdk.Common
{
    public class PageReview
    {
        public string PageId { get; set; }
        public ICollection<Review> Reviews { get; }

        public PageReview()
        {
            PageId = string.Empty;
            Reviews = new List<Review>();
        }
    }

    public class Review
    {
        public string UserId { get; set; }
        public string UserAvatarUrl { get; set; }
        public string UserDisplayName { get; set; }
        public string Content { get; set; }
        public int? RateScore { get; set; }

        public Review()
        {
            UserId = string.Empty;
            UserDisplayName = string.Empty;
            UserAvatarUrl = string.Empty;
            Content = string.Empty;
            RateScore = 0;
        }
    }
}
