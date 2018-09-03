﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Orcus.Administration.Library.Exceptions;
using Orcus.Administration.Library.Extensions;
using Orcus.Utilities;

namespace Orcus.Administration.Library.StatusBar
{
    public static class ShellStatusBarExtensions
    {
        private const int DefaultMessageTimeout = 5;

        public static async void ShowMessage(this IShellStatusBar shellStatusBar, string message,
            StatusBarAnimation animation = StatusBarAnimation.None, int? seconds = null,
            CancellationToken cancellationToken = default)
        {
            using (shellStatusBar.PushStatus(new TextStatusMessage(message) {Animation = animation}))
            {
                await Task.Delay((seconds ?? DefaultMessageTimeout) * 1000, cancellationToken);
            }
        }

        public static IDisposable ShowStatus(this IShellStatusBar shellStatusBar, string message,
            StatusBarAnimation animation = StatusBarAnimation.None)
        {
            return shellStatusBar.PushStatus(new TextStatusMessage(message) {Animation = animation});
        }

        public static async void ShowSuccess(this IShellStatusBar shellStatusBar, string message,
            StatusBarAnimation animation = StatusBarAnimation.None, int? seconds = null,
            CancellationToken cancellationToken = default)
        {
            using (shellStatusBar.PushStatus(new TextStatusMessage(message)
            {
                Animation = animation,
                StatusBarMode = StatusBarMode.Success
            }))
            {
                await Task.Delay((seconds ?? DefaultMessageTimeout) * 1000, cancellationToken);
            }
        }

        public static async void ShowError(this IShellStatusBar shellStatusBar, string message,
            StatusBarAnimation animation = StatusBarAnimation.None, int? seconds = null,
            CancellationToken cancellationToken = default)
        {
            using (shellStatusBar.PushStatus(new TextStatusMessage(message)
            {
                Animation = animation,
                StatusBarMode = StatusBarMode.Error
            }))
            {
                await Task.Delay((seconds ?? DefaultMessageTimeout) * 1000, cancellationToken);
            }
        }

        public static async void ShowWarning(this IShellStatusBar shellStatusBar, string message,
            StatusBarAnimation animation = StatusBarAnimation.None, int? seconds = null,
            CancellationToken cancellationToken = default)
        {
            using (shellStatusBar.PushStatus(new TextStatusMessage(message)
            {
                Animation = animation,
                StatusBarMode = StatusBarMode.Warning
            }))
            {
                await Task.Delay((seconds ?? DefaultMessageTimeout) * 1000, cancellationToken);
            }
        }

        public static async Task ShowMessage(this IShellStatusBar shellStatusBar, string message, Task task,
            StatusBarAnimation animation = StatusBarAnimation.None)
        {
            using (shellStatusBar.PushStatus(new TextStatusMessage(message) {Animation = animation}))
            {
                await task;
            }
        }

        public static async Task<T> ShowMessage<T>(this IShellStatusBar shellStatusBar, string message, Task<T> task,
            StatusBarAnimation animation = StatusBarAnimation.None)
        {
            using (shellStatusBar.PushStatus(new TextStatusMessage(message) { Animation = animation }))
            {
                return await task;
            }
        }

        public static async Task DisplayOnStatusBar(this Task task, IShellStatusBar shellStatusBar, string message,
            StatusBarAnimation animation = StatusBarAnimation.None)
        {
            using (shellStatusBar.PushStatus(new TextStatusMessage(message) {Animation = animation}))
            {
                await task;
            }
        }

        public static async Task<T> DisplayOnStatusBar<T>(this Task<T> task, IShellStatusBar shellStatusBar,
            string message, StatusBarAnimation animation = StatusBarAnimation.None, bool mustShow = false)
        {
            await DisplayOnStatusBar((Task) task, shellStatusBar, message, animation, mustShow);
            return task.Result;
        }

        public static async Task DisplayOnStatusBar(this Task task, IShellStatusBar shellStatusBar,
            string message, StatusBarAnimation animation = StatusBarAnimation.None, bool mustShow =false)
        {
            //if the task executes synchronously, don't display anything to avoid "flickering"
            if (!mustShow)
            {
                var completedTask = await Task.WhenAny(task, Task.Delay(200));
                if (completedTask == task)
                    return;
            }

            var beginTime = DateTimeOffset.UtcNow;
            var status = shellStatusBar.PushStatus(new TextStatusMessage(message) {Animation = animation});

            try
            {
                await task;
            }
            catch (Exception)
            {
                status.Dispose();
                throw;
            }

            //the status should be displayed at least one second to avoid flickering. Even if the task finished,
            //continue displaying the status for the remaining of the time

            var diff = TimeSpan.FromSeconds(1) - (DateTimeOffset.UtcNow - beginTime);
            if (diff < TimeSpan.Zero)
                status.Dispose();
            else Task.Delay(diff).ContinueWith(_ => status.Dispose()).Forget();
        }

        public static async Task<SuccessOrError<T>> DisplayOnStatusBarCatchErrors<T>(this Task<T> task,
            IShellStatusBar shellStatusBar, string message, StatusBarAnimation animation = StatusBarAnimation.None, bool mustShow = false)
        {
            try
            {
                return await task.DisplayOnStatusBar(shellStatusBar, message, animation, mustShow);
            }
            catch (RestException e)
            {
                shellStatusBar.ShowError(e.GetRestExceptionMessage());
                return SuccessOrError<T>.DefaultFailed;
            }
            catch (Exception e)
            {
                shellStatusBar.ShowError(e.Message);
                return SuccessOrError<T>.DefaultFailed;
            }
        }

        public static async Task<bool> DisplayOnStatusBarCatchErrors(this Task task, IShellStatusBar shellStatusBar,
            string message, StatusBarAnimation animation = StatusBarAnimation.None, bool mustShow = false)
        {
            try
            {
                await task.DisplayOnStatusBar(shellStatusBar, message, animation, mustShow);
                return true;
            }
            catch (RestException e)
            {
                shellStatusBar.ShowError(e.GetRestExceptionMessage());
                return false;
            }
            catch (Exception e)
            {
                shellStatusBar.ShowError(e.Message);
                return false;
            }
        }
    }
}