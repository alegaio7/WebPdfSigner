@echo off
cls
REM *************************
REM ** CA CERT CREATION
REM *************************

REM Generate keys for CA
openssl genrsa 2048 > DesktopModuleCA-key.pem

REM Generate x509 certificate for CA
openssl req -new -x509 -nodes -days 10950 -key DesktopModuleCA-key.pem -out DesktopModuleCA.pem -config opensslCA.cnf

REM Generate a cer file encoded in DER, from a pem file.
openssl x509 -outform der -in DesktopModuleCA.pem -out DesktopModuleCA.cer 

REM *************************
REM ** SSL CERT CREATION
REM *************************
REM Generate CSR for the desktop module
openssl req -new -out DesktopModule.req -keyout DesktopModule-key.pem -newkey rsa:2048 -sha512 -config openssl.cnf

REM Create the SSL certificate using the CA cert and key and the request created before
openssl x509 -req -in DesktopModule.req -CAkey DesktopModuleCA-key.pem -CA DesktopModuleCA.pem -days 3650 -sha512 -out DesktopModule.cer -set_serial 12345678 -extfile v3.ext

REM Convert cert + key into pfx, for importing into windows
openssl pkcs12 -inkey DesktopModule-key.pem -in DesktopModule.cer -export -out DesktopModule.pfx

REM ********************************************
REM ** DIGITAL SIGNATURE CERTIFICATE FOR TESTING
REM ********************************************
REM Generate CSR for the desktop module
openssl req -new -out MyTestingCert.req -keyout MyTestingCert-key.pem -newkey rsa:2048 -sha512 -config opensslTESTING.cnf

REM Create the SSL certificate using the CA cert and key and the request created before
openssl x509 -req -in MyTestingCert.req -CAkey DesktopModuleCA-key.pem -CA DesktopModuleCA.pem -days 730 -sha512 -out MyTestingCert.cer -set_serial 98765432

REM Convert cert + key into pfx, for importing into windows
openssl pkcs12 -inkey MyTestingCert-key.pem -in MyTestingCert.cer -export -out MyTestingCert.pfx


pause
