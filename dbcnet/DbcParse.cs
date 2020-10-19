using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace dbcnet
{
    public class DbcParse
    {
        private const string STRMSG = "BO_";                //Message definition.   BO_ <CAN-ID> <MessageName>: <MessageLength> <SendingNode>
        private const string STRSIG = "SG_";                //Signal definition.    SG_ <SignalName> [M|m<MultiplexerIdentifier>] : <StartBit>|<Length>@<Endianness><Signed> (<Factor>,<Offset>) [<Min>|<Max>] "[Unit]" [ReceivingNodes]
        private const string STRDES = "CM_";                //Description field.    CM_ [<BU_|BO_|SG_> [CAN-ID] [SignalName]] "<DescriptionText>";
        private const string STRBUS = "BS_";                //Bus configuration.    BS_: <Speed>
        private const string STRNODES = "BU_:";              //List of all CAN-Nodes, seperated by whitespaces.  BU_ X1 X2
        private const string STRNODE = "BU_";               //
        private const string STRATTDEF = "BA_DEF_";         //Attribute definition.     BA_DEF_ [BU_|BO_|SG_] "<AttributeName>" <DataType> [Config];
        private const string STRATTDFT = "BA_DEF_DEF_";     //Attribute default value   BA_DEF_DEF_ "<AttributeName>" ["]<DefaultValue>["];
        private const string STRATT = "BA_";                //Attribute                 BA_ "<AttributeName>" [BU_|BO_|SG_] [Node|CAN-ID] [SignalName] <AttributeValue>;
        private const string STRVAL = "VAL_";               //Value definitions for signals.        VAL_ <CAN-ID> <SignalsName> <ValTableName|ValTableDefinition>;
        private const string STRVALTAB = "VAL_TABLE_";      //Value table definition for signals.   VAL_TABLE_ <ValueTableName> <ValueTableDefinition>;

        public static Cluster Parse(string fileName)
        {
            var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var sr = new StreamReader(fs);
            var lines = sr.ReadToEnd().Split(new[] {'\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            sr.Close();
            fs.Close();

            var cluster = new Cluster();
            var strtable = new Dictionary<string,string>();
            var frameFormat = new List<string>();

            //便利字符串数组,读取需要的信息
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var lineWords = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (lineWords.Length > 0)
                {
                    //识别消息Message和signal
                    //BO_ <CAN-ID> <MessageName>: <MessageLength> <SendingNode>
                    switch (lineWords[0])
                    {
                        case STRMSG:    //Message definition.
                            var msg = new Message
                            {
                                Identifier = uint.Parse(lineWords[1]),
                                Name = lineWords[2].Substring(0, lineWords[2].Length - 1),   //移除名称后面的冒号,是否肯定有冒号？
                                Length = uint.Parse(lineWords[3])
                            };
                            cluster.Messages.Add(msg);
                            //识别发送节点
                            var nodeName = lineWords[4];
                            var node = cluster.Nodes.FirstOrDefault(n => n.Name == nodeName);
                            if (node == null)
                            {
                                node = new Node()
                                {
                                    Name = nodeName
                                };
                                cluster.Nodes.Add(node);
                            }
                            node.MessagesTransmitted.Add(msg);

                            //识别消息Message下的信号Signal
                            //SG_ Signal0 : 0|32@1- (1,0) [0|0] "" Node1 Node2
                            //SG_ <SignalName> [M|m<MultiplexerIdentifier>] : <StartBit>|<Length>@<Endianness><Signed> (<Factor>,<Offset>) [<Min>|<Max>] "[Unit]" [ReceivingNodes]
                            lineWords = lines[i + 1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);  //忽略空字符
                            while (lineWords.Length > 0 && lineWords[0] == STRSIG)  //Signal definition.
                            {
                                var sig = new Signal
                                {
                                    Name = lineWords[1]
                                };

                                var offset = 0;
                                //[M|m<MultiplexerIdentifier>]
                                if (lineWords[2] != ":")
                                    offset = 1;
                                //起始位
                                var sp = lineWords[3 + offset].Split(new[] { '|', '@' }, StringSplitOptions.RemoveEmptyEntries);
                                if (sp.Length == 3)
                                {
                                    sig.StartBit = int.Parse(sp[0]);
                                    sig.NumberOfBits = int.Parse(sp[1]);
                                    sig.ByteOrder = (ByteOrder)uint.Parse(sp[2].Remove(sp[2].Length - 1));
                                    if(sig.ByteOrder == ByteOrder.BigEndian)
                                    {
                                        sig.StartBit = CalTrueStartBit(sig.StartBit,sig.NumberOfBits);
                                    }
                                    if (sp[2][sp[2].Length - 1] == '+')
                                        sig.DataType = DataType.Unsigned;
                                    else
                                        sig.DataType = DataType.Signed;
                                }
                                else
                                {
                                    throw new Exception($"解析失败,{line}");
                                }

                                sp = lineWords[4 + offset].Split(new[] { '(', ',', ')' }, StringSplitOptions.RemoveEmptyEntries);
                                if (sp.Length == 2)
                                {
                                    sig.ScalingFactor = double.Parse(sp[0]);
                                    sig.ScalingOffset = double.Parse(sp[1]);
                                }
                                else
                                {
                                    throw new Exception($"解析失败,{line}");
                                }

                                sp = lineWords[5 + offset].Split(new[] { '[', '|', ']' }, StringSplitOptions.RemoveEmptyEntries);
                                if (sp.Length == 2)
                                {
                                    sig.MinimumValue = double.Parse(sp[0]);
                                    sig.MaximumValue = double.Parse(sp[1]);
                                }
                                else
                                {
                                    throw new Exception($"解析失败,{line}");
                                }
                                var unit = lineWords[6 + offset];
                                sig.Unit = unit.Substring(1, unit.Length - 2);

                                var rcvNode = cluster.Nodes.FirstOrDefault(n => n.Name == lineWords[7 + offset].Trim());
                                if (rcvNode != null && !rcvNode.MessagesReceived.Contains(msg))
                                    rcvNode.MessagesReceived.Add(msg);


                                msg.Signals.Add(sig);
                                i++;
                                lineWords = lines[i + 1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);  //忽略空字符
                                if (lineWords.Length <= 0 && lineWords[0] != STRSIG) i--;
                            }
                            break;

                        case STRDES:    //Description field.
                            if (lineWords[1] == STRSIG)
                            {
                                var id = uint.Parse(lineWords[2]);
                                var name = lineWords[3];
                                var comment = "";
                                line = line.TrimEnd();
                                while (line[line.Length - 1] != ';')
                                    line += lines[++i].TrimEnd();
                                comment = line.Substring(line.IndexOf('\"') + 1, line.LastIndexOf('\"') - line.IndexOf('\"') - 1);
                                cluster.Messages.First(m => m.Identifier == id).Signals.First(s => s.Name == name).Comment = comment;
                            }
                            else if (lineWords[1] == STRMSG)
                            {
                                var id = uint.Parse(lineWords[2]);
                                var comment = "";
                                line = line.TrimEnd();
                                while (line[line.Length - 1] != ';')
                                    line += lines[++i].TrimEnd();
                                comment = line.Substring(line.IndexOf('\"') + 1, line.LastIndexOf('\"') - line.IndexOf('\"') - 1);
                                cluster.Messages.First(m => m.Identifier == id).Comment = comment;
                            }
                            else if (lineWords[1] == STRNODE)
                            {
                                nodeName = lineWords[2];
                                var comment = "";
                                line = line.TrimEnd();
                                while (line.Trim()[line.Length - 1] != ';')
                                    line += lines[++i].TrimEnd();
                                comment = line.Substring(line.IndexOf('\"') + 1, line.LastIndexOf('\"') - line.IndexOf('\"') - 1);
                                cluster.Nodes.First(n => n.Name == nodeName).Comment = comment;
                            }
                            break;

                        case STRATT:
                            switch (lineWords[1])
                            {
                                case "\"GenMsgCycleTime\"":
                                    if (lineWords[2] == STRMSG)
                                    {
                                        var id = uint.Parse(lineWords[3]);
                                        var ped = double.Parse(lineWords[4].Substring(0, lineWords[4].IndexOf(';')));
                                        cluster.Messages.First(m => m.Identifier == id).CANTxTime = ped / 1000;
                                    }
                                    break;
                                case "\"GenMsgPeriod\"":
                                    if(lineWords[2] == STRMSG)
                                    {
                                        var id = uint.Parse(lineWords[3]);
                                        var ped = double.Parse(lineWords[4].Substring(0,lineWords[4].IndexOf(';')));
                                        cluster.Messages.First(m => m.Identifier == id).CANTxTime = ped / 1000;
                                    }
                                    break;
                                case "\"VFrameFormat\"":
                                    if(lineWords[2] == STRMSG)
                                    {
                                        //BA_ "VFrameFormat" BO_ 0 14;
                                        var id = uint.Parse(lineWords[3]);
                                        var msg1 = cluster.Messages.FirstOrDefault(m => m.Identifier == id);
                                        if(msg1 != null)
                                        {
                                            switch (frameFormat[int.Parse(Substring(lineWords[4]))])
                                            {
                                                case "\"StandardCAN\"" :
                                                    msg1.IoMode = IoMode.CAN;
                                                    msg1.CANExtID = false;
                                                    break;
                                                case "\"ExtendedCAN\"" :
                                                    msg1.IoMode = IoMode.CAN;
                                                    msg1.CANExtID = true;
                                                    break;
                                                case "\"StandardCAN_FD\"" :
                                                    msg1.IoMode = IoMode.CANFD;
                                                    msg1.CANExtID = false;
                                                    break;
                                                case "\"ExtendedCAN_FD\"" :
                                                    msg1.IoMode = IoMode.CANFD;
                                                    msg1.CANExtID = true;
                                                    break;
                                            }
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case STRATTDEF:
                            switch(lineWords[1])
                            {
                                case STRMSG :
                                    if(lineWords[2] == "\"VFrameFormat\"")
                                    {
                                        //BA_DEF_ BO_  "VFrameFormat" ENUM  "StandardCAN","ExtendedCAN","reserved","reserved","reserved","reserved","reserved","reserved","reserved","reserved","reserved","reserved","reserved","reserved","StandardCAN_FD","ExtendedCAN_FD";
                                        frameFormat.AddRange(lineWords[4].Split(','));
                                        var index = frameFormat[frameFormat.Count - 1].IndexOf(';');
                                        if(index >= 0)
                                        {
                                            frameFormat[frameFormat.Count - 1] = frameFormat[frameFormat.Count - 1].Substring(0,index);
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case STRATTDFT:
                            switch (lineWords[1])
                            {
                                case "\"BusType\"":
                                    if (lineWords[2] == "\"CAN\"")
                                        cluster.IoMode = IoMode.CAN;
                                    else if (lineWords[2] == "\"CAN FD\"")
                                        cluster.IoMode = IoMode.CANFD;
                                    break;
                            }
                            break;
                        case STRVAL:        //VAL_ 4321 Value0 2 "Value2" 1 "Value1" 0 "Value0";VAL_ 4321 Value1 Numbers;
                            if(lineWords.Length > 2)
                            {
                                var id = uint.Parse(lineWords[1]);
                                var sigName = lineWords[2];
                                var msg1 = cluster.Messages.FirstOrDefault(m => m.Identifier == id);
                                if(msg1 != null)
                                {
                                    var sig = msg1.Signals.FirstOrDefault(s => s.Name == sigName);
                                    if( sig != null)
                                    {
                                        if(lineWords[3].EndsWith(";") && strtable.ContainsKey(lineWords[3].Remove(lineWords[3].Length -1)))
                                        {
                                            sig.ValueDefinition = strtable[lineWords[3].Remove(lineWords[3].Length -1)];
                                        }
                                        else
                                        {
                                            var valStartIndex = line.IndexOf(sigName) + sigName.Length + 1;
                                            sig.ValueDefinition = line.Substring(valStartIndex ,line.Length - valStartIndex);
                                        }
                                    }
                                }
                            }
                            break;
                        case STRVALTAB:     //VAL_TABLE_ Numbers 3 "Three" 2 "Two" 1 "One" 0 "Zero";
                            if(lineWords.Length > 1)
                            {
                                var tableName = lineWords[1];
                                var valStartIndex = line.IndexOf(tableName) + tableName.Length + 1;
                                var tableVal = line.Substring(valStartIndex ,line.Length - valStartIndex);
                                strtable.Add(tableName,tableVal);
                            }
                            break;
                        case STRBUS:
                            if(lineWords.Length > 1 && lineWords[1] == ":")
                            {
                                cluster.BaudRate = ulong.Parse(lineWords[2]) * 1000;
                            }
                            break;
                        case STRNODES:
                            if(lineWords.Length >1)
                            {
                                for (int j = 1; j < lineWords.Length - 1 ; j++)
                                {
                                    var nodeName1 = lineWords[j];
                                    var node1 = cluster.Nodes.FirstOrDefault(n => n.Name == nodeName1);
                                    if (node1 == null)
                                    {
                                        node1 = new Node()
                                        {
                                            Name = nodeName1
                                        };
                                        cluster.Nodes.Add(node1);
                                    }
                                }
                            }
                            break;
                    }
                }
                else
                {
                    //忽略为空的值
                }
            }
            //转换扩展帧,最后转换是因为可能上面会根据ID查找消息,先转换可能会找不到。
            foreach (var msg in cluster.Messages)
            {
                if (msg.Identifier > 0x80000000)
                {
                    msg.Identifier -= 0x80000000;
                    msg.CANExtID = true;
                }
            }
            cluster.Messages = cluster.Messages.OrderBy(m => m.Identifier).ToList();
            return cluster;
        }

        private static int CalTrueStartBit(int startBit, int numberOfBits)
        {
            while (startBit % 8 - numberOfBits < -1)
            {
                startBit += 8;
                numberOfBits -= 8;
            }
            startBit = startBit - numberOfBits + 1;
            return startBit;
        }

        private static string Substring(string str,char endchar = ';')
        {
            var index = str.IndexOf(endchar);
            return index >= 0 ? str.Substring(0,index) : str;
        }
    }
}
