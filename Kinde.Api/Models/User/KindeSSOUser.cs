using Kinde.Api.Models.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlTypes;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kinde.Api.Models.User
{
    public class KindeSSOUser
    {

        protected OauthToken TokenSorce { get; set; }
        public JwtSecurityToken AccessToken { get; set; }
        public JwtSecurityToken IdToken { get; set; }
        public static KindeSSOUser FromToken(OauthToken token)
        {
            var handler = new JwtSecurityTokenHandler();

            var user = new KindeSSOUser();
            if (!string.IsNullOrEmpty(token.AccessToken))
            {
                user.AccessToken = handler.ReadJwtToken(token.AccessToken);
            }
            if (!string.IsNullOrEmpty(token.IdToken))
            {
                user.IdToken = handler.ReadJwtToken(token.IdToken);
            }


            user.TokenSorce = token;
            return user;

        }
        public KindeSSOUser()
        {

        }
        public bool IsAuthorized { get { return !TokenSorce?.IsExpired ?? false; } }
        public string Id { get { return IdToken.Payload["sub"]?.ToString() ?? AccessToken.Payload["sub"]?.ToString(); } }
        public string GivenName { get { return IdToken.Payload["given_name"]?.ToString() ?? AccessToken.Payload["given_name"]?.ToString(); } }
        public string FamilyName { get { return IdToken.Payload["family_name"]?.ToString() ?? AccessToken.Payload["family_name"]?.ToString(); } }
        public string Email { get { return IdToken.Payload["email"]?.ToString() ?? AccessToken.Payload["email"]?.ToString(); } }

        public string? GetOrganisation()
        {
            return GetClaim<string>("org_code");
        }
        public string[]? GetOrganisations()
        {
            return GetClaim<string[]>("org_codes");
        }
        public OrganisationPermissionsCollection GetPermissions()
        {
            return new OrganisationPermissionsCollection()
            {
                Permissions = GetClaim<string[]>("permissions")?.ToList() ?? new List<string>(),
                OrganisationId = GetOrganisation()

            };

        }
        public OrganisationPermission GetPermission(string key)
        {
            if (GetClaim<string[]>("permissions").Any(x => x == key))
            {
                return new OrganisationPermission() { Id = key, OrganisationId = GetOrganisation(), Granted = true };
            }
            return new OrganisationPermission() { Id = key, OrganisationId = GetOrganisation(), Granted = false };
        }
        public object? GetClaim(string key)
        {
            return GetClaim<object>(key);
        }
        protected T? GetClaim<T>(string key)
        {
            return ((T)AccessToken.Payload[key]) ?? ((T)IdToken.Payload[key]);
        }

        public class OrganisationPermission
        {
            public string Id { get; set; }
            public bool Granted { get; set; }
            public string? OrganisationId { get; set; }

        }
        public class OrganisationPermissionsCollection
        {
            public string OrganisationId { get; set; }
            public List<string> Permissions { get; set; }


        }
    }
}
