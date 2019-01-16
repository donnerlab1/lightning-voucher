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
        Task<long> SatCost(string payreq);
        Task<Invoice> GetPayReq(long amt);
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

        public Task<long> SatCost(string payreq)
        {
            return Task.FromResult(1L);
        }

        public Task<Invoice> GetPayReq(long amt)
        {
            counter += 1;
            return Task.FromResult(new Invoice{PaymentRequest = "payreq"+counter, Value = amt} );
        }
    }
}
