﻿using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TickTrader.Algo.Api;
using TickTrader.Algo.Core;
using TickTrader.Toaster.Messages;

namespace TickTrader.BotTerminal
{
    internal class NotificationsViewModel : PropertyChangedBase
    {
        private IAccountInfoProvider _accountInfo;
        private ConnectionManager _connectionModel;
        private INotificationCenter _notificationCenter;
        private ProfileManager _profileManager;


        public bool SoundsEnabled
        {
            get { return _notificationCenter.SoundNotification.Enabled; }
            private set { ToggleSounds(value, false); }
        }

        public bool NotificationsEnabled
        {
            get { return _notificationCenter.PopupNotification.Enabled; }
            private set { ToggleNotifications(value, false); }
        }


        public NotificationsViewModel(INotificationCenter notificationCenter, IAccountInfoProvider accountInfo, ConnectionManager connectionManager, PersistModel storage)
        {
            _accountInfo = accountInfo;
            _connectionModel = connectionManager;
            _notificationCenter = notificationCenter;
            _profileManager = storage.ProfileManager;

            _profileManager.ProfileUpdated += LoadSettings;

            LoadSettings();
        }


        private void LoadSettings()
        {
            ToggleSounds(_profileManager.CurrentProfile.Settings.EnableSounds, true);
            ToggleNotifications(_profileManager.CurrentProfile.Settings.EnableNotifications, true);
        }

        private void ToggleSounds(bool enableSounds, bool isInit)
        {
            if (_notificationCenter.SoundNotification.Enabled == enableSounds)
                return;

            if (enableSounds)
            {
                _connectionModel.StateChanged += ConnectionStateChanged;
            }
            else
            {
                _connectionModel.StateChanged -= ConnectionStateChanged;
            }

            _notificationCenter.SoundNotification.Enabled = enableSounds;
            NotifyOfPropertyChange(nameof(SoundsEnabled));

            if (!isInit)
            {
                _profileManager.CurrentProfile.Settings.EnableSounds = enableSounds;
                _profileManager.CurrentProfile.Save();
            }
        }

        private void ToggleNotifications(bool enableNotifications, bool isInit)
        {
            if (_notificationCenter.PopupNotification.Enabled == enableNotifications)
                return;

            if (enableNotifications)
            {
                _accountInfo.OrderUpdated += PopupNotificationOnOrderUpdated;
                _accountInfo.PositionUpdated += PopupNotificationOnPositionUpdated;
            }
            else
            {
                _accountInfo.OrderUpdated -= PopupNotificationOnOrderUpdated;
                _accountInfo.PositionUpdated -= PopupNotificationOnPositionUpdated;
            }

            _notificationCenter.PopupNotification.Enabled = enableNotifications;
            NotifyOfPropertyChange(nameof(NotificationsEnabled));

            if (!isInit)
            {
                _profileManager.CurrentProfile.Settings.EnableNotifications = enableNotifications;
                _profileManager.CurrentProfile.Save();
            }
        }

        private void ConnectionStateChanged(ConnectionManager.States oldState, ConnectionManager.States newState)
        {
            if (newState == ConnectionManager.States.Online)
            {
                _notificationCenter.SoundNotification.Notify(AppSounds.Positive);
            }
            else if (newState == ConnectionManager.States.Offline)
            {
                _notificationCenter.SoundNotification.Notify(AppSounds.NegativeLong);
            }
        }

        private void SoundNotificationOnConnected()
        {
            _notificationCenter.SoundNotification.Notify(AppSounds.Positive);
        }

        private void PopupNotificationOnOrderUpdated(OrderExecReport obj)
        {
            var message = NotificationBuilder.BuildMessage(obj);
            if (!message.IsEmpty)
                _notificationCenter.PopupNotification.Notify(new InfoMessage(message.Header, message.Body));
        }

        private void PopupNotificationOnPositionUpdated(PositionExecReport obj)
        {
            var message = NotificationBuilder.BuildMessage(obj);
            if (!message.IsEmpty)
                _notificationCenter.PopupNotification.Notify(new InfoMessage(message.Header, message.Body));
        }
    }


    public class NotificationBuilder
    {
        public class Message
        {
            public string Body { get; private set; }

            public string Header { get; private set; }

            public bool IsEmpty => string.IsNullOrWhiteSpace(Body) && string.IsNullOrWhiteSpace(Header);


            public Message(string header, string body)
            {
                Header = header;
                Body = body;
            }
        }


        public static Message BuildMessage(OrderExecReport obj)
        {
            var order = obj.OrderCopy;
            string header = "";
            string body = "";
            switch (obj.ExecAction)
            {
                case OrderExecAction.Opened:
                    switch (order.Type)
                    {
                        case OrderType.Position:
                            header = $"Order {obj.OrderId} Filled at {obj.OrderCopy.Price}";
                            body = $"Your request to {order.Side} {order.LastFillVolume} lots of {order.Symbol} was filled at {order.LastFillPrice}. Order #{order.Id}";
                            break;
                        case OrderType.Limit:
                        case OrderType.Stop:
                            header = $"Order {obj.OrderId} Placed at {order.Price}";
                            body = $"Your order #{order.Id} {order.Side} {order.Type} order for {order.RemainingVolume} lots of {order.Symbol} at {order.Price} was placed.";
                            break;
                    }
                    break;
                case OrderExecAction.Modified:
                    switch (order.Type)
                    {
                        case OrderType.Position:
                            header = $"Order {obj.OrderId} Modified";
                            body = $"Order #{order.Id} {order.Side} {order.RemainingVolume} lots of {order.Symbol} at {order.Price} was modified.";
                            break;
                        case OrderType.Limit:
                        case OrderType.Stop:
                            header = $"Order {obj.OrderId} Modified";
                            body = $"Order #{order.Id} {order.Side} {order.Type} {order.RemainingVolume} lots of {order.Symbol} at {order.Price} was modified.";
                            break;
                    }
                    break;
                case OrderExecAction.Closed:
                    if (order.Type == Algo.Api.OrderType.Position)
                    {
                        header = $"Order {obj.OrderId} Filled at {order.Price}";
                        body = $"Your request to close position #{order.Id} {order.Side} {order.RemainingVolume} lots of {order.Symbol} was filled at {order.LastFillPrice}.";
                    }
                    break;
                case OrderExecAction.Canceled:
                    header = $"Order {obj.OrderId} Canceled";
                    body = $"Your order #{order.Id} {order.Side} {order.Type} {order.RemainingVolume} lots of {order.Symbol} at {order.Price} was canceled.";
                    break;
                case OrderExecAction.Filled:
                    header = $"Order {obj.OrderId} Filled at {order.LastFillPrice}";
                    body = $"Your order #{order.Id} {order.Side} {order.Type} {order.LastFillVolume} lots of {order.Symbol} was filled at {order.LastFillPrice}.";
                    break;
            }

            return new Message(header, body);
        }

        public static Message BuildMessage(PositionExecReport obj)
        {
            var position = obj.PositionCopy;
            string header = "";
            string body = "";
            switch (obj.ExecAction)
            {
                case OrderExecAction.Modified:
                    header = $"Position Modified";
                    body = $"Position {position.Side} {position.Amount} of {position.Symbol} at {position.Price} modified.";
                    break;
                case OrderExecAction.Closed:
                    header = $"Position Closed";
                    body = $"Position {position.Side} {position.Amount} of {position.Symbol} at {position.Price} closed.";
                    break;
            }
            return new Message(header, body);
        }
    }
}
