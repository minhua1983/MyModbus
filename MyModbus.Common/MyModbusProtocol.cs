namespace MyModbus.Common
{
    public class MyModbusProtocol
    {
        static void BuildMBAP(byte slaveId, byte[] bytes, byte[] lengthBytes)
        {
            // default transaction id
            bytes[0] = 0x00;
            bytes[1] = 0x00;

            // protocol id
            bytes[2] = 0x00;
            bytes[3] = 0x00;

            // bytes length (unit id byte + PDU bytes)
            bytes[4] = lengthBytes[0];
            bytes[5] = lengthBytes[1];

            // unit id
            bytes[6] = slaveId;
        }

        public static byte[] BuildReadInputCoils(byte slaveId, ushort start, ushort quantity)
        {
            var lengthBytes = MyDataConverter.GetProtocolBytesFromUInt16((ushort)6);
            var startBytes = MyDataConverter.GetProtocolBytesFromUInt16(start);
            var quantityBytes = MyDataConverter.GetProtocolBytesFromUInt16(quantity);

            byte[] bytes = new byte[12];

            // build MBAP header bytes
            BuildMBAP(slaveId, bytes, lengthBytes);

            // function code
            bytes[7] = 0x02;

            // start
            bytes[8] = startBytes[0];
            bytes[9] = startBytes[1];

            // quantity
            bytes[10] = quantityBytes[0];
            bytes[11] = quantityBytes[1];

            return bytes;
        }

        public static byte[] BuildReadOutputCoils(byte slaveId, ushort start, ushort quantity)
        {
            var lengthBytes = MyDataConverter.GetProtocolBytesFromUInt16((ushort)6);
            var startBytes = MyDataConverter.GetProtocolBytesFromUInt16(start);
            var quantityBytes = MyDataConverter.GetProtocolBytesFromUInt16(quantity);

            byte[] bytes = new byte[12];

            // build MBAP header bytes
            BuildMBAP(slaveId, bytes, lengthBytes);

            // function code
            bytes[7] = 0x01;

            // start
            bytes[8] = startBytes[0];
            bytes[9] = startBytes[1];

            // quantity
            bytes[10] = quantityBytes[0];
            bytes[11] = quantityBytes[1];

            return bytes;
        }

        public static byte[] BuildReadInputRegisters(byte slaveId, ushort start, ushort quantity)
        {
            var lengthBytes = MyDataConverter.GetProtocolBytesFromUInt16((ushort)6);
            var startBytes = MyDataConverter.GetProtocolBytesFromUInt16(start);
            var quantityBytes = MyDataConverter.GetProtocolBytesFromUInt16(quantity);

            byte[] bytes = new byte[12];

            // build MBAP header bytes
            BuildMBAP(slaveId, bytes, lengthBytes);

            // function code
            bytes[7] = 0x04;

            // start
            bytes[8] = startBytes[0];
            bytes[9] = startBytes[1];

            // quantity
            bytes[10] = quantityBytes[0];
            bytes[11] = quantityBytes[1];

            return bytes;
        }

        public static byte[] BuildReadOutputRegisters(byte slaveId, ushort start, ushort quantity)
        {
            var lengthBytes = MyDataConverter.GetProtocolBytesFromUInt16((ushort)6);
            var startBytes = MyDataConverter.GetProtocolBytesFromUInt16(start);
            var quantityBytes = MyDataConverter.GetProtocolBytesFromUInt16(quantity);

            byte[] bytes = new byte[12];

            // build MBAP header bytes
            BuildMBAP(slaveId, bytes, lengthBytes);

            // function code
            bytes[7] = 0x03;

            // start
            bytes[8] = startBytes[0];
            bytes[9] = startBytes[1];

            // quantity
            bytes[10] = quantityBytes[0];
            bytes[11] = quantityBytes[1];

            return bytes;
        }

        public static byte[] BuildWriteSingleCoil(byte slaveId, ushort address, bool value)
        {
            var lengthBytes = MyDataConverter.GetProtocolBytesFromUInt16((ushort)6);
            var addressBytes = MyDataConverter.GetProtocolBytesFromUInt16(address);

            byte[] bytes = new byte[12];

            // build MBAP header bytes
            BuildMBAP(slaveId, bytes, lengthBytes);

            // function code
            bytes[7] = 0x05;

            // address
            bytes[8] = addressBytes[0];
            bytes[9] = addressBytes[1];

            // value
            bytes[10] = value ? (byte)0xFF : (byte)0x00;
            bytes[11] = 0x00;

            return bytes;
        }

        public static byte[] BuildWriteSingleRegister(byte slaveId, ushort address, byte[] valueBytes)
        {
            var lengthBytes = MyDataConverter.GetProtocolBytesFromUInt16((ushort)6);
            var addressBytes = MyDataConverter.GetProtocolBytesFromUInt16(address);

            byte[] bytes = new byte[12];

            // build MBAP header bytes
            BuildMBAP(slaveId, bytes, lengthBytes);

            // function code
            bytes[7] = 0x06;

            // address
            bytes[8] = addressBytes[0];
            bytes[9] = addressBytes[1];

            // value
            bytes[10] = valueBytes[0];
            bytes[11] = valueBytes[1];

            return bytes;
        }

        public static byte[] BuildWriteMultiCoils(byte slaveId, ushort start, ushort quantity, byte valueBytesLength, byte[] valueBytes)
        {
            var length = 6 + 1 + valueBytesLength;
            var lengthBytes = MyDataConverter.GetProtocolBytesFromUInt16((ushort)(length));
            var startBytes = MyDataConverter.GetProtocolBytesFromUInt16(start);
            var quantityBytes = MyDataConverter.GetProtocolBytesFromUInt16(quantity);

            byte[] bytes = new byte[6 + length];

            // build MBAP header bytes
            BuildMBAP(slaveId, bytes, lengthBytes);

            // function code
            bytes[7] = 0x0F;

            // start
            bytes[8] = startBytes[0];
            bytes[9] = startBytes[1];

            // quantity
            bytes[10] = quantityBytes[0];
            bytes[11] = quantityBytes[1];

            // valueBytesLength
            bytes[12] = valueBytesLength;

            // valueBytes
            for (byte i = 0; i < valueBytesLength; i++)
            {
                bytes[13 + i] = valueBytes[i];
            }

            return bytes;
        }

        public static byte[] BuildWriteMultiRegisters(byte slaveId, ushort start, ushort quantity, byte valueBytesLength, byte[] valueBytes)
        {
            var length = 6 + 1 + valueBytesLength;
            var lengthBytes = MyDataConverter.GetProtocolBytesFromUInt16((ushort)(length));
            var startBytes = MyDataConverter.GetProtocolBytesFromUInt16(start);
            var quantityBytes = MyDataConverter.GetProtocolBytesFromUInt16(quantity);

            byte[] bytes = new byte[6 + length];

            // build MBAP header bytes
            BuildMBAP(slaveId, bytes, lengthBytes);

            // function code
            bytes[7] = 0x10;

            // start
            bytes[8] = startBytes[0];
            bytes[9] = startBytes[1];

            // quantity
            bytes[10] = quantityBytes[0];
            bytes[11] = quantityBytes[1];

            // valueBytesLength
            bytes[12] = valueBytesLength;

            // valueBytes
            for (byte i = 0; i < valueBytesLength; i++)
            {
                bytes[13 + i] = valueBytes[i];
            }

            return bytes;
        }
    }
}

public enum FunctionCode
{
    FC01 = 1,
    FC02 = 2,
    FC03 = 3,
    FC04 = 4,
    FC05 = 5,
    FC06 = 6,
    FC0F = 15,
    FC10 = 16,
}
