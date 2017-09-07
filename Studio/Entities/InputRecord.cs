using System;
using System.Text;
namespace TeslagradStudio.Entities {
	[Flags]
	public enum Actions {
		None,
		Left = 0x1,
		Right = 0x2,
		Up = 0x4,
		Down = 0x8,
		Jump = 0x10,
		Positive = 0x20,
		Negative = 0x40,
		Blink = 0x80,
		CloakPositive = 0x100,
		CloakNegative = 0x200,
		Start = 0x400,
		Map = 0x800,
		ToggleCloak = 0x1000
	}
	public class InputRecord {
		public static char Delimiter = ',';
		public int Frames { get; set; }
		public Actions Actions { get; set; }
		public string Notes { get; set; }
		public int ZeroPadding { get; set; }
		public InputRecord(int frameCount, Actions actions, string notes = null) {
			Frames = frameCount;
			Actions = actions;
			Notes = notes;
		}
		public InputRecord(string line) {
			Notes = string.Empty;

			int index = 0;
			Frames = ReadFrames(line, ref index);
			if (Frames == 0) {
				Notes = line;
				return;
			}

			while (index < line.Length) {
				char c = line[index];

				switch (char.ToUpper(c)) {
					case 'L': Actions ^= Actions.Left; break;
					case 'R': Actions ^= Actions.Right; break;
					case 'U': Actions ^= Actions.Up; break;
					case 'D': Actions ^= Actions.Down; break;
					case 'J': Actions ^= Actions.Jump; break;
					case 'P': Actions ^= Actions.Positive; break;
					case 'N': Actions ^= Actions.Negative; break;
					case 'B': Actions ^= Actions.Blink; break;
					case ']': Actions ^= Actions.CloakPositive; break;
					case '[': Actions ^= Actions.CloakNegative; break;
					case 'S': Actions ^= Actions.Start; break;
					case 'M': Actions ^= Actions.Map; break;
					case 'C': Actions ^= Actions.ToggleCloak; break;
				}

				index++;
			}
		}
		private int ReadFrames(string line, ref int start) {
			bool foundFrames = false;
			int frames = 0;

			while (start < line.Length) {
				char c = line[start];

				if (!foundFrames) {
					if (char.IsDigit(c)) {
						foundFrames = true;
						frames = c ^ 0x30;
						if (c == '0') { ZeroPadding = 1; }
					} else if (c != ' ') {
						return frames;
					}
				} else if (char.IsDigit(c)) {
					if (frames < 9999) {
						frames = frames * 10 + (c ^ 0x30);
						if (c == '0' && frames == 0) { ZeroPadding++; }
					} else {
						frames = 9999;
					}
				} else if (c != ' ') {
					return frames;
				}

				start++;
			}

			return frames;
		}
		public bool HasActions(Actions actions) {
			return (Actions & actions) != 0;
		}
		public override string ToString() {
			return Frames == 0 ? Notes : Frames.ToString().PadLeft(ZeroPadding, '0').PadLeft(4, ' ') + ActionsToString();
		}
		public string ActionsToString() {
			StringBuilder sb = new StringBuilder();
			if (HasActions(Actions.Left)) { sb.Append(",L"); }
			if (HasActions(Actions.Right)) { sb.Append(",R"); }
			if (HasActions(Actions.Up)) { sb.Append(",U"); }
			if (HasActions(Actions.Down)) { sb.Append(",D"); }
			if (HasActions(Actions.Jump)) { sb.Append(",J"); }
			if (HasActions(Actions.Positive)) { sb.Append(",P"); }
			if (HasActions(Actions.Negative)) { sb.Append(",N"); }
			if (HasActions(Actions.Blink)) { sb.Append(",B"); }
			if (HasActions(Actions.CloakPositive)) { sb.Append(",]"); }
			if (HasActions(Actions.CloakNegative)) { sb.Append(",["); }
			if (HasActions(Actions.Start)) { sb.Append(",S"); }
			if (HasActions(Actions.Map)) { sb.Append(",M"); }
			if (HasActions(Actions.ToggleCloak)) { sb.Append(",C"); }
			return sb.ToString();
		}
		public override bool Equals(object obj) {
			return obj is InputRecord && ((InputRecord)obj) == this;
		}
		public override int GetHashCode() {
			return Frames ^ (int)Actions;
		}
		public static bool operator ==(InputRecord one, InputRecord two) {
			bool oneNull = (object)one == null;
			bool twoNull = (object)two == null;
			if (oneNull != twoNull) {
				return false;
			} else if (oneNull && twoNull) {
				return true;
			}
			return one.Frames == two.Frames && one.Actions == two.Actions;
		}
		public static bool operator !=(InputRecord one, InputRecord two) {
			bool oneNull = (object)one == null;
			bool twoNull = (object)two == null;
			if (oneNull != twoNull) {
				return true;
			} else if (oneNull && twoNull) {
				return false;
			}
			return one.Frames != two.Frames || one.Actions != two.Actions;
		}
		public int ActionPosition() {
			return Frames == 0 ? -1 : Math.Max(4, Frames.ToString().Length);
		}
	}
}