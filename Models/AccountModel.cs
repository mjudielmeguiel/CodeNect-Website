namespace CodeNect_Website.Models
{
    public class AccountModel
    {
        public int ID { get; set; }
        public int SERIAL_NUMBER { get; set; }
        public int ACCOUNT_ID { get; set; }
        public string ACCOUNT { get; set; } = string.Empty;
        public string ADDRESS { get; set; } = string.Empty;
        public string CONTACT { get; set; } = string.Empty;
        public string EMAIL { get; set; } = string.Empty;
        public string USERNAME { get; set; } = string.Empty;
        public string PASSWORD { get; set; } = string.Empty;
        public string STATUS { get; set; } = "ACTIVE";
        public DateTime CREATE_AT { get; set; }
    }
}