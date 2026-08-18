using System;
using ModpackInstaller.ViewModels;

namespace ModpackInstaller.Services.Notifications;

public static class NotificationManager {
    private static bool _isConsole;

    public static bool IsConsole {
        get => _isConsole;
        set {
            if (_isConsole == value)
                return;

            _isConsole = value;

            if (value)
                foreach (var notification in Notifications)
                    notification.EnableConsole();
            else
                foreach (var notification in Notifications)
                    notification.DisableConsole();
        }
    }

    public static ObservableList<NotificationCardViewModel> Notifications { get; } = [];

    public static NotificationCardViewModel Show(
        string title,
        string body = "",
        bool autoClose = true,
        TimeSpan? duration = null) {
        var notification = new NotificationCardViewModel {
            Title = title, Body = body, Progress = 0, AutoClose = autoClose,
        };

        if (IsConsole)
            notification.EnableConsole();
        Notifications.Add(notification);

        if (autoClose && duration != null)
            _ = notification.StartAutoCloseAsync(duration.Value);

        return notification;
    }

    public static void Remove(NotificationCardViewModel notification) {
        Notifications.Remove(notification);
    }
}