//  StringPointer


using System.Text;

namespace Leguar.TotalJSON.Internal {

	class StringPointer {

		private string str;
		private int strLength;
		private int nextChar;

		// For debugging
		private ParseStringRunner psrForDebug;
		private char lastChr;
		private int line;
		private int col;

		// For stepping back
		private char sbLastChr;
		private int sbLine;
		private int sbCol;

//		internal StringPointer(string str) : this(str,0) {
//		}

		internal StringPointer(string str, int startIndex, ParseStringRunner psrForDebug) {
			this.str=str;
			strLength=str.Length;
			nextChar=startIndex;
			this.psrForDebug = psrForDebug;
			lastChr='\0';
			line=1;
			col=0;
		}

		internal bool containsNonWhiteChars() {
			foreach (char chr in str) {
				if (!isWhiteChar(chr)) {
					return true;
				}
			}
			return false;
		}

		internal bool tryGetNextNonWhiteChar(out char chr) {
			do {
				if (nextChar>=strLength) {
					chr=default(char);
					return false;
				}
				chr=getNextChar();
			} while (isWhiteChar(chr));
			return true;
		}

		internal char getNextNonWhiteChar() {
			char chr;
			do {
				chr=getNextChar();
			} while (isWhiteChar(chr));
			return chr;
		}

		internal char getNextChar() {
			if (nextChar>=strLength) {
				throw ParseException.forInvalidEnd(psrForDebug);
			}
			sbLastChr=lastChr;
			sbLine=line;
			sbCol=col;
			char chr=str[nextChar];
			nextChar++;
			if (chr=='\n' || chr=='\r') {
				if ((chr=='\n' && lastChr!='\r') || (chr=='\r' && lastChr!='\n')) {
					line++;
					col=0;
				}
			} else {
				col++;
			}
			lastChr=chr;
			return chr;
		}

		internal bool isNextChars(string sequence) {
			foreach (char chr in sequence) {
				if (getNextChar()!=chr) {
					return false;
				}
			}
			return true;
		}

		internal int getCurrentIndex() {
			return nextChar;
		}

		internal void stepBack() {
			nextChar--;
			lastChr=sbLastChr;
			line=sbLine;
			col=sbCol;
		}

		internal string getSubStringStartingFrom(int start) {
			return str.Substring(start,nextChar-start);
		}

		internal string getLineAndColumnForException() {
			return ("line "+line+", col "+col);
		}

		internal string getSubStringForException(int count) {
			int start=nextChar-count;
			if (start<0) {
				count+=start;
				start=0;
			}
			if (start+count>strLength) {
				count=strLength-start;
			}
			if (count==0) {
				return "";
			}
			StringBuilder sb=new StringBuilder();
			if (start>0) {
				sb.Append("...");
			}
			sb.Append(str.Substring(start,count));
			if (start+count<strLength) {
				sb.Append("...");
			}
			return sb.ToString();
		}

		private bool isWhiteChar(char chr) {
			return (chr==' ' || chr=='\t' || chr=='\n' || chr=='\r');
		}

	}

}
