﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightningVoucher.ln;
using LightningVoucher.Models;
using Lnrpc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightningVoucher.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VoucherController : ControllerBase
    {
        private readonly VoucherContext _context;
        private readonly ILightning _lightning;

        public VoucherController(VoucherContext context, ILightning lightning)
        {
            _context = context;
            _lightning = lightning;

            if (_context.VoucherItems.Count() == 0)
            {
                // Create a new TodoItem if collection is empty,
                // which means you can't delete all TodoItems.
                _context.VoucherItems.Add(new VoucherItem() {StartSat = 2});
                _context.SaveChanges();
            }
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<VoucherItem>>> GetVoucherItems()
        {
            return await _context.VoucherItems.ToListAsync();
        }

        // GET: api/Todo/5
        [HttpGet("{token}")]
        public async Task<ActionResult<VoucherItem>> GetVoucherI(string token)
        {
            var voucherItem = await _context.VoucherItems.FindAsync(token);

            if (voucherItem == null)
            {
                return NotFound();
            }

            return voucherItem;
        }

        [HttpGet("/api/[controller]/pay/{token}/{payreq}")]
        public async Task<ActionResult<String>> PayVoucher(string token, string payreq)
        {
            var voucherItem = await _context.VoucherItems.FindAsync(token);

            if (voucherItem == null)
            {
                return NotFound();
            }
           using(var transaction = _context.Database.BeginTransaction())
            {
                var cost = await _lightning.SatCost(payreq);
                if (cost > voucherItem.StartSat - voucherItem.UsedSat)
                {
                    return "Error: not enough sat on voucher";
                }

                if (cost == voucherItem.StartSat - voucherItem.UsedSat)
                {
                    _context.Entry(voucherItem).State = EntityState.Deleted;
                    await _context.SaveChangesAsync();
                    return await _lightning.SendPayment(payreq);
                }

                voucherItem.UsedSat += cost;
                _context.Entry(voucherItem).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                transaction.Commit();
                return await _lightning.SendPayment(payreq);
            }
        }

        [HttpGet("/api/[controller]/buy/{amt}/{satPerVoucher}")]
        public async Task<ActionResult<Invoice>> GetVoucherInvoice(int amt, long satPerVoucher)
        {
            var payReq = await _lightning.GetPayReq(amt * satPerVoucher);
            var buyItem = _context.VoucherBuyItems.Add(new VoucherBuyItem() { Id = payReq.PaymentRequest, Amount = amt, SatPerVoucher = satPerVoucher, claimed = false}).Entity;
            _context.SaveChanges();
            return payReq;
        }

        [HttpGet("/api/[controller]/claim/{preimage}/{payreq}")]
        public async Task<ActionResult<ClaimVoucherResponse>> ClaimVoucherInvoice(string preimage, string payreq)
        {
            var voucherBuyItem = await _context.VoucherBuyItems.FindAsync(payreq);
            
            var response = new ClaimVoucherResponse();
            if (voucherBuyItem == null)
            {
                response.ErrorCode = "Payment not Found";
            }



            if (await _lightning.ValidatePayment(payreq, preimage))
            {
                response.ErrorCode = "Ok";
                for (int i = 0; i < voucherBuyItem.Amount; i++)
                {
                    var voucher =_context.VoucherItems.Add(new VoucherItem() {StartSat = voucherBuyItem.SatPerVoucher }).Entity;
                    response.Vouchers.Add(voucher);

                }
                
                _context.SaveChanges();
            }
            else
            {
                response.ErrorCode = "Payment not valid";
               
            }

            return response;
        }


    }


}

public class ClaimVoucherResponse
{
    public string ErrorCode { get; set; }
    public List<VoucherItem> Vouchers { get; }

    public ClaimVoucherResponse()
    {
        this.Vouchers = new List<VoucherItem>();
    }

    public void AddItem(VoucherItem item)
    {
        this.Vouchers.Add(item);
    }
}
