using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConsoleGUI : MonoBehaviour, IConsoleUI
{
    [SerializeField] private Transform _panel;
    [SerializeField] private TMP_InputField _tmpInputField;
    [SerializeField] private TMP_Text _tmpBuildIdText;
    [SerializeField] private TMP_Text _tmpAreaText;
    [SerializeField] private KeyCode _consoleKeyToggle;

    private  int _wantedCaretPosition = -1;

    private List<string> _lines;

    public ConsoleGUI()
    {
        _lines = new List<string>();
    }

    public ConsoleGUI(Transform panel)
    {
        this._panel = panel;
    }

    private void Awake()
    {
        _tmpInputField.onEndEdit.AddListener(OnSubmit);
    }

    private void OnSubmit(string value)
    {
        if (!Input.GetKey(KeyCode.Return)
            && !Input.GetKey(KeyCode.KeypadEnter))
        {
            return;
        }

        _tmpInputField.text = "";
        _tmpInputField.ActivateInputField();

        Console.EnqueueCommand(value);
    }


    public void Init()
    {
        var gameBuildId = 0.5f;
        _tmpBuildIdText.text = gameBuildId + " (" + Application.unityVersion + " )";
    }

    public void Shutdown()
    {
        
    }

    public void OutputString(string s)
    {
        _lines.Add(s);
        var count = Math.Min(100, _lines.Count);
        var start = _lines.Count - count;
        _tmpAreaText.text = string.Join("\n", _lines.GetRange(start, count).ToArray());
    }

    public void ClearString()
    {
        _lines.Clear();
        _tmpAreaText.text = "";
    }

    public bool IsOpen()
    {
        return _panel.gameObject.activeSelf;
    }

    public void SetOpen(bool open)
    {
        _panel.gameObject.SetActive(open);
        if(open) _tmpInputField.ActivateInputField();
    }

    public void ConsoleUpdate()
    {
        //按F1打开关闭终端
        if (Input.GetKeyDown(_consoleKeyToggle)
            || Input.GetKeyDown(KeyCode.Backslash))
        {
            SetOpen(!IsOpen());
        }

        if (!IsOpen())
        {
            return;
        }

        _tmpInputField.ActivateInputField();

        //按Tab键自动补全
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (_tmpInputField.caretPosition == _tmpInputField.text.Length
                && _tmpInputField.text.Length > 0)
            {
                var res = Console.TabComplete(_tmpInputField.text);
                _tmpInputField.text = res;
                _tmpInputField.caretPosition = res.Length;
            }
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            _tmpInputField.text = Console.HistoryUp(_tmpInputField.text);
            _wantedCaretPosition = _tmpInputField.text.Length;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            _tmpInputField.text = Console.HistoryDown();
            _wantedCaretPosition = _tmpInputField.text.Length;
        }
    }

    public void ConsoleLateUpdate()
    {
        if (_wantedCaretPosition > -1)
        {
            _tmpInputField.caretPosition = _wantedCaretPosition;
            _wantedCaretPosition = -1;
        }
    }
}
