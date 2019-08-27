# lightning voucher

HIGHLY EXPERIMENTAL

[demo](https://donnerlab.com/voucher/)


simple voucher system for lnd. 

## usage

place tls.cert and admin.macaroon in application folder

needs .net core 2.2

`dotnet restore`

`dotnet ef migrations add InitialCreate`

`dotnet ef database update`

`dotnet run rpc="localhost:10009" fee=5 max_sat=500 max_voucher=100`
