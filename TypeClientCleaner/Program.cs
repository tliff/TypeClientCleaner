using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Data.Common;
using System.Data.SQLite;

namespace TypeClientCleaner
{
    class Program
    {
        static void Main(string[] args)
        {
            Microsoft.Win32.RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Type 1 Installer\Type 1 Fonts", RegistryKeyPermissionCheck.ReadWriteSubTree);
            string profiledir = System.Environment.GetEnvironmentVariable("USERPROFILE");
            string dbpath = profiledir + @"\Lokale Einstellungen\Anwendungsdaten\Extensis\UTC\cache\UniversalType.db";
            foreach (var keyname in key.GetValueNames())
            {
                var keyval = (string[])key.GetValue(keyname);
                Boolean matches = false;
                foreach (string str in keyval)
                {
                    if (new Regex("UTC").Split(str).Length > 1)
                        matches = true;
                }
                if (matches)
                {
                    Console.WriteLine(keyname);
                    //TODO: check permissions
                    key.DeleteValue(keyname);
                }
            }

            SQLiteConnection con = new SQLiteConnection("Data Source="+dbpath);
            con.Open();
            SQLiteCommand command = new SQLiteCommand(con);
            command.CommandText = "DELETE FROM FontActivationFile";
            command.ExecuteNonQuery();
            command.CommandText = "DELETE FROM FontActivation";
            command.ExecuteNonQuery();
            command.CommandText = "UPDATE FontSet SET permActiveFontCount = 0";
            command.ExecuteNonQuery();

            Console.WriteLine(profiledir);
        }

    }
}

