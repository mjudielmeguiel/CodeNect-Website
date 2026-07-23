using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CodeNect_Website.Models
{
    public class InventoryModel
    {
        [Key]
        public int ID { get; set; }
        public string? ACCOUNT_ID { get; set; }
        public string? BRANCH_ID { get; set; }
        [NotMapped]
        public string? BRANCH_NAME { get; set; }
        public string? BARCODE { get; set; }
        public string? BRAND { get; set; }
        [Column("CATERY")]
        public string? CATEGORY { get; set; }
        public string? SIZE { get; set; }
        public string? UNIT { get; set; }
        public int? AVAILABLE { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? PRICE { get; set; }
        public string? SKU { get; set; }
        public string? VENDOR { get; set; }
        public DateTime? DATE_ADDED { get; set; }
    }
}