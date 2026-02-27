-- Entity type definitions
-- Each define_entity() call registers a template that spawn() can reference.

define_entity("door", {
    sprite = { shape = "block", width = 64, height = 48, draw_layer = 1, offset_y = 16 },
    colors = {
        top   = {139, 90, 43},
        left  = {100, 65, 30},
        right = {120, 78, 36},
    },
    collision = { blocks = true },
    interaction = "door",
})

-- Separate template for open door (translucent)
define_entity("door_open", {
    sprite = { shape = "block", width = 64, height = 48, draw_layer = 1, offset_y = 16 },
    colors = {
        top   = {139, 90, 43, 80},
        left  = {100, 65, 30, 80},
        right = {120, 78, 36, 80},
    },
    collision = { blocks = false },
    interaction = "door",
    start_open = true,
})

define_entity("furniture", {
    sprite = { shape = "block", width = 64, height = 48, draw_layer = 1, offset_y = 16 },
    colors = { top = {160, 120, 80} },
    collision = { blocks = true },
    interaction = "push",
    pushable = true,
})

define_entity("item", {
    sprite = { shape = "diamond", width = 10, height = 10, draw_layer = 2 },
    colors = { top = {255, 215, 0} },
    interaction = "pickup",
    pickupable = true,
})
