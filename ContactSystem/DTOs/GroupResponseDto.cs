namespace ContactSystem.DTOs
{
    public class GroupResponseDto
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public int? ProjectId { get; set; }
    }
}
