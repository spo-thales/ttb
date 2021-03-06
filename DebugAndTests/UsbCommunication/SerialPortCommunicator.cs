﻿using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using Spo.ToolsTestsBenchmarks.Common.Common.Helpers;

namespace Spo.ToolsTestsBenchmarks.DebugAndTests.SerialPortCommunication
{
    public class SerialPortCommunicator : IDisposable
    {
        private const int MaxComBaudRate = 128000;
        private const int DefaultBaudRate = 9600;
        private static readonly object Locker = new object();
        private static SerialPort _serialPort;
        private static SerialPortCommunicator _serialPortCommunicator;

        private SerialPortCommunicator(string portName, int baudRate)
        {
            _serialPort = new SerialPort(portName, baudRate);
            _serialPort.Encoding = Encoding.Unicode;

            // Set the read/write timeouts
            _serialPort.ReadTimeout = 500;
            _serialPort.WriteTimeout = 500;
            _serialPort.Open();
        }

        public static SerialPortCommunicator OpenConnection(string portName, int baudRate = DefaultBaudRate)
        {
            if (string.IsNullOrWhiteSpace(portName))
            {
                throw new ArgumentNullException("portName");
            }
            if (baudRate < 0 || baudRate > MaxComBaudRate)
            {
                throw new ArgumentOutOfRangeException("baudRate");
            }

            lock (Locker)
            {
                if (_serialPortCommunicator == null)
                {
                    try
                    {
                        _serialPortCommunicator = new SerialPortCommunicator(portName, baudRate);
                    }
                    catch (IOException e)
                    {
                        _serialPortCommunicator = null;
                    }
                }

                return _serialPortCommunicator;
            }
        }

        public void Write(string messageToWrite)
        {
            if (string.IsNullOrWhiteSpace(messageToWrite))
            {
                throw new ArgumentNullException(nameof(messageToWrite));
            }

            if (_serialPort == null)
            {
                return;
            }

            byte[] binaryMessage = EncodingHelper.ToBytes(messageToWrite);
            if (binaryMessage != null && binaryMessage.Length > 0)
            {
                try
                {
                    _serialPort.Write(binaryMessage, 0, binaryMessage.Length);
                }
                catch (InvalidOperationException)
                {
                }
                catch (ArgumentException)
                {
                }
            }
        }

        public string Read()
        {
            if (!(_serialPort?.BytesToRead > 0))
            {
                return null;
            }

            string receivedMessage = string.Empty;
            try
            {
                byte[] input = new byte[_serialPort.ReadBufferSize];
                int readenLength = _serialPort.Read(input, 0, input.Length);
                string asciiValue = EncodingHelper.BytesToAscii(input, readenLength);

                receivedMessage = asciiValue.Replace("\r", Environment.NewLine);
            }
            catch (TimeoutException)
            {
                receivedMessage = string.Empty;
            }

            return receivedMessage;
        }

        public void Dispose()
        {
            _serialPort.Close();
            _serialPort.Dispose();
            _serialPortCommunicator = null;
        }
    }
}