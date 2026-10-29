# IX0 interaction prompt

One InteractionPrompt label below screen centre, composed by WorldController and inherited by FPS. Text changes only on PlayerInteraction.PromptChanged; no polling or gameplay arithmetic. MouseFilter.Ignore preserves GUI/weapon routing. It hides when interaction focus ends and disconnects on exit. Position avoids the existing bottom stamina/body/ammo/hotbar panels. Local normal/narrow viewport and mouse-capture layout checks are pending.
