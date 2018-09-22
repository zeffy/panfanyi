﻿using Microsoft.Win32;
using OpenMcdf;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using System.Net;

namespace panfanyi
{
    class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WritePrivateProfileString(
            [MarshalAs(UnmanagedType.LPWStr)] string lpAppName,
            [MarshalAs(UnmanagedType.LPWStr)] string lpKeyName,
            [MarshalAs(UnmanagedType.LPWStr)] string lpString,
            [MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.U4)]
        internal static extern uint GetPrivateProfileString(
            [MarshalAs(UnmanagedType.LPWStr)] string lpAppName,
            [MarshalAs(UnmanagedType.LPWStr)] string lpKeyName,
            [MarshalAs(UnmanagedType.LPWStr)] string lpDefault,
            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder lpReturnedString,
            [MarshalAs(UnmanagedType.U4)] uint nSize,
            [MarshalAs(UnmanagedType.LPWStr)] string lpFileName);
    }

    class Program
    {
        static void Main(string[] args)
        {

            int index;
            string lang;
            string installDir;
            string file;
            string fanyi = Path.GetFullPath("fanyi.ini");

            Console.WriteLine(
@",-------------------------------------------------.
| panfanyi 1.0                                    |
| Copyright 2018 zeffy <https://github.com/zeffy> |
| Simple Understanding Public License v1 (SUPL)   |
 `+---------------------------------------------+-'
  | OpenMcdf 2.1                                |
  | Copyright (c) 2010-2018, Federico Blaseotto |
  | Mozilla Public License 2.0 (MPL-2.0)        |
  `---------------------------------------------'
");

            if ( (index = Array.IndexOf(args, "-update")) != -1 ) {
                using ( var wc = new WebClient()) {
                    string part = fanyi + ".part";

                    Console.WriteLine("Downloading latest fanyi.ini...");
                    wc.DownloadFile(
                        "https://raw.githubusercontent.com/zeffy/panfanyi/master/panfanyi/fanyi.ini",
                        part);
                    File.Copy(fanyi, fanyi + DateTime.Now.ToString("_yyyy-MM-dd_HH-mm-ss.bak"));
                    File.Move(part, fanyi);
                    Console.Write("\nDone! Press any key to continue... ");
                    Console.ReadKey(true);
                    return;
                }
            }

            if ( (index = Array.IndexOf(args, "-lang")) != -1 ) {
                if ( ++index >= args.Length )
                    return;

                lang = args[index];
            } else {
                lang = "en";
            }

            if ( (index = Array.IndexOf(args, "-file")) != -1 ) {
                if ( ++index >= args.Length )
                    return;

                file = args[index];
            } else {
                installDir = Registry.GetValue(
                    @"HKEY_CURRENT_USER\Software\Baidu\BaiduYunGuanjia",
                    "installDir",
                    null) as string;
                if ( string.IsNullOrEmpty(installDir) )
                    return;

                file = installDir + "\\resource.db";
            }

            using ( var fs = new FileStream(file,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.Read) )
            using ( var cf = new CompoundFile(fs,
                CFSUpdateMode.Update,
                CFSConfiguration.SectorRecycle | CFSConfiguration.EraseFreeSectors) ) {

                CFStream cfs = cf.RootStorage.GetStream("StringTable.xml");
                XDocument xd = XDocument.Parse(Encoding.UTF8.GetString(cfs.GetData()));
                string xlang;
                string bakfile;
                StringBuilder sb;
                string id;

                if ( Array.IndexOf(args, "-x") != -1 ) {
                    if ( ++index < args.Length && args[index][0] != '-' )
                        xlang = args[index];
                    else
                        xlang = "cn";

                    Console.WriteLine("Writing current string table to fanyi.ini...");
                    foreach ( var element in xd.Root.Elements("String") ) {
                        if ( !NativeMethods.WritePrivateProfileString(xlang,
                            (string)element.Attribute("id"),
                            (string)element.Attribute("value"),
                            fanyi) ) {

                            Console.WriteLine("Failed to write string!");
                        }
                    }
                    Console.Write("\nDone! Press any key to continue... ");
                    Console.ReadKey(true);
                    return;
                }

                bakfile = file + DateTime.Now.ToString("_yyyy-MM-dd_HH-mm-ss.bak");
                File.Copy(file, bakfile);
                Console.WriteLine("Copied old resource.db to \"{0}\"", bakfile);

                Console.WriteLine("Translating string table to {0}...", lang);
                sb = new StringBuilder(0x200);
                foreach ( var element in xd.Root.Elements("String") ) {
                    id = (string)element.Attribute("id");
                    if ( NativeMethods.GetPrivateProfileString(lang, id, "", sb, (uint)sb.MaxCapacity, fanyi) > 0 )
                        element.Attribute("value").SetValue(sb.ToString());
                }
                cfs.SetData(Encoding.UTF8.GetBytes(xd.ToString()));
                cf.Commit(true);
                Console.Write("\nDone! Press any key to continue... ");
                Console.ReadKey(true);
            }
        }
    }
}
