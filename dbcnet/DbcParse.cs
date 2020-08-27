using System;
using System.IO;
using System.Linq;

namespace dbcnet
{
    public class DbcParse
    {
        private const string msgstr = "BO_";                //Message definition.   BO_ <CAN-ID> <MessageName>: <MessageLength> <SendingNode>
        private const string sigstr = "SG_";                //Signal definition.    SG_ <SignalName> [M|m<MultiplexerIdentifier>] : <StartBit>|<Length>@<Endianness><Signed> (<Factor>,<Offset>) [<Min>|<Max>] "[Unit]" [ReceivingNodes]
        private const string desstr = "CM_";                //Description field.    CM_ [<BU_|BO_|SG_> [CAN-ID] [SignalName]] "<DescriptionText>";
        private const string busstr = "BS_";                //Bus configuration.    BS_: <Speed>
        private const string nodesstr = "BU_";              //List of all CAN-Nodes, seperated by whitespaces.
        private const string attdefstr = "BA_DEF_";         //Attribute definition.     BA_DEF_ [BU_|BO_|SG_] "<AttributeName>" <DataType> [Config];
        private const string attdftstr = "BA_DEF_DEF_";     //Attribute default value   BA_DEF_DEF_ "<AttributeName>" ["]<DefaultValue>["];
        private const string attstr = "BA_";                //Attribute                 BA_ "<AttributeName>" [BU_|BO_|SG_] [Node|CAN-ID] [SignalName] <AttributeValue>;
        private const string valstr = "VAL_";               //Value definitions for signals.        VAL_ <CAN-ID> <SignalsName> <ValTableName|ValTableDefinition>;
        private const string valtabstr = "VAL_TABLE_";      //Value table definition for signals.   VAL_TABLE_ <ValueTableName> <ValueTableDefinition>;


        private const int msgcount = 5;


        private string fileName;

        public DbcParse(string fileName)
        {
            this.fileName = fileName;
        }

        public Cluster Parse()
        {
            var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var sr = new StreamReader(fs);
            var lines = sr.ReadToEnd().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var cluster = new Cluster();

            //读取具体消息
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var keyWords = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if(keyWords.Length > 0)
                {
                    //识别消息Message
                    //BO_ <CAN-ID> <MessageName>: <MessageLength> <SendingNode>
                    if (keyWords[0] == msgstr && keyWords.Length == msgcount)
                    {
                        var msg = new Message
                        {
                            Identifier = uint.Parse(keyWords[1]),
                            Name = keyWords[2].Substring(0, keyWords[2].Length - 1),   //移除名称后面的冒号,是否肯定有冒号？
                            Length = uint.Parse(keyWords[3])
                        };
                        cluster.Messages.Add(msg);

                        //识别消息Message下的信号Signal
                        //SG_ Signal0 : 0|32@1- (1,0) [0|0] "" Node1 Node2
                        //SG_ <SignalName> [M|m<MultiplexerIdentifier>] : <StartBit>|<Length>@<Endianness><Signed> (<Factor>,<Offset>) [<Min>|<Max>] "[Unit]" [ReceivingNodes]
                        keyWords = lines[i + 1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);  //忽略空字符
                        if (keyWords.Length > 0)
                        {
                            while(keyWords[0] == sigstr)
                            {
                                var sig = new Signal();
                                sig.Name = keyWords[1];

                                var offset = 0;
                                //[M|m<MultiplexerIdentifier>]
                                if (keyWords[2] != ":")
                                    offset = 1;
                                //起始位
                                var sp = keyWords[3+offset].Split(new[] { '|', '@' }, StringSplitOptions.RemoveEmptyEntries);
                                if (sp.Length == 3)
                                {
                                    sig.StartBit = uint.Parse(sp[0]);
                                    sig.NumberOfBits = uint.Parse(sp[1]);
                                    sig.ByteOrder = (ByteOrder)uint.Parse(sp[2].Remove(sp[2].Length - 1));
                                    if (sp[2][sp[2].Length - 1] == '+')
                                        sig.DataType = DataType.Unsigned;
                                    else
                                        sig.DataType = DataType.Signed;
                                }
                                else
                                {
                                    throw new Exception($"解析失败,{line}");
                                }

                                sp = keyWords[4 + offset].Split(new[] { '(', ',', ')' }, StringSplitOptions.RemoveEmptyEntries);
                                if(sp.Length == 2)
                                {
                                    sig.ScalingFactor = uint.Parse(sp[0]);
                                    sig.ScalingOffset = uint.Parse(sp[1]);
                                }
                                else
                                {
                                    throw new Exception($"解析失败,{line}");
                                }

                                sp = keyWords[5 + offset].Split(new[] { '[', '|', ']' }, StringSplitOptions.RemoveEmptyEntries);
                                if (sp.Length == 2)
                                {
                                    sig.MinimumValue = double.Parse(sp[0]);
                                    sig.MaximumValue = double.Parse(sp[1]);
                                }
                                else
                                {
                                    throw new Exception($"解析失败,{line}");
                                }
                                var unit = keyWords[6 + offset];
                                sig.Unit = unit.Substring(1,unit.Length -2);

                                msg.Signals.Add(sig);
                                i++;
                                keyWords = lines[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);  //忽略空字符
                                if (keyWords[0] != sigstr) i--;
                            }
                        }
                    }

                    //识别描述信息
                    //CM_ [<BU_|BO_|SG_> [CAN-ID] [SignalName]] "<DescriptionText>";
                    else if (keyWords[0] == desstr)
                    {
                        if(keyWords[1] == sigstr)
                        {
                            var id = uint.Parse(keyWords[2]);
                            var name = keyWords[3];
                            var comment = "";
                            while (line[line.Length - 1] != ';')
                                line += lines[++i];
                            comment = line.Substring(line.IndexOf('\"') + 1, line.LastIndexOf('\"') - line.IndexOf('\"') - 1);
                            cluster.Messages.First(m => m.Identifier == id).Signals.First(s => s.Name == name).Comment = comment;
                        }
                        else if(keyWords[1] == msgstr)
                        {
                            var id = uint.Parse(keyWords[2]);
                            var comment = "";
                            while (line[line.Length - 1] != ';')
                                line += lines[++i];
                            comment = line.Substring(line.IndexOf('\"') + 1, line.LastIndexOf('\"') - line.IndexOf('\"') - 1);
                            cluster.Messages.First(m => m.Identifier == id).Comment = comment;
                        }
                    }
                }
            }

            sr.Close();
            fs.Close();
            return null;
        }
    }
}
