@echo off

:: Capture the true ESC character reliably without delayed expansion
for /F "delims=#" %%A in ('"prompt #$E# & for %%B in (1) do rem"') do set "ESC=%%A"
echo %ESC%[37mTesting with no header key%ESC%[31m
curl -X POST https://localhost:7155/api/Payments -H "content-type: application/json" -d "{\"OrderId\": \"300\", \"Amount\": 300}"
echo.
echo %ESC%[37mTesting with OrderId = 300 and Amount = 300 should be saved%ESC%[32m
curl -X POST https://localhost:7155/api/Payments -H "content-type: application/json" -H "X-Idempotency-Key: testkey5" -d "{\"OrderId\": \"300\", \"Amount\": 300}"
echo.
echo %ESC%[37mTesting with OrderId = 300 and Amount = 300 should return cached response%ESC%[33m
curl -X POST https://localhost:7155/api/Payments -H "content-type: application/json" -H "X-Idempotency-Key: testkey5" -d "{\"OrderId\": \"300\", \"Amount\": 300}"
echo.
echo %ESC%[37mTesting with OrderId = 300 and Amount = 302 should return error due to mismatched hashes.%ESC%[31m
curl -X POST https://localhost:7155/api/Payments -H "content-type: application/json" -H "X-Idempotency-Key: testkey5" -d "{\"OrderId\": \"300\", \"Amount\": 302}"
:: Reset back to normal
echo %ESC%[0m
