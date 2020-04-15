using System;
using System.IO;
using System.Linq;

namespace GameDataWriter
{
	class SaveBuffer
	{
		const int GAMEDATASIZE = (4 * 8192);
		public byte[] save_p;
		public int Length;
		static UInt32 power_of_256(int power)
		{
			UInt32 result = 1;
			for (int i = 0; i < power; i++)
			{
				result *= 256;
			}
			return result;
		}
		public void WRITEUINT32(UInt32 content)
		{
			int factor = 0;
			for (int i = 0; i < sizeof(UInt32); i++)
			{
				save_p[Length++] = (byte)(content >> (8*factor));
				factor += 1;
			}
		}
		public void WRITEUINT16(UInt16 content)
		{
			int factor = 0;
			for (int i = 0; i < sizeof(UInt16); i++)
			{
				save_p[Length++] = (byte)(content >> (8 * factor));
				factor += 1;
			}
		}
		public void WRITEUINT8(byte content)
		{
			save_p[Length++] = content;
		}
		public UInt32 READUINT32()
		{
			UInt32 sum = 0;
			int size = sizeof(UInt32);
			for (int i = 0; i < size; i++)
			{
				sum += save_p[0] * power_of_256(i);
				save_p = save_p.Skip(1).ToArray();
			}
			Length -= size;
			return sum;
		}
		public UInt16 READUINT16()
		{
			UInt16 sum = 0;
			int size = sizeof(UInt16);
			for (int i = 0; i < size; i++)
			{
				sum += (UInt16)(save_p[0] * power_of_256(i));
				save_p = save_p.Skip(1).ToArray();
			}
			Length -= size;
			return sum;
		}
		public byte READUINT8()
		{
			byte sum = save_p[0];
			save_p = save_p.Skip(sizeof(byte)).ToArray();
			Length -= sizeof(byte);
			return sum;
		}
		public SaveBuffer()
		{
			save_p = new byte[GAMEDATASIZE];
			Length = 0;
		}
		public SaveBuffer(byte[] bytes)
		{
			save_p = bytes;
			Length = bytes.Length;
		}
	}

	class Record
	{
		public UInt32 score;
		public UInt32 time;
		public UInt16 rings;
	}
	class Mare
	{
		public UInt32 score;
		public UInt32 time;
		public byte grade;
	}
	class NightsRecord
	{
		public Mare[] mares;
		public byte nummares;
		public NightsRecord()
		{
			mares = new Mare[0];
		}
	}
	class Program
	{
		const int NUMMAPS = 1035;
		const byte MV_MAX = 63;
		const int MAXCONDITIONSETS = 128;
		const int MAXEMBLEMS = 512;
		const int MAXEXTRAEMBLEMS = 16;
		const int MAXUNLOCKABLES = 32;
		const int MAXSCORE = 99999990;
		const int GRADE_S = 6;
		static UInt32 totalplaytime = 0;
		static UInt32 modified = 0;
		static byte[] mapvisited = new byte[NUMMAPS];

		static byte[] emblemlocations = new byte[MAXEMBLEMS];
		static byte[] extraemblems = new byte[MAXEXTRAEMBLEMS];
		static byte[] unlockables = new byte[MAXUNLOCKABLES];
		static byte[] conditionSets = new byte[MAXCONDITIONSETS];

		static UInt32 timesBeaten = 0;
		static UInt32 timesBeatenWithEmeralds = 0;
		static UInt32 timesBeatenUltimate = 0;

		static Record[] mainrecords = new Record[NUMMAPS];
		static NightsRecord[] nightsrecords = new NightsRecord[NUMMAPS];

		static string gamedatafilename = "";
		static void G_SaveGameData()
		{
			int length;
			Int32 i, j;
			byte btemp;

			Int32 curmare;

			SaveBuffer saveBuffer = new SaveBuffer();

			// Version test
			saveBuffer.WRITEUINT32(0xFCAFE211);

			saveBuffer.WRITEUINT32(totalplaytime);

			btemp = (byte)modified;
			saveBuffer.WRITEUINT8(btemp);

			// TODO put another cipher on these things? meh, I don't care...
			for (i = 0; i < NUMMAPS; i++)
				saveBuffer.WRITEUINT8((byte)(mapvisited[i] & MV_MAX));

			// To save space, use one bit per collected/achieved/unlocked flag
			for (i = 0; i < MAXEMBLEMS;)
			{
				btemp = 0;
				for (j = 0; j < 8 && j + i < MAXEMBLEMS; ++j)
					btemp |= (byte)(emblemlocations[j + i] << j);
				saveBuffer.WRITEUINT8(btemp);
				i += j;
			}
			for (i = 0; i < MAXEXTRAEMBLEMS;)
			{
				btemp = 0;
				for (j = 0; j < 8 && j + i < MAXEXTRAEMBLEMS; ++j)
					btemp |= (byte)(extraemblems[j + i] << j);
				saveBuffer.WRITEUINT8(btemp);
				i += j;
			}
			for (i = 0; i < MAXUNLOCKABLES;)
			{
				btemp = 0;
				for (j = 0; j < 8 && j + i < MAXUNLOCKABLES; ++j)
					btemp |= (byte)(unlockables[j + i] << j);
				saveBuffer.WRITEUINT8(btemp);
				i += j;
			}
			for (i = 0; i < MAXCONDITIONSETS;)
			{
				btemp = 0;
				for (j = 0; j < 8 && j + i < MAXCONDITIONSETS; ++j)
					btemp |= (byte)(conditionSets[j + i] << j);
				saveBuffer.WRITEUINT8(btemp);
				i += j;
			}

			saveBuffer.WRITEUINT32(timesBeaten);
			saveBuffer.WRITEUINT32(timesBeatenWithEmeralds);
			saveBuffer.WRITEUINT32(timesBeatenUltimate);

			// Main records
			for (i = 0; i < NUMMAPS; i++)
			{
				if (mainrecords[i] != null)
				{
					saveBuffer.WRITEUINT32(mainrecords[i].score);
					saveBuffer.WRITEUINT32(mainrecords[i].time);
					saveBuffer.WRITEUINT16(mainrecords[i].rings);
				}
				else
				{
					saveBuffer.WRITEUINT32(0);
					saveBuffer.WRITEUINT32(0);
					saveBuffer.WRITEUINT16(0);
				}
				saveBuffer.WRITEUINT8(0); // compat
			}

			// NiGHTS records
			for (i = 0; i < NUMMAPS; i++)
			{
				if (nightsrecords[i] == null || nightsrecords[i].nummares == 0)
				{
					saveBuffer.WRITEUINT8(0);
					continue;
				}

				saveBuffer.WRITEUINT8(nightsrecords[i].nummares);

				for (curmare = 0; curmare < (nightsrecords[i].nummares + 1); ++curmare)
				{
					saveBuffer.WRITEUINT32(nightsrecords[i].mares[curmare].score);
					saveBuffer.WRITEUINT8(nightsrecords[i].mares[curmare].grade);
					saveBuffer.WRITEUINT32(nightsrecords[i].mares[curmare].time);
				}
			}
			length = saveBuffer.Length;

			byte[] bytes = new byte[length];
			bytes = saveBuffer.save_p.SkipLast(saveBuffer.save_p.Length - length).ToArray();
			using (BinaryWriter writer = new BinaryWriter(File.Open(gamedatafilename, FileMode.Create)))
			{
				writer.Write(bytes);
			}
		}
		static void G_ClearRecords()
		{
			Int16 i;
			for (i = 0; i < NUMMAPS; ++i)
			{
				if (mainrecords[i] != null)
				{
					mainrecords[i] = null;
				}
				if (nightsrecords[i] != null)
				{
					nightsrecords[i] = null;
				}
			}
		}
		static void M_ClearSecrets()
		{
			Int32 i;
			for (i = 0; i < mapvisited.Length; i++)
			{
				mapvisited[i] = 0;
			}

			for (i = 0; i < MAXEMBLEMS; ++i)
				emblemlocations[i] = 0;
			for (i = 0; i < MAXEXTRAEMBLEMS; ++i)
				extraemblems[i] = 0;
			for (i = 0; i < MAXUNLOCKABLES; ++i)
				unlockables[i] = 0;
			for (i = 0; i < MAXCONDITIONSETS; ++i)
				conditionSets[i] = 0;

			timesBeaten = timesBeatenWithEmeralds = timesBeatenUltimate = 0;
		}
		static void G_LoadGameData()
		{
			int length;
			Int32 i, j;
			byte rtemp;

			//For records
			UInt32 recscore;
			UInt32 rectime;
			UInt16 recrings;

			byte recmares;
			Int32 curmare;

			// Clear things so previously read gamedata doesn't transfer
			// to new gamedata
			G_ClearRecords(); // main and nights records
			M_ClearSecrets(); // emblems, unlocks, maps visited, etc
			totalplaytime = 0; // total play time (separate from all)
			byte[] bytes;
			try
			{
				bytes = File.ReadAllBytes(gamedatafilename);
			}
			catch (IOException e)
			{
				Console.WriteLine("{0}: {1}", e.GetType().Name, e.Message);
				return;
			}

			length = bytes.Length;
			if (length == 0) // Aw, no game data. Their loss!
				return;

			SaveBuffer save_p = new SaveBuffer(bytes);
			// Version check
			if (save_p.READUINT32() != 0xFCAFE211)
			{
				Console.WriteLine("Game data is from another version of SRB2.\nDelete {0} and try again.", gamedatafilename);
			}

			totalplaytime = save_p.READUINT32();

			modified = save_p.READUINT8();

			// Aha! Someone's been screwing with the save file!
			if ((modified == 1))
				Console.WriteLine("Warning, the game data is modified. If this gamedata doesn't belong to a mod, you're screwed.");
			else if (modified != 1 && modified != 0)
				goto datacorrupt;

			// TODO put another cipher on these things? meh, I don't care...
			for (i = 0; i < NUMMAPS; i++)
				if ((mapvisited[i] = save_p.READUINT8()) > MV_MAX)
					goto datacorrupt;

			// To save space, use one bit per collected/achieved/unlocked flag
			for (i = 0; i < MAXEMBLEMS;)
			{
				rtemp = save_p.READUINT8();
				for (j = 0; j < 8 && j + i < MAXEMBLEMS; ++j)
					emblemlocations[j + i] = (byte)((rtemp >> j) & 1);
				i += j;
			}
			for (i = 0; i < MAXEXTRAEMBLEMS;)
			{
				rtemp = save_p.READUINT8();
				for (j = 0; j < 8 && j + i < MAXEXTRAEMBLEMS; ++j)
					extraemblems[j + i] = (byte)((rtemp >> j) & 1);
				i += j;
			}
			for (i = 0; i < MAXUNLOCKABLES;)
			{
				rtemp = save_p.READUINT8();
				for (j = 0; j < 8 && j + i < MAXUNLOCKABLES; ++j)
					unlockables[j + i] = (byte)((rtemp >> j) & 1);
				i += j;
			}
			for (i = 0; i < MAXCONDITIONSETS;)
			{
				rtemp = save_p.READUINT8();
				for (j = 0; j < 8 && j + i < MAXCONDITIONSETS; ++j)
					conditionSets[j + i] = (byte)((rtemp >> j) & 1);
				i += j;
			}

			timesBeaten = save_p.READUINT32();
			timesBeatenWithEmeralds = save_p.READUINT32();
			timesBeatenUltimate = save_p.READUINT32();

			// Main records
			for (i = 0; i < NUMMAPS; ++i)
			{
				recscore = save_p.READUINT32();
				rectime = save_p.READUINT32();
				recrings = save_p.READUINT16();
				//save_p++; // compat
				save_p.READUINT8();

				if (recrings > 10000 || recscore > MAXSCORE)
					goto datacorrupt;

				if (mainrecords[i] == null)
				{
					mainrecords[i] = new Record();
				}
				mainrecords[i].score = recscore;
				mainrecords[i].time = rectime;
				mainrecords[i].rings = recrings;
			}

			// Nights records
			for (i = 0; i < NUMMAPS; ++i)
			{

				if (nightsrecords[i] == null)
				{
					nightsrecords[i] = new NightsRecord();
				}
				if ((recmares = save_p.READUINT8()) == 0)
					continue;


				nightsrecords[i].mares = new Mare[recmares+1];

				for (curmare = 0; curmare < (recmares + 1); ++curmare)
				{
					if (nightsrecords[i].mares[curmare] == null)
					{
						nightsrecords[i].mares[curmare] = new Mare();
					}
					nightsrecords[i].mares[curmare].score = save_p.READUINT32();
					nightsrecords[i].mares[curmare].grade = save_p.READUINT8();
					nightsrecords[i].mares[curmare].time = save_p.READUINT32();

					if (nightsrecords[i].mares[curmare].grade > GRADE_S)
						goto datacorrupt;
				}

				nightsrecords[i].nummares = recmares;
			}

			save_p = null;
			return;

		// Landing point for corrupt gamedata
		datacorrupt:
			{
				Console.WriteLine("Corrupt game data file.\nDelete {0} and try again.", gamedatafilename);
			}
		}
		static void CommandHelp(string[] parameters)
		{
			foreach (Command command in commands)
			{
				Console.WriteLine("{0}: {1}", string.Join("/",command.CommandNames), command.HelpString);
			}
		}
		static void CommandExit(string[] parameters)
		{
			Environment.Exit(0);
		}
		static void CommandSave(string[] parameters)
		{
			G_SaveGameData();
		}
		static void CommandSaveAs(string[] parameters)
		{
			gamedatafilename = parameters[0];
			G_SaveGameData();
		}
		static void CommandLoad(string[] parameters)
		{
			gamedatafilename = parameters[0];
			G_LoadGameData();
		}
		static int CountTrues(byte[] bytes)
		{
			int sum = 0;
			foreach (byte b in bytes)
			{
				if (b == 1)
				{
					sum++;
				}
			}
			return sum;
		}
		static void PrintArrayOnes(byte[] bytes)
		{
			for(int i = 0; i < bytes.Length; i++)
			{
				byte b = bytes[i];
				if (b == 1)
				{
					Console.Write("{0} ", i+1);
				}
			}
			Console.WriteLine();
		}
		static void CommandEmblemInfo(string[] parameters)
		{
			Console.WriteLine("Total emblems: {0}", CountTrues(emblemlocations) + CountTrues(extraemblems));
			Console.WriteLine("emblemlocations:");
			PrintArrayOnes(emblemlocations);
			Console.WriteLine("Total: {0}", CountTrues(emblemlocations));
			Console.WriteLine("extraemblems:");
			PrintArrayOnes(extraemblems);
			Console.WriteLine("Total: {0}", CountTrues(extraemblems));
		}
		static void CommandRecordInfo(string[] parameters)
		{
			Console.WriteLine("mainrecords/nightsrecords: ");
			for (int i = 0; i < NUMMAPS; i++)
			{
				Record record = mainrecords[i];
				NightsRecord nightsRecord = nightsrecords[i];
				if (record != null && (record.score != 0 || record.time != 0 || record.rings != 0))
				{
					Console.WriteLine("Map {0}: Score: {1}, Time: {2}, Rings: {3}", i+1, record.score, record.time, record.rings);
				}
				if (nightsrecords[i] != null && nightsrecords[i].nummares != 0)
				{
					Console.Write("Map: {0}: ", i+1);
					for (int j = 0; j < nightsRecord.nummares; j++)
					{
						Mare mare = nightsRecord.mares[j];
						Console.Write("Mare {0}: Score: {1}, Grade: {2}, Time: {3}, ", j, mare.score, mare.grade, mare.time);
					}
					Console.WriteLine();
				}
			}
		}
		unsafe class Variable
		{
			private UInt32 *Value;
			public string Name;
			public Variable(UInt32* v, string n)
			{
				Value = v;
				Name = n;
			} 
			public UInt32 GetValue()
			{
				return *Value;
			}
			public void SetValue(UInt32 value)
			{
				*Value = value;
			}
		}
		static Variable[] variables;
		unsafe static void Init()
		{
			fixed (UInt32* timesBeatenP = &timesBeaten, 
				timesBeatenWithEmeraldsP = &timesBeatenWithEmeralds,  
				timesBeatenUltimateP = &timesBeatenUltimate,
				modifiedP = &modified,
				totalplaytimeP = &totalplaytime)
			{

				variables = new Variable[]
				{
					new Variable(timesBeatenP, "timesBeaten"),
					new Variable(timesBeatenWithEmeraldsP, "timesBeatenWithEmeralds"),
					new Variable(timesBeatenUltimateP, "timesBeatenUltimate"),
					new Variable(modifiedP, "modified"),
					new Variable(totalplaytimeP, "totalplaytime")
				};
			}
		}
		static void CommandMiscInfo(string[] parameters)
		{
			Console.WriteLine("unlockables:");
			PrintArrayOnes(unlockables);
			Console.WriteLine("Total: {0}", CountTrues(unlockables));
			Console.WriteLine("conditionSets:");
			PrintArrayOnes(conditionSets);
			Console.WriteLine("Total: {0}", CountTrues(conditionSets));
			Console.WriteLine("mapvisited:");
			PrintArrayOnes(mapvisited);
			Console.WriteLine("Total: {0}", CountTrues(mapvisited));
		}
		static void CommandVariableInfo(string[] parameters)
		{
			foreach (Variable variable in variables)
			{
				Console.WriteLine("{0}: {1}", variable.Name, variable.GetValue());
			}
		}
		static void CommandSetVariable(string[] parameters)
		{
			bool found = false;
			foreach (Variable variable in variables)
			{
				if(variable.Name.ToUpper() == parameters[0].ToUpper())
				{
					try
					{
						variable.SetValue(UInt32.Parse(parameters[1]));
						found = true;
						Console.WriteLine("{0}: {1}", variable.Name, variable.GetValue());
					}
					catch (Exception e)
					{
						Console.WriteLine("{0}: {1}", e.Data.GetType().ToString(), e.Message);
					}
				}
			}
			if (!found)
			{
				Console.WriteLine("Couldn't find a variable {0}", parameters[0]);
			}
		}
		static void CommandSetRecord(string[] parameters)
		{
			try
			{
				int map = int.Parse(parameters[0]) - 1;
				if (map < 0 || map >= NUMMAPS)
				{
					Console.WriteLine("Couldn't find the map {0}. Please select a map between 1 and {1}", map + 1, NUMMAPS);
					return;
				}
				mainrecords[map] = new Record();
				mainrecords[map].score = UInt32.Parse(parameters[1]);
				mainrecords[map].time = UInt32.Parse(parameters[2]);
				mainrecords[map].rings = UInt16.Parse(parameters[3]);
			}
			catch (FormatException e)
			{
				Console.WriteLine("{0}: {1}", e.Data.GetType().ToString(), e.Message);
			}
		}
		static bool GetByte(string message, ref byte returnValue)
		{
			UInt32 tempValue = 0;
			bool valueTaken = GetUInt32(message, ref tempValue);
			returnValue = (byte)tempValue;
			return valueTaken;
		}
		static bool GetUInt32(string message, ref UInt32 returnValue)
		{
			bool trying = true;
			while (trying)
			{
				try
				{
					Console.Write(message);
					string line = Console.ReadLine();
					if (line == "")
					{
						return false;
					}
					UInt32 value = UInt32.Parse(line);
					returnValue = value;
					trying = false;
				}
				catch (Exception e)
				{
					Console.WriteLine("{0}: {1}", e.Data.GetType().ToString(), e.Message);
					Console.WriteLine("Insert empty line to skip.");
				}
			}
			return true;
		}
		static void CommandSetNights(string[] parameters)
		{
			try
			{
				int map = int.Parse(parameters[0]) - 1;
				if (map < 0 || map >= NUMMAPS)
				{
					Console.WriteLine("Couldn't find the map {0}. Please select a map between 1 and {1}", map + 1, NUMMAPS);
					return;
				}
				byte numMares = byte.Parse(parameters[1]);
				nightsrecords[map] = new NightsRecord();
				nightsrecords[map].nummares = (byte)(numMares);
				nightsrecords[map].mares = new Mare[numMares + 1];

				for (byte i = 0; i < numMares; i++)
				{
					nightsrecords[map].mares[i] = new Mare();
					Console.WriteLine("Mare {0}:", i);

					GetUInt32("Score: ", ref nightsrecords[map].mares[i].score);
					GetByte("Grade (0-6): ", ref nightsrecords[map].mares[i].grade);
					GetUInt32("Time: ", ref nightsrecords[map].mares[i].time);
				}
				Mare emptyMare = new Mare();
				emptyMare.score = 0;
				emptyMare.grade = 0;
				emptyMare.time = 0;
				nightsrecords[map].mares[numMares] = emptyMare;
			}
			catch (FormatException e)
			{
				Console.WriteLine("{0}: {1}", e.Data.GetType().ToString(), e.Message);
			}
		}
		static void TryUnlock(string[] parameters, ref byte[] list, string name, UInt32 maxId, byte maxValue)
		{
			try
			{
				int id = int.Parse(parameters[0]) - 1;
				if (id < 0 || id >= maxId)
				{
					Console.WriteLine("Couldn't find the {0} {1}. Please select a value between 1 and {2}", name, id + 1, maxId);
					return;
				}
				byte value = byte.Parse(parameters[1]);
				if (value <= maxValue)
				{
					list[id] = value;
				}
				else
				{
					Console.WriteLine("Invalid value {0} for {1}. Accepted values are between 0 and {2}", value, name, maxValue);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("{0}: {1}", e.Data.GetType().ToString(), e.Message);
			}
		}
		static void CommandSetEmblem(string[] parameters)
		{
			TryUnlock(parameters, ref emblemlocations, "emblem", MAXEMBLEMS, 1);
		}
		static void CommandSetExtraEmblem(string[] parameters)
		{
			TryUnlock(parameters, ref extraemblems, "extra emblem", MAXEXTRAEMBLEMS, 1);
		}
		static void CommandSetUnlockable(string[] parameters)
		{
			TryUnlock(parameters, ref unlockables, "unlockable ", MAXUNLOCKABLES, 1);
		}
		static void CommandSetConditionSet(string[] parameters)
		{
			TryUnlock(parameters, ref conditionSets, "condition set", MAXCONDITIONSETS, 1);
		}
		static void CommandSetMapVisited(string[] parameters)
		{
			TryUnlock(parameters, ref mapvisited, "map visited", NUMMAPS, MV_MAX);
		}
		static void SetArray(ref byte[] array, byte value)
		{
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = value;
			}
		}
		static void CommandClearAll(string[] parameters)
		{
			G_ClearRecords(); // main and nights records
			M_ClearSecrets(); // emblems, unlocks, maps visited, etc
			totalplaytime = 0; // total play time (separate from all)
			modified = 0;
			SetArray(ref mapvisited, 0);
			SetArray(ref emblemlocations, 0);
			SetArray(ref extraemblems, 0);
			SetArray(ref unlockables, 0);
			SetArray(ref conditionSets, 0);

			timesBeaten = 0;
			timesBeatenWithEmeralds = 0;
			timesBeatenUltimate = 0;

			for (int i = 0; i < NUMMAPS; ++i)
			{
				//if (recrings > 10000 || recscore > MAXSCORE)

				if (mainrecords[i] == null)
				{
					mainrecords[i] = new Record();
				}
				mainrecords[i].score = 0;
				mainrecords[i].time = 0;
				mainrecords[i].rings = 0;
			}
			for (int i = 0; i < NUMMAPS; ++i)
			{

				if (nightsrecords[i] == null)
				{
					nightsrecords[i] = new NightsRecord();
				}

				nightsrecords[i].mares = new Mare[0];
			}
		}
		static void CommandUnlockAll(string[] parameters)
		{
			totalplaytime = 0; // total play time (separate from all)
			modified = 0;
			SetArray(ref mapvisited, MV_MAX);
			SetArray(ref emblemlocations, 1);
			SetArray(ref extraemblems, 1);
			SetArray(ref unlockables, 1);
			SetArray(ref conditionSets, 1);

			timesBeaten = UInt32.MaxValue;
			timesBeatenWithEmeralds = UInt32.MaxValue;
			timesBeatenUltimate = UInt32.MaxValue;

			for (int i = 0; i < NUMMAPS; ++i)
			{
				if (mainrecords[i] == null)
				{
					mainrecords[i] = new Record();
				}
				mainrecords[i].score = MAXSCORE;
				mainrecords[i].time = 1;
				mainrecords[i].rings = 10000;
			}
			// Let's assume there aren't any nights maps after 200
			for (int i = 0; i < 200; ++i)
			{

				if (nightsrecords[i] == null)
				{
					nightsrecords[i] = new NightsRecord();
				}
				byte nummares = 4; // 4 at most
				nightsrecords[i].mares = new Mare[nummares + 1];

				for (int curmare = 0; curmare < (nummares + 1); ++curmare)
				{
					Mare mare = new Mare();
					mare.score = MAXSCORE;
					mare.grade = GRADE_S;
					mare.time = 1;
					nightsrecords[i].mares[curmare] = mare;
				}
				nightsrecords[i].nummares = nummares;
			}
		}
		static void CommandInfo(string[] parameters)
		{
			CommandEmblemInfo(null);
			CommandMiscInfo(null);
			CommandRecordInfo(null);
			CommandVariableInfo(null);
		}
		struct Command
		{
			public string[] CommandNames;
			public int NumParameters;
			public string HelpString;
			public Action<string[]> Function;
			public Command(string[] c, int n, string h, Action<string[]> f)
			{
				CommandNames = c;
				NumParameters = n;
				HelpString = h;
				Function = f;
			}
		}
		static Command[] commands = new Command[]{
			new Command(new string[]{"LOAD"},1,"Parameters: <gamedata's path>. Loads the gamedata", CommandLoad),
			new Command(new string[]{"SAVE"},0,"Saves the gamedata", CommandSave),
			new Command(new string[]{"SAVEAS"},1,"Parameters: <gamedata's new path>. Saves the gamedata", CommandSaveAs),
			new Command(new string[]{"EMBLEMINFO"},0,"Prints the emblem information", CommandEmblemInfo),
			new Command(new string[]{"MISCINFO"},0,"Prints the record information", CommandMiscInfo),
			new Command(new string[]{"VARIABLEINFO"},0,"Prints the variable information", CommandVariableInfo),
			new Command(new string[]{"RECORDINFO"},0,"Prints the record information", CommandRecordInfo),
			new Command(new string[]{"INFO"},0,"Prints the emblem, misc and record information", CommandInfo),
			new Command(new string[]{"SETVARIABLE","SET"},2,"Parameters: <variable> <value>. Sets a value to the variable", CommandSetVariable),
			new Command(new string[]{"SETRECORD","RECORD"},4,"Parameters: <map> <score> <time> <rings>. Sets a record to the map.", CommandSetRecord),
			new Command(new string[]{"SETNIGHTS","NIGHTS"},2,"Parameters: <variable> <mares>. Sets a nights record to the map.", CommandSetNights),
			new Command(new string[]{"SETEMBLEM","EMBLEM"},2,"Parameters: <ID> <value>. Sets a value for the emblem (0 or 1)", CommandSetEmblem),
			new Command(new string[]{"SETEXTRAEMBLEM","EXTRAEMBLEM"},2,"Parameters: <ID> <value>. Sets a value for the extra emblem (0 or 1)", CommandSetExtraEmblem),
			new Command(new string[]{"SETUNLOCKABLE","UNLOCKABLE"},2,"Parameters: <ID> <value>. Sets a value for the unlocable (0 or 1)", CommandSetUnlockable),
			new Command(new string[]{"SETCONDITIONSET","CONDITIONSET"},2,"Parameters: <ID> <value>. Sets a value for the conditionset (0 or 1)", CommandSetConditionSet),
			new Command(new string[]{"SETMAPVISITED","MAPVISITED"},2,"Parameters: <ID> <value>. Sets a value for the map visited (0 or 1)", CommandSetMapVisited),
			new Command(new string[]{"CLEARALL","CLEAR"},0,"Sets all conditions to best values (0 or 1)", CommandClearAll),
			new Command(new string[]{"UNLOCKALL"},0,"Clears all progress (0 or 1)", CommandUnlockAll),
			new Command(new string[]{"HELP"}, 0, "Displays all the parameters", CommandHelp),
			new Command(new string[]{"EXIT","QUIT"}, 0, "Exits the application", CommandExit)
		};

		static void Main(string[] args)
		{
			Init();
			G_ClearRecords();
			M_ClearSecrets();

			Console.WriteLine("Please give your gamedata's path! (Empty line to skip)");
			string firstInput = Console.ReadLine();
			if (firstInput != "")
			{
				gamedatafilename = firstInput;
				G_LoadGameData();
			}
			while (true)
			{
				Console.Write("> ");
				string input = Console.ReadLine();
				string[] parts = input.Split(' ',StringSplitOptions.RemoveEmptyEntries);
				if(parts.Length == 0)
				{
					continue;
				}

				bool commandFound = false;
				foreach (Command command in commands)
				{
					if (command.CommandNames.Contains(parts[0].ToUpper()))
					{
						commandFound = true;
						if (parts.Length - 1 == command.NumParameters)
						{
							command.Function(parts.Skip(1).ToArray());
						}
						else
						{
							Console.WriteLine("Error, wrong amount of parameters. <Help> for more info.");
						}
						break;
					}
				}
				if( !commandFound )
				{
					Console.WriteLine("Error, unknown command {0}. <Help> for more info.", parts[0]);
				}
			}
		}
	}
}