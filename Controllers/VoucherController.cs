using System;
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

            if (_context.VoucherBuyItems.Count() == 0)
            {
                _context.VoucherBuyItems.Add(new VoucherBuyItem()
                {
                    Amount = 3,
                    claimed = false,
                    Id =
                        "lnbc150n1pwr7z63pp568q60gxz5ta69v6jqxxpnlt2yg006hvkcq6pn8s5lc03pmkk3yzqdqqcqzyskp254xkgme7knwf0ygufdr7t24zz0dnctglttv8ezk0r9t4jhyh4q8eenjc0vgc59jladn50wxupua2sm624sp57cg4z9j8p3phd76qqlx6tn3",
                    SatPerVoucher = 5
                });
                _context.SaveChanges();
            }
        }

        /*
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VoucherItem>>> GetVoucherItems()
        {
            return await _context.VoucherItems.ToListAsync();
        }*/

        // GET: api/Todo/5
        [HttpGet("{token}")]
        public async Task<ActionResult<VoucherItem>> GetVoucherI(string token)
        {

            Console.WriteLine("CONTROLLERLOG: GetVoucherI " + token);
            var voucherItem = await _context.VoucherItems.FindAsync(token);

            if (voucherItem == null)
            {
                return NotFound();
            }

            return voucherItem;
        }

        [HttpGet("/api/[controller]/decode/{payreq}")]
        public async Task<ActionResult<PayReq>> DecodePayReq(string payreq)
        {

            Console.WriteLine("CONTROLLERLOG: DecodePayReq " + payreq);
            var payReq = await _lightning.DecodePayReq(payreq);
            return payReq;
        }

        [HttpGet("/api/[controller]/pay/{token}/{payreq}")]
        public async Task<ActionResult<SendResponse>> PayVoucher(string token, string payreq)
        {

            Console.WriteLine("CONTROLLERLOG: PayVoucher " + token + " " + payreq);
            var voucherItem = await _context.VoucherItems.FindAsync(token);

            if (voucherItem == null)
            {
                return NotFound();
            }
           
                

            using (var transaction = _context.Database.BeginTransaction())
            {

                var cost = await _lightning.SatCost(payreq);
                if (cost > voucherItem.StartSat - voucherItem.UsedSat)
                {

                    return new SendResponse
                    {
                        PaymentError = "not enough satoshi on voucher"
                    };
                }
                Console.WriteLine("CONTROLLERLOG: PayVoucher BEFORE PAYMENT" + token + " " + payreq);
                var res = await _lightning.SendPayment(payreq);
                Console.WriteLine("CONTROLLERLOG: PayVoucher AFTER PAYMENT" + token + " " + payreq);
                if (res.PaymentError != "")
                {

                    _context.Entry(voucherItem).State = EntityState.Unchanged;
                    return res;
                }
                Console.WriteLine("COST: " + (ulong)res.PaymentRoute.TotalAmt + " AND " +(ulong)res.PaymentRoute.TotalAmtMsat + " AND " +(ulong)(res.PaymentRoute.TotalAmtMsat / 1000));
                cost = (ulong) res.PaymentRoute.TotalAmt;
                voucherItem.UsedSat += cost;
                _context.Entry(voucherItem).State = EntityState.Modified;

                if (voucherItem.UsedSat >= voucherItem.StartSat)
                {

                    _context.Entry(voucherItem).State = EntityState.Deleted;
                }
                await _context.SaveChangesAsync();
                transaction.Commit();
                
                return res;
            }
        }

        [HttpGet("/api/[controller]/buy/{amt}/{satPerVoucher}")]
        public async Task<ActionResult<Invoice>> GetVoucherInvoice(uint amt, ulong satPerVoucher)
        {

            Console.WriteLine("CONTROLLERLOG: GetVoucherInvoice " + amt + " " + satPerVoucher);
            if (amt > _lightning.getMaxAmt() || satPerVoucher > _lightning.getMaxSat())
                return new Invoice {PaymentRequest = "ERROR: Voucheramount is capped at "+_lightning.getMaxAmt()+" and satoshi per voucher is capped at " + _lightning.getMaxSat()};
            var payReq = await _lightning.GetPayReq(amt * satPerVoucher);
            var buyItem = _context.VoucherBuyItems.Add(new VoucherBuyItem() { Id = payReq.PaymentRequest, Amount = amt, SatPerVoucher = satPerVoucher, claimed = false}).Entity;
            _context.SaveChanges();
            return payReq;
        }
        [HttpGet("/api/[controller]/buy/fee/")]
        public ActionResult<FeeResponse> GetFee()
        {
            Console.WriteLine("CONTROLLERLOG: GetFee ");

            return new FeeResponse {fee = _lightning.getFee()};
        }

        [HttpGet("/api/[controller]/claim/{payreq}")]
        public async Task<ActionResult<ClaimVoucherResponse>> ClaimVoucherInvoice(string payreq)
        {

            Console.WriteLine("CONTROLLERLOG: ClaimVoucherInvoice " + payreq);
            var voucherBuyItem = await _context.VoucherBuyItems.FindAsync(payreq);

            var response = new ClaimVoucherResponse();
            if (voucherBuyItem == null)
            {
                response.ErrorCode = "Payment not Found";
            }

            else if (voucherBuyItem.claimed)
            {
                var data = _context.VoucherBuyItems.Include(a => a.Vouchers).FirstOrDefault(u => u.Id == payreq);
                foreach (var voucherItem in data.Vouchers)
                {
                    response.ErrorCode = "Ok";
                    response.Vouchers.Add(voucherItem);
                }
            } else
            {
                if (await _lightning.ValidatePayment(payreq, ""))
                {
                    response.ErrorCode = "Ok";
                    for (int i = 0; i < voucherBuyItem.Amount; i++)
                    {
                        var voucher = _context.VoucherItems
                            .Add(new VoucherItem() {StartSat = voucherBuyItem.SatPerVoucher}).Entity;
                        voucherBuyItem.Vouchers.Add(voucher);
                        response.Vouchers.Add(voucher);

                    }

                    voucherBuyItem.claimed = true;
                    _context.Entry(voucherBuyItem).State = EntityState.Modified;
                    
                    _context.SaveChanges();
                }
                else
                {
                    response.ErrorCode = "Payment not valid";

                }

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

public class FeeResponse
{
    public uint fee { get; set; }
}
