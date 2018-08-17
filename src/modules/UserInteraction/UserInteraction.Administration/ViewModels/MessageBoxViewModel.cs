﻿using System.Windows.Forms;
using Anapher.Wpf.Swan;
using Orcus.Administration.Library.Clients;
using Prism.Commands;
using Prism.Mvvm;
using UserInteraction.Administration.Extensions;
using UserInteraction.Administration.Rest;
using UserInteraction.Dtos;

namespace UserInteraction.Administration.ViewModels
{
    public class MessageBoxViewModel : BindableBase
    {
        private readonly IPackageRestClient _restClient;

        private string _caption;
        private SystemButtons _messageBoxButtons;
        private SystemIcon _messageBoxIcon;
        private AsyncRelayCommand _sendCommand;
        private DelegateCommand _testCommand;
        private string _text;

        public MessageBoxViewModel(ITargetedRestClient restClient)
        {
            _restClient = restClient.CreateLocal();
        }

        public SystemIcon MessageBoxIcon
        {
            get => _messageBoxIcon;
            set => SetProperty(ref _messageBoxIcon, value);
        }

        public SystemButtons MessageBoxButtons
        {
            get => _messageBoxButtons;
            set => SetProperty(ref _messageBoxButtons, value);
        }

        public string Caption
        {
            get => _caption;
            set => SetProperty(ref _caption, value);
        }

        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        public DelegateCommand TestCommand
        {
            get
            {
                return _testCommand ?? (_testCommand = new DelegateCommand(() =>
                {
                    MessageBox.Show(Text, Caption, (MessageBoxButtons) MessageBoxButtons,
                        SystemIconToMessageBoxIcon(MessageBoxIcon));
                }));
            }
        }

        public AsyncRelayCommand SendCommand
        {
            get
            {
                return _sendCommand ?? (_sendCommand = new AsyncRelayCommand(parameter =>
                {
                    var dto = new OpenMessageBoxDto
                    {
                        Caption = Caption,
                        Text = Text,
                        Icon = MessageBoxIcon,
                        Buttons = MessageBoxButtons
                    };

                    return MessageBoxResource.OpenAsync(dto, _restClient);
                }));
            }
        }

        private static MessageBoxIcon SystemIconToMessageBoxIcon(SystemIcon icon)
        {
            switch (icon)
            {
                case SystemIcon.Error:
                    return System.Windows.Forms.MessageBoxIcon.Error;
                case SystemIcon.Info:
                    return System.Windows.Forms.MessageBoxIcon.Information;
                case SystemIcon.Warning:
                    return System.Windows.Forms.MessageBoxIcon.Warning;
                case SystemIcon.Question:
                    return System.Windows.Forms.MessageBoxIcon.Question;
                default:
                    return System.Windows.Forms.MessageBoxIcon.None;
            }
        }
    }
}