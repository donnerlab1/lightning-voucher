# lightning-voucher

## usage

place tls.cert and admin macaroon in application folder

dotnet restore

dotnet ef migrations add InitialCreate
dotnet ef database update

dotnet run rpc="localhost:10009"