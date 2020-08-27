using System;
using System.Collections.Generic;
using System.IO;

namespace dbcnet
{
    public class DbcParse
    {
        private string fileName;

        public DbcParse(string fileName)
        {
            this.fileName = fileName;
        }

        public Cluster Parse()
        {
            var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var sr = new StreamReader(fs);
            var lines = sr.ReadToEnd().Split('\n');

            var cluster = new Cluster();

            //读取具体消息
            var i = 0;
            while (i < lines.Length)
            {
                var line = lines[i];
                var list = line.Split(' ');
                if (list.Length == 5 && list[0] == "BO_")
                {
                    var msg = new Message();
                    msg.Identifier = uint.Parse(list[1]);
                    msg.Name = list[2].Substring(0, list[2].Length - 1);   //移除名称后面的冒号,是否肯定有冒号？
                    msg.Length = uint.Parse(list[3]);

                }
                i++;
            }

            sr.Close();
            fs.Close();
            return null;
        }
    }
}
