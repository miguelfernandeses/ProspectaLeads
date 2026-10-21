window.prospectaiJs = {
    downloadFromBase64: function (fileName, contentType, base64Data) {
        const linkSource = `data:${contentType};base64,${base64Data}`;
        const downloadLink = document.createElement("a");
        downloadLink.href = linkSource;
        downloadLink.download = fileName;
        document.body.appendChild(downloadLink);
        downloadLink.click();
        document.body.removeChild(downloadLink);
    },

    copyToClipboard: function (text) {
        if (navigator.clipboard && window.isSecureContext) {
            navigator.clipboard.writeText(text);
        } else {
            const textArea = document.createElement("textarea");
            textArea.value = text;
            textArea.style.position = "fixed";
            textArea.style.left = "-999999px";
            textArea.style.top = "-999999px";
            document.body.appendChild(textArea);
            textArea.focus();
            textArea.select();
            document.execCommand('copy');
            textArea.remove();
        }
    },

    getAuthCallbackParameters: function () {
        const hash = new URLSearchParams(window.location.hash.replace(/^#/, ""));
        const query = new URLSearchParams(window.location.search);
        const read = (name) => hash.get(name) || query.get(name);

        return {
            accessToken: read("access_token"),
            refreshToken: read("refresh_token"),
            expiresIn: Number.parseInt(read("expires_in") || "", 10) || null,
            error: read("error"),
            errorDescription: read("error_description")
        };
    },

    clearAuthCallbackParameters: function () {
        window.history.replaceState({}, document.title, window.location.pathname);
    }
};
