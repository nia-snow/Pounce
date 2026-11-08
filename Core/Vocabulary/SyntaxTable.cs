using System.Diagnostics;

namespace Pounce.Core
{
    enum Fixity
    {
        Prefix, // Unary
        Infix,  // Binary
        Postfix // Unary
    }

    enum Associativity
    {
        Left,
        Right,
        None
    }

    record OperatorRef(string Symbol, Fixity Fixity, int Line);

    abstract record Placement;
    record Tighter(OperatorRef Than) : Placement;
    record Looser(OperatorRef Than) : Placement;
    record SameAs(OperatorRef As) : Placement;


    record Signature(List<string> OperandTypes, string OutputType)
    {
        public virtual bool Equals(Signature? other)
        {
            return other is not null
            && OutputType == other.OutputType
            && OperandTypes.SequenceEqual(other.OperandTypes);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(OutputType);
            foreach (string type in OperandTypes) hash.Add(type);
            return hash.ToHashCode();
        }
    }

    class OperatorInfo
    {
        public string Name { get; set; }
        public Fixity Fixity { get; set; }
        public int Precedence { get; set; }
        public Associativity Associativity { get; set; }
        public Signature Signature { get; set; }
        public HashSet<string> Symbols { get; set; }


        public OperatorInfo(string name,
                            Fixity fixity,
                            int precedence,
                            Associativity associativity,
                            Signature signature)
        {
            Name = name;
            Fixity = fixity;
            Precedence = precedence;
            Associativity = associativity;
            Signature = signature;
            Symbols = new();
        }
    }



    class SyntaxTable
    {
        // Fields
        // ------

        // From an operator's name, get the operator's info
        private Dictionary<string, OperatorInfo> Operators = new();


        // Given an OperatorRef (string, fixity), give the name of the symbol.
        private Dictionary<(string, Fixity), List<string>> SymbolToName = new();


        // The level index to a list of all operator names at that level.
        private List<List<string>> Levels = new();


        // Methods
        // -------

        public void AddOperator(string name,
                                Fixity fixity,
                                Placement? placement,
                                Associativity associativity,
                                Signature signature,
                                int defLine,
                                int sigLine,
                                int assocLine)
        {
            OperatorInfo op = new OperatorInfo(name, fixity, 0, associativity, signature);
            if (Operators.ContainsKey(name))
            {
                throw Error(defLine, $"Name \"{name}\" already added as operator");
            }
            if (fixity != Fixity.Infix && signature.OperandTypes.Count != 1
                || fixity == Fixity.Infix && signature.OperandTypes.Count != 2)
            {
                throw Error(sigLine, $"Operator marked as {fixity} yet signature takes {signature.OperandTypes.Count} arguments");
            }
            if (fixity != Fixity.Infix && associativity != Associativity.None)
            {
                throw Error(assocLine, $"Unary operator should not be given associativity");
            }
            if (fixity == Fixity.Infix && associativity == Associativity.None)
            {
                throw Error(assocLine, "Binary operator should be given non-None associativity");
            }

            int precedence;
            bool renumber = false;
            switch (placement)
            {
                case null:
                    if (Levels.Count > 0)
                    {
                        throw Error(defLine, "Placement required for operators after first operator");
                    }
                    precedence = 0;
                    Levels.Add(new List<string>{name});
                    break;
                case Tighter t:
                    precedence = GetPrecedence(t.Than) + 1;
                    Levels.Insert(precedence, new List<string>{name});
                    renumber = true;
                    break;
                case Looser l:
                    precedence = GetPrecedence(l.Than);
                    Levels.Insert(precedence, new List<string>{name});
                    renumber = true;
                    break;
                case SameAs s:
                    OperatorInfo info = GetOperatorInfoOfAnyMatching(s.As);
                    precedence = info.Precedence;
                    if (info.Associativity != associativity)
                    {
                        throw Error(assocLine, $"Associativity of operator '{name}' is not the same as '{s.As.Symbol}' ({info.Associativity})");
                    }
                    Levels[precedence].Add(name);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown type '{placement.GetType()}' for placement");
            }
            Operators[name] = op;
            if (renumber)
            {
                Renumber(precedence);
            }
            Operators[name].Precedence = precedence;
            Debug.Assert(CheckInvariants());
        }

        public void RemoveOperator(string name, int line)
        {
            OperatorInfo? opInfo;
            if (!Operators.TryGetValue(name, out opInfo))
            {
                throw Error(line, $"Operator \"{name}\" was never defined");
            }

            Levels[opInfo.Precedence].Remove(name);
            if (Levels[opInfo.Precedence].Count == 0)
            {
                Levels.RemoveAt(opInfo.Precedence);
                Renumber(opInfo.Precedence);
            }
            List<string> symbols = opInfo.Symbols.ToList();
            for (int i = 0; i < symbols.Count; i++)
            {
                RemoveSymbol(new OperatorRef(symbols[i], opInfo.Fixity, line), name);
            }
            Operators.Remove(name);
            Debug.Assert(CheckInvariants());
        }

        public void AddSymbol(OperatorRef opRef, string name)
        {
            OperatorInfo? opInfo;
            if (!Operators.TryGetValue(name, out opInfo))
            {
                throw Error(opRef.Line, $"Nonexistent operator \"{name}\"");
            }
            if (opInfo.Fixity != opRef.Fixity)
            {
                throw Error (opRef.Line, $"Operator {name} has fixity {opInfo.Fixity}, not the suggested fixity {opRef.Fixity}");
            }
            if (opInfo.Symbols.Contains(opRef.Symbol))
            {
                throw Error(opRef.Line, $"Symbol {opRef.Symbol} already registered with name \"{name}\"");
            }

            List<string>? names;
            var key = (opRef.Symbol, opRef.Fixity);
            SymbolToName.TryGetValue(key, out names);
            if (names != null)
            {
                foreach (string anotherName in names)
                {
                    if (Operators[anotherName].Precedence != Operators[name].Precedence)
                    {
                        throw Error(opRef.Line, $"Operator '{opRef.Symbol}'' does not have same precedence as {anotherName}, which shares the same symbol");
                    }
                    if (Operators[anotherName].Signature.OperandTypes.SequenceEqual(Operators[name].Signature.OperandTypes))
                    {
                        throw Error(opRef.Line, $"Operator '{opRef.Symbol}' has the same parameters as {anotherName} in its signature, which share the same symbol");
                    }
                }
            }
            else
            {
                names = new List<string>();
                SymbolToName[key] = names;
            }

            names.Add(name);
            opInfo.Symbols.Add(opRef.Symbol);
            
            Debug.Assert(CheckInvariants());
        }

        public void RemoveSymbol(OperatorRef opRef, string name)
        {
            OperatorInfo? opInfo;
            if (!Operators.TryGetValue(name, out opInfo))
            {
                throw Error(opRef.Line, $"Nonexistent operator \"{name}\"");
            }
            List<string>? names;
            if (!SymbolToName.TryGetValue((opRef.Symbol, opRef.Fixity), out names))
            {
                throw Error(opRef.Line, $"Unregistered symbol '{opRef.Symbol}' with fixity {opRef.Fixity}");
            }

            opInfo.Symbols.Remove(opRef.Symbol);
            names.Remove(name);
            if (names.Count == 0)
            {
                SymbolToName.Remove((opRef.Symbol,opRef.Fixity));
            }
            Debug.Assert(CheckInvariants());
        }

        public bool TryLookup(string symbol, Fixity fixity, List<string> operandTypes, out OperatorInfo? opInfo)
        {
            List<string>? names;
            opInfo = null;
            if (!SymbolToName.TryGetValue((symbol, fixity), out names))
            {
                return false;
            }
            for (int i = 0; i < names.Count; i++)
            {
                if (operandTypes.SequenceEqual(Operators[names[i]].Signature.OperandTypes))
                {
                    opInfo = Operators[names[i]];
                    return true;
                }
            }
            return false;
        }

        private PounceException Error(int line, string message)
        {
            return new PounceException($"[Line: {line}] {message}");
        }

        private int GetPrecedence(OperatorRef opRef)
        {
            List<string>? names;
            if (!SymbolToName.TryGetValue((opRef.Symbol, opRef.Fixity), out names))
            {
                throw Error(opRef.Line, $"Undefined operator '{opRef.Symbol}");
            }

            // All operators that share a symbol must have the same precedence
            string name = names.First();

            return Operators[name].Precedence;
        }

        private OperatorInfo GetOperatorInfoOfAnyMatching(OperatorRef opRef)
        {
            List<string>? names;
            if (!SymbolToName.TryGetValue((opRef.Symbol, opRef.Fixity), out names))
            {
                throw Error(opRef.Line, $"Undefined operator '{opRef.Symbol}");
            }
            string name = names.First();

            return Operators[name];
        }

        private void Renumber(int level)
        {
            for (int i = level; i < Levels.Count; i++)
            {
                for (int j = 0; j < Levels[i].Count; j++)
                {
                    string name = Levels[i][j];
                    Operators[name].Precedence = i;
                }
            }
        }

        private bool CheckInvariants()
        {
            // 1. All names in Levels are in Operators, and only occur once.
            HashSet<string> seen = new();
            foreach (List<string> level in Levels)
            {
                foreach  (string name in level)
                {
                    if (seen.Contains(name) || !Operators.ContainsKey(name))
                    {
                        return false;
                    }
                    seen.Add(name);
                }
            }

            // 2. Each operator's precedence is the level it is in.
            for (int i = 0; i < Levels.Count; i++)
            {
                foreach  (string name in Levels[i])
                {
                    if (Operators[name].Precedence != i)
                    {
                        return false;
                    }
                }
            }

            // 3. All levels are occupied.
            for (int i = 0; i < Levels.Count; i++)
            {
                if (Levels[i].Count == 0)
                {
                    return false;
                }
            }

            // 4. All operators on a level have the same associativity.
            foreach (List<string> level in Levels)
            {
                Associativity associativity = Operators[level.First()].Associativity;
                for  (int j = 1; j < level.Count; j++)
                {
                    if (Operators[level[j]].Associativity != associativity)
                    {
                        return false;
                    }
                }
            }

            // 5. The symbols in each operator indeed map in SymbolToName to that operator,
            //    and each name in SymbolToName is a real name.
            foreach (OperatorInfo opInfo in Operators.Values)
            {
                foreach (string symbol in opInfo.Symbols)
                {
                    if (!SymbolToName[(symbol, opInfo.Fixity)].Contains(opInfo.Name))
                    {
                        return false;
                    }
                }
            }
            foreach (List<string> names in SymbolToName.Values)
            {
                foreach (string name in names)
                {
                    if (!Operators.ContainsKey(name))
                    {
                        return false;
                    }
                }
            }

            // 6. Each SymbolToName list is non-empty.
            foreach (List<string> names in SymbolToName.Values)
            {
                if (names.Count == 0)
                {
                    return false;
                }
            }

            // 7. All names that share a symbol have the same precedence but different arguments.
            foreach (List<string> names in SymbolToName.Values)
            {
                List<List<string>> seen2 = new();
                int precedence = Operators[names.First()].Precedence;
                foreach (string name in names)
                {
                    OperatorInfo opInfo = Operators[name];
                    List<string> arguments = opInfo.Signature.OperandTypes;
                    foreach (List<string> args in seen2)
                    {
                        if (args.SequenceEqual(arguments))
                        {
                            return false;
                        }
                    }
                    seen2.Add(arguments);
                    if (opInfo.Precedence != precedence)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}