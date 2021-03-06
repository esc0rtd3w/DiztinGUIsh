﻿using System;
using System.Collections.Generic;
using System.Linq;
using Diz.Core.export;
using Diz.Core.model;
using Diz.Core.util;
using IX.Observable;
using Xunit;

namespace Diz.Test
{
    public sealed class LogCreatorTests
    {
        public class ParsedOutput
        {
            protected bool Equals(ParsedOutput other)
            {
                return Label == other.Label && Instr == other.Instr && Pc == other.Pc && Rawbytes == other.Rawbytes && Ia == other.Ia && Realcomment == other.Realcomment;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((ParsedOutput) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = Label.GetHashCode();
                    hashCode = (hashCode * 397) ^ Instr.GetHashCode();
                    hashCode = (hashCode * 397) ^ Pc.GetHashCode();
                    hashCode = (hashCode * 397) ^ Rawbytes.GetHashCode();
                    hashCode = (hashCode * 397) ^ Ia.GetHashCode();
                    hashCode = (hashCode * 397) ^ Realcomment.GetHashCode();
                    return hashCode;
                }
            }

            public string Label;
            public string Instr;

            public string Pc;
            public string Rawbytes;
            public string Ia;

            public string Realcomment;
        }

        public static ParsedOutput ParseLine(string line)
        {
            if (line == "")
                return null;

            var output = new ParsedOutput();

            var split = line.Split(new[] {';'}, 2,options:StringSplitOptions.None);
            var main = split[0].Trim(); 
            var comment = split[1].Trim();

            var csplit = comment.Split(new[] { '|' }, 3, options: StringSplitOptions.None);
            output.Pc = csplit[0].Trim();
            output.Rawbytes = csplit[1].Trim();

            var iasplit = csplit[2].Split(new[] { ';' }, 2, options: StringSplitOptions.None);
            output.Ia = iasplit[0].Trim();
            output.Realcomment = iasplit[1].Trim();

            var msplit = main.Split(new[] { ':' }, 2, options: StringSplitOptions.None);
            var m1 = msplit[0].Trim();
            var m2  = msplit.Length > 1 ? msplit[1].Trim() : "";

            if (m2 != "")
            {
                output.Label = m1;
                output.Instr = m2;
            }
            else
            {
                output.Label = "";
                output.Instr = m1;
            }

            return output;
        }

        public List<ParsedOutput> ParseAll(string lines) =>
            lines.Split(new[] {'\n'})
                .Select(line => ParseLine(line.Trim()))
                .ToList();

        [Fact]
        public void TestAFewLines()
        {
            var expectedRaw =
                //          label:       instructions                         ;PC    |rawbytes|ia
                "                        lorom                                ;      |        |      ;  \r\n" +
                "                                                             ;      |        |      ;  \r\n" +
                "                                                             ;      |        |      ;  \r\n" +
                "                        ORG $808000                          ;      |        |      ;  \r\n" +
                "                                                             ;      |        |      ;  \r\n" +
                "           CODE_808000: LDA.W Test_Data,X                    ;808000|BD5B80  |80805B;  \r\n" +
                "                        STA.W $0100,X                        ;808003|9D0001  |800100;  \r\n" +
                "           Test22:      DEX                                  ;808006|CA      |      ;  \r\n" +
                "                        BPL CODE_808000                      ;808007|10F7    |808000;  \r\n" +
                "                                                             ;      |        |      ;  \r\n" +
                "                        Test_Data = $80805B                  ;      |        |      ;  \r\n";

            var expectedOut = ParseAll(expectedRaw);

            var inputData = new Data
            {
                Labels = new ObservableDictionary<int, Label>
                {
                    {0x808000 + 0x06, new Label {Name = "Test22"}},
                    {0x808000 + 0x5B, new Label {Name = "Test_Data", Comment = "Pretty cool huh?"}},
                    // the CODE_XXXXXX labels are autogenerated
                },
                RomMapMode = RomMapMode.LoRom,
                RomSpeed = RomSpeed.FastRom,
                RomBytes =
                {
                    // --------------------------
                    // highlighting a particular section here
                    // we will use this for unit tests as well.

                    // CODE_808000: LDA.W Test_Data,X
                    new RomByte {Rom = 0xBD, TypeFlag = Data.FlagType.Opcode, MFlag = true, Point = Data.InOutPoint.InPoint, DataBank = 0x80, DirectPage = 0x2100},
                    new RomByte {Rom = 0x5B, TypeFlag = Data.FlagType.Operand, DataBank = 0x80, DirectPage = 0x2100}, // Test_Data
                    new RomByte {Rom = 0x80, TypeFlag = Data.FlagType.Operand, DataBank = 0x80, DirectPage = 0x2100}, // Test_Data
                
                    // STA.W $0100,X
                    new RomByte {Rom = 0x9D, TypeFlag = Data.FlagType.Opcode, MFlag = true, DataBank = 0x80, DirectPage = 0x2100},
                    new RomByte {Rom = 0x00, TypeFlag = Data.FlagType.Operand, DataBank = 0x80, DirectPage = 0x2100},
                    new RomByte {Rom = 0x01, TypeFlag = Data.FlagType.Operand, DataBank = 0x80, DirectPage = 0x2100},
                
                    // DEX
                    new RomByte {Rom = 0xCA, TypeFlag = Data.FlagType.Opcode, MFlag = true, DataBank = 0x80, DirectPage = 0x2100},

                    // BPL CODE_808000
                    new RomByte {Rom = 0x10, TypeFlag = Data.FlagType.Opcode, MFlag = true, Point = Data.InOutPoint.OutPoint, DataBank = 0x80, DirectPage = 0x2100},
                    new RomByte {Rom = 0xF7, TypeFlag = Data.FlagType.Operand, DataBank = 0x80, DirectPage = 0x2100},
                
                    // ------------------------------------
                }
            };

            var settings = new LogWriterSettings();
            settings.SetDefaults();
            settings.OutputToString = true;
            settings.Structure = LogCreator.FormatStructure.SingleFile;

            var lc = new LogCreator()
            {
                Data=inputData, 
                Settings = settings,
            };

            var result = lc.CreateLog();

            Assert.True(result.LogCreator != null);
            Assert.True(result.OutputStr != null);
            Assert.True(result.ErrorCount == 0);

            var actualOut = ParseAll(result.OutputStr);

            Assert.Equal(expectedOut.Count, actualOut.Count);

            for (var i = 0; i < expectedOut.Count; ++i)
            {
                Assert.Equal(expectedOut[i], actualOut[i]);
            }

            Assert.True(expectedOut.SequenceEqual(actualOut));
        }
    }
}