using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Computer
{
	class RAM
	{
		private uint[] registers = new uint[Instruction.maxAddressableLocation + 1];

		//MMU functionality


		/* offset for dynamic relocation */
		private uint relocationRegister = 0;

		/* Limit registers */
		private uint lowerLimitRegister = 0;
		private uint upperLimitRegister = Instruction.maxAddressableLocation;

		internal RAM()
		{

		}

		internal void LoadProgram(string location, uint relocationAddress)
		{
			List<uint> program = new();
			long amountOfBytes = new FileInfo(location).Length;

			SetRelocationRegister(relocationAddress);

			using (BinaryReader reader = new BinaryReader(File.Open(location, FileMode.Open)))
			{
				for (int b = 0; b < amountOfBytes / 4; b++)
				{
					program.Add(reader.ReadUInt32());
				}
			}

			Array.Copy(program.ToArray(), 0, registers, relocationAddress, program.Count);
		}

		internal void SetRelocationRegister(uint relocationAddress)
		{
			this.relocationRegister = relocationAddress;
		}

		internal uint Read(uint logicalAddress)
		{

			uint physicalAddress = logicalAddress + relocationRegister;

			if (physicalAddress < lowerLimitRegister || physicalAddress > upperLimitRegister)
			{
				//set interrupt flag
			}

			return registers[physicalAddress];
		}

		internal void Write(uint value, uint logicalAddress)
		{

			uint physicalAddress = logicalAddress + relocationRegister;

			if (physicalAddress < lowerLimitRegister || physicalAddress > upperLimitRegister)
			{
				//set interrupt flag

				return;
			}

			registers[physicalAddress] = value;
		}
	}
}
