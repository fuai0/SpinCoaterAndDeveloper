using Prism.Events;
using SpinCoaterAndDeveloper.Shared.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SpinCoaterAndDeveloper.Shared.Extensions
{
    public static class EventExtensions
    {
        /// <summary>
        /// 推送等待消息
        /// </summary>
        /// <param name="eventAggregator"></param>
        /// <param name="model"></param>
        public static void UpdateLoadingEvent(this IEventAggregator eventAggregator, bool isOpen)
        {
            eventAggregator.GetEvent<UpdateLoadingEvent>().Publish(new UpdateLoadingModel()
            {
                IsOpen = isOpen,
            });
        }
        /// <summary>
        /// 注册等待消息
        /// </summary>
        /// <param name="eventAggregator"></param>
        /// <param name="action"></param>
        public static void RegisterLoadingEvent(this IEventAggregator eventAggregator, Action<UpdateLoadingModel> action)
        {
            eventAggregator.GetEvent<UpdateLoadingEvent>().Subscribe(action);
        }

        /// <summary>
        /// 注册加载信息提示消息
        /// </summary>
        /// <param name="eventAggregator"></param>
        /// <param name="action"></param>
        public static void RegisterLaunchInfo(this IEventAggregator eventAggregator, Action<LaunchInfoEventModel> action, string filterName = "LaunchWindowInfo")
        {
            eventAggregator.GetEvent<LaunchInfoEvent>().Subscribe(action, ThreadOption.UIThread, true, m =>
            {
                return m.Filter.Equals(filterName);
            });
        }
        /// <summary>
        /// 发送加载提示消息
        /// </summary>
        /// <param name="eventAggregator"></param>
        /// <param name="launchInfo"></param>
        public static void SendLaunchInfo(this IEventAggregator eventAggregator, string launchInfo)
        {
            eventAggregator.GetEvent<LaunchInfoEvent>().Publish(new LaunchInfoEventModel()
            {
                LaunchProgressInfo = launchInfo.TryFindResourceEx(),
                Filter = "LaunchWindowInfo"
            });
        }

        /// <summary>
        /// 注册权限变更事件
        /// </summary>
        /// <param name="eventAggregator"></param>
        /// <param name="action"></param>
        /// <param name="filterName"></param>
        public static void RegisterPermissionChangeEvent(this IEventAggregator eventAggregator, Action<MessageModel> action, string filterName = "PremissionChange")
        {
            eventAggregator.GetEvent<MessageEvent>().Subscribe(action, ThreadOption.UIThread, true, m =>
            {
                return m.Filter.Equals(filterName);
            });
        }
        /// <summary>
        /// 发送权限变更事件
        /// </summary>
        /// <param name="eventAggregator"></param>
        /// <param name="message"></param>
        /// <param name="filterName"></param>
        public static void SendPremissionChangeEvent(this IEventAggregator eventAggregator, string message = null, string filterName = "PremissionChange")
        {
            eventAggregator.GetEvent<MessageEvent>().Publish(new MessageModel()
            {
                Filter = filterName,
                Message = message
            });
        }

        /// <summary>
        /// 注册设备状态更新事件
        /// </summary>
        /// <param name="eventAggregator"></param>
        /// <param name="action"></param>
        /// <param name="filterName"></param>
        public static void RegisterMachineStsChangeEvent(this IEventAggregator eventAggregator, Action<MessageModel> action, string filterName = "MachineStsUpdate")
        {
            eventAggregator.GetEvent<MessageEvent>().Subscribe(action, ThreadOption.UIThread, true, m =>
            {
                return m.Filter.Equals(filterName);
            });
        }
        /// <summary>
        /// 发送设备状态更新事件
        /// </summary>
        /// <param name="eventAggregator"></param>
        /// <param name="machineSts"></param>
        /// <param name="filterName"></param>
        public static void SendMachineStsChangeEvent(this IEventAggregator eventAggregator, string machineSts, string filterName = "MachineStsUpdate")
        {
            eventAggregator.GetEvent<MessageEvent>().Publish(new MessageModel()
            {
                Filter = filterName,
                Message = machineSts
            });
        }
    }
}
