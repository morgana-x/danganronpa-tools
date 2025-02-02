﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace LIN
{
    static class ScriptWrite
    {
        static public void WriteSource(Script s, System.IO.StreamWriter File, Game game = Game.Base, bool append = false)
        {
            Program.PrintLine("[write] writing decompiled file...");
            int indentLevel = 0;
            int shouldPlaceBracket = 0;
            int shouldPlaceBrackedChoice = 0;
            foreach (ScriptEntry e in s.ScriptData)
            {

                if (( (e.Opcode == 0x2B || e.Opcode == 0x29 || e.Opcode == 0x27) && shouldPlaceBrackedChoice > 0))
                {
                    if (shouldPlaceBrackedChoice > 0)
                    {
                        shouldPlaceBrackedChoice -= 1;
                    }
                    if (indentLevel > 0)
                    {
                        indentLevel -= 1;
                    }
                    File.Write(new string('\t', indentLevel));
                    File.Write('}');
                    File.Write('\n');
                    if (e.Opcode == 0x29 || e.Opcode == 0x27)
                    {
                        indentLevel = 0;
                    }
                }
                if ((e.Opcode == 0x29 || e.Opcode == 0x27) && shouldPlaceBracket > 0)// || (s.ScriptData.IndexOf(e) + 1 < s.ScriptData.Count) && s.ScriptData[s.ScriptData.IndexOf(e) + 1].Opcode == 0x29)))
                {

                    if ((shouldPlaceBracket > 0))
                    {
                        shouldPlaceBracket -= 1;
                    }
                    if (indentLevel > 0)
                    {
                        indentLevel -= 1;
                    }
                    File.Write(new string('\t', indentLevel));
                    File.Write('}');
                    File.Write('\n');
                }
                /*if (e.Opcode == 0x29 || e.Opcode == 0x27) // CheckObject, Check Character
                {

                    if (indentLevel > 0)
                    {
                        indentLevel -= 1;//-= 1;
                    }
                    indentLevel += 1;
                    if (shouldPlaceBracket > 0)
                    {
                        shouldPlaceBracket -= 1;
                        File.WriteLine("}");
                    }
                }*/
                if (s.ScriptData.IndexOf(e) > 1 && (s.ScriptData[s.ScriptData.IndexOf(e) - 2].Opcode == 0x3C)) // If flag check
                {
                    if (indentLevel > 0)
                    {
                        indentLevel -= 1;
                    }
                    if (shouldPlaceBracket > 0)
                    {
                        shouldPlaceBracket -= 1;
                    }
                    File.Write(new string('\t', indentLevel));
                    File.Write('}');
                    File.Write('\n');
                }

                if (indentLevel > 0)
                {
                    File.Write(new string('\t', indentLevel));

                   //File.Write(new string('\t', ((indentLevel > 0 && (e.Opcode == 0x29 || e.Opcode == 0x27)) ? indentLevel - 1 : indentLevel)));
                }
                File.Write(Opcode.GetOpName(e.Opcode, game));
                if (e.Opcode == 0x02)
                {
                    string Text = e.Text;
                    while (Text.EndsWith("\0")) Text = Text.Remove(Text.Length - 1);

                    // Escapes
                    Text = Text.Replace("\\", "\\\\");
                    Text = Text.Replace("\"", "\\\"");
                    Text = Text.Replace("\r", "\\r");
                    Text = Text.Replace("\n", "\\n");

                    File.Write("(\"" + Text + "\")");
                }
                else
                {
                    File.Write("(");
                    if (e.Args.Length > 0)
                    {
                        for (int a = 0; a < e.Args.Length; a++)
                        {
                            if (a > 0) File.Write(", ");
                            File.Write(e.Args[a].ToString());
                        }
                    }
                    File.Write(")");
                }
                File.WriteLine();
                if (e.Opcode == 0x29 || e.Opcode == 0x27 || e.Opcode == 0x3C || (e.Opcode == 0x2B)) // Check Object, Check Character, If_Flag
                {
                    File.Write(new string('\t', indentLevel));
                    File.Write('{');
                    File.Write('\n');
                    indentLevel += 1;
                    if (e.Opcode != 0x2B)
                    {
                        shouldPlaceBracket += 1;
                    }
                    else
                    {
                        shouldPlaceBrackedChoice += 1;
                    }
                    
                }
                if ((s.ScriptData.IndexOf(e) == s.ScriptData.Count - 1 && shouldPlaceBrackedChoice > 0))
                {
                    shouldPlaceBrackedChoice -= 1;
                    
                    if (indentLevel > 0)
                    {
                        indentLevel -= 1;
                    }
                    File.WriteLine(new string('\t', indentLevel) + '}');

                }
                if ((s.ScriptData.IndexOf(e) == s.ScriptData.Count - 1 && shouldPlaceBracket > 0))
                {
                    shouldPlaceBracket -= 1;
                    if (indentLevel > 0)
                    {
                        indentLevel -= 1;
                    }
                    File.WriteLine(new string('\t', indentLevel) + '}');
                }
          

            }
            if (!append)
            {
                File.Close();
            }
            Program.PrintLine("[write] done.");
        }
        static public void WriteSource(Script s, string Filename, Game game = Game.Base, bool append = false)
        {
            System.IO.StreamWriter File = new System.IO.StreamWriter(Filename, false, Encoding.Unicode);
            WriteSource(s, File, game, append);
        }

        static public void WriteCompiled(Script s, string Filename, Game game = Game.Base)
        {
            Program.PrintLine("[write] writing compiled file...");
            List<byte> File = new List<byte>();

            // Header
            File.AddRange(BitConverter.GetBytes((Int32)s.Type));
            File.AddRange(BitConverter.GetBytes(s.Type == ScriptType.Text ? 16 : 12));
            switch (s.Type)
            {
                case ScriptType.Textless:
                    File.AddRange(BitConverter.GetBytes(s.FileSize));
                    break;
                case ScriptType.Text:
                    File.AddRange(BitConverter.GetBytes(s.TextBlockPos));
                    File.AddRange(BitConverter.GetBytes(s.FileSize));
                    break;
                default: throw new Exception("[write] error: unknown script type.");
            }

            Dictionary<int, string> TextData = new Dictionary<int, string>();
            if (s.Type == ScriptType.Text)
            {
                s.TextEntries = 0;
                foreach (ScriptEntry e in s.ScriptData)
                {
                    if (e.Opcode == 0x02)
                    {
                        while (TextData.ContainsKey(s.TextEntries)) s.TextEntries++;
                        TextData.Add(s.TextEntries, e.Text);

                        e.Args[0] = (byte)(s.TextEntries >> 8 & 0xFF);
                        e.Args[1] = (byte)(s.TextEntries & 0xFF);

                        s.TextEntries++;
                    }
                }

                s.TextEntries = Math.Max(s.TextEntries, TextData.Keys.Max() + 1);
            }

            foreach (ScriptEntry e in s.ScriptData)
            {
                File.Add(0x70);
                File.Add(e.Opcode);
                File.AddRange(e.Args);
            }

            while (File.Count % 4 != 0) File.Add(0x00);

            s.TextBlockPos = File.Count;
            for (int i = 0; i < 4; i++) File[0x08 + i] = BitConverter.GetBytes(s.TextBlockPos)[i];

            if (s.Type == ScriptType.Textless)
            {
                s.FileSize = s.TextBlockPos;
            }
            else if (s.Type == ScriptType.Text)
            {
                File.AddRange(BitConverter.GetBytes(s.TextEntries));
                int[] StartPoints = new int[s.TextEntries];
                int Total = 8 + s.TextEntries * 4;

                for (int i = 0; i < s.TextEntries; i++)
                {
                    string Text = "";
                    if (TextData.ContainsKey(i)) Text = TextData[i];
                    if (!Text.EndsWith("\0")) Text += '\0';

                    byte[] ByteText = Encoding.Unicode.GetBytes(Text);
                    if (ByteText[0] != 0xFF || ByteText[1] != 0xFE)
                    {
                        byte[] Temp = new byte[ByteText.Length + 2];
                        Temp[0] = 0xFF;
                        Temp[1] = 0xFE;
                        ByteText.CopyTo(Temp, 2);
                        ByteText = Temp;
                    }

                    StartPoints[i] = Total;
                    Total += ByteText.Length;
                }

                foreach (int sp in StartPoints)
                {
                    File.AddRange(BitConverter.GetBytes(sp));
                }
                File.AddRange(BitConverter.GetBytes(Total));

                for (int i = 0; i < s.TextEntries; i++)
                {
                    string Text = "";
                    if (TextData.ContainsKey(i)) Text = TextData[i];
                    if (!Text.EndsWith("\0")) Text += '\0';

                    byte[] ByteText = Encoding.Unicode.GetBytes(Text);
                    if (ByteText[0] != 0xFF || ByteText[1] != 0xFE)
                    {
                        byte[] Temp = new byte[ByteText.Length + 2];
                        Temp[0] = 0xFF;
                        Temp[1] = 0xFE;
                        ByteText.CopyTo(Temp, 2);
                        ByteText = Temp;
                    }

                    File.AddRange(ByteText);
                }

                while (File.Count % 4 != 0) File.Add(0x00);
                s.FileSize = File.Count;
                for (int i = 0; i < 4; i++) File[0x0C + i] = BitConverter.GetBytes(s.FileSize)[i];
            }
            System.IO.File.WriteAllBytes(Filename, File.ToArray());
            Program.PrintLine("[write] done.");
        }
    }
}
