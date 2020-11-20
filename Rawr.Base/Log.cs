using System;
using System.IO;

//i... hurt myself today
namespace Rawr
{
    //to see if i still feel
    public class Log
    {
        public static void Write(string log)
        {
            int ms = DateTime.Now.Millisecond;
            using (StreamWriter sw = new StreamWriter(File.Open("Rawrlog.txt", FileMode.Append, FileAccess.Write, FileShare.Write)))
            {
                //but i remember... everything
                sw.WriteLine("{0} {1}:{2}:{3}.{4}: {5}", DateTime.Now.ToShortDateString(), DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second,
                    (ms < 100 ? "0" : "") + (ms < 10 ? "0" : "") + ms.ToString(), log);
            }
        }
    }
}
