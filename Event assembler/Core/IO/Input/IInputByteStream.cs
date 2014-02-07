using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nintenlord.Event_Assembler.Core.Code.Language.Types.IntegerRepresentations;

namespace Nintenlord.Event_Assembler.Core.IO.Input
{
    public interface IInputByteStream
    {
        int Offset { get; set; }
        int Length { get; }
        int BytesLeft { get; }

        byte[] ReadBytes(int count);
        short ReadInt16();
        ushort ReadUInt16();
        int ReadInt32();
        uint ReadUInt32();

        byte[] PeekBytes(int count);
        short PeekInt16();
        ushort PeekUInt16();
        int PeekInt32();
        uint PeekUInt32();
    }
}
