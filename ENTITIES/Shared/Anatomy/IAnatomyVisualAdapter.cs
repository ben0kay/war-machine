// Presentation boundary: shared anatomy never knows species mesh names or corpse implementation.
public interface IAnatomyVisualAdapter
{
    void Apply(in AnatomySnapshot snapshot);
}
