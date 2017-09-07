namespace TeslagradStudio.Entities {
	public enum GameMode {
		Standard,
		TimeAttack,
		SpeedrunSelfish,
		SpeedrunFull,
		SpeedrunAny
	}
	public enum ChronometerState {
		Off,
		Running,
		Finished
	}
	public enum LockControlType {
		None,
		Locked,
		LockedNoCam,
		FreezeAll,
		LockedNoPhys
	}
	public enum PlayerState {
		Grounded,
		Jumping,
		Falling,
		StuckWall,
		StuckCeiling,
		WallBouncing,
		Bouncing,
		Swimming,
		OnGeyser,
		Dead
	}
}