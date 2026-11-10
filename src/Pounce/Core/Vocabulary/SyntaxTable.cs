namespace Pounce.Core;

using System.Diagnostics;

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

record ConstantRef(string Symbol, int Line);

abstract record Placement;
record Tighter(OperatorRef Than) : Placement;
record Looser(OperatorRef Than) : Placement;
record SameAs(OperatorRef As) : Placement;

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

class ConstantInfo
{
    public string Name { get; set; }
    public PounceType Type { get; set; }
    public HashSet<string> Symbols { get; set; }

    public ConstantInfo(string name, PounceType type)
    {
        Name = name;
        Type = type;
        Symbols = new();
    }
}



class SyntaxTable
{
    // Fields
    // ------

    // Operators

    // From an operator's name, get the operator's info
    private Dictionary<string, OperatorInfo> Operators = new();


    // Given an OperatorRef (string, fixity), give the name of the symbol.
    private Dictionary<(string, Fixity), List<string>> SymbolToOperatorName = new();


    // The level index to a list of all operator names at that level.
    private List<List<string>> Levels = new();


    // Constants

    // From a constant's name, get the constant's info
    private Dictionary<string, ConstantInfo> Constants = new();

    // Given a symbol, give the name of the symbol
    private Dictionary<string, List<string>> SymbolToConstantName = new();


    // Methods
    // -------

    // Operators

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
        if (Constants.ContainsKey(name))
        {
            throw Error(defLine, $"Name \"{name}\" already added as constant");
        }
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
        Debug.Assert(TestOperatorInvariants() == null);
    }

    public void RemoveOperator(string name, int line)
    {
        OperatorInfo? opInfo;
        if (!Operators.TryGetValue(name, out opInfo))
        {
            throw Error(line, $"Operator \"{name}\" was never defined");
        }
        List<string> symbols = opInfo.Symbols.ToList();
        for (int i = 0; i < symbols.Count; i++)
        {
            RemoveOperatorSymbol(new OperatorRef(symbols[i], opInfo.Fixity, line), name);
        }
        Levels[opInfo.Precedence].Remove(name);
        if (Levels[opInfo.Precedence].Count == 0)
        {
            Levels.RemoveAt(opInfo.Precedence);
            Renumber(opInfo.Precedence);
        }
        Operators.Remove(name);
        Debug.Assert(TestOperatorInvariants() == null);
    }

    public void AddOperatorSymbol(OperatorRef opRef, string name)
    {
        OperatorInfo? opInfo;
        if (!Operators.TryGetValue(name, out opInfo))
        {
            throw Error(opRef.Line, $"Nonexistent operator \"{name}\"");
        }
        if (opInfo.Fixity != opRef.Fixity)
        {
            throw Error(opRef.Line, $"Operator {name} has fixity {opInfo.Fixity}, not the suggested fixity {opRef.Fixity}");
        }
        if (opInfo.Symbols.Contains(opRef.Symbol))
        {
            throw Error(opRef.Line, $"Symbol '{opRef.Symbol}' already registered with name \"{name}\"");
        }
        if (opRef.Fixity == Fixity.Prefix && IsConstant(opRef.Symbol))
        {
            throw Error(opRef.Line, $"Can not define prefix operator '{opRef.Symbol}' when constant already uses this symbol");
        }

        List<string>? names;
        var key = (opRef.Symbol, opRef.Fixity);
        SymbolToOperatorName.TryGetValue(key, out names);
        if (names != null)
        {
            foreach (string anotherName in names)
            {
                if (Operators[anotherName].Precedence != Operators[name].Precedence)
                {
                    throw Error(opRef.Line, $"Operator '{opRef.Symbol}' does not have same precedence as {anotherName}, which shares the same symbol");
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
            SymbolToOperatorName[key] = names;
        }

        names.Add(name);
        opInfo.Symbols.Add(opRef.Symbol);
        
        Debug.Assert(TestOperatorInvariants() == null);
    }

    public void RemoveOperatorSymbol(OperatorRef opRef, string name)
    {
        OperatorInfo? opInfo;
        if (!Operators.TryGetValue(name, out opInfo))
        {
            throw Error(opRef.Line, $"Nonexistent operator \"{name}\"");
        }
        List<string>? names;
        if (!SymbolToOperatorName.TryGetValue((opRef.Symbol, opRef.Fixity), out names))
        {
            throw Error(opRef.Line, $"Unregistered symbol '{opRef.Symbol}' with fixity {opRef.Fixity}");
        }

        if (!opInfo.Symbols.Contains(opRef.Symbol))
        {
            throw Error(opRef.Line, $"Symbol '{opRef.Symbol}' not registered with name \"{name}\"");
        }

        opInfo.Symbols.Remove(opRef.Symbol);
        names.Remove(name);
        if (names.Count == 0)
        {
            SymbolToOperatorName.Remove((opRef.Symbol,opRef.Fixity));
        }
        Debug.Assert(TestOperatorInvariants() == null);
    }

    public bool TryLookupOperator(string symbol, Fixity fixity, List<PounceType> operandTypes, out OperatorInfo? opInfo)
    {
        List<string>? names;
        opInfo = null;
        if (!SymbolToOperatorName.TryGetValue((symbol, fixity), out names))
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

    public bool IsOperator(string symbol, Fixity fixity)
    {
        return SymbolToOperatorName.ContainsKey((symbol, fixity));
    }

    public IReadOnlyList<string> AllOperatorsWith(string symbol, Fixity fixity)
    {
        if (!IsOperator(symbol, fixity))
        {
            return new List<string>();
        }
        return SymbolToOperatorName[(symbol, fixity)];
    }

    public int PrecedenceOf(string name)
    {
        return Operators[name].Precedence;
    }

    // Constants

    public void AddConstant(string name, PounceType type, int line)
    {
        if (Constants.ContainsKey(name))
        {
            throw Error(line, $"Name \"{name}\" already added as constant");
        }
        if (Operators.ContainsKey(name))
        {
            throw Error(line, $"Name \"{name}\" already added as operator");
        }
        Constants.Add(name, new ConstantInfo(name, type));
        Debug.Assert(TestConstantInvariants() == null);
    }

    public void RemoveConstant(string name, int line)
    {
        ConstantInfo? constInfo;
        if (!Constants.TryGetValue(name, out constInfo))
        {
            throw Error(line, $"Constant '{name}' was never defined");
        }

        List<string> symbols = constInfo.Symbols.ToList();
        for (int i = 0; i < symbols.Count; i++)
        {
            RemoveConstantSymbol(new ConstantRef(symbols[i], line), name);
        }
        Constants.Remove(name);
        Debug.Assert(TestConstantInvariants() == null);
    }

    public void AddConstantSymbol(ConstantRef constRef, string name)
    {
        ConstantInfo? constInfo;
        if (!Constants.TryGetValue(name, out constInfo))
        {
            throw Error(constRef.Line, $"Nonexistent constant \"{name}\"");
        }
        if (constInfo.Symbols.Contains(constRef.Symbol))
        {
            throw Error(constRef.Line, $"Constant '{constRef.Symbol}' already registered with name \"{name}\"");
        }
        if (IsOperator(constRef.Symbol, Fixity.Prefix))
        {
            throw Error(constRef.Line, $"Can not define constant '{constRef.Symbol}' when prefix operator already uses this symbol");
        }

        List<string>? names;
        SymbolToConstantName.TryGetValue(constRef.Symbol, out names);
        if (names != null)
        {
            foreach (string anotherName in names)
            {
                if (Constants[anotherName].Type == Constants[name].Type)
                {
                    throw Error(constRef.Line, $"Constant '{constRef.Symbol}' has the same type as {anotherName}, which share the same symbol");
                }
            }
        }
        else
        {
            names = new List<string>();
            SymbolToConstantName[constRef.Symbol] = names;
        }

        names.Add(name);
        constInfo.Symbols.Add(constRef.Symbol);
        Debug.Assert(TestConstantInvariants() == null);
    }

    public void RemoveConstantSymbol(ConstantRef constRef, string name)
    {
        ConstantInfo? constInfo;
        if (!Constants.TryGetValue(name, out constInfo))
        {
            throw Error(constRef.Line, $"Nonexistent constant \"{name}\"");
        }
        List<string>? names;
        if (!SymbolToConstantName.TryGetValue(constRef.Symbol, out names))
        {
            throw Error(constRef.Line, $"Unregistered constant '{constRef.Symbol}' with type {constInfo.Type}");
        }

        if (!constInfo.Symbols.Contains(constRef.Symbol))
        {
            throw Error(constRef.Line, $"Symbol '{constRef.Symbol}' not registered with name \"{name}\"");
        }

        constInfo.Symbols.Remove(constRef.Symbol);
        names.Remove(name);
        if (names.Count == 0)
        {
            SymbolToConstantName.Remove(constRef.Symbol);
        }
        Debug.Assert(TestConstantInvariants() == null);
    }

    public bool TryLookupConstant(string symbol, PounceType type, out ConstantInfo? constInfo)
    {
        List<string>? names;
        constInfo = null;
        if (!SymbolToConstantName.TryGetValue(symbol, out names))
        {
            return false;
        }
        for (int i = 0; i < names.Count; i++)
        {
            if (type == Constants[names[i]].Type)
            {
                constInfo = Constants[names[i]];
                return true;
            }
        }
        return false;
    }

    public bool IsConstant(string symbol)
    {
        return SymbolToConstantName.ContainsKey(symbol);
    }

    public IReadOnlyList<string> AllConstantsWith(string symbol)
    {
        if (!IsConstant(symbol))
        {
            return new List<string>();
        }
        return SymbolToConstantName[symbol];
    }


    // Privates

    private PounceException Error(int line, string message)
    {
        return new PounceException($"[Line: {line}] {message}");
    }

    private int GetPrecedence(OperatorRef opRef)
    {
        List<string>? names;
        if (!SymbolToOperatorName.TryGetValue((opRef.Symbol, opRef.Fixity), out names))
        {
            throw Error(opRef.Line, $"Undefined operator '{opRef.Symbol}'");
        }

        // All operators that share a symbol must have the same precedence
        string name = names.First();

        return Operators[name].Precedence;
    }

    private OperatorInfo GetOperatorInfoOfAnyMatching(OperatorRef opRef)
    {
        List<string>? names;
        if (!SymbolToOperatorName.TryGetValue((opRef.Symbol, opRef.Fixity), out names))
        {
            throw Error(opRef.Line, $"Undefined operator '{opRef.Symbol}'");
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

    // Debug

    public string? TestOperatorInvariants()
    {
        // 1. All names in Levels are in Operators and only occur once.
        Dictionary<string, int> seen = new();
        for (int i = 0; i < Levels.Count; i++)
        {
            foreach  (string name in Levels[i])
            {
                if (seen.ContainsKey(name))
                {
                    return $"Operator {name} occurs in level {i} and level {seen[name]}";
                }
                seen.Add(name, i);
                if (!Operators.ContainsKey(name))
                {
                    return $"\"{name}\" is not registered as an operator";
                }
            }
        }

        // 2. Each operator's precedence is the level it is in.
        for (int i = 0; i < Levels.Count; i++)
        {
            foreach  (string name in Levels[i])
            {
                if (Operators[name].Precedence != i)
                {
                    return $"Operator {name} has precedence {Operators[name].Precedence} yet is located on level {i}";
                }
            }
        }

        // 3. All levels are occupied.
        for (int i = 0; i < Levels.Count; i++)
        {
            if (Levels[i].Count == 0)
            {
                return $"Level {i} is empty";
            }
        }

        // 4. All operators on a level have the same associativity.
        foreach (List<string> level in Levels)
        {
            Associativity associativity = Operators[level.First()].Associativity;
            for  (int j = 1; j < level.Count; j++)
            {
                Associativity storedAssociativity = Operators[level[j]].Associativity;
                if (storedAssociativity != associativity)
                {
                    return $"Operator {level[j]} has associativity {storedAssociativity} yet operators on level {Operators[level[j]].Precedence} have associativity {associativity}";
                }
            }
        }

        // 5. The symbols in each operator indeed map in SymbolToOperatorName to that operator,
        //    and each name in SymbolToOperatorName is a real name.
        foreach (OperatorInfo opInfo in Operators.Values)
        {
            foreach (string symbol in opInfo.Symbols)
            {
                if (!SymbolToOperatorName[(symbol, opInfo.Fixity)].Contains(opInfo.Name))
                {
                    return $"Symbol '{symbol}' is listed for operator {opInfo.Name} yet is not registered in symbol table";
                }
            }
        }
        foreach (List<string> names in SymbolToOperatorName.Values)
        {
            foreach (string name in names)
            {
                if (!Operators.ContainsKey(name))
                {
                    return $"Name \"{name}\" in symbol table but not in Operators table";
                }
            }
        }

        // 6. Each SymbolToOperatorName list is non-empty.
        foreach ((string symbol, Fixity fixity) in SymbolToOperatorName.Keys)
        {
            if (SymbolToOperatorName[(symbol, fixity)].Count == 0)
            {
                return $"Symbol '{symbol}' with fixity {fixity} has an empty list of names associated";
            }
        }

        // 7. All names that share a symbol have the same precedence but different arguments.
        foreach (List<string> names in SymbolToOperatorName.Values)
        {
            List<List<PounceType>> seen2 = new();
            int precedence = Operators[names.First()].Precedence;
            foreach (string name in names)
            {
                OperatorInfo opInfo = Operators[name];
                List<PounceType> arguments = opInfo.Signature.OperandTypes;
                foreach (List<PounceType> args in seen2)
                {
                    if (args.SequenceEqual(arguments))
                    {
                        return $"{name} shares the same arguments as another operator with the same symbol";
                    }
                }
                seen2.Add(arguments);
                if (opInfo.Precedence != precedence)
                {
                    return $"{name}'s precedence is not the same as other operators with the same symbol";
                }
            }
        }

        return null;
    }

    public string? TestConstantInvariants()
    {
        // 1. The symbols in each constant indeed map in SymbolToConstantName to that constant,
        // and each name in SymbolToConstantName is a real name.
        foreach (ConstantInfo constInfo in Constants.Values)
        {
            foreach (string symbol in constInfo.Symbols)
            {
                if (!SymbolToConstantName[symbol].Contains(constInfo.Name))
                {
                    return $"Symbol '{symbol}' is listed for constant {constInfo.Name} yet is not registered in symbol table";;
                }
            }
        }
        foreach (List<string> names in SymbolToConstantName.Values)
        {
            foreach (string name in names)
            {
                if (!Constants.ContainsKey(name))
                {
                    return $"Name \"{name}\" in symbol table but not in Constants table";;
                }
            }
        }

        // 2. Each SymbolToConstantName list is non-empty.
        foreach (string symbol in SymbolToConstantName.Keys)
        {
            if (SymbolToConstantName[symbol].Count == 0)
            {
                return $"Symbol '{symbol}' has an empty list of names associated";;
            }
        }

        // 3. All names that share a symbol have different types.
        foreach (List<string> names in SymbolToConstantName.Values)
        {
            HashSet<PounceType> seen = new();
            foreach (string name in names)
            {
                ConstantInfo constInfo = Constants[name];
                PounceType type = constInfo.Type;
                if (seen.Contains(type))
                {
                    return $"{name} shares the type {constInfo.Type} as another constant with the same symbol";
                }
                seen.Add(type);
            }
        }
        return null;
    }
}