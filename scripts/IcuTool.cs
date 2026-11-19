// Extrai itens (ex.: icudt64l/pt.res) do pacote de dados comum do ICU embutido em icudt64.dll.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public static class IcuTool
{
    public static Dictionary<string, byte[]> ReadCommonData(string dllPath)
    {
        byte[] b = File.ReadAllBytes(dllPath);
        for (int i = 0; i + 16 < b.Length; i++)
        {
            if (b[i + 2] != 0xDA || b[i + 3] != 0x27) continue;
            if (b[i + 12] != (byte)'C' || b[i + 13] != (byte)'m' || b[i + 14] != (byte)'n' || b[i + 15] != (byte)'D') continue;
            int headerSize = BitConverter.ToUInt16(b, i);
            int toc = i + headerSize;
            int count = BitConverter.ToInt32(b, toc);
            var result = new Dictionary<string, byte[]>();
            var names = new List<string>(); var offs = new List<int>();
            for (int k = 0; k < count; k++)
            {
                int nameOff = BitConverter.ToInt32(b, toc + 4 + k * 8);
                int dataOff = BitConverter.ToInt32(b, toc + 8 + k * 8);
                int end = toc + nameOff; while (b[end] != 0) end++;
                names.Add(Encoding.ASCII.GetString(b, toc + nameOff, end - toc - nameOff));
                offs.Add(dataOff);
            }
            // O tamanho de cada item vai ate o inicio do proximo (itens sao contiguos e ordenados)
            for (int k = 0; k < count; k++)
            {
                int start = toc + offs[k];
                int stop = k + 1 < count ? toc + offs[k + 1] : b.Length;
                var item = new byte[stop - start];
                Buffer.BlockCopy(b, start, item, 0, item.Length);
                result[names[k]] = item;
            }
            return result;
        }
        throw new Exception("Pacote CmnD do ICU nao encontrado");
    }
}
