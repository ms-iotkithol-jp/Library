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
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace EGIoTKit.Utility
{
    /// <summary>
    /// UDP Multicast communication utility
    /// </summary>
    public class MulticastClient
    {
        private Socket mySocket;
        private IPAddress localAddr;
        private IPAddress multicastAddr;
        private int multicastPort;
        private IPEndPoint multicastEP;
        private int _ttl = 1;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="localAddress"></param>
        public MulticastClient(IPAddress localAddress)
        {
            localAddr = localAddress;

            mySocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        public int TTL { get { return _ttl; } set { _ttl = value; } }

        /// <summary>
        /// Join to specified UDP Multicast Group
        /// </summary>
        /// <param name="groupAddress">224.0.0.1 - 239.255.255.255 </param>
        /// <param name="groupPort"></param>
        public void JoinGroup(IPAddress groupAddress, int groupPort)
        {
            multicastAddr = groupAddress;
            multicastPort = groupPort;

            #region Setting for Send
            byte[] multicastAddrBytes = multicastAddr.GetAddressBytes();
            byte[] ipAddrBytes = IPAddress.Any.GetAddressBytes();
            byte[] multicastOpt = new byte[]
            {
                multicastAddrBytes[0],multicastAddrBytes[1],multicastAddrBytes[2],multicastAddrBytes[3],
                ipAddrBytes[0],ipAddrBytes[1],ipAddrBytes[2],ipAddrBytes[3]
            };
            // WsDiscovery Multicast Address: 239.255.255.250

            mySocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, multicastOpt);
            if (_ttl > 1)
            {
                mySocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, _ttl);
            }
            multicastEP = new IPEndPoint(multicastAddr, multicastPort);

            #endregion

            var thread = new Thread(MCCommReceive);
            thread.Start();
            Debug.Print("Multicast Group : Joined " + groupAddress.ToString() + ":" + groupPort);
        }

        /// <summary>
        /// Send message to joined Multicast Group
        /// </summary>
        /// <param name="msg"></param>
        public void SendMessage(string msg)
        {
            if (joinFlag)
            {
                byte[] msgBytes = System.Text.UTF8Encoding.UTF8.GetBytes(msg);
                int len = msgBytes.Length;
                if (len > RECEIVEBUFSIZE - 2)
                {
                    throw new ArgumentOutOfRangeException("Message length should be less than " + (RECEIVEBUFSIZE - 2) + " bytes");
                }
                byte[] dataBytes = new byte[2 + len];
                dataBytes[0] = (byte)(len >> 8);
                dataBytes[1] = (byte)(len & 0xff);
                msgBytes.CopyTo(dataBytes, 2);
                mySocket.SendTo(dataBytes, dataBytes.Length, SocketFlags.None, multicastEP);
                //    GC.WaitForPendingFinalizers();

            }
        }
        /// <summary>
        /// Send long message to Multicast Group
        /// The message will be sent as divided packet
        /// </summary>
        /// <param name="msgId"></param>
        /// <param name="data"></param>
        public void SendLongMessage(byte[] msgId, byte[] data)
        {
            int blockSize = 1400 - msgId.Length - 4;
            byte[] dataBytes = new byte[8 + msgId.Length];
            dataBytes[0] = 0xff;
            dataBytes[1] = 0xff;
            dataBytes[2] = (byte)(blockSize >> 8);
            dataBytes[3] = (byte)(blockSize & 0xff);
            int blockNo = data.Length / blockSize;
            int lastBlockSize = data.Length % blockSize;
            if ((data.Length % blockSize) != 0)
            {
                blockNo++;
            }
            dataBytes[4] = (byte)(blockNo >> 8);
            dataBytes[5] = (byte)(blockNo & 0xff);
            dataBytes[6] = (byte)(lastBlockSize >> 8);
            dataBytes[7] = (byte)(lastBlockSize & 0xff);
            Array.Copy(msgId, 0, dataBytes, 8, msgId.Length);

            lmMsgId = new byte[msgId.Length];
            Array.Copy(msgId, 0, lmMsgId, 0, msgId.Length);
            lmDataBytes = new byte[data.Length];
            Array.Copy(data, 0, lmDataBytes, 0, data.Length);
            lmBlockSize = blockSize;
            lmLastSize = lastBlockSize;
            var thread = new Thread(SendLongMsgFrags);
            thread.Start();

            mySocket.SendTo(dataBytes, dataBytes.Length, SocketFlags.None, multicastEP);

            thread.Join();
        }

        int lmRestSize;
        int lmBlockSize;
        int lmLastSize;
        byte[] lmMsgId;
        byte[] lmDataBytes;
        System.Threading.AutoResetEvent ackWaiting = new AutoResetEvent(false);
        void SendLongMsgFrags()
        {
            lmRestSize = lmDataBytes.Length;
            int count = 0;
            int basePosition = 0;
            while (lmRestSize > 0)
            {
                ackWaiting.WaitOne();
                int bufSize = lmBlockSize;
                if (lmRestSize < lmBlockSize)
                {
                    bufSize = lmLastSize;
                }
                var dataBytes = new byte[bufSize + 4 + lmMsgId.Length];
                dataBytes[0] = 0xff;
                dataBytes[1] = 0xfe;
                dataBytes[2] = (byte)(count >> 8);
                dataBytes[3] = (byte)(count & 0xff);
                int baseIndex = 4;
                Array.Copy(lmMsgId, 0, dataBytes, baseIndex, lmMsgId.Length);
                baseIndex += lmMsgId.Length;
                Array.Copy(lmDataBytes, basePosition, dataBytes, baseIndex, bufSize);

                mySocket.SendTo(dataBytes, dataBytes.Length, SocketFlags.None, multicastEP);
                lmRestSize -= bufSize;
                basePosition += bufSize;
                count++;
            }
        }

        int RECEIVEBUFSIZE = 1024;

        bool joinFlag = false;
        void MCCommReceive()
        {
            var receiveSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            receiveSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);
            receiveSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            //    receiveSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, false);
            IPEndPoint localEP = new IPEndPoint(IPAddress.Any, multicastPort);
            receiveSocket.Bind(localEP);
            var rbuf = receiveSocket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer);

            byte[] multicastAddrBytes = multicastAddr.GetAddressBytes();
            byte[] ipAddrBytes = IPAddress.Any.GetAddressBytes();
            byte[] multicastOpt = new byte[]
            {
               multicastAddrBytes[0], multicastAddrBytes[1], multicastAddrBytes[2], multicastAddrBytes[3],    // WsDiscovery Multicast Address: 239.255.255.250
                 ipAddrBytes       [0], ipAddrBytes       [1], ipAddrBytes       [2], ipAddrBytes       [3]
            };
            receiveSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, multicastOpt);

            //            mySocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, ipAddrBytes);
            EndPoint senderEP = new IPEndPoint(multicastAddr, multicastPort);
            int len = 0;
            byte[] dataBytes = new byte[RECEIVEBUFSIZE];
            bool mcgJoining = false;
            lock (this)
            {
                joinFlag = true;
                mcgJoining = joinFlag;
            }
            while (mcgJoining)
            {
                try
                {
                    len = receiveSocket.Receive(dataBytes, dataBytes.Length, SocketFlags.None);
                    if (dataBytes[0] == 0xff)
                    {
                        if (dataBytes[1] == 0xfa)
                        {
                            var ackMsgId = new byte[len - 2];
                            Array.Copy(dataBytes, 2, ackMsgId, 0, len - 2);
                            ackWaiting.Set();
                        }
                    }
                    else
                    {
                        len = ((int)(dataBytes[0]) << 8) + dataBytes[1];
                        byte[] buf = new byte[len];
                        for (int i = 0; i < len; i++)
                        {
                            buf[i] = dataBytes[i + 2];
                        }
                        if (OnMulticastMessageReceived != null)
                        {
                            OnMulticastMessageReceived(buf, len, (IPEndPoint)senderEP);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print(ex.Message);
                }
                lock (this)
                {
                    mcgJoining = joinFlag;
                }
            }

            receiveSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropMembership, multicastOpt);
        }

        /// <summary>
        /// Event handler to receive messages from joined Multicast Group.
        /// </summary>
        public event OnMulticastMessageReceivedDelegate OnMulticastMessageReceived;
        public delegate void OnMulticastMessageReceivedDelegate(byte[] msg, int len, IPEndPoint sender);
    }
}
