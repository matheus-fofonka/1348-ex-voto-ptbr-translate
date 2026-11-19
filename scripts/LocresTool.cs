// Leitura/escrita de arquivos .locres (UE4.20+ / UE5, versoes 2 e 3) e .locmeta.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class LocresEntry
{
    public string Namespace;
    public string Key;
    public uint NamespaceHash;
    public uint KeyHash;
    public uint SourceStringHash;
    public string Text;
}

public class LocresFile
{
    static readonly byte[] Magic = { 0x0E, 0x14, 0x74, 0x75, 0x67, 0x4A, 0x03, 0xFC, 0x4A, 0x15, 0x90, 0x9D, 0xC3, 0x37, 0x7F, 0x1B };
    public byte Version;
    public List<LocresEntry> Entries = new List<LocresEntry>();

    public static LocresFile Load(string path)
    {
        var f = new LocresFile();
        using (var r = new BinaryReader(File.OpenRead(path)))
        {
            if (!r.ReadBytes(16).SequenceEqual(Magic)) throw new Exception("Nao e um .locres suportado: " + path);
            f.Version = r.ReadByte();
            if (f.Version < 2 || f.Version > 3) throw new Exception("Versao de locres nao suportada: " + f.Version);
            long strOffset = r.ReadInt64();
            r.ReadInt32(); // total de entradas
            long back = r.BaseStream.Position;
            r.BaseStream.Position = strOffset;
            int strCount = r.ReadInt32();
            var strings = new string[strCount];
            for (int i = 0; i < strCount; i++) { strings[i] = PakReader.ReadFString(r); r.ReadInt32(); }
            r.BaseStream.Position = back;
            int nsCount = r.ReadInt32();
            for (int n = 0; n < nsCount; n++)
            {
                uint nsHash = r.ReadUInt32();
                string ns = PakReader.ReadFString(r);
                int keyCount = r.ReadInt32();
                for (int k = 0; k < keyCount; k++)
                {
                    var e = new LocresEntry();
                    e.Namespace = ns; e.NamespaceHash = nsHash;
                    e.KeyHash = r.ReadUInt32();
                    e.Key = PakReader.ReadFString(r);
                    e.SourceStringHash = r.ReadUInt32();
                    e.Text = strings[r.ReadInt32()];
                    f.Entries.Add(e);
                }
            }
        }
        return f;
    }

    public void Save(string path)
    {
        var strings = new List<string>();
        var refs = new List<int>();
        var index = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var e in Entries)
        {
            int i;
            if (!index.TryGetValue(e.Text, out i)) { i = strings.Count; index[e.Text] = i; strings.Add(e.Text); refs.Add(0); }
            refs[i]++;
        }
        using (var w = new BinaryWriter(File.Create(path)))
        {
            w.Write(Magic); w.Write(Version);
            long offsetPos = w.BaseStream.Position; w.Write(0L);
            w.Write(Entries.Count);
            var groups = Entries.GroupBy(e => e.Namespace).ToList();
            w.Write(groups.Count);
            foreach (var g in groups)
            {
                w.Write(g.First().NamespaceHash);
                PakReader.WriteFString(w, g.Key);
                w.Write(g.Count());
                foreach (var e in g)
                {
                    w.Write(e.KeyHash);
                    PakReader.WriteFString(w, e.Key);
                    w.Write(e.SourceStringHash);
                    w.Write(index[e.Text]);
                }
            }
            long strOffset = w.BaseStream.Position;
            w.Write(strings.Count);
            for (int i = 0; i < strings.Count; i++) { PakReader.WriteFString(w, strings[i]); w.Write(refs[i]); }
            w.BaseStream.Position = offsetPos; w.Write(strOffset);
        }
    }
}

public static class LocmetaTool
{
    // Adiciona uma cultura a lista de culturas compiladas de um .locmeta
    public static void AddCulture(string inPath, string outPath, string culture)
    {
        string native, nativeLocres; var cultures = new List<string>(); byte[] magic; byte version;
        using (var r = new BinaryReader(File.OpenRead(inPath)))
        {
            magic = r.ReadBytes(16); version = r.ReadByte();
            native = PakReader.ReadFString(r); nativeLocres = PakReader.ReadFString(r);
            int n = r.ReadInt32();
            for (int i = 0; i < n; i++) cultures.Add(PakReader.ReadFString(r));
        }
        if (!cultures.Contains(culture)) cultures.Add(culture);
        using (var w = new BinaryWriter(File.Create(outPath)))
        {
            w.Write(magic); w.Write(version);
            PakReader.WriteFString(w, native); PakReader.WriteFString(w, nativeLocres);
            w.Write(cultures.Count);
            foreach (var c in cultures) PakReader.WriteFString(w, c);
        }
    }
}
