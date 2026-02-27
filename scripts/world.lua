-- World setup: spawn entities and lights
-- This replaces the hardcoded entities in MainGame.Initialize
-- and the hardcoded lights in MainGame.LoadContent.

log("Setting up world...")

-- Test room entities
spawn("door", 97, 95, { start_open = false })
spawn("furniture", 97, 97)
spawn("item", 96, 96)
spawn("item", 101, 100)

-- Lights
add_light(100, 100, 255, 220, 150, 8, 1.0)
add_light(105, 98,  150, 200, 255, 6, 0.8)
add_light(96, 103,  255, 180, 100, 10, 1.0)
add_light(97, 97,   255, 200, 120, 5, 1.0)

log("World setup complete.")
