using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Computer;

namespace Assembler
{
    /// <summary>
    /// For currently supported features, see example program "example.dra"
    /// 
    /// TODO: add keywords for JCD: EQUAL, GREATER, ...
    /// 
    /// </summary>
    public class MachineCodeGenerator
    {
        /*private readonly Dictionary<String, uint> opcodeTable = new()
        {
            { "lda", 0b00000 },
            { "str", 0b00001 },
            { "add", 0b00010 },
            { "sub", 0b00011 },
            { "mul", 0b00100 },
            { "div", 0b00101 },
            { "mod", 0b00110 },
            { "cmp", 0b00111 },
            { "jmp", 0b01000 },
            { "jcd", 0b01001 },
            { "call", 0b01010 },
            { "ret", 0b01011 },
            { "push", 0b01100 },
            { "pop", 0b01101 },
            { "print", 0b01110 },
            { "stop", 0b01111 }
        };*/

        private readonly Dictionary<String, int> symbolTable = new();

        private readonly string[] srcLines;

        private readonly Dictionary<String, String> dictionary = new();

        private List<uint> machineCode = new();
        public uint[] getMachineCode()
        {
            return machineCode.ToArray();
        }

        private string[] Tokenize(string line)
        {
            List<String> tokens = new();

            string stringToken = "";
            if (line.Length != 0 && !line.StartsWith("|"))
            {
                foreach (char c in line)
                {
                    if (c == '|')
                    {
                        if (stringToken.Length > 0)
                        {
                            tokens.Add(stringToken);
                            stringToken = "";
                        }
                        break;
                    }
                    if (c == ' ' || c == '\t')
                    {
                        if (stringToken.Length > 0)
                        {
                            tokens.Add(stringToken);
                            stringToken = "";
                        }
                        continue;
                    }
                    if (c == ':' || c == ',' || c == ';' || c == '(' || c == ')' || c == '+' || c == '-')
                    {
                        if (stringToken.Length > 0)
                        {
                            tokens.Add(stringToken);
                            stringToken = "";
                        }
                        tokens.Add(c.ToString());
                        continue;
                    }
                    stringToken += c;
                }

                if (stringToken.Length > 0)
                {
                    tokens.Add(stringToken);
                    stringToken = "";
                }
            }
            return tokens.ToArray();
        }

        private string[] ReplaceSynonyms(string[] tokens)
        {
            string[] translatedTokens = new string[tokens.Length];
            Array.Copy(tokens, translatedTokens, tokens.Length);

            for (int i = 0; i < translatedTokens.Length; i++)
            {
                if (dictionary.ContainsKey(translatedTokens[i]))
                {
                    translatedTokens[i] = dictionary[translatedTokens[i]];
                }
            }
            return translatedTokens;
        }
        public MachineCodeGenerator(string pathToSourceCode, string pathToDictionary)
        {
            srcLines = System.IO.File.ReadAllLines(pathToSourceCode);
            GenerateDictionary(pathToDictionary);
            GenerateSymbolTable();
            Console.WriteLine("Symbol Table: ");
            foreach (string symbol in symbolTable.Keys)
            {
                Console.WriteLine(symbol + " -> " + symbolTable[symbol]);
            }
            GenerateMachineCode();
            WriteMachineCodeToFile(pathToSourceCode);
        }

        private void WriteMachineCodeToFile(string pathToSourceCode)
        {
            string fileName = Path.GetFileNameWithoutExtension(pathToSourceCode);
            string destPath = Path.GetDirectoryName(pathToSourceCode) + "\\" + fileName + ".bin";
            using (BinaryWriter writer = new BinaryWriter(File.Open(destPath, FileMode.Create)))
            {
                foreach (uint machineCodeLine in machineCode)
                {
                    writer.Write(machineCodeLine);
                    Instruction instr = Instruction.Of(machineCodeLine);
                    Console.WriteLine("Written instruction: ");
                    Instruction.PrintInstructionLayout(instr);
                }
            }
            using (BinaryReader reader = new BinaryReader(File.Open(destPath, FileMode.Open)))
            {
                for (int i = 0; i < machineCode.Count; i++)
                {
                    Console.WriteLine(reader.ReadUInt32());
                }

            }
        }

        private void GenerateSymbolTable()
        {
            int lineCounter = 1;
            int programCounter = 0;

            foreach (string srcLine in srcLines)
            {
                string[] tokens = Tokenize(srcLine);
                tokens = ReplaceSynonyms(tokens);

                if (LineContainsLabel(tokens))
                {
                    string label = retrieveLabel(tokens);
                    if (!symbolTable.ContainsKey(label))
                        symbolTable.Add(label, programCounter);
                    else Console.WriteLine("ERROR: " + label + " has multiple occurences (at line " + lineCounter + ")");
                }

                int increment = 0;

                switch (LineType(tokens))
                {
                    case "COMMENT": break;
                    case "RESERVE": increment = AmountOfReservations(tokens); break;
                    case "CONSTANT": increment = AmountOfConstants(tokens); break;
                    case "INSTRUCTION": increment = 1; break;
                }
                lineCounter++;
                programCounter += increment;
            }
        }

        private string LineType(string[] tokens)
        {
            string state = "START";
            if (tokens.Length == 0) { return "COMMENT"; }
            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i];
                if (state == "START")
                {
                    if (token == "|" || token == "") return "COMMENT";
                    else
                    {
                        state = "NAME";
                        if (i == tokens.Length - 1) { return "INSTRUCTION"; }
                        continue;
                    }
                }
                if (state == "NAME")
                {
                    if (token == ":") { state = "COLON"; continue; }
                    else { return "INSTRUCTION"; }
                }
                if (state == "COLON")
                {
                    if (token == "reserve") return "RESERVE";
                    else return "CONSTANT";
                }
            }
            return state;
        }

        /// <summary>
        /// Returns the amount of reservations in a line of type reservation
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private int AmountOfReservations(string[] tokens)
        {
            int amount = 0;
            int.TryParse(tokens[3], out amount);
            return amount;
        }

        private int AmountOfConstants(string[] tokens)
        {
            int amountOfSemiColons = 0;

            foreach (string token in tokens)
            {
                if (token == ";") amountOfSemiColons++;
            }
            return amountOfSemiColons + 1;
        }

        private string retrieveLabel(string[] tokens)
        {
            return tokens[0];
        }
        private bool LineContainsLabel(string[] tokens)
        {
            return tokens.Contains(":");
        }
        private void GenerateMachineCode()
        {
            int lineCounter = 1;
            int programCounter = 0;

            foreach (string srcLine in srcLines)
            {
                string[] tokens = Tokenize(srcLine);
                tokens = ReplaceSynonyms(tokens);

                switch (LineType(tokens))
                {
                    case "COMMENT": break;
                    case "INSTRUCTION": GenerateMachineInstruction(tokens); break;
                    case "RESERVE": GenerateReservations(tokens); break;
                    case "CONSTANT": GenerateConstants(tokens); break;
                }
            }
        }

        private void GenerateConstants(string[] tokens)
        {
            for (int i = 2; i < tokens.Length; i++)
            {
                Console.WriteLine("Constant?: ");
                if (tokens[i] != ";")
                {
                    uint constant = uint.Parse(tokens[i]);
                    Console.WriteLine("added constant: " + constant);
                    machineCode.Add(constant);
                }

            }
        }

        private void GenerateReservations(string[] tokens)
        {
            uint amountOfReservations = uint.Parse(tokens[3]);
            for (uint i = 0; i < amountOfReservations; i++)
            {
                machineCode.Add(0);
            }
        }

        private void GenerateMachineInstruction(string[] tokens)
        {

            //Replace labels with the correct address, allow users to perform arithmetic on these labels

            for (int j = 0; j < tokens.Length; j++)
            {
                string token = tokens[j];
                if (symbolTable.ContainsKey(token))
                {
                    uint calculatedExpression = (uint)symbolTable[token];
                    if (j + 1 < tokens.Length)
                    {
                        if (tokens[j + 1] == "+")
                        {
                            calculatedExpression += uint.Parse(tokens[j + 2]);
                            tokens = RemoveElementAt(j + 1, tokens);
                            tokens = RemoveElementAt(j + 1, tokens);
                        }
                        else if (tokens[j + 1] == "-")
                        {
                            calculatedExpression -= uint.Parse(tokens[j + 2]);
                            tokens = RemoveElementAt(j + 1, tokens);
                            tokens = RemoveElementAt(j + 1, tokens);
                        }
                    }

                    tokens[j] = calculatedExpression.ToString();
                }
            }

            string[] RemoveElementAt(int index, string[] arr)
            {
                string[] result = new string[arr.Length - 1];
                for (int j = 0; j < index; j++)
                {
                    result[j] = arr[j];
                }
                for (int j = index + 1; j < arr.Length; j++)
                {
                    result[j - 1] = arr[j];
                }
                return result;
            }

            Instruction.Opcode opcode = Instruction.Opcode.UNKNOWN_INSTR;
            uint mode = 0;
            uint indexing = 0;
            uint accumulator = 0;
            uint indexRegister = 0;
            uint operand = 0;

            int i = 0; // token index

            CompileOpcode();
            CompileArguments();
            ReadyToGenerateUint();

            void CompileOpcode()
            {
                string opcodeString = "";
                opcodeString = getToken();
                string[] parts = opcodeString.Split(":");
                string opcodeName = parts[0];
                string modeString = parts[1];
                mode = (uint)int.Parse(modeString); // fill in the mode field

                opcode = Instruction.GetOpcodeFromName(opcodeName);

            }

            void CompileArguments()
            {
                if (hasMoreTokens())
                {
                    if (opcode == Instruction.Opcode.JCD)
                    {
                        string conditionString = getToken();
                        switch (conditionString)
                        {
                            case "EQUAL": accumulator = 0b000; break;
                            case "NEQUAL": accumulator = 0b001; break;
                            case "GREATER": accumulator = 0b010; break;
                            case "LESSEQ": accumulator = 0b011; break;
                            case "LESS": accumulator = 0b100; break;
                            case "GREATEREQ": accumulator = 0b100; break;
                        }
                        eat(",");
                        compileIndexedOperand();

                    }
                    else if (readToken()[0] == 'r' || readToken()[0] == 'R')
                    {
                        string registerString = getToken();
                        accumulator = uint.Parse(registerString[1].ToString());
                        if (hasMoreTokens())
                        {
                            eat(",");
                            if (readToken()[0] == 'r' || readToken()[0] == 'R')
                            {
                                //Instruction performs a register-register operation, this gets implicitly converted from: "instr Rx, Ry" to: "instr Rx, 0(Ry)"
                                string secondRegisterString = getToken();
                                indexRegister = uint.Parse(secondRegisterString[1].ToString());
                                indexing = 0b100;
                                mode = 0b00; //!! mode is set back to "value", because default interpretation is "direct address"
                            }
                            else
                            {
                                compileIndexedOperand();
                            }
                        }
                        else
                        {
                            //Instruction with one argument = a register
                            indexing = 0b111;
                            indexRegister = 0b1111;
                            operand = 0x3fff;
                        }
                    }
                    else
                    {
                        accumulator = 0b111;
                        compileIndexedOperand();
                    }
                }
                else
                {
                    // The instruction has no arguments
                    indexing = 0b101;
                    accumulator = 0b1111;
                    indexRegister = 0b1111;
                    operand = 0x3fff;
                }
            }

            ///indexed operand: <number> || <number> "(" Rx ")"
            void compileIndexedOperand()
            {
                operand = uint.Parse(getToken());// the numerical value part

                if (hasMoreTokens())
                {
                    //The operand has an index register, with potentially post/pre increment/decrement
                    eat("(");
                    compileIndexRegister();
                    eat(")");
                }
                else
                {
                    //No index register => no indexing
                    indexRegister = 0b1111;
                    indexing = 0b111;
                }

            }

            void compileIndexRegister()
            {
                if (readToken() == "+") { getToken(); indexing = 0b000; IndexRegister(); return; } //pre-increment
                if (readToken() == "-") { getToken(); indexing = 0b010; IndexRegister(); return; } //pre-decrement
                IndexRegister();
                if (readToken() == "+") { getToken(); indexing = 0b001; return; } //post-incement
                if (readToken() == "-") { getToken(); indexing = 0b011; return; } //post-decrement

                void IndexRegister()
                {
                    indexRegister = uint.Parse(getToken()[1].ToString());
                }
            }

            void ReadyToGenerateUint()
            {
                Instruction instr = new Instruction(opcode, mode, indexing, accumulator, indexRegister, operand);
                machineCode.Add(instr.bitRepresentation);
            }

            bool TokenIsRegister(string token)
            {
                return (token[0] == 'r' || token[0] == 'R');
            }

            void eat(string expectedToken)
            {
                if (tokens[i] != expectedToken)
                {
                    throw new Exception("expected: " + expectedToken);
                }
                i++;
            }

            string readToken()
            {
                return tokens[i];
            }

            string getToken()
            {
                string token = tokens[i];
                i++;
                return token;
            }

            void putBack()
            {
                i--;
            }

            bool hasMoreTokens() { return i <= tokens.Length - 1; }
        }


        private void GenerateDictionary(string pathToDictionary)
        {
            //Generate the dictionary that maps synonym opcodes to the native opcodes used by the MachineCodeGenerator
            string[] dictionaryLines = System.IO.File.ReadAllLines(pathToDictionary);

            foreach (string line in dictionaryLines)
            {

                string cleanLine = line.Replace(" ", string.Empty);
                cleanLine = cleanLine.Replace("\t", string.Empty);
                string[] parts = cleanLine.Split("=");
                if (parts != null && parts.Length == 2)
                {
                    string[] synonyms = parts[1].Split(",");

                    if (synonyms != null)
                    {
                        foreach (string synonym in synonyms)
                        {
                            dictionary.Add(synonym, parts[0]);
                        }
                    }

                }
            }
            foreach (string key in dictionary.Keys)
            {
                Console.WriteLine(key + "->" + dictionary[key]);
            }

        }
    }
}
