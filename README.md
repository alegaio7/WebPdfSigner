# Web-signing PDF files with client certificates
## A module that allows web applications sign PDF files using locally installed certificates

A desktop module that runs in the background and allows a browser to digitally sign a PDF file uploaded by the user, using her own certificates installed in the Windows Certificates Store.

## Features

- Use any locally installed certificate (digital or hardware tokens) to sign pdf files uploaded with the browser.
- Sends only hashed for signing, instead of complete files, to boost performance and protect PDFs from traveling between endpoints.
- Signed files are automatically downloaded from the web page.

## Components
The solution is made up of 3 projects:
- The windows application desktop module (WinForms)
- The web application: handles file uploads and performs the signature preparation (hash calculation) and completion (embedding signed hash into the prepared file).
- An installer project to generate the module setup (and .msi installer)

## Software used

The desktop module solution uses the following software stack:

- [Visual Studio 2022] - For building the complete solution
- [Syncfusion PDF .Net Core] - Syncfusion.Pdf.Net.Core for preparing the PDF files for external signatures
- [Wix toolset] - For building the .msi installer
- [OpenSSL] - Optionally, if you want to rebuild the SSL certificates.

## Installation

Build the solution in release mode and run the generated .msi installer.

The setup package installs the desktop module and configures it to autorun after Windows login.

The setup also installs a couple of certificates needed for SSL communication between the desktop module and the page from the web project.

After the module is installed and running, launch the web project, upload a PDF and click the "Sign file" button. Select a certificate from the list and that's it, wait for the signed PDF file is prepared for downloading.

## Blog post
There's a blog post detailing all the stuff about this project, from its internal workings to the build and installation procedures. You can find it at https://www.alexgaio.com/post/signing-pdf-files-from-a-web-application

## License

MIT

[Visual Studio 2022]: <https://visualstudio.microsoft.com/downloads/>
[Syncfusion PDF .Net Core]: <https://www.syncfusion.com/document-processing/pdf-framework/net-core>
[Wix toolset]: <https://wixtoolset.org/releases/>
[OpenSSL]: <https://wiki.openssl.org/index.php/Binaries>