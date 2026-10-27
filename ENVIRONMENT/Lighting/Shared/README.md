# Shared lighting behaviours — reserved

Generic, reusable local-light lifecycle helpers may live here later: cached OmniLight3D enable/disable, transient lifetime, flicker while burning and relevance controls. No runtime manager or loop is created at scaffold time. Avoid a global frame poller; do not duplicate Sky3D's WorldEnvironment or static sun/moon.
