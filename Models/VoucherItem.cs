using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace LightningVoucher.Models {
    public class VoucherItem {


        public string Id {get;set;}


        public long UsedSat { get; set; }

        public long StartSat{get;set;}

        
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime Created{get;set;} = DateTime.UtcNow;

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime LastUsed { get; set; } = DateTime.UtcNow;


    }
}