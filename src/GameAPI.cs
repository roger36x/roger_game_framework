using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NLua;

namespace Game;

public class GameAPI
{
    private readonly EntityManager _entities;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly CollisionSystem _collision;
    private readonly AudioSystem _audio;
    private readonly LightingSystem _lighting;
    private readonly ScriptEngine _engine;

    private readonly Dictionary<string, EntityTemplate> _templates = new();
    private readonly Dictionary<int, string> _interactionCallbacks = new();

    public IReadOnlyDictionary<string, EntityTemplate> Templates => _templates;
    public IReadOnlyDictionary<int, string> InteractionCallbacks => _interactionCallbacks;

    public GameAPI(
        ScriptEngine engine,
        EntityManager entities,
        GraphicsDevice graphicsDevice,
        CollisionSystem collision,
        AudioSystem audio,
        LightingSystem lighting)
    {
        _engine = engine;
        _entities = entities;
        _graphicsDevice = graphicsDevice;
        _collision = collision;
        _audio = audio;
        _lighting = lighting;
    }

    public void Register()
    {
        var lua = _engine.LuaState;
        lua.RegisterFunction("define_entity", this, GetType().GetMethod(nameof(DefineEntity)));
        lua.RegisterFunction("spawn", this, GetType().GetMethod(nameof(Spawn)));
        lua.RegisterFunction("destroy", this, GetType().GetMethod(nameof(Destroy)));
        lua.RegisterFunction("get_position", this, GetType().GetMethod(nameof(GetPosition)));
        lua.RegisterFunction("set_position", this, GetType().GetMethod(nameof(SetPosition)));
        lua.RegisterFunction("play_sound", this, GetType().GetMethod(nameof(PlaySound)));
        lua.RegisterFunction("can_move_to", this, GetType().GetMethod(nameof(CanMoveTo)));
        lua.RegisterFunction("add_light", this, GetType().GetMethod(nameof(AddLight)));
        lua.RegisterFunction("log", this, GetType().GetMethod(nameof(Log)));
    }

    public void DefineEntity(string name, LuaTable table)
    {
        var template = new EntityTemplate { Name = name };

        if (table["sprite"] is LuaTable spriteTable)
        {
            template.Sprite = new SpriteTemplate
            {
                Shape = ParseShapeType(spriteTable["shape"] as string),
                Width = ToInt(spriteTable["width"], 16),
                Height = ToInt(spriteTable["height"], 16),
                DrawLayer = ToInt(spriteTable["draw_layer"], 2),
                OffsetY = ToFloat(spriteTable["offset_y"], 0f)
            };
        }

        if (table["colors"] is LuaTable colorsTable)
            template.Colors = ParseColors(colorsTable);
        else
            template.Colors = new ColorTemplate { Top = Color.Gray, Left = new Color(90, 90, 90), Right = new Color(64, 64, 64) };

        if (table["collision"] is LuaTable collisionTable)
        {
            template.HasCollision = true;
            template.BlocksMovement = ToBool(collisionTable["blocks"], true);
        }

        if (table["interaction"] is string interactionStr)
            template.InteractionType = interactionStr;

        if (table["on_interact"] is string callbackName)
        {
            template.OnInteractCallback = callbackName;
            if (template.InteractionType == null)
                template.InteractionType = "custom";
        }

        template.HasPushable = ToBool(table["pushable"], false);
        template.HasPickupable = ToBool(table["pickupable"], false);
        template.StartOpen = ToBool(table["start_open"], false);

        _templates[name] = template;
        Console.WriteLine($"[GameAPI] Registered entity template: {name}");
    }

    public int Spawn(string typeName, double x, double y, LuaTable props)
    {
        if (!_templates.TryGetValue(typeName, out var template))
        {
            Console.WriteLine($"[GameAPI] ERROR: Unknown entity type '{typeName}'");
            return 0;
        }

        int tileX = (int)Math.Round(x);
        int tileY = (int)Math.Round(y);
        int id = _entities.CreateEntity();

        // Position
        _entities.Positions[id] = new Position { TilePosition = new Vector2((float)x, (float)y) };

        // Sprite
        var texture = TextureGenerator.GetOrCreate(_graphicsDevice, template);
        _entities.Sprites[id] = new Sprite
        {
            Texture = texture,
            Width = template.Sprite.Width,
            Height = template.Sprite.Height,
            DrawLayer = template.Sprite.DrawLayer,
            OffsetY = template.Sprite.OffsetY
        };

        // Door open state
        bool isOpen = template.StartOpen;
        if (props?["start_open"] != null)
            isOpen = ToBool(props["start_open"], isOpen);

        // Collision
        if (template.HasCollision)
        {
            bool blocks = template.BlocksMovement;
            if (template.InteractionType == "door")
                blocks = !isOpen;
            _entities.Collisions[id] = new Collision { BlocksMovement = blocks };
        }

        // Interactable
        if (template.InteractionType != null)
        {
            var interType = template.InteractionType switch
            {
                "door" => Game.InteractionType.Door,
                "pickup" => Game.InteractionType.Pickup,
                "push" => Game.InteractionType.Push,
                _ => Game.InteractionType.None
            };
            _entities.Interactables[id] = new Interactable { Type = interType, IsOpen = isOpen };
        }

        if (template.HasPushable || template.InteractionType == "push")
            _entities.Pushables[id] = new Pushable();

        if (template.HasPickupable || template.InteractionType == "pickup")
            _entities.Pickupables[id] = new Pickupable();

        if (template.OnInteractCallback != null)
            _interactionCallbacks[id] = template.OnInteractCallback;

        _entities.AddToSpatialIndex(id, tileX, tileY);
        return id;
    }

    public void Destroy(double entityId)
    {
        int id = (int)entityId;
        _interactionCallbacks.Remove(id);
        _entities.DestroyEntity(id);
    }

    public LuaTable GetPosition(double entityId)
    {
        int id = (int)entityId;
        if (!_entities.Positions.TryGetValue(id, out var pos))
            return null;

        _engine.LuaState.NewTable("_tmp_pos");
        var t = _engine.LuaState.GetTable("_tmp_pos");
        t["x"] = (double)pos.TilePosition.X;
        t["y"] = (double)pos.TilePosition.Y;
        _engine.LuaState["_tmp_pos"] = null;
        return t;
    }

    public void SetPosition(double entityId, double x, double y)
    {
        int id = (int)entityId;
        if (!_entities.Positions.TryGetValue(id, out var pos))
            return;

        int oldTX = (int)MathF.Round(pos.TilePosition.X);
        int oldTY = (int)MathF.Round(pos.TilePosition.Y);
        int newTX = (int)Math.Round(x);
        int newTY = (int)Math.Round(y);

        pos.TilePosition = new Vector2((float)x, (float)y);
        _entities.Positions[id] = pos;

        if (oldTX != newTX || oldTY != newTY)
            _entities.MoveSpatialIndex(id, oldTX, oldTY, newTX, newTY);
    }

    public void PlaySound(string sfxName, double x, double y)
    {
        if (Enum.TryParse<SfxType>(sfxName, ignoreCase: true, out var sfxType))
            _audio?.PlaySfx(sfxType, (float)x, (float)y);
        else
            Console.WriteLine($"[GameAPI] Unknown sound: {sfxName}");
    }

    public bool CanMoveTo(double x, double y)
    {
        return _collision.CanMoveTo((float)x, (float)y);
    }

    public void AddLight(double tileX, double tileY, double r, double g, double b, double radius, double intensity)
    {
        _lighting.AddLight(new LightSource
        {
            TilePosition = new Vector2((float)tileX, (float)tileY),
            Color = new Color((int)r, (int)g, (int)b),
            Radius = (int)radius,
            Intensity = (float)intensity
        });
    }

    public void Log(string message)
    {
        Console.WriteLine($"[Lua] {message}");
    }

    // --- Helpers ---

    private static ShapeType ParseShapeType(string s)
    {
        return s?.ToLowerInvariant() switch
        {
            "block" => ShapeType.Block,
            "diamond" => ShapeType.Diamond,
            "rect" => ShapeType.Rect,
            _ => ShapeType.Rect
        };
    }

    private static ColorTemplate ParseColors(LuaTable table)
    {
        Color top = ParseColor(table["top"] as LuaTable) ?? Color.Gray;

        Color left = ParseColor(table["left"] as LuaTable)
                     ?? new Color((int)(top.R * 0.7f), (int)(top.G * 0.7f), (int)(top.B * 0.7f), top.A);
        Color right = ParseColor(table["right"] as LuaTable)
                      ?? new Color((int)(top.R * 0.5f), (int)(top.G * 0.5f), (int)(top.B * 0.5f), top.A);

        return new ColorTemplate { Top = top, Left = left, Right = right };
    }

    private static Color? ParseColor(LuaTable rgb)
    {
        if (rgb == null) return null;
        int r = ToInt(rgb[1L], 128);
        int g = ToInt(rgb[2L], 128);
        int b = ToInt(rgb[3L], 128);
        int a = ToInt(rgb[4L], 255);
        return new Color(r, g, b, a);
    }

    private static int ToInt(object val, int fallback)
    {
        if (val is double d) return (int)d;
        if (val is long l) return (int)l;
        if (val is int i) return i;
        return fallback;
    }

    private static float ToFloat(object val, float fallback)
    {
        if (val is double d) return (float)d;
        if (val is long l) return l;
        if (val is float f) return f;
        return fallback;
    }

    private static bool ToBool(object val, bool fallback)
    {
        if (val is bool b) return b;
        return fallback;
    }
}
