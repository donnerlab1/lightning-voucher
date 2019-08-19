using Google.Protobuf;
using Grpc.Core;
using LightningVoucher.Models;
using Lnrpc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters.Xml;
using Prometheus;
using System.Linq;

namespace LightningVoucher.ln
{
    public class LndGrpcService : ILightning
    {
        public Lightning.LightningClient client;

        public static uint feePercentage;
        public static ulong maxSatPerPayment;
        public static uint maxVouchers;
        
        public static readonly Counter PaymentsSent = Metrics.CreateCounter("payments_sent","number of payments sent.");
        public static readonly Counter PaymentErrors = Metrics.CreateCounter("payments_errors", "number of payment not sent because of errors.");
        public static readonly Counter SatoshiReceived = Metrics.CreateCounter("satoshi_received", "number of satoshis received. (in msat)");
        public static readonly Counter SatoshiSent = Metrics.CreateCounter("satoshi_sent", "number of payments sent. (in msat)");
        public static readonly Counter FeeEarned = Metrics.CreateCounter("fee_earned", "fees earned. (in sat)");
        private GetInfoResponse getInfo;

        public LndGrpcService(IConfiguration config)
        {
            
            /*
            var certLoc = config.GetValue<string>("cert");
            var macLoc = config.GetValue<string>("mac");
            ;

            var directory = Path.GetFullPath(certLoc);
            Console.WriteLine(rpc + " stuff " + directory);*/
            var directory = Environment.CurrentDirectory;
            var tls = File.ReadAllText(directory + "/tls.cert");
            var hexMac = Util.ToHex(File.ReadAllBytes(directory + "/admin.macaroon"));
            var rpc = config.GetValue<string>("rpc");
            feePercentage = config.GetValue<uint>("fee");
            maxSatPerPayment = config.GetValue<uint>("max_sat");
            maxVouchers = config.GetValue<uint>("max_voucher");
            var macaroonCallCredentials = new MacaroonCallCredentials(hexMac);
            var channelCreds = ChannelCredentials.Create(new SslCredentials(tls), macaroonCallCredentials.credentials);
            var lndChannel = new Grpc.Core.Channel(rpc, channelCreds);

            client = new Lightning.LightningClient(lndChannel);
            getInfo = client.GetInfo(new GetInfoRequest());
            Console.WriteLine(getInfo.ToString());

        }
        public async Task<SendResponse> SendPayment(string payreq)
        {

            Console.WriteLine("LIGHTNINGLOG: SendPayment " + payreq);
            var payment = await DecodePayReq(payreq);
            if (payment.NumSatoshis > (long) maxSatPerPayment)
            {

                return new SendResponse
                {
                    PaymentError = "Error: too big of a payment"
                };
            }

            var s = await client.SendPaymentSyncAsync(new SendRequest {PaymentRequest = payreq});
            if (s.PaymentError == "")
            {
                PaymentsSent.Inc();
                SatoshiSent.Inc(s.PaymentRoute.TotalAmtMsat);
            }
            else
                PaymentErrors.Inc();
            return s;
        }

        public async Task<PayReq> DecodePayReq(string payreq)
        {
            Console.WriteLine("LIGHTNINGLOG: DecodePayReq " + payreq);
            var s = await client.DecodePayReqAsync(new PayReqString
            {
                PayReq = payreq
            });
            return s;
        }

        public uint getFeePercentage()
        {
            Console.WriteLine("LIGHTNINGLOG: GetFee ");
            return feePercentage;
        }

       
        public async Task<Invoice> GetPayReq(ulong amt)
        {
            Console.WriteLine("LIGHTNINGLOG: GetPayReq " + amt);
            
            long fee = (long) (amt * (feePercentage/100f));
            if (fee < 1 && feePercentage != 0)
                fee = 1;
            FeeEarned.Inc(fee);
            var payreq = await client.AddInvoiceAsync(new Invoice
            {
                Value = (long)amt+fee
            });
            var res = await client.LookupInvoiceAsync(new PaymentHash
            {
                RHash = payreq.RHash
            });
            return res;
        }

        public async Task<bool> ValidatePayment(string payreq, string preimage)
        {
            Console.WriteLine("LIGHTNINGLOG: ValidatePayment " + payreq + " " + preimage);
            var payReq = await client.DecodePayReqAsync(new PayReqString
            {
                PayReq = payreq
            });
            var lookup = await client.LookupInvoiceAsync(new PaymentHash
            {
                RHashStr = payReq.PaymentHash
            });
            if (lookup.Settled)
            {
                Console.WriteLine("invoice settled");
                SatoshiReceived.Inc(lookup.AmtPaidMsat);
                return true;
            }

            return false;
        }

        public async Task<long> SatCost(string payreq)
        {
            Console.WriteLine("LIGHTNINGLOG: SatCost " + payreq);
            var decode = await client.DecodePayReqAsync(new PayReqString
            {
                PayReq = payreq
            });

            return  decode.NumSatoshis;
        }


        public bool VerifyMessage(string pubkey, string message)
        {
            Console.WriteLine("LIGHTNINGLOG: VerifyMessage " + pubkey + " " + message);

            var response = client.VerifyMessage(new Lnrpc.VerifyMessageRequest
            {
                Msg = ByteString.CopyFromUtf8(pubkey),
                Signature = message
            });
            if (string.Equals(response.Pubkey, pubkey))
                return true;
            else
                return false;
        }

        public Task<GetInfoResponse> GetInfo()
        {

            Console.WriteLine("LIGHTNINGLOG: GetInfo ");
            return Task.FromResult(this.getInfo);
        }

        public ulong getMaxSat()
        {
            return maxSatPerPayment;
        }

        public uint getMaxAmt()
        {
            return maxVouchers;
        }

        public async Task<long> getSatFee(string payreq)
        {
            Console.WriteLine("LIGHTNINGLOG: GETSATFEE " + payreq);
            var decode = await client.DecodePayReqAsync(new PayReqString
            {
                PayReq = payreq
            });
            QueryRoutesResponse cost;
            try
            {
                cost = await client.QueryRoutesAsync(new QueryRoutesRequest { Amt = decode.NumSatoshis, PubKey = decode.Destination, UseMissionControl = true });
            } catch(RpcException e)
            {
                Console.WriteLine(e);
                return 10;
            }
            if (cost == null)
                return 10;
            long highestCost = -1;
            foreach(var route in cost.Routes)
            {
                
                if (route.TotalFeesMsat > highestCost)
                    highestCost = route.TotalFeesMsat;
            }
            if (highestCost == -1)
                return -1;
            return (highestCost / 1000);
        }
        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
    }
    public static class Util
    {
        public static bool CheckPreImage(string pHash, string preImage)
        {
            var calcPhash = ComputeSha256Hash(StringToByteArrayFastest(preImage));
            if (string.Equals(calcPhash, pHash))
                return true;
            return false;
        }
        public static string ToHex(byte[] input)
        {
            return BitConverter.ToString(input).Replace("-", string.Empty);
        }


        public static string ComputeSha256Hash(byte[] rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(rawData);
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        public static byte[] StringToByteArrayFastest(string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }

        public static int GetHexVal(char hex)
        {
            int val = (int)hex;
            //For uppercase A-F letters:
            //return val - (val < 58 ? 48 : 55);
            //For lowercase a-f letters:
            //return val - (val < 58 ? 48 : 87);
            //Or the two combined, but a bit slower:
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }

        
    }
}
