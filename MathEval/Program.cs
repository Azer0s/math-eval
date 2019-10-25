using System;
using System.Collections.Generic;
using System.Linq;

namespace MathEval
{
    class Program
    {
        enum Type
        {
            OPERATOR,
            NUMBER,
            IDENTIFIER
        }
        
        struct Token
        {
            public Type Type;
            public string Value;

            public override string ToString()
            {
                return $"[{Type.ToString().ToUpper()}] {Value}";
            }
        }

        private static IEnumerable<Token> Lex(string input)
        {
            var buffer = new Token();

            Token ReturnBuffer()
            {
                if (buffer.Type == Type.NUMBER && buffer.Value.EndsWith("."))
                {
                    throw new Exception();
                }
                
                return buffer;
            }
            
            foreach (var c in input)
            {
                switch (c)
                {
                    case char x when char.IsControl(x):
                    case char y when char.IsWhiteSpace(y):
                        break;
                    case '+':
                    case '-':
                    case '*':
                    case '/':
                    case '(':
                    case ')':
                        if (!string.IsNullOrEmpty(buffer.Value))
                        {
                            yield return ReturnBuffer(); buffer = new Token();
                        }
                        
                        yield return new Token{Type = Type.OPERATOR, Value = c.ToString()};
                        break;
                    case '.':
                        if (buffer.Type != Type.NUMBER)
                        {
                            throw new Exception();
                        }

                        if (buffer.Value != null && buffer.Value.Contains("."))
                        {
                            throw new Exception();
                        }

                        buffer.Value += c;
                        
                        break;
                    case char x when char.IsDigit(x):
                        if (buffer.Type != Type.NUMBER && !string.IsNullOrEmpty(buffer.Value))
                        {
                            yield return ReturnBuffer(); buffer = new Token();
                        }

                        buffer.Type = Type.NUMBER;
                        buffer.Value += c;
                        
                        break;
                    case char x when char.IsLetter(x):
                        if (buffer.Type != Type.IDENTIFIER && !string.IsNullOrEmpty(buffer.Value))
                        {
                            yield return ReturnBuffer(); buffer = new Token();
                        }
                        
                        buffer.Type = Type.IDENTIFIER;
                        buffer.Value += c;
                        
                        break;
                }
            }
            
            if (!string.IsNullOrEmpty(buffer.Value))
            {
                yield return ReturnBuffer();
            }
        }

        private static float Calculate(IEnumerable<Token> tokens, Dictionary<string, float> variables = null)
        {
            if (variables == null)
            {
                variables = new Dictionary<string, float>();
            }
            
            var tks = tokens.Select(a =>
            {
                if (a.Type == Type.IDENTIFIER)
                {
                    a.Type = Type.NUMBER;
                    a.Value = variables.ContainsKey(a.Value) ? variables[a.Value].ToString() : throw new Exception();
                }

                return a;
            }).ToList();
            
            if(tks.Count == 2)
            {
                if(tks[0].Value == "-" && tks[1].Type == Type.NUMBER)
                {
                    return float.Parse($"-{tks[1].Value}");
                }
            }
            
            IEnumerable<TSource> IndexRange<TSource>(IList<TSource> source, int fromIndex, int toIndex)
            {
                var currIndex = fromIndex;
                while (currIndex <= toIndex)
                {
                    yield return source[currIndex];
                    currIndex++;
                }
            }
            
            (int, int) NextIndex(int start, string close, string open)
            {
                var buffer = 1;
                var count = 0;
                var i = start + 1;
                for (;i < tks.Count && buffer != 0; i++)
                {
                    var token = tks[i];
                    if (token.Value == open)
                    {
                        buffer++;
                    }

                    if (token.Value == close)
                    {
                        buffer--;
                    }

                    count++;
                }

                return (count, i);
            }
            
            for (var i = 0; i < tks.Count; i++)
            {
                var token = tks[i];
                if (token.Value == "(")
                {
                    var (count, index) = NextIndex(i, ")", "(");

                    var before = tks.GetRange(0, i);
                    var result = new Token
                    {
                        Type = Type.NUMBER,
                        Value = Calculate(tks.GetRange(i + 1, count - 1)).ToString()
                    };
                    var after = tks.GetRange(index, tks.Count - index);
                    
                    var newTks = new List<Token>();
                    newTks.AddRange(before);
                    newTks.Add(result);
                    newTks.AddRange(after);
                    
                    tks = newTks;
                }
            }

            void DoCalculation(string op1, string op2, Func<float, float, float> op1Func, Func<float, float, float> op2Func)
            {
                while (tks.Any(a => a.Value == op1 || a.Value == op2))
                {
                    var idx = tks.FindIndex(a => a.Value == op1 || a.Value == op2);
                    var a = tks[idx - 1];
                    var b = tks[idx + 1];
                    var op = tks[idx];

                    var result = new Token
                    {
                        Type = Type.NUMBER, 
                        Value = (op.Value == op1
                            ? op1Func(float.Parse(a.Value), float.Parse(b.Value))
                            : op2Func(float.Parse(a.Value), float.Parse(b.Value))).ToString()
                    };

                    var before = idx - 1 == 0 ? new List<Token>() : IndexRange(tks, 0, idx - 2).ToList();
                    var after = tks.GetRange(idx + 2, tks.Count - idx - 2);
                    
                    var newTks = new List<Token>();
                    newTks.AddRange(before);
                    newTks.Add(result);
                    newTks.AddRange(after);

                    tks = newTks;
                }
            }
            
            DoCalculation("*", "/", (i, i1) => i * i1, (i, i1) => i / i1);
            DoCalculation("+", "-", (i, i1) => i + i1, (i, i1) => i - i1);

            return float.Parse(tks[0].Value);
        }
        
        static void Main()
        {
            Console.WriteLine(Calculate(Lex("423 * 2 + 30 - 4 + 3 * 10"))); //902
            Console.WriteLine(Calculate(Lex("423 * (2 + (30 - 4) + 3) - 10"))); //13103
            Console.WriteLine(Calculate(Lex("(11 - 15 / 5) * 4 - 12"))); //20
            Console.WriteLine(Calculate(Lex("109 - (7 * 3 - 15) / 3"))); //107
            Console.WriteLine(Calculate(Lex("89 * (52 / 13 + 6) + 18"))); //908
            Console.WriteLine(Calculate(Lex("(14.6 + 8.8) * 0.5 - (26.7 - 12.9) / 0.3"))); //-34.3
            Console.WriteLine(Calculate(Lex("0.2 * (34.2 - 2.5 / 0.1) + 0.04 * 0.1"))); //1.844
            Console.WriteLine(Calculate(Lex("89 * (a / 13 + 6) + b"), new Dictionary<string, float>{{"a", 52}, {"b", 18}})); //908
            Console.WriteLine(Calculate(Lex("(14.6 + a) * 0.5 - (b - 12.9) / 0.3"), new Dictionary<string, float>{{"a", 8.8f}, {"b", 26.7f}})); //-34.3
            Console.WriteLine(Calculate(Lex("0.2 * (a - 2.5 / b) + c * 0.1"), new Dictionary<string, float>{{"a", 34.2f}, {"b", 0.1f}, {"c", 0.04f}})); //1.844
        }
    }
}
