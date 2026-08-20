@echo off
set "JSON={\"AccountId\":\"22222222-2222-2222-2222-222222222222\",\"VendorId\":\"33333333-3333-3333-3333-33333333333b\",\"ItemId\":\"44444444-4444-4444-4444-444444444444\",\"Qty\":10}"
:: FIX: Remove the quotes entirely around %JSON%
echo %JSON% 
curl -X POST "https://localhost:7155/api/Orders" -H "content-type: application/json" -d "%JSON%"

REM Connect to SQL Server and run a query
sqlcmd -S localhost -d CommerceDb -U sa -P %PASS_CODE%  -Q "SELECT balance FROM [demo].Accounts where id='22222222-2222-2222-2222-222222222222'" -h -1 -W > result.txt

REM Read the result into a variable
set /p result=<result.txt
echo Balance:  %result% 
REM Compare the result against a value
rem if %result% GTR 100 (
    rem echo More than 100 active employees
rem ) else (
    rem echo 100 or fewer active employees
rem )
