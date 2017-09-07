﻿using TeslagradStudio.Entities;
using TeslagradStudio.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;
using System.Drawing;
using Microsoft.Win32;
namespace TeslagradStudio {
	public partial class Studio : Form {
		private static string titleBarText = "Studio v" + Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
		private const string RegKey = "HKEY_CURRENT_USER\\SOFTWARE\\TeslagradStudio\\Form";
		[STAThread]
		public static void Main() {
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Studio());
		}

		private List<InputRecord> Lines = new List<InputRecord>();
		private TeslagradMemory memory = new TeslagradMemory();
		private int totalFrames = 0, currentFrame = 0, visibleDecimalPoints = 2, visibleWidthChange = 0;
		private bool updating = false;
		private DateTime lastChanged = DateTime.MinValue;
		private Stopwatch stopWatch = new Stopwatch();
		public Studio() {
			InitializeComponent();
			Text = titleBarText;

			InputRecord.Delimiter = (char)RegRead("delim", (int)',');
			Lines.Add(new InputRecord(""));
			EnableStudio(false);

			DesktopLocation = new Point(RegRead("x", DesktopLocation.X), RegRead("y", DesktopLocation.Y));
			Size = new Size(RegRead("w", Size.Width), RegRead("h", Size.Height));
		}
		private void TASStudio_FormClosed(object sender, FormClosedEventArgs e) {
			RegWrite("delim", (int)InputRecord.Delimiter);
			RegWrite("x", DesktopLocation.X); RegWrite("y", DesktopLocation.Y);
			RegWrite("w", Size.Width); RegWrite("h", Size.Height);
		}
		private void Studio_Shown(object sender, EventArgs e) {
			Graphics g = CreateGraphics();
			string posVel = "(0.0|0.0)(0.0|0.0)";
			SizeF size = g.MeasureString(posVel, lblStatus.Font);
			posVel = "(0.00|0.00)(0.00|0.00)";
			SizeF size2 = g.MeasureString(posVel, lblStatus.Font);
			visibleWidthChange = (int)(size2.Width - size.Width + 24);
			g.Dispose();

			Thread updateThread = new Thread(UpdateLoop);
			updateThread.IsBackground = true;
			updateThread.Start();

			DetermineDecimalPlaces();
		}
		private void Studio_Resize(object sender, EventArgs e) {
			if (visibleWidthChange == 0) { return; }

			DetermineDecimalPlaces();
		}
		private void DetermineDecimalPlaces() {
			int oldDecimalPoints = 0;
			Graphics g = CreateGraphics();
			do {
				oldDecimalPoints = visibleDecimalPoints;
				string posVel = memory.PlayerPosition().ToString(visibleDecimalPoints) + memory.PlayerVelocity().ToString(visibleDecimalPoints);
				SizeF size = g.MeasureString(posVel, lblStatus.Font);
				if (size.Width > Width - 24) {
					if (visibleDecimalPoints > 1) {
						visibleDecimalPoints--;
					}
				} else if (size.Width < Width - visibleWidthChange && visibleDecimalPoints < 6) {
					visibleDecimalPoints++;
				}
			} while (oldDecimalPoints != visibleDecimalPoints);
			g.Dispose();
		}
		private void Studio_KeyDown(object sender, KeyEventArgs e) {
			try {
				if (e.Modifiers == (Keys.Shift | Keys.Control) && e.KeyCode == Keys.S) {
					tasText.SaveNewFile();
				} else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.S) {
					tasText.SaveFile();
				} else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.O) {
					tasText.OpenFile();
				} else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.T) {
					int height = statusBar.Height;
					int increase = 18;
					if (height == 44) {
						statusBar.Height += increase;
						tasText.Height -= increase;
					} else {
						statusBar.Height -= increase;
						tasText.Height += increase;
					}
				} else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.D) {
					string newDelimiter = InputRecord.Delimiter.ToString();
					if (ShowInputDialog("Choose delimiter", ref newDelimiter) == DialogResult.OK) {
						InputRecord.Delimiter = newDelimiter[0];
						tasText.ReloadFile();
					}
				}
			} catch (Exception ex) {
				MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		private DialogResult ShowInputDialog(string title, ref string input) {
			Size size = new Size(200, 70);
			DialogResult result = DialogResult.Cancel;

			using (Form inputBox = new Form()) {
				inputBox.FormBorderStyle = FormBorderStyle.FixedDialog;
				inputBox.ClientSize = size;
				inputBox.Text = title;
				inputBox.StartPosition = FormStartPosition.CenterParent;
				inputBox.MinimizeBox = false;
				inputBox.MaximizeBox = false;

				TextBox textBox = new TextBox();
				textBox.Size = new Size(size.Width - 10, 23);
				textBox.Location = new Point(5, 5);
				textBox.Font = tasText.Font;
				textBox.Text = input;
				textBox.MaxLength = 1;
				inputBox.Controls.Add(textBox);

				Button okButton = new Button();
				okButton.DialogResult = DialogResult.OK;
				okButton.Name = "okButton";
				okButton.Size = new Size(75, 23);
				okButton.Text = "&OK";
				okButton.Location = new Point(size.Width - 80 - 80, 39);
				inputBox.Controls.Add(okButton);

				Button cancelButton = new Button();
				cancelButton.DialogResult = DialogResult.Cancel;
				cancelButton.Name = "cancelButton";
				cancelButton.Size = new Size(75, 23);
				cancelButton.Text = "&Cancel";
				cancelButton.Location = new Point(size.Width - 80, 39);
				inputBox.Controls.Add(cancelButton);

				inputBox.AcceptButton = okButton;
				inputBox.CancelButton = cancelButton;

				result = inputBox.ShowDialog(this);
				input = textBox.Text;
			}
			return result;
		}
		private void UpdateLoop() {
			bool lastHooked = false;
			while (true) {
				try {
					bool hooked = memory.HookProcess();
					if (lastHooked != hooked) {
						lastHooked = hooked;
						this.Invoke((Action)delegate () { EnableStudio(hooked); });
					}
					if (lastChanged.AddSeconds(0.6) < DateTime.Now) {
						lastChanged = DateTime.Now;
						this.Invoke((Action)delegate () {
							if ((!string.IsNullOrEmpty(tasText.LastFileName) || !string.IsNullOrEmpty(tasText.SaveToFileName)) && tasText.IsChanged) {
								tasText.SaveFile();
							}
						});
					}
					if (hooked) {
						UpdateValues();
					}

					Thread.Sleep(14);
				} catch { }
			}
		}
		public void EnableStudio(bool hooked) {
			if (hooked) {
				string fileName = Path.Combine(Path.GetDirectoryName(memory.Program.MainModule.FileName), "Teslagrad.tas");
				if (string.IsNullOrEmpty(tasText.LastFileName)) {
					if (string.IsNullOrEmpty(tasText.SaveToFileName)) {
						tasText.OpenBindingFile(fileName, Encoding.ASCII);
					}
					tasText.LastFileName = fileName;
				}
				tasText.SaveToFileName = fileName;
				if (tasText.LastFileName != tasText.SaveToFileName) {
					tasText.SaveFile(true);
				}
				tasText.Focus();
			} else {
				lblStatus.Text = "Searching...";
			}
		}
		public void UpdateValues() {
			if (this.InvokeRequired) {
				this.Invoke((Action)UpdateValues);
			} else {
				string tas = memory.TASOutput();
				if (!string.IsNullOrEmpty(tas)) {
					int index = tas.IndexOf('[');
					string num = tas.Substring(0, index);
					int temp = 0;
					if (int.TryParse(num, out temp)) {
						temp--;
						if (tasText.CurrentLine != temp) {
							tasText.CurrentLine = temp;
						}
					}

					index = tas.IndexOf(':');
					if (index >= 0) {
						num = tas.Substring(index + 2, tas.IndexOf(')', index) - index - 2);
						if (int.TryParse(num, out temp)) {
							if (!stopWatch.IsRunning) {
								stopWatch.Start();
							}
							currentFrame = temp;
						}
					}

					index = tas.IndexOf('(');
					if (index > 0) {
						int index2 = tas.IndexOf(' ', index);
						num = tas.Substring(index + 1, index2 - index - 1);
						if (tasText.CurrentLineText != num) {
							tasText.CurrentLineText = num;
						}
					}
				} else {
					currentFrame = 0;
					if (tasText.CurrentLine >= 0) {
						tasText.CurrentLine = -1;
					}
					if (stopWatch.IsRunning) {
						stopWatch.Stop();
						Console.WriteLine(stopWatch.Elapsed);
					}
				}

				UpdateStatusBar();
			}
		}
		private void tasText_LineRemoved(object sender, LineRemovedEventArgs e) {
			int count = e.Count;
			while (count-- > 0) {
				InputRecord input = Lines[e.Index];
				totalFrames -= input.Frames;
				Lines.RemoveAt(e.Index);
			}

			UpdateStatusBar();
		}
		private void tasText_LineInserted(object sender, LineInsertedEventArgs e) {
			RichText tas = (RichText)sender;
			int count = e.Count;
			while (count-- > 0) {
				InputRecord input = new InputRecord(tas.GetLineText(e.Index + count));
				Lines.Insert(e.Index, input);
				totalFrames += input.Frames;
			}

			UpdateStatusBar();
		}
		private void UpdateStatusBar() {
			if (memory.IsHooked) {
				string posVel = memory.PlayerPosition().ToString(visibleDecimalPoints) + memory.PlayerVelocity().ToString(visibleDecimalPoints);
				if (statusBar.Height != 44) {
					posVel += "\r\nCrown: " + memory.CarryingCrown().ToString();
				}
				lblStatus.Text = "F(" + (currentFrame > 0 ? currentFrame + "/" : "") + totalFrames + ") CP(" + memory.CheckpointScene().ToString() + "-" + memory.CheckpointIndex().ToString() + ") " + (memory.CanBlink() ? "CanBlink" : "") + "\r\n" + posVel;
			} else {
				lblStatus.Text = "F(" + totalFrames + ")\r\nSearching...";
			}
		}
		private void tasText_TextChanged(object sender, TextChangedEventArgs e) {
			lastChanged = DateTime.Now;
			UpdateLines((RichText)sender, e.ChangedRange);
		}
		private void UpdateLines(RichText tas, Range range) {
			if (updating) { return; }
			updating = true;

			int start = range.Start.iLine;
			int end = range.End.iLine;
			while (start <= end) {
				InputRecord old = Lines.Count > start ? Lines[start] : null;

				string text = tas[start++].Text;

				InputRecord input = new InputRecord(text);
				if (old != null) {
					totalFrames -= old.Frames;

					string line = input.ToString();
					if (text != line) {
						if (old.Frames == 0 && old.ZeroPadding == input.ZeroPadding && old.Equals(input) && line.Length >= text.Length) {
							line = string.Empty;
						}
						Range oldRange = tas.Selection.Clone();
						tas.Selection = tas.GetLine(start - 1);
						tas.SelectedText = line;

						int actionPosition = input.ActionPosition();
						if (!string.IsNullOrEmpty(line)) {
							int index = oldRange.Start.iChar + line.Length - text.Length;
							if (index < 0) { index = 0; }
							if (index > 4) { index = 4; }
							if (old.Frames == input.Frames && old.ZeroPadding == input.ZeroPadding) { index = 4; }

							tas.Selection.Start = new Place(index, start - 1);
						}

						Text = titleBarText + " ***";
					}
					Lines[start - 1] = input;
				}

				totalFrames += input.Frames;
			}

			UpdateStatusBar();

			updating = false;
		}
		private void tasText_NoChanges(object sender, EventArgs e) {
			Text = titleBarText;
		}
		private void tasText_FileOpening(object sender, EventArgs e) {
			Lines.Clear();
			totalFrames = 0;
			UpdateStatusBar();
		}
		private void tasText_LineNeeded(object sender, LineNeededEventArgs e) {
			InputRecord record = new InputRecord(e.SourceLineText);
			e.DisplayedLineText = record.ToString();
		}
		private void tasText_FileOpened(object sender, EventArgs e) {
			try {
				tasText.SaveFile(true);
			} catch { }
		}
		private int RegRead(string name, int def) {
			object o = null;
			try {
				o = Registry.GetValue(RegKey, name, null);
			} catch { }

			if (o is int) {
				return (int)o;
			}

			return def;
		}
		private void RegWrite(string name, int val) {
			try {
				Registry.SetValue(RegKey, name, val);
			} catch { }
		}
	}
}