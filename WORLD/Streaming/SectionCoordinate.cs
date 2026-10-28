// Value identity in a world-local X/Z grid. Negative coordinates use mathematical floor, not truncation.
public readonly record struct SectionCoordinate(int X, int Z);
