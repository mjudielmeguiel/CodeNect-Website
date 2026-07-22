namespace CodeNect_Website.Models
{
    public class BranchModel
    {
        public int BRANCH_ID { get; set; }
        public string BRANCH { get; set; } = string.Empty;
        public string BUSINESS_TYPE { get; set; } = string.Empty;
        public string CONTACT { get; set; } = string.Empty;
        public string? EMAIL { get; set; }
        public string? MANAGER { get; set; }
        public string? TIN { get; set; }
        public DateTime REGISTRATION_DATE { get; set; }
        public string STATUS { get; set; } = "Active";
    }
}