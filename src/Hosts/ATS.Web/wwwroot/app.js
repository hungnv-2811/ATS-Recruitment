// Ham JS toi thieu ma Blazor goi qua IJSRuntime. Khong framework, khong build step.
window.ats = {
    // Tai file (CV goc) da doc qua API bang JWT — trinh duyet khong tu gan header Authorization
    // cho the <a>, nen Web doc bytes roi tao Blob tai cho.
    downloadFile: function (fileName, contentType, base64) {
        const bytes = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
        const url = URL.createObjectURL(new Blob([bytes], { type: contentType }));
        const a = document.createElement("a");
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        a.remove();
        setTimeout(() => URL.revokeObjectURL(url), 10000);
    },

    // Xem truoc PDF trong iframe (HR-3). Tra ve object URL de huy khi roi trang.
    previewPdf: function (iframeId, base64) {
        const bytes = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
        const url = URL.createObjectURL(new Blob([bytes], { type: "application/pdf" }));
        const frame = document.getElementById(iframeId);
        if (frame) { frame.src = url; }
        return url;
    },

    revoke: function (url) {
        if (url) { URL.revokeObjectURL(url); }
    }
};
