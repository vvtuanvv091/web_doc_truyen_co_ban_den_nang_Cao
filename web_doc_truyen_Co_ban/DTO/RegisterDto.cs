namespace web_doc_truyen_Co_ban.DTO
{
    public record RegisterDto
    {
        public string Username {get; set;}
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
