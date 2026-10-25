using System.ComponentModel.DataAnnotations;

namespace AlliedSM.Models
{
    // A single-row table holding settings the Admin can edit from the UI
    // instead of hand-editing appsettings.json.
    //
    // FIX: SchoolSettingId must NOT have an explicit default value (it was
    // "= 1" before). SQL Server auto-generates this column (IDENTITY), and
    // trying to insert an explicit 1 into an IDENTITY column throws
    // "Cannot insert explicit value... when IDENTITY_INSERT is set to OFF."
    // Leaving it at its natural default (0) lets EF Core know to let the
    // database assign the real value itself.
    public class SchoolSetting
    {
        [Key]
        public int SchoolSettingId { get; set; }

        [StringLength(100)]
        public string BankName { get; set; } = "Bank Al-Habib";

        [StringLength(50)]
        public string AccountNumber { get; set; } = "";

        [StringLength(30)]
        public string JazzCashNumber { get; set; } = "";

        // Digits only, country code first, no + or leading 0 — e.g. 923001234567
        [StringLength(20)]
        public string AccountsWhatsApp { get; set; } = "";

        [StringLength(20)]
        public string PrincipalMobile { get; set; } = "";

        // 24-hour "HH:mm" — when the automatic day-end SMS goes out
        [StringLength(5)]
        public string DayEndReportTime { get; set; } = "15:00";
    }
}
