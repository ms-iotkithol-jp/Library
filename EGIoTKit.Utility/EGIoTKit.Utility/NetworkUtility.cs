//  --------------------------------------------------------------------------------- 
//  Copyright (c) Microsoft Corporation  All rights reserved. 
//  
// Microsoft Public License (Ms-PL)
// 
// This license governs use of the accompanying software. If you use the software, you accept this license. If you do not accept the license, do not use the software.
// 
// 1. Definitions
// 
// The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under U.S. copyright law.
// 
// A "contribution" is the original software, or any additions or changes to the software.
// 
// A "contributor" is any person that distributes its contribution under this license.
// 
// "Licensed patents" are a contributor's patent claims that read directly on its contribution.
// 
// 2. Grant of Rights
// 
// (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
// 
// (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.
// 
// 3. Conditions and Limitations
// 
// (A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
// 
// (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically.
// 
// (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.
// 
// (D) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.
// 
// (E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.
// 
// Written by Hiroshi Ota 
// Twitter http://www.twitter.com/embedded_george
// Blog    http://blogs.msdn.com/hirosho
//  --------------------------------------------------------------------------------- 
using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Net.NetworkInformation;
using System.Threading;

namespace EGIoTKit.Utility
{
    /// <summary>
    /// Helper class for connecting internet
    /// var ipAddress = Networkutility.Current.SetupNetwork();
    /// </summary>
    public class NetworkUtility
    {
        private static NetworkUtility instance;
        public static NetworkUtility Current
        {
            get
            {
                if (instance == null)
                {
                    instance = new NetworkUtility();
                }
                return instance;
            }
        }

        private string ipAddress = "0.0.0.0";
        public string IPAddress { get { return ipAddress; } set { ipAddress = value; } }
        private bool isNetworkConnected = false;
        public bool IsNetworkConnected { get { return isNetworkConnected; } }

        /// <summary>
        /// The count to try to get meaningfull ip address in SetupNetwork method calling.
        /// </summary>
        public int ConnectTryCount { get; set; }

        public NetworkUtility()
        {
            ConnectTryCount = 10;
        }

        public string SetupNetwork()
        {
            Thread.Sleep(500);
            var nifs = NetworkInterface.GetAllNetworkInterfaces();
            var enetifs = new NetworkInterface[nifs.Length];
            int enetifIndex = 0;
            foreach (var ni in nifs)
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    ipAddress = ni.IPAddress;
                    if (ipAddress != null && ipAddress != "0.0.0.0")
                    {
                        break;
                    }
                    enetifs[enetifIndex++] = ni;
                    FixIPAddress(ni);
                }
            }
            if (ipAddress != "0.0.0.0")
            {
                isNetworkConnected = true;
            }
            else
            {
                int tryCount = 1;
                while (tryCount < ConnectTryCount && ipAddress == "0.0.0.0")
                {
                    foreach (var ni in enetifs)
                    {
                        if (ni != null)
                        {
                            Thread.Sleep(500);
                            ipAddress = ni.IPAddress;
                        }
                    }
                    tryCount++;
                }
            }
            return ipAddress;
        }

        private void FixIPAddress(NetworkInterface ni)
        {
            if (ni.IsDhcpEnabled)
            {
                ni.RenewDhcpLease();
                Thread.Sleep(500);
                ipAddress = ni.IPAddress;
                if (ipAddress == "0.0.0.0")
                {
                    ipAddress = ni.IPAddress;
                }
            }
            else
            {
                ni.EnableDhcp();
                Thread.Sleep(500);
                ipAddress = ni.IPAddress;
            }
        }
    }
}
