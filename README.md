# Compiler Learning Kit
## Introduction
CLK is a grammar library implemented while learning the principle
of compiler.
This project includes a lib and a client writen both in c#.
The lib include kinds of grammar and related algorithms,
such as LR(1) table,first,follow set. The client is a sample
application on it.You will notice a directory called DemoInterperter,
if you take a look at project.The DemoInterperter has not been 
implemented totally because of the limited time(Final test is coming...)

## Usage
### Library
There'll be a full documentaion after fully test(It's not stable currently).
#### Create Grammar
```C#
//use default factory create a CFG, you define by yourself
Grammar grammar = DefaultGrammarFactory.CreateFromFile(@"Path to grammar file");
CFG cfg = (CFG)grammar;		
Console.WriteLine($"First:");		//the first set of nonterminal
cfg.GetFirstSetOfNonterminals().Print();
Console.WriteLine($"FirstSet:");	//the first set of grammar structure
cfg.GetFirstSetOfStructure().Print();
Console.WriteLine($"Follow");		//the follow set of nonterminal
cfg.GetFollow().Print();
Console.WriteLine("LRTable");		//LR(1)table
var lrTable = cfg.GetLRTable();
lrTable.Print();
Console.WriteLine("lritems");		
var items = cfg.GetItemsSet();
items.Print();
/* ... */
```
#### Create a Parser for CFG
```C#
// suppose a CFG type has been created called cfg.
// Actually, every type of parse is a Finite State Machine 
LRParser lRParser = new LRParser(cfg);	//Create a LR1parser, you can also create LLParser
SymbolStream lrInput = DefaultSymbolStreamFactory.CreateFromStr(cfg, "something to be analyzed"); // Terminal stream
lRParser.Init(lrInput);	
do
{
    lRParser.Walk();		//move one step, use Run() will move to end
    lRParser.PrintState();	//print state stack
} while (lRParser.GetState() == ParserState.Unfinished);
var atl = lRParser.GetParseResult();
if (atl != null)
{
    atl.Print();	// print ATL, if succeed
}
```
### Client
The client application is a Finite State Machine too.
Just compile it and run reasonable commands .
## What next?
Demo Interpreter has not been finished.
Some algorithms need fix bugs.
In one word: many work to do.


