using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace InputLagAzaltici
{
    class RegTweak
    {
        public string Group, Key, Name;
        public object Value;
        public RegistryValueKind Kind;
        public RegTweak(string g, string k, string n, object v, RegistryValueKind kind)
        { Group = g; Key = k; Name = n; Value = v; Kind = kind; }
    }

    class Opt
    {
        public string Id, Short, Text;
        public int Level;
        public CheckBox Box;
        public Label Dot;
        public Action Apply;
        public Func<bool> Check;
        public bool Applicable = true;
        public bool LastState = false;
    }
}