//  CreateStringRunner


using System;
using System.Text;
using UnityEngine;

namespace Leguar.TotalJSON.Internal {

	class CreateStringRunner {

		private CreateStringSettings settings;
		private StringBuilder builder;

		private string indentString;
		private int currentIndent;
		private string newLineString;

		internal CreateStringRunner(CreateStringSettings settings) {
			this.settings = settings;
			builder=new StringBuilder();
			if (settings.HumanReadable) {
				if (settings.IndentUsingTab) {
					indentString="\t";
				} else {
					indentString=new string(' ',settings.IndentSpaceCount);
				}
				currentIndent=0;
			}
			newLineString=getNewLineString();
		}

		internal void append(char chr) {
			builder.Append(chr);
		}

		internal void append(char chr, bool space) {
			builder.Append(chr);
			if (space && settings.HumanReadable) {
				builder.Append(' ');
			}
		}

		internal void append(string str) {
			builder.Append(str);
		}

		internal void append(char chr1, char chr2) {
			builder.Append(chr1);
			if (settings.HumanReadable) {
				builder.Append(' ');
			}
			builder.Append(chr2);
		}

		internal void append(char chr, int indentChange) {
			builder.Append(chr);
			if (settings.HumanReadable) {
				builder.Append(newLineString);
				currentIndent += indentChange;
				for (int n = 0; n<currentIndent; n++) {
					builder.Append(indentString);
				}
			}
		}

		internal void append(int indentChange, char chr) {
			if (settings.HumanReadable) {
				builder.Append(newLineString);
				currentIndent += indentChange;
				for (int n = 0; n<currentIndent; n++) {
					builder.Append(indentString);
				}
			}
			builder.Append(chr);
		}

		internal bool isEscapeForwardSlashes() {
			return settings.EscapeForwardSlashes;
		}

		internal string getFinalString() {
			return builder.ToString();
		}

		private string getNewLineString() {
			if (settings.NewLine==CreateStringSettings.NewLineTypes.EnvironmentDefault) {
				return Environment.NewLine;
			} else if (settings.NewLine==CreateStringSettings.NewLineTypes.LF) {
				return "\n";
			} else if (settings.NewLine==CreateStringSettings.NewLineTypes.CR_LF) {
				return "\r\n";
			} else {
				Debug.LogError("Leguar.TotalJSON: CreateStringRunner.getNewLine(): Internal error: Unspecified new line type");
				return Environment.NewLine;
			}
		}

	}

}
