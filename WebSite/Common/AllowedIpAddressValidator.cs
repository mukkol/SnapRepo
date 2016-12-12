using System;
using System.Configuration;
using System.Linq;
using System.Security;
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
            //It's possible to configure the use of HTTP_X_FORWARDED_FOR header in case of proxy server
            return GetUserHostIpAddress(request,
                useXForwardedForHeader: bool.Parse(ConfigurationManager.AppSettings["SnapRepo.UseXForwardedForHeader"] ?? "False"),
                xForwardedProxyIp: ConfigurationManager.AppSettings["SnapRepo.XForwardedProxyIp"]);
        }

        public static string GetUserHostIpAddress(HttpRequestBase request, bool useXForwardedForHeader, string xForwardedProxyIp)
        {
            if (!useXForwardedForHeader)
                return request.ServerVariables["REMOTE_ADDR"];
            if(useXForwardedForHeader && string.IsNullOrEmpty(xForwardedProxyIp))
                throw new ArgumentException("Using HTTP_X_FORWARDED_FOR header without static proxy IP is not safe.", nameof(useXForwardedForHeader));
            //Make sure that proxy server will force HTTP_X_FORWARDED_FOR header!
            var ip = request.ServerVariables["REMOTE_ADDR"];
            var forwardedIp = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (forwardedIp == null || forwardedIp.ToLower() == "unknown")
                return ip;
            if(ip != xForwardedProxyIp)
                throw new SecurityException($"Can't use HTTP_X_FORWARDED_FOR header IP with request proxy ({xForwardedProxyIp}).");
            return forwardedIp;
        }
    }
}