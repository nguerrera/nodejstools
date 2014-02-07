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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.NodejsTools.Debugger.Commands;
using Microsoft.NodejsTools.Debugger.Events;
using Newtonsoft.Json.Linq;

namespace Microsoft.NodejsTools.Debugger.Communication {
    sealed class DebuggerClient : IDebuggerClient {
        private readonly IDebuggerConnection _connection;

        private ConcurrentDictionary<int, TaskCompletionSource<JObject>> _messages =
            new ConcurrentDictionary<int, TaskCompletionSource<JObject>>();

        public DebuggerClient(IDebuggerConnection connection) {
            _connection = connection;
            _connection.OutputMessage += OnOutputMessage;
        }

        public async Task SendRequestAsync(IDebuggerCommand command, CancellationToken cancellationToken = new CancellationToken()) {
            TaskCompletionSource<JObject> promise = _messages.GetOrAdd(command.Id, i => new TaskCompletionSource<JObject>());
            await _connection.SendMessageAsync(command.ToString()).ConfigureAwait(false);

            JObject response = await promise.Task.ConfigureAwait(false);
            command.ProcessResponse(response);

            _messages.TryRemove(command.Id, out promise);
        }

        public event EventHandler<BreakpointEventArgs> BreakpointEvent;
        public event EventHandler<CompileScriptEventArgs> CompileScriptEvent;
        public event EventHandler<ExceptionEventArgs> ExceptionEvent;

        private void OnOutputMessage(object sender, MessageEventArgs args) {
            JObject message;

            try {
                message = JObject.Parse(args.Message);
            }
            catch (Exception e) {
                Debug.Fail(string.Format("Invalid event message: {0}", e));
                return;
            }

            switch ((string)message["type"]) {
                case "event":
                    HandleEventMessage(message);
                    break;

                case "response":
                    HandleResponseMessage(message);
                    break;

                default:
                    Debug.Print("Unrecognized message type: {0}", message);
                    break;
            }
        }

        /// <summary>
        /// Handles event message.
        /// </summary>
        /// <param name="message">Message.</param>
        private void HandleEventMessage(JObject message) {
            switch ((string)message["event"]) {
                case "afterCompile":
                    EventHandler<CompileScriptEventArgs> compileScriptHandler = CompileScriptEvent;
                    if (compileScriptHandler != null) {
                        var compileScriptEvent = new CompileScriptEvent(message);
                        compileScriptHandler(this, new CompileScriptEventArgs(compileScriptEvent));
                    }
                    break;

                case "break":
                    EventHandler<BreakpointEventArgs> breakpointHandler = BreakpointEvent;
                    if (breakpointHandler != null) {
                        var breakpointEvent = new BreakpointEvent(message);
                        breakpointHandler(this, new BreakpointEventArgs(breakpointEvent));
                    }
                    break;

                case "exception":
                    EventHandler<ExceptionEventArgs> exceptionHandler = ExceptionEvent;
                    if (exceptionHandler != null) {
                        var exceptionEvent = new ExceptionEvent(message);
                        exceptionHandler(this, new ExceptionEventArgs(exceptionEvent));
                    }
                    break;

                default:
                    var errorMessage = (string)message["message"];
                    ConcurrentDictionary<int, TaskCompletionSource<JObject>> messages =
                        Interlocked.Exchange(ref _messages, new ConcurrentDictionary<int, TaskCompletionSource<JObject>>());

                    foreach (var kv in messages) {
                        kv.Value.SetException(new Exception(errorMessage));
                    }

                    messages.Clear();
                    Debug.Print("Unrecognized event type: {0}", message);
                    break;
            }
        }

        /// <summary>
        /// Handles response message.
        /// </summary>
        /// <param name="message">Message.</param>
        private void HandleResponseMessage(JObject message) {
            TaskCompletionSource<JObject> promise;

            var messageId = (int)message["request_seq"];
            if (!_messages.TryGetValue(messageId, out promise)) {
                Debug.Print("Invalid response message identifier {0}: {1}", messageId, message);
                return;
            }

            promise.SetResult(message);
        }
    }
}