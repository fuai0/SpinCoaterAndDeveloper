using DataBaseServiceInterface;
using FSM;
using LogServiceInterface;
using MotionControlActuation;
using Prism.Services.Dialogs;
using PrismAutomationPlatformExt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SpinCoaterAndDeveloper.Shared.Extensions
{
    /// <summary>
    /// 弹窗扩展
    /// Prism自带的DialogService会默认使用激活窗口为父窗口.
    /// 如果同时出现两个弹窗的时候,第二个弹窗的父窗体会以第一个窗口为其父窗体.
    /// 在查找窗口模拟点击BtnYes的时候,如果点击了第一个窗口的BtnYes,因其为第二个弹窗的父窗体,在关闭第一个窗口的同时,系统会将第二个弹窗一起关闭.
    /// 第二个窗口将返回None,导致流程出现错误.
    /// 解决方案,如果在流程中弹出阻塞弹窗并且有其他非阻塞弹窗出现时,需使用IDialogWithNoParentService.
    /// 将弹窗的窗口不设置父窗体,这样在模拟点击的时候就不会出现关闭其中一个窗体时会将其他弹窗被系统默认关闭.
    /// 如果运行时会同时出现两个弹窗,建议使用IDialogWithNoParentService
    /// </summary>
    public static class DialogExtensions
    {
        /// <summary>
        /// 用于系统提示弹窗,无父窗体,不阻塞
        /// </summary>
        /// <param name="dialogWithNoParentService"></param>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="sysDialogLevel"></param>
        /// <param name="cancelInfo"></param>
        /// <param name="yesInfo"></param>
        /// <returns></returns>
        public static ButtonResult AppAlert(this IDialogWithNoParentService dialogWithNoParentService, string title, string content, SysDialogLevel sysDialogLevel, string cancelInfo = "取消", string yesInfo = "确认")
        {
            ButtonResult result = ButtonResult.None;

            DialogParameters par = new DialogParameters();
            par.Add("Title", title.TryFindResourceEx());
            par.Add("Content", content.TryFindResourceEx());
            par.Add("CancelInfo", cancelInfo.TryFindResourceEx());
            par.Add("YesInfo", yesInfo.TryFindResourceEx());
            par.Add("DialogLevel", sysDialogLevel);

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                dialogWithNoParentService.Show("DialogSystemMessageView", par, r => { result = r.Result; });
            });
            return result;
        }
        /// <summary>
        /// 阻塞自定义弹窗,有父窗体
        /// </summary>
        /// <param name="dialogService"></param>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="cancelInfo"></param>
        /// <param name="yesInfo"></param>
        /// <returns></returns>
        public static ButtonResult QuestionShowDialog(this IDialogService dialogService, string title, string content, string cancelInfo = "取消", string yesInfo = "确认")
        {
            ButtonResult result = ButtonResult.None;

            DialogParameters par = new DialogParameters();
            par.Add("Title", title.TryFindResourceEx());
            par.Add("Content", content.TryFindResourceEx());
            par.Add("IgnoreInfo", "");  //Ignore按钮默认不显示
            par.Add("CancelInfo", cancelInfo.TryFindResourceEx());
            par.Add("YesInfo", yesInfo.TryFindResourceEx());
            par.Add("RedirectInfo", "");

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                dialogService.ShowDialog("DialogMessageView", par, r => { result = r.Result; });
            });
            return result;
        }

        /// <summary>
        /// 非阻塞自定义弹窗,有父窗体
        /// </summary>
        /// <param name="dialogService"></param>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="cancelInfo"></param>
        /// <param name="yesInfo"></param>
        [Obsolete("不建议使用此方法进行进行弹窗,请使用QuestionShowWithNoParent")]
        public static void QuestionShow(this IDialogService dialogService, string title, string content, string cancelInfo = "取消", string yesInfo = "确认")
        {
            ButtonResult result = ButtonResult.None;

            DialogParameters par = new DialogParameters();
            par.Add("Title", title.TryFindResourceEx());
            par.Add("Content", content.TryFindResourceEx());
            par.Add("IgnoreInfo", "");  //Ignore按钮默认不显示
            par.Add("CancelInfo", cancelInfo.TryFindResourceEx());
            par.Add("YesInfo", yesInfo.TryFindResourceEx());
            par.Add("RedirectInfo", "");

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                dialogService.Show("DialogMessageView", par, r => { result = r.Result; });
            });
            return;
        }

        /// <summary>
        /// 非阻塞自定义弹窗,不设置弹窗窗体的父窗体.
        /// </summary>
        /// <param name="dialogWithNoParentService"></param>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="cancelInfo"></param>
        /// <param name="yesInfo"></param>
        public static void QuestionShowWithNoParent(this IDialogWithNoParentService dialogWithNoParentService, string title, string content, string cancelInfo = "取消", string yesInfo = "确认")
        {
            ButtonResult result = ButtonResult.None;

            DialogParameters par = new DialogParameters();
            par.Add("Title", title.TryFindResourceEx());
            par.Add("Content", content.TryFindResourceEx());
            par.Add("IgnoreInfo", "");  //Ignore按钮默认不显示
            par.Add("CancelInfo", cancelInfo.TryFindResourceEx());
            par.Add("YesInfo", yesInfo.TryFindResourceEx());
            par.Add("RedirectInfo", "");

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                dialogWithNoParentService.ShowWithNoParent("DialogMessageView", par, r => { result = r.Result; });
            });
            return;
        }

        /// <summary>
        /// 阻塞自定义弹窗,同时报警,用于流程,不设置弹窗窗体的父窗体
        /// 弹窗窗口报警,关闭窗口报警关闭
        /// </summary>
        /// <param name="dialogWithNoParentService"></param>
        /// <param name="actuationManager">调用此窗口的管理线程,如果为Null则不受管理线程管控</param>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="redirectInfo"></param>
        /// <param name="ignoreInfo"></param>
        /// <param name="cancelResetInfo"></param>
        /// <param name="yesRetryInfo"></param>
        /// <returns></returns>
        public static IDialogResult QuestionShowDialogWithAlarm(this IDialogWithNoParentService dialogWithNoParentService, ActuationManagerAbs actuationManager, string title, string content, string redirectInfo = "", string ignoreInfo = "", string cancelResetInfo = "取消", string yesRetryInfo = "确认")
        {
            IDialogResult result = new DialogResult();

            DialogParameters par = new DialogParameters();
            par.Add("Title", title.TryFindResourceEx());
            par.Add("Content", content.TryFindResourceEx());
            par.Add("IgnoreInfo", ignoreInfo.TryFindResourceEx());
            par.Add("CancelInfo", cancelResetInfo.TryFindResourceEx());
            par.Add("YesInfo", yesRetryInfo.TryFindResourceEx());
            par.Add("RedirectInfo", redirectInfo.TryFindResourceEx());
            par.Add("ActuationManager", actuationManager);
            EventWaitHandle eventWaitHandle = new AutoResetEvent(false);

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.Alarm));
                dialogWithNoParentService.ShowWithNoParent("DialogMessageView", par, r => { result = r; eventWaitHandle.Set(); });
            });
            eventWaitHandle.WaitOne();
            return result;
        }

        /// <summary>
        /// 处理Running流程中不设置弹窗窗体的父窗体的弹窗结果.
        /// </summary>
        /// <param name="dialogWithNoParentService"></param>
        /// <param name="result"></param>
        /// <param name="logService"></param>
        /// <returns></returns>
        public static ActuationResult<UserOperateType> ExeShowDialogResult(this IDialogWithNoParentService dialogWithNoParentService, IDialogResult result, ILogService logService, string redirectActuaionName = "")
        {
            switch (result.Result)
            {
                case ButtonResult.Ignore:   //用户选择忽略
                    logService.WriteLog(LogTypes.DB.ToString(), "用户选择忽略", MessageDegree.INFO);
                    if (GlobalValues.MachineStatus == FSMStateCode.Alarming || GlobalValues.MachineStatus == FSMStateCode.BurnInAlarming)
                    {
                        //当前状态为报警状态则自动发送启动事件使设备状态从报警切换到运行中
                        logService.WriteLog(LogTypes.DB.ToString(), $@"用户选择忽略,当前状态为报警状态,自动发送启动事件", MessageDegree.INFO);
                        GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.StartUp));
                    }
                    return new ActuationResult<UserOperateType>() { Result = UserOperateType.Ignore };
                case ButtonResult.Cancel:   //用户选择复位
                    logService.WriteLog(LogTypes.DB.ToString(), "用户选择复位", MessageDegree.INFO);
                    //弹窗选择复位有可能在Alarming中,Pausing中,RunningGroup可能还在运行,轴可能还在运行,因此先急停在复位
                    GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                    GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.Reset));
                    return new ActuationResult<UserOperateType>() { Result = UserOperateType.Reset };
                case ButtonResult.Yes:  //用户选择重试
                    logService.WriteLog(LogTypes.DB.ToString(), "用户选择重试", MessageDegree.INFO);
                    if (GlobalValues.MachineStatus == FSMStateCode.Alarming || GlobalValues.MachineStatus == FSMStateCode.BurnInAlarming)
                    {
                        //当前状态为报警状态则自动发送启动事件使设备状态从报警切换到运行中
                        logService.WriteLog(LogTypes.DB.ToString(), $@"用户选择重试,当前状态为报警状态,自动发送启动事件", MessageDegree.INFO);
                        GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.StartUp));
                    }
                    return new ActuationResult<UserOperateType>() { Result = UserOperateType.Retry };
                case ButtonResult.Abort:    //用户选择重定向到其他动作
                    logService.WriteLog(LogTypes.DB.ToString(), $"用户选择重定向", MessageDegree.INFO);
                    //未定义跳转名称,做一下记录,指令集内部会返回异常,指令管理线程会返回异常结束,进入急停
                    if (string.IsNullOrWhiteSpace(redirectActuaionName))
                        logService.WriteLog(LogTypes.DB.ToString(), $"未定义重定向指令名称", MessageDegree.ERROR);
                    if (GlobalValues.MachineStatus == FSMStateCode.Alarming || GlobalValues.MachineStatus == FSMStateCode.BurnInAlarming)
                    {
                        //当前状态为报警状态则自动发送启动事件使设备状态从报警切换到运行中
                        logService.WriteLog(LogTypes.DB.ToString(), $@"用户选择跳转,当前状态为报警状态,自动发送启动事件", MessageDegree.INFO);
                        GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.StartUp));
                    }
                    return new ActuationResult<UserOperateType> { Result = UserOperateType.Redirect, RedirectActuationName = redirectActuaionName };
                case ButtonResult.None:
                    logService.WriteLog(LogTypes.DB.ToString(), "报警弹窗自动退出", MessageDegree.WARN);
                    return new ActuationResult<UserOperateType>() { Result = UserOperateType.Unselected };
                default:
                    logService.WriteLog(LogTypes.DB.ToString(), "用户未选择弹窗按钮", MessageDegree.FATAL);
                    return new ActuationResult<UserOperateType>() { Result = UserOperateType.Unselected };
            }
        }
    }
}
