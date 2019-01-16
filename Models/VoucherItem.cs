using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace LightningVoucher.Models {
    public class VoucherItem {


        public string Id {get;set;}


        public ulong UsedSat { get; set; }

        public ulong StartSat{get;set;}

        
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Nullable<DateTime> Created{get;set;} = DateTime.UtcNow;

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public Nullable<DateTime> LastUsed { get; set; } = DateTime.UtcNow;


    }
}