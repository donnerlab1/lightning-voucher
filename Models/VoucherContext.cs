using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace LightningVoucher.Models
{
    public class VoucherContext :DbContext
    {
        public VoucherContext(DbContextOptions<VoucherContext> options)
            : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<VoucherItem>()
                .Property(p => p.UsedSat)
                .HasDefaultValue(0);
        }

        public DbSet<VoucherItem> VoucherItems { get; set;}
        public DbSet<VoucherBuyItem> VoucherBuyItems { get; set; }
    }
}
