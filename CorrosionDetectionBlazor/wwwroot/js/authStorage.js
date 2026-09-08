window.authStorage = {
    setToken: function (token) { localStorage.setItem('authToken', token); },
    getToken: function () { return localStorage.getItem('authToken'); },
    removeToken: function () { localStorage.removeItem('authToken'); }
};