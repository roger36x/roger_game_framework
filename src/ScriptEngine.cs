using System;
using System.IO;
using NLua;

namespace Game;

public class ScriptEngine : IDisposable
{
    private Lua _lua;
    private readonly string _scriptsPath;

    public Lua LuaState => _lua;

    public ScriptEngine()
    {
        _scriptsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts");
        Initialize();
    }

    private void Initialize()
    {
        _lua = new Lua();

        // Sandbox: remove dangerous modules
        _lua.DoString(@"
            os = nil
            io = nil
            loadfile = nil
            dofile = nil
        ");
    }

    public void LoadAll()
    {
        if (!Directory.Exists(_scriptsPath))
        {
            Console.WriteLine($"[ScriptEngine] Warning: scripts directory not found at {_scriptsPath}");
            return;
        }

        string[] orderedFiles = { "entities.lua", "interactions.lua", "world.lua" };
        foreach (var filename in orderedFiles)
        {
            string path = Path.Combine(_scriptsPath, filename);
            if (File.Exists(path))
                LoadFile(path);
        }
    }

    public void LoadFile(string path)
    {
        try
        {
            string code = File.ReadAllText(path);
            _lua.DoString(code, Path.GetFileName(path));
            Console.WriteLine($"[ScriptEngine] Loaded: {Path.GetFileName(path)}");
        }
        catch (NLua.Exceptions.LuaException ex)
        {
            Console.WriteLine($"[ScriptEngine] ERROR in {Path.GetFileName(path)}: {ex.Message}");
        }
    }

    public object[] CallFunction(string functionName, params object[] args)
    {
        try
        {
            var func = _lua[functionName] as LuaFunction;
            if (func == null) return null;
            return func.Call(args);
        }
        catch (NLua.Exceptions.LuaException ex)
        {
            Console.WriteLine($"[ScriptEngine] ERROR calling {functionName}: {ex.Message}");
            return null;
        }
    }

    public void Reload()
    {
        Console.WriteLine("[ScriptEngine] Hot-reloading scripts...");
        _lua?.Dispose();
        Initialize();
    }

    public void Dispose()
    {
        _lua?.Dispose();
        _lua = null;
    }
}
