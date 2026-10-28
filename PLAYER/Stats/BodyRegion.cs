// Player anatomy uses the actor's own left/right; ordinary capsule hits resolve Unknown to Torso.
public enum BodyRegion { Unknown = -1, Head = 0, Torso, LeftArm, RightArm, LeftLeg, RightLeg }
