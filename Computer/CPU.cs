using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Computer
{
	/* Instruction layout:
	 * 
	 * (32-bit instruction)
	 * 
	 * opcode	= 5 bits
	 * mode		= 2 bits (0 if operand is to be interpreted as a value (no mem read); 01 if operand is a direct address(single mem read); 10 if operand is an indirect address(double mem read); 11 if mode is not applicable)
	 * indexing = 3 bits (000 == pre-increment ; 001 == post-increment ; 010 == pre-decrement ; 011 == post-decrement; 100 == indexing without operation; 101 == no indexing; 111 == not applicable; these are operations on indexregister when indexing is enabled)
	 * acc		= 4 bits (1111 == not applicable, accumulator also used with JCD to specify condition)
	 * indexreg = 4 bits (1111 == not applicable)
	 * operand	= 14 bits
	 * 
	 * 
	 * Indexing is turned off when indexing == 0b101.
	 * When indexing is enabled: the calculated operand is equal to the value stored in the index register + operand
	 * 
	 */
	/* Instruction list:
	 * 
	 * !! the operand (expression after comma) has the structure: numericalVal or numericalVal(Rx) ; respectively no indexing and indexing on
	 * 
	 * LDA opcode (0):
	 * 
	 * lda.v Rx, value		|	Load the value into register x									(mode == 00)
	 * lda.d Rx, address	|	Load the value found at RAM[address] into register x,			(mode == 01)
	 * lda.i Rx, ptrAddr	|	Load the value found at RAM[RAM[address]] into register x,		(mode == 10)
	 * 
	 * STR opcode(1):
	 * 
	 * str  Rx, address		|	Store the value in register x in memory at address,							(mode == 00 , no mem read needed to get operand)
	 * stri Rx, ptrAddr		|	Store the value in register x in memory at address found in ram at ptrAddr	(mode == 01 , a single mem read needed to get operand)
	 * 
	 * ADD opcode(2):
	 * 
	 * add.v Rx, value		|	Add the value to register x (result stored in register x)	(mode == 00)
	 * add.d Rx, address	|	Add the value found at RAM[address] to register x			(mode == 01)
	 * add.i Rx, ptrAddr	|	Add the value found at RAM[RAM[address]] to register x		(mode == 10)
	 * 
	 * CMP opcode(3):
	 * 
	 * cmp.v Rx, value		|	Compare value in register x with value and set CC					(mode == 00)
	 * cmp.d Rx, address	|	Compare value in register x with value in mem at address			(mode == 01)
	 * cmp.i Rx, ptrAddr	|	Compare value in register x with value found at RAM[RAM[address]]	(mode == 10)
	 * 
	 * JMP opcode(4):
	 * 
	 * jmp	address			|	Set program counter to address		(mode == 00)
	 * jmpi ptrAddr			|	Set program counter to RAM[ptrAddr]	(mode == 01)
	 * 
	 * JCD opcode(5):
	 * 
	 * jcd	address			|	
	 * jcdi ptrAddr			|
	 * 
	 * CALL opcode():
	 * 
	 * call subroutine		|	
	 * 
	 */
	class CPU
	{
		//program counter
		private uint pc = 0;
		/*
		 * Condition code register:
		 * 
		 * EQUAL 	== 0b00
		 * POSITIVE == 0b01
		 * NEGATIVE == 0b10
		 * 
		 * In the accumulator field of the instruction:
		 * EQUAL or ZERO 	== 0b000
		 * NEQUAL or NZERO	== 0b001
		 * GREATER or POS 	== 0b010
		 * LESSEQ or NPOS 	== 0b011
		 * LESS  or NEG  	== 0b100
		 * GREATEREQ or NNEG== 0b101
		 * 
		 */
		private uint CC;
		//The instruction which is currently being executed
		private Instruction currentInstruction;

		private readonly uint[] registers = new uint[10];

		private bool userMode = false;
		private bool kernelMode = true;

		private uint calculatedOperand = 0;

		private readonly HashSet<Instruction.Opcode> privilegedInstructions = new HashSet<Instruction.Opcode>();

		private readonly RAM ram;

		internal CPU()
		{
			ram = new RAM();

			privilegedInstructions.Add(Instruction.Opcode.STOP);
		}

		internal void Run(string pathToProgram, uint relocationAddress)
		{
			ram.LoadProgram(pathToProgram, relocationAddress);

			bool isStopped = false;

			while (!isStopped)
			{
				//Fetch
				currentInstruction = Instruction.Of(ram.Read(pc));

				pc++;

				//Analyze
				if (currentInstruction.opcode == Instruction.Opcode.UNKNOWN_INSTR)
				{
					//set interrupt flag level 9
				}
				if (!kernelMode && privilegedInstructions.Contains(currentInstruction.opcode))
				{
					//set interrupt flag level 9
				}

				uint operandFieldValue = currentInstruction.operand;

				//Pre-increment
				if (currentInstruction.indexing == 0b00) registers[currentInstruction.indexRegister]++;
				//Pre-decrement
				if (currentInstruction.indexing == 0b010) registers[currentInstruction.indexRegister]--;

				//Indexation turned off if indexing-field == 0b101 or not applicable == 0b111
				if (currentInstruction.indexing != 0b101 && currentInstruction.indexing != 0b111)
				{
					//Indexation turned on
					operandFieldValue += registers[currentInstruction.indexRegister];

				}

				//Post-increment
				if (currentInstruction.indexing == 0b001) registers[currentInstruction.indexRegister]++;
				//Post-decrement
				if (currentInstruction.indexing == 0b011) registers[currentInstruction.indexRegister]++;

				switch (currentInstruction.mode)
				{
					//The operand-field is a value (nothing has to be read from memory)
					case 0b00:
						calculatedOperand = operandFieldValue;
						break;
					//operand-field is a direct address (read the operand from memory at address given by the operand-field) = 1 mem read
					case 0b01:
						calculatedOperand = ram.Read(operandFieldValue);
						break;

					//operand-field is an indirect address (read the operand from memory at address in RAM[operand-field]) = 2 mem reads
					case 0b10:
						calculatedOperand = ram.Read(ram.Read(operandFieldValue));
						break;
				}

				//Execute
				switch (currentInstruction.opcode)
				{

					case Instruction.Opcode.LDA:
						registers[currentInstruction.accumulator] = calculatedOperand;
						setCC(calculatedOperand);
						break;

					case Instruction.Opcode.STR:
						ram.Write(registers[currentInstruction.accumulator], calculatedOperand);
						break;

					case Instruction.Opcode.ADD:
						registers[currentInstruction.accumulator] += calculatedOperand;
						break;

					case Instruction.Opcode.SUB:
						registers[currentInstruction.accumulator] -= calculatedOperand;
						break;

					case Instruction.Opcode.MUL:
						registers[currentInstruction.accumulator] *= calculatedOperand;
						break;

					case Instruction.Opcode.DIV:
						registers[currentInstruction.accumulator] /= calculatedOperand;
						break;

					case Instruction.Opcode.MOD:
						registers[currentInstruction.accumulator] %= calculatedOperand;
						break;

					/*
					 * Condition code register:
					 * 
					 * EQUAL 	== 0b00
					 * POSITIVE == 0b01
					 * NEGATIVE == 0b10
					 * 
					 * In the accumulator field of the instruction:
					 * EQUAL or ZERO 	== 0b000
					 * NEQUAL or NZERO	== 0b001
					 * GREATER or POS 	== 0b010
					 * LESSEQ or NPOS 	== 0b011
					 * LESS  or NEG  	== 0b100
					 * GREATEREQ or NNEG== 0b101
					 * 
					 */

					case Instruction.Opcode.CMP:
						uint difference = registers[currentInstruction.accumulator] - calculatedOperand;
						setCC(difference);
						break;

					case Instruction.Opcode.JCD:
						uint condition = currentInstruction.accumulator;
						bool allowedJump = false;
						allowedJump = (condition == 0 && CC == 0) ||
										(condition == 1 && CC != 0) ||
										(condition == 2 && CC == 1) ||
										(condition == 3 && CC != 1) ||
										(condition == 4 && CC == 2) ||
										(condition == 5 && CC != 2);

						if (allowedJump) pc = calculatedOperand;
						break;

					//condition stored in accumulator
					case Instruction.Opcode.JMP:
						pc = calculatedOperand;
						break;

					case Instruction.Opcode.CALL:
						//push return address
						registers[9]--;
						ram.Write(pc, registers[9]);
						//jump to subroutine
						pc = calculatedOperand;
						break;
					case Instruction.Opcode.RET:
						pc = ram.Read(registers[9]);
						registers[9]++;
						break;

					case Instruction.Opcode.PUSH:
						registers[9]--;
						ram.Write(registers[currentInstruction.accumulator], registers[9]);
						break;

					case Instruction.Opcode.POP:
						registers[currentInstruction.accumulator] = ram.Read(registers[9]);
						registers[9]--;
						break;

					case Instruction.Opcode.PRINT:
						Console.WriteLine(registers[0]);
						break;

					case Instruction.Opcode.STOP:
						isStopped = true;
						//Put the CPU in halt state
						break;

				}
			}


		}

		void setCC(uint result)
		{
			if (result == 0) CC = 0;
			else if (result > 0) CC = 1;
			else if (result < 0) CC = 2;
		}
	}
}
