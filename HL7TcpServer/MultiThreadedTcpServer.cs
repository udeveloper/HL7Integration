using HL7NhapiClient.Helpers;
using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapi.Model.V25.Datatype;
using NHapi.Model.V25.Message;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HL7TcpServer
{
    public class MultiThreadedTcpServer
    {
        private TcpListener _tcpListener;
        private  char END_OF_BLOCK = '\u001c';
        private  char START_OF_BLOCK = '\u000b';
        private  char CARRIAGE_RETURN = (char)13;
        private static int MESSAGE_CONTROL_ID_LOCATION = 9;
        private static char FIELD_DELIMITER = '|';
        private string _extractedPdfOutputDirectory = "N:\\HL7TestOutputs";

        public void StartTcpServer(int portNumberToListenOn)
        {
            try
            {
                _tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 1080);

                //start the TCP listener that we have instantiated
                _tcpListener.Start();

                Console.WriteLine("Started server successfully...");

                while(true)
                {
                    var incomingTcpClientConnection = _tcpListener.AcceptTcpClient();

                    Console.WriteLine("Accepted incoming client connection...");

                    //create a new thread to process this client connection
                    var clientProcessingThread = new Thread(ProcessClientConnection);

                    //start processing client connections to this server
                    clientProcessingThread.Start(incomingTcpClientConnection);
                }
            }
            catch (Exception ex)
            {
                //print any exceptions during the communications to the console
                Console.WriteLine(ex.Message);
            }
            finally
            {
                //stop the TCP listener before you dispose of it
                _tcpListener?.Stop();
            }
        }

        private void ProcessClientConnection(object argumentPassedForThreadProcessing)
        {

            //the argument passed to the thread delegate is the incoming tcp client connection
            var guidConnection = Guid.NewGuid();
            var tcpClientConnection = (TcpClient)argumentPassedForThreadProcessing;
            Console.WriteLine($"A client connection was initiated from localhost {tcpClientConnection.Client.RemoteEndPoint} con id {guidConnection}");

            var receivedByteBuffer = new byte[10025];
            var netStream = tcpClientConnection.GetStream();

            try
            {
                // Keep receiving data from the client closes connection
                int bytesReceived; // Received byte count
                var hl7Data = string.Empty;

                //keeping reading until there is data available from the client and echo it back
                while ((bytesReceived = netStream.Read(receivedByteBuffer, 0, receivedByteBuffer.Length)) > 0)
                {
                    hl7Data += Encoding.UTF8.GetString(receivedByteBuffer, 0, bytesReceived);

                    // Find start of MLLP frame, a VT character ...
                    var startOfMllpEnvelope = hl7Data.IndexOf(START_OF_BLOCK);
                    if (startOfMllpEnvelope >= 0)
                    {
                        // Now look for the end of the frame, a FS character
                        var end = hl7Data.IndexOf(END_OF_BLOCK);
                        if (end >= startOfMllpEnvelope) //end of block received
                        {
                            //if both start and end of block are recognized in the data transmitted, then extract the entire message
                            var hl7MessageData = hl7Data.Substring(startOfMllpEnvelope + 1, end - startOfMllpEnvelope);

                           Console.WriteLine(hl7MessageData);

                           //GetBinaryDataMessageHL7(hl7MessageData);
                            
                            //create a HL7 acknowledgement message
                            var ackMessage = GetSimpleAcknowledgementMessage(hl7MessageData);
                            

                            Console.WriteLine(ackMessage);

                            //echo the received data back to the client 
                            var buffer = Encoding.UTF8.GetBytes(ackMessage);

                            if (netStream.CanWrite)
                            {
                                netStream.Write(buffer, 0, buffer.Length);
                                netStream.Flush();
                                Console.WriteLine("Ack message was sent back to the client...");
                            }
                        }
                    }

                }


            }
            catch (Exception e)
            {
                //print any exceptions during the communications to the console
                Console.WriteLine(e.Message);
            }
            finally
            {
                // Close the stream and the connection with the client
                netStream.Close();
                netStream.Dispose();
                tcpClientConnection.Close();
            }

        }

        private string GetSimpleAcknowledgementMessage(string incomingHl7Message)
        {
            if (string.IsNullOrEmpty(incomingHl7Message))
                throw new ApplicationException("Invalid HL7 message for parsing operation. Please check your inputs");

            //retrieve the message control ID of the incoming HL7 message 
            var messageControlId = GetMessageControlID(incomingHl7Message);            

            //build an acknowledgement message and include the control ID with it
            var ackMessage = new StringBuilder();
            ackMessage = ackMessage.Append(START_OF_BLOCK)
                .Append("MSH|^~\\&|||||||ACK||P|2.5")
                .Append(CARRIAGE_RETURN)
                .Append("MSA|AA|")
                .Append(messageControlId)
                .Append(CARRIAGE_RETURN)
                .Append(END_OF_BLOCK)
                .Append(CARRIAGE_RETURN);

            return ackMessage.ToString();
        }

        private string GetMessageControlID(string incomingHl7Message)
        {

            var fieldCount = 0;
            //parse the message into segments using the end of segment separter
            var hl7MessageSegments = incomingHl7Message.Split(CARRIAGE_RETURN);

            //tokenize the MSH segment into fields using the field separator
            var hl7FieldsInMshSegment = hl7MessageSegments[0].Split(FIELD_DELIMITER);

            //retrieve the message control ID in order to reply back with the message ack
            foreach (var field in hl7FieldsInMshSegment)
            {
                if (fieldCount == MESSAGE_CONTROL_ID_LOCATION)
                {
                    return field;
                }
                fieldCount++;
            }

            return string.Empty; //you can also throw an exception here if you wish
        }

        private void GetBinaryDataMessageHL7(string hl7MessageData)
        {
            
            var messageHL7Parsed = new PipeParser().Parse(hl7MessageData);

            if( messageHL7Parsed is ORU_R01)
            {
                var oruMessage = (ORU_R01)messageHL7Parsed;

                if (oruMessage != null)
                {
                    // Display the updated HL7 message using Pipe delimited format
                    LogToDebugConsole("Parsed HL7 Message:");
                    LogToDebugConsole(new PipeParser().Encode(messageHL7Parsed));

                    var encapsulatedPdfDataInBase64Format = ExtractEncapsulatedPdfDataInBase64Format(oruMessage);

                    //if no encapsulated data was found, you can cease operation
                    if (encapsulatedPdfDataInBase64Format == null) return;

                    var extractedPdfByteData = GetBase64DecodedPdfByteData(encapsulatedPdfDataInBase64Format);

                    WriteExtractedPdfByteDataToFile(extractedPdfByteData);
                }
            }
          
            

        }

        private byte[] GetBase64DecodedPdfByteData(ED encapsulatedPdfDataInBase64Format)
        {
            var helpeB64 = new Base64Helper();

            LogToDebugConsole("Extracting PDF data stored in Base-64 encoded form from OBX-5..");
            var base64EncodedByteData = encapsulatedPdfDataInBase64Format.Data.Value;
            var extractedPdfByteData = helpeB64.ConvertFromBase64String(base64EncodedByteData);
            return extractedPdfByteData;
        }

        private ED ExtractEncapsulatedPdfDataInBase64Format(ORU_R01 oruMessage)
        {            
            //start retrieving the OBX segment data to get at the PDF report content
            LogToDebugConsole("Extracting message data from parsed message..");
            var orderObservation = oruMessage.GetPATIENT_RESULT().GetORDER_OBSERVATION();
            var observation = orderObservation.GetOBSERVATION(0);
            var obxSegment = observation.OBX;

            var encapsulatedPdfDataInBase64Format = obxSegment.GetObservationValue(0).Data as ED;            
            return encapsulatedPdfDataInBase64Format;
        }

        private  void WriteExtractedPdfByteDataToFile(byte[] extractedPdfByteData)
        {
            LogToDebugConsole($"Creating output directory at '{_extractedPdfOutputDirectory}'..");

            if (!Directory.Exists(_extractedPdfOutputDirectory))
                Directory.CreateDirectory(_extractedPdfOutputDirectory);

            var pdfOutputFile = Path.Combine(_extractedPdfOutputDirectory, Guid.NewGuid() + ".pdf");
            LogToDebugConsole(
                $"Writing the extracted PDF data to '{pdfOutputFile}'. You should be able to see the decoded PDF content..");
            try
            {
                File.WriteAllBytes(pdfOutputFile, extractedPdfByteData);
            }
            catch (Exception ex)
            {
                LogToDebugConsole("Extraction operation was successfully completed.. - " + ex.Message);
            }
                     
        }

        private static void LogToDebugConsole(string informationToLog)
        {
            Debug.WriteLine(informationToLog);
        }

    }
}
