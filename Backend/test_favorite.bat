@echo off
echo Testing Favorite Feature
echo ========================
echo.

REM First, login to get a token
echo Step 1: Logging in...
curl -X POST http://localhost:5235/api/auth/login ^
  -H "Content-Type: application/json" ^
  -d "{\"email\":\"user1@gmail.com\",\"password\":\"user123\"}" ^
  -o login_response.json

echo.
echo Login response saved to login_response.json
echo Please copy the token from the response and set it below:
echo.

REM TODO: Replace YOUR_TOKEN_HERE with actual token from login_response.json
set TOKEN=YOUR_TOKEN_HERE

echo.
echo Step 2: Testing authentication...
curl -X GET http://localhost:5235/api/favorite/debug/auth ^
  -H "Authorization: Bearer %TOKEN%"

echo.
echo.
echo Step 3: Toggling favorite for listing 19...
curl -X POST http://localhost:5235/api/favorite/toggle/19 ^
  -H "Authorization: Bearer %TOKEN%"

echo.
echo.
echo Done! Check the responses above.
pause
