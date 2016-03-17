﻿using Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UniActionsCore.ScenarioCreation;

namespace UniActionsCore
{
    public class ServerThreading
    {
        public class ServerThreadingSettings
        {
            public static class Defaults
            {
                public static readonly ushort DistributionPort = 6001;
                public static readonly bool ResolveAll = true;
                public static readonly uint ReceiveTimout = 1000;
                public static readonly uint SendTimout = 1000;

                public static readonly IEnumerable<ushort> ActionPorts = new List<ushort>() {
                    6002,6003,6004,6005,6006,6007,6008,6009,6010
                };

                public static readonly uint SharingTryCount = 4;
                public static readonly ushort SharingPort = 6000;
                public static readonly Encoding ServerEncoding = Encoding.UTF8;
            }

            public ushort _distributionPort;
            public ushort DistributionPort
            {
                get
                {
                    return _distributionPort;
                }
                set
                {
                    if (value != _sharingPort)
                        if (ActionsPorts == null || !ActionsPorts.Contains(value))
                            _distributionPort = value;
                }
            }

            public bool ResolveAllIp { get; set; }

            public SkeddedList<IPAddress> ResolvedIp { get; internal set; }

            private SkeddedList<ushort> _actionsPorts;
            public SkeddedList<ushort> ActionsPorts
            {
                get
                {
                    return _actionsPorts;
                }
                internal set
                {
                    value.ItemAdd += (sender, args) =>
                    {
                        if (args.Item == this.DistributionPort ||
                            args.Item == Defaults.SharingPort ||
                            _actionsPorts.Any(x => x.Equals(args.Item))
                            )
                            args.Cancel = true;
                    };

                    _actionsPorts = value;
                }
            }

            public uint SharingTryCount { get; set; }

            public ushort _sharingPort;
            public ushort SharingPort
            {
                get
                {
                    return _sharingPort;
                }
                set
                {
                    if (value != DistributionPort)
                        if (ActionsPorts == null || !ActionsPorts.Contains(value))
                            _sharingPort = value;
                }
            }
        }

        private class ThreadPortOccupation
        {
            public ThreadPortOccupation(Thread t, TcpListenerEx listener, ushort port)
            {
                Thread = t;
                Port = port;
                Listener = listener;
            }

            public Thread Thread { get; private set; }
            public TcpListenerEx Listener { get; private set; }
            public ushort Port { get; private set; }
            public bool IsOccupiedByClient { get; set; }
        }

        public Uni Uni { get; internal set; }

        public ServerThreadingSettings Settings { get; private set; }

        private volatile List<ThreadPortOccupation> _threadPortOccupations;

        public event Action WhenStopped;

        private volatile bool _prepareToStop;
        private Action _whenStoppedCallback;
        public VoidResult BeginStop(Action callback)
        {
            _whenStoppedCallback = callback;

            var result = new VoidResult();
            try
            {
                _prepareToStop = true;

                _listenerPortDistribution.Stop();

                _threadPortOccupations.ToList().ForEach(x =>
                {
                    if (!x.IsOccupiedByClient)
                    {
                        x.Listener.Stop();
                    }
                });
            }
            catch (Exception e)
            {
                result.AddException(e);
            }

            return result;
        }
        private bool _isStopped;
        public bool IsStopped
        {
            get
            {
                return _isStopped;
            }
            private set
            {
                _isStopped = value;
                if (value && _whenStoppedCallback != null)
                {
                    _whenStoppedCallback();
                    _whenStoppedCallback = null;
                }
                if (value && WhenStopped != null)
                    WhenStopped();
            }
        }

        private void SendString(NetworkStream stream, string str)
        {
            var bytesToSend = ServerThreadingSettings.Defaults.ServerEncoding.GetBytes(str);
            stream.WriteByte((byte)bytesToSend.Length);
            stream.Write(bytesToSend, 0, bytesToSend.Length);
        }

        private string GetNextString(NetworkStream stream)
        {
            var len = stream.ReadByte();
            var buff = new byte[len];
            stream.Read(buff, 0, buff.Length);
            return ServerThreadingSettings.Defaults.ServerEncoding.GetString(buff);
        }

        public void Initialize()
        {
            this.Settings = new ServerThreadingSettings();
            this.Settings.ResolvedIp = new SkeddedList<IPAddress>();
            this.Settings.ActionsPorts = SkeddedList<ushort>.Create(ServerThreadingSettings.Defaults.ActionPorts);
            this.Settings.SharingPort = ServerThreadingSettings.Defaults.SharingPort;
            this.Settings.SharingTryCount = ServerThreadingSettings.Defaults.SharingTryCount;
            IsStopped = true;
        }

        private TcpListenerEx _listenerPortDistribution;
        private Thread _threadPortDistribution;
        private List<IPAddress> _activeClients;

        public VoidResult BeginStart()
        {
            VoidResult result = new VoidResult();

            if (!IsStopped)
            {
                result.AddException(new Exception("Is started now"));
                return result;
            }

            if (_threadPortOccupations == null)
                _threadPortOccupations = new List<ThreadPortOccupation>();

            if (_activeClients == null)
                _activeClients = new List<IPAddress>();

            IsStopped = false;
            _prepareToStop = false;

            _threadPortDistribution = ThreadHelper.AlterThread(() =>
            {
                _listenerPortDistribution = new TcpListenerEx(Settings.DistributionPort);
                _listenerPortDistribution.Start();
                while (!_prepareToStop)
                {
                    try
                    {
                        using (var client = _listenerPortDistribution.AcceptTcpClient())
                        {
                            var address = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
                            if (!_activeClients.Any(x => x.Equals(address)))
                                _activeClients.Add(address);

                            ThreadPortOccupation occupation = null;

                            while (occupation == null)
                            {
                                occupation = _threadPortOccupations.FirstOrDefault(x => !x.IsOccupiedByClient);
                                if (occupation == null)
                                    Thread.Sleep(10);
                            }

                            SendString(client.GetStream(), occupation.Port.ToString());
                        }
                    }
                    catch (Exception e)
                    {
                        if (e is SocketException && e.Message.Contains("WSACancelBlockingCall"))
                        {
                            //do nothing
                        }
                        else
                            Log.Write(e);
                    }
                }
            });

            for (int i = 0; i < Settings.ActionsPorts.Count; i++)
            {
                try
                {
                    var port = Settings.ActionsPorts[i];

                    var listener = new TcpListenerEx(port);

                    listener.Server.SendTimeout = (int)ServerThreadingSettings.Defaults.SendTimout;
                    listener.Server.ReceiveTimeout = (int)ServerThreadingSettings.Defaults.ReceiveTimout;
                    ThreadPortOccupation portOccupation = null;
                    Thread t = new Thread(() =>
                    {
                        while (true)
                        {
                            try
                            {
                                using (TcpClient client = listener.AcceptTcpClient())
                                {
                                    portOccupation.IsOccupiedByClient = true;

                                    if (Settings.ResolvedIp != null && Settings.ResolvedIp.Any() && !Settings.ResolveAllIp)
                                    {
                                        lock (Settings.ResolvedIp)
                                        {
                                            var ip = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
                                            if (!Settings.ResolvedIp.Where(x => x.Equals(ip)).Any())
                                            {
                                                client.Close();
                                                throw new Exception("Not resolved ip: " + ip);
                                            }
                                        }
                                    }

                                    var stream = client.GetStream();
                                    var command = GetNextString(stream);

                                    CommandHandling(client, stream, command);
                                }
                            }
                            catch (Exception e)
                            {
                                if (e is SocketException && e.Message.Contains("WSACancelBlockingCall"))
                                {
                                    //do nothing
                                }
                                else
                                    Log.Write(e);
                            }

                            portOccupation.IsOccupiedByClient = false;

                            if (!listener.IsActive)
                            {
                                lock (_threadPortOccupations)
                                {
                                    _threadPortOccupations.RemoveAll(x => x.Port == port);

                                    if (!_threadPortOccupations.Any())
                                        IsStopped = true;
                                }
                                break;
                            }
                        }
                    });

                    _threadPortOccupations.Add(portOccupation = new ThreadPortOccupation(t, listener, port));
                    t.IsBackground = false;
                    listener.Start();
                    t.Start();
                }
                catch (Exception e)
                {
                    result.AddException(e);
                }
            }

            if (ServerStarted != null)
                ServerStarted();

            return result;
        }

        private void CommandHandling(TcpClient client, NetworkStream stream, string command)
        {
            if (command == VAC.ServerCommands.Command_GetStartCommands)
            {
                var fastActions = Uni.TasksPool.Scenarios
                    .Where(x => string.IsNullOrEmpty(x.Category) && x.UseServerThreading && !string.IsNullOrEmpty(x.ServerCommand))
                    .OrderBy(x => x.Index)
                    .OrderBy(x => x.Name);
                var categories = Uni.TasksPool.Scenarios
                    .Where(x => !string.IsNullOrEmpty(x.Category) && !string.IsNullOrEmpty(x.ServerCommand) && x.UseServerThreading).Select(x => x.Category)
                    .Distinct()
                    .OrderBy(x => x);
                SendString(stream, (fastActions.Count() + categories.Count()).ToString());
                foreach (var action in fastActions)
                {
                    SendString(stream, action.ServerCommand);
                    SendString(stream, action.CheckState());
                }

                SendString(stream, VAC.ServerCommands.Command_EndFastActions);

                foreach (var category in categories)
                {
                    SendString(stream, category);
                }
            }
            else if (command == VAC.ServerCommands.Command_GetCategoryCommands)
            {
                var category = GetNextString(stream);

                var actions = Uni.TasksPool.Scenarios
                    .Where(x => x.Category == category && x.UseServerThreading && !string.IsNullOrEmpty(x.ServerCommand))
                    .OrderBy(x => x.Index)
                    .OrderBy(x => x.Name);

                SendString(stream, actions.Count().ToString());

                foreach (var action in actions)
                {
                    SendString(stream, action.ServerCommand);
                    SendString(stream, action.CheckState());
                }
            }
            else if (command == VAC.ServerCommands.Command_GetStatus)
            {
                var actions = Uni.TasksPool.Scenarios
                    .Where(x => x.UseServerThreading && !string.IsNullOrEmpty(x.ServerCommand));
                SendString(stream, actions.Count().ToString());
                foreach (var action in Uni.TasksPool.Scenarios)
                {
                    SendString(stream, action.ServerCommand);
                    SendString(stream, action.CheckState());
                }
            }
            else
            {
                var remoteAction = Uni.TasksPool.Scenarios.Where(x => x.ServerCommand == command).FirstOrDefault();
                if (remoteAction != null)
                {
                    var state = GetNextString(stream);
                    state = remoteAction.Execute(state, false);
                    SendString(stream, state);
                    ShareState(remoteAction, ((IPEndPoint)client.Client.RemoteEndPoint).Address);
                }
            }
        }

        public void ShareState(Scenario exceptItem, IPAddress exceptAddress)
        {
            foreach (var address in _activeClients.ToArray())
            {
                new Thread(() =>
                {
                    try
                    {
                        var tcpClient = new TcpClient(address.ToString(), this.Settings.SharingPort);
                        tcpClient.ReceiveTimeout = (int)ServerThreadingSettings.Defaults.ReceiveTimout;
                        tcpClient.SendTimeout = (int)ServerThreadingSettings.Defaults.SendTimout;

                        var stream = tcpClient.GetStream();

                        SendString(stream, VAC.ServerCommands.Command_NeedUpdate);

                        if (exceptItem != null && exceptAddress != null && address.Equals(exceptAddress))
                        {
                            SendString(stream, VAC.ServerCommands.Command_Except);
                            SendString(stream, exceptItem.ServerCommand);
                        }
                        else
                            SendString(stream, VAC.ServerCommands.Command_All);
                    }
                    catch
                    {
                        _activeClients.Remove(address);
                    }
                })
                {
                    IsBackground = true
                }.Start();
            }
        }

        public void ShareState()
        {
            ShareState(null, null);
        }

        public event Action ServerStarted;
    }
}
