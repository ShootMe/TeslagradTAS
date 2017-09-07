using System;
using System.Diagnostics;
namespace TeslagradStudio.Entities {
	public class TeslagradMemory {
		private static ProgramPointer Player = new ProgramPointer(false, new ProgramSignature(PointerVersion.V1, "DFF1DDD87A02761FE8????????D905????????DFF1DDD87A02730CD905|12"));
		private static ProgramPointer TAS = new ProgramPointer(false, new ProgramSignature(PointerVersion.V1, "83C4108BC8B8????????890883EC0C6A1EE8????????83C410E8????????C9C3|-26"));
		public Process Program { get; set; }
		public bool IsHooked { get; set; } = false;
		private DateTime lastHooked;

		public TeslagradMemory() {
			lastHooked = DateTime.MinValue;
		}

		public int CheckpointIndex() {
			return Player.Read<int>(Program, 0x0, 0x3c, 0x20);
		}
		public string CheckpointScene() {
			return Player.Read(Program, 0x0, 0x3c, 0x1c);
		}
		public bool CanBlink() {
			bool canBlink = Player.Read<bool>(Program, 0x0, 0x131);
			bool shooting = Player.Read<bool>(Program, 0x0, 0x100);
			bool haveBlink = Player.Read<bool>(Program, 0x0, 0xd1);
			float time = TAS.Read<float>(Program, 0x20);
			float lastTime = Player.Read<float>(Program, 0x0, 0x18c);
			float cooldown = Player.Read<float>(Program, 0x0, 0x188);
			return canBlink && haveBlink && !shooting && time - lastTime > cooldown;
		}
		public bool CarryingCrown() {
			return Player.Read<bool>(Program, 0x0, 0x13e);
		}
		public Vector2 PlayerVelocity() {
			//PlayerController.Instance._velocity
			float x = TAS.Read<float>(Program, 0x8);
			float y = TAS.Read<float>(Program, 0xc);
			return new Vector2(x, y);
		}
		public Vector2 PlayerPosition() {
			//PlayerController.Instance._position
			float x = Player.Read<float>(Program, 0x4);
			float y = Player.Read<float>(Program, 0x8);
			return new Vector2(x, y);
		}
		public string TASOutput() {
			return TAS.Read(Program, 0x4);
		}
		public bool HookProcess() {
			if ((Program == null || Program.HasExited) && DateTime.Now > lastHooked.AddSeconds(1)) {
				lastHooked = DateTime.Now;
				Process[] processes = Process.GetProcessesByName("Teslagrad");
				Program = processes.Length == 0 ? null : processes[0];
			}

			IsHooked = Program != null && !Program.HasExited;

			return IsHooked;
		}
		public void Dispose() {
			if (Program != null) {
				Program.Dispose();
			}
		}
	}
	public enum PointerVersion {
		V1
	}
	public class ProgramSignature {
		public PointerVersion Version { get; set; }
		public string Signature { get; set; }
		public ProgramSignature(PointerVersion version, string signature) {
			Version = version;
			Signature = signature;
		}
		public override string ToString() {
			return Version.ToString() + " - " + Signature;
		}
	}
	public class ProgramPointer {
		private int lastID;
		private DateTime lastTry;
		private ProgramSignature[] signatures;
		private int[] offsets;
		private bool is64bit;
		public IntPtr Pointer { get; private set; }
		public PointerVersion Version { get; private set; }
		public bool AutoDeref { get; private set; }

		public ProgramPointer(bool autoDeref, params ProgramSignature[] signatures) {
			AutoDeref = autoDeref;
			this.signatures = signatures;
			lastID = -1;
			lastTry = DateTime.MinValue;
		}
		public ProgramPointer(bool autoDeref, params int[] offsets) {
			AutoDeref = autoDeref;
			this.offsets = offsets;
			lastID = -1;
			lastTry = DateTime.MinValue;
		}

		public T Read<T>(Process program, params int[] offsets) where T : struct {
			GetPointer(program);
			return program.Read<T>(Pointer, offsets);
		}
		public string Read(Process program, params int[] offsets) {
			GetPointer(program);
			IntPtr ptr = (IntPtr)program.Read<uint>(Pointer, offsets);
			return program.Read(ptr, is64bit);
		}
		public void Write<T>(Process program, T value, params int[] offsets) where T : struct {
			GetPointer(program);
			program.Write<T>(Pointer, value, offsets);
		}
		public IntPtr GetPointer(Process program) {
			if ((program?.HasExited).GetValueOrDefault(true)) {
				Pointer = IntPtr.Zero;
				lastID = -1;
				return Pointer;
			} else if (program.Id != lastID) {
				Pointer = IntPtr.Zero;
				lastID = program.Id;
			}

			if (Pointer == IntPtr.Zero && DateTime.Now > lastTry.AddSeconds(1)) {
				lastTry = DateTime.Now;

				Pointer = GetVersionedFunctionPointer(program);
				if (Pointer != IntPtr.Zero) {
					is64bit = program.Is64Bit();
					Pointer = (IntPtr)program.Read<uint>(Pointer);
					if (AutoDeref) {
						if (is64bit) {
							Pointer = (IntPtr)program.Read<ulong>(Pointer);
						} else {
							Pointer = (IntPtr)program.Read<uint>(Pointer);
						}
					}
				}
			}
			return Pointer;
		}
		private IntPtr GetVersionedFunctionPointer(Process program) {
			if (signatures != null) {
				for (int i = 0; i < signatures.Length; i++) {
					ProgramSignature signature = signatures[i];

					IntPtr ptr = program.FindSignatures(signature.Signature)[0];
					if (ptr != IntPtr.Zero) {
						Version = signature.Version;
						return ptr;
					}
				}
			} else {
				IntPtr ptr = (IntPtr)program.Read<uint>(program.MainModule.BaseAddress, offsets);
				if (ptr != IntPtr.Zero) {
					return ptr;
				}
			}

			return IntPtr.Zero;
		}
	}
}
