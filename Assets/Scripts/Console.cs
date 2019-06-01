using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

public interface IConsoleUI
{
    void Init();
    void Shutdown();
    void OutputString(string message);
    void ClearString();
    bool IsOpen();
    void SetOpen(bool open);
    void ConsoleUpdate();
    void ConsoleLateUpdate();
}

public class Console
{
    public delegate void MethodDelegate(string[] args);

    class ConsoleCommand
    {
        public readonly string Name;
        public MethodDelegate Method;
        public readonly string Description;
        public readonly int Tag;

        public ConsoleCommand(string name, MethodDelegate method, string description, int tag)
        {
            this.Name = name;
            this.Method = method;
            this.Description = description;
            this.Tag = tag;
        }
    }

    public static void AddCommand(string name, MethodDelegate method, string description, int tag = 0)
    {
        name = name.ToLower();
        if (Commands.ContainsKey(name))
        {
            return;
        }
        Commands.Add(name, new ConsoleCommand(name, method, description, tag));
    }

    public static bool RemoveCommand(string name)
    {
        return Commands.Remove(name.ToLower());
    }

    public static void RemoveCommandsWithTag(int tag)
    {
        var removals =new List<string>();
        foreach (var c in Commands)
        {
            if (c.Value.Tag == tag)
            {
                removals.Add(c.Key);
            }
        }

        foreach (var c in removals)
        {
            RemoveCommand(c);
        }
    }

    public static void Init(IConsoleUI consoleUi)
    {
        _consoleUi = consoleUi;
        consoleUi.Init();

        AddCommand("help", CmdHelp, "Show available commands");
        AddCommand("clear", CmdClear, "Clear area text");
        AddCommand("commit", CmdCommit, "Commit command test");

        OutputString("Console ready");
    }

    public static void CmdHelp(string[] arguments)
    {
        OutputString("Available commands:");
        foreach (var c in Commands)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("<color=#FF00FF>{0}</color>", c.Value.Name);
            sb.Append(c.Value.Description);
            OutputString(sb.ToString());
        }
    }

    public static void CmdClear(string[] arguments)
    {
        OutputString("Clear:");
        _consoleUi.ClearString();
    }

    public static void CmdCommit(string[] arguments)
    {
        OutputString("Commit:");
        OutputString("Commit command test");
    }


    public static void ConsoleUpdate()
    {
        _consoleUi.ConsoleUpdate();
        while (PendingCommands.Count > 0)
        {
            var cmd = PendingCommands[0];
            PendingCommands.RemoveAt(0);
            ExecuteCommand(cmd);
        }
    }

    public static void ExecuteCommand(string command)
    {
        var tokens = Tokenize(command);
        if (tokens.Count < 1)
        {
            return;
        }
        
        OutputString('>' + command);
        var commandName = tokens[0].ToLower();

        ConsoleCommand consoleCommand;
        if (Commands.TryGetValue(commandName, out consoleCommand))
        {
            var arguments = tokens.GetRange(1, tokens.Count - 1).ToArray();
            consoleCommand.Method(arguments);
        }
    }

    public static List<string> Tokenize(string input)
    {
        var pos = 0;
        var res = new List<string>();
        var c = 0;

        while (pos < input.Length && c++ < 10000)
        {
            SkipWhite(input, ref pos);
            if (pos == input.Length)
            {
                break;
            }

            if (input[pos] == '"' && (pos == 0 || input[pos - 1] != '\\'))
            {
                res.Add(ParseQuoted(input, ref pos));
            }
            else
            {
                res.Add(Parse(input, ref pos));
            }
        }

        return res;
    }

    public static string ParseQuoted(string input, ref int pos)
    {
        pos++;
        var stargPos = pos;
        while (pos < input.Length)
        {
            if (input[pos] == '"' && input[pos - 1] != '\\')
            {
                pos++;
                return input.Substring(stargPos, pos - stargPos - 1);
            }

            pos++;
        }

        return input.Substring(stargPos);
    }

    public static string Parse(string input, ref int pos)
    {
        int startPos = pos;
        while (pos < input.Length)
        {
            if (" \t".IndexOf(input[pos]) > -1)
            {
                return input.Substring(startPos, pos - startPos);
            }
            pos++;
        }
        return input.Substring(startPos);
    }

    public static void SkipWhite(string input, ref int pos)
    {
        while (pos < input.Length && " \t".IndexOf(input[pos]) > -1)
        {
            pos++;
        }
    }


    public static void OutputString(string message)
    {
        _consoleUi.OutputString(message);
    }

    public static string TabComplete(string prefix)
    {
        var matches = new List<string>();

        foreach (var c in Commands)
        {
            var name = c.Key;
            if (!name.StartsWith(prefix, true, null))
            {
                continue;
            }
            matches.Add(name);
        }

        if (matches.Count == 0)
        {
            return prefix;
        }

        var lcp = matches[0].Length;
        for(var i = 0; i< matches.Count - 1; i++)
        {
            lcp = Mathf.Min(lcp, CommonPrefix(matches[i], matches[i + 1]));
        }

        prefix += matches[0].Substring(prefix.Length, lcp - prefix.Length);
        if (matches.Count > 1)
        {
            foreach (var t in matches)
            {
                _consoleUi.OutputString(" " + t);
            }
        }
        else
        {
            prefix += " ";
        }

        return prefix;
    }

    public static int CommonPrefix(string a, string b)
    {
        var min = Mathf.Min(a.Length, b.Length);
        for (var i = 1; i <= min; i++)
        {
            if (!a.StartsWith(b.Substring(0, i), true, null))
            {
                return i - 1;
            }
        }

        return min;
    }

    public static void EnqueueCommandNoHistory(string command)
    {
        PendingCommands.Add(command);
    }

    public static void EnqueueCommand(string command)
    {
        History[_historyNextIndex % HistoryCount] = command;
        _historyNextIndex++;
        _historyIndex = _historyNextIndex;

        EnqueueCommandNoHistory(command);
    }


    public static string HistoryUp(string current)
    {
        if (_historyIndex == 0 || _historyNextIndex - _historyIndex >= HistoryCount - 1)
        {
            return "";
        }
        if (_historyIndex == _historyNextIndex)
        {
            History[_historyIndex % HistoryCount] = current;
        }

        _historyIndex--;

        return History[_historyIndex % HistoryCount];
    }

    public static string HistoryDown()
    {
        if (_historyIndex == _historyNextIndex)
        {
            return "";
        }

        _historyIndex++;

        return History[_historyIndex % HistoryCount];
    }


    private static readonly Dictionary<string, ConsoleCommand> Commands = new Dictionary<string, ConsoleCommand>();
    private static IConsoleUI _consoleUi;
    private const int HistoryCount = 50;
    private static readonly string[] History = new string[HistoryCount];
    private static int _historyNextIndex = 0;
    private static int _historyIndex = 0;
    private static readonly List<string> PendingCommands = new List<string>();
}

