namespace Law_Secret_Santa.Models
{
    public class EventNameIdQuery
    {
        public string? EventID { get; set; }
        public string? EventTitle { get; set; }
    }
    public class UserObject
    {
        public string? DiscordId { get; set; }
        public string? EventId { get; set; }
        public string? StreetAddress { get; set; }
    }
    public class EventData
    {
        public string? GuildId { get; set; }
        public string? EventId { get; set; }
        public string? EventTitle { get; set; }
        public string? PriceRange { get; set; }
        public DateTime Deadline { get; set; }
        public string? DiscordRoleId { get; set; }
        public string? StartedExchange { get; set; }
        public string? ActiveEvent { get; set; }
        public string? EventCount { get; set; }  
        public string? EventCreatorId { get; set; }
    }
    public class PairData
    {
        public string? PairId { get; set; }
        public string? EventId { get; set; }
        public string? SantaId { get; set; }
        public string? SubjectId { get; set; }
        public string? GiftStatus { get; set; }
    }
}
