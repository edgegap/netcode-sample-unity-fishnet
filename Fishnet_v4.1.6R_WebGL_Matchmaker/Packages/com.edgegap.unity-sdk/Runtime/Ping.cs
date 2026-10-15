using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Edgegap
{
    public class Ping
    {
        internal MonoBehaviour Parent;
        internal int TimeoutSeconds = 3;

        public Ping(MonoBehaviour parent)
        {
            Parent = parent;
        }

        public Ping(MonoBehaviour parent, int timeoutSeconds)
        {
            Parent = parent;
            TimeoutSeconds = timeoutSeconds;
        }

        public IEnumerator GetAverageRoundTripTime(
            string ip,
            Action<double> onCompleteDelegate,
            int requests
        )
        {
            List<int> pings = new List<int>();
            for (int i = 0; i < requests; i++)
            {
                Parent.StartCoroutine(_IcmpPing(ip, (int rtt) => pings.Add(rtt)));
            }

            yield return new WaitUntil(() => pings.Count == requests);

            List<int> finishedPings = pings.Where((int p) => p > 0).ToList();
            onCompleteDelegate(
                finishedPings.Count() > 0 ? Math.Round(finishedPings.Average(), 2) : 0f
            );
        }

        public IEnumerator _IcmpPing(string ip, Action<int> onCompleteDelegate)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // browsers can't send ICMP, so UnityEngine.Ping doesn't exist in WebGL builds
            onCompleteDelegate(-1);
            yield break;
#else
            UnityEngine.Ping ping = new UnityEngine.Ping(ip);
            double start = Time.realtimeSinceStartupAsDouble;

            yield return new WaitUntil(
                () => ping.isDone || Time.realtimeSinceStartupAsDouble - start > TimeoutSeconds
            );

            onCompleteDelegate(ping.time);
            ping.DestroyPing();
#endif
        }
    }
}
