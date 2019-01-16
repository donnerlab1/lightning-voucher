using LightningVoucher.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lnrpc;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LightningVoucher.ln
{
    public interface ILightning
    {
        Task<SendResponse> SendPayment(string payreq);
        Task<bool> ValidatePayment(string payreq, string preimage);
        Task<ulong> SatCost(string payreq);
        Task<Invoice> GetPayReq(ulong amt);
    }

    public class DemoLightning : ILightning

    {
        private static int counter = 0;

        public Task<SendResponse> SendPayment(string payreq)
        {
            return Task.FromResult(new SendResponse());
        }

        public Task<bool> ValidatePayment(string payreq, string preimage)
        {
            return Task.FromResult(true);
        }

        public Task<ulong> SatCost(string payreq)
        {
            return Task.FromResult(1UL);
        }

        public Task<Invoice> GetPayReq(ulong amt)
        {
            counter += 1;
            return Task.FromResult(new Invoice{PaymentRequest = "payreq"+counter, Value = (long)amt} );
        }
    }
}
