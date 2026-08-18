@echo off
set KEY=X-Idempotency-Key: order-key-v05
@echo off
:: Construct JSON with escaped internal quotes
set "JSON={\"AccountId\":\"22222222-2222-2222-2222-222222222222\",\"VendorId\":\"33333333-3333-3333-3333-33333333333b\",\"ItemId\":\"44444444-4444-4444-4444-444444444444\",\"Qty\":10}"
set "WRONGJSON={\"AccountId\":\"22222222-2222-2222-2222-222222222222\",\"VendorId\":\"33333333-3333-3333-3333-33333333333b\",\"ItemId\":\"44444444-4444-4444-4444-444444444444\",\"Qty\":12}"
:: echo  %key%
:: echo -d %json%
:: echo -d %wrongjson% 
:: Capture the true ESC character reliably without delayed expansion
for /F "delims=#" %%A in ('"prompt #$E# & for %%B in (1) do rem"') do set "ESC=%%A"

echo %ESC%[37mTesting with no header key. Should fail %ESC%[31m
curl -X POST "https://localhost:7155/api/Orders" -H "content-type: application/json" -d "%JSON%"
echo.

echo %ESC%[37mTesting with %JSON% should be saved%ESC%[32m
sqlcmd -S localhost -d CommerceDb -U sa -P %PASS_CODE%  -Q "SELECT balance FROM [demo].Accounts where id='22222222-2222-2222-2222-222222222222'" -h -1 -W > result.txt
REM Read the result into a variable
set /p result=<result.txt
echo Opening Balance:  %result% 
curl -X POST "https://localhost:7155/api/Orders" -H "content-type: application/json" -H "%KEY%" -d "%JSON%"
sqlcmd -S localhost -d CommerceDb -U sa -P %PASS_CODE%  -Q "SELECT balance FROM [demo].Accounts where id='22222222-2222-2222-2222-222222222222'" -h -1 -W > result.txt
REM Read the result into a variable
set /p result=<result.txt
echo.
echo New Balance:  %result% 
echo %ESC%[37mTesting with %JSON% - Return cached results%ESC%[33m
curl -X POST "https://localhost:7155/api/Orders" -H "content-type: application/json" -H "%KEY%" -d "%JSON%"
echo.
echo %ESC%[37mTesting with %WRONGJSON% - Should be rejected %ESC%[31m
curl -X POST "https://localhost:7155/api/Orders" -H "content-type: application/json" -H "%KEY%" -d "%JSON%"
:: Reset back to normal
echo %ESC%[0m
