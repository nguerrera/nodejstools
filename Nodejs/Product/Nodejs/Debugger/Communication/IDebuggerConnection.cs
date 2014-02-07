﻿/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the Apache License, Version 2.0, please send an email to 
 * vspython@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/

using System;
using System.Threading.Tasks;

namespace Microsoft.NodejsTools.Debugger.Communication {
    interface IDebuggerConnection {
        /// <summary>
        /// Gets a value indicating whether connection established.
        /// </summary>
        bool Connected { get; }

        /// <summary>
        /// Connect to debugger.
        /// </summary>
        void Connect();

        /// <summary>
        /// Connect to specified debugger endpoint.
        /// </summary>
        /// <param name="host">Host address.</param>
        /// <param name="port">Port number.</param>
        void Connect(string host, ushort port);

        /// <summary>
        /// Close connection.
        /// </summary>
        void Close();

        /// <summary>
        /// Send a message.
        /// </summary>
        /// <param name="message">Message.</param>
        Task SendMessageAsync(string message);

        /// <summary>
        /// Fired when received inbound message.
        /// </summary>
        event EventHandler<MessageEventArgs> OutputMessage;

        /// <summary>
        /// Fired when connection was closed.
        /// </summary>
        event EventHandler<EventArgs> ConnectionClosed;
    }
}