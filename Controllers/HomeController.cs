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

    public class HomeController : Controller
    {
        private readonly ILightning _lightning;
        public HomeController(ILightning lightning)
        {
            this._lightning = lightning;
        }
        public async Task<ActionResult> Index(string id)
        {
            Console.Write("CONTROLLERLOG: INDEX " + id);
            var getInfo = await _lightning.GetInfo();
            var channelString = getInfo.IdentityPubkey + ":9735";
            ViewData["Channel"] = channelString;
            ViewData["Fee"] = _lightning.getFee().ToString() + "%";
            return View();
        }

        [Route("/{voucher}")]
        public ActionResult GetIndexVoucher(string voucher)
        {
            Console.Write("CONTROLLERLOG: HOMEVOUCHER " + voucher);
            ViewData["VoucherId"] = voucher;
            return View("Voucher");
        }

    }

}