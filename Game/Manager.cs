using J2i.Net.XInputWrapper;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
namespace TAS {
	[Flags]
	public enum State {
		None = 0,
		Enable = 1,
		Record = 2,
		FrameStep = 4,
		Disable = 8,
		Save = 16,
		Load = 32,
		Kill = 64
	}
	public class Manager {
		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		public static extern short GetAsyncKeyState(Keys vkey);
		public static bool Running, Recording, CanBlink;
		private static InputController controller;
		private static State state, nextState;
		private static int frameRate;
		private static XboxController xbox;
		public static string CurrentStatus;
		public static Vector3 PlayerVelocity;
		public static Vector3 LastPlayerPosition;
		public static float CurrentTime;
		public static SavedGame savedGame = new SavedGame();
		static Manager() {
			controller = new InputController("Teslagrad.tas");
			xbox = XboxController.RetrieveController(0);
			XboxController.UpdateFrequency = 30;
			XboxController.StartPolling();
		}
		private static bool IsKeyDown(Keys key) {
			return (GetAsyncKeyState(key) & 32768) == 32768;
		}
		public static bool IsLoading() {
			return CameraController.controller.loading > 0;
		}
		public static void UpdateInputs() {
			HandleFrameRates();
			CheckControls();
			FrameStepping();
			PlayerVelocity = (Player.position - LastPlayerPosition) * 150;
			LastPlayerPosition = Player.position;
			CurrentTime = Time.time;
			if (!Application.runInBackground) {
				Application.runInBackground = true;
			}

			if (HasFlag(state, State.Enable)) {
				Running = true;
				if (HasFlag(state, State.Record)) {
					controller.RecordPlayer();
				} else {
					controller.PlaybackPlayer();

					if (!controller.CanPlayback) {
						DisableRun();
					}
				}
				string status = controller.Current.Line + "[" + controller.ToString() + "]";
				CurrentStatus = status;
			} else {
				Running = false;
				CurrentStatus = null;
			}
		}
		public static bool GetInput(InputType button) {
			InputRecord input = controller.Current;
			switch (button) {
				case InputType.MoveLeft: return input.HasActions(Actions.Left);
				case InputType.MoveRight: return input.HasActions(Actions.Right);
				case InputType.MoveUp: return input.HasActions(Actions.Up);
				case InputType.MoveDown: return input.HasActions(Actions.Down);
				case InputType.Jump: return input.HasActions(Actions.Jump);
				case InputType.Positive: return input.HasActions(Actions.Positive);
				case InputType.Negative: return input.HasActions(Actions.Negative);
				case InputType.Blink: return input.HasActions(Actions.Blink);
				case InputType.SuitPositive: return input.HasActions(Actions.CloakPositive);
				case InputType.SuitNegative: return input.HasActions(Actions.CloakNegative);
				case InputType.Menu: return input.HasActions(Actions.Start);
				case InputType.Map: return input.HasActions(Actions.Map);
				case InputType.Suit: return input.HasActions(Actions.ToggleCloak);
			}
			return false;
		}
		private static void HandleFrameRates() {
			if (HasFlag(state, State.Enable) && !HasFlag(state, State.FrameStep) && !HasFlag(state, State.Record)) {
				float rightStickX = (float)xbox.RightThumbStickX / 32768f;
				if (IsKeyDown(Keys.LShiftKey)) {
					rightStickX = -0.65f;
				} else if (IsKeyDown(Keys.RShiftKey)) {
					rightStickX = 1f;
				}

				if (rightStickX <= -0.9) {
					SetFrameRate(3);
				} else if (rightStickX <= -0.8) {
					SetFrameRate(10);
				} else if (rightStickX <= -0.7) {
					SetFrameRate(30);
				} else if (rightStickX <= -0.6) {
					SetFrameRate(50);
				} else if (rightStickX <= -0.5) {
					SetFrameRate(70);
				} else if (rightStickX <= -0.4) {
					SetFrameRate(90);
				} else if (rightStickX <= -0.3) {
					SetFrameRate(110);
				} else if (rightStickX <= -0.2) {
					SetFrameRate(130);
				} else if (rightStickX <= 0.2) {
					SetFrameRate();
				} else if (rightStickX <= 0.3) {
					SetFrameRate(200);
				} else if (rightStickX <= 0.4) {
					SetFrameRate(250);
				} else if (rightStickX <= 0.5) {
					SetFrameRate(300);
				} else if (rightStickX <= 0.6) {
					SetFrameRate(350);
				} else if (rightStickX <= 0.7) {
					SetFrameRate(400);
				} else if (rightStickX <= 0.8) {
					SetFrameRate(450);
				} else if (rightStickX <= 0.9) {
					SetFrameRate(500);
				} else {
					SetFrameRate(600);
				}
			} else {
				SetFrameRate();
			}
		}
		private static void SetFrameRate(int newFrameRate = 150) {
			frameRate = newFrameRate;
			Time.timeScale = (float)newFrameRate / 150f;
			Time.captureFramerate = newFrameRate;
			Application.targetFrameRate = newFrameRate;
			Time.fixedDeltaTime = 1f / 150f;
			Time.maximumDeltaTime = Time.fixedDeltaTime;
			QualitySettings.vSyncCount = 0;
		}
		private static void FrameStepping() {
			bool rightTrigger = xbox.RightTrigger >= 245;
			bool dpadUp = xbox.IsDPadUpPressed || IsKeyDown(Keys.OemOpenBrackets);

			if (HasFlag(state, State.Enable) && !HasFlag(state, State.Record) && (HasFlag(state, State.FrameStep) || (dpadUp && !rightTrigger))) {
				bool continueLoop = dpadUp;
				while (HasFlag(state, State.Enable)) {
					float rightStickX = (float)xbox.RightThumbStickX / 32768f;
					if (IsKeyDown(Keys.RShiftKey)) {
						rightStickX = 0.65f;
					}
					rightTrigger = xbox.RightTrigger >= 245;
					dpadUp = xbox.IsDPadUpPressed || IsKeyDown(Keys.OemOpenBrackets);
					bool dpadDown = xbox.IsDPadDownPressed || IsKeyDown(Keys.OemCloseBrackets);

					CheckControls();
					if (!continueLoop && ((dpadUp && !rightTrigger))) {
						state |= State.FrameStep;
						break;
					} else if (dpadDown && !rightTrigger) {
						state &= ~State.FrameStep;
						break;
					} else if (rightStickX >= 0.2) {
						state |= State.FrameStep;
						int sleepTime = 0;
						if (rightStickX <= 0.3) {
							sleepTime = 200;
						} else if (rightStickX <= 0.4) {
							sleepTime = 100;
						} else if (rightStickX <= 0.5) {
							sleepTime = 80;
						} else if (rightStickX <= 0.6) {
							sleepTime = 64;
						} else if (rightStickX <= 0.7) {
							sleepTime = 48;
						} else if (rightStickX <= 0.8) {
							sleepTime = 32;
						} else if (rightStickX <= 0.9) {
							sleepTime = 16;
						}
						Thread.Sleep(sleepTime);
						break;
					}
					continueLoop = dpadUp;
					Thread.Sleep(1);
				}
				ReloadRun();
			}
		}
		private static void CheckControls() {
			bool openBracket = IsKeyDown(Keys.ControlKey) && IsKeyDown(Keys.OemOpenBrackets);
			bool closeBrackets = IsKeyDown(Keys.ControlKey) && IsKeyDown(Keys.OemCloseBrackets);
			bool backSpace = IsKeyDown(Keys.ControlKey) && IsKeyDown(Keys.Back);
			bool leftStick = xbox.IsLeftStickPressed || backSpace;
			bool rightStick = xbox.IsRightStickPressed || openBracket || closeBrackets;
			bool dpadDown = xbox.IsDPadDownPressed;
			bool dpadUp = xbox.IsDPadUpPressed;
			bool dpadRight = xbox.IsDPadRightPressed || (IsKeyDown(Keys.ControlKey) && IsKeyDown(Keys.K));

			if (!HasFlag(state, State.Enable) && rightStick) {
				nextState |= State.Enable;
			} else if (HasFlag(state, State.Enable) && rightStick) {
				nextState |= State.Disable;
			} else if (!HasFlag(state, State.Enable) && !HasFlag(state, State.Record) && leftStick) {
				nextState |= State.Record;
			} else if (!HasFlag(state, State.Enable) && !HasFlag(state, State.Record) && dpadUp) {
				nextState |= State.Save;
			} else if (!HasFlag(state, State.Enable) && !HasFlag(state, State.Record) && dpadDown) {
				nextState |= State.Load;
			} else if (!HasFlag(state, State.Enable) && !HasFlag(state, State.Record) && dpadRight) {
				nextState |= State.Kill;
			}

			if (!rightStick && HasFlag(nextState, State.Enable)) {
				EnableRun();
			} else if (!rightStick && HasFlag(nextState, State.Disable)) {
				DisableRun();
			} else if (!leftStick && HasFlag(nextState, State.Record)) {
				RecordRun();
			} else if (!dpadDown && HasFlag(nextState, State.Save)) {
				nextState &= ~State.Save;
				SaveGame(SavedGame._savedGame);
			} else if (!dpadDown && HasFlag(nextState, State.Load)) {
				nextState &= ~State.Load;
				if (!string.IsNullOrEmpty(savedGame.scene)) {
					LoadGame(SavedGame._savedGame);
					MenuPrefabLoader.loader.ShowMenu();
					SavedGame.SaveBossDefeat((Boss)0);
					Scene.allScenes.Clear();
					Application.LoadLevel("100 Menu Cutscene");
				}
			} else if (!dpadRight && HasFlag(nextState, State.Kill)) {
				nextState &= ~State.Kill;
				if (Player.player != null) {
					Player.player.Die(CauseOfDeath.KillZone, true);
				}
			}
		}
		private static void SaveGame(SavedGame save) {
			savedGame.glove = save.glove;
			savedGame.blink = save.blink;
			savedGame.suit = save.suit;
			savedGame.staff = save.staff;
			savedGame.defeatedBosses = save.defeatedBosses;
			savedGame.openBarriers = save.openBarriers;
			savedGame.legacyOrbsFound = save.legacyOrbsFound;
			savedGame.scene = Player.player.checkpoint.GetSceneName();
			savedGame.sceneIndex = save.sceneIndex;
			savedGame.checkpointIndex = save.checkpointIndex;
			savedGame.gameComplete = save.gameComplete;
			savedGame.savedGameSlot = save.savedGameSlot;
			savedGame.orbsFound = new BoolArray();
			savedGame.orbsFound.AssignByString(save.orbsFound.ToString());
			savedGame.scenesOnMap = new BoolArray();
			savedGame.scenesOnMap.AssignByString(save.scenesOnMap.ToString());
			savedGame.secretSceneExtensionsOnMap = new BoolArray();
			savedGame.secretSceneExtensionsOnMap.AssignByString(save.secretSceneExtensionsOnMap.ToString());
		}
		private static void LoadGame(SavedGame save) {
			save.glove = savedGame.glove;
			save.blink = savedGame.blink;
			save.suit = savedGame.suit;
			save.staff = savedGame.staff;
			save.defeatedBosses = savedGame.defeatedBosses;
			save.openBarriers = savedGame.openBarriers;
			save.legacyOrbsFound = savedGame.legacyOrbsFound;
			save.sceneIndex = savedGame.sceneIndex;
			save.checkpointIndex = savedGame.checkpointIndex;
			save.gameComplete = savedGame.gameComplete;
			save.savedGameSlot = savedGame.savedGameSlot;
			save.orbsFound = new BoolArray();
			save.orbsFound.AssignByString(savedGame.orbsFound.ToString());
			save.scenesOnMap = new BoolArray();
			save.scenesOnMap.AssignByString(savedGame.scenesOnMap.ToString());
			save.secretSceneExtensionsOnMap = new BoolArray();
			save.secretSceneExtensionsOnMap.AssignByString(savedGame.secretSceneExtensionsOnMap.ToString());
		}
		public static void GetCurrentInputs(InputRecord record) {
			if (TeslagradInput.GetKey(InputType.MoveLeft)) {
				record.Actions |= Actions.Left;
			}
			if (TeslagradInput.GetKey(InputType.MoveRight)) {
				record.Actions |= Actions.Right;
			}
			if (TeslagradInput.GetKey(InputType.MoveUp)) {
				record.Actions |= Actions.Up;
			}
			if (TeslagradInput.GetKey(InputType.MoveDown)) {
				record.Actions |= Actions.Down;
			}
			if (TeslagradInput.GetKey(InputType.Jump)) {
				record.Actions |= Actions.Jump;
			}
			if (TeslagradInput.GetKey(InputType.Positive)) {
				record.Actions |= Actions.Positive;
			}
			if (TeslagradInput.GetKey(InputType.Negative)) {
				record.Actions |= Actions.Negative;
			}
			if (TeslagradInput.GetKey(InputType.Blink)) {
				record.Actions |= Actions.Blink;
			}
			if (TeslagradInput.GetKey(InputType.SuitPositive)) {
				record.Actions |= Actions.CloakPositive;
			}
			if (TeslagradInput.GetKey(InputType.SuitNegative)) {
				record.Actions |= Actions.CloakNegative;
			}
			if (TeslagradInput.GetKey(InputType.Menu)) {
				record.Actions |= Actions.Start;
			}
			if (TeslagradInput.GetKey(InputType.Map)) {
				record.Actions |= Actions.Map;
			}
			if (TeslagradInput.GetKey(InputType.Suit)) {
				record.Actions |= Actions.ToggleCloak;
			}
		}
		private static void DisableRun() {
			Running = false;
			if (Recording) {
				controller.WriteInputs();
			}
			Recording = false;
			nextState &= ~State.Disable;
			state = State.None;
		}
		private static void EnableRun() {
			nextState &= ~State.Enable;
			UpdateVariables(false);
		}
		private static void RecordRun() {
			nextState &= ~State.Record;
			UpdateVariables(true);
		}
		private static void ReloadRun() {
			controller.ReloadPlayback();
		}
		private static void UpdateVariables(bool recording) {
			state |= State.Enable;
			state &= ~State.FrameStep;
			if (recording) {
				Recording = recording;
				state |= State.Record;
				controller.InitializeRecording();
			} else {
				state &= ~State.Record;
				controller.InitializePlayback();
			}
			Running = true;
		}
		private static bool HasFlag(State state, State flag) {
			return (state & flag) == flag;
		}
	}
}