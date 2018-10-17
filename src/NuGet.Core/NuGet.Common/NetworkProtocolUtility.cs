// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Reflection;

namespace NuGet.Common
{
    public static class NetworkProtocolUtility
    {
#if IS_CORECLR
        private static readonly AssemblyName _servicePointAssemblyName = new AssemblyName() { Name = "System.Net.ServicePoint" };
        private static readonly string _servicePointManagerTypeName = "System.Net.ServicePointManager";
        private static readonly string _securityProtocolTypeTypeName = "System.Net.SecurityProtocolType";
#endif

        /// <summary>
        /// Configure SSL protocols.
        /// </summary>
        public static void ConfigureSupportedSslProtocols(Common.ILogger log)
        {
            var newValue = "<unknown>";

#if IS_CORECLR
            if (TryGetAssembly(_servicePointAssemblyName, out var assembly)
                && TryGetType(assembly, _servicePointManagerTypeName, out var servicePointManager)
                && TryGetType(assembly, _securityProtocolTypeTypeName, out var securityProtocolType))
            {
                var securityProtocol = servicePointManager.GetRuntimeProperty("SecurityProtocol");

                if (securityProtocol != null)
                {
                    var tls = securityProtocolType.GetRuntimeField("Tls");
                    var tls11 = securityProtocolType.GetRuntimeField("Tls11");
                    var tls12 = securityProtocolType.GetRuntimeField("Tls12");

                    var intValue = (int)tls.GetValue(obj: securityProtocolType)
                        + (int)tls11.GetValue(obj: securityProtocolType)
                        + (int)tls12.GetValue(obj: securityProtocolType);

                    var enumValue = Enum.ToObject(securityProtocolType, intValue);

                    securityProtocol.SetValue(obj: servicePointManager, value: enumValue);

                    newValue = securityProtocol.GetValue(obj: servicePointManager).ToString();
                }
            }
#else
            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls |
                SecurityProtocolType.Tls11 |
                SecurityProtocolType.Tls12;

            newValue = ServicePointManager.SecurityProtocol.ToString();
#endif

            log.LogDebug($"ServicePointManager.SecurityProtocol:  {newValue}");
        }

        /// <summary>
        /// Set ServicePointManager.DefaultConnectionLimit
        /// </summary>
        public static void SetConnectionLimit(Common.ILogger log)
        {
            var limit = Environment.GetEnvironmentVariable("NUGET_DEFAULT_CONNECTION_LIMIT");
            int? connectionLimit = null;

            if (int.TryParse(limit, out var value))
            {
                connectionLimit = value;
            }

            var newValue = "<unknown>";

#if IS_CORECLR
            if (TryGetAssembly(_servicePointAssemblyName, out var assembly)
                && TryGetType(assembly, _servicePointManagerTypeName, out var servicePointManager))
            {
                var defaultConnectionLimit = servicePointManager.GetRuntimeProperty("DefaultConnectionLimit");

                if (connectionLimit.HasValue)
                {
                    defaultConnectionLimit.SetValue(obj: servicePointManager, value: connectionLimit);
                }

                newValue = ((int)defaultConnectionLimit.GetValue(obj: servicePointManager)).ToString();
            }
#else
            // Increase the maximum number of connections per server.
            if (!RuntimeEnvironmentHelper.IsMono)
            {
                ServicePointManager.DefaultConnectionLimit = connectionLimit ?? 64;
            }
            else
            {
                // Keep mono limited to a single download to avoid issues.
                ServicePointManager.DefaultConnectionLimit = 1;
            }

            newValue = ServicePointManager.DefaultConnectionLimit.ToString();
#endif

            log.LogDebug($"ServicePointManager.DefaultConnectionLimit:  {newValue}");
        }

#if IS_CORECLR
        private static bool TryGetType(Assembly assembly, string typeName, out Type type)
        {
            type = assembly.GetType(typeName, throwOnError: false);

            return type != null;
        }

        private static bool TryGetAssembly(AssemblyName assemblyName, out Assembly assembly)
        {
            assembly = null;

            try
            {
                assembly = Assembly.Load(assemblyName);
            }
            catch (FileNotFoundException)
            {
            }

            return assembly != null;
        }
#endif
    }
}