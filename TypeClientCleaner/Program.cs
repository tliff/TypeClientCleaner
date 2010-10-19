using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Net;

namespace TypeClientCleaner
{
    class Program
    {
        static void Main(string[] args)
        {
            RegistryKey[] keys = {
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Type 1 Installer\Type 1 Fonts", RegistryKeyPermissionCheck.ReadWriteSubTree),
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts", RegistryKeyPermissionCheck.ReadWriteSubTree)
            };
            
            string profiledir = System.Environment.GetEnvironmentVariable("USERPROFILE");
            string dbpath = profiledir + @"\Lokale Einstellungen\Anwendungsdaten\Extensis\UTC\cache\UniversalType.db";
            foreach (var key in keys)
            {
                foreach (var keyname in key.GetValueNames())
                {
                    Boolean matches = false;
                    try
                    {
                        var keyval = (string[])key.GetValue(keyname);

                        foreach (string str in keyval)
                        {
                            if (new Regex("UTC").Split(str).Length > 1)
                                matches = true;
                        }

                    }
                    catch {
                        try
                        {
                            var keyval = (string)key.GetValue(keyname);
                            if (new Regex("UTC").Split(keyval).Length > 1)
                                matches = true;
                        }
                        catch { 
                        
                        }
                    }
                    if (matches)
                    {
                        key.DeleteValue(keyname);
                    }
                }
            }
            if (File.Exists(dbpath))
            {

                SQLiteConnection con = new SQLiteConnection("Data Source=" + dbpath);
                con.Open();
                SQLiteCommand command = new SQLiteCommand(con);
                command.CommandText = "SELECT User.userId, Server.address, Server.port, ServerSession.session_uuid FROM ServerSession LEFT JOIN User on ServerSession.user_id = User.id LEFT JOIN Server ON ServerSession.server_id = Server.id";
                SQLiteDataReader data = command.ExecuteReader();
                data.Read();
                var username = data.GetValue(0);
                var address = data.GetValue(1);
                var port = data.GetValue(2);
                var sessionid = data.GetValue(3);
                data.Close();
                command.CommandText = "DELETE FROM FontActivationFile;";
                command.ExecuteNonQuery();
                command.CommandText = "DELETE FROM FontActivation;";
                command.ExecuteNonQuery();
                command.CommandText = "UPDATE FontSet SET permActiveFontCount = 0;";
                command.ExecuteNonQuery();
                command.CommandText = "UPDATE AssetFile SET isReady=0, isObfuscated=1;";
                command.ExecuteNonQuery();
                command.CommandText = "UPDATE FontFace SET fontIsLocal=0;";
                command.ExecuteNonQuery();
                command.CommandText = "DELETE FROM ServerSession;";
                command.ExecuteNonQuery();

                try
                {
                    String body = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:SOAP-ENC=\"http://schemas.xmlsoap.org/soap/encoding/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:esp3=\"http://jaxb.dev.java.net/array\" xmlns:esp=\"http://extensis.com/service/font\"><SOAP-ENV:Body><esp:logout><arg0 xsi:type=\"esp:credentials\"><attributes xsi:type=\"esp:attribute\" name=\"credentials.esp.group.name\"><value>esp.default.all-users</value></attributes><attributes xsi:type=\"esp:attribute\" name=\"credentials.esp.loginID\"><value>" + sessionid + "</value></attributes><attributes xsi:type=\"esp:attribute\" name=\"credentials.esp.user.name\"><value>" + username + "</value></attributes></arg0></esp:logout></SOAP-ENV:Body></SOAP-ENV:Envelope>";
                    HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create("http://" + address + ":" + port + "/ws/FontService");
                    request.Method = "POST";
                    request.UserAgent = "gSOAP/2.7";
                    request.ContentType = "text/xml";
                    request.Headers.Add("SOAPAction", "");
                    request.ContentLength = body.Length;
                    Stream datastream = request.GetRequestStream();

                    var encoding = System.Text.Encoding.ASCII;
                    datastream.Write(encoding.GetBytes(body), 0, body.Length);
                    datastream.Close();
                    WebResponse response = request.GetResponse();
                }
                catch {
                
                }
            }




       }


    }
}

