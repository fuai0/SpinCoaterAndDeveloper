using MaterialDesignThemes.Wpf;
using SpinCoaterAndDeveloper.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SpinCoaterAndDeveloper.App.Extensions
{
    public static class SnackbarMessageQueue
    {
        public static void EnqueueEx(this ISnackbarMessageQueue snackbarMessageQueue, string message)
        {
            snackbarMessageQueue.Enqueue(message.TryFindResourceEx());
        }
    }
}
