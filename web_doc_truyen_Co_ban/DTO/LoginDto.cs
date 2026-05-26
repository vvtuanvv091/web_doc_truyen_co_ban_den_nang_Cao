namespace web_doc_truyen_Co_ban.DTO
{
    public record LoginDto
    {
        public string Email { get; set; }

        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}