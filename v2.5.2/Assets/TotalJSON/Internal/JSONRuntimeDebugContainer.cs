//  TotalJSON - JSONRuntimeDebugContainer


#if UNITY_EDITOR

using UnityEngine;
using System.Collections.Generic;

namespace Leguar.TotalJSON.Internal {

	public class JSONRuntimeDebugContainer : MonoBehaviour {

		private Dictionary<string,JValue> content;

		internal JSONRuntimeDebugContainer() {
			content=new Dictionary<string, JValue>();
		}

		internal void add(string debugName, JValue jValue) {
			foreach (string key in content.Keys) {
				if (content[key]==jValue) {
					if (key.Equals(debugName)) {
						return; // Adding same object with same name, all done
					}
					content.Remove(key); // Remove previous one so that same object is not in debug twice (but this allows "changing name" of debugged object)
					break;
				}
			}
			content[debugName]=jValue;
		}

		public Dictionary<string, JValue> getContent() {
			return content;
		}

	}

}

#endif
