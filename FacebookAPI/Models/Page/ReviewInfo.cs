namespace FacebookAPI.Models.Page
{
    public class ReviewInfo
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string AvatarUrl { get; set; }
        public string Content { get; set; }
        public int? RateScore { get; set; }

        public ReviewInfo()
        {
            Id = string.Empty;
            DisplayName = string.Empty;
            AvatarUrl = string.Empty;
            Content = string.Empty;
            RateScore = 0;
        }
    }
}
