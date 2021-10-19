using System;
using System.Collections.Generic;
using System.Text;

namespace PDFCreator
{
    class ServerHandle
    {
        public static void WelcomeReceived(int _fromClient, Packet _packet)
        {
            int _clientIdCheck = _packet.ReadInt();
            string _username = _packet.ReadString();

            Console.WriteLine($"{Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} connected successfully. Welcome {_username}.");
            if (_fromClient != _clientIdCheck)
            {
                Console.WriteLine($"Player \"{_username}\" (ID: {_fromClient}) has assumed the wrong client ID ({_clientIdCheck})!");
            }
        }

        public static void MakeNewDocument(int _fromClient, Packet _packet)
        {
            string name = _packet.ReadString();
            int amountOfPages = _packet.ReadInt();
            double width = _packet.ReadDouble();
            double height = _packet.ReadDouble();

            PDFCreator.pdfCreator.CreateDocument(name, amountOfPages, width, height);
            //Console.WriteLine($"{name} Page received! Amount of pages: {amountOfPages}, Width: {width}, Height: {height}");
        }

        public static void ImageReceived(int _fromClient, Packet _packet)
        {
            string documentName = _packet.ReadString();
            string path = _packet.ReadString();
            int pageNR = _packet.ReadInt();
            double pivotX = _packet.ReadDouble();
            double pivotY = _packet.ReadDouble();
            double sizeX = _packet.ReadDouble();
            double sizeY = _packet.ReadDouble();
            double posX = _packet.ReadDouble();
            double posY = _packet.ReadDouble();

            PDFCreator.pdfCreator.DrawImage(documentName, path, pageNR, pivotX, pivotY, sizeX, sizeY, posX, posY);
            //Console.WriteLine($"Image received! Path: {path}, Width: {sizeX}, Height: {sizeY}, PositionX: {posX}, PositionY: {posY}");
        }

        public static void RTFTextWithTagsReceived(int _fromClient, Packet _packet)
        {
            string documentName = _packet.ReadString();
            string RTFtext = _packet.ReadString();
            int pageNR = _packet.ReadInt();
            string fontFamily = _packet.ReadString();
            double fontSize = _packet.ReadDouble();
            int fontStyle = _packet.ReadInt(); //Has to align with XFontStyle
            int alignment = _packet.ReadInt(); //Has to align with XParagraphAlignment
            double lineSpace = _packet.ReadDouble();
            double paragraphSpace = _packet.ReadDouble();
            double pivotX = _packet.ReadDouble();
            double pivotY = _packet.ReadDouble();
            double sizeX = _packet.ReadDouble();
            double sizeY = _packet.ReadDouble();
            double posX = _packet.ReadDouble();
            double posY = _packet.ReadDouble();

            PDFCreator.pdfCreator.DrawRTFTagString(documentName, RTFtext, pageNR, fontFamily, fontSize, fontStyle, alignment, lineSpace, paragraphSpace, pivotX, pivotY, sizeX, sizeY, posX, posY);
            //Console.WriteLine($"RTFText received! text: {RTFtext}, Width: {sizeX}, Height: {sizeY}, PositionX: {posX}, PositionY: {posY}");
        }

        public static void SaveDocument(int _fromClient, Packet _packet)
        {
            string path = _packet.ReadString();
            string name = _packet.ReadString();
            PDFCreator.pdfCreator.SaveDocument(path, name);
        }

        public static void Disconnect(int _fromClient, Packet _packet)
        {
            PDFCreator.pdfCreator.Reset();
            Server.DisconnectClient(_fromClient);
            Console.WriteLine($"Client {_fromClient} Disconnected");
        }
    }
}
