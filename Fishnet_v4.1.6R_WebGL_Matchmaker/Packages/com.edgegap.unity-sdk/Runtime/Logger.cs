using UnityEngine;

namespace Edgegap
{
    public static class Logger
    {
        public static void Log<T>(T message)
        {
            Debug.Log($"ℹ️ Edgegap {message}");
        }

        public static void Warn<T>(T message)
        {
            Debug.LogWarning($"⚠️ Edgegap {message}");
        }

        public static void Error<T>(T message)
        {
            Debug.LogError($"❗ Edgegap {message}");
        }

        public static string FormatNotifyMessage<T>(
            string service,
            string subject,
            string message,
            T value
        )
        {
            return $"{service} | {subject}.notify('{message}')\n{ToStringOrNull(value)}";
        }

        public static string FormatUpdateMessage<T>(
            string service,
            string subject,
            string message,
            T previous,
            T current
        )
        {
            return string.Join(
                "\n",
                new string[]
                {
                    $"{service} | {subject}.changed('{message}')",
                    $"current: {ToStringOrNull(current)}",
                    $"previous: {ToStringOrNull(previous)}",
                }
            );
        }

        public static string FormatErrorMessage<T>(
            string service,
            string subject,
            string message,
            T value
        )
        {
            return $"{service} | {subject}.error:{message}\n{ToStringOrNull(value)}";
        }

        public static string ToStringOrNull<T>(T value)
        {
            return value is null ? "null" : value.ToString();
        }

        public static void SubscribeLogger<O>(
            Observable<O> observable,
            string service,
            string subject,
            bool enabled = true
        )
            where O : new()
        {
            observable.Subscribe(
                (Observable<O> obs, ObservableActionType type, string message) =>
                {
                    if (!enabled)
                        return;

                    if (type == ObservableActionType.Update)
                    {
                        Log(
                            FormatUpdateMessage(
                                service,
                                subject,
                                message,
                                obs.Previous,
                                obs.Current
                            )
                        );
                    }
                    else if (type == ObservableActionType.Error)
                    {
                        Error(FormatErrorMessage(service, subject, message, obs.Current));
                    }
                    else
                    {
                        string log = FormatNotifyMessage(service, subject, message, obs.Current);
                        if (type == ObservableActionType.Log)
                        {
                            Log(log);
                        }
                        else
                        {
                            Warn(log);
                        }
                    }
                }
            );
        }
    }
}
