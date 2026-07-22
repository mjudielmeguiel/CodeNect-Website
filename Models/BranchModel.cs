using System;

namespace CodeNect_Website.Models
{
    public class BranchModel
    {
        public int ID { get; set; }
        public string BRANCH_ID { get; set; } = string.Empty;
        public string BRANCH { get; set; } = string.Empty;
        public string ACCOUNT { get; set; } = string.Empty;
        public string BUSINESS_TYPE { get; set; } = string.Empty;
        public string TIN { get; set; } = string.Empty;
        public DateTime? TIN_REGISTERED { get; set; }
        public string ADDRESS { get; set; } = string.Empty;
        public string EMAIL { get; set; } = string.Empty;
        public string CONTACT { get; set; } = string.Empty;
        public string MANAGER { get; set; } = string.Empty;
        public DateTime? REGISTRATION_DATE { get; set; }
        public string STATUS { get; set; } = "Active";
    }
}