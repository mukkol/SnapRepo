using System;
using System.Linq;
using System.Security.Principal;
using System.Web;

namespace SnapRepo.Common
{
    public static class AllowedIpAddressValidator
    {
        public static bool IsAllowed(HttpRequestBase request, IPrincipal user)
        {
            return IsAllowed(GetUserHostIpAddress(request), SettingsFactory.AllowedIpAddresses, user.Identity.IsAuthenticated);
        }
        public static bool IsAllowed(string userIp, IPrincipal user)
        {
            return IsAllowed(userIp, SettingsFactory.AllowedIpAddresses, user.Identity.IsAuthenticated);
        }

        public static bool IsAllowed(string userIp, string allowedIpAddresses, bool isAuthenticated)
        {
            if (string.IsNullOrEmpty(allowedIpAddresses) || isAuthenticated)
            {
                return true;
            }
            var allowedIPs = allowedIpAddresses.Split(new[] { ',',';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());
            return allowedIPs.Contains(userIp);
        }

        public static string GetUserHostIpAddress(HttpRequestBase request)
        {
            // Look for a proxy address first
            var ip = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            // If there is no proxy, get the standard remote address
            if (ip == null || ip.ToLower() == "unknown")
                ip = request.ServerVariables["REMOTE_ADDR"];
            return ip;
        }
    }
}