using System;
using System.Runtime.InteropServices;

public static class PocketSphinxNative
{
    private const string LIB = "pocketsphinx"; // Unity resolve pocketsphinx.dll automaticamente

    [DllImport(LIB)] public static extern IntPtr ps_config_init(IntPtr defn);
    [DllImport(LIB, CharSet = CharSet.Ansi)]
    public static extern int ps_config_set_str(IntPtr config, string name, string val);
    [DllImport(LIB)] public static extern IntPtr ps_init(IntPtr config);
    [DllImport(LIB)] public static extern int ps_start_utt(IntPtr ps);
    [DllImport(LIB)] public static extern int ps_process_raw(IntPtr ps, short[] data, UIntPtr n_samples, int no_search, int full_utt);
    [DllImport(LIB)] public static extern int ps_end_utt(IntPtr ps);
    [DllImport(LIB)] public static extern IntPtr ps_get_hyp(IntPtr ps, IntPtr out_best_score);
    [DllImport(LIB, CharSet = CharSet.Ansi)]
    public static extern int ps_add_word(IntPtr ps, string word, string phones, int update); // adiciona palavra em runtime (seção 6)
    [DllImport(LIB)] public static extern int ps_free(IntPtr ps);
    [DllImport(LIB)] public static extern int ps_config_free(IntPtr config);
}