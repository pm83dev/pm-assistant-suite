@echo off
pushd "%~dp0..\frontend"
if not exist node_modules (
  call npm install
)
call npx ng build --configuration production
popd
