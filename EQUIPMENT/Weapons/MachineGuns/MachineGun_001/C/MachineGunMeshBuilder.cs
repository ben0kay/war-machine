// MachineGun_001 visual pass: compact detailed light machine gun with shaped receiver,
// side ammunition box, layered feed cover, barrel shroud, muzzle brake, carrying handle and folded bipod.
// The original weapon mount, -Z muzzle, scene transform and firing mechanics remain unchanged.
using Godot;

public partial class MachineGunMeshBuilder : Node3D
{
	private static readonly StandardMaterial3D Gunmetal = new()
	{ AlbedoColor = new Color(0.13f, 0.18f, 0.18f), Metallic = 0.55f, Roughness = 0.54f };
	private static readonly StandardMaterial3D Dark = new()
	{ AlbedoColor = new Color(0.07f, 0.085f, 0.09f), Metallic = 0.35f, Roughness = 0.72f };
	private static readonly StandardMaterial3D Polymer = new()
	{ AlbedoColor = new Color(0.29f, 0.34f, 0.27f), Roughness = 0.88f };
	private static readonly StandardMaterial3D Accent = new()
	{ AlbedoColor = new Color(0.40f, 0.44f, 0.27f), Metallic = 0.30f, Roughness = 0.65f };
	private static readonly StandardMaterial3D Brass = new()
	{ AlbedoColor = new Color(0.60f, 0.45f, 0.23f), Metallic = 0.65f, Roughness = 0.48f };

	// =========================================================
	// Pure shape/detail upgrade: original bounding envelope and Muzzle at (0,0.06,-1.20) retained.
	public override void _Ready()
	{
		if (GetNodeOrNull<Node3D>("Generated") != null) return;
		var art = new Node3D { Name = "Generated" }; AddChild(art);

		// Wider support stock, slightly tapered neck, functional cheek pad and a shallow shoulder pad.
		Part(art, "Buttstock", new(0, 0.009f, 0.331f), new(0.155f, 0.150f, 0.408f), Polymer,
			WeaponVisualGeometry.TaperForward);
		Part(art, "StockNeck", new(0, 0.020f, 0.127f), new(0.11f, 0.11f, 0.178f), Gunmetal);
		Part(art, "ButtPad", new(0, 0.010f, 0.570f), new(0.170f, 0.213f, 0.071f), Dark);
		Part(art, "ShoulderPocket", new(0, -0.063f, 0.541f), new(0.136f, 0.055f, 0.039f), Dark);
		Part(art, "CheekRiser", new(0, 0.108f, 0.311f), new(0.123f, 0.054f, 0.261f), Dark);
		Part(art, "StockSpine", new(0, 0.070f, 0.369f), new(0.047f, 0.024f, 0.266f), Accent);

		// Swept pistol grip, inset ribs and a visible rather than painted trigger well.
		Part(art, "PistolGrip", new(0, -0.140f, 0.068f), new(0.100f, 0.256f, 0.126f), Dark,
			WeaponVisualGeometry.TaperRear, new Vector3(-0.16f, 0, 0));
		for (int i = 0; i < 3; i++)
			Part(art, "GripGroove", new(0.052f, -0.13f - i * 0.053f, 0.07f),
				new(0.007f, 0.011f, 0.088f), Accent, WeaponVisualGeometry.Box);
		Part(art, "TriggerHousing", new(0, -0.066f, -0.052f), new(0.115f, 0.086f, 0.166f), Gunmetal);
		Part(art, "TriggerGuardFront", new(0, -0.111f, -0.131f),
			new(0.018f, 0.093f, 0.017f), Dark, WeaponVisualGeometry.Box);
		Part(art, "TriggerGuardBottom", new(0, -0.153f, -0.077f),
			new(0.018f, 0.014f, 0.116f), Gunmetal, WeaponVisualGeometry.Box);
		Part(art, "Trigger", new(0, -0.107f, -0.058f), new(0.017f, 0.057f, 0.014f), Dark,
			WeaponVisualGeometry.Box, new Vector3(-0.10f, 0, 0));

		// Angled-edge receiver panels, ejection cover and multi-part top/feed assembly.
		Part(art, "MainReceiver", new(0, 0.047f, -0.200f), new(0.212f, 0.183f, 0.465f), Gunmetal);
		Part(art, "ReceiverUnderside", new(0, -0.052f, -0.198f), new(0.152f, 0.055f, 0.390f), Dark);
		Part(art, "ReceiverTop", new(0, 0.147f, -0.176f), new(0.161f, 0.046f, 0.420f), Dark);
		Part(art, "HingedFeedCover", new(0, 0.172f, -0.216f), new(0.178f, 0.035f, 0.332f), Accent);
		Part(art, "FeedCoverLip", new(0, 0.196f, -0.204f), new(0.134f, 0.012f, 0.355f), Gunmetal,
			WeaponVisualGeometry.Box);
		Part(art, "EjectionPort", new(-0.111f, 0.057f, -0.231f),
			new(0.007f, 0.072f, 0.188f), Dark, WeaponVisualGeometry.Box);
		Part(art, "BoltSlide", new(-0.120f, 0.050f, -0.250f),
			new(0.015f, 0.030f, 0.100f), Accent);
		Tube(art, "ChargingHandle", new(-0.137f, 0.053f, 0.007f), 0.049f, 0.058f, Dark);
		for (int i = 0; i < 3; i++)
			Part(art, "FeedHinge", new(0.099f, 0.164f, -0.070f - i * 0.122f),
				new(0.017f, 0.026f, 0.024f), Gunmetal);

		// Side-hung ammunition box remains a distinct heavier silhouette without getting larger.
		Part(art, "AmmoBracket", new(0.108f, -0.023f, -0.130f),
			new(0.042f, 0.105f, 0.265f), Gunmetal);
		Part(art, "AmmoBox", new(0.208f, -0.100f, -0.155f),
			new(0.223f, 0.232f, 0.343f), Polymer, WeaponVisualGeometry.TaperForward);
		Part(art, "AmmoBoxLid", new(0.207f, 0.028f, -0.153f),
			new(0.246f, 0.038f, 0.359f), Dark);
		Part(art, "AmmoBoxLatch", new(0.325f, -0.015f, -0.156f),
			new(0.012f, 0.083f, 0.052f), Accent, WeaponVisualGeometry.Box);
		for (int i = 0; i < 3; i++)
			Part(art, "AmmoBoxReinforcement", new(0.325f, -0.09f, -0.27f + i * 0.112f),
				new(0.008f, 0.135f, 0.019f), Gunmetal, WeaponVisualGeometry.Box);
		Part(art, "FeedChute", new(0.115f, 0.071f, -0.178f),
			new(0.130f, 0.070f, 0.184f), Gunmetal);
		for (int i = 0; i < 5; i++)
			Tube(art, "FeedRound", new(0.123f + i * 0.012f, 0.112f, -0.267f + i * 0.040f),
				0.019f, 0.035f, Brass);

		// Ribbed heat shroud with inset vent slots; outer width matches the old 0.21m handguard.
		Part(art, "ForeEnd", new(0, 0.056f, -0.552f),
			new(0.209f, 0.150f, 0.342f), Polymer, WeaponVisualGeometry.TaperRear);
		Part(art, "BarrelHousing", new(0, 0.143f, -0.556f),
			new(0.165f, 0.034f, 0.328f), Dark);
		foreach (float x in new[] { -0.107f, 0.107f })
		{
			for (int i = 0; i < 4; i++)
				Part(art, "CoolingSlot", new(x, 0.064f, -0.449f - i * 0.073f),
					new(0.009f, 0.041f, 0.043f), Dark, WeaponVisualGeometry.Box);
			Part(art, "SideRail", new(x, -0.001f, -0.562f),
				new(0.018f, 0.014f, 0.293f), Gunmetal, WeaponVisualGeometry.Box);
		}
		for (int i = 0; i < 4; i++)
			Part(art, "ShroudBand", new(0, 0.150f, -0.442f - i * 0.079f),
				new(0.191f, 0.010f, 0.016f), Gunmetal, WeaponVisualGeometry.Box);

		Tube(art, "HeavyBarrel", new(0, 0.060f, -0.905f), 0.076f, 0.483f, Gunmetal);
		Tube(art, "BarrelCollar", new(0, 0.060f, -0.754f), 0.107f, 0.071f, Dark);
		Tube(art, "MuzzleBrake", new(0, 0.060f, -1.134f), 0.123f, 0.117f, Dark);
		for (float side = -1; side <= 1; side += 2)
			Part(art, "BrakeVent", new(side * 0.062f, 0.061f, -1.141f),
				new(0.009f, 0.049f, 0.040f), Gunmetal, WeaponVisualGeometry.Box);
		Tube(art, "Bore", new(0, 0.060f, -1.196f), 0.052f, 0.004f, Dark);

		// Raised carrying handle has actual open air under the beam.
		foreach (float z in new[] { -0.340f, -0.065f })
			Part(art, "CarryHandlePost", new(0, 0.239f, z),
				new(0.047f, 0.109f, 0.041f), Dark);
		Part(art, "CarryHandleBridge", new(0, 0.300f, -0.202f),
			new(0.063f, 0.043f, 0.346f), Accent);
		Part(art, "HandleRubberInset", new(0, 0.319f, -0.202f),
			new(0.065f, 0.012f, 0.225f), Dark, WeaponVisualGeometry.Box);

		// Protective sight ears, rails and a distinctly folded bipod under the front shroud.
		Part(art, "RearSight", new(0, 0.204f, 0.004f), new(0.098f, 0.066f, 0.047f), Dark);
		foreach (float x in new[] { -0.046f, 0.046f })
			Part(art, "FrontSightEar", new(x, 0.162f, -1.030f),
				new(0.018f, 0.151f, 0.039f), Gunmetal);
		Part(art, "FrontSightPost", new(0, 0.172f, -1.030f),
			new(0.018f, 0.078f, 0.027f), Dark, WeaponVisualGeometry.Box);
		foreach (float x in new[] { -0.091f, 0.091f })
		{
			Tube(art, "BipodHinge", new(x, -0.050f, -0.706f), 0.059f, 0.058f, Dark);
			Part(art, "FoldedBipodLeg", new(x, -0.083f, -0.802f),
				new(0.029f, 0.061f, 0.328f), Gunmetal);
			Part(art, "BipodFoot", new(x, -0.126f, -0.938f),
				new(0.060f, 0.025f, 0.081f), Dark);
		}

		SetProcess(false); SetPhysicsProcess(false);
	}

	// =========================================================
	private static void Part(Node3D parent, string name, Vector3 pos, Vector3 size,
		Material material, Mesh? mesh = null, Vector3? rotate = null)
		=> WeaponVisualGeometry.Part(parent, name, pos, size, material, mesh, rotate);

	private static void Tube(Node3D parent, string name, Vector3 pos, float diameter, float length, Material material)
		=> WeaponVisualGeometry.Tube(parent, name, pos, diameter, length, material);
}
