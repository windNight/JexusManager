﻿// Copyright (c) Lex Li. All rights reserved.
// 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Microsoft.Web.Administration
{
    internal static class NativeMethods
    {
        private static readonly HTTPAPI_VERSION s_httpApiVersion = new HTTPAPI_VERSION(2, 0);

        #region DllImport

        [DllImport("httpapi.dll", SetLastError = true)]
        private static extern uint HttpInitialize(
            HTTPAPI_VERSION version,
            uint flags,
            IntPtr pReserved);

        [DllImport("httpapi.dll", SetLastError = true)]
        private static extern uint HttpSetServiceConfiguration(
                IntPtr serviceIntPtr,
                HTTP_SERVICE_CONFIG_ID configId,
                IntPtr pConfigInformation,
                int configInformationLength,
                IntPtr pOverlapped);

        [DllImport("httpapi.dll", SetLastError = true)]
        private static extern uint HttpDeleteServiceConfiguration(
            IntPtr serviceIntPtr,
            HTTP_SERVICE_CONFIG_ID configId,
            IntPtr pConfigInformation,
            int configInformationLength,
            IntPtr pOverlapped);

        [DllImport("httpapi.dll", SetLastError = true)]
        private static extern uint HttpTerminate(
            uint Flags,
            IntPtr pReserved);

        [DllImport("httpapi.dll", SetLastError = true)]
        private static extern uint HttpQueryServiceConfiguration(
                IntPtr serviceIntPtr,
                HTTP_SERVICE_CONFIG_ID configId,
                IntPtr pInputConfigInfo,
                int inputConfigInfoLength,
                IntPtr pOutputConfigInfo,
                int outputConfigInfoLength,
                [Optional]
                        out int pReturnLength,
                IntPtr pOverlapped);

        private enum HTTP_SERVICE_CONFIG_ID
        {
            HttpServiceConfigIPListenList = 0,
            HttpServiceConfigSSLCertInfo,
            HttpServiceConfigUrlAclInfo,
            HttpServiceConfigTimeout,
            HttpServiceConfigCache,
            HttpServiceConfigSslSniCertInfo,
            HttpServiceConfigMax
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HTTP_SERVICE_CONFIG_SSL_SET
        {
            public HTTP_SERVICE_CONFIG_SSL_KEY KeyDesc;
            public HTTP_SERVICE_CONFIG_SSL_PARAM ParamDesc;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HTTP_SERVICE_CONFIG_SSL_SNI_SET
        {
            public HTTP_SERVICE_CONFIG_SSL_SNI_KEY KeyDesc;
            public HTTP_SERVICE_CONFIG_SSL_PARAM ParamDesc;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HTTP_SERVICE_CONFIG_SSL_SNI_KEY
        {
            public SOCKADDR_STORAGE IpPort;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Host;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SOCKADDR_STORAGE
        {
            public short ss_family;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public byte[] __ss_pad1;

            public long __ss_align;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 112)]
            public byte[] __ss_pad2;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HTTP_SERVICE_CONFIG_SSL_KEY
        {
            public IntPtr pIpPort;

            public HTTP_SERVICE_CONFIG_SSL_KEY(IntPtr pIpPort)
            {
                this.pIpPort = pIpPort;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct HTTP_SERVICE_CONFIG_SSL_PARAM
        {
            public int SslHashLength;
            public IntPtr pSslHash;
            public Guid AppId;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pSslCertStoreName;
            public uint DefaultCertCheckMode;
            public int DefaultRevocationFreshnessTime;
            public int DefaultRevocationUrlRetrievalTimeout;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pDefaultSslCtlIdentifier;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pDefaultSslCtlStoreName;
            public uint DefaultFlags;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        private struct HTTPAPI_VERSION
        {
            public ushort HttpApiMajorVersion;
            public ushort HttpApiMinorVersion;

            public HTTPAPI_VERSION(ushort majorVersion, ushort minorVersion)
            {
                HttpApiMajorVersion = majorVersion;
                HttpApiMinorVersion = minorVersion;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HTTP_SERVICE_CONFIG_SSL_QUERY
        {
            public HTTP_SERVICE_CONFIG_QUERY_TYPE QueryDesc;
            public HTTP_SERVICE_CONFIG_SSL_KEY KeyDesc;
            public uint dwToken;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HTTP_SERVICE_CONFIG_SSL_SNI_QUERY
        {
            public HTTP_SERVICE_CONFIG_QUERY_TYPE QueryDesc;
            public HTTP_SERVICE_CONFIG_SSL_SNI_KEY KeyDesc;
            public uint dwToken;
        }

        private enum HTTP_SERVICE_CONFIG_QUERY_TYPE
        {
            HttpServiceConfigQueryExact = 0,
            HttpServiceConfigQueryNext,
            HttpServiceConfigQueryMax
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HTTP_SERVICE_CONFIG_URLACL_QUERY
        {
            public HTTP_SERVICE_CONFIG_QUERY_TYPE QueryDesc;
            public HTTP_SERVICE_CONFIG_URLACL_KEY KeyDesc;
            public uint dwToken;
        }

        private struct HTTP_SERVICE_CONFIG_URLACL_KEY
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pUrlPrefix;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HTTP_SERVICE_CONFIG_URLACL_SET
        {
            public HTTP_SERVICE_CONFIG_URLACL_KEY KeyDesc;
            public HTTP_SERVICE_CONFIG_URLACL_PARAM ParamDesc;
        }

        private struct HTTP_SERVICE_CONFIG_URLACL_PARAM
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pStringSecurityDescriptor;
        }

        #endregion

        #region Constants

        internal const uint HTTP_INITIALIZE_CONFIG = 0x00000002;
        internal const uint HTTP_SERVICE_CONFIG_SSL_FLAG_NEGOTIATE_CLIENT_CERT = 0x00000002;
        internal const uint HTTP_SERVICE_CONFIG_SSL_FLAG_NO_RAW_FILTER = 0x00000004;
        private const uint NOERROR = 0;
        private const uint ERROR_INSUFFICIENT_BUFFER = 122;
        private const uint ERROR_ALREADY_EXISTS = 183;
        private const uint ERROR_ACCESS_DENIED = 5;
        private const uint ERROR_FILE_NOT_FOUND = 2;
        private const int ERROR_NO_MORE_ITEMS = 259;

        #endregion

        #region Public methods

        public class SslCertificateInfo
        {
            public byte[] Hash { get; set; }
            public Guid AppId { get; set; }
            public string StoreName { get; set; }
            public IPEndPoint IpPort { get; set; }
        }

        public static SslCertificateInfo QuerySslCertificateInfo(IPEndPoint ipPort)
        {
            SslCertificateInfo result = null;

            uint retVal;
            CallHttpApi(delegate
            {
                GCHandle sockAddrHandle = CreateSockaddrStructure(ipPort);
                IntPtr pIpPort = sockAddrHandle.AddrOfPinnedObject();
                HTTP_SERVICE_CONFIG_SSL_KEY sslKey = new HTTP_SERVICE_CONFIG_SSL_KEY(pIpPort);

                HTTP_SERVICE_CONFIG_SSL_QUERY inputConfigInfoQuery =
                    new HTTP_SERVICE_CONFIG_SSL_QUERY
                    {
                        QueryDesc = HTTP_SERVICE_CONFIG_QUERY_TYPE.HttpServiceConfigQueryExact,
                        KeyDesc = sslKey
                    };

                IntPtr pInputConfigInfo =
                    Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(HTTP_SERVICE_CONFIG_SSL_QUERY)));
                Marshal.StructureToPtr(inputConfigInfoQuery, pInputConfigInfo, false);

                IntPtr pOutputConfigInfo = IntPtr.Zero;
                int returnLength = 0;

                try
                {
                    HTTP_SERVICE_CONFIG_ID queryType = HTTP_SERVICE_CONFIG_ID.HttpServiceConfigSSLCertInfo;
                    int inputConfigInfoSize = Marshal.SizeOf(inputConfigInfoQuery);
                    retVal = HttpQueryServiceConfiguration(IntPtr.Zero,
                                                            queryType,
                                                            pInputConfigInfo,
                                                            inputConfigInfoSize,
                                                            pOutputConfigInfo,
                                                            returnLength,
                                                            out returnLength,
                                                            IntPtr.Zero);
                    if (retVal == ERROR_FILE_NOT_FOUND)
                        return;

                    if (ERROR_INSUFFICIENT_BUFFER == retVal) // ERROR_INSUFFICIENT_BUFFER = 122
                    {
                        pOutputConfigInfo = Marshal.AllocCoTaskMem(returnLength);

                        try
                        {
                            retVal = HttpQueryServiceConfiguration(IntPtr.Zero,
                                                                                    queryType,
                                                                                    pInputConfigInfo,
                                                                                    inputConfigInfoSize,
                                                                                    pOutputConfigInfo,
                                                                                    returnLength,
                                                                                    out returnLength,
                                                                                    IntPtr.Zero);
                            ThrowWin32ExceptionIfError(retVal);

                            var outputConfigInfo =
                                (HTTP_SERVICE_CONFIG_SSL_SET)
                                Marshal.PtrToStructure(pOutputConfigInfo, typeof(HTTP_SERVICE_CONFIG_SSL_SET));

                            byte[] hash = new byte[outputConfigInfo.ParamDesc.SslHashLength];
                            Marshal.Copy(outputConfigInfo.ParamDesc.pSslHash, hash, 0, hash.Length);

                            Guid appId = outputConfigInfo.ParamDesc.AppId;
                            string storeName = outputConfigInfo.ParamDesc.pSslCertStoreName;

                            result = new SslCertificateInfo { AppId = appId, Hash = hash, StoreName = storeName, IpPort = ipPort };
                        }
                        finally
                        {
                            Marshal.FreeCoTaskMem(pOutputConfigInfo);
                        }
                    }
                    else
                    {
                        ThrowWin32ExceptionIfError(retVal);
                    }
                }
                finally
                {
                    Marshal.FreeCoTaskMem(pInputConfigInfo);
                    if (sockAddrHandle.IsAllocated)
                        sockAddrHandle.Free();
                }
            });

            return result;
        }

        public static void BindCertificate(IPEndPoint ipPort, byte[] hash, string storeName, Guid appId)
        {
            if (ipPort == null) throw new ArgumentNullException(nameof(ipPort));
            if (hash == null) throw new ArgumentNullException(nameof(hash));

            CallHttpApi(
                delegate
                {
                    HTTP_SERVICE_CONFIG_SSL_SET configSslSet = new HTTP_SERVICE_CONFIG_SSL_SET();

                    GCHandle sockAddrHandle = CreateSockaddrStructure(ipPort);
                    IntPtr pIpPort = sockAddrHandle.AddrOfPinnedObject();
                    HTTP_SERVICE_CONFIG_SSL_KEY httpServiceConfigSslKey =
                        new HTTP_SERVICE_CONFIG_SSL_KEY(pIpPort);
                    HTTP_SERVICE_CONFIG_SSL_PARAM configSslParam = new HTTP_SERVICE_CONFIG_SSL_PARAM();


                    GCHandle handleHash = GCHandle.Alloc(hash, GCHandleType.Pinned);
                    configSslParam.AppId = appId;
                    configSslParam.DefaultCertCheckMode = 0;
                    configSslParam.DefaultFlags = 0; //HTTP_SERVICE_CONFIG_SSL_FLAG_NEGOTIATE_CLIENT_CERT;
                    configSslParam.DefaultRevocationFreshnessTime = 0;
                    configSslParam.DefaultRevocationUrlRetrievalTimeout = 0;
                    configSslParam.pSslCertStoreName = storeName;
                    configSslParam.pSslHash = handleHash.AddrOfPinnedObject();
                    configSslParam.SslHashLength = hash.Length;
                    configSslSet.ParamDesc = configSslParam;
                    configSslSet.KeyDesc = httpServiceConfigSslKey;

                    IntPtr pInputConfigInfo =
                        Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(HTTP_SERVICE_CONFIG_SSL_SET)));
                    Marshal.StructureToPtr(configSslSet, pInputConfigInfo, false);

                    try
                    {
                        uint retVal = HttpSetServiceConfiguration(IntPtr.Zero,
                                                                    HTTP_SERVICE_CONFIG_ID.HttpServiceConfigSSLCertInfo,
                                                                    pInputConfigInfo,
                                                                    Marshal.SizeOf(configSslSet),
                                                                    IntPtr.Zero);

                        if (ERROR_ALREADY_EXISTS != retVal)
                        {
                            ThrowWin32ExceptionIfError(retVal);
                        }
                        else
                        {
                            retVal = HttpDeleteServiceConfiguration(IntPtr.Zero,
                                                                    HTTP_SERVICE_CONFIG_ID.HttpServiceConfigSSLCertInfo,
                                                                    pInputConfigInfo,
                                                                    Marshal.SizeOf(configSslSet),
                                                                    IntPtr.Zero);
                            ThrowWin32ExceptionIfError(retVal);

                            retVal = HttpSetServiceConfiguration(IntPtr.Zero,
                                                                    HTTP_SERVICE_CONFIG_ID.HttpServiceConfigSSLCertInfo,
                                                                    pInputConfigInfo,
                                                                    Marshal.SizeOf(configSslSet),
                                                                    IntPtr.Zero);
                            ThrowWin32ExceptionIfError(retVal);
                        }
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(pInputConfigInfo);
                        if (handleHash.IsAllocated)
                            handleHash.Free();
                        if (sockAddrHandle.IsAllocated)
                            sockAddrHandle.Free();
                    }
                });
        }

        public static void DeleteCertificateBinding(params IPEndPoint[] ipPorts)
        {
            if (ipPorts == null || ipPorts.Length == 0)
                return;

            CallHttpApi(
            delegate
            {
                foreach (var ipPort in ipPorts)
                {
                    HTTP_SERVICE_CONFIG_SSL_SET configSslSet =
                        new HTTP_SERVICE_CONFIG_SSL_SET();

                    GCHandle sockAddrHandle = CreateSockaddrStructure(ipPort);
                    IntPtr pIpPort = sockAddrHandle.AddrOfPinnedObject();
                    HTTP_SERVICE_CONFIG_SSL_KEY httpServiceConfigSslKey =
                        new HTTP_SERVICE_CONFIG_SSL_KEY(pIpPort);
                    configSslSet.KeyDesc = httpServiceConfigSslKey;

                    IntPtr pInputConfigInfo =
                        Marshal.AllocCoTaskMem(
                            Marshal.SizeOf(typeof(HTTP_SERVICE_CONFIG_SSL_SET)));
                    Marshal.StructureToPtr(configSslSet, pInputConfigInfo, false);

                    try
                    {
                        uint retVal = HttpDeleteServiceConfiguration(IntPtr.Zero,
                                                                        HTTP_SERVICE_CONFIG_ID.HttpServiceConfigSSLCertInfo,
                                                                        pInputConfigInfo,
                                                                        Marshal.SizeOf(configSslSet),
                                                                        IntPtr.Zero);
                        ThrowWin32ExceptionIfError(retVal);
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(pInputConfigInfo);
                        if (sockAddrHandle.IsAllocated)
                            sockAddrHandle.Free();
                    }
                }
            });
        }

        public static SslCertificateInfo[] QuerySslCertificateInfo()
        {
            var result = new List<SslCertificateInfo>();

            CallHttpApi(
                delegate
                {
                    uint token = 0;

                    uint retVal;
                    do
                    {
                        HTTP_SERVICE_CONFIG_SSL_QUERY inputConfigInfoQuery =
                            new HTTP_SERVICE_CONFIG_SSL_QUERY
                            {
                                QueryDesc = HTTP_SERVICE_CONFIG_QUERY_TYPE.HttpServiceConfigQueryNext,
                                dwToken = token
                            };

                        IntPtr pInputConfigInfo =
                            Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(HTTP_SERVICE_CONFIG_SSL_QUERY)));
                        Marshal.StructureToPtr(inputConfigInfoQuery, pInputConfigInfo, false);

                        IntPtr pOutputConfigInfo = IntPtr.Zero;
                        int returnLength = 0;

                        const HTTP_SERVICE_CONFIG_ID queryType = HTTP_SERVICE_CONFIG_ID.HttpServiceConfigSSLCertInfo;

                        try
                        {
                            int inputConfigInfoSize = Marshal.SizeOf(inputConfigInfoQuery);
                            retVal = HttpQueryServiceConfiguration(IntPtr.Zero,
                                                                    queryType,
                                                                    pInputConfigInfo,
                                                                    inputConfigInfoSize,
                                                                    pOutputConfigInfo,
                                                                    returnLength,
                                                                    out returnLength,
                                                                    IntPtr.Zero);
                            if (ERROR_NO_MORE_ITEMS == retVal)
                                break;
                            if (ERROR_INSUFFICIENT_BUFFER == retVal) // ERROR_INSUFFICIENT_BUFFER = 122
                            {
                                pOutputConfigInfo = Marshal.AllocCoTaskMem(returnLength);

                                try
                                {
                                    retVal = HttpQueryServiceConfiguration(
                                        IntPtr.Zero,
                                        queryType,
                                        pInputConfigInfo,
                                        inputConfigInfoSize,
                                        pOutputConfigInfo,
                                        returnLength,
                                        out returnLength,
                                        IntPtr.Zero);
                                    ThrowWin32ExceptionIfError(retVal);

                                    var outputConfigInfo =
                                        (HTTP_SERVICE_CONFIG_SSL_SET)
                                        Marshal.PtrToStructure(pOutputConfigInfo, typeof(HTTP_SERVICE_CONFIG_SSL_SET));

                                    byte[] hash = new byte[outputConfigInfo.ParamDesc.SslHashLength];
                                    Marshal.Copy(outputConfigInfo.ParamDesc.pSslHash, hash, 0, hash.Length);

                                    Guid appId = outputConfigInfo.ParamDesc.AppId;
                                    string storeName = outputConfigInfo.ParamDesc.pSslCertStoreName;
                                    IPEndPoint ipPort = ReadSockaddrStructure(outputConfigInfo.KeyDesc.pIpPort);

                                    var resultItem = new SslCertificateInfo
                                    {
                                        AppId = appId,
                                        Hash = hash,
                                        StoreName = storeName,
                                        IpPort = ipPort
                                    };
                                    result.Add(resultItem);
                                    token++;
                                }
                                finally
                                {
                                    Marshal.FreeCoTaskMem(pOutputConfigInfo);
                                }
                            }
                            else
                            {
                                ThrowWin32ExceptionIfError(retVal);
                            }
                        }
                        finally
                        {
                            Marshal.FreeCoTaskMem(pInputConfigInfo);
                        }
                    } while (NOERROR == retVal);
                });

            return result.ToArray();
        }

        public class SslSniInfo
        {
            public byte[] Hash { get; set; }
            public Guid AppId { get; set; }
            public string StoreName { get; set; }
            public int Port { get; set; }

            public string Host { get; set; }
        }

        public static SslSniInfo QuerySslSniInfo(Tuple<string, int> binding)
        {
            if (string.IsNullOrWhiteSpace(binding.Item1))
            {
                return null;
            }

            if (Environment.OSVersion.Version < new Version(6, 2))
            {
                return null;
            }

            SslSniInfo result = null;

            uint retVal;
            CallHttpApi(delegate
            {
                HTTP_SERVICE_CONFIG_SSL_SNI_KEY sslKey = new HTTP_SERVICE_CONFIG_SSL_SNI_KEY();
                sslKey.Host = binding.Item1;
                sslKey.IpPort = CreateSockAddrStorageStructure(binding.Item2);

                HTTP_SERVICE_CONFIG_SSL_SNI_QUERY inputConfigInfoQuery =
                    new HTTP_SERVICE_CONFIG_SSL_SNI_QUERY
                    {
                        QueryDesc = HTTP_SERVICE_CONFIG_QUERY_TYPE.HttpServiceConfigQueryExact,
                        KeyDesc = sslKey
                    };

                IntPtr pInputConfigInfo =
                    Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(HTTP_SERVICE_CONFIG_SSL_SNI_QUERY)));
                Marshal.StructureToPtr(inputConfigInfoQuery, pInputConfigInfo, false);

                IntPtr pOutputConfigInfo = IntPtr.Zero;
                int returnLength = 0;

                try
                {
                    HTTP_SERVICE_CONFIG_ID queryType = HTTP_SERVICE_CONFIG_ID.HttpServiceConfigSslSniCertInfo;
                    int inputConfigInfoSize = Marshal.SizeOf(inputConfigInfoQuery);
                    retVal = HttpQueryServiceConfiguration(IntPtr.Zero,
                                                            queryType,
                                                            pInputConfigInfo,
                                                            inputConfigInfoSize,
                                                            pOutputConfigInfo,
                                                            returnLength,
                                                            out returnLength,
                                                            IntPtr.Zero);
                    if (retVal == ERROR_FILE_NOT_FOUND)
                        return;

                    if (ERROR_INSUFFICIENT_BUFFER == retVal) // ERROR_INSUFFICIENT_BUFFER = 122
                    {
                        pOutputConfigInfo = Marshal.AllocCoTaskMem(returnLength);

                        try
                        {
                            retVal = HttpQueryServiceConfiguration(IntPtr.Zero,
                                                                                    queryType,
                                                                                    pInputConfigInfo,
                                                                                    inputConfigInfoSize,
                                                                                    pOutputConfigInfo,
                                                                                    returnLength,
                                                                                    out returnLength,
                                                                                    IntPtr.Zero);
                            ThrowWin32ExceptionIfError(retVal);

                            var outputConfigInfo =
                                (HTTP_SERVICE_CONFIG_SSL_SNI_SET)
                                Marshal.PtrToStructure(pOutputConfigInfo, typeof(HTTP_SERVICE_CONFIG_SSL_SNI_SET));

                            byte[] hash = new byte[outputConfigInfo.ParamDesc.SslHashLength];
                            Marshal.Copy(outputConfigInfo.ParamDesc.pSslHash, hash, 0, hash.Length);

                            Guid appId = outputConfigInfo.ParamDesc.AppId;
                            string storeName = outputConfigInfo.ParamDesc.pSslCertStoreName;

                            var host = outputConfigInfo.KeyDesc.Host;
                            var ipPort = ReadSockAddrStorageStructure(outputConfigInfo.KeyDesc.IpPort);

                            result = new SslSniInfo { AppId = appId, Hash = hash, StoreName = storeName, Port = ipPort.Port, Host = host };
                        }
                        finally
                        {
                            Marshal.FreeCoTaskMem(pOutputConfigInfo);
                        }
                    }
                    else
                    {
                        ThrowWin32ExceptionIfError(retVal);
                    }
                }
                finally
                {
                    Marshal.FreeCoTaskMem(pInputConfigInfo);
                }
            });

            return result;
        }

        public static void BindSni(Tuple<string, int> binding, byte[] hash, string storeName, Guid appId)
        {
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            if (hash == null) throw new ArgumentNullException(nameof(hash));

            if (Environment.OSVersion.Version < new Version(6, 2))
            {
                return;
            }

            CallHttpApi(
                delegate
                {
                    HTTP_SERVICE_CONFIG_SSL_SNI_SET configSslSet = new HTTP_SERVICE_CONFIG_SSL_SNI_SET();


                    HTTP_SERVICE_CONFIG_SSL_SNI_KEY httpServiceConfigSslKey =
                        new HTTP_SERVICE_CONFIG_SSL_SNI_KEY();
                    httpServiceConfigSslKey.Host = binding.Item1;
                    httpServiceConfigSslKey.IpPort = CreateSockAddrStorageStructure(binding.Item2);
                    HTTP_SERVICE_CONFIG_SSL_PARAM configSslParam = new HTTP_SERVICE_CONFIG_SSL_PARAM();


                    GCHandle handleHash = GCHandle.Alloc(hash, GCHandleType.Pinned);
                    configSslParam.AppId = appId;
                    configSslParam.DefaultCertCheckMode = 0;
                    configSslParam.DefaultFlags = 0; //HTTP_SERVICE_CONFIG_SSL_FLAG_NEGOTIATE_CLIENT_CERT;
                    configSslParam.DefaultRevocationFreshnessTime = 0;
                    configSslParam.DefaultRevocationUrlRetrievalTimeout = 0;
                    configSslParam.pSslCertStoreName = storeName;
                    configSslParam.pSslHash = handleHash.AddrOfPinnedObject();
                    configSslParam.SslHashLength = hash.Length;
                    configSslSet.ParamDesc = configSslParam;
                    configSslSet.KeyDesc = httpServiceConfigSslKey;

                    IntPtr pInputConfigInfo =
                        Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(HTTP_SERVICE_CONFIG_SSL_SNI_SET)));
                    Marshal.StructureToPtr(configSslSet, pInputConfigInfo, false);

                    try
                    {
                        uint retVal = HttpSetServiceConfiguration(IntPtr.Zero,
                                                                    HTTP_SERVICE_CONFIG_ID.HttpServiceConfigSslSniCertInfo,
                                                                    pInputConfigInfo,
                                                                    Marshal.SizeOf(configSslSet),
                                                                    IntPtr.Zero);

                        if (ERROR_ALREADY_EXISTS != retVal)
                        {
                            ThrowWin32ExceptionIfError(retVal);
                        }
                        else
                        {
                            retVal = HttpDeleteServiceConfiguration(IntPtr.Zero,
                                                                    HTTP_SERVICE_CONFIG_ID.HttpServiceConfigSslSniCertInfo,
                                                                    pInputConfigInfo,
                                                                    Marshal.SizeOf(configSslSet),
                                                                    IntPtr.Zero);
                            ThrowWin32ExceptionIfError(retVal);

                            retVal = HttpSetServiceConfiguration(IntPtr.Zero,
                                                                    HTTP_SERVICE_CONFIG_ID.HttpServiceConfigSslSniCertInfo,
                                                                    pInputConfigInfo,
                                                                    Marshal.SizeOf(configSslSet),
                                                                    IntPtr.Zero);
                            ThrowWin32ExceptionIfError(retVal);
                        }
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(pInputConfigInfo);
                        if (handleHash.IsAllocated)
                            handleHash.Free();
                    }
                });
        }

        public static void DeleteSniBinding(params Tuple<string, int>[] bindings)
        {
            if (bindings == null || bindings.Length == 0)
                return;

            if (Environment.OSVersion.Version < new Version(6, 2))
            {
                return;
            }

            CallHttpApi(
            delegate
            {
                foreach (var binding in bindings)
                {
                    HTTP_SERVICE_CONFIG_SSL_SNI_SET configSslSet =
                        new HTTP_SERVICE_CONFIG_SSL_SNI_SET();

                    HTTP_SERVICE_CONFIG_SSL_SNI_KEY httpServiceConfigSslKey =
                        new HTTP_SERVICE_CONFIG_SSL_SNI_KEY();
                    httpServiceConfigSslKey.Host = binding.Item1;
                    httpServiceConfigSslKey.IpPort = CreateSockAddrStorageStructure(binding.Item2);
                    configSslSet.KeyDesc = httpServiceConfigSslKey;

                    IntPtr pInputConfigInfo =
                        Marshal.AllocCoTaskMem(
                            Marshal.SizeOf(typeof(HTTP_SERVICE_CONFIG_SSL_SNI_SET)));
                    Marshal.StructureToPtr(configSslSet, pInputConfigInfo, false);

                    try
                    {
                        uint retVal = HttpDeleteServiceConfiguration(IntPtr.Zero,
                                                                        HTTP_SERVICE_CONFIG_ID.HttpServiceConfigSslSniCertInfo,
                                                                        pInputConfigInfo,
                                                                        Marshal.SizeOf(configSslSet),
                                                                        IntPtr.Zero);
                        ThrowWin32ExceptionIfError(retVal);
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(pInputConfigInfo);
                    }
                }
            });
        }

        public static SslSniInfo[] QuerySslSniInfo()
        {
            var result = new List<SslSniInfo>();

            if (Environment.OSVersion.Version < new Version(6, 2))
            {
                return result.ToArray();
            }

            CallHttpApi(
                delegate
                {
                    uint token = 0;

                    uint retVal;
                    do
                    {
                        HTTP_SERVICE_CONFIG_SSL_SNI_QUERY inputConfigInfoQuery =
                            new HTTP_SERVICE_CONFIG_SSL_SNI_QUERY
                            {
                                QueryDesc = HTTP_SERVICE_CONFIG_QUERY_TYPE.HttpServiceConfigQueryNext,
                                dwToken = token
                            };

                        IntPtr pInputConfigInfo =
                            Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(HTTP_SERVICE_CONFIG_SSL_SNI_QUERY)));
                        Marshal.StructureToPtr(inputConfigInfoQuery, pInputConfigInfo, false);

                        IntPtr pOutputConfigInfo = IntPtr.Zero;
                        int returnLength = 0;

                        const HTTP_SERVICE_CONFIG_ID queryType = HTTP_SERVICE_CONFIG_ID.HttpServiceConfigSslSniCertInfo;

                        try
                        {
                            int inputConfigInfoSize = Marshal.SizeOf(inputConfigInfoQuery);
                            retVal = HttpQueryServiceConfiguration(IntPtr.Zero,
                                                                    queryType,
                                                                    pInputConfigInfo,
                                                                    inputConfigInfoSize,
                                                                    pOutputConfigInfo,
                                                                    returnLength,
                                                                    out returnLength,
                                                                    IntPtr.Zero);
                            if (ERROR_NO_MORE_ITEMS == retVal)
                                break;
                            if (ERROR_INSUFFICIENT_BUFFER == retVal) // ERROR_INSUFFICIENT_BUFFER = 122
                            {
                                pOutputConfigInfo = Marshal.AllocCoTaskMem(returnLength);

                                try
                                {
                                    retVal = HttpQueryServiceConfiguration(
                                        IntPtr.Zero,
                                        queryType,
                                        pInputConfigInfo,
                                        inputConfigInfoSize,
                                        pOutputConfigInfo,
                                        returnLength,
                                        out returnLength,
                                        IntPtr.Zero);
                                    ThrowWin32ExceptionIfError(retVal);

                                    var outputConfigInfo =
                                        (HTTP_SERVICE_CONFIG_SSL_SNI_SET)
                                        Marshal.PtrToStructure(pOutputConfigInfo, typeof(HTTP_SERVICE_CONFIG_SSL_SNI_SET));

                                    byte[] hash = new byte[outputConfigInfo.ParamDesc.SslHashLength];
                                    Marshal.Copy(outputConfigInfo.ParamDesc.pSslHash, hash, 0, hash.Length);

                                    Guid appId = outputConfigInfo.ParamDesc.AppId;
                                    string storeName = outputConfigInfo.ParamDesc.pSslCertStoreName;
                                    var ipPort = ReadSockAddrStorageStructure(outputConfigInfo.KeyDesc.IpPort);

                                    var host = outputConfigInfo.KeyDesc.Host;
                                    var resultItem = new SslSniInfo
                                    {
                                        AppId = appId,
                                        Hash = hash,
                                        StoreName = storeName,
                                        Host = host,
                                        Port = ipPort.Port
                                    };
                                    result.Add(resultItem);
                                    token++;
                                }
                                finally
                                {
                                    Marshal.FreeCoTaskMem(pOutputConfigInfo);
                                }
                            }
                            else
                            {
                                ThrowWin32ExceptionIfError(retVal);
                            }
                        }
                        finally
                        {
                            Marshal.FreeCoTaskMem(pInputConfigInfo);
                        }
                    } while (NOERROR == retVal);
                });

            return result.ToArray();
        }

        public class HttpNamespaceAcl
        {
            public string UrlPrefix { get; set; }
            public string SecurityDescriptor { get; set; }
        }

        public static HttpNamespaceAcl[] QueryHttpNamespaceAcls()
        {
            var result = new List<HttpNamespaceAcl>();

            CallHttpApi(
                delegate
                {
                    uint token = 0;

                    uint retVal;
                    do
                    {
                        HTTP_SERVICE_CONFIG_URLACL_QUERY inputConfigInfoQuery =
                            new HTTP_SERVICE_CONFIG_URLACL_QUERY
                            {
                                QueryDesc = HTTP_SERVICE_CONFIG_QUERY_TYPE.HttpServiceConfigQueryNext,
                                dwToken = token
                            };

                        IntPtr pInputConfigInfo =
                            Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(HTTP_SERVICE_CONFIG_URLACL_QUERY)));
                        Marshal.StructureToPtr(inputConfigInfoQuery, pInputConfigInfo, false);

                        IntPtr pOutputConfigInfo = IntPtr.Zero;
                        int returnLength = 0;

                        const HTTP_SERVICE_CONFIG_ID queryType = HTTP_SERVICE_CONFIG_ID.HttpServiceConfigUrlAclInfo;

                        try
                        {
                            int inputConfigInfoSize = Marshal.SizeOf(inputConfigInfoQuery);
                            retVal = HttpQueryServiceConfiguration(IntPtr.Zero,
                                                                    queryType,
                                                                    pInputConfigInfo,
                                                                    inputConfigInfoSize,
                                                                    pOutputConfigInfo,
                                                                    returnLength,
                                                                    out returnLength,
                                                                    IntPtr.Zero);
                            if (ERROR_NO_MORE_ITEMS == retVal)
                                break;
                            if (ERROR_INSUFFICIENT_BUFFER == retVal) // ERROR_INSUFFICIENT_BUFFER = 122
                            {
                                pOutputConfigInfo = Marshal.AllocCoTaskMem(returnLength);

                                try
                                {
                                    retVal = HttpQueryServiceConfiguration(
                                        IntPtr.Zero,
                                        queryType,
                                        pInputConfigInfo,
                                        inputConfigInfoSize,
                                        pOutputConfigInfo,
                                        returnLength,
                                        out returnLength,
                                        IntPtr.Zero);
                                    ThrowWin32ExceptionIfError(retVal);

                                    var outputConfigInfo =
                                        (HTTP_SERVICE_CONFIG_URLACL_SET)
                                        Marshal.PtrToStructure(pOutputConfigInfo, typeof(HTTP_SERVICE_CONFIG_URLACL_SET));

                                    var resultItem = new HttpNamespaceAcl
                                    {
                                        UrlPrefix = outputConfigInfo.KeyDesc.pUrlPrefix,
                                        SecurityDescriptor = outputConfigInfo.ParamDesc.pStringSecurityDescriptor
                                    };
                                    result.Add(resultItem);
                                    token++;
                                }
                                finally
                                {
                                    Marshal.FreeCoTaskMem(pOutputConfigInfo);
                                }
                            }
                            else
                            {
                                ThrowWin32ExceptionIfError(retVal);
                            }
                        }
                        finally
                        {
                            Marshal.FreeCoTaskMem(pInputConfigInfo);
                        }
                    } while (NOERROR == retVal);
                });

            return result.ToArray();
        }

        public static void BindHttpNamespaceAcl(string networkURL, string securityDescriptor)
        {
            var retVal = NOERROR; // NOERROR = 0

            CallHttpApi(() =>
            {
                if (NOERROR == retVal)
                {
                    var keyDesc = new HTTP_SERVICE_CONFIG_URLACL_KEY { pUrlPrefix = networkURL };
                    var paramDesc = new HTTP_SERVICE_CONFIG_URLACL_PARAM { pStringSecurityDescriptor =  securityDescriptor};

                    var inputConfigInfoSet = new HTTP_SERVICE_CONFIG_URLACL_SET();
                    inputConfigInfoSet.KeyDesc = keyDesc;
                    inputConfigInfoSet.ParamDesc = paramDesc;

                    var pInputConfigInfo = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(HTTP_SERVICE_CONFIG_URLACL_SET)));
                    Marshal.StructureToPtr(inputConfigInfoSet, pInputConfigInfo, false);

                    retVal = HttpSetServiceConfiguration(
                        IntPtr.Zero,
                        HTTP_SERVICE_CONFIG_ID.HttpServiceConfigUrlAclInfo,
                        pInputConfigInfo,
                        Marshal.SizeOf(inputConfigInfoSet),
                        IntPtr.Zero);

                    if (ERROR_ALREADY_EXISTS == retVal) // ERROR_ALREADY_EXISTS = 183
                    {
                        retVal = HttpDeleteServiceConfiguration(
                            IntPtr.Zero,
                            HTTP_SERVICE_CONFIG_ID.HttpServiceConfigUrlAclInfo,
                            pInputConfigInfo,
                            Marshal.SizeOf(inputConfigInfoSet),
                            IntPtr.Zero);

                        if (NOERROR == retVal)
                        {
                            retVal = HttpSetServiceConfiguration(IntPtr.Zero,
                                HTTP_SERVICE_CONFIG_ID.HttpServiceConfigUrlAclInfo,
                                pInputConfigInfo,
                                Marshal.SizeOf(inputConfigInfoSet),
                                IntPtr.Zero);
                        }
                    }
                    else if (ERROR_ACCESS_DENIED == retVal)
                    {
                        //Debug.WriteLine("ERROR_ACCESS_DENIED reserving a HTTP url");
                        retVal = NOERROR;
                    }

                    Marshal.FreeCoTaskMem(pInputConfigInfo);
                }

                if (NOERROR != retVal)
                {
                    throw new Win32Exception(Convert.ToInt32(retVal));
                }
            });
        }

        public static void DeleteHttpNamespaceAcl(string networkURL, string securityDescriptor)
        {
            var retVal = NOERROR; // NOERROR = 0

            CallHttpApi(() =>
            {
                if (NOERROR == retVal)
                {
                    var keyDesc = new HTTP_SERVICE_CONFIG_URLACL_KEY { pUrlPrefix = networkURL };
                    var paramDesc = new HTTP_SERVICE_CONFIG_URLACL_PARAM { pStringSecurityDescriptor = securityDescriptor };

                    var inputConfigInfoSet = new HTTP_SERVICE_CONFIG_URLACL_SET();
                    inputConfigInfoSet.KeyDesc = keyDesc;
                    inputConfigInfoSet.ParamDesc = paramDesc;

                    var pInputConfigInfo = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(HTTP_SERVICE_CONFIG_URLACL_SET)));
                    Marshal.StructureToPtr(inputConfigInfoSet, pInputConfigInfo, false);

                    retVal = HttpDeleteServiceConfiguration(
                            IntPtr.Zero,
                            HTTP_SERVICE_CONFIG_ID.HttpServiceConfigUrlAclInfo,
                            pInputConfigInfo,
                            Marshal.SizeOf(inputConfigInfoSet),
                            IntPtr.Zero);

                    if (ERROR_ACCESS_DENIED == retVal)
                    {
                        //Debug.WriteLine("ERROR_ACCESS_DENIED reserving a HTTP url");
                        retVal = NOERROR;
                    }

                    Marshal.FreeCoTaskMem(pInputConfigInfo);
                }

                if (NOERROR != retVal)
                {
                    throw new Win32Exception(Convert.ToInt32(retVal));
                }
            });
        }
        #endregion

        private static void ThrowWin32ExceptionIfError(uint retVal)
        {
            if (NOERROR != retVal)
            {
                throw new Win32Exception(Convert.ToInt32(retVal));
            }
        }

        private static void CallHttpApi(Action body)
        {
            uint retVal = HttpInitialize(s_httpApiVersion, HTTP_INITIALIZE_CONFIG, IntPtr.Zero);
            ThrowWin32ExceptionIfError(retVal);

            try
            {
                body();
            }
            finally
            {
                HttpTerminate(HTTP_INITIALIZE_CONFIG, IntPtr.Zero);
            }
        }

        /// <summary>
        /// Creates an unmanaged sockaddr structure to pass to a WinAPI function.
        /// </summary>
        /// <param name="ipEndPoint">IP address and port number</param>
        /// <returns>a handle for the structure. Use the AddrOfPinnedObject Method to get a stable pointer to the object. </returns>
        /// <remarks>When the handle goes out of scope you must explicitly release it by calling the Free method; otherwise, memory leaks may occur. </remarks>
        private static GCHandle CreateSockaddrStructure(IPEndPoint ipEndPoint)
        {
            SocketAddress socketAddress = ipEndPoint.Serialize();

            // use an array of bytes instead of the sockaddr structure 
            byte[] sockAddrStructureBytes = new byte[socketAddress.Size];
            GCHandle sockAddrHandle = GCHandle.Alloc(sockAddrStructureBytes, GCHandleType.Pinned);
            for (int i = 0; i < socketAddress.Size; ++i)
            {
                sockAddrStructureBytes[i] = socketAddress[i];
            }
            return sockAddrHandle;
        }

        private static SOCKADDR_STORAGE CreateSockAddrStorageStructure(int port)
        {
            var result = new SOCKADDR_STORAGE();
            result.ss_family = (short)AddressFamily.InterNetwork;
            var ipEndPoint = new IPEndPoint(IPAddress.Any, port);
            var socketAddress = ipEndPoint.Serialize();
            result.__ss_pad1 = new byte[6];
            for (var i = 0; i < result.__ss_pad1.Length; i++)
            {
                result.__ss_pad1[i] = socketAddress[i + 2];
            }

            return result;
        }

        /// <summary>
        /// Reads the unmanaged sockaddr structure returned by a WinAPI function
        /// </summary>
        /// <param name="pSockaddrStructure">pointer to the unmanaged sockaddr structure</param>
        /// <returns>IP address and port number</returns>
        private static IPEndPoint ReadSockaddrStructure(IntPtr pSockaddrStructure)
        {
            short sAddressFamily = Marshal.ReadInt16(pSockaddrStructure);
            AddressFamily addressFamily = (AddressFamily)sAddressFamily;

            int sockAddrSructureSize;
            IPEndPoint ipEndPointAny;
            switch (addressFamily)
            {
                case AddressFamily.InterNetwork:
                    // IP v4 address
                    sockAddrSructureSize = 16;
                    ipEndPointAny = new IPEndPoint(IPAddress.Any, 0);
                    break;
                case AddressFamily.InterNetworkV6:
                    // IP v6 address
                    sockAddrSructureSize = 28;
                    ipEndPointAny = new IPEndPoint(IPAddress.IPv6Any, 0);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pSockaddrStructure), "Unknown address family");
            }


            // get bytes of the sockadrr structure
            byte[] sockAddrSructureBytes = new byte[sockAddrSructureSize];
            Marshal.Copy(pSockaddrStructure, sockAddrSructureBytes, 0, sockAddrSructureSize);

            // create SocketAddress from bytes
            var socketAddress = new SocketAddress(AddressFamily.Unspecified, sockAddrSructureSize);
            for (int i = 0; i < sockAddrSructureSize; i++)
            {
                socketAddress[i] = sockAddrSructureBytes[i];
            }

            // create IPEndPoint from SocketAddress
            IPEndPoint result = (IPEndPoint)ipEndPointAny.Create(socketAddress);
            return result;
        }

        /// <summary>
        /// Reads the unmanaged sockaddr structure returned by a WinAPI function
        /// </summary>
        /// <param name="pSockaddrStructure">pointer to the unmanaged sockaddr structure</param>
        /// <returns>IP address and port number</returns>
        private static IPEndPoint ReadSockAddrStorageStructure(SOCKADDR_STORAGE sockAddrStorageStructure)
        {
            short sAddressFamily = sockAddrStorageStructure.ss_family;
            AddressFamily addressFamily = (AddressFamily)sAddressFamily;

            int sockAddrSructureSize;
            IPEndPoint ipEndPointAny;
            switch (addressFamily)
            {
                case AddressFamily.InterNetwork:
                    // IP v4 address
                    sockAddrSructureSize = 8;
                    var socketAddress = new SocketAddress(AddressFamily.InterNetwork, sockAddrSructureSize);
                    socketAddress[0] = 2;
                    socketAddress[1] = 0;
                    for (int i = 2; i < sockAddrSructureSize; i++)
                    {
                        socketAddress[i] = sockAddrStorageStructure.__ss_pad1[i - 2];
                    }

                    ipEndPointAny = new IPEndPoint(IPAddress.Any, 0);
                    return (IPEndPoint)ipEndPointAny.Create(socketAddress);
                default:
                    throw new ArgumentOutOfRangeException(nameof(sockAddrStorageStructure), "Unknown address family");
            }
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct SHELLEXECUTEINFO
        {
            public int cbSize;
            public uint fMask;
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpVerb;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpFile;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpParameters;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpDirectory;
            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }

        private const int SW_SHOW = 5;
        private const uint SEE_MASK_INVOKEIDLIST = 12;

        internal static bool ShowFileProperties(string Filename)
        {
            SHELLEXECUTEINFO info = new SHELLEXECUTEINFO();
            info.cbSize = Marshal.SizeOf(info);
            info.lpVerb = "properties";
            info.lpFile = Filename;
            info.nShow = SW_SHOW;
            info.fMask = SEE_MASK_INVOKEIDLIST;
            return ShellExecuteEx(ref info);
        }
    }
}
