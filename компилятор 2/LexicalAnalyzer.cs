using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace компилятор_2
{
    internal class LexicalAnalyzer
    {
public enum TokenType
    {
        Number,
        Plus,
        Minus,
        Multiply,
        Divide,
        LParen,
        RParen,
        End,
        Unknown
    }

    public class Token
    {
        public TokenType Type { get; set; }
        public string Value { get; set; }
        public int Position { get; set; }
    }

    public class Lexer
    {
        private string text;
        private int pos;
        private int length;

        public Lexer(string input)
        {
            text = input;
            pos = 0;
            length = text.Length;
        }

        public Token GetNextToken()
        {
            while (pos < length && char.IsWhiteSpace(text[pos]))
                pos++;

            if (pos >= length)
                return new Token { Type = TokenType.End, Value = "", Position = pos };

            char current = text[pos];

            if (char.IsDigit(current))
            {
                int start = pos;
                StringBuilder sb = new StringBuilder();
                while (pos < length && char.IsDigit(text[pos]))
                {
                    sb.Append(text[pos]);
                    pos++;
                }
                return new Token { Type = TokenType.Number, Value = sb.ToString(), Position = start };
            }

            switch (current)
            {
                case '+':
                    pos++;
                    return new Token { Type = TokenType.Plus, Value = "+", Position = pos - 1 };
                case '-':
                    pos++;
                    return new Token { Type = TokenType.Minus, Value = "-", Position = pos - 1 };
                case '*':
                    pos++;
                    return new Token { Type = TokenType.Multiply, Value = "*", Position = pos - 1 };
                case '/':
                    pos++;
                    return new Token { Type = TokenType.Divide, Value = "/", Position = pos - 1 };
                case '(':
                    pos++;
                    return new Token { Type = TokenType.LParen, Value = "(", Position = pos - 1 };
                case ')':
                    pos++;
                    return new Token { Type = TokenType.RParen, Value = ")", Position = pos - 1 };
                default:
                    pos++;
                    return new Token { Type = TokenType.Unknown, Value = current.ToString(), Position = pos - 1 };
            }
        }
    }

    public class Parser
    {
        private Lexer lexer;
        private Token currentToken;
        public List<(string Error, int Position)> Errors { get; private set; }
        public List<string> Poliz { get; private set; }

        public Parser(string input)
        {
            lexer = new Lexer(input);
            currentToken = lexer.GetNextToken();
            Errors = new List<(string, int)>();
            Poliz = new List<string>();
        }

        private void Eat(TokenType type)
        {
            if (currentToken.Type == type)
            {
                currentToken = lexer.GetNextToken();
            }
            else
            {
                Errors.Add(($"Ожидался {type}, найдено {currentToken.Value}", currentToken.Position));
            }
        }

        public void Parse()
        {
            E();
            if (currentToken.Type != TokenType.End)
            {
                Errors.Add(("Ожидался конец выражения", currentToken.Position));
            }

        }

        // E → T A
        private void E()
        {
            T();
            A();
        }

        // A → ε | + T A | - T A
        private void A()
        {
            if (currentToken.Type == TokenType.Plus)
            {
                Token op = currentToken;
                Eat(TokenType.Plus);
                T();
                Poliz.Add(op.Value);
                A();
            }
            else if (currentToken.Type == TokenType.Minus)
            {
                Token op = currentToken;
                Eat(TokenType.Minus);
                T();
                Poliz.Add(op.Value);
                A();
            }
            // else: ε (пустая альтернатива), ничего не делаем
        }

        // T → O B
        private void T()
        {
            O();
            B();
        }

        // B → ε | * O B | / O B
        private void B()
        {
            if (currentToken.Type == TokenType.Multiply)
            {
                Token op = currentToken;
                Eat(TokenType.Multiply);
                O();
                Poliz.Add(op.Value);
                B();
            }
            else if (currentToken.Type == TokenType.Divide)
            {
                Token op = currentToken;
                Eat(TokenType.Divide);
                O();
                Poliz.Add(op.Value);
                B();
            }
            // else: ε
        }

            // O → num | (E)
            // В классе Parser, метод O()
            private void O()
            {
                if (currentToken.Type == TokenType.Number)
                {
                    Poliz.Add(currentToken.Value);
                    Eat(TokenType.Number);
                }
                else if (currentToken.Type == TokenType.LParen)
                {
                    Eat(TokenType.LParen);
                    E();

                    if (currentToken.Type == TokenType.RParen)
                    {
                        Eat(TokenType.RParen);
                    }
                    else
                    {
                        // Проверяем, была ли уже добавлена такая ошибка на эту позицию
                        if (!Errors.Any(e => e.Error == "Не хватает закрывающей скобки" && e.Position == currentToken.Position))
                        {
                            Errors.Add(("Не хватает закрывающей скобки", currentToken.Position));
                        }
                    }
                }
                else
                {
                    Errors.Add(("Ожидалось число или (", currentToken.Position));
                    Eat(currentToken.Type); // пропускаем ошибочный токен
                }
            }
            public double? Evaluate()
            {
                Stack<double> stack = new Stack<double>();

                foreach (var token in Poliz)
                {
                    if (double.TryParse(token, out double number))
                    {
                        stack.Push(number);
                    }
                    else
                    {
                        if (stack.Count < 2)
                        {
                            Errors.Add(($"Недостаточно операндов для оператора '{token}'", -1));
                            return null;
                        }

                        double b = stack.Pop();
                        double a = stack.Pop();

                        switch (token)
                        {
                            case "+":
                                stack.Push(a + b);
                                break;
                            case "-":
                                stack.Push(a - b);
                                break;
                            case "*":
                                stack.Push(a * b);
                                break;
                            case "/":
                                if (b == 0)
                                {
                                    Errors.Add(("Деление на ноль", -1));
                                    return null;
                                }
                                stack.Push(a / b);
                                break;
                            default:
                                Errors.Add(($"Неизвестный оператор '{token}'", -1));
                                return null;
                        }
                    }
                }

                if (stack.Count != 1)
                {
                    Errors.Add(("Ошибка при вычислении выражения", -1));
                    return null;
                }

                return stack.Pop();
            }

        }
    }
}
