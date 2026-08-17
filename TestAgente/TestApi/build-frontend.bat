@echo off
cd /d "%~dp0..\TestFrontend"
if not exist node_modules (
  npm install
)
npx ng build --configuration production
