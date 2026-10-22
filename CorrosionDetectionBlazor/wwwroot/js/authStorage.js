window.authStorage = {
    setToken: function (token) { localStorage.setItem('authToken', token); },
    getToken: function () { return localStorage.getItem('authToken'); },
    removeToken: function () { localStorage.removeItem('authToken'); }
};

window.downloadFileFromBase64 = function (fileName, contentType, base64Data) {
    const linkSource = `data:${contentType};base64,${base64Data}`;
    const downloadLink = document.createElement("a");
    downloadLink.href = linkSource;
    downloadLink.download = fileName;
    downloadLink.click();
};
