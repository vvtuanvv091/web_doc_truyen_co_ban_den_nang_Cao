namespace web_doc_truyen_Co_ban.DTO
{
    public record BookMarkDto
    {
        public Guid Id { get; set; }

        public Guid? ChapterId { get; set; }

        public decimal? ChapterNumber { get; set; }

        public DateTime UpdatedAt { get; set; }

        public string? StoryName { get; set; }

        public string? ChapterTitle { get; set; }
    }
}
