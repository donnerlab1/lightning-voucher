# lightning voucher

HIGHLY EXPERIMENTAL

[demo](https://donnerlab.com/voucher/)


simple voucher system for lnd. 

## usage

place tls.cert and admin macaroon in application folder

`dotnet restore`

`dotnet ef migrations add InitialCreate`

`dotnet ef database update`

`dotnet run rpc="localhost:10009"`
