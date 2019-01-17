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

namespace LightningVoucher.ln
{
    public class LndGrpcService : ILightning
    {
        public Lightning.LightningClient client;

        public static uint feePercentage;

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
            if (payment.NumSatoshis > 100)
            {

                return new SendResponse
                {
                    PaymentError = "Error: too big of a payment"
                };
            }
            var s = await client.SendPaymentSyncAsync(new SendRequest {PaymentRequest = payreq});
            
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

        public uint getFee()
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
                return true;
            }

            return false;
        }

        public async Task<ulong> SatCost(string payreq)
        {
            Console.WriteLine("LIGHTNINGLOG: SatCost " + payreq);
            var decode = await client.DecodePayReqAsync(new PayReqString
            {
                PayReq = payreq
            });
            return (ulong) decode.NumSatoshis;
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
