using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using Kenergie.Controllers;
using Kenergie.Models;
using Kenergie.Models.Enums;
using Kenergie.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Kenergie.Tests
{
    /// <summary>
    /// Audit : TechnicienDashboard exige le claim Role JWT (= "Technicien"),
    /// pas des permissions dans le token.
    /// </summary>
    public class TechnicienDashboardJwtAuthTests
    {
        private const string SecretKey = "Kenergie-SecretKey-2025-V1-Ultra-Secure-Key-For-JWT-Token-Generation";

        [Fact]
        public void TechnicienDashboardController_RequiresOnlyTechnicienOrSuperAdminRole()
        {
            var attr = typeof(TechnicienDashboardController)
                .GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(attr);
            Assert.Equal("Technicien,Super-Admin", attr!.Roles);
            Assert.True(string.IsNullOrEmpty(attr.Policy));
        }

        [Fact]
        public void RoleSeedName_MatchesAuthorizeAndUserRolesConstant()
        {
            Assert.Equal("Technicien", UserRoles.TECHNICIEN);
        }

        [Fact]
        public void GenerateToken_WithActiveUserRoleTechnicien_IsAcceptedByProgramCsStyleValidation()
        {
            var jwt = CreateJwtService();
            var token = jwt.GenerateToken(CreateTechnicienWithUserRoles(statutActif: true));

            // Décodage brut (sans remap) pour inspecter le payload
            var raw = new JwtSecurityTokenHandler().ReadJwtToken(token);
            Assert.Contains(raw.Claims, c => c.Value == "Technicien" &&
                (c.Type == ClaimTypes.Role || c.Type == "role"));

            var principal = ValidateExactlyLikeProgramCs(token);

            Assert.True(
                principal.IsInRole("Technicien"),
                "JWT Technicien doit passer [Authorize(Roles=Technicien)] avec la config Program.cs. " +
                $"Claims: {FormatClaims(principal)}");

            Assert.DoesNotContain(principal.Claims, c =>
                c.Type.Contains("permission", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("Technicien", principal.FindFirst("primaryRole")?.Value);
        }

        [Fact]
        public void GenerateToken_WithLegacyRoleOnly_ProducesTechnicienRoleClaim()
        {
            var jwt = CreateJwtService();
            var utilisateur = new Utilisateur
            {
                IdUtilisateur = 42,
                Email = "tech@test.local",
                NomComplet = "Tech Legacy",
                IdRole = 6,
                IdSociete = 1,
                UserRoles = null,
                Role = new Role { IdRole = 6, Nom = "Technicien", Niveau = 6, Statut = true }
            };

            var principal = ValidateExactlyLikeProgramCs(jwt.GenerateToken(utilisateur));
            Assert.True(principal.IsInRole("Technicien"), FormatClaims(principal));
        }

        [Fact]
        public void GenerateToken_InactiveUserRoleWithoutLegacyRole_DoesNotGrantTechnicien()
        {
            var jwt = CreateJwtService();
            var utilisateur = CreateTechnicienWithUserRoles(statutActif: false);
            utilisateur.Role = null;
            utilisateur.IdRole = 0;

            var principal = ValidateExactlyLikeProgramCs(jwt.GenerateToken(utilisateur));
            Assert.False(principal.IsInRole("Technicien"), FormatClaims(principal));
        }

        [Fact]
        public void GenerateToken_ClientRole_IsDeniedTechnicienDashboard()
        {
            var jwt = CreateJwtService();
            var utilisateur = new Utilisateur
            {
                IdUtilisateur = 7,
                Email = "client@test.local",
                NomComplet = "Client",
                IdRole = 7,
                UserRoles = new List<UserRole>
                {
                    new UserRole
                    {
                        IdUtilisateur = 7,
                        IdRole = 7,
                        Statut = true,
                        Role = new Role { IdRole = 7, Nom = "Client", Niveau = 7, Statut = true }
                    }
                }
            };

            var principal = ValidateExactlyLikeProgramCs(jwt.GenerateToken(utilisateur));
            Assert.False(principal.IsInRole("Technicien"));
            Assert.True(principal.IsInRole("Client"), FormatClaims(principal));
        }

        [Fact]
        public void CurrentUserService_ReadsTechnicienRoleFromValidatedJwt()
        {
            var jwt = CreateJwtService();
            var principal = ValidateExactlyLikeProgramCs(
                jwt.GenerateToken(CreateTechnicienWithUserRoles(statutActif: true)));

            var accessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
            var currentUser = new CurrentUserService(accessor);

            Assert.Equal("Technicien", currentUser.GetUserRole());
            Assert.Equal("Technicien", currentUser.PrimaryRole);
        }

        private static SimpleJwtService CreateJwtService()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:SecretKey"] = SecretKey,
                    ["Jwt:Issuer"] = "Kenergie",
                    ["Jwt:Audience"] = "KenergieUsers",
                    ["Jwt:ExpirationMinutes"] = "60"
                })
                .Build();
            return new SimpleJwtService(config);
        }

        private static Utilisateur CreateTechnicienWithUserRoles(bool statutActif)
        {
            var role = new Role { IdRole = 6, Nom = "Technicien", Niveau = 6, Statut = true };
            return new Utilisateur
            {
                IdUtilisateur = 100,
                Email = "technicien@test.local",
                NomComplet = "Technicien Test",
                IdRole = 6,
                IdSociete = 1,
                Role = role,
                UserRoles = new List<UserRole>
                {
                    new UserRole
                    {
                        IdUtilisateur = 100,
                        IdRole = 6,
                        Statut = statutActif,
                        Role = role
                    }
                }
            };
        }

        /// <summary>Même TokenValidationParameters que Program.cs (pas de RoleClaimType custom).</summary>
        private static ClaimsPrincipal ValidateExactlyLikeProgramCs(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
            return handler.ValidateToken(token, parameters, out _);
        }

        private static string FormatClaims(ClaimsPrincipal principal) =>
            string.Join("; ", principal.Claims.Select(c => $"{c.Type}={c.Value}"));
    }
}
