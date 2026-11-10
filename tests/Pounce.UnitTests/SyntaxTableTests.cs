namespace Pounce.UnitTests;

using Pounce.Core;
using Xunit.Abstractions; 

public class SyntaxTableTests
{
    private readonly ITestOutputHelper output;

    public SyntaxTableTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    private static void AssertOperatorsConsistent(SyntaxTable st) =>
       Assert.Null(st.TestOperatorInvariants());

    private static void AssertConstantsConsistent(SyntaxTable st) =>
       Assert.Null(st.TestConstantInvariants());

    [Fact]
    public void OnlyFirstOperatorNull()
    {
        PounceType Statement = new PounceType("Statement");
        Signature statements = new Signature(new List<PounceType>()
                                                {
                                                    Statement,
                                                    Statement
                                                },
                                                Statement
                                            );
        SyntaxTable st = new();
        st.AddOperator("LogicalAnd",
                       Fixity.Infix,
                       null,
                       Associativity.Left,
                       statements,
                       0,
                       0,
                       0);
        Assert.Throws<PounceException>(() =>
            st.AddOperator("LogicalOr",
                           Fixity.Infix,
                           null,
                           Associativity.Left,
                           statements,
                           0,
                           0,
                           0)
        );
        AssertOperatorsConsistent(st);
    }

    [Fact]
    public void ThreeOperatorsHaveRightPrecedence()
    {
        PounceType Statement = new PounceType("Statement");
        Signature statements = new Signature(new List<PounceType>()
                                                {
                                                    Statement,
                                                    Statement
                                                },
                                                Statement
                                            );
        SyntaxTable st = new();
        st.AddOperator("LogicalOr",
                       Fixity.Infix,
                       null,
                       Associativity.Left,
                       statements,
                       0,
                       0,
                       0);
        st.AddOperatorSymbol(new OperatorRef("∨", Fixity.Infix, 0), "LogicalOr");

        Tighter tighter = new Tighter(new OperatorRef("∨", Fixity.Infix, 0));
        st.AddOperator("LogicalAnd",
                       Fixity.Infix,
                       tighter,
                       Associativity.Left,
                       statements,
                       0,
                       0,
                       0);
        Looser looser  = new Looser(new OperatorRef("∨", Fixity.Infix, 0));
        Signature oneStatement = new Signature(new List<PounceType>() {Statement}, Statement);
        st.AddOperator("LogicalNeg",
                       Fixity.Prefix,
                       looser,
                       Associativity.None,
                       oneStatement,
                       0,
                       0,
                       0);
        AssertOperatorsConsistent(st);

        Assert.True(st.PrecedenceOf("LogicalAnd") > st.PrecedenceOf("LogicalOr"));
        Assert.True(st.PrecedenceOf("LogicalOr") > st.PrecedenceOf("LogicalNeg"));
    }

    [Fact]
    public void SameAsJoinsRightLevel()
    {
        PounceType Integer = new PounceType("Integer");
        Signature integers = new Signature(new List<PounceType>()
                                                {
                                                    Integer,
                                                    Integer
                                                },
                                                Integer
                                            );
        SyntaxTable st = new();
        st.AddOperator("IntegerAdd",
                       Fixity.Infix,
                       null,
                       Associativity.Left,
                       integers,
                       0,
                       0,
                       0);
        st.AddOperatorSymbol(new OperatorRef("+", Fixity.Infix, 0), "IntegerAdd");
        SameAs sameAs = new SameAs(new OperatorRef("+", Fixity.Infix, 0));
        Assert.Throws<PounceException>(() =>
            st.AddOperator("IntegerSubtract",
                           Fixity.Infix,
                           sameAs,
                           Associativity.Right, // will throw
                           integers,
                           0,
                           0,
                           0)
        );

        st.AddOperator("IntegerSubtract",
                       Fixity.Infix,
                       sameAs,
                       Associativity.Left, // won't throw
                       integers,
                       0,
                       0,
                       0);
        AssertOperatorsConsistent(st);
        Assert.True(st.PrecedenceOf("IntegerAdd") == st.PrecedenceOf("IntegerSubtract"));
    }

    [Fact]
    public void RemovingOnlyOperatorOfLevelRenumbers()
    {
        PounceType Statement = new PounceType("Statement");
        Signature statements = new Signature(new List<PounceType>()
                                                {
                                                    Statement,
                                                    Statement
                                                },
                                                Statement
                                            );
        SyntaxTable st = new();
        st.AddOperator("LogicalOr",
                       Fixity.Infix,
                       null,
                       Associativity.Left,
                       statements,
                       0,
                       0,
                       0);
        st.AddOperatorSymbol(new OperatorRef("∨", Fixity.Infix, 0), "LogicalOr");

        Tighter tighter = new Tighter(new OperatorRef("∨", Fixity.Infix, 0));
        st.AddOperator("LogicalAnd",
                       Fixity.Infix,
                       tighter,
                       Associativity.Left,
                       statements,
                       0,
                       0,
                       0);
        Looser looser  = new Looser(new OperatorRef("∨", Fixity.Infix, 0));
        Signature oneStatement = new Signature(new List<PounceType>() {Statement}, Statement);
        st.AddOperator("LogicalNeg",
                       Fixity.Prefix,
                       looser,
                       Associativity.None,
                       oneStatement,
                       0,
                       0,
                       0);

        int andPrecedence = st.PrecedenceOf("LogicalAnd");
        int negPrecedence = st.PrecedenceOf("LogicalNeg");
        
        st.RemoveOperator("LogicalOr", 0);
        AssertOperatorsConsistent(st);
        Assert.True(st.PrecedenceOf("LogicalAnd") + 1 == andPrecedence);
        Assert.True(st.PrecedenceOf("LogicalNeg") == negPrecedence);
    }

    [Fact]
    public void RemovingOperatorOfFilledLevelDoesNotRenumber()
    {
        PounceType RealNumber = new PounceType("RealNumber");
        Signature reals = new Signature(new List<PounceType>()
                                                {
                                                    RealNumber,
                                                    RealNumber
                                                },
                                                RealNumber
                                            );
        SyntaxTable st = new();
        st.AddOperator("RealsAdd",
                       Fixity.Infix,
                       null,
                       Associativity.Left,
                       reals,
                       0,
                       0,
                       0);
        st.AddOperatorSymbol(new OperatorRef("+", Fixity.Infix, 0), "RealsAdd");
        SameAs sameAs = new SameAs(new OperatorRef("+", Fixity.Infix, 0));

        st.AddOperator("RealsSubtract",
                       Fixity.Infix,
                       sameAs,
                       Associativity.Left,
                       reals,
                       0,
                       0,
                       0);

        Tighter tighter = new Tighter(new OperatorRef("+", Fixity.Infix, 0));
        st.AddOperator("RealsMultiply",
                       Fixity.Infix,
                       tighter,
                       Associativity.Left,
                       reals,
                       0,
                       0,
                       0);
        st.AddOperatorSymbol(new OperatorRef("*", Fixity.Infix, 0), "RealsMultiply");
        sameAs = new SameAs(new OperatorRef("*", Fixity.Infix, 0));
        st.AddOperator("RealsDivide",
                       Fixity.Infix,
                       sameAs,
                       Associativity.Left,
                       reals,
                       0,
                       0,
                       0);
        int addPrecedence = st.PrecedenceOf("RealsAdd");
        int subtractPrecedence = st.PrecedenceOf("RealsSubtract");
        int dividePrecedence = st.PrecedenceOf("RealsDivide");

        st.RemoveOperator("RealsMultiply", 0);
        AssertOperatorsConsistent(st);
        Assert.True(st.PrecedenceOf("RealsAdd") == addPrecedence);
        Assert.True(st.PrecedenceOf("RealsSubtract") == subtractPrecedence);
        Assert.True(st.PrecedenceOf("RealsDivide") == dividePrecedence);
        Assert.Throws<KeyNotFoundException>(() => st.PrecedenceOf("RealsMultiply"));
    }

    [Fact]
    public void DifferentSymbolSameSignature()
    {
        PounceType Statement = new PounceType("Statement");
        Signature statements = new Signature(new List<PounceType>()
                                                {
                                                    Statement,
                                                    Statement
                                                },
                                                Statement
                                            );
        SyntaxTable st = new();
        st.AddOperator("LogicalAnd",
                       Fixity.Infix,
                       null,
                       Associativity.Left,
                       statements,
                       0,
                       0,
                       0);
        st.AddOperatorSymbol(new OperatorRef("∧", Fixity.Infix, 0), "LogicalAnd");
        st.AddOperatorSymbol(new OperatorRef("+", Fixity.Infix, 0), "LogicalAnd");
        AssertOperatorsConsistent(st);
    }

    [Fact]
    public void SameSymbolDifferentSignature()
    {
        
        PounceType RealNumber = new PounceType("RealNumber");
        Signature reals = new Signature(new List<PounceType>()
                                                {
                                                    RealNumber,
                                                    RealNumber
                                                },
                                                RealNumber
                                            );
        SyntaxTable st = new();
        st.AddOperator("RealsAdd",
                       Fixity.Infix,
                       null,
                       Associativity.Left,
                       reals,
                       0,
                       0,
                       0);
        st.AddOperatorSymbol(new OperatorRef("+", Fixity.Infix, 0), "RealsAdd");
        SameAs sameAs = new SameAs(new OperatorRef("+", Fixity.Infix, 0));

        st.AddOperator("RealsSubtract",
                       Fixity.Infix,
                       sameAs,
                       Associativity.Left,
                       reals,
                       0,
                       0,
                       0);
        // Throws because same signature
        Assert.Throws<PounceException>(() =>
            st.AddOperatorSymbol(new OperatorRef("+", Fixity.Infix, 0), "RealsSubtract")
        );
        AssertOperatorsConsistent(st);

        PounceType Integer = new PounceType("Integer");
        Signature integers = new Signature(new List<PounceType>()
                                                {
                                                    Integer,
                                                    Integer
                                                },
                                                Integer
                                            );
        st.AddOperator("IntegerAdd",
                       Fixity.Infix,
                       sameAs,
                       Associativity.Left,
                       integers,
                       0,
                       0,
                       0);
        st.AddOperatorSymbol(new OperatorRef("+", Fixity.Infix, 0), "IntegerAdd"); // Is fine because different signature
        AssertOperatorsConsistent(st);
    }

    [Fact]
    public void SameSymbolMeansSamePrecedence()
    {
        PounceType RealNumber = new PounceType("RealNumber");
        Signature reals = new Signature(new List<PounceType>()
                                                {
                                                    RealNumber,
                                                    RealNumber
                                                },
                                                RealNumber
                                            );
        SyntaxTable st = new();
        st.AddOperator("RealsAdd",
                       Fixity.Infix,
                       null,
                       Associativity.Left,
                       reals,
                       0,
                       0,
                       0);
        st.AddOperatorSymbol(new OperatorRef("+", Fixity.Infix, 0), "RealsAdd");
        Tighter tighter = new Tighter(new OperatorRef("+", Fixity.Infix, 0));
        st.AddOperator("RealsMultiply",
                       Fixity.Infix,
                       tighter,
                       Associativity.Left,
                       reals,
                       0,
                       0,
                       0);
        Assert.Throws<PounceException>( () =>
            st.AddOperatorSymbol(new OperatorRef("+", Fixity.Infix, 0), "RealsMultiply")
        );
        AssertOperatorsConsistent(st);
    }

    [Fact]
    public void LookupPicksRightOverload()
    {
        PounceType RealNumber = new PounceType("RealNumber");
        Signature reals = new Signature(new List<PounceType>()
                                                {
                                                    RealNumber,
                                                    RealNumber
                                                },
                                                RealNumber
                                            );
        SyntaxTable st = new();
        st.AddOperator("RealsAdd",
                       Fixity.Infix,
                       null,
                       Associativity.Left,
                       reals,
                       0,
                       0,
                       0);
        st.AddOperatorSymbol(new OperatorRef("+", Fixity.Infix, 0), "RealsAdd");

        SameAs sameAs = new SameAs(new OperatorRef("+", Fixity.Infix, 0));
        PounceType Integer = new PounceType("Integer");
        Signature integers = new Signature(new List<PounceType>()
                                                {
                                                    Integer,
                                                    Integer
                                                },
                                                Integer
                                            );
        st.AddOperator("IntegerAdd",
                       Fixity.Infix,
                       sameAs,
                       Associativity.Left,
                       integers,
                       0,
                       0,
                       0);
        st.AddOperatorSymbol(new OperatorRef("+", Fixity.Infix, 0), "IntegerAdd");

        OperatorInfo? opInfo;
        st.TryLookupOperator("+", Fixity.Infix, integers.OperandTypes, out opInfo);
        Assert.True(opInfo != null && opInfo.Name == "IntegerAdd");

        st.TryLookupOperator("-", Fixity.Infix, integers.OperandTypes, out opInfo);
        Assert.True(opInfo == null);

        AssertOperatorsConsistent(st);
    }

    [Fact]
    public void RemoveOperatorRemovesSymbols()
    {
        PounceType RealNumber = new PounceType("RealNumber");
        Signature reals = new Signature(new List<PounceType>()
                                                {
                                                    RealNumber,
                                                    RealNumber
                                                },
                                                RealNumber
                                            );
        SyntaxTable st = new();
        st.AddOperator("RealsAdd",
                       Fixity.Infix,
                       null,
                       Associativity.Left,
                       reals,
                       0,
                       0,
                       0);
        st.AddOperatorSymbol(new OperatorRef("+", Fixity.Infix, 0), "RealsAdd");

        SameAs sameAs = new SameAs(new OperatorRef("+", Fixity.Infix, 0));

        PounceType Integer = new PounceType("Integer");
        Signature integers = new Signature(new List<PounceType>()
                                                {
                                                    Integer,
                                                    Integer
                                                },
                                                Integer
                                            );
        st.AddOperator("IntegerAdd",
                       Fixity.Infix,
                       sameAs,
                       Associativity.Left,
                       integers,
                       0,
                       0,
                       0);
        st.AddOperatorSymbol(new OperatorRef("+", Fixity.Infix, 0), "IntegerAdd");

        st.RemoveOperator("RealsAdd", 0);
        Assert.True(!st.IsOperator("RealsAdd", Fixity.Infix));

        OperatorInfo? opInfo;
        st.TryLookupOperator("+", Fixity.Infix, reals.OperandTypes, out opInfo);
        Assert.True(opInfo == null);

        st.TryLookupOperator("+", Fixity.Infix, integers.OperandTypes, out opInfo);
        Assert.True(opInfo != null && opInfo.Name == "IntegerAdd");

        AssertOperatorsConsistent(st);
    }

    [Fact]
    public void ConstantsAndPrefixOperatorsCannotShareASymbol()
    {
        PounceType Integer = new PounceType("Integer");
        Signature integerSig = new Signature(new List<PounceType>()
                                                {
                                                    Integer
                                                },
                                                Integer
                                            );
        SyntaxTable st = new();
        st.AddConstant("TestConstant", Integer, 0);
        st.AddConstantSymbol(new ConstantRef("-", 0), "TestConstant");

        st.AddOperator("IntegerSubtract",
                       Fixity.Prefix,
                       null,
                       Associativity.None,
                       integerSig,
                       0,
                       0,
                       0);
        Assert.Throws<PounceException>(() =>
            st.AddOperatorSymbol(new OperatorRef("-", Fixity.Prefix, 0), "IntegerSubtract")
        );

        AssertConstantsConsistent(st);
        st.RemoveConstantSymbol(new ConstantRef("-", 0), "TestConstant");

        // Other order throws too.
        st.AddOperatorSymbol(new OperatorRef("-", Fixity.Prefix, 0), "IntegerSubtract");
        Assert.Throws<PounceException>(() =>
            st.AddConstantSymbol(new ConstantRef("-", 0), "TestConstant")
        );
        AssertConstantsConsistent(st);
    }

    [Fact]
    public void SameSymbolDifferentTypes()
    {
        PounceType Integer = new PounceType("Integer");
        PounceType Real = new PounceType("Real");

        SyntaxTable st = new();
        st.AddConstant("IntegerZero", Integer, 0);
        st.AddConstantSymbol(new ConstantRef("0", 0), "IntegerZero");

        st.AddConstant("RealZero", Integer, 0);
        // Throws because we gave realzero same type
        Assert.Throws<PounceException>(() =>
            st.AddConstantSymbol(new ConstantRef("0", 0), "RealZero")
        );

        st.RemoveConstant("RealZero", 0);
        AssertConstantsConsistent(st);
        st.AddConstant("RealZero", Real, 0);

        // And now it's fine
        st.AddConstantSymbol(new ConstantRef("0", 0), "RealZero");
        
        AssertConstantsConsistent(st);
    }
}
