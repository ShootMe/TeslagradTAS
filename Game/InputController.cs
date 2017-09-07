using System.Collections.Generic;
using System.IO;
using System.Text;
namespace TAS {
	public class InputController {
		private List<InputRecord> inputs = new List<InputRecord>();
		private int currentFrame, inputIndex, frameToNext;
		private string filePath;

		public InputController(string filePath) {
			this.filePath = filePath;
		}

		public bool CanPlayback { get { return inputIndex < inputs.Count; } }
		public int CurrentFrame { get { return currentFrame; } }
		public int CurrentInputFrame { get { return currentFrame - frameToNext + Current.Frames; } }
		public InputRecord Current { get; set; }
		public override string ToString() {
			if (frameToNext == 0 && Current != null) {
				return Current.ToString() + "(" + currentFrame.ToString() + ")";
			} else if (inputIndex < inputs.Count && Current != null) {
				int inputFrames = Current.Frames;
				int startFrame = frameToNext - inputFrames;
				return Current.ToString() + "(" + (currentFrame - startFrame).ToString() + " / " + inputFrames + " : " + currentFrame + ")";
			}
			return string.Empty;
		}
		public string NextInput() {
			if (frameToNext != 0 && inputIndex + 1 < inputs.Count) {
				return inputs[inputIndex + 1].ToString();
			}
			return string.Empty;
		}
		public void InitializePlayback() {
			ReadFile();

			currentFrame = 0;
			inputIndex = 0;
			if (inputs.Count > 0) {
				Current = inputs[0];
				frameToNext = Current.Frames;
			} else {
				Current = new InputRecord();
				frameToNext = 1;
			}
		}
		public void ReloadPlayback() {
			int playedBackFrames = currentFrame;
			InitializePlayback();
			currentFrame = playedBackFrames;

			while (currentFrame >= frameToNext) {
				if (inputIndex + 1 >= inputs.Count) {
					inputIndex++;
					return;
				}
				Current = inputs[++inputIndex];
				frameToNext += Current.Frames;
			}
		}
		public void InitializeRecording() {
			currentFrame = 0;
			inputIndex = 0;
			Current = new InputRecord();
			frameToNext = 0;
			inputs.Clear();
		}
		public void PlaybackPlayer() {
			if (inputIndex < inputs.Count && !Manager.IsLoading()) {
				if (currentFrame >= frameToNext) {
					if (inputIndex + 1 >= inputs.Count) {
						inputIndex++;
						return;
					}
					Current = inputs[++inputIndex];
					frameToNext += Current.Frames;
				}

				currentFrame++;
			}
		}
		public void RecordPlayer() {
			InputRecord input = new InputRecord() { Line = inputIndex + 1, Frames = currentFrame };
			Manager.GetCurrentInputs(input);

			if (currentFrame == 0 && input == Current) {
				return;
			} else if (input != Current && !Manager.IsLoading()) {
				Current.Frames = currentFrame - Current.Frames;
				inputIndex++;
				if (Current.Frames != 0) {
					inputs.Add(Current);
				}
				Current = input;
			}
			currentFrame++;
		}
		public void WriteInputs() {
			using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) {
				for (int i = 0; i < inputs.Count; i++) {
					InputRecord record = inputs[i];
					byte[] data = Encoding.ASCII.GetBytes(record.ToString() + "\r\n");
					fs.Write(data, 0, data.Length);
				}
				fs.Close();
			}
		}
		private void ReadFile() {
			inputs.Clear();
			if (!File.Exists(filePath)) { return; }

			int lines = 0;
			using (StreamReader sr = new StreamReader(filePath)) {
				while (!sr.EndOfStream) {
					string line = sr.ReadLine();

					InputRecord input = new InputRecord(++lines, line);
					if (input.Frames != 0) {
						inputs.Add(input);
					}
				}
			}
		}
	}
}