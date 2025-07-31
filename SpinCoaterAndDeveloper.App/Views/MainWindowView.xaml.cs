using LogServiceInterface;
using PermissionServiceInterface;
using Prism.Events;
using Prism.Services.Dialogs;
using PrismAutomationPlatformExt;
using SpinCoaterAndDeveloper.App.Common;
using SpinCoaterAndDeveloper.App.Extensions;
using SpinCoaterAndDeveloper.App.Views.Loading;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.Event;
using SpinCoaterAndDeveloper.Shared.Extensions;
using System;
using System.Windows;
using System.Windows.Input;

namespace SpinCoaterAndDeveloper.App.Views
{
    /// <summary>
    /// Interaction logic for MainWindowView.xaml
    /// </summary>
    public partial class MainWindowView : Window
    {
        private readonly IEventAggregator eventAggregator;

        public MainWindowView(IEventAggregator eventAggregator, IDialogHostService dialogHostService)
        {
            InitializeComponent();
            this.eventAggregator = eventAggregator;
            //注册加载等待窗口
            eventAggregator.RegisterLoadingEvent(arg =>
            {
                DialogHost.IsOpen = arg.IsOpen;
                if (DialogHost.IsOpen)
                {
                    DialogHost.DialogContent = new LoadingView();
                }
            });
            //HSL授权
            if (!HslCommunication.Authorization.SetAuthorizationCode("e816432a-a131-403c-a274-d6b73d2ffb46"))
                throw new Exception("HSL授权失败");
        }

        private void QuickLogin_Execute(object sender, ExecutedRoutedEventArgs e)
        {
#if DEBUG
            eventAggregator.GetEvent<MessageEvent>().Publish(new MessageModel
            {
                Filter = "QuickLoginOrOut",
                Message = "QuickLogin_Dev",
            });
#endif
        }

        private void QuickLanguageChangeZhCN_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            eventAggregator.GetEvent<MessageEvent>().Publish(new MessageModel
            {
                Filter = "LanguageChanged",
                Message = "zh-CN",
            });
        }

        private void QuickLanguageChangeEnUS_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            eventAggregator.GetEvent<MessageEvent>().Publish(new MessageModel
            {
                Filter = "LanguageChanged",
                Message = "en-US",
            });
        }

        private void QuickLanguageChangeViVN_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            eventAggregator.GetEvent<MessageEvent>().Publish(new MessageModel
            {
                Filter = "LanguageChanged",
                Message = "vi-VN",
            });
        }
    }
}
