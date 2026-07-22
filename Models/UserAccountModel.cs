using System;
using System.ComponentModel.DataAnnotations;

namespace CodeNect_Website.Models
{
    public class UserAccountModel
    {
        public int ID { get; set; }

        [Required]
        [Display(Name = "Account ID")]
        public string ACCOUNT_ID { get; set; } = string.Empty;

        [Display(Name = "Branch ID")]
        public string? BRANCH_ID { get; set; }

        [Required]
        [Display(Name = "Full Name")]
        public string FULL_NAME { get; set; } = string.Empty;

        [Required]
        public string USERNAME { get; set; } = string.Empty;

        [Required]
        public string PASSWORD { get; set; } = string.Empty;

        [EmailAddress]
        public string? EMAIL { get; set; }

        [Display(Name = "Contact Number")]
        public string? CONTACT { get; set; }

        [Required]
        [Display(Name = "User Type")]
        public string USER_TYPE { get; set; } = "Staff";

        [Display(Name = "Plan Type")]
        public string? PLAN_TYPE { get; set; }

        [Display(Name = "Profile Image URL")]
        public string? PROFILE_IMAGE { get; set; }

        [Required]
        public string STATUS { get; set; } = "Active";

        public DateTime DATE_CREATED { get; set; }
        public DateTime? LAST_LOGIN_DATETIME { get; set; }
    }
}