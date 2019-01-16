using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace LightningVoucher.Models
{
    public class VoucherBuyItem
    {
        public string Id { get; set; }

        public int Amount { get; set; }
        public long SatPerVoucher { get; set; }
        public bool claimed { get; set; }


        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public Nullable<DateTime> LastUsed { get; set; } = DateTime.UtcNow;
    }
}
