// Leitor/escritor minimo de .pak do Unreal Engine 5 (versao 11), sem dependencias.
// Le o indice (nao criptografado) e extrai arquivos sem compressao ou com Zlib.
// Escreve paks v11 sem compressao (com indices de hash de caminho e de diretorios).
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

public class PakEntryInfo
{
    public string Path;
    public long Offset;
    public long Size;
    public long UncompressedSize;
    public int CompressionMethod; // 0 = nenhum, 1.. = indice em CompressionMethods
    public bool Encrypted;
}

public class PakReader
{
    public string FilePath;
    public int Version;
    public string MountPoint;
    public ulong PathHashSeed;
    public string[] CompressionMethods = new string[5];
    public List<PakEntryInfo> Entries = new List<PakEntryInfo>();
    public Dictionary<ulong, int> PathHashIndex = new Dictionary<ulong, int>();

    const uint Magic = 0x5A6F12E1;

    public PakReader(string path)
    {
        FilePath = path;
        using (var fs = File.OpenRead(path))
        using (var br = new BinaryReader(fs))
        {
            fs.Seek(-204, SeekOrigin.End);
            if (br.ReadUInt32() != Magic) throw new Exception("Magic de pak nao encontrado (formato diferente de v11?)");
            Version = br.ReadInt32();
            long indexOffset = br.ReadInt64();
            long indexSize = br.ReadInt64();
            br.ReadBytes(20);
            for (int i = 0; i < 5; i++) CompressionMethods[i] = Encoding.ASCII.GetString(br.ReadBytes(32)).TrimEnd('\0');
            fs.Seek(-205, SeekOrigin.End);
            if (br.ReadByte() != 0) throw new Exception("Indice criptografado - nao suportado");
            if (Version < 10) throw new Exception("Versao de pak " + Version + " nao suportada");

            fs.Seek(indexOffset, SeekOrigin.Begin);
            var idx = new BinaryReader(new MemoryStream(br.ReadBytes((int)indexSize)));
            MountPoint = ReadFString(idx);
            idx.ReadInt32(); // NumEntries
            PathHashSeed = idx.ReadUInt64();
            long phOff = 0, phSize = 0, fdOff = 0, fdSize = 0;
            if (idx.ReadInt32() != 0) { phOff = idx.ReadInt64(); phSize = idx.ReadInt64(); idx.ReadBytes(20); }
            bool hasFull = idx.ReadInt32() != 0;
            if (hasFull) { fdOff = idx.ReadInt64(); fdSize = idx.ReadInt64(); idx.ReadBytes(20); }
            if (!hasFull) throw new Exception("Pak sem indice completo de diretorios - nao suportado");
            byte[] encoded = idx.ReadBytes(idx.ReadInt32());
            int nonEncoded = idx.ReadInt32();
            var legacy = new List<PakEntryInfo>();
            for (int i = 0; i < nonEncoded; i++) legacy.Add(ReadLegacyEntry(idx));

            if (phSize > 0)
            {
                fs.Seek(phOff, SeekOrigin.Begin);
                var ph = new BinaryReader(new MemoryStream(br.ReadBytes((int)phSize)));
                int n = ph.ReadInt32();
                for (int i = 0; i < n; i++) { ulong h = ph.ReadUInt64(); int loc = ph.ReadInt32(); PathHashIndex[h] = loc; }
            }

            fs.Seek(fdOff, SeekOrigin.Begin);
            var fd = new BinaryReader(new MemoryStream(br.ReadBytes((int)fdSize)));
            int dirCount = fd.ReadInt32();
            for (int d = 0; d < dirCount; d++)
            {
                string dir = ReadFString(fd);
                int fileCount = fd.ReadInt32();
                for (int f = 0; f < fileCount; f++)
                {
                    string name = ReadFString(fd);
                    int loc = fd.ReadInt32();
                    PakEntryInfo e = loc >= 0 ? DecodeEntry(encoded, loc) : legacy[-loc - 1];
                    e.Path = (dir == "/" ? "" : dir) + name;
                    Entries.Add(e);
                }
            }
        }
    }

    static PakEntryInfo DecodeEntry(byte[] buf, int pos)
    {
        var e = new PakEntryInfo();
        uint v = BitConverter.ToUInt32(buf, pos); pos += 4;
        if ((v & 0x3f) == 0x3f) pos += 4; // tamanho de bloco explicito
        e.CompressionMethod = (int)((v >> 23) & 0x3f);
        if ((v & (1u << 31)) != 0) { e.Offset = BitConverter.ToUInt32(buf, pos); pos += 4; } else { e.Offset = BitConverter.ToInt64(buf, pos); pos += 8; }
        if ((v & (1u << 30)) != 0) { e.UncompressedSize = BitConverter.ToUInt32(buf, pos); pos += 4; } else { e.UncompressedSize = BitConverter.ToInt64(buf, pos); pos += 8; }
        if (e.CompressionMethod != 0)
        {
            if ((v & (1u << 29)) != 0) { e.Size = BitConverter.ToUInt32(buf, pos); pos += 4; } else { e.Size = BitConverter.ToInt64(buf, pos); pos += 8; }
        }
        else e.Size = e.UncompressedSize;
        e.Encrypted = (v & (1u << 22)) != 0;
        return e;
    }

    static PakEntryInfo ReadLegacyEntry(BinaryReader r)
    {
        var e = new PakEntryInfo();
        e.Offset = r.ReadInt64(); e.Size = r.ReadInt64(); e.UncompressedSize = r.ReadInt64();
        e.CompressionMethod = (int)r.ReadUInt32();
        r.ReadBytes(20);
        if (e.CompressionMethod != 0) { int n = r.ReadInt32(); r.ReadBytes(16 * n); }
        e.Encrypted = r.ReadByte() != 0;
        r.ReadUInt32();
        return e;
    }

    public byte[] Extract(PakEntryInfo e)
    {
        if (e.Encrypted) throw new Exception("Arquivo criptografado: " + e.Path);
        using (var fs = File.OpenRead(FilePath))
        using (var br = new BinaryReader(fs))
        {
            fs.Seek(e.Offset, SeekOrigin.Begin);
            br.ReadInt64(); br.ReadInt64(); br.ReadInt64();
            uint method = br.ReadUInt32();
            br.ReadBytes(20);
            var blocks = new List<long[]>();
            if (method != 0)
            {
                int n = br.ReadInt32();
                for (int i = 0; i < n; i++) blocks.Add(new long[] { br.ReadInt64(), br.ReadInt64() });
            }
            br.ReadByte(); br.ReadUInt32();
            if (method == 0) return br.ReadBytes((int)e.UncompressedSize);
            string name = CompressionMethods[method - 1];
            if (!name.Equals("Zlib", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Compressao '" + name + "' nao suportada: " + e.Path);
            var outMs = new MemoryStream();
            foreach (var b in blocks)
            {
                fs.Seek(e.Offset + b[0] + 2, SeekOrigin.Begin);
                var data = br.ReadBytes((int)(b[1] - b[0] - 2));
                using (var ds = new DeflateStream(new MemoryStream(data), CompressionMode.Decompress)) ds.CopyTo(outMs);
            }
            return outMs.ToArray();
        }
    }

    public static string ReadFString(BinaryReader r)
    {
        int len = r.ReadInt32();
        if (len == 0) return "";
        if (len > 0) return Encoding.ASCII.GetString(r.ReadBytes(len)).TrimEnd('\0');
        return Encoding.Unicode.GetString(r.ReadBytes(-len * 2)).TrimEnd('\0');
    }

    public static void WriteFString(BinaryWriter w, string s)
    {
        if (s.Length == 0) { w.Write(0); return; }
        if (s.All(c => c < 128)) { w.Write(s.Length + 1); w.Write(Encoding.ASCII.GetBytes(s)); w.Write((byte)0); }
        else { w.Write(-(s.Length + 1)); w.Write(Encoding.Unicode.GetBytes(s)); w.Write((short)0); }
    }

    // Hash de caminho usado no indice de hash (FPakFile::HashPath)
    public static ulong HashPath(string relPath, ulong seed)
    {
        byte[] data = Encoding.Unicode.GetBytes(relPath.ToLowerInvariant());
        ulong h = 0xcbf29ce484222325UL + seed;
        foreach (byte b in data) { h ^= b; h *= 0x00000100000001b3UL; }
        return h;
    }
}

public static class PakWriter
{
    // files: caminho relativo ao mount point -> caminho do arquivo no disco
    public static void Write(string outPath, string mountPoint, IDictionary<string, string> files)
    {
        const ulong seed = 0;
        var sha = SHA1.Create();
        using (var fs = File.Create(outPath))
        using (var w = new BinaryWriter(fs))
        {
            var encoded = new MemoryStream();
            var ew = new BinaryWriter(encoded);
            var locations = new Dictionary<string, int>();
            foreach (var kv in files.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                byte[] data = File.ReadAllBytes(kv.Value);
                long offset = fs.Position;
                if (offset > uint.MaxValue) throw new Exception("Pak grande demais");
                w.Write(0L); w.Write((long)data.Length); w.Write((long)data.Length);
                w.Write(0u); w.Write(sha.ComputeHash(data)); w.Write((byte)0); w.Write(0u);
                w.Write(data);
                locations[kv.Key] = (int)encoded.Position;
                ew.Write((1u << 31) | (1u << 30));
                ew.Write((uint)offset);
                ew.Write((uint)data.Length);
            }

            // Indice de hash de caminho
            var ph = new MemoryStream(); var phw = new BinaryWriter(ph);
            phw.Write(locations.Count);
            foreach (var kv in locations) { phw.Write(PakReader.HashPath(kv.Key, seed)); phw.Write(kv.Value); }
            phw.Write(0);
            byte[] phBytes = ph.ToArray();

            // Indice completo de diretorios (inclui todos os diretorios-pai)
            var dirs = new SortedDictionary<string, SortedDictionary<string, int>>(StringComparer.Ordinal);
            dirs["/"] = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var kv in locations)
            {
                int slash = kv.Key.LastIndexOf('/');
                string dir = slash < 0 ? "/" : kv.Key.Substring(0, slash + 1);
                string name = kv.Key.Substring(slash + 1);
                string d = dir;
                while (d != "/" && !dirs.ContainsKey(d))
                {
                    dirs[d] = new SortedDictionary<string, int>(StringComparer.Ordinal);
                    int p = d.LastIndexOf('/', d.Length - 2);
                    d = p < 0 ? "/" : d.Substring(0, p + 1);
                }
                dirs[dir][name] = kv.Value;
            }
            var fd = new MemoryStream(); var fdw = new BinaryWriter(fd);
            fdw.Write(dirs.Count);
            foreach (var d in dirs)
            {
                PakReader.WriteFString(fdw, d.Key);
                fdw.Write(d.Value.Count);
                foreach (var f in d.Value) { PakReader.WriteFString(fdw, f.Key); fdw.Write(f.Value); }
            }
            byte[] fdBytes = fd.ToArray();

            byte[] encBytes = encoded.ToArray();
            // Tamanho do indice primario (para calcular offsets dos indices secundarios)
            var tmp = BuildPrimary(mountPoint, locations.Count, seed, 0, phBytes, sha, 0, fdBytes, encBytes);
            long indexOffset = fs.Position;
            long phOffset = indexOffset + tmp.Length;
            long fdOffset = phOffset + phBytes.Length;
            byte[] primary = BuildPrimary(mountPoint, locations.Count, seed, phOffset, phBytes, sha, fdOffset, fdBytes, encBytes);
            w.Write(primary); w.Write(phBytes); w.Write(fdBytes);

            // Rodape (FPakInfo v11)
            w.Write(new byte[16]); w.Write((byte)0);
            w.Write(0x5A6F12E1u); w.Write(11);
            w.Write(indexOffset); w.Write((long)primary.Length);
            w.Write(sha.ComputeHash(primary));
            w.Write(new byte[32 * 5]);
        }
    }

    static byte[] BuildPrimary(string mount, int count, ulong seed, long phOff, byte[] ph, SHA1 sha, long fdOff, byte[] fd, byte[] enc)
    {
        var ms = new MemoryStream(); var w = new BinaryWriter(ms);
        PakReader.WriteFString(w, mount);
        w.Write(count);
        w.Write(seed);
        w.Write(1); w.Write(phOff); w.Write((long)ph.Length); w.Write(sha.ComputeHash(ph));
        w.Write(1); w.Write(fdOff); w.Write((long)fd.Length); w.Write(sha.ComputeHash(fd));
        w.Write(enc.Length); w.Write(enc);
        w.Write(0);
        return ms.ToArray();
    }
}
