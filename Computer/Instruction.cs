using System;
using System.Collections.Generic;
using System.Text;

namespace Computer
{
	public class Instruction
	{
		public static readonly int wordLength = opcodeLength + modeLength + indexingLength + accumulatorLength + indexRegisterLength + operandLength;

		public static readonly int opcodeLength = 5;
		public static readonly int modeLength = 2;
		public static readonly int indexingLength = 3;
		public static readonly int accumulatorLength = 4;
		public static readonly int indexRegisterLength = 4;
		public static readonly int operandLength = 14;

		/// <summary>
		/// The highest possible address(inclusive) that is addressable by the operand
		/// </summary>
		public static readonly uint maxAddressableLocation = 16383;

		public enum Opcode
		{
			LDA = 0, STR = 1, ADD = 2, SUB = 3, MUL = 4, DIV = 5, MOD = 6, CMP = 7, JMP = 8, JCD = 9, CALL = 10, RET = 11, PUSH = 12, POP = 13, PRINT = 14, STOP = 15, UNKNOWN_INSTR

		}
		public static Opcode GetOpcodeFromName(string opcodeName)
		{
			return (Instruction.Opcode)Enum.Parse(typeof(Instruction.Opcode), opcodeName);
		}

		public readonly uint bitRepresentation;

		public readonly Opcode opcode;
		public readonly uint mode;
		public readonly uint indexing;
		public readonly uint accumulator;
		public readonly uint indexRegister;
		public readonly uint operand;

		public Instruction(Opcode opcode, uint mode, uint indexing, uint accumulator, uint indexRegister, uint operand)
		{
			this.opcode = opcode;
			this.mode = mode;
			this.indexing = indexing;
			this.accumulator = accumulator;
			this.indexRegister = indexRegister;
			this.operand = operand;

			uint bits = 0;

			bits |= operand;

			bits |= indexRegister << operandLength;

			bits |= accumulator << operandLength + indexRegisterLength;

			bits |= indexing << operandLength + indexRegisterLength + accumulatorLength;

			bits |= mode << operandLength + indexRegisterLength + accumulatorLength + indexingLength;

			bits |= (uint)opcode << operandLength + indexRegisterLength + accumulatorLength + indexingLength + modeLength;

			this.bitRepresentation = bits;
		}

		public static Instruction Of(uint bitRepresentation)
		{
			uint l_operand = bitRepresentation & GenerateMask(operandLength);
			bitRepresentation >>= operandLength;
			uint l_indexRegister = bitRepresentation & GenerateMask(indexRegisterLength);
			bitRepresentation >>= indexRegisterLength;
			uint l_accumulator = bitRepresentation & GenerateMask(accumulatorLength);
			bitRepresentation >>= accumulatorLength;
			uint l_indexing = bitRepresentation & GenerateMask(indexingLength);
			bitRepresentation >>= indexingLength;
			uint l_mode = bitRepresentation & GenerateMask(modeLength);
			bitRepresentation >>= modeLength;
			Opcode l_opcode = Opcode.UNKNOWN_INSTR;
			if (Enum.IsDefined(typeof(Opcode), (int)(bitRepresentation & GenerateMask(opcodeLength))))
				l_opcode = (Opcode)(bitRepresentation & GenerateMask(opcodeLength));

			return new Instruction(l_opcode, l_mode, l_indexing, l_accumulator, l_indexRegister, l_operand);
		}

		public static void PrintInstructionLayout(Instruction instr)
		{
			Console.WriteLine("Opcode:" + instr.opcode + " mode:" + instr.mode + " indexing:" + instr.indexing + " accumulator:" + instr.accumulator + " indexRegister:" + instr.indexRegister + " operand:" + instr.operand);
		}

		/// <summary>
		/// 
		/// Generates a mask of 1's of length numBits e.g. 0b11111
		/// 
		/// </summary>
		/// <param name="numBits"></param>
		/// <returns></returns>
		public static uint GenerateMask(int numBits)
		{
			uint power = 1;
			uint mask = 0;

			for (int i = 0; i < numBits; i++)
			{
				mask += power;
				power *= 2;
			}

			return mask;
		}

	}
}
