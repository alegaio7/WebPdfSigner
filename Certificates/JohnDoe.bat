@echo off
cls
REM ********************************************
REM ** DIGITAL SIGNATURE CERTIFICATE FOR TESTING
REM ********************************************
REM Generate CSR for the desktop module
openssl req -new -out MyTestingCert.req -keyout MyTestingCert-key.pem -newkey rsa:2048 -sha512 -config opensslTESTING.cnf

REM Create the SSL certificate using the CA cert and key and the request created before
openssl x509 -req -in MyTestingCert.req -CAkey DesktopModuleCA-key.pem -CA DesktopModuleCA.pem -days 730 -sha512 -out MyTestingCert.cer -set_serial 98765432 -extfile v3_test.ext

REM Convert cert + key into pfx, for importing into windows
openssl pkcs12 -inkey MyTestingCert-key.pem -in MyTestingCert.cer -export -out MyTestingCert.pfx


pause
