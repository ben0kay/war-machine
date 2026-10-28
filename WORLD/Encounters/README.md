# Encounters

World-owned placement only. RobotPlacement samples saved terrain, validates the route/foot corridor and initializes the existing reusable robot. Zombie offsets currently belong to WorldSettings and WorldController; zombie logic/art belongs to ENTITIES/TYPES/Zombies. No enemy implementation belongs here.

Village robot placement is now relative to VillageOriginXZ rather than player spawn. GetRouteReservations supplies scale-aware closed-edge rectangles for tree/rock exclusions; Initialize additionally rejects expanded building/site intersections on every sampled segment and scales its foot-support probes. The authored 3–4 marker route, robot patrol/scanner and dormant-on-failure lifecycle are reused. Current default loop centre (-115,-50) relative to the village, corners +/-20 X and +/-45 Z. Local patrol/terrain checks pending.
