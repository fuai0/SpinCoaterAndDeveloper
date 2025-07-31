using Prism.Services.Dialogs;
using PrismAutomationPlatformExt;
using SpinCoaterAndDeveloper.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SpinCoaterAndDeveloper.App.Extensions
{
    public static class HostDialogExtensions
    {
        /// <summary>
        /// 询问窗口
        /// </summary>
        /// <param name="dialogHost">指定的DialogHost会话主机</param>
        /// <param name="title">标题</param>
        /// <param name="conten">询问内容</param>
        /// <param name="dialogHostName">会话主机名称（唯一）</param>
        /// <returns></returns>
        public static async Task<IDialogResult> HostQuestion(this IDialogHostService dialogHost, string title, string conten, string cancelInfo, string saveInfo, string dialogHostName = "Root")
        {
            DialogParameters param = new DialogParameters();
            param.Add("Title", title.TryFindResourceEx());
            param.Add("Content", conten.TryFindResourceEx());
            param.Add("CancelInfo", cancelInfo.TryFindResourceEx());
            param.Add("SaveInfo", saveInfo.TryFindResourceEx());
            param.Add("dialogHostName", dialogHostName);

            var dialogResult = await dialogHost.ShowHostDialog("DialogHostMessageView", param, dialogHostName);
            return dialogResult;
        }
    }
}
