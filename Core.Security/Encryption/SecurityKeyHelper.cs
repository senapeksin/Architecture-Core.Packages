using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Core.Security.Encryption;

// bize verilen security anahtarını security key e dönüştüren extension olacak.
public static class SecurityKeyHelper
{
    public static SecurityKey CreateSecurityKey(string securityKey) => new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey));


}
