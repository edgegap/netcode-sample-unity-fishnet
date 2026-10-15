using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

[assembly: UnityEngine.Scripting.AlwaysLinkAssembly]

namespace Edgegap
{
    using L = Logger;

    public class SafeHttpRequest
    {
        internal MonoBehaviour Parent;
        internal int TimeoutSeconds = 3;

        public SafeHttpRequest(MonoBehaviour parent, int timeoutSeconds = 0)
        {
            Parent = parent;
            TimeoutSeconds = timeoutSeconds > 0 ? timeoutSeconds : TimeoutSeconds;
        }

        public void Post(
            string url,
            string authToken,
            string body,
            Action<string, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate,
            RetryParameters retryParameters = null
        )
        {
#if UNITY_2022_3_OR_NEWER
            UnityWebRequest request = UnityWebRequest.Post(url, body, "application/json");
#else
            // UnityWebRequest.Post(url, string) form-encodes the body before Unity 2022.3,
            // which the matchmaker rejects with HTTP 400, so send the JSON raw.
            UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body)),
                downloadHandler = new DownloadHandlerBuffer(),
            };
            request.SetRequestHeader("Content-Type", "application/json");
#endif
            ProcessRetryableRequest(
                request,
                authToken,
                onSuccessDelegate,
                onErrorDelegate,
                (string error, UnityWebRequest req, RetryParameters retry) =>
                {
                    Post(url, authToken, body, onSuccessDelegate, onErrorDelegate, retry);
                },
                retryParameters
            );
        }

        public void Get(
            string url,
            string authToken,
            Action<string, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate,
            RetryParameters retryParameters = null
        )
        {
            UnityWebRequest request = UnityWebRequest.Get(url);
            ProcessRetryableRequest(
                request,
                authToken,
                onSuccessDelegate,
                onErrorDelegate,
                (string error, UnityWebRequest req, RetryParameters retry) =>
                {
                    Get(url, authToken, onSuccessDelegate, onErrorDelegate, retry);
                },
                retryParameters
            );
        }

        public void Delete(
            string url,
            string authToken,
            Action<string, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate,
            RetryParameters retryParameters = null
        )
        {
            UnityWebRequest request = UnityWebRequest.Delete(url);
            ProcessRetryableRequest(
                request,
                authToken,
                onSuccessDelegate,
                onErrorDelegate,
                (string error, UnityWebRequest req, RetryParameters retry) =>
                {
                    Delete(url, authToken, onSuccessDelegate, onErrorDelegate, retry);
                },
                retryParameters
            );
        }

        public void Patch(
            string url,
            string authToken,
            string body,
            Action<string, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate,
            RetryParameters retryParameters = null
        )
        {
            UnityWebRequest request = UnityWebRequest.Put(url, body);
            // hack for UnityWebRequest not supporting PATCH
            request.method = "PATCH";

            ProcessRetryableRequest(
                request,
                authToken,
                onSuccessDelegate,
                onErrorDelegate,
                (string error, UnityWebRequest req, RetryParameters retry) =>
                {
                    Patch(url, authToken, body, onSuccessDelegate, onErrorDelegate, retry);
                },
                retryParameters
            );
        }

        public void ProcessRetryableRequest(
            UnityWebRequest request,
            string authToken,
            Action<string, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate,
            Action<string, UnityWebRequest, RetryParameters> onRetryableDelegate,
            RetryParameters retryParameters = null
        )
        {
            request.SetRequestHeader("Authorization", authToken);
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = TimeoutSeconds;
            retryParameters ??= new RetryParameters();

            Parent.StartCoroutine(
                SendRequestEnumerator(
                    request,
                    onSuccessDelegate,
                    (string error, UnityWebRequest req) =>
                    {
                        Dictionary<string, string> responseHeaders = req.GetResponseHeaders();
                        if (responseHeaders is null)
                        {
                            onErrorDelegate(error, req);
                            return;
                        }

                        bool retryAfterHeader = responseHeaders.TryGetValue(
                            "Retry-After",
                            out var retryAfter
                        );
                        if (
                            retryParameters.RemainingAttempts <= 0
                            || (
                                retryAfterHeader == false
                                && req.responseCode != 429
                                && req.responseCode < 500
                            )
                        )
                        {
                            onErrorDelegate(error, req);
                            return;
                        }

                        L.Warn(
                            $"Retrying ({retryParameters.RemainingAttempts}/{retryParameters.MaxAttempts}) {request.method} {request.url}.\n{error}"
                        );
                        retryParameters.RemainingAttempts--;
                        if (retryAfterHeader)
                        {
                            try
                            {
                                Single.TryParse(retryAfter, out float retryAfterFloat);
                                retryParameters.BackoffSeconds = () =>
                                    retryAfterFloat + (0.1f * Random.value);
                            }
                            catch (Exception e)
                            {
                                L.Error($"Error parsing Retry-After header: {e.Message}");
                            }
                        }
                        else
                        {
                            retryParameters.BackoffSeconds = RetryParameters.DefaultBackoffSeconds;
                        }
                        onRetryableDelegate(error, req, retryParameters);
                    },
                    retryParameters
                )
            );
        }

        public IEnumerator SendRequestEnumerator(
            UnityWebRequest request,
            Action<string, UnityWebRequest> onSuccessDelegate,
            Action<string, UnityWebRequest> onErrorDelegate,
            RetryParameters retryParameters = null
        )
        {
            if (
                retryParameters is not null
                && retryParameters.RemainingAttempts < retryParameters.MaxAttempts
            )
            {
                yield return new WaitForSeconds(retryParameters.BackoffSeconds());
            }

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onErrorDelegate(
                    $"HTTP {request.responseCode}: {request.error}\n{request.downloadHandler?.text}",
                    request
                );
            }
            else
            {
                string stringResponse =
                    request.downloadHandler == null ? "" : request.downloadHandler.text;
                onSuccessDelegate(stringResponse, request);
            }
            request.Dispose();
        }
    }

    public class RetryParameters
    {
        public uint MaxAttempts = 3;
        public uint RemainingAttempts = 3;
        public Func<float> BackoffSeconds = DefaultBackoffSeconds;

        public static readonly Func<float> DefaultBackoffSeconds = () => 1f + (0.1f * Random.value);
    }
}
