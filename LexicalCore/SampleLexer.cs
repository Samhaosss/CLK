using System;
using System.Collections.Generic;
using System.IO;

namespace CLK.LexicalCore
{
    public enum TakenType { Keyword, Id, Op, delimiterChars, Num };
    public class Taken
    {
        private TakenType type;
        private string value;

        public Taken(TakenType type, string value)
        {
            this.type = type;
            this.value = value;
        }
        public TakenType Type => type;
        public string Value => value;
        public override bool Equals(object obj)
        {
            var taken = obj as Taken;
            return taken != null &&
                   type == taken.type &&
                   value == taken.value;
        }

        public override int GetHashCode()
        {
            var hashCode = 1148455455;
            hashCode = hashCode * -1521134295 + type.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(value);
            return hashCode;
        }
    }
    class SampleLexer
    {
        private TakenReader takenReader;
        private uint rowNo;
        private uint colNo;
        private List<string> keywordsList;
        private const string keywordFileName = "keywords";
        private bool isFinish;
        private char? lastCh;
        private static List<char> delimiterChars = new List<char> { ' ', ',', ';', '\r', '\t' };
        private static List<char> unaryOperator = new List<char> { '.', '+', '-', '*', '/', '=', '&', '|', '!', '%' };

        public uint ColNo { get => colNo; }
        public uint RowNo { get => rowNo; }

        public SampleLexer(string targetFile)
        {
            // ADD keyword 
            var path = Environment.CurrentDirectory + keywordFileName;
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"File:{path} does not exist");
            }
            keywordsList = new List<string>();
            var keywords = System.IO.File.ReadAllText(path);
            foreach (var key in keywords.Split(','))
            {
                keywordsList.Add(key.Trim());
            }
            takenReader = new TakenReader(targetFile);
            colNo = rowNo = 0;
            isFinish = (takenReader.Length > 0) ? false : true;
            lastCh = (!isFinish) ? takenReader.next() : null;
        }
        private bool IsInRange(char tar, char start, char end)
        {
            return (tar >= start) && (tar <= end);
        }

        public Taken analyze()
        {
            while (true)
            {
                if (IsInRange(lastCh.Value, 'a', 'z') || IsInRange(lastCh.Value, 'A', 'Z') || lastCh.Equals('_'))
                {
                    // TODO: parse keywords or id 
                    throw new NotImplementedException();
                }
                else if (IsInRange(lastCh.Value, '0', '9'))
                {
                    // TODO:parse num
                    throw new NotImplementedException();
                }
                else if (unaryOperator.Contains(lastCh.Value))
                {
                    // TODO: parse op
                    throw new NotImplementedException();
                }
                else if (delimiterChars.Contains(lastCh.Value))
                {
                    // TODO: parse delimiter chars
                    throw new NotImplementedException();
                }
                else
                {

                }
            }
            throw new NotImplementedException();
        }


    }
}
