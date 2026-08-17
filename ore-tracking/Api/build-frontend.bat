@echo off
cd /d "%~dp0..\Web"
if not exist node_modules (
  npm install
)
npx ng build --configuration production
