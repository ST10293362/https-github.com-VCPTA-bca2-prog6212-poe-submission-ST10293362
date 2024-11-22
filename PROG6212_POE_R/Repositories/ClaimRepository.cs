// Repositories/ClaimRepository.cs
using System.Collections.Generic;
using PROG6212_POE_R.Models;

namespace PROG6212_POE_R.Repositories
{
    public static class ClaimRepository
    {
        private static List<Claim> claims = new List<Claim>();
        private static int nextId = 1;

        public static void AddClaim(Claim claim)
        {
            claim.Id = nextId++;
            claims.Add(claim);
        }

        public static List<Claim> GetPendingClaims()
        {
            return claims.FindAll(c => c.Status == "Pending");
        }

        public static void ApproveClaim(int id)
        {
            var claim = claims.Find(c => c.Id == id);
            if (claim != null)
            {
                claim.Status = "Approved";
            }
        }

        public static void RejectClaim(int id)
        {
            var claim = claims.Find(c => c.Id == id);
            if (claim != null)
            {
                claim.Status = "Rejected";
            }
        }

        public static List<Claim> GetAllClaims()
        {
            return claims;
        }
    }
}