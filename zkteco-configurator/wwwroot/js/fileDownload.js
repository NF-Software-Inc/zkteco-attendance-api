// Triggers a browser-style file download using a Blob, for platforms where a native save picker is unavailable.
// This relies on standard browser download APIs (anchor + Blob URL) provided by the BlazorWebView's underlying webview.
window.fileDownload = {
    downloadFileFromBase64: function (fileName, base64Content, contentType) {
        const bytes = atob(base64Content);
        const buffer = new Uint8Array(bytes.length);

        for (let i = 0; i < bytes.length; i++) {
            buffer[i] = bytes.charCodeAt(i);
        }

        const blob = new Blob([buffer], { type: contentType || "application/octet-stream" });
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement("a");

        anchor.href = url;
        anchor.download = fileName;
        document.body.appendChild(anchor);
        anchor.click();
        document.body.removeChild(anchor);

        URL.revokeObjectURL(url);
    }
};
