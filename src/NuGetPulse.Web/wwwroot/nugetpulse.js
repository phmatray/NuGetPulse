// NuGetPulse — client-side helpers

/**
 * Trigger a file download from Blazor Server.
 * @param {string} fileName  - suggested file name
 * @param {string} mimeType  - MIME type (e.g. "text/csv")
 * @param {number[]} data    - byte array (from C# byte[])
 */
window.downloadFile = function (fileName, mimeType, data) {
    const blob = new Blob([new Uint8Array(data)], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    setTimeout(() => URL.revokeObjectURL(url), 10000);
};
