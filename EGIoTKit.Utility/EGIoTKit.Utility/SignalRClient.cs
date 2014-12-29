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
using System.Net;
using System.Collections;
using System.IO;

namespace EGIoTKit.Utility
{
    /// <summary>
    /// SinalR Helper for .NET Micro Framework
    /// Current version support join and send capability only
    /// </summary>
    class SignalRClient
    {
        public string signalRServerUri;

        public string HubName { get; set; }
        public string ModelName { get; set; }
        public string ConnectionData { get; set; }

        private bool isConnected = false;
        public bool IsConnected { get { return isConnected; } }

        public long CurrentTicks { get { return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond / 10; } }

        public SignalRClient(string serverUri)
        {
            signalRServerUri = serverUri;
            if (signalRServerUri.LastIndexOf("/") != signalRServerUri.Length - 1)
            {
                signalRServerUri += "/";
            }
        }

        private string connectionToken;
        private string connectionId;
        int order = 0;

        /// <summary>
        /// Connect to SignalR Hub
        /// </summary>
        /// <returns></returns>
        public bool Connect()
        {
            //     var req = HttpWebRequest.Create("http://wwww.bing.com");
            //   var res= req.GetResponse();
            var currentTicks = CurrentTicks;
            string entryPoint = signalRServerUri + "negotiate?connectionData=%5B%7B%22name%22%3A%22" + ConnectionData + "%22%7D%5D&clientProtocol=1.3&_=" + currentTicks;
            //   string entryPoint = signalRServerUri + "negotiate?connectionData=%5B%7B%22name%22%3A%22" + ConnectionData + "%22%7D%5D&clientProtocol=1.3";
            var request = HttpWebRequest.Create(entryPoint) as HttpWebRequest;
            request.Method = "GET";
            try
            {
                var response = request.GetResponse() as HttpWebResponse;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using (var resStream = response.GetResponseStream())
                    {
                        var reader = new StreamReader(resStream);
                        var jsonContent = reader.ReadToEnd();
                        int ctStartIndex = jsonContent.IndexOf(keyConnectionType);
                        jsonContent = jsonContent.Substring(ctStartIndex + keyConnectionType.Length + 3);
                        connectionToken = jsonContent.Substring(0, jsonContent.IndexOf("\""));
                        int ciStartIndex = jsonContent.IndexOf(keyConnectionId);
                        jsonContent = jsonContent.Substring(ciStartIndex + keyConnectionId.Length + 3);
                        connectionId = jsonContent.Substring(0, jsonContent.IndexOf("\""));

                        string ct = "";
                        int plusIndex = connectionToken.IndexOf("+");
                        if (plusIndex > 0)
                        {
                            Debug.Print("Original ConnectionToken : " + connectionToken);
                            while (plusIndex >= 0)
                            {
                                if (plusIndex > 0)
                                {
                                    ct += connectionToken.Substring(0, plusIndex);
                                    connectionToken = connectionToken.Substring(plusIndex + 1);
                                }
                                ct += "%2b";
                                plusIndex = connectionToken.IndexOf("+");
                            }
                            if (connectionToken.Length > 0)
                            {
                                ct += connectionToken;
                            }

#if false
                        char[] ctChars = connectionToken.ToCharArray();
                        foreach (var ctChar in ctChars)
                        {
                            if (ctChar == '+')
                            {
                                ct += "%2b";
                            }
                            else
                            {
                                ct += ctChar;
                            }
                        }
#endif
                            connectionToken = ct;
                            Debug.Print("-> ConnectionToken : " + connectionToken);
                        }
                        Debug.Print("Connect - Negotiate Succeeded");
                        isConnected = true;

#if false
// This code is to will be received from Server
                    // should run on other thread
                    string connectUri = signalRServerUri + "connect?transport=foreverFrame&connectionToken=" + connectionToken + "&connectionData=%5B%7B%22name%22%3A%22" + ConnectionData + "%22%7D%5D&tid=" + tidIndex + "&_=" + DateTime.Now.Ticks;
                    var requestConnectFF = HttpWebRequest.Create(connectUri) as HttpWebRequest;
                    requestConnectFF.Method = "GET";
                    
                    var responseConnectFF = requestConnectFF.GetResponse() as HttpWebResponse;
                    if (responseConnectFF.StatusCode == HttpStatusCode.OK)
                    {
                        Debug.Print("Connected");
                        order = 0;
                        isConnected = true;
                    }
                    
                    tidIndex++;
                    string connectUri = signalRServerUri + "connect?transport=longPolling&connectionToken=" + connectionToken + "&connectionData=%5B%7B%22name%22%3A%22" + ConnectionData + "%22%7D%5D&tid=" + tidIndex + "&frameId=1&_=" + DateTime.Now.Ticks;
                    var requestConnect = HttpWebRequest.Create(connectUri) as HttpWebRequest;
                    requestConnect.Method = "GET";
                    var responseConnect = requestConnect.GetResponse() as HttpWebResponse;
                    if (responseConnect.StatusCode == HttpStatusCode.OK)
                    {
                        Debug.Print("Connected");
                        order = 0;
                        isConnected = true;
                    }
#endif

                    }

                    request.ContinueDelegate = new HttpContinueDelegate(ContinueDelegateCallback);
                }
            }
            catch (Exception exe)
            {
                Debug.Print(exe.Message);
                return false;
            }
            return isConnected;
        }

        private void ContinueDelegateCallback(int StatusCode, WebHeaderCollection httpHeaders)
        {
            Debug.Print("ContinueDelegateCallback - " + StatusCode);
        }
#if false
        public bool Send(ArrayList paramOrder, Hashtable data)
        {
            bool result = false;
            if (isConnected)
            {
                try
                {
                    string sendUri = signalRServerUri + "send?transport=longPolling&connectionToken=" + connectionToken;
                    if (ConnectionData != null && ConnectionData != "")
                    {
                        sendUri += "&connectionData=%5B%7B%22name%22%3A%22" + ConnectionData + "%22%7D%5D";
                    }
                    using (var request = HttpWebRequest.Create(sendUri) as HttpWebRequest)
                    {
                        request.Method = "POST";
                        //        request.Headers.Add("User-Agent", "NETMF - Gadgeteer");
                        request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                        request.KeepAlive = true;
                        request.Headers.Add(HttpKnownHeaderNames.CacheControl, "no-cache");
                        request.UserAgent = "NETMF Gadgeteer";
                        //     request.SendChunked = true;


                        string updateContent = "{\"H\":\"" + HubName + "\",\"M\":\"" + ModelName + "\",\"A\":[{";
                        string dataPart = "";
                        foreach (var paramName in paramOrder)
                        {
                            foreach (var key in data.Keys)
                            {
                                if ((string)key == (string)paramName)
                                {
                                    if (dataPart.Length > 0)
                                    {
                                        dataPart += ",";
                                    }
                                    dataPart += "\"" + (string)key + "\":";
                                    if (data[key] is string)
                                    {
                                        dataPart += "\"" + (string)data[key] + "\"";
                                    }
                                    else
                                    {
                                        dataPart += data[key].ToString();
                                    }
                                    break;
                                }
                            }
                        }
                        updateContent += dataPart;
                        updateContent += "}],\"I\":" + order + "}";
                        string encoded = "data=" + HttpUtility.UrlEncode(updateContent);
                        SendContent = encoded;
                        byte[] content = System.Text.UTF8Encoding.UTF8.GetBytes(encoded);
                        request.ContentLength = content.Length;
                        using (var reqStream = request.GetRequestStream())
                        {
                            reqStream.Write(content, 0, content.Length);
                            var response = request.GetResponse() as HttpWebResponse;
                            if (response.StatusCode == HttpStatusCode.OK)
                            {
                                Debug.Print("Send Succeeded");

                                result = true;
                            }
                            else
                            {
                                Debug.Print("Send Failed:" + response.StatusCode);
                            }
                            if (response.ContentLength > 0)
                            {
                                using (var sendResStream = response.GetResponseStream())
                                {
                                    byte[] resContentBytes = new byte[(int)response.ContentLength];
                                    sendResStream.Read(resContentBytes, 0, (int)response.ContentLength);
                                    char[] resContentChars = System.Text.UTF8Encoding.UTF8.GetChars(resContentBytes);
                                    string resContent = new string(resContentChars);
                                    Debug.Print(resContent);
                                }
                            }
                            Debug.Print("Send:" + updateContent);
                            response.Dispose();
                            order++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print("Update Position - " + ex.Message);
                }
            }
            return result;
        }
#endif

        /// <summary>
        /// Send data packet to SignalR Hub
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool Send(Hashtable data)
        {
            bool result = false;
            if (isConnected)
            {
                try
                {
                    string sendUri = signalRServerUri + "send?transport=longPolling&connectionToken=" + connectionToken;
                    if (ConnectionData != null && ConnectionData != "")
                    {
                        sendUri += "&connectionData=%5B%7B%22name%22%3A%22" + ConnectionData + "%22%7D%5D";
                    }
                    using (var request = HttpWebRequest.Create(sendUri) as HttpWebRequest)
                    {
                        request.Method = "POST";
                        //        request.Headers.Add("User-Agent", "NETMF - Gadgeteer");
                        request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                        request.KeepAlive = true;
                        request.Headers.Add(HttpKnownHeaderNames.CacheControl, "no-cache");
                        request.UserAgent = "NETMF Gadgeteer";
                        //     request.SendChunked = true;


                        string updateContent = "{\"H\":\"" + HubName + "\",\"M\":\"" + ModelName + "\",\"A\":[{";
                        string dataPart = "";
                        foreach (var key in data.Keys)
                        {
                            if (dataPart.Length > 0)
                            {
                                dataPart += ",";
                            }
                            dataPart += "\"" + (string)key + "\":";
                            if (data[key] is string)
                            {
                                dataPart += "\"" + (string)data[key] + "\"";
                            }
                            else
                            {
                                dataPart += data[key].ToString();
                            }
                        }
                        updateContent += dataPart;
                        updateContent += "}],\"I\":" + order + "}";
                        string encoded = "data=" + HttpUtility.UrlEncode(updateContent);
                        SendContent = encoded;
                        byte[] content = System.Text.UTF8Encoding.UTF8.GetBytes(encoded);
                        request.ContentLength = content.Length;
                        using (var reqStream = request.GetRequestStream())
                        {
                            reqStream.Write(content, 0, content.Length);
                            var response = request.GetResponse() as HttpWebResponse;
                            if (response.StatusCode == HttpStatusCode.OK)
                            {
                                Debug.Print("Send Succeeded");

                                result = true;
                            }
                            else
                            {
                                Debug.Print("Send Failed:" + response.StatusCode);
                                Debug.Print(" Data:" + updateContent);
                            }

                            if (response.ContentLength > 0)
                            {
                                using (var sendResStream = response.GetResponseStream())
                                {
                                    byte[] resContentBytes = new byte[(int)response.ContentLength];
                                    sendResStream.Read(resContentBytes, 0, (int)response.ContentLength);
                                    char[] resContentChars = System.Text.UTF8Encoding.UTF8.GetChars(resContentBytes);
                                    string resContent = new string(resContentChars);
                                    Debug.Print(resContent);
                                }
                            }
                            response.Dispose();
                            order++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print("Update Position - " + ex.Message);
                }
            }
            GC.WaitForPendingFinalizers();
            return result;
        }

        public string SendContent { get; set; }

        private readonly string keyConnectionType = "ConnectionToken";
        private readonly string keyConnectionId = "ConnectionId";
    }
}
