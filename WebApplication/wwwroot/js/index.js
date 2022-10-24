import DesktopAgent from '/js/desktopAgent.js';

export default class Index {
    constructor() {
        this.selectedFile = null;
        this.fileRef = null;

        var _t = this;
        var f = document.getElementById("txtFile");
        f.onchange = _t.fileSelected.bind(_t);

        var b = document.getElementById("btnSign");
        b.onclick = _t.signFile.bind(_t);
    }

    async fileSelected(e) {
        if (e.target.files && e.target.files.length)
            this.selectedFile = e.target.files[0];
        else
            this.selectedFile = null;

        if (this.selectedFile) {
            var uploadResult = await this.uploadFile();
            if (uploadResult.Result) {
                document.getElementById("btnSign").removeAttribute("disabled");
                this.fileRef = uploadResult.Contents;
                this.logState(`File ${this.fileRef.OriginalFile} uploaded successfully.`);
            } else {
                alert(uploadResult.Message);
            }
        }
        else
            document.getElementById("btnSign").setAttribute("disabled", "disabled");
    }

    async uploadFile() {
        var fd = new FormData();
        fd.append(`file`, this.selectedFile);

        var url = "/index/uploadfile";
        var ret;
        var response;
        var options = {
            method: 'POST',
            headers: {},
            body: fd
        };

        try {
            response = await fetch(url, options);
        } catch (e) {
            ret = {
                Result: false,
                Message: "Fetch failed"
            };
            return ret;
        }

        ret = await response.json();
        return ret;
    }

    async signFile() {
        if (!this.fileRef)
            return;

        /*
         this.fileRef structure:
         this.fileRef.File = name of the file given by backend (temporary)
         this.fileRef.OriginalFile = name of the original file uploaded by the user
         */
        try {
            this.setUIState(false);
            var agent = new DesktopAgent();
            this.logState("Checking agent status...");
            var response = await agent.checkAgent(false);
            if (!response || !response.Result)
                throw "The desktop module is not active!";

            this.logState("Requesting a digital ID...");
            const selectIdResponse = await agent.selectDigitalId();
            if (!selectIdResponse.Result)
                throw selectIdResponse.Message;

            this.logState("Selected digital ID: " + selectIdResponse.CertificateFriendlyName + ". Preparing file now...");

            var options = {
                method: 'POST',
                headers: {},
                body: JSON.stringify({
                    CertificateEncodedBase64: selectIdResponse.CertificateEncodedBase64,
                    NameInSignature: document.getElementById("txtNameInSignature").value,
                    File: this.fileRef.File,
                    OriginalFile: this.fileRef.OriginalFile
                })
            };
            options.headers['Content-Type'] = 'application/json';
            var prepareResponse = await fetch(`/index/preparefile`, options);
            prepareResponse = await parseFetchResponse(prepareResponse);
            if (!prepareResponse.Result) {
                if (prepareResponse.Message)
                    this.logState(prepareResponse.Message);
                return;
            }
            /*  prepareResponse structure:
                prepareResponse.Result
                prepareResponse.Contents.FileHashesToSign
                prepareResponse.Contents.FileHashesToSign[n].FileHash = pdfBytesToSign;
                prepareResponse.Contents.FileHashesToSign[n].OriginalFile = name of the file before preparation;
                prepareResponse.Contents.FileHashesToSign[n].PreparedFile = the file after preparation;
                prepareResponse.Contents.CertificateThumbprint
             */

            this.logState("File prepared successfully");

            const signResponse = await agent.signHashes(prepareResponse.Contents);
            if (!signResponse.Result) {
                if (signResponse.Message)
                    this.logState(signResponse.Message);
                return;
            }
            /*
                signResponse structure: 
                signResponse.Result
                signResponse.SignedHashes
                signResponse.SignedHashes[n].OriginalFile
                signResponse.SignedHashes[n].PreparedFile
                signResponse.SignedHashes[n].FileHash // string base64
                signResponse.SignedHashes[n].SignedFileHash // string base64
            */

            // Send the signed hashes to backend to complete the external signature process
            this.logState("Completing the signing process...");
            options = {
                method: 'POST',
                headers: {},
                body: JSON.stringify({
                    CertificateEncodedBase64: selectIdResponse.CertificateEncodedBase64,
                    SignedFileHashes: signResponse.SignedHashes
                })
            };
            options.headers['Content-Type'] = 'application/json';
            const completeResponse = await fetch(`/index/completelocalsigning`, options);

            // trigger a download
            this.logState("Downloading the signed file...");
            var blob = await completeResponse.blob();
            var url = window.URL.createObjectURL(blob);
            var a = document.createElement('a');
            a.href = url;
            var cd = completeResponse.headers.get("content-disposition");
            if (cd) {
                cd = cd.split("filename=")[1].split(";")[0];
                if (cd.startsWith("\"") || cd.startsWith("'"))
                    cd = cd.substr(1);
                if (cd.endsWith("\"") || cd.endsWith("'"))
                    cd = cd.substr(0, cd.length - 1);
                a.download = cd;
            }
            document.body.appendChild(a); // we need to append the element to the dom -> otherwise it will not work in firefox
            a.click();
            a.remove();  //afterwards we remove the element again 
        } catch (e) {
            var m;
            if (typeof e === 'string')
                m = e;
            else if (e.Message)
                m = e.Message;
            else if (e.message)
                m = e.message;
            this.logState(m);
        } finally {
            this.setUIState(true);
        }
    }

    logState(state) {
        var c = document.getElementById("txtLog");
        var s = c.value;
        if (s)
            s += "\r\n";
        s += state;
        c.value = s;
    }

    setUIState(value) {
        if (value) {
            document.getElementById("txtFile").removeAttribute("disabled");
            document.getElementById("btnSign").removeAttribute("disabled");
        } else {
            document.getElementById("txtFile").setAttribute("disabled", "disabled");
            document.getElementById("btnSign").setAttribute("disabled", "disabled");
        }
    }
}
