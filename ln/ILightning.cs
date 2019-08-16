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
        Task<Invoice> GetPayReq(ulong amt);
        Task<PayReq> DecodePayReq(string payreq);
        Task<GetInfoResponse> GetInfo();
        uint getFeePercentage();
        ulong getMaxSat();

        uint getMaxAmt();
        Task<long> getSatFee(string payreq);

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

        public Task<Invoice> GetPayReq(ulong amt)
        {
            counter += 1;
            return Task.FromResult(new Invoice{PaymentRequest = "payreq"+counter, Value = (long)amt} );
        }

        public Task<PayReq> DecodePayReq(string payreq)
        {
            return Task.FromResult(new PayReq { Description = "test", NumSatoshis = 100 });
        }

        public uint getFeePercentage()
        {
            return 1;
        }

        public Task<GetInfoResponse> GetInfo()
        {
            return Task.FromResult(new GetInfoResponse { Alias = "test" });
        }

        public ulong getMaxSat()
        {
            return 100;
        }

        public uint getMaxAmt()
        {
            return 100;
        }

        public Task<long> getSatFee(string payreq)
        {
            return Task.FromResult(1l);
        }

    }
}
