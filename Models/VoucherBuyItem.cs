using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace LightningVoucher.Models
{
    public class VoucherBuyItem
    {
        public string Id { get; set; }

        public uint Amount { get; set; }
        public ulong SatPerVoucher { get; set; }
        public bool claimed { get; set; }

        public ICollection<VoucherItem> Vouchers { get; set; } = new List<VoucherItem>();

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public Nullable<DateTime> LastUsed { get; set; } = DateTime.UtcNow;

        
    }
}
