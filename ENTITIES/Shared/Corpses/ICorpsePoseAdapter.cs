// Species/rig boundary for cheap fall poses. Optional IArticulatedCorpsePose supplies a connected-body death blueprint.
public interface ICorpsePoseAdapter
{
    void Apply(float progress);
}
