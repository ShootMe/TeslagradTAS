namespace TeslagradStudio.Entities {
	public class Vector2 {
		public float X { get; set; }
		public float Y { get; set; }
		public Vector2(float x, float y) {
			X = x;
			Y = y;
		}
		public override string ToString() {
			return ToString(2);
		}
		public string ToString(int decimalPoints = 2) {
			return "(" + X.ToString("0.".PadRight(decimalPoints + 2, '0')) + "|" + Y.ToString("0.".PadRight(decimalPoints + 2, '0')) + ")";
		}
	}
}
