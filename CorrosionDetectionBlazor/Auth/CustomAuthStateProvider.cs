using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace CorrosionDetectionBlazor.Auth
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly IJSRuntime _js;
        private readonly AuthenticationState _anonymous;

        public CustomAuthStateProvider(IJSRuntime js)
        {
            _js = js;
            _anonymous = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            string? token;
            try
            {
                token = await _js.InvokeAsync<string>("authStorage.getToken");
            }
            catch
            {
                return _anonymous;
            }

            if (string.IsNullOrEmpty(token))
                return _anonymous;

            var claims = ParseClaimsFromJwt(token);
            var identity = new ClaimsIdentity(claims, "jwt");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }

        public async Task MarkUserAsAuthenticated(string token)
        {
            await _js.InvokeVoidAsync("authStorage.setToken", token);
            var claims = ParseClaimsFromJwt(token);
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        public async Task MarkUserAsLoggedOut()
        {
            await _js.InvokeVoidAsync("authStorage.removeToken");
            NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));
        }

        private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var kvPairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes)!;

            var claims = new List<Claim>();
            foreach (var kv in kvPairs)
            {
                if (kv.Value is JsonElement { ValueKind: JsonValueKind.Array } arrEl)
                {
                    foreach (var item in arrEl.EnumerateArray())
                        claims.Add(new Claim(kv.Key, item.ToString()));
                }
                else
                {
                    claims.Add(new Claim(kv.Key, kv.Value.ToString()!));
                }
            }
            return claims;
        }

        private static byte[] ParseBase64WithoutPadding(string base64)
        {
            base64 = base64.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }
}