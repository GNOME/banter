/***************************************************************************
 *  Enum.cs
 *
 *  Copyright (C) 2006 INdT
 *  Written by
 *      Andre Moreira Magalhaes <andre.magalhaes@indt.org.br>
 *      Kenneth Christiansen <kenneth.christiansen@gmail.com>
 *      Renato Araujo Oliveira Filho <renato.filho@indt.org>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW:
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the Software),
 *  to deal in the Software without restriction, including without limitation
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,
 *  and/or sell copies of the Software, and to permit persons to whom the
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 *  DEALINGS IN THE SOFTWARE.
 */

using System;

namespace Tapioca
{
	public enum ContactPresence : uint
	{
		Offline = 1,
		Available = 2,
		Away = 3,
		XA = 4,
		Hidden = 5,
		Busy = 6
	}
	
	public enum ContactSubscriptionStatus : uint
	{
		NotSubscribed = 0,
		RemotePending = 1,
		Subscribed = 2
	}
	
	public enum ContactAuthorizationStatus : uint
	{
		NonExistent = 0,
		LocalPending = 1,
		Authorized = 2
	}

    public enum  ConnectionStatus : uint
    {
        Connected = 0,
        Connecting = 1,
        Disconnected = 2
    }

    public enum ConnectionStatusReason : uint
    {
       	NoneSpecified = 0,
        Requested = 1,
        NetworkError = 2,
        AuthenticationFailed = 3,
        EncryptionError = 4,
        NameInUse = 5,
        CertificateNotProviede = 6,
        CertificateUntrusted = 7,
        CertificateExpired = 8,
        CertificateNotActivated = 9,
        CertificateHostnameMismatch = 10,
        CertificateFigerprintMismatch = 11,
        CertificateSelfSigned = 12,
        CertificateOtherError = 13
    }
    
    public enum ChannelType : uint
	{
		Text = 1,
		StreamedMedia = 2
	}

	public enum TextChannelMessageDeliveryError : uint
	{
		Unknown = 0,
		ContactOffline = 1,
		InvalidContact = 2,
		PermissionDenied = 3,
		MessageTooLong = 4
	}
	
	public enum TextChannelMessageType : uint
	{
    		Normal,
           	Action,
           	Notice,
           	AutoReply
	}
	
	public enum StreamState : uint
	{
	      Stopped = 0,
	      Playing = 1,
	      Connecting = 2,
	      Connected = 3
	}	
	
	public enum StreamType : uint 
	{
		Audio = 0,
		Video = 1		
	}
	
	public enum ContactCapabilities : uint 
	{
		None = 0,
		Text = 1,
		Audio = 2,
		Video = 4
	}
}
