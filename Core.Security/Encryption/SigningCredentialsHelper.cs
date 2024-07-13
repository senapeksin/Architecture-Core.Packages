using Microsoft.IdentityModel.Tokens;

namespace Core.Security.Encryption;


// parametre : security anahtarı ve kullanacağımız algoritma olacak
// jwt yi imzlayacağımız nesneyi oluşturup döndürüyor olacağız
public static class SigningCredentialsHelper
{
    public static SigningCredentials CreateSigningCredentials(SecurityKey securityKey) => new(securityKey, SecurityAlgorithms.HmacSha512Signature);
}
