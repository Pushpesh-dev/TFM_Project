using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using tfm_web.Models;

namespace tfm_web.Services
{
    public class JwtToken
    {
        public readonly IConfiguration _Config;

        public JwtToken(IConfiguration configService)
        {
            _Config = configService;
        }
        public string GenerateJSONWebToken(int Id , String Email, String RoleName, int RoleId)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_Config["JwtString:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha512);
            //string roleName = RoleId == 1 ? "Admin" : "Employee";

            var claims = new[]
            {
                 new Claim("Id",Id.ToString()),
                 new Claim("Email",Email.ToString()),
                  new Claim("roleId", RoleId.ToString()),
                 new Claim("RoleName",RoleName.ToString()),
                new Claim(ClaimTypes.Role,RoleName )
            };
            var token = new JwtSecurityToken(
                 issuer: _Config["JwtString:Issuer"],
                 audience: _Config["JwtString:Audience"],
                 claims: claims,
                 expires: DateTime.UtcNow.AddMinutes(15),
                 signingCredentials: credentials
             );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
   


}
